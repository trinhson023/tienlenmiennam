// turnTimer.js - authoritative 60s turn timeout watchdog
import { Client } from "boardgame.io/client";
import { SocketIO } from "boardgame.io/multiplayer";
import http from "http";
import { default as TienLen } from "./src/TienLen";
import { Combinations } from "./src/constants";

const pendingTimeouts = new Set();

function httpRequest(options) {
  return new Promise((resolve, reject) => {
    const req = http.request(options, res => {
      let body = "";
      res.on("data", chunk => (body += chunk));
      res.on("end", () => {
        try {
          resolve(JSON.parse(body));
        } catch (e) {
          resolve(null);
        }
      });
    });
    req.on("error", reject);
    req.end();
  });
}

function chooseOpeningCard(hand) {
  if (!hand || hand.length === 0) return null;

  // If 3♠ is still in hand, the game requires it in the first play.
  const threeSpades = hand.find(card => card.rank === "3" && card.suit === "S");
  if (threeSpades) return threeSpades;

  // Preserve a 2 when possible. Hands are already sorted from low to high.
  return hand.find(card => card.rank !== "2") || hand[0];
}

function runTimeoutMove(matchID, playerID, credentials, port, expectedTurn) {
  return new Promise(resolve => {
    const config = {
      game: TienLen,
      gameID: matchID,
      playerID: String(playerID),
      multiplayer: SocketIO({ server: `http://localhost:${port}` }),
    };
    if (credentials) config.credentials = credentials;

    const client = Client(config);
    let finished = false;
    let unsubscribe = null;

    const cleanup = result => {
      if (finished) return;
      finished = true;
      if (unsubscribe) unsubscribe();
      try {
        client.stop();
      } catch (e) {
        // no-op
      }
      resolve(result);
    };

    const hardTimeout = setTimeout(() => cleanup(false), 5000);
    const finishSoon = result => {
      clearTimeout(hardTimeout);
      setTimeout(() => cleanup(result), 650);
    };

    client.start();
    unsubscribe = client.subscribe(state => {
      if (finished || !state || !state.G || !state.ctx) return;

      // The real player may have moved while the timeout client was connecting.
      // Never act on a newer turn or on a game that already ended.
      if (
        state.ctx.gameover ||
        String(state.ctx.currentPlayer) !== String(playerID) ||
        Number(state.ctx.turn) !== Number(expectedTurn) ||
        !state.G.turnDeadline ||
        Date.now() < Number(state.G.turnDeadline)
      ) {
        clearTimeout(hardTimeout);
        cleanup(false);
        return;
      }

      const player = state.G.players && state.G.players[playerID];
      const hand = (player && player.hand) || [];
      const center = state.G.center || [];
      const stage =
        state.ctx.activePlayers && state.ctx.activePlayers[String(playerID)];
      const canPass =
        state.G.roundType !== Combinations.ANY && center.length > 0;

      try {
        if (canPass && stage !== "tienLen" && client.moves.passTurn) {
          console.log(
            `[TURN TIMER] ${matchID} ghế ${playerID} hết 60s -> auto Pass`
          );
          client.moves.passTurn();
          finishSoon(true);
          return;
        }

        const card = chooseOpeningCard(hand);
        if (!card) {
          console.warn(
            `[TURN TIMER] ${matchID} ghế ${playerID} hết giờ nhưng không còn bài để đánh`
          );
          finishSoon(false);
          return;
        }

        const cardID = `${card.rank}${card.suit}`;
        console.log(
          `[TURN TIMER] ${matchID} ghế ${playerID} hết 60s -> auto đánh ${cardID}`
        );

        if (stage === "tienLen" && client.moves.tienLenPlayDirect) {
          client.moves.tienLenPlayDirect([cardID]);
        } else if (client.moves.playCardsDirect) {
          client.moves.playCardsDirect([cardID]);
        }
        finishSoon(true);
      } catch (err) {
        console.error(`[TURN TIMER] auto action error:`, err);
        finishSoon(false);
      }
    });
  });
}

export function startTurnTimerWatchdog(db, port = 8000) {
  if (!db) return;

  setInterval(async () => {
    let roomsData;
    try {
      roomsData = await httpRequest({
        hostname: "localhost",
        port,
        path: "/games/tien-len",
        method: "GET",
      });
    } catch (e) {
      return;
    }

    if (!roomsData || !roomsData.rooms) return;

    for (const room of roomsData.rooms) {
      try {
        const result = await db.fetch(room.gameID, {
          state: true,
          metadata: true,
        });
        const state = result && result.state;
        const metadata = result && result.metadata;
        if (!state || !state.G || !state.ctx || state.ctx.gameover) continue;

        const deadline = Number(state.G.turnDeadline || 0);
        if (!deadline || Date.now() < deadline) continue;

        const playerID = String(state.ctx.currentPlayer);
        const turn = Number(state.ctx.turn);
        const token = `${room.gameID}:${turn}:${playerID}`;
        if (pendingTimeouts.has(token)) continue;

        const playerMeta =
          metadata && metadata.players ? metadata.players[playerID] : null;
        const credentials = playerMeta && playerMeta.credentials;

        pendingTimeouts.add(token);
        runTimeoutMove(room.gameID, playerID, credentials, port, turn)
          .catch(err => {
            console.error(`[TURN TIMER] ${room.gameID} timeout move failed:`, err);
          })
          .finally(() => {
            // Keep the token briefly to avoid duplicate timeout clients during
            // the socket round-trip before the new turn reaches storage.
            setTimeout(() => pendingTimeouts.delete(token), 2500);
          });
      } catch (e) {
        // Ignore a room that disappeared between list and fetch.
      }
    }
  }, 1000);

  console.log("[TURN TIMER] 60-second turn watchdog enabled");
}
