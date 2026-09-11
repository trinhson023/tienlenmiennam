// src/moves/cardPlayMoves.js
import { Combinations } from "../constants";
import {
  validCombination,
  validChop,
  compareHighest,
  compareCards,
} from "./helper-functions/cardComparison";
const _ = require("lodash");

export function validPlay(stagingArea, roundType, center, threeSpadesInHand, totalCardsInHand) {
  const handType = validCombination(stagingArea);
  if (stagingArea.length === 0 || handType === undefined) {
    return "Invalid Combination";
  } else if (threeSpadesInHand) {
    return "First Play Must Include 3♠";
  }

  // Thối Heo: Không được đánh quân 2 làm nước bài cuối cùng để về đích
  if (
    totalCardsInHand !== undefined &&
    stagingArea.length === totalCardsInHand &&
    stagingArea.some((c) => c.rank === "2")
  ) {
    return "Không được về bằng Heo (Thối Heo)";
  }

  if (roundType === Combinations.ANY || validChop(center, stagingArea)) {
    return true;
  } else if (roundType !== handType || stagingArea.length !== center.length) {
    return "Does Not Match Center";
  } else if (compareHighest(stagingArea, center) !== 1) {
    return "Does Not Beat Center";
  } else {
    return true;
  }
}

export function tienLenPlay(G, ctx) {
  let stagingArea = G.players[ctx.currentPlayer].stagingArea;
  const handType = validCombination(stagingArea);
  if (validChop(G.center, stagingArea)) {
    G.roundType = handType;
  } else if (
    G.roundType !== handType ||
    compareHighest(stagingArea, G.center) !== 1 ||
    stagingArea.length !== G.center.length
  ) {
    ctx.events.setStage("notTurn");
    const numPlayers = ctx.numPlayers || Object.keys(G.players).length;
    const allPlayerIds = Array.from({ length: numPlayers }, (_, i) => i);
    G.turnOrder = allPlayerIds.map((x) =>
      G.winners.includes(x.toString()) ? null : x
    );
    G.roundType = handType;
  }
  cardsToCenter(G, ctx);
}

export function playCardsDirect(G, ctx, cardIds) {
  const currentPlayer = ctx.currentPlayer;
  if (!G.players || !G.players[currentPlayer]) return;
  G.players[currentPlayer].stagingArea = [];
  for (const cid of cardIds) {
    const idx = G.players[currentPlayer].hand.findIndex(
      (c) => c.rank + c.suit === cid
    );
    if (idx !== -1) {
      const [c] = G.players[currentPlayer].hand.splice(idx, 1);
      G.players[currentPlayer].stagingArea.push(c);
    }
  }
  cardsToCenter(G, ctx);
}

export function tienLenPlayDirect(G, ctx, cardIds) {
  const currentPlayer = ctx.currentPlayer;
  if (!G.players || !G.players[currentPlayer]) return;
  G.players[currentPlayer].stagingArea = [];
  for (const cid of cardIds) {
    const idx = G.players[currentPlayer].hand.findIndex(
      (c) => c.rank + c.suit === cid
    );
    if (idx !== -1) {
      const [c] = G.players[currentPlayer].hand.splice(idx, 1);
      G.players[currentPlayer].stagingArea.push(c);
    }
  }
  tienLenPlay(G, ctx);
}

export function cardsToCenter(G, ctx) {
  const currentPlayer = ctx.currentPlayer;
  let stagingArea = G.players[currentPlayer].stagingArea;
  G.roundType = validCombination(stagingArea);

  G.center = _.cloneDeep(stagingArea).sort(compareCards);
  G.players[currentPlayer].stagingArea = [];
  G.cardsLeft[currentPlayer] -= G.center.length;
  G.lastPlayBy = parseInt(currentPlayer); // Người vừa ra nước bài cao nhất hiện tại trên bàn

  if (G.cardsLeft[currentPlayer] === 0) {
    G.winners.push(currentPlayer);
    G.turnOrder = G.turnOrder.map((x) => (x === "W" ? null : x));
    G.turnOrder[parseInt(ctx.currentPlayer)] = "W";
  }
  nextTurn(G, ctx);
}

export function passTurn(G, ctx) {
  if (G.roundType === Combinations.ANY || !G.center || G.center.length === 0) {
    return;
  }
  G.turnOrder[parseInt(ctx.currentPlayer)] = null;
  nextTurn(G, ctx);
}

function nextTurn(G, ctx) {
  const numPlayers = ctx.numPlayers || Object.keys(G.players).length;
  let currentPlayer = parseInt(ctx.currentPlayer);

  // Danh sách những người còn quyền đánh trong vòng hiện tại
  const activeInTrick = G.turnOrder.filter(
    (x) => x !== null && x !== "W"
  );

  // Khi tất cả đối thủ đã bỏ lượt (activeInTrick <= 1): Vòng bài kết thúc!
  if (activeInTrick.length <= 1) {
    // Người thắng vòng là người vừa ra nước bài cuối cùng chưa ai đè được (G.lastPlayBy)
    let trickWinner =
      G.lastPlayBy !== undefined && G.lastPlayBy !== null
        ? G.lastPlayBy
        : (activeInTrick.length === 1 ? activeInTrick[0] : currentPlayer);

    // LUẬT HƯỞNG SÁI TIẾN LÊN MIỀN NAM:
    // Nếu người thắng vòng đã hết bài về đích, người ngồi KẾ TIẾP theo chiều đánh bài
    // (người bên tay phải / người có lượt tiếp theo theo vòng) sẽ được HƯỞNG SÁI để mở vòng mới!
    if (G.winners.includes(trickWinner.toString())) {
      trickWinner = getHuongSaiPlayer(trickWinner, G.winners, numPlayers);
    }

    // Dọn sạch bàn cho vòng bài mới tinh
    G.center = [];
    G.roundType = Combinations.ANY;
    G.lastPlayBy = null;
    G.turnOrder = Array.from({ length: numPlayers }, (_, i) =>
      G.winners.includes(i.toString()) ? null : i
    );

    ctx.events.endTurn({ next: trickWinner.toString() });
    return;
  }

  // Chuyển lượt bình thường cho người tiếp theo trong vòng
  let nextPlayer = findNextPlayer(G.turnOrder, currentPlayer, numPlayers);
  if (nextPlayer === "W") {
    G.turnOrder = G.turnOrder.map((x) => (x === "W" ? null : x));
    nextPlayer = findNextPlayer(G.turnOrder, currentPlayer, numPlayers);
  }

  ctx.events.endTurn({ next: nextPlayer.toString() });
}

// Tìm người kế tiếp theo vòng tròn chưa về đích để hưởng sái
function getHuongSaiPlayer(lastWinner, winners, numPlayers) {
  for (let i = 1; i < numPlayers; i++) {
    const candidate = (lastWinner + i) % numPlayers;
    if (!winners.includes(candidate.toString())) {
      return candidate;
    }
  }
  return lastWinner;
}

function findNextPlayer(turnOrder, currentPlayer, numPlayers) {
  let playerList = turnOrder
    .slice(currentPlayer + 1, numPlayers)
    .concat(turnOrder.slice(0, currentPlayer + 1));
  let found = playerList.find((i) => i !== null);
  return found !== undefined ? found.toString() : currentPlayer.toString();
}
