// src/components/PlayerArea.js
import PropTypes from "prop-types";
import React, { Component } from "react";
import Buttons from "./Buttons";
import { getClassName } from "./_helperFunctions";
import CardArea from "./CardArea";
import PlayerStatus from "./PlayerStatus";
import QuickChat from "./QuickChat";
const _ = require("lodash");

export default class PlayerArea extends Component {
  constructor(props) {
    super(props);
    this.state = {
      rematchLoading: false,
      rematchError: "",
    };
  }

  handleRematch = async () => {
    if (this.state.rematchLoading || !this.props.matchID) return;

    this.setState({ rematchLoading: true, rematchError: "" });
    try {
      const response = await fetch(`/api/rooms/${this.props.matchID}/rematch`, {
        method: "POST",
        headers: {
          "x-player-id": String(this.props.playerID || ""),
          "x-player-credentials": String(this.props.credentials || ""),
        },
      });
      const data = await response.json();
      if (!response.ok || !data.success) {
        throw new Error(data.error || "Không thể đánh lại trên bàn này.");
      }

      // boardgame.io 0.39 does not expose a clean "resync this match now"
      // hook to the board component. Reload the lobby and let the resume helper
      // open the SAME match ID again using the credential stored in lobbyState.
      window.location.href = `/?resume=${encodeURIComponent(this.props.matchID)}`;
    } catch (err) {
      this.setState({
        rematchLoading: false,
        rematchError: err.message || "Không thể đánh lại.",
      });
    }
  };

  renderGameOver(playerID) {
    const winnersList =
      (this.props.ctx.gameover && this.props.ctx.gameover.winners) ||
      this.props.G.winners ||
      [];
    const medals = [
      "🥇 Quán Quân (Về Nhất)",
      "🥈 Á Quân (Về Nhì)",
      "🥉 Hạng Ba",
      "💩 Về Bét",
    ];
    const hasBots =
      this.props.gameMetadata &&
      this.props.gameMetadata.some(p => p.name && p.name.includes("Bot"));

    return (
      <div className="gameover-container">
        <h2 className="gameover-title">🏆 KẾT QUẢ VÁN ĐẤU 🏆</h2>
        <div className="podium">
          {winnersList.map((pid, rank) => {
            const p = _.find(this.props.gameMetadata, { id: parseInt(pid) });
            const pName = p ? p.name : `Người chơi ${parseInt(pid) + 1}`;
            const isMe = pid === playerID;
            return (
              <div
                key={pid}
                className={`podium-item ${isMe ? "podium-me" : ""}`}
              >
                <span className="podium-rank">
                  {medals[rank] || `Hạng ${rank + 1}`}:
                </span>
                <span className="podium-name">
                  {pName} {isMe ? " (Bạn)" : ""}
                </span>
              </div>
            );
          })}
        </div>
        <div className="premium-rematch-copy">
          {hasBots
            ? "Giữ nguyên bàn và ghế Bot, chia bài mới ngay tại đây."
            : "Giữ nguyên bàn và toàn bộ người chơi, bắt đầu ván mới."}
        </div>
        <div className="premium-gameover-actions">
          <button
            className="play-active"
            onClick={this.handleRematch}
            disabled={this.state.rematchLoading}
          >
            {this.state.rematchLoading ? "⏳ Đang chia bài..." : "🔥 Đánh Lại Trên Bàn Này"}
          </button>
          <button
            className="play-active premium-secondary-action"
            onClick={() => {
              window.location.href = "/";
            }}
          >
            ↩ Trở Về Sảnh
          </button>
        </div>
        {this.state.rematchError && (
          <div className="premium-rematch-error">{this.state.rematchError}</div>
        )}
      </div>
    );
  }

  render() {
    const playerID = this.props.playerID;
    const areaClass = getClassName(this.props, playerID, "player-area");

    if (!playerID) {
      return <div className={areaClass} />;
    }

    if (this.props.ctx.gameover) {
      return <div className={areaClass}>{this.renderGameOver(playerID)}</div>;
    }

    const winner = this.props.G.winners.findIndex(x => x === playerID);
    const player = _.find(this.props.gameMetadata, {
      id: parseInt(playerID),
    });
    const playerName = player ? player.name : playerID;
    const emote =
      this.props.G.lastEmote && this.props.G.lastEmote.playerID === playerID
        ? this.props.G.lastEmote
        : null;

    if (winner !== -1) {
      return (
        <div className={areaClass}>
          <div className="premium-finished-player">
            <PlayerStatus
              playerName={playerName}
              cardsLeft={this.props.G.cardsLeft[playerID]}
              className={
                getClassName(this.props, playerID, "player-status") + " no-shadow"
              }
              winner={winner}
              emote={emote}
            />
            <div className="premium-finished-copy">
              🎉 Bạn đã đánh hết bài! Đang chờ ván kết thúc...
            </div>
            <QuickChat
              onSendEmote={text =>
                this.props.moves.sendEmote && this.props.moves.sendEmote(text)
              }
            />
          </div>
        </div>
      );
    }

    return (
      <div className={areaClass}>
        <div className="premium-control-deck">
          <div className="premium-control-player">
            <PlayerStatus
              playerName={playerName}
              cardsLeft={this.props.G.cardsLeft[playerID]}
              className={
                getClassName(this.props, playerID, "player-status") + " no-shadow"
              }
              winner={winner}
              emote={emote}
            />
          </div>

          <div className="premium-control-staging">
            <div className="premium-control-label">BÀI ĐANG CHỌN</div>
            <CardArea
              className={getClassName(
                this.props,
                this.props.playerID,
                "staging-area"
              )}
              listName="stagingArea"
              cards={this.props.G.players[playerID].stagingArea}
              onCardClick={cardId =>
                this.props.moves.toggleCard &&
                this.props.moves.toggleCard(cardId, "stagingArea")
              }
            />
          </div>

          <div className="premium-control-actions">
            <Buttons {...this.props} />
            <QuickChat
              onSendEmote={text =>
                this.props.moves.sendEmote && this.props.moves.sendEmote(text)
              }
            />
          </div>
        </div>

        <div className="premium-hand-slot">
          <CardArea
            className={getClassName(this.props, playerID, "hand")}
            listName="hand"
            cards={this.props.G.players[playerID].hand}
            onCardClick={cardId =>
              this.props.moves.toggleCard &&
              this.props.moves.toggleCard(cardId, "hand")
            }
          />
        </div>
      </div>
    );
  }
}

PlayerArea.propTypes = {
  G: PropTypes.object,
  ctx: PropTypes.object,
  moves: PropTypes.object,
  playerID: PropTypes.string,
  gameMetadata: PropTypes.array,
  matchID: PropTypes.string,
  credentials: PropTypes.string,
};
