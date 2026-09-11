// src/PlayerStatus.js
import React, { PureComponent } from "react";
import PropTypes from "prop-types";
import Emoji from "a11y-react-emoji";

export default class PlayerStatus extends PureComponent {
  constructor(props) {
    super(props);
    this.state = {
      activeEmote: null,
    };
    this.emoteTimeout = null;
  }

  componentDidMount() {
    this.checkEmote(this.props.emote);
  }

  componentDidUpdate(prevProps) {
    if (this.props.emote !== prevProps.emote) {
      this.checkEmote(this.props.emote);
    }
  }

  componentWillUnmount() {
    if (this.emoteTimeout) clearTimeout(this.emoteTimeout);
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

  render() {
    const isCurrent =
      this.props.className && this.props.className.includes("current-player-status");
    const isPassed =
      this.props.className && this.props.className.includes("passed-player-status");
    const isNotYetWon =
      this.props.winner === -1 || this.props.winner === undefined;

    return (
      <div className={this.props.className} style={{ position: "relative" }}>
        {this.state.activeEmote && (
          <div className="chat-bubble">
            {this.state.activeEmote}
          </div>
        )}
        <div style={{ fontWeight: "bold", fontSize: "1.05em", marginBottom: "2px" }}>
          {this.props.playerName}
        </div>
        {isCurrent && isNotYetWon && (
          <div className="turn-badge">👉 ĐANG ĐÁNH</div>
        )}
        {isPassed && isNotYetWon && (
          <div className="pass-badge">❌ BỎ LƯỢT</div>
        )}
        {winners(this.props.winner, this.props.cardsLeft)}
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
      return (
        <div>
          <Emoji symbol="🂠" label="cards left" />: {cardsLeft} lá
        </div>
      );
  }
}

PlayerStatus.propTypes = {
  playerName: PropTypes.string,
  cardsLeft: PropTypes.number,
  className: PropTypes.string,
  winner: PropTypes.number,
  emote: PropTypes.object,
};
