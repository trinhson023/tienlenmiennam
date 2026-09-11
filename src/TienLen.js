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

function sendEmote(G, ctx, text) {
  G.lastEmote = {
    playerID: ctx.playerID,
    text: text,
    time: Date.now(),
  };
}

const TienLen = {
  name: "tien-len",
  minPlayers: 2,
  maxPlayers: 4,
  setup: setUp,
  moves: {
    relocateCards: relocateCards,
    clearStagingArea: clearStagingArea,
    cardsToCenter: cardsToCenter,
    passTurn: passTurn,
    tienLenPlay: tienLenPlay,
    sortStagingArea: sortStagingArea,
    toggleCard: toggleCard,
    sortHand: sortHand,
    playCardsDirect: playCardsDirect,
    tienLenPlayDirect: tienLenPlayDirect,
    sendEmote: sendEmote,
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
        sendEmote,
      },
    },
    notTurn: {
      moves: {
        relocateCards,
        clearStagingArea,
        sortStagingArea,
        toggleCard,
        sortHand,
        sendEmote,
      },
    },
  },
  turn: {
    order: {
      first: G => G.firstPlayer,
    },
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
      let w = G.winners.concat(
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

  // Nếu không ai có 3 Bích (xảy ra khi chơi 2 hoặc 3 người), người có lá bài nhỏ nhất sẽ đi trước
  if (!hasThreeSpades) {
    let minCard = null;
    let minPlayer = 0;
    for (let i = 0; i < numPlayers; i++) {
      let playerLowest = chunkedDeck[i][0];
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
    players: players,
    roundType: Combinations.ANY,
    winners: [],
    firstPlayer: firstPlayer,
    cardsLeft: cardsLeft,
    lastEmote: null,
    lastPlayBy: null,
  };
}

export default TienLen;
