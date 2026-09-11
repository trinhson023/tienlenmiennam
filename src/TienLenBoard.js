// src/TienLenBoard.js

import React, { Component } from "react";
import PropTypes from "prop-types";
import { DragDropContext } from "react-beautiful-dnd";
import GameArea from "./components/GameArea";
import GameHUD from "./components/GameHUD";
import PlayerArea from "./components/PlayerArea";
import { validChop } from "./moves/helper-functions/cardComparison";
import {
  playCardSound,
  playChopSound,
  playVictorySound,
  playChatSound,
} from "./utils/soundEffects";

class TienLenBoard extends Component {
  componentDidUpdate(prevProps) {
    if (!this.props.G || !prevProps.G) return;

    const prevCenter = prevProps.G.center || [];
    const currentCenter = this.props.G.center || [];
    if (
      currentCenter.length > 0 &&
      currentCenter !== prevCenter &&
      (prevCenter.length === 0 ||
        currentCenter[0] !== prevCenter[0] ||
        currentCenter.length !== prevCenter.length)
    ) {
      if (prevCenter.length > 0 && validChop(prevCenter, currentCenter)) {
        playChopSound();
      } else {
        playCardSound();
      }
    }

    const prevWinners = (prevProps.G.winners && prevProps.G.winners.length) || 0;
    const currWinners = (this.props.G.winners && this.props.G.winners.length) || 0;
    if (
      currWinners > prevWinners ||
      (!prevProps.ctx.gameover && this.props.ctx.gameover)
    ) {
      playVictorySound();
    }

    const prevEmote = prevProps.G.lastEmote;
    const currEmote = this.props.G.lastEmote;
    if (currEmote && (!prevEmote || currEmote.time !== prevEmote.time)) {
      playChatSound();
    }
  }

  render() {
    return (
      <div className="game premium-table-shell">
        <GameHUD {...this.props} />
        <DragDropContext onDragEnd={this.props.moves.relocateCards}>
          <GameArea {...this.props} />
          <div className="premium-player-dock">
            <PlayerArea {...this.props} />
          </div>
        </DragDropContext>
      </div>
    );
  }
}

TienLenBoard.propTypes = {
  G: PropTypes.object,
  ctx: PropTypes.object,
  moves: PropTypes.object,
  playerID: PropTypes.string,
  gameMetadata: PropTypes.array,
};

export default TienLenBoard;
