import React, { useEffect, useRef, useState } from "react";

function loadSavedFeed() {
  try {
    const raw = window.sessionStorage.getItem("tienlen.shorts.feed");
    return raw ? JSON.parse(raw) : [];
  } catch (e) {
    return [];
  }
}

function loadSavedIndex() {
  try {
    return Number(window.sessionStorage.getItem("tienlen.shorts.index")) || 0;
  } catch (e) {
    return 0;
  }
}

export default function ShortsLounge() {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState(() => {
    try {
      return window.sessionStorage.getItem("tienlen.shorts.query") || "";
    } catch (e) {
      return "";
    }
  });
  const [feed, setFeed] = useState(loadSavedFeed);
  const [index, setIndex] = useState(loadSavedIndex);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [muted, setMuted] = useState(true);
  const [playerReady, setPlayerReady] = useState(false);
  const playerRef = useRef(null);
  const nodeRef = useRef(null);
  const wheelLockRef = useRef(false);

  const current = feed[index] || null;

  useEffect(() => {
    if (window.YT && window.YT.Player) {
      setPlayerReady(true);
      return undefined;
    }

    if (!document.getElementById("youtube-iframe-api")) {
      const tag = document.createElement("script");
      tag.id = "youtube-iframe-api";
      tag.src = "https://www.youtube.com/iframe_api";
      document.body.appendChild(tag);
    }

    const timer = setInterval(() => {
      if (window.YT && window.YT.Player) {
        clearInterval(timer);
        setPlayerReady(true);
      }
    }, 250);
    return () => clearInterval(timer);
  }, []);

  useEffect(() => {
    if (!playerReady || !nodeRef.current || playerRef.current) return undefined;
    playerRef.current = new window.YT.Player(nodeRef.current, {
      width: "100%",
      height: "100%",
      playerVars: {
        playsinline: 1,
        rel: 0,
        controls: 1,
        modestbranding: 1,
      },
      events: {
        onReady: event => {
          event.target.mute();
        },
        onStateChange: event => {
          if (window.YT && event.data === window.YT.PlayerState.ENDED) {
            next();
          }
        },
      },
    });

    return () => {
      try {
        if (playerRef.current && playerRef.current.destroy) {
          playerRef.current.destroy();
        }
      } catch (e) {
        // no-op
      }
      playerRef.current = null;
    };
  }, [playerReady]);

  useEffect(() => {
    if (!playerRef.current || !current || !current.videoId) return;
    try {
      playerRef.current.loadVideoById(current.videoId);
      if (muted) playerRef.current.mute();
      else playerRef.current.unMute();
    } catch (e) {
      // player may still be initializing
    }
  }, [current && current.videoId]);

  useEffect(() => {
    if (!playerRef.current) return;
    try {
      if (muted) playerRef.current.mute();
      else playerRef.current.unMute();
    } catch (e) {
      // no-op
    }
  }, [muted]);

  useEffect(() => {
    try {
      window.sessionStorage.setItem("tienlen.shorts.feed", JSON.stringify(feed));
      window.sessionStorage.setItem("tienlen.shorts.index", String(index));
      window.sessionStorage.setItem("tienlen.shorts.query", query);
    } catch (e) {
      // no-op
    }
  }, [feed, index, query]);

  const search = async () => {
    const value = query.trim();
    if (!value || loading) return;
    setLoading(true);
    setError("");
    try {
      const response = await fetch(`/api/youtube/shorts?q=${encodeURIComponent(value)}`);
      const data = await response.json();
      if (!response.ok || !data.success) {
        throw new Error(data.error || "Không tìm được Shorts.");
      }
      const items = data.items || [];
      setFeed(items);
      setIndex(0);
      setOpen(true);
    } catch (err) {
      setError(err.message || "Không tìm được Shorts.");
    } finally {
      setLoading(false);
    }
  };

  const next = () => {
    if (feed.length === 0) return;
    setIndex(currentIndex => (currentIndex + 1) % feed.length);
  };

  const previous = () => {
    if (feed.length === 0) return;
    setIndex(currentIndex => (currentIndex - 1 + feed.length) % feed.length);
  };

  const onWheel = event => {
    if (!open || feed.length < 2 || wheelLockRef.current) return;
    if (Math.abs(event.deltaY) < 20) return;
    wheelLockRef.current = true;
    if (event.deltaY > 0) next();
    else previous();
    setTimeout(() => {
      wheelLockRef.current = false;
    }, 450);
  };

  return (
    <div className={`shorts-lounge ${open ? "shorts-lounge--open" : ""}`}>
      <button className="shorts-lounge__toggle" onClick={() => setOpen(!open)}>
        📱 Shorts Lounge
      </button>

      <div className={`shorts-lounge__panel ${open ? "" : "shorts-lounge__panel--hidden"}`}>
        <div className="shorts-lounge__header">
          <div>
            <span>SHORT FEED</span>
            <strong>{current ? `${index + 1}/${feed.length}` : "Tìm chủ đề để xem"}</strong>
          </div>
          <button onClick={() => setOpen(false)}>×</button>
        </div>

        <div className="shorts-lounge__viewport" onWheel={onWheel}>
          <div className="shorts-lounge__player" ref={nodeRef} />
          {!current && (
            <div className="shorts-lounge__empty">
              Search một chủ đề như “meme việt”, “football funny”, “remix” rồi lướt liên tục.
            </div>
          )}
        </div>

        {current && (
          <div className="shorts-lounge__meta">
            <strong>{current.title}</strong>
            <span>{current.channelTitle}</span>
          </div>
        )}

        <div className="shorts-lounge__controls">
          <button onClick={previous} disabled={feed.length < 2}>↑ Trước</button>
          <button onClick={() => setMuted(!muted)}>{muted ? "🔇 Mở tiếng" : "🔊 Tắt tiếng"}</button>
          <button onClick={next} disabled={feed.length < 2}>Tiếp ↓</button>
        </div>

        <div className="shorts-lounge__search">
          <input
            value={query}
            onChange={event => setQuery(event.target.value)}
            onKeyDown={event => {
              if (event.key === "Enter") search();
            }}
            placeholder="meme việt, remix, football..."
          />
          <button onClick={search} disabled={loading}>
            {loading ? "..." : "Tìm"}
          </button>
        </div>

        {error && <div className="shorts-lounge__error">{error}</div>}
        <div className="shorts-lounge__hint">
          Cuộn chuột lên/xuống để đổi clip. Hết video sẽ tự sang clip tiếp theo.
        </div>
      </div>
    </div>
  );
}
