import React, { Component } from "react";
import PropTypes from "prop-types";

export default class StagingAreaButtons extends Component {
  render() {
    const className =
      this.props.currentPlayer === this.props.playerID
        ? "button current-staging-area-btn"
        : "button staging-area-btn";
    return (
      <div className="center-container" style={{ flexWrap: "wrap" }}>
        <button
          className={className}
          key="clearStagingArea"
          onClick={() => this.props.clearStagingArea()}
        >
          Hạ bài xuống (Clear)
        </button>
        <button
          className={className}
          key="sortStagingArea"
          onClick={() => this.props.sortStagingArea()}
        >
          Xếp ô chọn
        </button>
        {this.props.moves && this.props.moves.sortHand && (
          <button
            className={className}
            key="sortHand"
            onClick={() => this.props.moves.sortHand()}
          >
            Sắp xếp bài trên tay
          </button>
        )}
      </div>
    );
  }
}

StagingAreaButtons.propTypes = {
  currentPlayer: PropTypes.string,
  playerID: PropTypes.string,
  clearStagingArea: PropTypes.func,
  sortStagingArea: PropTypes.func,
  moves: PropTypes.object,
};
