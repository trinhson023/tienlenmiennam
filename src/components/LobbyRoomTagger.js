import { useEffect } from "react";

function roomSignature(room) {
  return (room.players || [])
    .map(player => (player && player.name ? player.name : "[free]"))
    .join("|");
}

export default function LobbyRoomTagger() {
  useEffect(() => {
    let cancelled = false;

    const tagRows = async () => {
      try {
        const response = await fetch("/games/tien-len");
        if (!response.ok || cancelled) return;
        const data = await response.json();
        if (cancelled) return;

        const rooms = data.rooms || [];
        const rows = Array.from(document.querySelectorAll("#instances tr"));
        if (!rows.length) return;

        const usedRows = new Set();
        rooms.forEach(room => {
          const signature = roomSignature(room);
          const seatNames = signature.split("|");
          const row = rows.find(candidate => {
            if (usedRows.has(candidate)) return false;
            const text = candidate.textContent || "";
            return seatNames.every(name =>
              name === "[free]"
                ? text.includes("[free]")
                : text.includes(name)
            );
          });

          if (row) {
            row.dataset.gameId = room.gameID;
            usedRows.add(row);
          }
        });
      } catch (e) {
        // DOM tagging is a convenience layer only; never break the lobby.
      }
    };

    tagRows();
    const timer = window.setInterval(tagRows, 900);
    return () => {
      cancelled = true;
      window.clearInterval(timer);
    };
  }, []);

  return null;
}
