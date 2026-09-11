// server.js

import "dotenv/config";
import https from "https";
import { Server } from "boardgame.io/server";
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
const server = Server({ games: [TienLen] });

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

async function searchYouTube(query) {
  const apiKey = process.env.YOUTUBE_API_KEY;
  if (!apiKey) {
    throw new Error(
      "Chưa cấu hình YOUTUBE_API_KEY. Hãy thêm key vào file .env hoặc biến môi trường của server."
    );
  }

  const params = new URLSearchParams({
    part: "snippet",
    type: "video",
    maxResults: "8",
    q: query,
    videoEmbeddable: "true",
    videoSyndicated: "true",
    safeSearch: "moderate",
    key: apiKey,
  });
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

server.app.use(async (ctx, next) => {
  if (ctx.method === "GET" && ctx.path === "/api/server-info") {
    ctx.body = {
      success: true,
      port: PORT,
      hostIp: process.env.HOST_IP || null,
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
      const items = await searchYouTube(query);
      ctx.body = { success: true, items };
    } catch (err) {
      console.error("YouTube search error:", err.message);
      ctx.status = 500;
      ctx.body = { success: false, error: err.message };
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
  console.log(`Server Tiến Lên đang chạy tại port ${PORT}`);
  setServerDb(server.db);
  startBotWatchdog(PORT);
});
