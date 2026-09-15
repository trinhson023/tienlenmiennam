<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useLobbyStore } from '@/stores/lobby'
import { useTienLenStore } from '@/stores/tienlen'

const route = useRoute()
const router = useRouter()
const lobby = useLobbyStore()
const game = useTienLenStore()
const busy = ref(false)
const error = ref('')
const visible = computed(() => route.path.startsWith('/games/tien-len/') && game.match?.status === 'InProgress')

async function abandon() {
  const roomId = game.match?.roomId
  if (!roomId || busy.value) return
  if (!window.confirm('Bỏ ván và rời phòng? Bot sẽ đánh tiếp ghế của bạn để không phá ván của người còn lại.')) return
  busy.value = true
  error.value = ''
  try {
    await lobby.leaveRoom(roomId)
    await game.leaveView()
    await router.replace('/')
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Không thể bỏ ván.'
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <div v-if="visible" class="match-exit">
    <button type="button" :disabled="busy" @click="abandon">{{ busy ? 'Đang rời…' : '🚪 Bỏ ván' }}</button>
    <small v-if="error" role="alert">{{ error }}</small>
  </div>
</template>

<style scoped>
.match-exit{position:fixed;right:14px;top:14px;z-index:95;display:grid;justify-items:end;gap:5px}.match-exit button{border:1px solid #ff8e7c88;border-radius:999px;padding:7px 11px;background:#551f1bcc;color:#ffe0d9;font-size:.68rem;font-weight:900;box-shadow:0 8px 24px #0006}.match-exit button:disabled{opacity:.5}.match-exit small{max-width:260px;padding:5px 8px;border-radius:8px;background:#6b2525;color:#ffd7d7;font-size:.62rem}@media(max-width:680px){.match-exit{right:7px;top:auto;bottom:58px}.match-exit button{padding:6px 9px}}
</style>
