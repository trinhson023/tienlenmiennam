import { useEffect } from "react";

function readLobbyCookie() {
  try {
    const item = document.cookie
      .split(";")
      .map(part => part.trim())
      .find(part => part.indexOf("lobbyState=") === 0);
    if (!item) return null;

    let raw = item.substring("lobbyState=".length);
    raw = decodeURIComponent(raw);
    if (raw.indexOf("j:") === 0) raw = raw.substring(2);
    return JSON.parse(raw);
  } catch (e) {
    return null;
  }
}

async function loadTargetRoom(matchID) {
  try {
    const response = await fetch("/games/tien-len");
    if (!response.ok) return null;
    const data = await response.json();
    return (data.rooms || []).find(room => room.gameID === matchID) || null;
  } catch (e) {
    return null;
  }
}

function findMatchingLobbyRow(matchID, targetRoom, playerName) {
  const exact = document.querySelector(
    `#instances tr[data-game-id="${matchID}"]`
  );
  if (exact) return exact;
  if (!targetRoom) return null;

  const occupiedNames = (targetRoom.players || [])
    .map(player => player && player.name)
    .filter(Boolean);
  const rows = Array.from(document.querySelectorAll("#instances tr"));

  const candidates = rows.filter(row => {
    const text = row.textContent || "";
    return occupiedNames.every(name => text.includes(name));
  });

  if (candidates.length === 1) return candidates[0];
  if (playerName) {
    const ownRow = candidates.find(row =>
      (row.textContent || "").includes(playerName)
    );
    if (ownRow) return ownRow;
  }
  return candidates[0] || null;
}

export default function LobbyResumeGuard() {
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const matchID = params.get("resume");
    if (!matchID) return undefined;

    const lobbyState = readLobbyCookie();
    const playerName =
      lobbyState && lobbyState.playerName ? String(lobbyState.playerName) : "";

    let cancelled = false;
    let attempts = 0;
    let targetRoom = null;
    let retryTimer = null;

    const tryResume = async () => {
      if (cancelled) return;
      attempts += 1;

      if (!targetRoom || attempts % 4 === 1) {
        targetRoom = await loadTargetRoom(matchID);
      }

      const row = findMatchingLobbyRow(matchID, targetRoom, playerName);
      if (row) {
        const buttons = Array.from(row.querySelectorAll("button"));
        const play = buttons.find(
          button => button.textContent.trim().toLowerCase() === "play"
        );
        if (play) {
          window.history.replaceState({}, document.title, "/");
          play.click();
          return;
        }
      }

      if (attempts >= 32) {
        window.history.replaceState({}, document.title, "/");
        console.warn(`[Lobby] Không thể tự mở lại bàn ${matchID}.`);
        return;
      }

      retryTimer = window.setTimeout(tryResume, 250);
    };

    const startTimer = window.setTimeout(tryResume, 150);
    return () => {
      cancelled = true;
      window.clearTimeout(startTimer);
      if (retryTimer) window.clearTimeout(retryTimer);
    };
  }, []);

  return null;
}
