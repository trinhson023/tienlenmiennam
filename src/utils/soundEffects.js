// src/utils/soundEffects.js
// Lightweight Web Audio sound design for Tiến Lên.

let audioCtx = null;
let soundEnabled = true;

if (typeof window !== "undefined") {
  try {
    const saved = window.localStorage.getItem("tienlen:sound-enabled");
    soundEnabled = saved !== "false";
  } catch (e) {
    soundEnabled = true;
  }
}

export function setSoundEnabled(enabled) {
  soundEnabled = Boolean(enabled);
  if (typeof window !== "undefined") {
    try {
      window.localStorage.setItem(
        "tienlen:sound-enabled",
        soundEnabled ? "true" : "false"
      );
    } catch (e) {
      return soundEnabled;
    }
  }
  return soundEnabled;
}

export function getSoundEnabled() {
  return soundEnabled;
}

function getAudioContext() {
  if (!soundEnabled || typeof window === "undefined") return null;
  try {
    if (!audioCtx) {
      const AudioContext = window.AudioContext || window.webkitAudioContext;
      if (AudioContext) audioCtx = new AudioContext();
    }
    if (audioCtx && audioCtx.state === "suspended") {
      audioCtx.resume().catch(() => false);
    }
  } catch (e) {
    return null;
  }
  return audioCtx;
}

function tone(type, from, to, volume, duration, delay = 0) {
  const ctx = getAudioContext();
  if (!ctx) return;
  const start = ctx.currentTime + delay;
  const osc = ctx.createOscillator();
  const gain = ctx.createGain();
  osc.type = type;
  osc.frequency.setValueAtTime(from, start);
  osc.frequency.exponentialRampToValueAtTime(Math.max(1, to), start + duration);
  gain.gain.setValueAtTime(volume, start);
  gain.gain.exponentialRampToValueAtTime(0.001, start + duration);
  osc.connect(gain);
  gain.connect(ctx.destination);
  osc.start(start);
  osc.stop(start + duration + 0.02);
}

export function playCardSound() {
  try {
    tone("triangle", 520, 105, 0.22, 0.07);
    tone("sine", 170, 70, 0.16, 0.11, 0.015);
  } catch (e) {
    return false;
  }
}

export function playChopSound() {
  try {
    tone("triangle", 180, 34, 0.48, 0.58);
    tone("sawtooth", 270, 48, 0.22, 0.42);
    tone("sine", 880, 220, 0.13, 0.16, 0.02);
  } catch (e) {
    return false;
  }
}

export function playVictorySound() {
  try {
    const notes = [523.25, 659.25, 783.99, 1046.5];
    notes.forEach((freq, idx) => {
      tone("triangle", freq, freq, 0.2, idx === 3 ? 0.55 : 0.22, idx * 0.11);
    });
  } catch (e) {
    return false;
  }
}

export function playChatSound() {
  try {
    tone("sine", 590, 880, 0.14, 0.13);
  } catch (e) {
    return false;
  }
}

export function playTurnSound() {
  try {
    tone("sine", 660, 880, 0.1, 0.1);
    tone("sine", 880, 1040, 0.08, 0.12, 0.09);
  } catch (e) {
    return false;
  }
}
