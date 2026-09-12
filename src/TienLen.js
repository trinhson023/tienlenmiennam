// src/TienLen.js

import { PlayerView, Stage } from "boardgame.io/core";
import { Suits, Ranks, Combinations } from "./constants";
import {
  cardsToCenter,
  passTurn,
  tienLenPlay,
  playCardsDirect,
  tienLenPlayDirect,
} from "./moves/cardPlayMoves";
import {
  relocateCards,
  clearStagingArea,
  sortStagingArea,
  toggleCard,
  sortHand,
} from "./moves/cardAreaMoves";
import { compareCards } from "./moves/helper-functions/cardComparison";
const _ = require("lodash");

export const TURN_TIME_MS = 60 * 1000;

function beginTimedTurn(G) {
  const now = Date.now();
  G.turnStartedAt = now;
  G.turnDeadline = now + TURN_TIME_MS;
  return G;
}

function sendEmote(G, ctx, text) {
  G.lastEmote = {
    playerID: ctx.playerID,
    text: text,
    time: Date.now(),
  };
}

function isMusicHost(ctx) {
  return ctx.playerID !== undefined && String(ctx.playerID) === "0";
}

function safeTrack(track) {
  if (!track || !track.videoId) return null;
  return {
    videoId: String(track.videoId).slice(0, 32),
    title: String(track.title || "YouTube").slice(0, 160),
    channelTitle: String(track.channelTitle || "").slice(0, 100),
    thumbnail: String(track.thumbnail || "").slice(0, 500),
  };
}

function musicSelect(G, ctx, track) {
  if (!isMusicHost(ctx)) return;
  const clean = safeTrack(track);
  if (!clean) return;
  G.musicRoom.current = clean;
  G.musicRoom.playing = true;
  G.musicRoom.position = 0;
  G.musicRoom.startedAt = Date.now();
  G.musicRoom.revision += 1;
}

function musicQueue(G, ctx, track) {
  if (!isMusicHost(ctx)) return;
  const clean = safeTrack(track);
  if (!clean) return;
  G.musicRoom.queue = G.musicRoom.queue.concat(clean).slice(0, 20);
  G.musicRoom.revision += 1;
}

function musicToggle(G, ctx, playing, position) {
  if (!isMusicHost(ctx) || !G.musicRoom.current) return;
  const pos = Math.max(0, Number(position) || 0);
  G.musicRoom.position = pos;
  G.musicRoom.playing = Boolean(playing);
  G.musicRoom.startedAt = playing ? Date.now() - pos * 1000 : null;
  G.musicRoom.revision += 1;
}

function musicSeek(G, ctx, position) {
  if (!isMusicHost(ctx) || !G.musicRoom.current) return;
  const pos = Math.max(0, Number(position) || 0);
  G.musicRoom.position = pos;
  if (G.musicRoom.playing) {
    G.musicRoom.startedAt = Date.now() - pos * 1000;
  }
  G.musicRoom.revision += 1;
}

function musicNext(G, ctx) {
  if (!isMusicHost(ctx)) return;
  if (G.musicRoom.queue.length > 0) {
    const next = G.musicRoom.queue[0];
    G.musicRoom.queue = G.musicRoom.queue.slice(1);
    G.musicRoom.current = next;
    G.musicRoom.playing = true;
    G.musicRoom.position = 0;
    G.musicRoom.startedAt = Date.now();
  } else {
    G.musicRoom.playing = false;
    G.musicRoom.position = 0;
    G.musicRoom.startedAt = null;
  }
  G.musicRoom.revision += 1;
}

function musicClearQueue(G, ctx) {
  if (!isMusicHost(ctx)) return;
  G.musicRoom.queue = [];
  G.musicRoom.revision += 1;
}

const socialMoves = {
  sendEmote,
  musicSelect,
  musicQueue,
  musicToggle,
  musicSeek,
  musicNext,
  musicClearQueue,
};

const TienLen = {
  name: "tien-len",
  minPlayers: 2,
  maxPlayers: 4,
  setup: setUp,
  moves: {
    relocateCards,
    clearStagingArea,
    cardsToCenter,
    passTurn,
    tienLenPlay,
    sortStagingArea,
    toggleCard,
    sortHand,
    playCardsDirect,
    tienLenPlayDirect,
    ...socialMoves,
  },
  stages: {
    tienLen: {
      moves: {
        tienLenPlay,
        tienLenPlayDirect,
        playCardsDirect,
        toggleCard,
        sortHand,
        clearStagingArea,
        sortStagingArea,
        relocateCards,
        ...socialMoves,
      },
    },
    notTurn: {
      moves: {
        relocateCards,
        clearStagingArea,
        sortStagingArea,
        toggleCard,
        sortHand,
        ...socialMoves,
      },
    },
  },
  turn: {
    order: {
      first: G => G.firstPlayer,
    },
    onBegin: beginTimedTurn,
    activePlayers: {
      currentPlayer: { stage: Stage.NULL },
      others: { stage: "notTurn" },
    },
  },
  playerView: PlayerView.STRIP_SECRETS,
  endIf: (G, ctx) => {
    const numPlayers = ctx.numPlayers || Object.keys(G.players).length;
    if (G.winners.length === numPlayers - 1) {
      const allPlayers = Array.from({ length: numPlayers }, (_, i) => i.toString());
      const w = G.winners.concat(
        allPlayers.filter(x => !G.winners.includes(x))
      );
      return { winners: w };
    }
  },
};

function setUp(ctx) {
  let deck = [];
  for (let suit of Suits) {
    for (let rank of Ranks) {
      deck.push({ suit: suit, rank: rank });
    }
  }

  const n = ctx.random.Die(4);
  for (let i = 1 + n; i > 0; i--) {
    deck = ctx.random.Shuffle(deck);
  }
  const chunkedDeck = _.chunk(deck, 13).map(x => x.sort(compareCards));

  const numPlayers = ctx.numPlayers || 4;
  const players = {};
  const cardsLeft = {};
  const initialTurnOrder = [];
  let firstPlayer = 0;
  let hasThreeSpades = false;

  for (let i = 0; i < numPlayers; i++) {
    initialTurnOrder.push(i);
    cardsLeft[i] = 13;
    players[i] = {
      hand: chunkedDeck[i],
      stagingArea: [],
    };
    if (_.find(chunkedDeck[i], { rank: "3", suit: "S" })) {
      firstPlayer = i;
      hasThreeSpades = true;
    }
  }

  if (!hasThreeSpades) {
    let minCard = null;
    let minPlayer = 0;
    for (let i = 0; i < numPlayers; i++) {
      const playerLowest = chunkedDeck[i][0];
      if (!minCard || compareCards(playerLowest, minCard) === -1) {
        minCard = playerLowest;
        minPlayer = i;
      }
    }
    firstPlayer = minPlayer;
  }

  return {
    turnOrder: initialTurnOrder,
    center: [],
    players,
    roundType: Combinations.ANY,
    winners: [],
    firstPlayer,
    cardsLeft,
    lastEmote: null,
    lastPlayBy: null,
    turnStartedAt: null,
    turnDeadline: null,
    musicRoom: {
      current: null,
      queue: [],
      playing: false,
      position: 0,
      startedAt: null,
      revision: 0,
    },
  };
}

export default TienLen;
