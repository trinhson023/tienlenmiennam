import React, { useEffect, useRef, useState } from "react";
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

function storedNumber(key, fallback) {
  try {
    const value = window.localStorage.getItem(key);
    return value === null ? fallback : Number(value);
  } catch (e) {
    return fallback;
  }
}

function storedBoolean(key, fallback) {
  try {
    const value = window.sessionStorage.getItem(key);
    return value === null ? fallback : value === "1";
  } catch (e) {
    return fallback;
  }
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
  const currentVideoId = room.current && room.current.videoId
    ? room.current.videoId
    : null;

  const [open, setOpen] = useState(false);
  const [unlocked, setUnlocked] = useState(() =>
    storedBoolean("tienlen.music.unlocked", false)
  );
  const [query, setQuery] = useState("");
  const [results, setResults] = useState([]);
  const [searching, setSearching] = useState(false);
  const [searchError, setSearchError] = useState("");
  const [volume, setVolume] = useState(() =>
    Math.max(0, Math.min(100, storedNumber("tienlen.music.volume", 55)))
  );
  const [muted, setMuted] = useState(false);
  const [playerReady, setPlayerReady] = useState(false);
  const [playerError, setPlayerError] = useState("");
  const [localPosition, setLocalPosition] = useState(0);

  const playerRef = useRef(null);
  const playerNodeRef = useRef(null);
  const syncTimerRef = useRef(null);
  const duckTimerRef = useRef(null);
  const lastLoadedVideoRef = useRef(null);

  const expectedPositionNow = () => {
    if (!currentVideoId) return 0;
    if (room.playing && room.startedAt) {
      return Math.max(0, (Date.now() - Number(room.startedAt)) / 1000);
    }
    return Math.max(0, Number(room.position) || 0);
  };

  const rememberUnlocked = value => {
    setUnlocked(value);
    try {
      window.sessionStorage.setItem("tienlen.music.unlocked", value ? "1" : "0");
    } catch (e) {
      return;
    }
  };

  useEffect(() => {
    if (window.YT && window.YT.Player) {
      setPlayerReady(true);
      return undefined;
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
    if (!playerReady || !playerNodeRef.current || playerRef.current) return undefined;

    playerRef.current = new window.YT.Player(playerNodeRef.current, {
      width: "320",
      height: "200",
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
        onError: () =>
          setPlayerError("Video này không phát được trong trình nhúng."),
        onAutoplayBlocked: () => {
          rememberUnlocked(false);
          setPlayerError(
            "Trình duyệt đã chặn autoplay. Bấm “Bật nhạc bàn” một lần."
          );
        },
      },
    });

    return () => {
      if (playerRef.current && playerRef.current.destroy) {
        try {
          playerRef.current.destroy();
        } catch (e) {
          // no-op
        }
      }
      playerRef.current = null;
      lastLoadedVideoRef.current = null;
    };
  }, [playerReady]);

  /*
   * IMPORTANT: this effect only depends on primitive MUSIC playback state.
   * boardgame.io recreates G objects after normal card moves; depending on room/current
   * object identity caused the old player sync effect to run again during gameplay and
   * could reload/seek the YouTube iframe. Card moves must not touch playback.
   */
  useEffect(() => {
    const player = playerRef.current;
    if (!player || !currentVideoId || !unlocked) return;

    setPlayerError("");
    const target = expectedPositionNow();

    try {
      if (lastLoadedVideoRef.current !== currentVideoId) {
        if (room.playing && player.loadVideoById) {
          player.loadVideoById({
            videoId: currentVideoId,
            startSeconds: target,
          });
        } else if (player.cueVideoById) {
          player.cueVideoById({
            videoId: currentVideoId,
            startSeconds: target,
          });
        }
        lastLoadedVideoRef.current = currentVideoId;
        return;
      }

      const pos = player.getCurrentTime ? player.getCurrentTime() : target;
      if (Math.abs(pos - target) > 3.5 && player.seekTo) {
        player.seekTo(target, true);
      }

      if (room.playing && player.playVideo) player.playVideo();
      if (!room.playing && player.pauseVideo) player.pauseVideo();
    } catch (e) {
      setPlayerError("Không thể đồng bộ player.");
    }
  }, [
    playerReady,
    unlocked,
    currentVideoId,
    room.playing,
    room.position,
    room.startedAt,
  ]);

  useEffect(() => {
    const player = playerRef.current;
    if (!player) return;
    try {
      player.setVolume(volume);
      if (muted) player.mute();
      else player.unMute();
      window.localStorage.setItem("tienlen.music.volume", String(volume));
    } catch (e) {
      return;
    }
  }, [volume, muted]);

  useEffect(() => {
    const handleDuck = event => {
      const player = playerRef.current;
      if (!player || !unlocked || muted) return;
      const duration =
        event && event.detail && event.detail.duration
          ? Number(event.detail.duration)
          : 900;
      try {
        clearTimeout(duckTimerRef.current);
        player.setVolume(Math.max(8, Math.round(volume * 0.35)));
        duckTimerRef.current = setTimeout(() => {
          try {
            if (playerRef.current) playerRef.current.setVolume(volume);
          } catch (e) {
            return;
          }
        }, duration);
      } catch (e) {
        return;
      }
    };

    window.addEventListener("tienlen:duck-music", handleDuck);
    return () => {
      window.removeEventListener("tienlen:duck-music", handleDuck);
      clearTimeout(duckTimerRef.current);
    };
  }, [volume, muted, unlocked]);

  useEffect(() => {
    clearInterval(syncTimerRef.current);
    syncTimerRef.current = setInterval(() => {
      const player = playerRef.current;
      if (player && player.getCurrentTime && unlocked) {
        try {
          setLocalPosition(player.getCurrentTime());
          return;
        } catch (e) {
          // fall through
        }
      }
      setLocalPosition(expectedPositionNow());
    }, 1000);

    return () => clearInterval(syncTimerRef.current);
  }, [unlocked, currentVideoId, room.playing, room.position, room.startedAt]);

  const enableAudio = () => {
    rememberUnlocked(true);
    setPlayerError("");
    const player = playerRef.current;
    if (!player || !currentVideoId) return;

    try {
      const target = expectedPositionNow();
      player.loadVideoById({
        videoId: currentVideoId,
        startSeconds: target,
      });
      lastLoadedVideoRef.current = currentVideoId;
      player.setVolume(volume);
      if (muted) player.mute();
      if (!room.playing) player.pauseVideo();
    } catch (e) {
      setPlayerError("Không thể bật nhạc. Thử đóng/mở Music Room rồi bấm lại.");
    }
  };

  const search = async () => {
    const value = query.trim();
    if (!value || !isHost) return;

    const pastedId = extractVideoId(value);
    if (pastedId) {
      rememberUnlocked(true);
      moves.musicSelect &&
        moves.musicSelect({ videoId: pastedId, title: "YouTube video" });
      setResults([]);
      setQuery("");
      return;
    }

    setSearching(true);
    setSearchError("");
    try {
      const res = await fetch(`/api/youtube/search?q=${encodeURIComponent(value)}`);
      const data = await res.json();
      if (!res.ok || !data.success) {
        throw new Error(data.error || "Search failed");
      }
      setResults(data.items || []);
    } catch (err) {
      setSearchError(err.message || "Không tìm được nhạc.");
    } finally {
      setSearching(false);
    }
  };

  const playTrack = item => {
    rememberUnlocked(true);
    moves.musicSelect && moves.musicSelect(item);
  };

  const toggleHostPlayback = () => {
    if (!isHost || !room.current) return;
    rememberUnlocked(true);
    const player = playerRef.current;
    let pos = expectedPositionNow();
    try {
      if (player && player.getCurrentTime) pos = player.getCurrentTime();
    } catch (e) {
      pos = expectedPositionNow();
    }
    moves.musicToggle && moves.musicToggle(!room.playing, pos);
  };

  const seekHost = value => {
    if (!isHost || !room.current) return;
    const position = Math.max(0, Number(value) || 0);
    moves.musicSeek && moves.musicSeek(position);
  };

  let duration = 0;
  try {
    duration =
      playerRef.current && playerRef.current.getDuration
        ? playerRef.current.getDuration()
        : 0;
  } catch (e) {
    duration = 0;
  }

  return (
    <div className={`music-room ${open ? "music-room--open" : ""}`}>
      <button className="music-room__toggle" onClick={() => setOpen(!open)}>
        🎵 {room.current ? room.current.title : "Music Room"}
      </button>

      <div className={`music-room__panel ${open ? "" : "music-room__panel--hidden"}`}>
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
          {room.current && !unlocked && (
            <button className="music-room__unlock" onClick={enableAudio}>
              🎧 Bật nhạc bàn
            </button>
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
                    <button onClick={() => playTrack(item)}>▶</button>
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
    </div>
  );
}

MusicRoom.propTypes = {
  G: PropTypes.object.isRequired,
  playerID: PropTypes.string,
  moves: PropTypes.object.isRequired,
};
