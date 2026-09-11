import React, { Component } from "react";
import PropTypes from "prop-types";

export default class StagingAreaButtons extends Component {
  render() {
    const active = this.props.currentPlayer === this.props.playerID;
    const className = active
      ? "table-tool table-tool--active"
      : "table-tool";

    return (
      <div className="table-tools" aria-label="Công cụ xếp bài">
        <button
          className={className}
          key="clearStagingArea"
          onClick={() => this.props.clearStagingArea()}
          title="Bỏ toàn bộ bài đang chọn về tay"
        >
          <span className="table-tool__icon">↩</span>
          <span>Bỏ chọn</span>
        </button>

        <button
          className={className}
          key="sortStagingArea"
          onClick={() => this.props.sortStagingArea()}
          title="Sắp xếp các lá đang chọn"
        >
          <span className="table-tool__icon">⇅</span>
          <span>Xếp bài chọn</span>
        </button>

        {this.props.moves && this.props.moves.sortHand && (
          <button
            className={className}
            key="sortHand"
            onClick={() => this.props.moves.sortHand()}
            title="Sắp xếp lại bài trên tay"
          >
            <span className="table-tool__icon">♠</span>
            <span>Xếp tay</span>
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
