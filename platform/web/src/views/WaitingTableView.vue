<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from 'axios'
import { useAuthStore } from '@/stores/auth'
import { useLobbyStore } from '@/stores/lobby'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const lobby = useLobbyStore()
const error = ref('')
const roomId = computed(() => String(route.params.roomId || ''))
const room = computed(() => lobby.currentRoom?.id === roomId.value ? lobby.currentRoom : null)
const isHost = computed(() => !!room.value && room.value.hostUserId === auth.user?.id)
const isOpen = computed(() => room.value?.status === 'Open')
const canStart = computed(() => !!room.value && isHost.value && isOpen.value && room.value.members.length >= room.value.minPlayers)
const hasEmptySeat = computed(() => !!room.value && room.value.members.length < room.value.maxPlayers)

function memberAt(seat: number) { return room.value?.members.find(x => x.seatNumber === seat) }
function messageFrom(e: unknown, fallback: string) { return axios.isAxiosError(e) ? (e.response?.data?.message || fallback) : fallback }
function gameRoute(matchId: string) { return room.value?.gameSlug === 'tien-len' ? `/games/tien-len/${matchId}` : '/' }

async function start() { error.value=''; try { const result = await lobby.startCurrentMatch(); if (result?.activeMatchId) await router.replace(gameRoute(result.activeMatchId)) } catch(e) { error.value=messageFrom(e,'Không bắt đầu được ván.') } }
async function addBot() { error.value=''; try { await lobby.addBot() } catch(e) { error.value=messageFrom(e,'Không thêm được bot.') } }
async function fillBots() { error.value=''; try { await lobby.fillBots() } catch(e) { error.value=messageFrom(e,'Không lấp đầy bot được.') } }
async function removeBot(userId:string) { error.value=''; try { await lobby.removeBot(userId) } catch(e) { error.value=messageFrom(e,'Không xóa được bot.') } }
async function kick(userId:string) { if (!window.confirm('Mời người chơi này ra khỏi bàn?')) return; error.value=''; try { await lobby.kickMember(userId) } catch(e) { error.value=messageFrom(e,'Không kick được người chơi.') } }
async function leave() { if (!window.confirm('Rời phòng và nhường ghế hiện tại?')) return; error.value=''; try { await lobby.leaveRoom(roomId.value); await router.replace('/') } catch(e) { error.value=messageFrom(e,'Không rời được phòng.') } }
async function removeRoom() { if (!window.confirm('Xóa hẳn bàn này? Người chơi trong phòng sẽ bị đưa về sảnh.')) return; error.value=''; try { await lobby.deleteCurrentRoom(); await router.replace('/') } catch(e) { error.value=messageFrom(e,'Không xóa được bàn.') } }

watch(() => room.value?.activeMatchId, matchId => { if (room.value?.status === 'InGame' && matchId) void router.replace(gameRoute(matchId)) })
watch(() => lobby.currentRoom, next => { if (!next) void router.replace('/') })

onMounted(async () => {
  try {
    await lobby.ensureHub()
    const loaded = await lobby.loadRoom(roomId.value)
    if (!loaded.members.some(x => x.userId === auth.user?.id && !x.isBot)) { await router.replace('/'); return }
    await lobby.subscribeRoom(roomId.value)
    if (loaded.status === 'InGame' && loaded.activeMatchId) await router.replace(gameRoute(loaded.activeMatchId))
  } catch (e) { error.value=messageFrom(e,'Không vào được bàn chờ.') }
})
</script>

<template>
  <main class="waiting-page">
    <section v-if="room" class="waiting-shell">
      <header class="waiting-hud">
        <div class="brand"><span class="crest">♠</span><div><small>{{ room.gameName }}</small><h1>{{ room.name }}</h1><p>{{ room.members.length }}/{{ room.maxPlayers }} ghế · {{ room.status }}</p></div></div>
        <div class="actions">
          <button class="ghost" type="button" @click="router.push('/')">↩ Ra ngoài</button>
          <button v-if="isHost&&isOpen" class="ghost" type="button" :disabled="lobby.busy||!hasEmptySeat" @click="addBot">+ Bot</button>
          <button v-if="isHost&&isOpen" class="ghost" type="button" :disabled="lobby.busy||!hasEmptySeat" @click="fillBots">Fill Bots</button>
          <button v-if="isHost" class="start" type="button" :disabled="!canStart||lobby.busy" @click="start">▶ Bắt đầu ván</button>
          <button class="danger" type="button" :disabled="lobby.busy" @click="leave">Rời phòng</button>
          <button v-if="isHost&&isOpen" class="danger outline" type="button" :disabled="lobby.busy" @click="removeRoom">Xóa bàn</button>
        </div>
      </header>

      <p v-if="error" class="error" role="alert">{{ error }}</p>

      <div class="table-wrap">
        <div class="table-felt"><div class="table-mark">♠</div><div class="center-copy"><small>BÀN CHỜ</small><strong>{{ canStart ? 'Đã đủ người — host có thể bắt đầu' : `Đang chờ đủ ${room.minPlayers} người` }}</strong><span>Ngồi sẵn tại bàn, khi host bắt đầu mọi người sẽ tự vào ván.</span></div></div>
        <article v-for="seat in room.maxPlayers" :key="seat" class="seat" :class="`seat-${seat}`">
          <template v-if="memberAt(seat-1)">
            <div class="avatar">{{ memberAt(seat-1)!.isBot ? '🤖' : memberAt(seat-1)!.displayName.slice(0,1).toUpperCase() }}</div>
            <strong>{{ memberAt(seat-1)!.displayName }}</strong>
            <small>{{ memberAt(seat-1)!.isBot ? 'Server Bot' : `@${memberAt(seat-1)!.username}` }}</small>
            <div class="badges"><span v-if="memberAt(seat-1)!.isHost">HOST</span><span v-if="memberAt(seat-1)!.isBot">BOT</span></div>
            <button v-if="isHost&&isOpen&&memberAt(seat-1)!.isBot" type="button" class="seat-action" @click="removeBot(memberAt(seat-1)!.userId)">Xóa bot</button>
            <button v-else-if="isHost&&isOpen&&!memberAt(seat-1)!.isHost&&memberAt(seat-1)!.userId!==auth.user?.id" type="button" class="seat-action danger-text" @click="kick(memberAt(seat-1)!.userId)">Kick</button>
          </template>
          <template v-else><div class="empty-avatar">+</div><strong>Ghế trống {{ seat }}</strong><small>Đang chờ người chơi</small></template>
        </article>
      </div>

      <footer><span>💡 1 người vẫn có thể ngồi chờ tại bàn.</span><b v-if="isHost">Host mở ván từ {{ room.minPlayers }} người trở lên.</b><span v-else>Đang chờ host bắt đầu.</span></footer>
    </section>
    <div v-else class="loading">Đang mở bàn…</div>
  </main>
</template>

<style scoped>
.waiting-page{min-height:100vh;padding:18px;background:radial-gradient(circle at 50% -10%,#155d4166,transparent 42%),#031710;color:#eff7ef;font-family:Inter,system-ui,sans-serif}.waiting-shell{width:min(1180px,100%);margin:auto;padding:14px;border:1px solid #f4d06f44;border-radius:24px;background:#051f17e8;box-shadow:0 30px 80px #0007}.waiting-hud{display:flex;justify-content:space-between;align-items:center;gap:14px;padding:13px 15px;border:1px solid #ffffff12;border-radius:17px;background:#06291e}.brand{display:flex;align-items:center;gap:12px}.crest{width:48px;height:48px;display:grid;place-items:center;border:1px solid #f4d06f99;border-radius:50%;color:#f4d06f;font-size:1.5rem}.brand div{display:grid}.brand small{color:#f4d06f;font-weight:900;letter-spacing:.12em}.brand h1{margin:2px 0;font-family:Georgia,serif;color:#fff0b3}.brand p{margin:0;opacity:.55;font-size:.78rem}.actions{display:flex;flex-wrap:wrap;justify-content:flex-end;gap:7px}.actions button,.seat-action{border:1px solid #f4d06f55;border-radius:10px;padding:8px 10px;background:#0b3025;color:#f7e9b6;font-weight:800}.actions .start{background:#d7b84c;color:#14251b;border-color:#ffe48d}.actions .danger{background:#67231f;border-color:#a64a43;color:#ffe1dd}.actions .outline{background:transparent}.actions button:disabled{opacity:.35}.error{padding:9px 12px;border-radius:10px;background:#6e2225;color:#ffd8d8}.table-wrap{position:relative;min-height:570px;margin-top:12px}.table-felt{position:absolute;inset:80px 80px 90px;border:13px solid #4b2817;border-radius:48%/24%;background:radial-gradient(ellipse,#0b7049,#075236 58%,#063824);box-shadow:inset 0 0 80px #0006,0 20px 35px #0005}.table-mark{position:absolute;inset:0;display:grid;place-items:center;color:#f4d06f12;font-size:13rem}.center-copy{position:absolute;left:50%;top:50%;transform:translate(-50%,-50%);display:grid;text-align:center;gap:6px;width:70%}.center-copy small{color:#f4d06f;font-weight:900;letter-spacing:.18em}.center-copy strong{font-size:1.1rem}.center-copy span{opacity:.62;font-size:.78rem}.seat{position:absolute;z-index:4;width:190px;min-height:112px;display:grid;justify-items:center;align-content:center;padding:10px;border:1px solid #ffffff18;border-radius:16px;background:#082a20e8;box-shadow:0 10px 25px #0005}.seat-1{left:50%;bottom:8px;transform:translateX(-50%)}.seat-2{right:8px;top:50%;transform:translateY(-50%)}.seat-3{left:50%;top:8px;transform:translateX(-50%)}.seat-4{left:8px;top:50%;transform:translateY(-50%)}.avatar,.empty-avatar{width:38px;height:38px;display:grid;place-items:center;border:1px solid #f4d06f66;border-radius:50%;background:#0b3e2d;color:#ffe28c;font-weight:900}.seat small{opacity:.55}.badges{display:flex;gap:4px;margin-top:4px}.badges span{padding:2px 6px;border-radius:999px;background:#d6b53f;color:#173021;font-size:.58rem;font-weight:900}.seat-action{margin-top:6px;padding:4px 8px;font-size:.65rem}.danger-text{border-color:#b64a44;color:#ffb8b3;background:#391c1b}.waiting-shell footer{display:flex;justify-content:space-between;gap:10px;padding:10px 4px;color:#b9c9c1;font-size:.76rem}.waiting-shell footer b{color:#f4d06f}.loading{min-height:70vh;display:grid;place-items:center}@media(max-width:720px){.waiting-page{padding:6px}.waiting-hud{align-items:flex-start;flex-direction:column}.actions{justify-content:flex-start}.table-wrap{min-height:620px}.table-felt{inset:150px 15px}.seat{width:145px;min-height:100px}.seat-1{bottom:5px}.seat-3{top:30px}.seat-2{right:0}.seat-4{left:0}.waiting-shell footer{flex-direction:column}.center-copy span{display:none}}
</style>
