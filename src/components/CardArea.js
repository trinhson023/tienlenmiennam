// src/CardArea.js
import React from "react";
import Card from "./Card";
import { Droppable } from "react-beautiful-dnd";
import PropTypes from "prop-types";

export default function CardArea({
  cards,
  className,
  listName,
  disabled,
  onCardClick,
}) {
  return (
    <Droppable
      droppableId={listName}
      direction="horizontal"
      isDropDisabled={disabled}
    >
      {(provided, snapshot) => (
        <ul
          className={
            snapshot.isDraggingOver ? className + " dragging" : className
          }
          {...provided.droppableProps}
          ref={provided.innerRef}
        >
          {cards.map((card, index) => {
            return (
              <Card
                rank={card.rank}
                suit={card.suit}
                key={card.rank + card.suit}
                index={index}
                disabled={disabled}
                onCardClick={onCardClick}
              />
            );
          })}
          {provided.placeholder}
        </ul>
      )}
    </Droppable>
  );
}

CardArea.propTypes = {
  cards: PropTypes.array,
  className: PropTypes.string,
  disabled: PropTypes.bool,
  listName: PropTypes.string,
  onCardClick: PropTypes.func,
};
