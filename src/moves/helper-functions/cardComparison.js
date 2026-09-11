// src/moves/cardComparisonFunctions.js

import { Suits, Ranks, Combinations } from "../../constants";
const _ = require("lodash");

export function compareCards(card1, card2) {
  /*
    For two cards, returns 1 if if card1 is higher than card2,
    0 if they are the same, and -1 if card1 is lower than card2
  */

  // compare by ranks first
  let rank1Position = Ranks.indexOf(card1.rank);
  let rank2Position = Ranks.indexOf(card2.rank);
  if (rank1Position > rank2Position) {
    return 1;
  } else if (rank1Position < rank2Position) {
    return -1;
  } else {
    let suit1Position = Suits.indexOf(card1.suit);
    let suit2Position = Suits.indexOf(card2.suit);

    if (suit1Position > suit2Position) {
      return 1;
    } else if (suit1Position < suit2Position) {
      return -1;
    }
  }
  return 0;
}

export function compareHighest(cards1, cards2) {
  /*
    For two sets of cards, returns 1 if the highest card of cards1
    is higher than the highest cards of cards2, -1 if opposite,
    and undefined if card lists are empty or malformed
  */
  const cards1Copy = _.cloneDeep(cards1);
  const cards2Copy = _.cloneDeep(cards2);
  if (cards1.length === 0) {
    return -1;
  } else if (cards2.length === 0) {
    return 1;
  } else {
    const card1 = _.last(cards1Copy.sort(compareCards));
    const card2 = _.last(cards2Copy.sort(compareCards));
    return compareCards(card1, card2);
  }
}

export function validCombination(cards) {
  /*
    for an array of cards, returns true if
    the combination is a normal combination
  */

  if (cards.length === 1) {
    // single
    return Combinations.SINGLE;
  } else if (
    cards.length === 2 &&
    cards.every(card => card.rank === cards[0].rank)
  ) {
    // pair
    return Combinations.PAIR;
  } else if (
    cards.length === 3 &&
    cards.every(card => card.rank === cards[0].rank)
  ) {
    // triple
    return Combinations.TRIPLE;
  } else if (cards.length >= 3 && cards.every(card => card.rank !== "2")) {
    // straight
    const cardsCopy = _.cloneDeep(cards);
    cardsCopy.sort(compareCards);
    let ranks = cardsCopy.map(card => card.rank);
    if (consecutive(ranks)) {
      return Combinations.STRAIGHT;
    }
  } else if (cards.some(card => card.rank === 2)) {
    return undefined;
  }
  // four of a kind
  if (cards.length === 4 && cards.every(card => card.rank === cards[0].rank)) {
    return Combinations.FOUROFAKIND;
  }
  // three or four pair
  let groupedCards = _.groupBy(cards, "rank");
  // only pairs
  let onlyPairs = _.every(groupedCards, card => {
    return card.length === 2;
  });
  // consecutiveSuits
  const cardsCopy = _.cloneDeep(cards);
  cardsCopy.sort(compareCards);
  let ranks = _.uniq(cardsCopy.map(card => card.rank));
  let consecutiveSuits = consecutive(ranks);

  if (onlyPairs && consecutiveSuits) {
    if (cards.length === 6) {
      return Combinations.THREEPAIR;
    } else if (cards.length === 8) {
      return Combinations.FOURPAIR;
    }
  }
  return undefined;
}

export function validChop(center, cards) {
  if (!center || center.length === 0 || !cards || cards.length === 0) {
    return false;
  }
  const combo = validCombination(cards);
  const centerCombo = validCombination(center);
  const centerAllTwos = center.every(card => card.rank === "2");

  // 1. Chặt 1 con Heo (bằng 3 đôi thông, Tứ Quý, hoặc 4 đôi thông)
  if (center.length === 1 && center[0].rank === "2") {
    return [
      Combinations.THREEPAIR,
      Combinations.FOUROFAKIND,
      Combinations.FOURPAIR,
    ].includes(combo);
  }

  // 2. Chặt Đôi Heo (Tứ Quý hoặc 4 đôi thông chặt được Đôi Heo)
  if (center.length === 2 && centerAllTwos) {
    return [
      Combinations.FOUROFAKIND,
      Combinations.FOURPAIR,
    ].includes(combo);
  }

  // 3. Chặt đè 3 Đôi Thông (Tứ Quý hoặc 4 đôi thông chặt đè được)
  if (centerCombo === Combinations.THREEPAIR) {
    return [
      Combinations.FOUROFAKIND,
      Combinations.FOURPAIR,
    ].includes(combo);
  }

  // 4. Chặt đè Tứ Quý (4 đôi thông chặt đè được Tứ Quý)
  if (centerCombo === Combinations.FOUROFAKIND) {
    return combo === Combinations.FOURPAIR;
  }

  return false;
}

function consecutive(ranks) {
  let firstIndex = Ranks.indexOf(ranks[0]);
  return _.isEqual(Ranks.slice(firstIndex, firstIndex + ranks.length), ranks);
}
