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
    const numPlayers =
      this.props.ctx.numPlayers ||
      (this.props.gameMetadata && this.props.gameMetadata.length) ||
      Object.keys(this.props.G.players || {}).length ||
      2;
    const replayQuery = hasBots
      ? numPlayers >= 4
        ? "quick4=true"
        : "quick2=true"
      : numPlayers >= 4
      ? "human4=true"
      : "";

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
        <div className="premium-gameover-actions">
          <button
            className="play-active"
            onClick={() => {
              window.location.href = replayQuery ? `/?${replayQuery}` : "/";
            }}
          >
            {hasBots
              ? numPlayers >= 4
                ? "🔥 Đánh Lại (vs 3 Bot AI)"
                : "🔥 Đánh Lại (vs Bot AI)"
              : "🌟 Tạo Bàn Mới Cùng Nhóm"}
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
};
