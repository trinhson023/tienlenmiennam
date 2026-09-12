import React, { useEffect, useState } from "react";
import PropTypes from "prop-types";

const ROUND_LABELS = {
  any: "Tự do",
  single: "Bài lẻ",
  pair: "Đôi",
  "three-of-a-kind": "Bộ ba",
  straight: "Sảnh",
  "three-pair": "3 đôi thông",
  "four-of-a-kind": "Tứ quý",
  "four-pair": "4 đôi thông",
};

function getRoundLabel(roundType) {
  if (!roundType) return "Đang chờ";
  return ROUND_LABELS[roundType] || roundType;
}

function secondsLeft(deadline) {
  if (!deadline) return 60;
  return Math.max(0, Math.ceil((Number(deadline) - Date.now()) / 1000));
}

export default function GameHUD({ G, ctx, playerID, gameMetadata }) {
  const [remaining, setRemaining] = useState(() => secondsLeft(G && G.turnDeadline));

  useEffect(() => {
    setRemaining(secondsLeft(G && G.turnDeadline));
    const timer = setInterval(() => {
      setRemaining(secondsLeft(G && G.turnDeadline));
    }, 250);
    return () => clearInterval(timer);
  }, [G && G.turnDeadline, ctx && ctx.currentPlayer, ctx && ctx.turn]);

  const currentPlayerID =
    ctx && ctx.currentPlayer !== undefined && ctx.currentPlayer !== null
      ? String(ctx.currentPlayer)
      : "";
  const isMyTurn =
    playerID !== undefined &&
    playerID !== null &&
    String(playerID) === currentPlayerID;
  const metadata = gameMetadata || [];
  const currentPlayer = metadata.find(
    player => String(player.id) === currentPlayerID
  );
  const currentName = currentPlayer
    ? currentPlayer.name
    : currentPlayerID
    ? `Người chơi ${parseInt(currentPlayerID, 10) + 1}`
    : "—";
  const cardsOnTable = G && G.center ? G.center.length : 0;
  const urgent = remaining <= 10;
  const critical = remaining <= 5;

  return (
    <div className="premium-hud" aria-label="Thông tin ván đấu">
      <div className="premium-hud__brand">
        <div className="premium-hud__crest" aria-hidden="true">
          ♠
        </div>
        <div className="premium-hud__brand-copy">
          <span className="premium-hud__eyebrow">TIẾN LÊN MIỀN NAM</span>
          <strong className="premium-hud__title">Royal Table</strong>
        </div>
      </div>

      <div className="premium-hud__stats">
        <div className={`premium-hud__turn ${isMyTurn ? "is-mine" : ""}`}>
          <span className="premium-hud__pulse" aria-hidden="true" />
          <div>
            <span className="premium-hud__label">
              {isMyTurn ? "LƯỢT CỦA BẠN" : "ĐANG TỚI LƯỢT"}
            </span>
            <strong>{isMyTurn ? "Ra bài thôi!" : currentName}</strong>
          </div>
        </div>

        <div
          className={`premium-hud__metric premium-hud__timer ${
            urgent ? "is-urgent" : ""
          } ${critical ? "is-critical" : ""}`}
          title="Mỗi lượt có tối đa 60 giây"
        >
          <span className="premium-hud__label">THỜI GIAN</span>
          <strong>{remaining}s</strong>
        </div>

        <div className="premium-hud__metric">
          <span className="premium-hud__label">KIỂU VÒNG</span>
          <strong>{getRoundLabel(G && G.roundType)}</strong>
        </div>

        <div className="premium-hud__metric">
          <span className="premium-hud__label">TRÊN BÀN</span>
          <strong>{cardsOnTable} lá</strong>
        </div>
      </div>
    </div>
  );
}

GameHUD.propTypes = {
  G: PropTypes.object,
  ctx: PropTypes.object,
  playerID: PropTypes.string,
  gameMetadata: PropTypes.array,
};
