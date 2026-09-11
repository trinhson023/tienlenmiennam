// src/components/GameArea.js
import PropTypes from "prop-types";
import React, { Component } from "react";
import { getClassName } from "./_helperFunctions";
import CardArea from "./CardArea";
import PlayerStatus from "./PlayerStatus";
const _ = require("lodash");

export default class GameArea extends Component {
  render() {
    const playerID = this.props.playerID;
    const pID = playerID ? parseInt(playerID, 10) : 0;
    const numPlayers =
      this.props.ctx.numPlayers || Object.keys(this.props.G.players).length || 4;

    const renderPlayerBox = (idx, seat) => {
      const idxStr = idx.toString();
      const player = _.find(this.props.gameMetadata, { id: idx });
      const playerName = player ? player.name : `Người chơi ${idx + 1}`;
      const statusClass = getClassName(this.props, idxStr, "player-status");
      const winner = this.props.G.winners.findIndex(x => x === idxStr);
      const emote =
        this.props.G.lastEmote && this.props.G.lastEmote.playerID === idxStr
          ? this.props.G.lastEmote
          : null;

      return (
        <div className={`premium-seat premium-seat--${seat}`} key={idxStr}>
          <PlayerStatus
            playerName={playerName}
            cardsLeft={this.props.G.cardsLeft[idx]}
            className={statusClass}
            winner={winner}
            emote={emote}
          />
          <div className="premium-seat__cards" aria-hidden="true">
            <span />
            <span />
            <span />
          </div>
        </div>
      );
    };

    const center = this.props.ctx.gameover ? (
      <div className="premium-center-state premium-center-state--gameover">
        <span className="premium-center-state__icon">🏆</span>
        <strong>Ván đấu kết thúc</strong>
        <span>Kết quả đang được tổng hợp</span>
      </div>
    ) : (
      <div className="premium-center-stack">
        <div className="premium-center-label">
          <span>BÀN ĐẤU</span>
          <strong>{this.props.G.roundType || "any"}</strong>
        </div>
        <CardArea
          className="center premium-center-cards"
          listName="center"
          cards={this.props.G.center}
          disabled={true}
        />
        {(!this.props.G.center || this.props.G.center.length === 0) && (
          <div className="premium-center-empty">
            <span className="premium-center-empty__mark">♠</span>
            <span>Chờ nước bài đầu tiên</span>
          </div>
        )}
      </div>
    );

    let topPlayer = null;
    let leftPlayer = null;
    let rightPlayer = null;

    if (numPlayers === 4) {
      topPlayer = (pID + 2) % 4;
      leftPlayer = (pID + 3) % 4;
      rightPlayer = (pID + 1) % 4;
    } else if (numPlayers === 3) {
      leftPlayer = (pID + 1) % 3;
      rightPlayer = (pID + 2) % 3;
    } else if (numPlayers === 2) {
      topPlayer = (pID + 1) % 2;
    }

    return (
      <div className="game-area premium-game-area">
        <div className="premium-table-glow" aria-hidden="true" />
        {topPlayer !== null && renderPlayerBox(topPlayer, "top")}
        {leftPlayer !== null && renderPlayerBox(leftPlayer, "left")}
        {rightPlayer !== null && renderPlayerBox(rightPlayer, "right")}
        <div className="premium-center-zone">{center}</div>
      </div>
    );
  }
}

GameArea.propTypes = {
  G: PropTypes.object,
  ctx: PropTypes.object,
  playerID: PropTypes.string,
  gameMetadata: PropTypes.array,
};
