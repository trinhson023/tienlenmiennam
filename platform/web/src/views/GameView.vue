<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useTienLenStore } from '@/stores/tienlen'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const game = useTienLenStore()
const selected = ref<string[]>([])
const localError = ref('')
const secondsLeft = ref<number | null>(null)
let timerInterval: number | null = null
const matchId = String(route.params.matchId)
const isMyTurn = computed(() => !!game.match && game.match.currentPlayerUserId === auth.user?.id)
const completed = computed(() => game.match?.status === 'Completed')

function updateCountdown() {
  if (!game.match?.turnDeadlineUtc || completed.value) {
    secondsLeft.value = null
    return
  }
  const diff = Math.max(0, Math.ceil((new Date(game.match.turnDeadlineUtc).getTime() - Date.now()) / 1000))
  secondsLeft.value = diff
}

onMounted(async () => {
  try {
    await game.initialize(matchId)
    timerInterval = window.setInterval(updateCountdown, 400)
    updateCountdown()
  }
  catch (e) { localError.value = e instanceof Error ? e.message : 'Không vào được ván.' }
})

onBeforeUnmount(() => {
  if (timerInterval !== null) clearInterval(timerInterval)
  void game.leaveView()
})

function toggle(card: string) {
  selected.value = selected.value.includes(card) ? selected.value.filter(x => x !== card) : [...selected.value, card]
}

async function play() {
  localError.value = ''
  try { await game.playCards(selected.value); selected.value = [] }
  catch { localError.value = game.error || 'Nước đánh không hợp lệ.' }
}

async function pass() {
  localError.value = ''
  try { await game.passTurn(); selected.value = [] }
  catch { localError.value = game.error || 'Không bỏ lượt được.' }
}
</script>

<template>
  <main class="lobby-shell">
    <header class="lobby-topbar">
      <div><span class="eyebrow">M6 · PERSISTENCE & TIMER</span><strong>Tiến Lên Miền Nam</strong><small>Match {{ matchId.slice(0, 8) }}</small></div>
      <button class="ghost" @click="router.push('/')">Lobby</button>
    </header>

    <p v-if="localError || game.error" class="error">{{ localError || game.error }}</p>
    <section v-if="game.match" class="room-panel">
      <div class="room-panel-head">
        <div>
          <span class="eyebrow">VERSION {{ game.match.version }}</span>
          <h2>
            {{ completed ? '🏆 Ván đã kết thúc' : isMyTurn ? `Đến lượt bạn ${secondsLeft !== null ? `(${secondsLeft}s)` : ''}` : `Đang chờ đối thủ ${secondsLeft !== null ? `(${secondsLeft}s)` : ''}` }}
          </h2>
        </div>
        <span class="host-badge">{{ game.match.centerType || 'OPEN' }}</span>
      </div>

      <div class="seat-grid">
        <article v-for="player in game.match.players" :key="player.userId" class="seat-card">
          <strong>{{ player.displayName }}</strong>
          <small>@{{ player.username }} · Ghế {{ player.seatNumber + 1 }}</small>
          <span>{{ player.hasFinished ? `#${player.finishPosition} VỀ` : `${player.cardCount} lá` }}</span>
          <span v-if="player.isBot" class="host-badge" style="background:#6366f1;">BOT</span>
          <span v-if="game.match.currentPlayerUserId === player.userId && !completed" class="host-badge">TURN</span>
        </article>
      </div>

      <section class="identity-summary">
        <span>Bài giữa bàn</span>
        <b>{{ game.match.center.length ? game.match.center.join(' · ') : 'Bàn trống' }}</b>
      </section>

      <section v-if="!completed" class="game-hand">
        <span class="eyebrow">BÀI CỦA BẠN · {{ game.match.hand.length }} LÁ</span>
        <div class="card-strip">
          <button v-for="card in game.match.hand" :key="card" type="button" class="card-button" :class="{ active: selected.includes(card) }" @click="toggle(card)">{{ card }}</button>
        </div>
        <div class="game-actions">
          <button class="primary" :disabled="!isMyTurn || !selected.length || game.busy" @click="play">Đánh {{ selected.length || '' }} lá</button>
          <button class="ghost" :disabled="!isMyTurn || !game.match.center.length || game.busy" @click="pass">Bỏ lượt</button>
        </div>
      </section>

      <section v-else class="identity-summary">
        <span>Thứ tự về</span>
        <b>{{ game.match.winnerOrder.map((id, index) => `${index + 1}. ${game.match?.players.find(x => x.userId === id)?.displayName || id.slice(0, 6)}`).join(' · ') }}</b>
      </section>
    </section>
    <div v-else class="empty-lobby">Đang đồng bộ state ván chơi…</div>
  </main>
</template>
