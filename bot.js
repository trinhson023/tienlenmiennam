// bot.js - AI Opponents for Tien Len (Multi-Bot Support & Smart Play)
import { Client } from "boardgame.io/client";
import { SocketIO } from "boardgame.io/multiplayer";
import http from "http";
import { default as TienLen } from "./src/TienLen";
import {
  compareCards,
  validCombination,
  compareHighest,
  validChop,
} from "./src/moves/helper-functions/cardComparison";
import { Combinations, Ranks } from "./src/constants";
const _ = require("lodash");

// Helper to make local HTTP requests
function httpRequest(options, postData) {
  return new Promise((resolve, reject) => {
    const req = http.request(options, (res) => {
      let body = "";
      res.on("data", (chunk) => (body += chunk));
      res.on("end", () => {
        try {
          resolve(JSON.parse(body));
        } catch (e) {
          resolve(body);
        }
      });
    });
    req.on("error", reject);
    if (postData) {
      req.write(JSON.stringify(postData));
    }
    req.end();
  });
}

// Helper to find a straight of specific length in hand that beats center
function findStraightMove(hand, center) {
  const length = center.length;
  // Sảnh chỉ từ 3 đến A, bỏ lá 2
  const noTwos = hand.filter((c) => c.rank !== "2");
  const grouped = _.groupBy(noTwos, "rank");

  // Ranks hợp lệ cho sảnh theo thứ tự từ 3 đến A
  const validRanks = Ranks.filter((r) => r !== "2");

  // Tìm tất cả các chuỗi rank liên tiếp độ dài 'length'
  for (let i = 0; i <= validRanks.length - length; i++) {
    const subRanks = validRanks.slice(i, i + length);
    // Kiểm tra xem hand có đủ tất cả các rank trong subRanks không
    const hasAll = subRanks.every((r) => grouped[r] && grouped[r].length > 0);
    if (hasAll) {
      // Chọn 1 lá cho mỗi rank (ưu tiên lá chất cao nhất ở rank cuối để so sánh)
      const straight = subRanks.map((r) => {
        const cardsOfRank = grouped[r];
        return cardsOfRank[cardsOfRank.length - 1]; // lá chất cao nhất của rank đó
      });

      if (compareHighest(straight, center) === 1) {
        return straight;
      }
    }
  }
  return null;
}

// Bot AI algorithm to choose which cards to play
function findBotMove(hand, roundType, center, hasThreeSpades) {
  if (!hand || hand.length === 0) return null;

  // 1. Nước đầu tiên: Bắt buộc phải có 3 Bích (3♠)
  if (hasThreeSpades) {
    const threeSpades = hand.find((c) => c.rank === "3" && c.suit === "S");
    if (threeSpades) {
      return [threeSpades];
    }
  }

  // 2. Quyền đánh tự do (Round mới / ANY)
  if (roundType === Combinations.ANY || !center || center.length === 0) {
    // Không đánh quân 2 đầu tiên nếu còn các lá khác (để dành chặt hoặc chặn)
    const nonTwos = hand.filter((c) => c.rank !== "2");
    if (nonTwos.length > 0) {
      return [nonTwos[0]];
    }
    return [hand[0]];
  }

  // 3. Đang đè lá đơn (SINGLE)
  if (roundType === Combinations.SINGLE) {
    const target = center[0];
    for (const card of hand) {
      if (compareCards(card, target) === 1) {
        return [card];
      }
    }
    // Chặt Heo bằng Tứ Quý
    if (target.rank === "2") {
      const grouped = _.groupBy(hand, "rank");
      for (const rank in grouped) {
        if (grouped[rank].length === 4) {
          return grouped[rank];
        }
      }
    }
    return null;
  }

  // 4. Đang đè Đôi (PAIR)
  if (roundType === Combinations.PAIR) {
    const grouped = _.groupBy(hand, "rank");
    for (const rank in grouped) {
      if (grouped[rank].length >= 2) {
        const pair = grouped[rank].slice(0, 2);
        if (compareHighest(pair, center) === 1) {
          return pair;
        }
      }
    }
    // Chặt đôi Heo bằng Tứ Quý
    if (center[0].rank === "2") {
      const grouped = _.groupBy(hand, "rank");
      for (const rank in grouped) {
        if (grouped[rank].length === 4) {
          return grouped[rank];
        }
      }
    }
    return null;
  }

  // 5. Đang đè Ba (TRIPLE)
  if (roundType === Combinations.TRIPLE) {
    const grouped = _.groupBy(hand, "rank");
    for (const rank in grouped) {
      if (grouped[rank].length >= 3) {
        const triple = grouped[rank].slice(0, 3);
        if (compareHighest(triple, center) === 1) {
          return triple;
        }
      }
    }
    return null;
  }

  // 6. Đang đè Sảnh (STRAIGHT)
  if (roundType === Combinations.STRAIGHT) {
    return findStraightMove(hand, center);
  }

  // 7. Đang đè Tứ Quý (FOUROFAKIND)
  if (roundType === Combinations.FOUROFAKIND) {
    const grouped = _.groupBy(hand, "rank");
    for (const rank in grouped) {
      if (grouped[rank].length === 4) {
        const quad = grouped[rank];
        if (compareHighest(quad, center) === 1) {
          return quad;
        }
      }
    }
    return null;
  }

  return null;
}

// Active bot clients registry: Key là `${matchID}_${playerID}`
const activeBots = new Map();

const BOT_NAMES = [
  "Bot Thần Bài 🤖",
  "Bot Vui Vẻ 😎",
  "Bot Gáy Sớm 🔥",
  "Bot Cay Cú 😭",
];

// Khởi tạo 1 bot client cho 1 ghế cụ thể
export function spawnBotClient(matchID, botPlayerID, credentials = null, port = 8000) {
  const botKey = `${matchID}_${botPlayerID}`;
  if (activeBots.has(botKey)) {
    return activeBots.get(botKey);
  }

  console.log(`[Bot AI] Khởi động Bot cho phòng ${matchID}, ghế ${botPlayerID}`);

  const clientConfig = {
    game: TienLen,
    gameID: matchID,
    playerID: botPlayerID.toString(),
    multiplayer: SocketIO({ server: `http://localhost:${port}` }),
  };
  if (credentials) {
    clientConfig.credentials = credentials;
  }

  const botClient = Client(clientConfig);
  botClient.start();
  activeBots.set(botKey, botClient);

  let isThinking = false;

  botClient.subscribe((state) => {
    if (!state) return;

    if (state.ctx.gameover) {
      console.log(`[Bot AI] Ván ${matchID} kết thúc. Bot ghế ${botPlayerID} ngừng hoạt động.`);
      botClient.stop();
      activeBots.delete(botKey);
      return;
    }

    if (state.ctx.currentPlayer === botPlayerID.toString() && !isThinking) {
      isThinking = true;

      // Giả lập suy nghĩ bài từ 1.0s đến 1.4s
      const delay = 1000 + Math.floor(Math.random() * 400);
      setTimeout(() => {
        try {
          if (
            !activeBots.has(botKey) ||
            !botClient.transport ||
            !botClient.transport.isConnected
          ) {
            isThinking = false;
            return;
          }

          const latestState = botClient.getState();
          if (
            !latestState ||
            latestState.ctx.currentPlayer !== botPlayerID.toString() ||
            latestState.ctx.gameover
          ) {
            isThinking = false;
            return;
          }

          const myHand =
            (latestState.G.players[botPlayerID] &&
              latestState.G.players[botPlayerID].hand) ||
            [];
          const hasThreeSpades = myHand.some(
            (c) => c.rank === "3" && c.suit === "S"
          );

          const cardsToPlay = findBotMove(
            myHand,
            latestState.G.roundType,
            latestState.G.center,
            hasThreeSpades
          );

          const isTienLenStage =
            latestState.ctx.activePlayers &&
            latestState.ctx.activePlayers[botPlayerID] === "tienLen";

          if (cardsToPlay && cardsToPlay.length > 0) {
            const cardIds = cardsToPlay.map((c) => c.rank + c.suit);
            console.log(
              `[Bot AI] Ghế ${botPlayerID} đánh bài: ${cardIds.join(" ")}`
            );

            // Kiểm tra xem có chặt heo không để gáy
            const isChop =
              latestState.G.center &&
              latestState.G.center.length > 0 &&
              validChop(latestState.G.center, cardsToPlay);

            if (isTienLenStage) {
              botClient.moves.tienLenPlayDirect(cardIds);
            } else {
              botClient.moves.playCardsDirect(cardIds);
            }

            if (isChop && botClient.moves.sendEmote) {
              setTimeout(() => {
                botClient.moves.sendEmote("🔥 Chặt nè con!");
              }, 400);
            } else if (Math.random() < 0.12 && botClient.moves.sendEmote) {
              setTimeout(() => {
                botClient.moves.sendEmote("😎 Đè tao đè lại!");
              }, 400);
            }
          } else {
            // Chỉ bỏ lượt khi không phải là người mở vòng
            if (
              latestState.G.roundType !== Combinations.ANY &&
              latestState.G.center &&
              latestState.G.center.length > 0
            ) {
              console.log(`[Bot AI] Ghế ${botPlayerID} bỏ lượt (Pass)`);
              botClient.moves.passTurn();

              if (Math.random() < 0.18 && botClient.moves.sendEmote) {
                setTimeout(() => {
                  botClient.moves.sendEmote("😭 Cay thế nhờ...");
                }, 300);
              }
            } else {
              // Trường hợp mở vòng nhưng findBotMove trả về null (rất hiếm): Đánh lá nhỏ nhất
              if (myHand.length > 0) {
                const cardIds = [myHand[0].rank + myHand[0].suit];
                botClient.moves.playCardsDirect(cardIds);
              }
            }
          }
        } catch (err) {
          console.error(`[Bot AI Error]:`, err);
        } finally {
          setTimeout(() => {
            isThinking = false;
          }, 300);
        }
      }, delay);
    }
  });

  return botClient;
}

// Thêm 1 bot vào ghế trống đầu tiên hoặc kết nối bot có sẵn
export async function spawnBotForMatch(matchID, port = 8000) {
  const roomsData = await httpRequest({
    hostname: "localhost",
    port: port,
    path: "/games/tien-len",
    method: "GET",
  });

  const room = roomsData.rooms.find((r) => r.gameID === matchID);
  if (!room) {
    throw new Error(`Không tìm thấy phòng với ID: ${matchID}`);
  }

  // Tìm ghế đã là bot mà chưa chạy
  const inactiveBotSlot = room.players.find(
    (p) => p.name && p.name.includes("Bot") && !activeBots.has(`${matchID}_${p.id}`)
  );
  if (inactiveBotSlot) {
    spawnBotClient(matchID, inactiveBotSlot.id.toString(), null, port);
    return { success: true, botPlayerID: inactiveBotSlot.id, matchID };
  }

  // Tìm ghế trống
  const emptySlot = room.players.find((p) => p.name === undefined || p.name === null);
  if (!emptySlot) {
    throw new Error(`Phòng ${matchID} đã đầy`);
  }

  const botPlayerID = emptySlot.id.toString();
  const botName = BOT_NAMES[emptySlot.id % BOT_NAMES.length];

  const joinRes = await httpRequest(
    {
      hostname: "localhost",
      port: port,
      path: `/games/tien-len/${matchID}/join`,
      method: "POST",
      headers: { "Content-Type": "application/json" },
    },
    { playerID: botPlayerID, playerName: botName }
  );

  spawnBotClient(matchID, botPlayerID, joinRes.playerCredentials, port);
  return { success: true, botPlayerID, matchID };
}

// Lấp đầy TẤT CẢ các ghế còn trống bằng Bot AI
export async function fillBotsForMatch(matchID, port = 8000, options = {}) {
  const leaveHumanSeat =
    options.leaveHumanSeat !== undefined ? options.leaveHumanSeat : true;

  const roomsData = await httpRequest({
    hostname: "localhost",
    port: port,
    path: "/games/tien-len",
    method: "GET",
  });

  const room = roomsData.rooms.find((r) => r.gameID === matchID);
  if (!room) {
    throw new Error(`Không tìm thấy phòng với ID: ${matchID}`);
  }

  let emptySlots = room.players.filter(
    (p) => p.name === undefined || p.name === null
  );

  // Kiểm tra xem phòng đã có người chơi thật (không phải Bot) hay chưa
  const hasHumanPlayer = room.players.some(
    (p) => p.name && !p.name.includes("Bot")
  );

  // Nếu phòng chưa có người chơi thật, LUÔN LUÔN để lại ghế 0 cho người chơi!
  if (!hasHumanPlayer && leaveHumanSeat) {
    emptySlots = emptySlots.filter((p) => p.id !== 0);
  }

  let filledCount = 0;
  for (const slot of emptySlots) {
    const botPlayerID = slot.id.toString();
    const botName = BOT_NAMES[slot.id % BOT_NAMES.length];

    try {
      const joinRes = await httpRequest(
        {
          hostname: "localhost",
          port: port,
          path: `/games/tien-len/${matchID}/join`,
          method: "POST",
          headers: { "Content-Type": "application/json" },
        },
        { playerID: botPlayerID, playerName: botName }
      );

      spawnBotClient(matchID, botPlayerID, joinRes.playerCredentials, port);
      filledCount++;
    } catch (err) {
      console.error(`Lỗi mời bot vào ghế ${botPlayerID}:`, err.message);
    }
  }

  return { success: true, filledCount, matchID };
}

// Dừng 1 bot cụ thể
export function stopBotClient(matchID, playerID) {
  const botKey = `${matchID}_${playerID}`;
  if (activeBots.has(botKey)) {
    const client = activeBots.get(botKey);
    activeBots.delete(botKey);
    try {
      client.stop();
    } catch (e) {}
  }
}

// Dừng tất cả bot trong một phòng (khi xóa phòng hoặc kết thúc)
export function stopBotsForMatch(matchID) {
  for (const [key, client] of activeBots.entries()) {
    if (key.startsWith(`${matchID}_`)) {
      activeBots.delete(key);
      try {
        client.stop();
      } catch (e) {}
    }
  }
}

let serverDb = null;
export function setServerDb(db) {
  serverDb = db;
}

// Watchdog: Tự động kiểm tra và kết nối Bot cho mọi phòng có người chơi mang tên "Bot"
export function startBotWatchdog(port = 8000) {
  setInterval(async () => {
    try {
      const data = await httpRequest({
        hostname: "localhost",
        port: port,
        path: "/games/tien-len",
        method: "GET",
      });

      if (!data || !data.rooms) return;

      for (const room of data.rooms) {
        for (const player of room.players) {
          if (
            player.name &&
            player.name.includes("Bot") &&
            !activeBots.has(`${room.gameID}_${player.id}`)
          ) {
            let credentials = null;
            if (serverDb) {
              try {
                const meta = await serverDb.fetch(room.gameID, { metadata: true });
                if (
                  meta &&
                  meta.metadata &&
                  meta.metadata.players &&
                  meta.metadata.players[player.id]
                ) {
                  credentials = meta.metadata.players[player.id].credentials;
                }
              } catch (e) {}
            }
            spawnBotClient(room.gameID, player.id.toString(), credentials, port);
          }
        }
      }
    } catch (e) {
      // Ignore network tick error
    }
  }, 2000);
}
