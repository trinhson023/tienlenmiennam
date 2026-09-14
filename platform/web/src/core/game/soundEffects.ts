// Lightweight Web Audio sound design ported from the stable React client.
let audioCtx: AudioContext | null = null
let soundEnabled = true

if (typeof window !== 'undefined') {
  try {
    soundEnabled = window.localStorage.getItem('tienlen:sound-enabled') !== 'false'
  } catch {
    soundEnabled = true
  }
}

export function getSoundEnabled() {
  return soundEnabled
}

export function setSoundEnabled(enabled: boolean) {
  soundEnabled = Boolean(enabled)
  if (typeof window !== 'undefined') {
    try {
      window.localStorage.setItem('tienlen:sound-enabled', soundEnabled ? 'true' : 'false')
    } catch {
      // Local storage is optional; audio should still work for this session.
    }
  }
  return soundEnabled
}

type WebKitAudioWindow = Window & typeof globalThis & {
  webkitAudioContext?: typeof AudioContext
}

function getAudioContext() {
  if (!soundEnabled || typeof window === 'undefined') return null
  try {
    if (!audioCtx) {
      const webkitWindow = window as WebKitAudioWindow
      const AudioContextCtor = window.AudioContext || webkitWindow.webkitAudioContext
      if (AudioContextCtor) audioCtx = new AudioContextCtor()
    }
    if (audioCtx?.state === 'suspended') void audioCtx.resume().catch(() => undefined)
  } catch {
    return null
  }
  return audioCtx
}

function tone(
  type: OscillatorType,
  from: number,
  to: number,
  volume: number,
  duration: number,
  delay = 0
) {
  const ctx = getAudioContext()
  if (!ctx) return
  const start = ctx.currentTime + delay
  const oscillator = ctx.createOscillator()
  const gain = ctx.createGain()
  oscillator.type = type
  oscillator.frequency.setValueAtTime(from, start)
  oscillator.frequency.exponentialRampToValueAtTime(Math.max(1, to), start + duration)
  gain.gain.setValueAtTime(volume, start)
  gain.gain.exponentialRampToValueAtTime(0.001, start + duration)
  oscillator.connect(gain)
  gain.connect(ctx.destination)
  oscillator.start(start)
  oscillator.stop(start + duration + 0.02)
}

export function playCardSound() {
  try {
    tone('triangle', 520, 105, 0.22, 0.07)
    tone('sine', 170, 70, 0.16, 0.11, 0.015)
  } catch {
    // Audio feedback must never affect gameplay.
  }
}

export function playTurnSound() {
  try {
    tone('sine', 660, 880, 0.1, 0.1)
    tone('sine', 880, 1040, 0.08, 0.12, 0.09)
  } catch {
    // Audio feedback must never affect gameplay.
  }
}

export function playVictorySound() {
  try {
    ;[523.25, 659.25, 783.99, 1046.5].forEach((frequency, index) => {
      tone('triangle', frequency, frequency, 0.2, index === 3 ? 0.55 : 0.22, index * 0.11)
    })
  } catch {
    // Audio feedback must never affect gameplay.
  }
}
