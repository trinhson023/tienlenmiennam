import React, { useEffect, useMemo, useRef, useState } from "react";
import PropTypes from "prop-types";

function extractVideoId(input) {
  const text = String(input || "").trim();
  if (!text) return null;
  const direct = text.match(/^[a-zA-Z0-9_-]{11}$/);
  if (direct) return direct[0];
  const patterns = [
    /youtu\.be\/([a-zA-Z0-9_-]{11})/,
    /youtube\.com\/watch\?[^#]*v=([a-zA-Z0-9_-]{11})/,
    /youtube\.com\/shorts\/([a-zA-Z0-9_-]{11})/,
    /youtube\.com\/embed\/([a-zA-Z0-9_-]{11})/,
  ];
  for (const pattern of patterns) {
    const match = text.match(pattern);
    if (match) return match[1];
  }
  return null;
}

function formatSeconds(value) {
  const seconds = Math.max(0, Math.floor(Number(value) || 0));
  const m = Math.floor(seconds / 60);
  const s = String(seconds % 60).padStart(2, "0");
  return `${m}:${s}`;
}

export default function MusicRoom({ G, playerID, moves }) {
  const room = G.musicRoom || {
    current: null,
    queue: [],
    playing: false,
    position: 0,
    startedAt: null,
    revision: 0,
  };
  const isHost = String(playerID) === "0";
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [results, setResults] = useState([]);
  const [searching, setSearching] = useState(false);
  const [searchError, setSearchError] = useState("");
  const [volume, setVolume] = useState(55);
  const [muted, setMuted] = useState(false);
  const [playerReady, setPlayerReady] = useState(false);
  const [playerError, setPlayerError] = useState("");
  const [localPosition, setLocalPosition] = useState(0);
  const playerRef = useRef(null);
  const playerNodeRef = useRef(null);
  const syncTimerRef = useRef(null);

  const expectedPosition = useMemo(() => {
    if (!room.current) return 0;
    if (room.playing && room.startedAt) {
      return Math.max(0, (Date.now() - room.startedAt) / 1000);
    }
    return Math.max(0, Number(room.position) || 0);
  }, [room.current, room.playing, room.position, room.startedAt, room.revision]);

  useEffect(() => {
    if (window.YT && window.YT.Player) {
      setPlayerReady(true);
      return;
    }
    const existing = document.getElementById("youtube-iframe-api");
    if (!existing) {
      const tag = document.createElement("script");
      tag.id = "youtube-iframe-api";
      tag.src = "https://www.youtube.com/iframe_api";
      document.body.appendChild(tag);
    }
    const previous = window.onYouTubeIframeAPIReady;
    window.onYouTubeIframeAPIReady = () => {
      if (typeof previous === "function") previous();
      setPlayerReady(true);
    };
    const timer = setInterval(() => {
      if (window.YT && window.YT.Player) {
        clearInterval(timer);
        setPlayerReady(true);
      }
    }, 250);
    return () => clearInterval(timer);
  }, []);

  useEffect(() => {
    if (!playerReady || !playerNodeRef.current || playerRef.current) return;
    playerRef.current = new window.YT.Player(playerNodeRef.current, {
      width: "320",
      height: "180",
      playerVars: {
        playsinline: 1,
        rel: 0,
        modestbranding: 1,
      },
      events: {
        onReady: event => {
          event.target.setVolume(volume);
          if (muted) event.target.mute();
        },
        onError: () => setPlayerError("Video này không phát được trong trình nhúng."),
      },
    });
    return () => {
      if (playerRef.current && playerRef.current.destroy) {
        playerRef.current.destroy();
      }
      playerRef.current = null;
    };
  }, [playerReady]);

  useEffect(() => {
    const player = playerRef.current;
    if (!player || !room.current || !room.current.videoId) return;
    setPlayerError("");
    try {
      const currentId = player.getVideoData && player.getVideoData().video_id;
      if (currentId !== room.current.videoId) {
        player.loadVideoById({
          videoId: room.current.videoId,
          startSeconds: expectedPosition,
        });
        if (!room.playing) player.pauseVideo();
      } else {
        const pos = player.getCurrentTime ? player.getCurrentTime() : 0;
        if (Math.abs(pos - expectedPosition) > 2.5 && player.seekTo) {
          player.seekTo(expectedPosition, true);
        }
        if (room.playing && player.playVideo) player.playVideo();
        if (!room.playing && player.pauseVideo) player.pauseVideo();
      }
    } catch (e) {
      setPlayerError("Không thể đồng bộ player.");
    }
  }, [room.current, room.playing, room.revision, expectedPosition]);

  useEffect(() => {
    const player = playerRef.current;
    if (!player) return;
    try {
      player.setVolume(volume);
      muted ? player.mute() : player.unMute();
    } catch (e) {
      return;
    }
  }, [volume, muted]);

  useEffect(() => {
    clearInterval(syncTimerRef.current);
    syncTimerRef.current = setInterval(() => {
      const player = playerRef.current;
      if (player && player.getCurrentTime) {
        try {
          setLocalPosition(player.getCurrentTime());
        } catch (e) {
          setLocalPosition(expectedPosition);
        }
      } else {
        setLocalPosition(expectedPosition);
      }
    }, 1000);
    return () => clearInterval(syncTimerRef.current);
  }, [expectedPosition]);

  const search = async () => {
    const value = query.trim();
    if (!value || !isHost) return;
    const pastedId = extractVideoId(value);
    if (pastedId) {
      moves.musicSelect && moves.musicSelect({ videoId: pastedId, title: "YouTube video" });
      setResults([]);
      setQuery("");
      return;
    }
    setSearching(true);
    setSearchError("");
    try {
      const res = await fetch(`/api/youtube/search?q=${encodeURIComponent(value)}`);
      const data = await res.json();
      if (!res.ok || !data.success) throw new Error(data.error || "Search failed");
      setResults(data.items || []);
    } catch (err) {
      setSearchError(err.message || "Không tìm được nhạc.");
    } finally {
      setSearching(false);
    }
  };

  const toggleHostPlayback = () => {
    if (!isHost || !room.current) return;
    const player = playerRef.current;
    let pos = expectedPosition;
    try {
      if (player && player.getCurrentTime) pos = player.getCurrentTime();
    } catch (e) {
      pos = expectedPosition;
    }
    moves.musicToggle && moves.musicToggle(!room.playing, pos);
  };

  const seekHost = value => {
    if (!isHost || !room.current) return;
    const position = Math.max(0, Number(value) || 0);
    moves.musicSeek && moves.musicSeek(position);
  };

  const duration = (() => {
    try {
      return playerRef.current && playerRef.current.getDuration
        ? playerRef.current.getDuration()
        : 0;
    } catch (e) {
      return 0;
    }
  })();

  return (
    <div className={`music-room ${open ? "music-room--open" : ""}`}>
      <button className="music-room__toggle" onClick={() => setOpen(!open)}>
        🎵 {room.current ? room.current.title : "Music Room"}
      </button>
      {open && (
        <div className="music-room__panel">
          <div className="music-room__header">
            <div>
              <span>TABLE MUSIC</span>
              <strong>{isHost ? "Bạn là DJ" : "DJ: Chủ bàn"}</strong>
            </div>
            <button onClick={() => setOpen(false)}>×</button>
          </div>

          <div className="music-room__player-wrap">
            <div className="music-room__player" ref={playerNodeRef} />
            {!room.current && (
              <div className="music-room__empty">Chủ bàn chưa chọn nhạc</div>
            )}
          </div>

          {room.current && (
            <div className="music-room__now">
              <strong>{room.current.title}</strong>
              <span>{room.current.channelTitle || "YouTube"}</span>
            </div>
          )}

          <div className="music-room__controls">
            <button onClick={toggleHostPlayback} disabled={!isHost || !room.current}>
              {room.playing ? "⏸" : "▶"}
            </button>
            <button
              onClick={() => moves.musicNext && moves.musicNext()}
              disabled={!isHost || !room.current}
            >
              ⏭
            </button>
            <button onClick={() => setMuted(!muted)}>{muted ? "🔇" : "🔊"}</button>
            <input
              type="range"
              min="0"
              max="100"
              value={volume}
              onChange={e => setVolume(Number(e.target.value))}
            />
          </div>

          <div className="music-room__timeline">
            <span>{formatSeconds(localPosition)}</span>
            <input
              type="range"
              min="0"
              max={Math.max(1, duration)}
              value={Math.min(localPosition, Math.max(1, duration))}
              disabled={!isHost || !room.current || duration <= 0}
              onChange={e => seekHost(e.target.value)}
            />
            <span>{duration > 0 ? formatSeconds(duration) : "--:--"}</span>
          </div>

          {isHost && (
            <div className="music-room__search">
              <div className="music-room__searchbar">
                <input
                  value={query}
                  onChange={e => setQuery(e.target.value)}
                  onKeyDown={e => {
                    if (e.key === "Enter") search();
                  }}
                  placeholder="Tìm YouTube hoặc dán link..."
                />
                <button onClick={search} disabled={searching}>
                  {searching ? "..." : "Tìm"}
                </button>
              </div>
              {searchError && <div className="music-room__error">{searchError}</div>}
              <div className="music-room__results">
                {results.map(item => (
                  <div className="music-room__result" key={item.videoId}>
                    {item.thumbnail && <img src={item.thumbnail} alt="" />}
                    <div>
                      <strong>{item.title}</strong>
                      <span>{item.channelTitle}</span>
                    </div>
                    <div className="music-room__result-actions">
                      <button onClick={() => moves.musicSelect && moves.musicSelect(item)}>
                        ▶
                      </button>
                      <button onClick={() => moves.musicQueue && moves.musicQueue(item)}>
                        +
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          <div className="music-room__queue">
            <div className="music-room__queue-title">
              <strong>Queue ({room.queue.length})</strong>
              {isHost && room.queue.length > 0 && (
                <button onClick={() => moves.musicClearQueue && moves.musicClearQueue()}>
                  Xóa queue
                </button>
              )}
            </div>
            {room.queue.slice(0, 5).map((item, index) => (
              <div className="music-room__queue-item" key={`${item.videoId}-${index}`}>
                <span>{index + 1}</span>
                <div>
                  <strong>{item.title}</strong>
                  <small>{item.channelTitle}</small>
                </div>
              </div>
            ))}
          </div>

          {playerError && <div className="music-room__error">{playerError}</div>}
          <div className="music-room__hint">
            Volume là riêng từng máy; Play / Pause / Next / Seek do chủ bàn điều khiển.
          </div>
        </div>
      )}
    </div>
  );
}

MusicRoom.propTypes = {
  G: PropTypes.object.isRequired,
  playerID: PropTypes.string,
  moves: PropTypes.object.isRequired,
};
