// server.js

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

// API endpoint để mời 1 Bot AI vào bàn
server.app.use(async (ctx, next) => {
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

  // API endpoint để lấp đầy ghế trống bằng Bot AI (mặc định chừa 1 ghế cho người chơi)
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

  // API endpoint để xóa bàn (dọn dẹp các phòng trống / xong ván)
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

  // API endpoint để giải phóng 1 ghế bị kẹt (khi đổi tên hoặc người chơi thoát)
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

// Build path relative to the server.js file
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
