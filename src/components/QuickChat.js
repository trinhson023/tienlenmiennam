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

  const opponents = (gameMetadata || []).filter(
    player => String(player.id) !== String(playerID) && player.name
  );

  const chooseThrowTarget = targetPlayerID => {
    if (!throwType || !onThrowReaction) return;
    onThrowReaction(throwType, String(targetPlayerID));
    setThrowType(null);
    setIsOpen(false);
  };

  return (
    <div className="quick-chat-container">
      <button
        type="button"
        className="quick-chat-toggle-btn"
        onClick={() => setIsOpen(!isOpen)}
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
                onClick={() => setThrowType(throwType === item.type ? null : item.type)}
                title={item.label}
              >
                <span>{item.emoji}</span>
                <small>{item.label}</small>
              </button>
            ))}
          </div>

          {throwType && (
            <div className="quick-chat-targets">
              <span>Ném vào ai?</span>
              <div>
                {opponents.map(player => (
                  <button
                    key={player.id}
                    type="button"
                    onClick={() => chooseThrowTarget(player.id)}
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
