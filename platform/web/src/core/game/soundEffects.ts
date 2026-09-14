let audioCtx: AudioContext | null = null
let soundEnabled = true

if (typeof window !== 'undefined') {
  try { soundEnabled = window.localStorage.getItem('tienlen:sound-enabled') !== 'false' }
  catch { soundEnabled = true }
}

export function getSoundEnabled() { return soundEnabled }

export function setSoundEnabled(enabled: boolean) {
  soundEnabled = Boolean(enabled)
  if (typeof window !== 'undefined') {
    try { window.localStorage.setItem('tienlen:sound-enabled', soundEnabled ? 'true' : 'false') }
    catch { /* storage is optional */ }
  }
  return soundEnabled
}

type WebKitAudioWindow = Window & typeof globalThis & { webkitAudioContext?: typeof AudioContext }

function getAudioContext() {
  if (!soundEnabled || typeof window === 'undefined') return null
  try {
    if (!audioCtx) {
      const webkitWindow = window as WebKitAudioWindow
      const AudioContextCtor = window.AudioContext || webkitWindow.webkitAudioContext
      if (AudioContextCtor) audioCtx = new AudioContextCtor()
    }
    if (audioCtx?.state === 'suspended') void audioCtx.resume().catch(() => undefined)
  } catch { return null }
  return audioCtx
}

function tone(type: OscillatorType, from: number, to: number, volume: number, duration: number, delay = 0) {
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
    tone('triangle', 520, 105, .22, .07)
    tone('sine', 170, 70, .16, .11, .015)
  } catch { /* audio must not affect gameplay */ }
}

export function playChopSound() {
  try {
    tone('triangle', 180, 34, .48, .58)
    tone('sawtooth', 270, 48, .22, .42)
    tone('sine', 880, 220, .13, .16, .02)
  } catch { /* audio must not affect gameplay */ }
}

export function playChatSound() {
  try { tone('sine', 590, 880, .14, .13) }
  catch { /* audio must not affect gameplay */ }
}

export function playTurnSound() {
  try {
    tone('sine', 660, 880, .1, .1)
    tone('sine', 880, 1040, .08, .12, .09)
  } catch { /* audio must not affect gameplay */ }
}

export function playVictorySound() {
  try {
    ;[523.25, 659.25, 783.99, 1046.5].forEach((frequency, index) => {
      tone('triangle', frequency, frequency, .2, index === 3 ? .55 : .22, index * .11)
    })
  } catch { /* audio must not affect gameplay */ }
}
