<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'
import { useAuthStore } from '@/stores/auth'
import { useLobbyStore } from '@/stores/lobby'

const auth = useAuthStore()
const lobby = useLobbyStore()
const router = useRouter()
const roomName = ref('')
const error = ref('')
const enabledGames = computed(() => lobby.games.filter(x => x.isEnabled))
const selectedGame = computed(() => lobby.games.find(x => x.slug === lobby.selectedGame))

onMounted(async () => {
  try { await lobby.initialize() }
  catch (e) { error.value = axios.isAxiosError(e) ? (e.response?.data?.message || 'Không tải được lobby.') : 'Không tải được lobby.' }
})

async function createRoom() {
  error.value = ''
  try { await lobby.createRoom(roomName.value, selectedGame.value?.maxPlayers) }
  catch (e) { error.value = axios.isAxiosError(e) ? (e.response?.data?.message || 'Không tạo được phòng.') : 'Không tạo được phòng.' }
}

async function join(roomId: string) {
  error.value = ''
  try { await lobby.joinRoom(roomId) }
  catch (e) { error.value = axios.isAxiosError(e) ? (e.response?.data?.message || 'Không vào được phòng.') : 'Không vào được phòng.' }
}

async function logout() {
  await auth.logout()
  await router.push('/login')
}
</script>

<template>
  <main class="lobby-shell">
    <header class="lobby-topbar">
      <div>
        <span class="eyebrow">ROYAL GAME LOUNGE</span>
        <strong>{{ auth.user?.displayName }}</strong>
        <small>@{{ auth.user?.username }}</small>
      </div>
      <button class="ghost" @click="logout">Đăng xuất</button>
    </header>

    <section class="game-catalog">
      <button
        v-for="game in lobby.games"
        :key="game.slug"
        class="game-tile"
        :class="{ active: lobby.selectedGame === game.slug, disabled: !game.isEnabled }"
        :disabled="!game.isEnabled"
        @click="lobby.changeGame(game.slug)"
      >
        <span>{{ game.icon }}</span>
        <div><b>{{ game.displayName }}</b><small>{{ game.isEnabled ? `${game.minPlayers}-${game.maxPlayers} người` : 'Sắp ra mắt' }}</small></div>
      </button>
    </section>

    <p v-if="error" class="error">{{ error }}</p>

    <section v-if="lobby.currentRoom" class="room-panel">
      <div class="room-panel-head">
        <div><span class="eyebrow">ĐANG Ở PHÒNG</span><h2>{{ lobby.currentRoom.name }}</h2></div>
        <button class="danger" :disabled="lobby.busy" @click="lobby.leaveCurrentRoom">Rời phòng</button>
      </div>
      <div class="seat-grid">
        <article v-for="seat in lobby.currentRoom.maxPlayers" :key="seat" class="seat-card">
          <template v-if="lobby.currentRoom.members.find(x => x.seatNumber === seat - 1)">
            <strong>{{ lobby.currentRoom.members.find(x => x.seatNumber === seat - 1)?.displayName }}</strong>
            <small>@{{ lobby.currentRoom.members.find(x => x.seatNumber === seat - 1)?.username }}</small>
            <span v-if="lobby.currentRoom.members.find(x => x.seatNumber === seat - 1)?.isHost" class="host-badge">HOST</span>
          </template>
          <template v-else><span class="empty-seat">Ghế trống {{ seat }}</span></template>
        </article>
      </div>
      <p class="muted">Room tồn tại độc lập với Match. M4 sẽ gắn nút Start Game vào Tiến Lên Service.</p>
    </section>

    <template v-else>
      <section class="lobby-actions">
        <div>
          <span class="eyebrow">{{ selectedGame?.displayName || 'GAME' }}</span>
          <h1>Chọn một bàn</h1>
          <p class="muted">Room metadata được lưu trong PostgreSQL và cập nhật realtime bằng SignalR.</p>
        </div>
        <form class="create-room" @submit.prevent="createRoom">
          <input v-model.trim="roomName" maxlength="60" placeholder="Tên bàn (để trống cũng được)" />
          <button class="primary" :disabled="lobby.busy || enabledGames.length === 0">+ Tạo bàn</button>
        </form>
      </section>

      <section class="room-list">
        <article v-for="room in lobby.rooms" :key="room.id" class="room-row">
          <div class="room-icon">{{ room.gameIcon }}</div>
          <div class="room-main"><strong>{{ room.name }}</strong><span>{{ room.gameName }} · {{ room.status }}</span></div>
          <div class="room-count">{{ room.playerCount }}/{{ room.maxPlayers }}</div>
          <button class="primary compact" :disabled="room.playerCount >= room.maxPlayers || room.status !== 'Open'" @click="join(room.id)">Vào bàn</button>
        </article>
        <div v-if="!lobby.rooms.length" class="empty-lobby">Chưa có bàn nào. Bro mở bàn đầu tiên đi :v</div>
      </section>
    </template>
  </main>
</template>
