import { useEffect } from "react";

export default function LobbyResumeGuard() {
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    const matchID = params.get("resume");
    if (!matchID) return undefined;

    let attempts = 0;
    const timer = setInterval(() => {
      attempts += 1;
      const rows = document.querySelectorAll("#instances tr");
      for (const row of rows) {
        if (!row.textContent.includes(matchID) && !row.innerHTML.includes(matchID)) {
          continue;
        }
        const buttons = Array.from(row.querySelectorAll("button"));
        const play = buttons.find(
          button => button.textContent.trim().toLowerCase() === "play"
        );
        if (play) {
          clearInterval(timer);
          window.history.replaceState({}, document.title, "/");
          play.click();
          return;
        }
      }

      if (attempts >= 25) {
        clearInterval(timer);
        window.history.replaceState({}, document.title, "/");
        console.warn(`[Lobby] Không thể tự mở lại bàn ${matchID}.`);
      }
    }, 250);

    return () => clearInterval(timer);
  }, []);

  return null;
}
