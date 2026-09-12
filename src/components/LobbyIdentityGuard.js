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

function isNameEntryPhase(phase) {
  if (!phase) return false;
  const title = phase.querySelector(".phase-title");
  return Boolean(
    title && title.textContent.trim().toLowerCase() === "choose a player name:"
  );
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

  // Never force-release a seat without the credential owned by this browser.
  // The manual "Hủy" control in the room manager remains the recovery path for
  // genuinely orphaned seats. This avoids deleting an active seat just because
  // another text field (for example YouTube search) fired Enter.
  if (!credentials) return;

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
        // Try another room containing the same old display name.
      }
    }
  } catch (e) {
    // Name entry should never be blocked because cleanup failed.
  }
}

export default function LobbyIdentityGuard() {
  const lastAttemptRef = useRef({ key: "", at: 0 });

  useEffect(() => {
    const triggerCleanup = (phase, input) => {
      if (!isNameEntryPhase(phase) || !input) return;

      const nextName = String(input.value || "").trim();
      if (!nextName) return;

      const lobbyState = readLobbyCookie();
      const oldName =
        lobbyState && lobbyState.playerName
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
      const button =
        event.target && event.target.closest
          ? event.target.closest("button")
          : null;
      if (!button || button.textContent.trim() !== "Enter") return;

      const phase = button.closest("#lobby-view .phase");
      if (!isNameEntryPhase(phase)) return;
      const input = phase.querySelector('input[type="text"]');
      triggerCleanup(phase, input);
    };

    const onKeyDownCapture = event => {
      if (event.key !== "Enter") return;
      const input = event.target;
      if (!input || input.tagName !== "INPUT" || input.type !== "text") return;

      const phase = input.closest("#lobby-view .phase");
      if (!isNameEntryPhase(phase)) return;
      triggerCleanup(phase, input);
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
