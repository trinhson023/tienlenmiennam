/*
 * Tien Len Online - Casino Edition
 */

import React, { useState, useEffect } from "react";
import { Lobby } from "boardgame.io/react";
import { GAME_SERVER_URL, WEB_SERVER_URL, APP_PRODUCTION } from "../config";
import { default as BoardTienLen } from "../TienLenBoard";
import { default as GameTienLen } from "../TienLen";
import RematchAwareClientFactory from "./RematchAwareClientFactory";
import Rules from "./Rules";
import "./lobby.scss";

import { BrowserRouter as Router, Switch, Route, Link } from "react-router-dom";

GameTienLen.minPlayers = 2;
GameTienLen.maxPlayers = 4;

const { protocol, hostname, port } = window.location;
const portPart = port ? `:${port}` : "";

let gameServer = APP_PRODUCTION
  ? `${protocol}//${hostname}${portPart}`
  : GAME_SERVER_URL;
let lobbyServer = APP_PRODUCTION
  ? `${protocol}//${hostname}${portPart}`
  : WEB_SERVER_URL;

const importedGames = [{ game: GameTienLen, board: BoardTienLen }];

function QuickBotHelper() {
  const [activeRooms, setActiveRooms] = useState([]);
  const [loading, setLoading] = useState(false);
  const [statusMsg, setStatusMsg] = useState("");
  const [networkIp, setNetworkIp] = useState("");
  const [copiedLink, setCopiedLink] = useState(false);

  useEffect(() => {
    fetch("/api/server-info")
      .then(res => res.json())
      .then(data => {
        if (data && data.hostIp) {
          setNetworkIp(data.hostIp);
        }
      })
      .catch(() => {});
  }, []);

  const fetchRooms = async () => {
    try {
      const res = await fetch("/games/tien-len");
      const data = await res.json();
      const uniqueRooms = (data.rooms || []).filter(
        (r, idx, arr) => arr.findIndex((x) => x.gameID === r.gameID) === idx
      );
      setActiveRooms(uniqueRooms);
    } catch (e) {
      console.error(e);
    }
  };

  useEffect(() => {
    fetchRooms();
    const interval = setInterval(fetchRooms, 3000);

    if (window.location.search.includes("quick4=true")) {
      window.history.replaceState({}, document.title, "/");
      setTimeout(() => {
        handleCreateBotRoom(4);
      }, 400);
    } else if (window.location.search.includes("quick2=true")) {
      window.history.replaceState({}, document.title, "/");
      setTimeout(() => {
        handleCreateBotRoom(2);
      }, 400);
    } else if (window.location.search.includes("human4=true")) {
      window.history.replaceState({}, document.title, "/");
      setTimeout(() => {
        handleCreateHumanRoom(4);
      }, 400);
    }

    return () => clearInterval(interval);
  }, []);

  const handleCreateBotRoom = async (numPlayers = 2) => {
    setLoading(true);
    const botCount = numPlayers - 1;
    setStatusMsg(`Đang tạo bàn ${numPlayers} người và mời ${botCount} Bot AI...`);
    try {
      const createRes = await fetch("/games/tien-len/create", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ numPlayers: numPlayers }),
      });
      const createData = await createRes.json();
      const matchID = createData.gameID;

      const fillRes = await fetch(`/api/bot/fill/${matchID}?leaveHuman=true`, {
        method: "POST",
      });
      const fillData = await fillRes.json();

      if (fillData.success) {
        setStatusMsg(
          `✅ Đã tạo bàn [${matchID.substring(
            0,
            6
          )}] với ${botCount} Bot AI! Đang tự động vào ghế cho bạn...`
        );
        await fetchRooms();

        setTimeout(() => {
          try {
            const rows = document.querySelectorAll("#instances tr");
            let joined = false;
            for (const row of rows) {
              if (
                row.textContent.includes(matchID) ||
                row.innerHTML.includes(matchID)
              ) {
                const joinBtn = row.querySelector("button");
                if (joinBtn && joinBtn.textContent.trim() === "Join") {
                  joinBtn.click();
                  joined = true;
                  break;
                }
              }
            }
            if (joined) {
              setStatusMsg(
                `🎉 Đã vào ghế thành công! Hãy bấm nút "Play" ở dòng bàn [${matchID.substring(
                  0,
                  6
                )}] bên dưới để bắt đầu ván đấu ngay!`
              );
            } else {
              setStatusMsg(
                `✅ Đã tạo bàn [${matchID.substring(
                  0,
                  6
                )}] với ${botCount} Bot AI! Hãy bấm nút "Join" ở dòng bàn bên dưới rồi bấm "Play" để chơi nhé!`
              );
            }
          } catch (e) {
            // fallback
          }
        }, 800);
      } else {
        setStatusMsg(`⚠️ Lỗi khi mời bot: ${fillData.error}`);
      }
    } catch (err) {
      setStatusMsg(`❌ Lỗi kết nối: ${err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateHumanRoom = async (numPlayers = 4) => {
    setLoading(true);
    setStatusMsg(`Đang tạo bàn ${numPlayers} người cho bạn bè cùng chơi...`);
    try {
      const createRes = await fetch("/games/tien-len/create", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ numPlayers: numPlayers }),
      });
      const createData = await createRes.json();
      const matchID = createData.gameID;

      setStatusMsg(
        `✅ Đã tạo bàn mới [${matchID.substring(
          0,
          6
        )}] cho 4 người chơi! Đang tự động vào ghế cho bạn...`
      );
      await fetchRooms();

      setTimeout(() => {
        try {
          const rows = document.querySelectorAll("#instances tr");
          let joined = false;
          for (const row of rows) {
            if (
              row.textContent.includes(matchID) ||
              row.innerHTML.includes(matchID)
            ) {
              const joinBtn = row.querySelector("button");
              if (joinBtn && joinBtn.textContent.trim() === "Join") {
                joinBtn.click();
                joined = true;
                break;
              }
            }
          }
          if (joined) {
            setStatusMsg(
              `🎉 Đã vào ghế thành công! Báo bạn bè bấm vào bàn [${matchID.substring(
                0,
                6
              )}] ở sảnh bên dưới để vào chơi cùng bạn nhé!`
            );
          }
        } catch (e) {
          // fallback
        }
      }, 800);
    } catch (err) {
      setStatusMsg(`❌ Lỗi kết nối: ${err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleFreeSeat = async (gameId, playerId) => {
    setLoading(true);
    try {
      const res = await fetch(`/api/rooms/${gameId}/free-seat/${playerId}`, {
        method: "POST",
      });
      const data = await res.json();
      if (data.success) {
        setStatusMsg(
          `✅ Đã giải phóng ghế ${parseInt(playerId) + 1} ở bàn [${gameId.substring(
            0,
            6
          )}]! Ghế này đã mở lại cho người khác vào.`
        );
        fetchRooms();
      } else {
        setStatusMsg(`⚠️ ${data.error}`);
      }
    } catch (err) {
      setStatusMsg(`❌ Lỗi: ${err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleFillBots = async (gameId) => {
    setLoading(true);
    setStatusMsg(`Đang lấp đầy Bot vào bàn ${gameId.substring(0, 6)}...`);
    try {
      const res = await fetch(`/api/bot/fill/${gameId}?leaveHuman=true`, {
        method: "POST",
      });
      const data = await res.json();
      if (data.success) {
        setStatusMsg(
          `✅ Đã thêm ${
            data.result ? data.result.filledCount : ""
          } Bot vào bàn ${gameId.substring(0, 6)}! Bấm Play để quất thôi!`
        );
        fetchRooms();
      } else {
        setStatusMsg(`⚠️ ${data.error}`);
      }
    } catch (err) {
      setStatusMsg(`❌ Lỗi: ${err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteRoom = async (gameId) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa bàn [${gameId.substring(0, 6)}]?`))
      return;
    setLoading(true);
    try {
      const res = await fetch(`/api/rooms/${gameId}`, { method: "DELETE" });
      const data = await res.json();
      if (data.success) {
        setStatusMsg(`🗑️ Đã xóa bàn [${gameId.substring(0, 6)}] thành công!`);
        fetchRooms();
      } else {
        setStatusMsg(`⚠️ ${data.error}`);
      }
    } catch (err) {
      setStatusMsg(`❌ Lỗi: ${err.message}`);
    } finally {
      setLoading(false);
    }
  };

  const currentOrigin =
    window.location.origin || `${protocol}//${hostname}${portPart}`;
  const isLocalhost =
    hostname === "localhost" || hostname === "127.0.0.1";
  const shareUrl =
    isLocalhost && networkIp
      ? `${protocol}//${networkIp}${portPart || ":8000"}`
      : currentOrigin;

  const handleCopyShareUrl = () => {
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(shareUrl);
      setCopiedLink(true);
      setTimeout(() => setCopiedLink(false), 2000);
    }
  };

  return (
    <div className="bot-panel">
      <div className="bot-panel-header">
        <h3>🤖 Quản Lý Bàn Chơi & Trợ Lý Bot AI</h3>
      </div>
      <div className="bot-panel-body">
        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            flexWrap: "wrap",
            gap: "8px",
            padding: "8px 14px",
            marginBottom: "14px",
            borderRadius: "12px",
            background: "rgba(16, 185, 129, 0.12)",
            border: "1px solid rgba(52, 211, 153, 0.35)",
            fontSize: "13px",
            color: "#d1fae5",
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
            <span style={{ fontSize: "16px" }}>📶</span>
            <span>
              <strong>Mạng LAN / Wi-Fi:</strong> Mời bạn bè cùng Wi-Fi vào link:{" "}
              <strong style={{ color: "#6ee7b7", textDecoration: "underline" }}>
                {shareUrl}
              </strong>
            </span>
          </div>
          <button
            type="button"
            onClick={handleCopyShareUrl}
            style={{
              padding: "4px 10px",
              fontSize: "12px",
              fontWeight: 700,
              borderRadius: "8px",
              background: "#059669",
              color: "#fff",
              border: "none",
              cursor: "pointer",
            }}
          >
            {copiedLink ? "✓ Đã copy link" : "📋 Copy link"}
          </button>
        </div>
        <div
          style={{
            display: "flex",
            gap: "10px",
            flexWrap: "wrap",
            marginBottom: "12px",
          }}
        >
          <button
            className="bot-btn-primary"
            onClick={() => handleCreateBotRoom(2)}
            disabled={loading}
          >
            ⚡ Tạo Bàn Solo (1 Bạn vs 1 Bot AI)
          </button>
          <button
            className="bot-btn-primary"
            style={{
              background: "linear-gradient(135deg, #10b981 0%, #059669 100%)",
              border: "1px solid #6ee7b7",
            }}
            onClick={() => handleCreateBotRoom(4)}
            disabled={loading}
          >
            🌟 Tạo Bàn 4 Người (1 Bạn vs 3 Bot AI)
          </button>
          <button
            className="bot-btn-primary"
            style={{
              background: "linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)",
              border: "1px solid #60a5fa",
            }}
            onClick={() => handleCreateHumanRoom(4)}
            disabled={loading}
          >
            👥 Tạo Bàn 4 Người Thật (Cho Bạn Bè)
          </button>
        </div>

        {activeRooms.length > 0 && (
          <div className="bot-existing-rooms" style={{ marginTop: "10px" }}>
            <div
              style={{
                fontWeight: 600,
                marginBottom: "8px",
                fontSize: "14px",
                color: "#ffd700",
              }}
            >
              Danh sách bàn hiện có ({activeRooms.length}):
            </div>
            <div
              style={{ display: "flex", flexDirection: "column", gap: "8px" }}
            >
              {activeRooms.map((r) => {
                const emptySlots = (r.players || []).filter(
                  (p) => p.name === undefined || p.name === null
                ).length;
                return (
                  <div
                    key={r.gameID}
                    style={{
                      display: "flex",
                      flexDirection: "column",
                      background: "rgba(255,255,255,0.06)",
                      padding: "8px 12px",
                      borderRadius: "10px",
                      fontSize: "13px",
                      border: "1px solid rgba(255, 255, 255, 0.1)",
                    }}
                  >
                    <div
                      style={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                        flexWrap: "wrap",
                        gap: "6px",
                      }}
                    >
                      <span>
                        🎲 Bàn <strong>[{r.gameID.substring(0, 6)}]</strong> (
                        {r.players.length} người) - Trống: {emptySlots} ghế
                      </span>
                      <div style={{ display: "flex", gap: "6px" }}>
                        {emptySlots > 0 && (
                          <button
                            className="bot-btn-secondary"
                            style={{ padding: "3px 8px", fontSize: "12px" }}
                            onClick={() => handleFillBots(r.gameID)}
                            disabled={loading}
                          >
                            ➕ Thêm Bot
                          </button>
                        )}
                        <button
                          style={{
                            background: "rgba(239, 68, 68, 0.2)",
                            border: "1px solid #ef4444",
                            color: "#fca5a5",
                            padding: "3px 8px",
                            borderRadius: "6px",
                            cursor: "pointer",
                            fontSize: "12px",
                          }}
                          onClick={() => handleDeleteRoom(r.gameID)}
                          disabled={loading}
                          title="Xóa bàn này khỏi danh sách"
                        >
                          🗑️ Xóa Bàn
                        </button>
                      </div>
                    </div>

                    <div
                      style={{
                        display: "flex",
                        flexWrap: "wrap",
                        gap: "6px",
                        marginTop: "6px",
                        paddingTop: "6px",
                        borderTop: "1px solid rgba(255,255,255,0.08)",
                      }}
                    >
                      {r.players.map((p) => {
                        const isOccupied =
                          p.name !== undefined && p.name !== null;
                        return (
                          <span
                            key={p.id}
                            style={{
                              background: isOccupied
                                ? "rgba(255, 215, 0, 0.12)"
                                : "rgba(255, 255, 255, 0.05)",
                              border: isOccupied
                                ? "1px solid rgba(255, 215, 0, 0.3)"
                                : "1px dashed rgba(255, 255, 255, 0.18)",
                              padding: "2px 8px",
                              borderRadius: "6px",
                              fontSize: "12px",
                              color: isOccupied ? "#ffd700" : "#94a3b8",
                              display: "inline-flex",
                              alignItems: "center",
                              gap: "4px",
                            }}
                          >
                            Ghế {p.id + 1}: {isOccupied ? p.name : "[Trống]"}
                            {isOccupied && (
                              <button
                                type="button"
                                style={{
                                  background: "rgba(239, 68, 68, 0.3)",
                                  border: "1px solid #ef4444",
                                  color: "#ff8888",
                                  cursor: "pointer",
                                  borderRadius: "4px",
                                  padding: "0 4px",
                                  fontSize: "10px",
                                  lineHeight: "1.2",
                                }}
                                onClick={() => handleFreeSeat(r.gameID, p.id)}
                                title="Giải phóng ghế này nếu bị kẹt tên cũ hoặc người chơi đã thoát"
                              >
                                ✕ Hủy
                              </button>
                            )}
                          </span>
                        );
                      })}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        )}

        {statusMsg && (
          <div className="bot-status-msg" style={{ marginTop: "10px" }}>
            {statusMsg}
          </div>
        )}
      </div>
    </div>
  );
}

function LobbyView() {
  return (
    <div className="casino-lobby-container">
      <Router>
        <div className="casino-navbar">
          <div className="casino-logo">
            🃏 TIẾN LÊN MIỀN NAM <span>CASINO EDITION</span>
          </div>
          <div className="nav-links">
            <button className="nav-btn">
              <Link to="/">Sảnh Chờ (Lobby)</Link>
            </button>
            <button className="nav-btn">
              <Link to="/rules">Luật Chơi (Rules)</Link>
            </button>
          </div>
        </div>

        <Switch>
          <Route exact path="/">
            <div className="lobby-content-wrapper">
              <QuickBotHelper />
              <div className="boardgame-lobby-wrapper">
                <Lobby
                  gameServer={gameServer}
                  lobbyServer={lobbyServer}
                  gameComponents={importedGames}
                  clientFactory={RematchAwareClientFactory}
                />
              </div>
            </div>
          </Route>
          <Route path="/rules">
            <Rules />
          </Route>
        </Switch>
      </Router>
    </div>
  );
}

export default LobbyView;
