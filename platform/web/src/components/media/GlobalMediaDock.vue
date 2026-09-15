<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useLobbyStore } from '@/stores/lobby'
import { useMediaStore, type MediaTrack } from '@/stores/media'
import { useTienLenStore } from '@/stores/tienlen'

const auth = useAuthStore()
const lobby = useLobbyStore()
const game = useTienLenStore()
const media = useMediaStore()

const musicOpen = ref(false)
const shortsOpen = ref(false)
const query = ref('')
const results = ref<MediaTrack[]>([])
const searching = ref(false)
const mediaError = ref('')
const shortQuery = ref('')
const shortFeed = ref<MediaTrack[]>([])
const shortIndex = ref(0)
const shorting = ref(false)
const shortError = ref('')
const playerHost = ref<HTMLElement | null>(null)
const player = ref<any>(null)
const lastVideo = ref<string | null>(null)
const localPosition = ref(0)
const durationSeconds = ref(0)
const volume = ref(loadNumber('royal.music.volume', 55, 0, 100))
const muted = ref(false)
const unlocked = ref(loadBoolean('royal.music.unlocked', false))
const shortMuted = ref(loadBoolean('royal.shorts.muted', true))
let positionTimer: number | null = null
let shortWheelLocked = false

const gameRoomId = computed(() => game.match?.roomId || null)
const roomId = computed(() => auth.isAuthenticated ? (gameRoomId.value || lobby.currentRoom?.id || null) : null)
const isHost = computed(() => !!roomId.value && lobby.currentRoom?.id === roomId.value && lobby.currentRoom.hostUserId === auth.user?.id)
const currentShort = computed(() => shortFeed.value[shortIndex.value] || null)
const shortSrc = computed(() => currentShort.value
  ? `https://www.youtube.com/embed/${currentShort.value.videoId}?autoplay=1&playsinline=1&rel=0&mute=${shortMuted.value ? 1 : 0}`
  : '')

function loadNumber(key: string, fallback: number, min: number, max: number) {
  try {
    const raw = Number(window.localStorage.getItem(key))
    return Number.isFinite(raw) ? Math.max(min, Math.min(max, raw)) : fallback
  } catch { return fallback }
}

function loadBoolean(key: string, fallback: boolean) {
  try {
    const raw = window.sessionStorage.getItem(key)
    return raw === null ? fallback : raw === '1'
  } catch { return fallback }
}

function errorText(error: unknown, fallback: string) {
  const payload = error as { response?: { data?: { message?: string } }; message?: string }
  return payload?.response?.data?.message || payload?.message || fallback
}

function formatSeconds(value: number) {
  const seconds = Math.max(0, Math.floor(Number.isFinite(value) ? value : 0))
  return `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, '0')}`
}

function expected() {
  const state = media.state
  if (!state?.current) return 0
  if (state.playing && state.startedAtUtc) return Math.max(0, (Date.now() - new Date(state.startedAtUtc).getTime()) / 1000)
  return Math.max(0, state.positionSeconds || 0)
}

async function ensureLobbyContext(id: string) {
  if (lobby.currentRoom?.id === id) return
  try {
    await lobby.loadRoom(id)
    await lobby.subscribeRoom(id)
  } catch {
    // Media remains optional. Gameplay authorization is still handled by Tiến Lên service.
  }
}

async function loadYT() {
  const win = window as any
  if (win.YT?.Player) return
  if (!document.getElementById('youtube-iframe-api')) {
    const tag = document.createElement('script')
    tag.id = 'youtube-iframe-api'
    tag.src = 'https://www.youtube.com/iframe_api'
    document.body.appendChild(tag)
  }
  await new Promise<void>((resolve, reject) => {
    let attempts = 0
    const timer = window.setInterval(() => {
      attempts += 1
      if ((window as any).YT?.Player) {
        window.clearInterval(timer)
        resolve()
      } else if (attempts >= 50) {
        window.clearInterval(timer)
        reject(new Error('YouTube player không tải được.'))
      }
    }, 200)
  })
}

function destroyPlayer() {
  try { player.value?.destroy?.() } catch { /* optional */ }
  player.value = null
  lastVideo.value = null
  localPosition.value = 0
  durationSeconds.value = 0
}

async function initPlayer() {
  if (player.value || !playerHost.value) return
  try {
    await loadYT()
    const YT = (window as any).YT
    player.value = new YT.Player(playerHost.value, {
      width: '100%',
      height: '190',
      playerVars: { playsinline: 1, rel: 0, modestbranding: 1 },
      events: {
        onReady: (event: any) => {
          event.target.setVolume(volume.value)
          if (muted.value) event.target.mute()
        },
        onError: () => { mediaError.value = 'Video này không phát được trong trình nhúng.' }
      }
    })
  } catch (error) {
    mediaError.value = errorText(error, 'Không tải được YouTube player.')
  }
}

async function syncPlayer() {
  // Do not create an iframe before the user first opens the panel, but once a
  // player exists keep it synchronized even while the panel is visually hidden.
  if (!player.value && !musicOpen.value) {
    localPosition.value = expected()
    return
  }
  await nextTick()
  await initPlayer()
  const p = player.value
  const state = media.state
  if (!p || !state?.current || !unlocked.value) {
    localPosition.value = expected()
    return
  }
  const pos = expected()
  try {
    if (lastVideo.value !== state.current.videoId) {
      if (state.playing) p.loadVideoById({ videoId: state.current.videoId, startSeconds: pos })
      else p.cueVideoById({ videoId: state.current.videoId, startSeconds: pos })
      lastVideo.value = state.current.videoId
    } else {
      const local = p.getCurrentTime?.() || 0
      if (Math.abs(local - pos) > 3.5) p.seekTo?.(pos, true)
      if (state.playing) p.playVideo?.()
      else p.pauseVideo?.()
    }
    p.setVolume?.(volume.value)
    if (muted.value) p.mute?.()
    else p.unMute?.()
    localPosition.value = p.getCurrentTime?.() || pos
    durationSeconds.value = Math.max(0, Number(p.getDuration?.() || 0))
  } catch {
    // Player may still be warming up; next state/timer tick retries.
  }
}

function refreshLocalPlaybackClock() {
  const p = player.value
  if (p && unlocked.value) {
    try {
      localPosition.value = Math.max(0, Number(p.getCurrentTime?.() || expected()))
      durationSeconds.value = Math.max(0, Number(p.getDuration?.() || durationSeconds.value))
      return
    } catch { /* fall through to authoritative clock */ }
  }
  localPosition.value = expected()
}

async function enable() {
  unlocked.value = true
  try { sessionStorage.setItem('royal.music.unlocked', '1') } catch { /* optional */ }
  mediaError.value = ''
  await syncPlayer()
}

async function search() {
  if (!query.value.trim() || !isHost.value || searching.value) return
  searching.value = true
  mediaError.value = ''
  try { results.value = await media.search(query.value.trim()) }
  catch (error) { mediaError.value = errorText(error, 'Không tìm được nhạc.') }
  finally { searching.value = false }
}

async function choose(track: MediaTrack) {
  mediaError.value = ''
  try {
    await enable()
    await media.select(track)
    results.value = []
  } catch (error) { mediaError.value = errorText(error, 'Không chọn được bài nhạc.') }
}

async function queueTrack(track: MediaTrack) {
  mediaError.value = ''
  try { await media.queue(track) }
  catch (error) { mediaError.value = errorText(error, 'Không thêm được vào hàng chờ.') }
}

async function toggle() {
  let pos = expected()
  try { pos = player.value?.getCurrentTime?.() ?? pos } catch { /* use expected position */ }
  try { await media.toggle(!media.state?.playing, pos) }
  catch (error) { mediaError.value = errorText(error, 'Không đổi được trạng thái phát.') }
}

async function seek(position: number) {
  if (!isHost.value || !media.state?.current) return
  const pos = Math.max(0, Number.isFinite(position) ? position : 0)
  localPosition.value = pos
  try { player.value?.seekTo?.(pos, true) } catch { /* authoritative update still proceeds */ }
  try { await media.seek(pos) }
  catch (error) { mediaError.value = errorText(error, 'Không tua được bài nhạc.') }
}

function onSeek(event: Event) {
  const target = event.target as HTMLInputElement
  void seek(Number(target.value))
}

async function findShorts() {
  if (!shortQuery.value.trim() || shorting.value) return
  shorting.value = true
  shortError.value = ''
  try {
    shortFeed.value = await media.search(shortQuery.value.trim(), true)
    shortIndex.value = 0
  } catch (error) { shortError.value = errorText(error, 'Không tìm được Shorts.') }
  finally { shorting.value = false }
}

function nextShort(delta: number) {
  if (!shortFeed.value.length) return
  shortIndex.value = (shortIndex.value + delta + shortFeed.value.length) % shortFeed.value.length
}

function onShortWheel(event: WheelEvent) {
  if (shortFeed.value.length < 2 || shortWheelLocked || Math.abs(event.deltaY) < 10) return
  shortWheelLocked = true
  nextShort(event.deltaY > 0 ? 1 : -1)
  window.setTimeout(() => { shortWheelLocked = false }, 350)
}

watch(roomId, async (next, previous) => {
  if (previous && previous !== next) {
    // The dock subtree is removed when no room is active. Destroy the old
    // iframe explicitly so we never keep a stale YT.Player handle for the next room.
    destroyPlayer()
    await media.leaveRoom()
  }
  if (!next) return
  await ensureLobbyContext(next)
  try {
    await media.joinRoom(next)
    mediaError.value = ''
  } catch (error) {
    mediaError.value = errorText(error, 'Không kết nối được Media Room.')
  }
}, { immediate: true })

watch(() => media.state?.revision, () => {
  localPosition.value = expected()
  void syncPlayer()
})
watch(musicOpen, value => { if (value) void syncPlayer() })
watch(volume, value => {
  try { localStorage.setItem('royal.music.volume', String(value)) } catch { /* optional */ }
  try { player.value?.setVolume?.(value) } catch { /* optional */ }
})
watch(shortMuted, value => {
  try { sessionStorage.setItem('royal.shorts.muted', value ? '1' : '0') } catch { /* optional */ }
})

onMounted(() => {
  positionTimer = window.setInterval(refreshLocalPlaybackClock, 1000)
})

onBeforeUnmount(() => {
  if (positionTimer !== null) window.clearInterval(positionTimer)
  void media.leaveRoom()
  destroyPlayer()
})
</script>

<template>
  <div v-if="roomId" class="media-dock">
    <div class="media-buttons">
      <button type="button" @click="musicOpen = !musicOpen">🎵 {{ media.state?.current?.title || 'Music Room' }}</button>
      <button type="button" @click="shortsOpen = !shortsOpen">📱 Shorts</button>
    </div>

    <section :class="['media-panel', 'music-panel', { 'panel-hidden': !musicOpen }]" :aria-hidden="!musicOpen">
      <header>
        <b>TABLE MUSIC</b>
        <span>{{ isHost ? 'Bạn là DJ' : 'DJ: Chủ bàn' }}</span>
        <button type="button" @click="musicOpen = false">×</button>
      </header>

      <div class="yt-box">
        <div ref="playerHost" class="yt-player" />
        <button v-if="media.state?.current && !unlocked" type="button" class="unlock" @click="enable">🎧 Bật nhạc bàn</button>
        <p v-if="!media.state?.current">Chủ bàn chưa chọn nhạc</p>
      </div>

      <div v-if="media.state?.current" class="now">
        <b>{{ media.state.current.title }}</b>
        <small>{{ media.state.current.channelTitle || 'YouTube' }}</small>
      </div>

      <div class="controls">
        <button type="button" :disabled="!isHost || !media.state?.current" @click="toggle">{{ media.state?.playing ? '⏸' : '▶' }}</button>
        <button type="button" :disabled="!isHost || !media.state?.current" @click="media.next()">⏭</button>
        <button type="button" @click="muted = !muted; syncPlayer()">{{ muted ? '🔇' : '🔊' }}</button>
        <input v-model.number="volume" aria-label="Âm lượng" type="range" min="0" max="100">
      </div>

      <div v-if="media.state?.current" class="timeline">
        <span>{{ formatSeconds(localPosition) }}</span>
        <input
          :value="Math.min(localPosition, Math.max(1, durationSeconds))"
          aria-label="Vị trí bài nhạc"
          type="range"
          min="0"
          :max="Math.max(1, durationSeconds)"
          :disabled="!isHost || durationSeconds <= 0"
          @change="onSeek"
        >
        <span>{{ durationSeconds > 0 ? formatSeconds(durationSeconds) : '--:--' }}</span>
      </div>

      <div v-if="isHost" class="search">
        <input v-model="query" maxlength="100" placeholder="Tìm YouTube..." @keyup.enter="search">
        <button type="button" :disabled="searching" @click="search">{{ searching ? '...' : 'Tìm' }}</button>
      </div>

      <p v-if="mediaError" class="media-error" role="alert">{{ mediaError }}</p>

      <div v-if="results.length" class="results">
        <article v-for="result in results" :key="result.videoId" class="result-row">
          <button type="button" class="result-main" @click="choose(result)">
            <img :src="result.thumbnail" alt="">
            <span><b>{{ result.title }}</b><small>{{ result.channelTitle }}</small></span>
          </button>
          <button type="button" class="queue-add" @click="queueTrack(result)">+ Queue</button>
        </article>
      </div>

      <div v-if="media.state?.queue.length" class="queue">
        <div class="queue-head">
          <span>Queue {{ media.state.queue.length }}/20</span>
          <button v-if="isHost" type="button" @click="media.clearQueue()">Xóa queue</button>
        </div>
        <div v-for="queued in media.state.queue" :key="`${queued.videoId}-${queued.title}`" class="queue-item">{{ queued.title }}</div>
      </div>
    </section>

    <section v-if="shortsOpen" class="media-panel shorts-panel">
      <header>
        <b>SHORTS LOUNGE</b>
        <span>{{ shortFeed.length ? `${shortIndex + 1}/${shortFeed.length}` : 'Tìm chủ đề' }}</span>
        <button type="button" @click="shortsOpen = false">×</button>
      </header>

      <div class="short-frame" @wheel.prevent="onShortWheel">
        <iframe
          v-if="currentShort"
          :key="`${currentShort.videoId}-${shortMuted ? 'm' : 'u'}`"
          :src="shortSrc"
          allow="autoplay; encrypted-media; picture-in-picture"
          allowfullscreen
        />
        <p v-else>Tìm “meme việt”, “football”, “remix”...</p>
      </div>

      <div v-if="currentShort" class="now">
        <b>{{ currentShort.title }}</b>
        <small>{{ currentShort.channelTitle }}</small>
      </div>

      <div class="controls shorts-controls">
        <button type="button" :disabled="shortFeed.length < 2" @click="nextShort(-1)">↑ Trước</button>
        <button type="button" @click="shortMuted = !shortMuted">{{ shortMuted ? '🔇 Mở tiếng' : '🔊 Tắt tiếng' }}</button>
        <button type="button" :disabled="shortFeed.length < 2" @click="nextShort(1)">Tiếp ↓</button>
      </div>

      <div class="search">
        <input v-model="shortQuery" maxlength="100" placeholder="Chủ đề Shorts..." @keyup.enter="findShorts">
        <button type="button" :disabled="shorting" @click="findShorts">{{ shorting ? '...' : 'Tìm' }}</button>
      </div>
      <p v-if="shortError" class="media-error" role="alert">{{ shortError }}</p>
      <small class="short-hint">Cuộn lên/xuống trên video để đổi clip.</small>
    </section>
  </div>
</template>

<style scoped>
.media-dock{position:fixed;right:14px;bottom:14px;z-index:80;font-family:Inter,system-ui,sans-serif}.media-buttons{display:flex;gap:8px;justify-content:flex-end}.media-buttons button,.media-panel button{border:1px solid rgba(244,208,111,.35);background:#102c26;color:#f6e6b4;border-radius:10px;padding:8px 10px}.media-buttons button{max-width:210px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.media-panel{position:absolute;right:0;bottom:46px;width:min(380px,calc(100vw - 20px));max-height:min(76vh,650px);overflow:auto;background:rgba(6,24,21,.98);border:1px solid rgba(244,208,111,.45);box-shadow:0 20px 50px #0009;border-radius:16px;padding:12px;color:#f8efd2}.panel-hidden{visibility:hidden;opacity:0;pointer-events:none}.media-panel header{display:grid;grid-template-columns:1fr auto auto;gap:8px;align-items:center}.media-panel header span{font-size:12px;opacity:.7}.yt-box,.short-frame{margin-top:10px;min-height:190px;background:#020807;border-radius:12px;display:grid;place-items:center;position:relative;overflow:hidden}.yt-player,.short-frame iframe{width:100%;height:190px;border:0}.unlock{position:absolute}.now{display:grid;gap:2px;margin:10px 0}.now small{opacity:.65}.controls,.search,.timeline{display:flex;gap:8px;align-items:center;margin-top:8px}.controls input,.timeline input{flex:1;min-width:0}.timeline span{font-size:11px;min-width:34px;text-align:center;opacity:.78}.search input{min-width:0;flex:1;background:#071c18;border:1px solid #ffffff22;color:white;border-radius:9px;padding:9px}.media-error{margin:8px 0 0;color:#ffb4ab;font-size:12px}.results{display:grid;gap:6px;margin-top:8px}.result-row{display:grid;grid-template-columns:1fr auto;gap:6px;align-items:stretch}.result-main{display:flex;text-align:left;gap:8px;min-width:0}.result-main img{width:80px;height:45px;object-fit:cover;border-radius:6px}.result-main span{display:grid;min-width:0}.result-main b,.result-main small{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.result-main b{font-size:12px}.result-main small{font-size:10px;opacity:.65}.queue-add{white-space:nowrap}.queue{display:grid;gap:5px;margin-top:10px}.queue-head{display:flex;justify-content:space-between;align-items:center}.queue-head button{padding:4px 8px}.queue-item{font-size:11px;padding:6px 8px;border-radius:8px;background:#ffffff0b;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.short-frame{height:360px}.short-frame iframe{height:360px}.short-hint{display:block;margin-top:8px;opacity:.6}.shorts-controls{justify-content:space-between}@media(max-width:600px){.media-dock{right:8px;bottom:8px}.media-panel{width:calc(100vw - 16px);max-height:72vh}.short-frame,.short-frame iframe{height:50vh}.result-row{grid-template-columns:minmax(0,1fr) auto}.media-buttons button{max-width:170px}}@media(prefers-reduced-motion:reduce){.media-panel,.media-buttons button{transition:none!important}}
</style>