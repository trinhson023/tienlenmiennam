/* eslint-disable react/prop-types */
import React from "react";
import PropTypes from "prop-types";
import { Client as RawClient } from "boardgame.io/client";

export default function RematchAwareClientFactory(opts) {
  const {
    game,
    board: Board,
    multiplayer,
    debug,
    numPlayers,
    enhancer,
  } = opts;

    const RematchAwareClient = class extends React.Component {
      constructor(props) {
      super(props);
      this.unsubscribe = null;
      this.client = RawClient({
        game,
        debug,
        numPlayers,
        multiplayer,
        gameID: props.gameID,
        playerID: props.playerID,
        credentials: props.credentials,
        enhancer,
      });
    }

    componentDidMount() {
      this.unsubscribe = this.client.subscribe(() => this.forceUpdate());
      this.client.start();
      window.addEventListener("tienlen:force-sync", this.handleForceSync);
      this.attachRematchSocketListener();
    }

    componentWillUnmount() {
      window.removeEventListener("tienlen:force-sync", this.handleForceSync);
      this.detachRematchSocketListener();
      this.client.stop();
      if (this.unsubscribe) this.unsubscribe();
    }

    componentDidUpdate(prevProps) {
      if (this.props.gameID !== prevProps.gameID) {
        this.client.updateGameID(this.props.gameID);
      }
      if (this.props.playerID !== prevProps.playerID) {
        this.client.updatePlayerID(this.props.playerID);
      }
      if (this.props.credentials !== prevProps.credentials) {
        this.client.updateCredentials(this.props.credentials);
      }
      this.attachRematchSocketListener();
    }

    attachRematchSocketListener = () => {
      const transport = this.client && this.client.transport;
      const socket = transport && transport.socket;
      if (!socket || this.rematchSocket === socket) return;

      this.detachRematchSocketListener();
      this.rematchSocket = socket;
      socket.on("table-rematch", this.handleServerRematch);
    };

    detachRematchSocketListener = () => {
      if (!this.rematchSocket) return;
      if (this.rematchSocket.removeListener) {
        this.rematchSocket.removeListener("table-rematch", this.handleServerRematch);
      } else if (this.rematchSocket.off) {
        this.rematchSocket.off("table-rematch", this.handleServerRematch);
      }
      this.rematchSocket = null;
    };

    handleServerRematch = gameID => {
      if (!gameID || String(gameID) !== String(this.client.gameID)) return;
      this.requestSync();
    };

    handleForceSync = event => {
      const targetGameID =
        event && event.detail && event.detail.gameID
          ? String(event.detail.gameID)
          : null;
      if (targetGameID && targetGameID !== String(this.client.gameID)) return;
      this.requestSync();
    };

    requestSync = () => {
      const transport = this.client && this.client.transport;
      const socket = transport && transport.socket;

      // boardgame.io 0.39 has no public resync method, but the SocketIO
      // transport already exposes the same sync event used on connect.
      // Emit it directly without resetting the React tree, so Music / Shorts
      // iframe players remain mounted and the page keeps its scroll position.
      if (socket && socket.emit) {
        socket.emit(
          "sync",
          this.client.gameID,
          this.client.playerID,
          transport.numPlayers
        );
        return;
      }

      // Fallback for a transport that has not connected yet.
      this.client.updateGameID(this.client.gameID);
    };

    render() {
      const state = this.client.getState();
      if (state === null) {
        return <div className="bgio-loading">connecting...</div>;
      }

      return (
        <div className="bgio-client">
          <Board
            {...state}
            {...this.props}
            isMultiplayer={Boolean(multiplayer)}
            moves={this.client.moves}
            events={this.client.events}
            gameID={this.client.gameID}
            playerID={this.client.playerID}
            reset={this.client.reset}
            undo={this.client.undo}
            redo={this.client.redo}
            log={this.client.log}
            gameMetadata={this.client.gameMetadata}
          />
        </div>
      );
    }
  };

  RematchAwareClient.propTypes = {
    gameID: PropTypes.any,
    playerID: PropTypes.any,
    credentials: PropTypes.any,
  };

  return RematchAwareClient;
}
