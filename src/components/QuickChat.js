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

export default function QuickChat({ onSendEmote }) {
  const [isOpen, setIsOpen] = useState(false);

  const handleSelect = (text) => {
    if (onSendEmote) {
      onSendEmote(text);
    }
    setIsOpen(false);
  };

  return (
    <div className="quick-chat-container">
      <button
        type="button"
        className="quick-chat-toggle-btn"
        onClick={() => setIsOpen(!isOpen)}
        title="Gáy Nhanh (Phím tắt trò chuyện)"
      >
        💬 Gáy Nhanh {isOpen ? "▲" : "▼"}
      </button>

      {isOpen && (
        <div className="quick-chat-menu">
          <div className="quick-chat-title">Chọn câu gáy:</div>
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
        </div>
      )}
    </div>
  );
}

QuickChat.propTypes = {
  onSendEmote: PropTypes.func.isRequired,
};
