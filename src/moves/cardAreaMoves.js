// src/moves/cardAreaMoves.js
import { compareCards } from "./helper-functions/cardComparison";
const _ = require("lodash");

export function relocateCards(G, ctx, result) {
  if (!result.destination) return;
  const players = G.players;
  const playerID = ctx.playerID;
  const source = result.source.droppableId;
  const destination = result.destination.droppableId;
  if (source === destination) {
    const items = _.cloneDeep(players[playerID][source]);
    const [reorderedItem] = items.splice(result.source.index, 1);
    items.splice(result.destination.index, 0, reorderedItem);
    players[playerID][source] = items;
  } else {
    const sourceItems = _.cloneDeep(players[playerID][source]);
    const destinationItems = _.cloneDeep(players[playerID][destination]);
    const [reorderedItem] = sourceItems.splice(result.source.index, 1);
    destinationItems.splice(result.destination.index, 0, reorderedItem);
    players[playerID][source] = sourceItems;
    players[playerID][destination] = destinationItems;
  }
}

export function toggleCard(G, ctx, cardId, currentArea) {
  const players = G.players;
  const playerID = ctx.playerID;
  if (!players || !players[playerID]) return;

  if (currentArea === "hand") {
    // Click trong tay -> đưa lên ô chọn bài
    const cardIndex = players[playerID].hand.findIndex(
      c => (c.rank + c.suit) === cardId
    );
    if (cardIndex !== -1) {
      const [card] = players[playerID].hand.splice(cardIndex, 1);
      players[playerID].stagingArea.push(card);
    }
  } else if (currentArea === "stagingArea") {
    // Click ở ô chọn bài -> hạ bài về lại tay và tự sắp xếp
    const cardIndex = players[playerID].stagingArea.findIndex(
      c => (c.rank + c.suit) === cardId
    );
    if (cardIndex !== -1) {
      const [card] = players[playerID].stagingArea.splice(cardIndex, 1);
      players[playerID].hand.push(card);
      players[playerID].hand.sort(compareCards);
    }
  }
}

export function clearStagingArea(G, ctx) {
  const players = G.players;
  const playerID = ctx.playerID;
  if (!players || !players[playerID]) return;
  players[playerID].hand = players[playerID].hand.concat(
    players[playerID].stagingArea
  );
  players[playerID].hand.sort(compareCards);
  players[playerID].stagingArea = [];
}

export function sortStagingArea(G, ctx) {
  const players = G.players;
  const playerID = ctx.playerID;
  if (!players || !players[playerID]) return;
  players[playerID].stagingArea.sort(compareCards);
}

export function sortHand(G, ctx) {
  const players = G.players;
  const playerID = ctx.playerID;
  if (!players || !players[playerID]) return;
  players[playerID].hand.sort(compareCards);
}
