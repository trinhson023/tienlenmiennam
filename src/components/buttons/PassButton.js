import React, { Component } from "react";
import PropTypes from "prop-types";

export default class PassButton extends Component {
  render() {
    return (
      <button
        className="pass-btn"
        key="passTurn"
        onClick={() => this.props.passTurn()}
      >
        Bỏ Lượt (Pass)
      </button>
    );
  }
}

PassButton.propTypes = {
  passTurn: PropTypes.func,
};
