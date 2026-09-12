// server.js

import "dotenv/config";
import https from "https";
import { Server } from "boardgame.io/server";
import { InitializeGame } from "boardgame.io/core";
import serve from "koa-static";
import path from "path";
import { default as TienLen } from "./src/TienLen";
import {
  spawnBotForMatch,
  fillBotsForMatch,
  stopBotsForMatch,
  stopBotClient,
  startBotWatchdog,
  setServerDb,
} from "./bot";

const PORT = process.env.PORT || 8000;
const BOOT_ID = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
const server = Server({ games: [TienLen] });

// Trace unexpected room deletion. This is intentionally noisy only when wipe()
// is actually called, so we can distinguish a DB wipe from a process restart.
if (server.db && typeof server.db.wipe === "function") {
  const originalWipe = server.db.wipe.bind(server.db);
  server.db.wipe = async matchID => {
    console.warn(`[DB WIPE][boot=${BOOT_ID}] match=${matchID}`);
    console.warn(new Error("wipe trace").stack);
    return originalWipe(matchID);
  };
}

process.on("uncaughtException", err => {
  console.error(`[UNCAUGHT][boot=${BOOT_ID}]`, err);
});

process.on("unhandledRejection", reason => {
  console.error(`[UNHANDLED_REJECTION][boot=${BOOT_ID}]`, reason);
});

function getJson(url) {
  return new Promise((resolve, reject) => {
    https
      .get(url, response => {
        let body = "";
        response.setEncoding("utf8");
        response.on("data", chunk => {
          body += chunk;
        });
        response.on("end", () => {
          try {
            const parsed = JSON.parse(body);
            if (response.statusCode >= 400) {
              const message =
                parsed && parsed.error && parsed.error.message
                  ? parsed.error.message
                  : `YouTube API HTTP ${response.statusCode}`;
              reject(new Error(message));
              return;
            }
            resolve(parsed);
          } catch (err) {
            reject(err);
          }
        });
      })
      .on("error", reject);
  });
}

async function searchYouTube(query, options = {}) {
  const apiKey = process.env.YOUTUBE_API_KEY;
  if (!apiKey) {
    throw new Error(
      "Chưa cấu hình YOUTUBE_API_KEY. Hãy thêm key vào file .env hoặc biến môi trường của server."
    );
  }

  const maxResults = Math.max(1, Math.min(25, Number(options.maxResults) || 8));
  const shortOnly = Boolean(options.shortOnly);
  const params = new URLSearchParams({
    part: "snippet",
    type: "video",
    maxResults: String(maxResults),
    q: shortOnly ? `${query} #shorts` : query,
    videoEmbeddable: "true",
    videoSyndicated: "true",
    safeSearch: "moderate",
    key: apiKey,
  });
  if (shortOnly) params.set("videoDuration", "short");

  const data = await getJson(
    `https://www.googleapis.com/youtube/v3/search?${params.toString()}`
  );

  return (data.items || [])
    .filter(item => item.id && item.id.videoId)
    .map(item => ({
      videoId: item.id.videoId,
      title: item.snippet ? item.snippet.title : "YouTube video",
      channelTitle: item.snippet ? item.snippet.channelTitle : "",
      thumbnail:
        item.snippet && item.snippet.thumbnails
          ? (item.snippet.thumbnails.medium || item.snippet.thumbnails.default || {})
              .url || ""
          : "",
    }));
}

function playerCanControlRoom(metadata, playerID, credentials) {
  if (!metadata || !metadata.players || playerID === undefined || playerID === null) {
    return false;
  }
  const player = metadata.players[playerID];
  if (!player) return false;
  if (!player.credentials) return true;
  return Boolean(credentials && credentials === player.credentials);
}

server.app.use(async (ctx, next) => {
  if (ctx.method === "GET" && ctx.path === "/api/server-info") {
    ctx.body = {
      success: true,
      port: PORT,
      hostIp: process.env.HOST_IP || null,
      bootId: BOOT_ID,
      storage: process.env.FLATFILE_DIR ? "flatfile" : "memory",
    };
    return;
  }

  if (ctx.method === "GET" && ctx.path === "/api/youtube/search") {
    const query = String(ctx.query.q || "").trim();
    if (!query) {
      ctx.status = 400;
      ctx.body = { success: false, error: "Thiếu từ khóa tìm kiếm." };
      return;
    }
    if (query.length > 100) {
      ctx.status = 400;
      ctx.body = { success: false, error: "Từ khóa tìm kiếm quá dài." };
      return;
    }
    try {
      const items = await searchYouTube(query, { maxResults: 8 });
      ctx.body = { success: true, items };
    } catch (err) {
      console.error("YouTube search error:", err.message);
      ctx.status = 500;
      ctx.body = { success: false, error: err.message };
    }
    return;
  }

  if (ctx.method === "GET" && ctx.path === "/api/youtube/shorts") {
    const query = String(ctx.query.q || "").trim();
    if (!query) {
      ctx.status = 400;
      ctx.body = { success: false, error: "Thiếu chủ đề Shorts." };
      return;
    }
    if (query.length > 100) {
      ctx.status = 400;
      ctx.body = { success: false, error: "Chủ đề tìm kiếm quá dài." };
      return;
    }
    try {
      const items = await searchYouTube(query, {
        maxResults: 20,
        shortOnly: true,
      });
      ctx.body = { success: true, items };
    } catch (err) {
      console.error("YouTube Shorts search error:", err.message);
      ctx.status = 500;
      ctx.body = { success: false, error: err.message };
    }
    return;
  }

  if (
    ctx.method === "POST" &&
    ctx.path.startsWith("/api/rooms/") &&
    ctx.path.endsWith("/rematch")
  ) {
    const matchID = ctx.path
      .replace("/api/rooms/", "")
      .replace(/\/rematch$/, "");
    const playerID = String(ctx.get("x-player-id") || "");
    const credentials = String(ctx.get("x-player-credentials") || "");

    try {
      const result = await server.db.fetch(matchID, {
        state: true,
        metadata: true,
      });
      const oldState = result && result.state;
      const metadata = result && result.metadata;

      if (!oldState || !metadata) {
        ctx.status = 404;
        ctx.body = { success: false, error: "Bàn này không còn tồn tại." };
        return;
      }
      if (!playerCanControlRoom(metadata, playerID, credentials)) {
        ctx.status = 403;
        ctx.body = { success: false, error: "Không có quyền đánh lại bàn này." };
        return;
      }
      if (!oldState.ctx || oldState.ctx.gameover === undefined) {
        ctx.status = 409;
        ctx.body = { success: false, error: "Ván hiện tại chưa kết thúc." };
        return;
      }

      const numPlayers =
        (metadata.players && Object.keys(metadata.players).length) ||
        (oldState.ctx && oldState.ctx.numPlayers) ||
        2;
      const nextState = InitializeGame({ game: TienLen, numPlayers });

      // Media belongs to the table, not to a single round. Keep the current
      // music queue/playback while dealing a completely new deck.
      if (oldState.G && oldState.G.musicRoom) {
        nextState.G.musicRoom = oldState.G.musicRoom;
      }

      const nextMetadata = { ...metadata };
      delete nextMetadata.gameover;
      nextMetadata.updatedAt = Date.now();

      // Keep the same match ID + seats + credentials. Only the round state is reset.
      await server.db.createGame(matchID, {
        initialState: nextState,
        metadata: nextMetadata,
      });

      // Finished bot clients stop themselves at game-over. Clearing the local
      // registry here guarantees the watchdog reconnects every bot to the same seats.
      stopBotsForMatch(matchID);

      console.log(
        `[REMATCH][boot=${BOOT_ID}] match=${matchID} player=${playerID} players=${numPlayers}`
      );
      ctx.body = { success: true, matchID, numPlayers };
    } catch (err) {
      console.error("Rematch error:", err);
      ctx.status = 500;
      ctx.body = { success: false, error: err.message || "Không thể đánh lại." };
    }
    return;
  }

  if (ctx.method === "POST" && ctx.path.startsWith("/api/bot/join/")) {
    const matchID = ctx.path.replace("/api/bot/join/", "");
    try {
      const result = await spawnBotForMatch(matchID, PORT);
      ctx.body = { success: true, result };
    } catch (err) {
      console.error("Error spawning bot:", err.message);
      ctx.status = 400;
      ctx.body = { success: false, error: err.message };
    }
    return;
  }

  if (ctx.method === "POST" && ctx.path.startsWith("/api/bot/fill/")) {
    const matchID = ctx.path.replace("/api/bot/fill/", "");
    const leaveHumanSeat = ctx.query.leaveHuman !== "false";
    try {
      const result = await fillBotsForMatch(matchID, PORT, { leaveHumanSeat });
      ctx.body = { success: true, result };
    } catch (err) {
      console.error("Error filling bots:", err.message);
      ctx.status = 400;
      ctx.body = { success: false, error: err.message };
    }
    return;
  }

  if (ctx.method === "DELETE" && ctx.path.startsWith("/api/rooms/")) {
    const matchID = ctx.path.replace("/api/rooms/", "");
    try {
      stopBotsForMatch(matchID);
      if (server.db && server.db.wipe) {
        await server.db.wipe(matchID);
      }
      ctx.body = { success: true, matchID };
    } catch (err) {
      console.error("Error wiping room:", err.message);
      ctx.status = 500;
      ctx.body = { success: false, error: err.message };
    }
    return;
  }

  if (
    ctx.method === "POST" &&
    ctx.path.startsWith("/api/rooms/") &&
    ctx.path.includes("/free-seat/")
  ) {
    const parts = ctx.path.replace("/api/rooms/", "").split("/free-seat/");
    const matchID = parts[0];
    const playerID = parts[1];
    try {
      if (server.db) {
        const metaResult = await server.db.fetch(matchID, { metadata: true });
        if (metaResult && metaResult.metadata && metaResult.metadata.players) {
          const players = metaResult.metadata.players;
          if (players[playerID]) {
            delete players[playerID].name;
            delete players[playerID].credentials;
            await server.db.setMetadata(matchID, metaResult.metadata);
          }
        }
      }
      stopBotClient(matchID, playerID);
      ctx.body = { success: true, matchID, playerID };
    } catch (err) {
      console.error("Error freeing seat:", err.message);
      ctx.status = 500;
      ctx.body = { success: false, error: err.message };
    }
    return;
  }

  await next();
});

const frontEndAppBuildPath = path.resolve(__dirname, "./build");
server.app.use(serve(frontEndAppBuildPath));

server.run(PORT, () => {
  server.app.use(
    async (ctx, next) =>
      await serve(frontEndAppBuildPath)(
        Object.assign(ctx, { path: "index.html" }),
        next
      )
  );
  console.log(
    `Server Tiến Lên đang chạy tại port ${PORT} | boot=${BOOT_ID} | storage=${
      process.env.FLATFILE_DIR ? `flatfile:${process.env.FLATFILE_DIR}` : "memory"
    }`
  );
  setServerDb(server.db);
  startBotWatchdog(PORT);
});
