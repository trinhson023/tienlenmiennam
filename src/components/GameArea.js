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
    const pID = playerID ? parseInt(playerID) : 0;
    const numPlayers = this.props.ctx.numPlayers || Object.keys(this.props.G.players).length || 4;

    const renderPlayerBox = (idx) => {
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
        <PlayerStatus
          key={idxStr}
          playerName={playerName}
          cardsLeft={this.props.G.cardsLeft[idx]}
          className={statusClass}
          winner={winner}
          emote={emote}
        />
      );
    };

    const center = this.props.ctx.gameover ? (
      <div key="center" className="round-type">
        🏆 Ván Đấu Kết Thúc!
      </div>
    ) : (
      <div key="center" className="round-type">
        {this.props.G.roundType}
        <CardArea
          className="center"
          listName="center"
          cards={this.props.G.center}
          disabled={true}
        />
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
      <div className="game-area">
        {topPlayer !== null && (
          <div className="center-container" style={{ marginBottom: "0.5em" }}>
            {renderPlayerBox(topPlayer)}
          </div>
        )}
        <div className="center-row">
          {leftPlayer !== null ? (
            <div>{renderPlayerBox(leftPlayer)}</div>
          ) : (
            <div style={{ width: "5em" }}></div>
          )}
          {center}
          {rightPlayer !== null ? (
            <div>{renderPlayerBox(rightPlayer)}</div>
          ) : (
            <div style={{ width: "5em" }}></div>
          )}
        </div>
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
