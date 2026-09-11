// scripts/patch-boardgame.js
// Patch boardgame.io LobbyConnection to prevent duplicate room entries in race conditions
const fs = require("fs");
const path = require("path");

const files = [
  "node_modules/boardgame.io/dist/esm/react.js",
  "node_modules/boardgame.io/dist/cjs/react.js",
  "node_modules/boardgame.io/src/lobby/connection.js",
];

let count = 0;
for (const relPath of files) {
  const fullPath = path.resolve(__dirname, "..", relPath);
  if (fs.existsSync(fullPath)) {
    let content = fs.readFileSync(fullPath, "utf8");
    const target = "this.rooms = this.rooms.concat(gameJson.rooms);";
    const replacement =
      "var _c = this.rooms.concat(gameJson.rooms); var _s = {}; this.rooms = _c.filter(function(r){ return r && r.gameID && !_s[r.gameID] && (_s[r.gameID] = true); });";
    if (content.includes(target)) {
      content = content.replace(target, replacement);
      fs.writeFileSync(fullPath, content, "utf8");
      console.log("Successfully patched:", relPath);
      count++;
    }
  }
}
console.log(`[Patch] Applied to ${count} files.`);
