import { useEffect, useRef } from "react";

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

async function releasePreviousSeat(nextName) {
  const lobbyState = readLobbyCookie();
  if (!lobbyState) return;

  const oldName = String(lobbyState.playerName || "").trim();
  const cleanNextName = String(nextName || "").trim();

  if (
    !oldName ||
    !cleanNextName ||
    oldName === cleanNextName ||
    oldName === "Visitor"
  ) {
    return;
  }

  const credentialStore = lobbyState.credentialStore || {};
  const credentials = credentialStore[oldName];

  try {
    const roomsResponse = await fetch("/games/tien-len");
    if (!roomsResponse.ok) return;
    const roomsData = await roomsResponse.json();
    const rooms = roomsData.rooms || [];

    const candidates = [];
    rooms.forEach(room => {
      (room.players || []).forEach(player => {
        if (player && player.name === oldName) {
          candidates.push({ room, player });
        }
      });
    });

    if (candidates.length === 0) return;

    // boardgame.io 0.39.16 stores credentials per player name in lobbyState.
    // Use the official /leave endpoint first so we only release the seat that
    // belongs to this browser/session.
    if (credentials) {
      for (const candidate of candidates) {
        try {
          const leaveResponse = await fetch(
            `/games/tien-len/${candidate.room.gameID}/leave`,
            {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify({
                playerID: candidate.player.id,
                credentials,
              }),
            }
          );

          if (leaveResponse.ok) {
            console.info(
              `[Lobby] Released old seat ${oldName} before changing name to ${cleanNextName}.`
            );
            return;
          }
        } catch (e) {
          // Try the next matching room before using the fallback below.
        }
      }
    }

    // Fallback for an already-orphaned seat whose credential cookie is missing.
    // Only do this when the old name appears exactly once, avoiding accidental
    // removal when two different people happen to use the same display name.
    if (candidates.length === 1) {
      const candidate = candidates[0];
      await fetch(
        `/api/rooms/${candidate.room.gameID}/free-seat/${candidate.player.id}`,
        { method: "POST" }
      );
    }
  } catch (e) {
    // Name entry should never be blocked just because stale-seat cleanup failed.
  }
}

export default function LobbyIdentityGuard() {
  const lastAttemptRef = useRef({ key: "", at: 0 });

  useEffect(() => {
    const triggerCleanup = input => {
      if (!input) return;
      const nextName = String(input.value || "").trim();
      if (!nextName) return;

      const lobbyState = readLobbyCookie();
      const oldName = lobbyState && lobbyState.playerName
        ? String(lobbyState.playerName).trim()
        : "";
      const key = `${oldName}->${nextName}`;
      const now = Date.now();

      if (
        lastAttemptRef.current.key === key &&
        now - lastAttemptRef.current.at < 1200
      ) {
        return;
      }

      lastAttemptRef.current = { key, at: now };
      releasePreviousSeat(nextName);
    };

    const onClickCapture = event => {
      const button = event.target && event.target.closest
        ? event.target.closest("button")
        : null;
      if (!button || button.textContent.trim() !== "Enter") return;

      const lobby = button.closest("#lobby-view");
      if (!lobby) return;
      const input = lobby.querySelector('.phase input[type="text"]');
      triggerCleanup(input);
    };

    const onKeyDownCapture = event => {
      if (event.key !== "Enter") return;
      const input = event.target;
      if (!input || input.tagName !== "INPUT" || input.type !== "text") return;
      if (!input.closest("#lobby-view .phase")) return;
      triggerCleanup(input);
    };

    document.addEventListener("click", onClickCapture, true);
    document.addEventListener("keydown", onKeyDownCapture, true);

    return () => {
      document.removeEventListener("click", onClickCapture, true);
      document.removeEventListener("keydown", onKeyDownCapture, true);
    };
  }, []);

  return null;
}
