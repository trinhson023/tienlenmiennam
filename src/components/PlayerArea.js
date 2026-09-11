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
  render() {
    let playerArea = [];
    const playerID = this.props.playerID;

    if (playerID) {
      const winner = this.props.G.winners.findIndex(x => x === playerID);
      const player = _.find(this.props.gameMetadata, {
        id: parseInt(playerID),
      });
      const playerName = player ? player.name : playerID;
      const emote =
        this.props.G.lastEmote && this.props.G.lastEmote.playerID === playerID
          ? this.props.G.lastEmote
          : null;

      if (!this.props.ctx.gameover) {
        playerArea.push(
          <div className="center-container" key="winScreen">
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
        );

        if (winner === -1) {
          playerArea.push(
            <div className="center-container" key="stagingArea">
              <CardArea
                className={getClassName(
                  this.props,
                  this.props.playerID,
                  "staging-area"
                )}
                listName="stagingArea"
                cards={this.props.G.players[playerID].stagingArea}
                onCardClick={(cardId) =>
                  this.props.moves.toggleCard &&
                  this.props.moves.toggleCard(cardId, "stagingArea")
                }
              />
            </div>
          );
          playerArea.push(
            <div key="buttons">
              <Buttons {...this.props} />
            </div>
          );
          playerArea.push(
            <div className="center-container" key="quickChat" style={{ margin: "4px 0" }}>
              <QuickChat
                onSendEmote={(text) =>
                  this.props.moves.sendEmote && this.props.moves.sendEmote(text)
                }
              />
            </div>
          );
          playerArea.push(
            <div className="center-container" key="hand">
              <CardArea
                className={getClassName(this.props, playerID, "hand")}
                listName="hand"
                cards={this.props.G.players[playerID].hand}
                onCardClick={(cardId) =>
                  this.props.moves.toggleCard &&
                  this.props.moves.toggleCard(cardId, "hand")
                }
              />
            </div>
          );
        } else {
          playerArea.push(
            <div key="congratulations" className="center-container" style={{ margin: "1em", fontSize: "1.2em", fontWeight: "bold" }}>
              🎉 Bạn đã đánh hết bài! Đang chờ ván kết thúc...
            </div>
          );
          playerArea.push(
            <div className="center-container" key="quickChat" style={{ margin: "4px 0" }}>
              <QuickChat
                onSendEmote={(text) =>
                  this.props.moves.sendEmote && this.props.moves.sendEmote(text)
                }
              />
            </div>
          );
        }
      } else {
        // Game Over screen with Leaderboard
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
          this.props.gameMetadata.some(
            (p) => p.name && p.name.includes("Bot")
          );

        playerArea.push(
          <div key="gameoverSummary" className="gameover-container">
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
            <div
              style={{
                marginTop: "1.5em",
                display: "flex",
                gap: "10px",
                justifyContent: "center",
                flexWrap: "wrap",
              }}
            >
              {hasBots ? (
                <button
                  className="play-active"
                  style={{
                    fontSize: "1.1em",
                    padding: "0.6em 1.6em",
                    background:
                      "linear-gradient(135deg, #10b981 0%, #059669 100%)",
                    border: "1px solid #6ee7b7",
                  }}
                  onClick={() => {
                    window.location.href = "/?quick4=true";
                  }}
                >
                  🔥 Chơi Tiếp Ván Mới (vs 3 Bot AI)
                </button>
              ) : (
                <button
                  className="play-active"
                  style={{
                    fontSize: "1.1em",
                    padding: "0.6em 1.6em",
                    background:
                      "linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)",
                    border: "1px solid #60a5fa",
                  }}
                  onClick={() => {
                    window.location.href = "/?human4=true";
                  }}
                >
                  🌟 Tạo Bàn Mới Cho 4 Người (Chơi Tiếp)
                </button>
              )}
              <button
                className="play-active"
                style={{ fontSize: "1.1em", padding: "0.6em 1.6em" }}
                onClick={() => {
                  window.location.href = "/";
                }}
              >
                🔄 Trở Về Sảnh (Lobby)
              </button>
            </div>
          </div>
        );
      }
    }
    return (
      <div className={getClassName(this.props, playerID, "player-area")}>
        {playerArea}
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
