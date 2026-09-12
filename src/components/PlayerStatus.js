// src/PlayerStatus.js
import React, { PureComponent } from "react";
import PropTypes from "prop-types";
import Emoji from "a11y-react-emoji";

const THROW_EMOJI = {
  bomb: "💣",
  tomato: "🍅",
  poop: "💩",
};

function getInitials(name) {
  if (!name) return "P";
  return name
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map(part => part.charAt(0).toUpperCase())
    .join("");
}

export default class PlayerStatus extends PureComponent {
  constructor(props) {
    super(props);
    this.state = {
      activeEmote: null,
      activeThrow: null,
    };
    this.emoteTimeout = null;
    this.throwTimeout = null;
  }

  componentDidMount() {
    this.checkEmote(this.props.emote);
    this.checkThrow(this.props.throwReaction);
  }

  componentDidUpdate(prevProps) {
    if (this.props.emote !== prevProps.emote) {
      this.checkEmote(this.props.emote);
    }
    if (this.props.throwReaction !== prevProps.throwReaction) {
      this.checkThrow(this.props.throwReaction);
    }
  }

  componentWillUnmount() {
    if (this.emoteTimeout) clearTimeout(this.emoteTimeout);
    if (this.throwTimeout) clearTimeout(this.throwTimeout);
  }

  checkEmote(emote) {
    if (emote && Date.now() - emote.time < 4500) {
      this.setState({ activeEmote: emote.text });
      if (this.emoteTimeout) clearTimeout(this.emoteTimeout);
      this.emoteTimeout = setTimeout(() => {
        this.setState({ activeEmote: null });
      }, 4000);
    }
  }

  checkThrow(reaction) {
    if (reaction && Date.now() - reaction.time < 3500) {
      this.setState({
        activeThrow: {
          type: reaction.type,
          key: `${reaction.time}-${reaction.fromPlayerID}`,
        },
      });
      if (this.throwTimeout) clearTimeout(this.throwTimeout);
      this.throwTimeout = setTimeout(() => {
        this.setState({ activeThrow: null });
      }, 1800);
    }
  }

  render() {
    const isCurrent =
      this.props.className && this.props.className.includes("current-player-status");
    const isPassed =
      this.props.className && this.props.className.includes("passed-player-status");
    const isNotYetWon =
      this.props.winner === -1 || this.props.winner === undefined;
    const throwType = this.state.activeThrow && this.state.activeThrow.type;

    return (
      <div
        className={`${this.props.className} premium-player-card${
          throwType ? ` premium-player-card--hit premium-player-card--hit-${throwType}` : ""
        }`}
      >
        {this.state.activeEmote && (
          <div className="chat-bubble premium-chat-bubble">
            {this.state.activeEmote}
          </div>
        )}

        {this.state.activeThrow && (
          <div
            className={`premium-throw-hit premium-throw-hit--${throwType}`}
            key={this.state.activeThrow.key}
            aria-hidden="true"
          >
            <span className="premium-throw-projectile">
              {THROW_EMOJI[throwType] || "💥"}
            </span>
            <span className="premium-throw-impact">
              {throwType === "bomb" ? "💥" : throwType === "tomato" ? "💦" : "🤢"}
            </span>
          </div>
        )}

        <div className="premium-player-card__avatar" aria-hidden="true">
          {getInitials(this.props.playerName)}
        </div>

        <div className="premium-player-card__content">
          <div className="premium-player-card__name">{this.props.playerName}</div>

          {isCurrent && isNotYetWon && (
            <div className="turn-badge premium-turn-badge">
              <span className="premium-turn-dot" /> ĐANG ĐÁNH
            </div>
          )}

          {isPassed && isNotYetWon && (
            <div className="pass-badge premium-pass-badge">ĐÃ BỎ LƯỢT</div>
          )}

          <div className="premium-player-card__meta">
            {winners(this.props.winner, this.props.cardsLeft)}
          </div>
        </div>
      </div>
    );
  }
}

function winners(winner, cardsLeft) {
  switch (winner) {
    case 0:
      return (
        <div>
          <Emoji symbol="🥇" label="1st place" /> Về Nhất
        </div>
      );
    case 1:
      return (
        <div>
          <Emoji symbol="🥈" label="2nd place" /> Về Nhì
        </div>
      );
    case 2:
      return (
        <div>
          <Emoji symbol="🥉" label="3rd place" /> Về Ba
        </div>
      );
    default:
      return <div>{cardsLeft} lá còn lại</div>;
  }
}

PlayerStatus.propTypes = {
  playerName: PropTypes.string,
  cardsLeft: PropTypes.number,
  className: PropTypes.string,
  winner: PropTypes.number,
  emote: PropTypes.object,
  throwReaction: PropTypes.object,
};
