import React from "react";
import PropTypes from "prop-types";
import { validPlay } from "../../moves/cardPlayMoves";
const _ = require("lodash");

export default function PlayCardsButton({
  currentPlayer,
  playerID,
  player,
  roundType,
  center,
  cardsToCenter,
}) {
  if (playerID !== currentPlayer) {
    return (
      <button className="wait" disabled={true} key="playcards">
        ⏳ Chưa tới lượt bạn
      </button>
    );
  }

  let stagingArea = player.stagingArea;
  let threeSpadesInHand = _.find(player.hand, {
    rank: "3",
    suit: "S",
  });

  if (stagingArea.length === 0) {
    return (
      <button className="disabled" disabled={true} key="playcards">
        {threeSpadesInHand ? "Chọn bài (Bắt buộc có 3♠)" : "Kéo bài lên ô giữa để đánh"}
      </button>
    );
  }

  const p = validPlay(stagingArea, roundType, center, threeSpadesInHand);

  if (typeof p === "string") {
    let message = p;
    if (p === "Invalid Combination") message = "Bộ bài không hợp lệ";
    else if (p === "First Play Must Include 3♠") message = "Nước đầu phải có 3♠";
    else if (p === "Does Not Match Center") message = "Không cùng loại bài trên bàn";
    else if (p === "Does Not Beat Center") message = "Bài nhỏ hơn bài trên bàn";

    return (
      <button className="disabled" disabled={true} key="playcards">
        {message}
      </button>
    );
  }

  return (
    <button className="play-active" key="playcards" onClick={() => cardsToCenter()}>
      {stagingArea.length === 1 ? "Đánh 1 Lá" : `Đánh (${stagingArea.length} Lá)`}
    </button>
  );
}

PlayCardsButton.propTypes = {
  currentPlayer: PropTypes.string,
  playerID: PropTypes.string,
  player: PropTypes.object,
  roundType: PropTypes.string,
  center: PropTypes.array,
  cardsToCenter: PropTypes.func,
};
