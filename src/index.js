// src/index.js

import React from "react";
import ReactDOM from "react-dom";
import "./index.scss";
import "./premium-v2.scss";
import "./final-polish.scss";
import "./integration-fixes.scss";
import "./media-layout.scss";
import "./social-fixes.scss";
import { default as App } from "./App";
import LobbyView from "./components/LobbyView";
import LobbyIdentityGuard from "./components/LobbyIdentityGuard";
import LobbyResumeGuard from "./components/LobbyResumeGuard";
import LobbyRoomTagger from "./components/LobbyRoomTagger";
import * as serviceWorker from "./serviceWorker";
import { LOBBY } from "./config";

if (LOBBY) {
  ReactDOM.render(
    <React.StrictMode>
      <LobbyIdentityGuard />
      <LobbyRoomTagger />
      <LobbyResumeGuard />
      <LobbyView />
    </React.StrictMode>,
    document.getElementById("root")
  );
} else {
  ReactDOM.render(
    <React.StrictMode>
      <App />
    </React.StrictMode>,
    document.getElementById("root")
  );
}
// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://bit.ly/CRA-PWA
serviceWorker.unregister();
