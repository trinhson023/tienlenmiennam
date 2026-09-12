// src/components/QuickChat.js
import React, { useState } from "react";
import PropTypes from "prop-types";

const QUICK_EMOTES = [
  "🔥 Chặt nè con!",
  "😭 Cay thế nhờ...",
  "😎 Đè tao đè lại!",
  "⚡ Đánh nhanh hộ cái!",
  "🍀 Bài son quá bạn ơi!",
  "🙏 Xin tha con Heo!",
];

const THROWABLES = [
  { type: "bomb", emoji: "💣", label: "Ném bom" },
  { type: "tomato", emoji: "🍅", label: "Ném cà chua" },
  { type: "poop", emoji: "💩", label: "Ném cứt" },
];

export default function QuickChat({
  onSendEmote,
  onThrowReaction,
  playerID,
  gameMetadata,
}) {
  const [isOpen, setIsOpen] = useState(false);
  const [customText, setCustomText] = useState("");
  const [throwType, setThrowType] = useState(null);

  const opponents = (gameMetadata || []).filter(
    player => String(player.id) !== String(playerID) && player.name
  );

  const handleSelect = text => {
    if (onSendEmote) onSendEmote(text);
    setIsOpen(false);
  };

  const sendCustom = () => {
    const clean = customText.replace(/\s+/g, " ").trim().slice(0, 80);
    if (!clean) return;
    if (onSendEmote) onSendEmote(clean);
    setCustomText("");
    setIsOpen(false);
  };

  const throwAt = (type, targetPlayerID) => {
    if (!type || !onThrowReaction) return;
    onThrowReaction(type, String(targetPlayerID));
    setThrowType(null);
    setIsOpen(false);
  };

  const selectThrowable = type => {
    if (!onThrowReaction || opponents.length === 0) return;

    if (opponents.length === 1) {
      throwAt(type, opponents[0].id);
      return;
    }

    setThrowType(throwType === type ? null : type);
  };

  return (
    <div className="quick-chat-container">
      <button
        type="button"
        className="quick-chat-toggle-btn"
        onClick={() => {
          setIsOpen(!isOpen);
          setThrowType(null);
        }}
        title="Gáy / ném đồ"
      >
        💬 Gáy {isOpen ? "▲" : "▼"}
      </button>

      {isOpen && (
        <div className="quick-chat-menu">
          <div className="quick-chat-title">Gáy nhanh</div>
          <div className="quick-chat-grid">
            {QUICK_EMOTES.map((text, idx) => (
              <button
                key={idx}
                type="button"
                className="quick-chat-item"
                onClick={() => handleSelect(text)}
              >
                {text}
              </button>
            ))}
          </div>

          <div className="quick-chat-custom">
            <input
              type="text"
              value={customText}
              maxLength={80}
              placeholder="Tự gáy..."
              onChange={event => setCustomText(event.target.value)}
              onKeyDown={event => {
                if (event.key === "Enter") sendCustom();
              }}
            />
            <button type="button" onClick={sendCustom} disabled={!customText.trim()}>
              Gửi
            </button>
          </div>

          <div className="quick-chat-title quick-chat-title--throw">Ném đồ</div>
          <div className="quick-chat-throwables">
            {THROWABLES.map(item => (
              <button
                key={item.type}
                type="button"
                className={`quick-chat-throw ${throwType === item.type ? "is-selected" : ""}`}
                onClick={() => selectThrowable(item.type)}
                title={
                  opponents.length === 1
                    ? `${item.label} vào ${opponents[0].name}`
                    : item.label
                }
              >
                <span>{item.emoji}</span>
                <small>{item.label}</small>
              </button>
            ))}
          </div>

          {opponents.length === 1 && (
            <div className="quick-chat-throw-hint">
              1v1: bấm món là ném thẳng vào {opponents[0].name}
            </div>
          )}

          {throwType && opponents.length > 1 && (
            <div className="quick-chat-targets">
              <span>Ném vào ai?</span>
              <div>
                {opponents.map(player => (
                  <button
                    key={player.id}
                    type="button"
                    onClick={() => throwAt(throwType, player.id)}
                  >
                    {player.name}
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

QuickChat.propTypes = {
  onSendEmote: PropTypes.func.isRequired,
  onThrowReaction: PropTypes.func,
  playerID: PropTypes.string,
  gameMetadata: PropTypes.array,
};
