// src/TienLenBoard.js

import React, { Component } from "react";
import PropTypes from "prop-types";
import { DragDropContext } from "react-beautiful-dnd";
import GameArea from "./components/GameArea";
import GameHUD from "./components/GameHUD";
import MusicRoom from "./components/MusicRoom";
import PlayerArea from "./components/PlayerArea";
import { validChop } from "./moves/helper-functions/cardComparison";
import {
  getSoundEnabled,
  playCardSound,
  playChatSound,
  playChopSound,
  playTurnSound,
  playVictorySound,
  setSoundEnabled,
} from "./utils/soundEffects";

class TienLenBoard extends Component {
  constructor(props) {
    super(props);
    this.state = {
      effect: null,
      soundEnabled: getSoundEnabled(),
    };
    this.effectTimer = null;
  }

  componentWillUnmount() {
    if (this.effectTimer) clearTimeout(this.effectTimer);
  }

  showEffect(type, label, duration = 900) {
    if (this.effectTimer) clearTimeout(this.effectTimer);
    this.setState({ effect: { type, label, key: Date.now() } });
    this.effectTimer = setTimeout(() => {
      this.setState({ effect: null });
    }, duration);
  }

  duckMusic(duration = 900) {
    if (typeof window === "undefined") return;
    window.dispatchEvent(
      new CustomEvent("tienlen:duck-music", { detail: { duration } })
    );
  }

  componentDidUpdate(prevProps) {
    if (!this.props.G || !prevProps.G) return;

    const prevCenter = prevProps.G.center || [];
    const currentCenter = this.props.G.center || [];
    const centerChanged =
      currentCenter.length > 0 &&
      currentCenter !== prevCenter &&
      (prevCenter.length === 0 ||
        currentCenter[0] !== prevCenter[0] ||
        currentCenter.length !== prevCenter.length);

    if (centerChanged) {
      const chopped = prevCenter.length > 0 && validChop(prevCenter, currentCenter);
      if (chopped) {
        playChopSound();
        this.duckMusic(1100);
        this.showEffect("chop", "⚡ CHẶT!", 1050);
      } else {
        playCardSound();
        const count = currentCenter.length;
        this.showEffect("play", count > 1 ? `RA ${count} LÁ` : "RA BÀI", 520);
      }
    }

    const prevWinners = (prevProps.G.winners && prevProps.G.winners.length) || 0;
    const currWinners = (this.props.G.winners && this.props.G.winners.length) || 0;
    if (
      currWinners > prevWinners ||
      (!prevProps.ctx.gameover && this.props.ctx.gameover)
    ) {
      playVictorySound();
      this.duckMusic(1400);
      this.showEffect(
        this.props.ctx.gameover ? "victory" : "finish",
        this.props.ctx.gameover ? "🏆 KẾT THÚC VÁN" : "✨ CÓ NGƯỜI VỀ!",
        1500
      );
    }

    const prevEmote = prevProps.G.lastEmote;
    const currEmote = this.props.G.lastEmote;
    if (currEmote && (!prevEmote || currEmote.time !== prevEmote.time)) {
      playChatSound();
    }

    const prevCurrent = prevProps.ctx && prevProps.ctx.currentPlayer;
    const current = this.props.ctx && this.props.ctx.currentPlayer;
    if (
      current !== prevCurrent &&
      String(current) === String(this.props.playerID) &&
      !this.props.ctx.gameover
    ) {
      playTurnSound();
      this.showEffect("turn", "ĐẾN LƯỢT BẠN", 650);
    }
  }

  toggleSound = () => {
    const next = !this.state.soundEnabled;
    setSoundEnabled(next);
    this.setState({ soundEnabled: next });
  };

  renderEffect() {
    const { effect } = this.state;
    if (!effect) return null;
    return (
      <div className={`game-event game-event--${effect.type}`} key={effect.key}>
        <div className="game-event__shock" />
        <div className="game-event__label">{effect.label}</div>
      </div>
    );
  }

  render() {
    const fxClass = this.state.effect
      ? ` game-vfx-${this.state.effect.type}`
      : "";

    return (
      <div className={`game premium-table-shell${fxClass}`}>
        <GameHUD {...this.props} />
        <div className="premium-utility-bar">
          <button
            className="premium-sound-toggle"
            onClick={this.toggleSound}
            title="Bật / tắt hiệu ứng âm thanh"
          >
            {this.state.soundEnabled ? "🔊 SFX" : "🔇 SFX"}
          </button>
          <span className="premium-utility-hint">Kéo hoặc click bài để chọn</span>
        </div>
        <DragDropContext onDragEnd={this.props.moves.relocateCards}>
          <GameArea {...this.props} />
          <div className="premium-player-dock">
            <PlayerArea {...this.props} />
          </div>
        </DragDropContext>
        <MusicRoom
          G={this.props.G}
          playerID={this.props.playerID}
          moves={this.props.moves}
        />
        {this.renderEffect()}
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
