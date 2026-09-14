<script setup lang="ts">
import { computed, ref } from 'vue'
import { useTienLenStore, type MatchPlayerView } from '@/stores/tienlen'

const props = defineProps<{ players: MatchPlayerView[]; selfUserId: string | undefined }>()
const game = useTienLenStore()
const open = ref(false)
const customText = ref('')
const throwType = ref<string | null>(null)
const error = ref('')

const presets = [
  '🔥 Chặt nè con!', '😭 Cay thế nhờ...', '😎 Đè tao đè lại!',
  '⚡ Đánh nhanh hộ cái!', '🍀 Bài son quá bạn ơi!', '🙏 Xin tha con Heo!'
]
const throwables = [
  { type: 'bomb', emoji: '💣', label: 'Ném bom' },
  { type: 'tomato', emoji: '🍅', label: 'Ném cà chua' },
  { type: 'poop', emoji: '💩', label: 'Ném cứt' }
]
const opponents = computed(() => props.players.filter(player => player.userId !== props.selfUserId))

async function send(text: string) {
  const clean = text.replace(/\s+/g, ' ').trim().slice(0, 80)
  if (!clean) return
  error.value = ''
  try {
    await game.sendQuickChat(clean)
    customText.value = ''
    open.value = false
  } catch { error.value = game.error || 'Không gửi được.' }
}

async function throwAt(type: string, targetUserId: string) {
  error.value = ''
  try {
    await game.throwReaction(type, targetUserId)
    throwType.value = null
    open.value = false
  } catch { error.value = game.error || 'Không ném được.' }
}

function selectThrowable(type: string) {
  if (opponents.value.length === 1) { void throwAt(type, opponents.value[0].userId); return }
  throwType.value = throwType.value === type ? null : type
}
</script>

<template>
  <div class="quick-chat">
    <button type="button" class="quick-toggle" @click="open = !open; throwType = null">💬 Gáy</button>
    <div v-if="open" class="quick-menu">
      <strong>GÁY NHANH</strong>
      <div class="quick-grid"><button v-for="text in presets" :key="text" type="button" @click="send(text)">{{ text }}</button></div>
      <div class="quick-custom">
        <input v-model="customText" maxlength="80" placeholder="Tự gáy..." @keyup.enter="send(customText)" />
        <button type="button" :disabled="!customText.trim()" @click="send(customText)">Gửi</button>
      </div>
      <strong>NÉM ĐỒ</strong>
      <div class="throw-grid">
        <button v-for="item in throwables" :key="item.type" type="button" :class="{ selected: throwType === item.type }" @click="selectThrowable(item.type)">
          <span>{{ item.emoji }}</span><small>{{ item.label }}</small>
        </button>
      </div>
      <div v-if="throwType && opponents.length > 1" class="throw-targets">
        <span>Ném vào ai?</span>
        <button v-for="player in opponents" :key="player.userId" type="button" @click="throwAt(throwType!, player.userId)">{{ player.displayName }}</button>
      </div>
      <small v-if="opponents.length === 1" class="quick-hint">1v1: bấm món để ném thẳng vào {{ opponents[0].displayName }}</small>
      <small v-if="error" class="quick-error">{{ error }}</small>
    </div>
  </div>
</template>

<style scoped>
.quick-chat { position: relative; z-index: 90; }
.quick-toggle { min-height: 30px; padding: 5px 9px; border: 1px solid rgba(244,208,111,.23); border-radius: 999px; color: #f7e8b4; background: rgba(255,255,255,.04); font-size: .64rem; font-weight: 800; }
.quick-menu { position: absolute; z-index: 120; right: 0; top: calc(100% + 8px); width: 320px; display: grid; gap: 8px; padding: 11px; border: 1px solid rgba(244,208,111,.34); border-radius: 14px; background: rgba(3,23,15,.98); box-shadow: 0 18px 45px rgba(0,0,0,.5); }
.quick-menu > strong { color: #f4d06f; font-size: .55rem; letter-spacing: .14em; }
.quick-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 5px; }
.quick-grid button, .throw-targets button { padding: 7px; border: 1px solid rgba(255,255,255,.09); border-radius: 8px; color: #e8f1eb; background: rgba(255,255,255,.045); text-align: left; font-size: .62rem; }
.quick-custom { display: grid; grid-template-columns: 1fr auto; gap: 5px; }
.quick-custom input { min-width: 0; padding: 7px 8px; border: 1px solid rgba(255,255,255,.12); border-radius: 8px; outline: none; color: #fff; background: rgba(0,0,0,.24); font-size: .65rem; }
.quick-custom button { padding: 6px 9px; border: 1px solid rgba(244,208,111,.34); border-radius: 8px; color: #102418; background: #e7c65f; font-size: .62rem; font-weight: 900; }
.quick-custom button:disabled { opacity: .4; }
.throw-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 6px; }
.throw-grid button { display: grid; place-items: center; gap: 2px; padding: 7px 4px; border: 1px solid rgba(255,255,255,.09); border-radius: 9px; color: #e7eee9; background: rgba(255,255,255,.04); }
.throw-grid button.selected { border-color: rgba(244,208,111,.68); background: rgba(244,208,111,.1); }.throw-grid span { font-size: 1.2rem; }.throw-grid small { font-size: .52rem; }
.throw-targets { display: flex; flex-wrap: wrap; align-items: center; gap: 5px; }.throw-targets > span { width: 100%; color: rgba(240,246,242,.5); font-size: .54rem; }.throw-targets button { text-align: center; }
.quick-hint { color: rgba(235,243,238,.42); font-size: .54rem; }.quick-error { color: #ff9b9b; font-size: .56rem; }
@media (max-width: 520px) { .quick-menu { position: fixed; top: 76px; right: 8px; left: 8px; width: auto; }.quick-grid { grid-template-columns: 1fr; } }
</style>
