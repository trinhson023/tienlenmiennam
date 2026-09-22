<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api } from '@/core/http/api'
import PlayingCard from '@/components/game/PlayingCard.vue'
import SamQuickChat from '@/components/game/SamQuickChat.vue'
import { useAuthStore } from '@/stores/auth'
import { useLobbyStore } from '@/stores/lobby'
import { useSamLocStore, type SamPlayerView } from '@/stores/samloc'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const lobby = useLobbyStore()
const game = useSamLocStore()

const matchId = computed(() => String(route.params.matchId || ''))
const selected = ref<string[]>([])
const localError = ref('')
const secondsLeft = ref<number | null>(null)
const rematchBusy = ref(false)
const exitBusy = ref(false)
let timer: number | null = null

const completed = computed(() => game.match?.status === 'Completed')
const declaring = computed(() => game.match?.status === 'Declaring')
const isMyTurn = computed(() => game.match?.currentPlayerUserId === auth.user?.id)
const selfPlayer = computed(() => game.match?.players.find(x => x.isSelf || x.userId === auth.user?.id) || null)
const winner = computed(() => game.match?.players.find(x => x.userId === game.match?.winnerUserId) || null)
const declarer = computed(() => game.match?.players.find(x => x.userId === game.match?.samDeclarerUserId) || null)
const rankOrder = ['3','4','5','6','7','8','9','T','J','Q','K','A','2']
const suitOrder = ['S','C','D','H']

const sortedHand = computed(() => [...(game.match?.hand || [])].sort((a,b) => {
  const ar=a.slice(0,-1), br=b.slice(0,-1), as=a.slice(-1), bs=b.slice(-1)
  return rankOrder.indexOf(ar)-rankOrder.indexOf(br) || suitOrder.indexOf(as)-suitOrder.indexOf(bs)
}))
const selectedCards = computed(() => selected.value.filter(card => game.match?.hand.includes(card)))
const opponents = computed(() => {
  const list = [...(game.match?.players || [])].sort((a,b)=>a.seatNumber-b.seatNumber)
  const selfIndex = list.findIndex(x => x.userId === auth.user?.id)
  if (selfIndex < 0) return [] as {player:SamPlayerView;position:'top'|'left'|'right'}[]
  const result:{player:SamPlayerView;position:'top'|'left'|'right'}[]=[]
  for(let offset=1;offset<list.length;offset++){
    const player=list[(selfIndex+offset)%list.length]
    const position = list.length===4 ? (offset===1?'right':offset===2?'top':'left') : list.length===3 ? (offset===1?'right':'left') : 'top'
    result.push({player,position})
  }
  return result
})
const centerLabel = computed(() => {
  const map:Record<string,string>={Single:'BÀI LẺ',Pair:'ĐÔI',Triple:'SÁM',Straight:'SẢNH',FourOfAKind:'TỨ QUÝ'}
  return game.match?.centerType ? (map[game.match.centerType] || game.match.centerType.toUpperCase()) : 'BÀN MỞ'
})

function latestChat(userId:string){return [...game.chatEvents].reverse().find(x=>x.senderUserId===userId)?.text||null}
function reaction(userId:string){return game.latestThrow?.targetUserId===userId?game.latestThrow.emoji:null}
function toggle(card:string){selected.value=selected.value.includes(card)?selected.value.filter(x=>x!==card):[...selected.value,card]}
function clearSelection(){selected.value=[]}

function updateTimer(){
  const deadline = declaring.value ? game.match?.declarationDeadlineUtc : game.match?.turnDeadlineUtc
  if (!deadline || completed.value){secondsLeft.value=null;return}
  secondsLeft.value=Math.max(0,Math.ceil((new Date(deadline).getTime()-Date.now())/1000))
}

async function declareSam(){localError.value='';try{await game.declareSam()}catch{localError.value=game.error||'Không báo Sâm được.'}}
async function play(){if(!selectedCards.value.length)return;localError.value='';try{await game.playCards(selectedCards.value);selected.value=[]}catch{localError.value=game.error||'Nước đánh không hợp lệ.'}}
async function pass(){localError.value='';try{await game.passTurn();selected.value=[]}catch{localError.value=game.error||'Không bỏ lượt được.'}}

async function requestRematch(){
  if(!game.match||rematchBusy.value)return
  rematchBusy.value=true;localError.value=''
  try{
    const{data}=await api.post<{activeMatchId:string|null}>(`/api/lobby/rooms/${game.match.roomId}/rematch`)
    if(data.activeMatchId&&data.activeMatchId!==matchId.value)await router.replace(`/games/sam-loc/${data.activeMatchId}`)
  }catch(e){localError.value=e instanceof Error?e.message:'Không thể đánh lại.'}
  finally{rematchBusy.value=false}
}

async function goToLobby(){
  if(!game.match||!completed.value||exitBusy.value)return
  exitBusy.value=true;localError.value=''
  try{
    const room=await lobby.returnToLobby(game.match.roomId)
    if(room.status==='InGame'&&room.activeMatchId){await router.replace(`/games/sam-loc/${room.activeMatchId}`);return}
    await router.push('/')
  }catch(e){localError.value=e instanceof Error?e.message:'Không thể về sảnh.'}
  finally{exitBusy.value=false}
}

async function load(id:string){
  selected.value=[];localError.value=''
  await game.initialize(id)
  if(game.match){
    try{
      if(lobby.currentRoom?.id!==game.match.roomId)await lobby.loadRoom(game.match.roomId)
      await lobby.subscribeRoom(game.match.roomId)
    }catch{/* gameplay auth remains authoritative */}
  }
  updateTimer()
}

watch(()=>game.match?.version,()=>{selected.value=selected.value.filter(card=>game.match?.hand.includes(card));updateTimer()})
watch(()=>game.rematchMatchId,newId=>{if(newId&&newId!==matchId.value)void router.replace(`/games/sam-loc/${newId}`)})
watch(()=>route.params.matchId,async(value,previous)=>{
  const next=String(value||'')
  if(!previous||!next||next===String(previous))return
  try{await game.leaveView();await load(next)}catch(e){localError.value=e instanceof Error?e.message:'Không vào được ván đánh lại.'}
})

onMounted(async()=>{try{await load(matchId.value);timer=window.setInterval(updateTimer,250)}catch(e){localError.value=e instanceof Error?e.message:'Không vào được ván Sâm.'}})
onBeforeUnmount(()=>{if(timer!==null)window.clearInterval(timer);void game.leaveView()})
</script>

<template>
  <main class="sam-page">
    <section v-if="game.match" class="sam-shell" :class="{ 'my-turn': isMyTurn && !completed }">
      <header class="sam-hud">
        <div class="brand"><div class="crest">🃏</div><div><small>M9 · SÂM LỐC</small><strong>Royal Sâm Table</strong><span>Match {{ matchId.slice(0,8) }} · v{{ game.match.version }}</span></div></div>
        <div class="phase">
          <div class="timer"><strong>{{ secondsLeft ?? '—' }}</strong><small>{{ declaring?'BÁO SÂM':'GIÂY' }}</small></div>
          <div><small>{{ declaring?'CỬA SỔ TUYÊN BỐ':completed?'VÁN ĐÃ XONG':isMyTurn?'ĐẾN LƯỢT BẠN':'ĐANG CHỜ' }}</small><b>{{ declaring?'Ai báo trước được quyền mở bài':completed?(winner?.displayName||'Đã có người thắng'):(isMyTurn?'Ra bài hoặc bỏ lượt':game.match.currentPlayerIsBot?'Bot đang tính':'Đối thủ đang đánh') }}</b></div>
        </div>
        <div class="hud-actions"><SamQuickChat :players="game.match.players" :self-user-id="auth.user?.id"/><button type="button" class="ghost" :disabled="!completed||exitBusy" @click="goToLobby">↩ Sảnh</button></div>
      </header>

      <p v-if="localError||game.error" class="game-error" role="alert">{{ localError||game.error }}</p>

      <div class="table">
        <div class="felt-mark">SÂM</div>

        <article v-for="item in opponents" :key="item.player.userId" class="opponent" :class="[`seat-${item.position}`,{current:game.match.currentPlayerUserId===item.player.userId&&!completed}]">
          <div v-if="latestChat(item.player.userId)" class="chat">{{ latestChat(item.player.userId) }}</div>
          <div v-if="reaction(item.player.userId)" class="reaction">{{ reaction(item.player.userId) }}</div>
          <div class="avatar">{{item.player.isBot?'🤖':item.player.displayName.slice(0,2).toUpperCase()}}</div>
          <div><strong>{{item.player.displayName}}</strong><small>{{item.player.isBot?'Server Bot':`@${item.player.username}`}}</small><span>{{item.player.cardCount}} lá</span></div>
          <em v-if="game.match.samDeclarerUserId===item.player.userId">BÁO SÂM</em>
        </article>

        <section class="center-zone">
          <div class="center-title"><small>{{game.match.center.length?'TRÊN BÀN':'SÂM LỐC'}}</small><strong>{{centerLabel}}</strong></div>
          <div v-if="game.match.center.length" class="center-cards"><PlayingCard v-for="(card,index) in game.match.center" :key="`${card}-${index}`" :code="card" compact disabled :style="{zIndex:index+1}"/></div>
          <div v-else class="empty-center"><span>🃏</span><b>{{declaring?'Đang chờ Báo Sâm':game.match.currentPlayerUserId===auth.user?.id?'Bạn có quyền mở vòng':'Đang chờ nước mở'}}</b><small v-if="declarer">Người báo: {{declarer.displayName}}</small></div>
        </section>

        <section v-if="declaring" class="declare-overlay">
          <small>CỬA SỔ BÁO SÂM</small><h2>Bro có muốn Báo Sâm?</h2><p>Người báo đầu tiên sẽ giành quyền đánh trước. Nếu bị người khác chặn thì Báo Sâm thất bại.</p>
          <button type="button" :disabled="game.busy" @click="declareSam">⚡ BÁO SÂM</button>
          <span>Tự đóng sau {{secondsLeft??0}} giây</span>
        </section>

        <section v-if="completed" class="result-overlay">
          <span>♛</span><h2>{{winner?.userId===auth.user?.id?'BẠN THẮNG!':`${winner?.displayName||'Đối thủ'} ĐÃ VỀ`}}</h2>
          <p v-if="game.match.declarationState==='Succeeded'">🔥 Báo Sâm thành công</p>
          <p v-else-if="game.match.declarationState==='Failed'">Báo Sâm đã bị chặn</p>
          <div class="result-actions"><button type="button" class="primary" :disabled="rematchBusy||exitBusy" @click="requestRematch">{{rematchBusy?'Đang chia bài…':'🔥 Đánh lại'}}</button><button type="button" class="ghost" :disabled="exitBusy||rematchBusy" @click="goToLobby">↩ Về sảnh</button></div>
        </section>
      </div>

      <section v-if="selfPlayer" class="self-panel">
        <div class="self"><div class="avatar">YOU</div><div><small>BẠN</small><strong>{{selfPlayer.displayName}}</strong><span>{{selfPlayer.cardCount}} lá · {{game.match.samDeclarerUserId===selfPlayer.userId?'BÁO SÂM':''}}</span></div></div>
        <div v-if="!declaring&&!completed" class="selection"><span>{{selectedCards.length?`Đã chọn ${selectedCards.length} lá`:'Chọn bài ở dưới'}}</span><button v-if="selectedCards.length" type="button" @click="clearSelection">Bỏ chọn</button></div>
        <div v-if="!declaring&&!completed" class="actions"><button class="play" type="button" :disabled="!isMyTurn||!selectedCards.length||game.busy" @click="play">ĐÁNH {{selectedCards.length||''}}</button><button class="pass" type="button" :disabled="!isMyTurn||!game.match.center.length||game.busy" @click="pass">BỎ LƯỢT</button></div>
      </section>

      <section v-if="!completed" class="hand">
        <div class="hand-head"><span>BÀI CỦA BẠN · {{game.match.hand.length}} LÁ</span><small>Sâm: chất không dùng để so sức mạnh</small></div>
        <div class="hand-cards"><PlayingCard v-for="(card,index) in sortedHand" :key="card" :code="card" :selected="selected.includes(card)" :disabled="declaring" :style="{zIndex:index+1}" @select="toggle"/></div>
      </section>
    </section>
    <div v-else class="loading">Đang đồng bộ bàn Sâm…</div>
  </main>
</template>

<style scoped>
.sam-page{min-height:100vh;padding:14px;color:#edf6ef;background:radial-gradient(circle at 50% -10%,#7a451f66,transparent 38%),linear-gradient(180deg,#181006,#07130d 70%)}.sam-shell{width:min(1220px,100%);margin:auto;padding:12px;border:1px solid #e7bd5c4f;border-radius:24px;background:#07170fef;box-shadow:0 30px 80px #0008}.sam-hud{display:grid;grid-template-columns:1fr auto 1fr;align-items:center;gap:14px;padding:10px 13px;border:1px solid #ffffff12;border-radius:17px;background:linear-gradient(180deg,#26371f,#13261b)}.brand{display:flex;align-items:center;gap:10px}.brand>div:last-child{display:grid}.brand small,.phase small{color:#f4d06f;font-size:.55rem;font-weight:900;letter-spacing:.12em}.brand strong{font-family:Georgia,serif;color:#fff0b3}.brand span{font-size:.62rem;opacity:.5}.crest{width:44px;height:44px;display:grid;place-items:center;border:1px solid #f4d06f88;border-radius:50%;background:#3c2814}.phase{display:flex;align-items:center;gap:9px}.phase>div:last-child{display:grid}.phase b{font-size:.76rem;max-width:240px}.timer{width:55px;height:55px;display:grid;place-items:center;align-content:center;border:2px solid #f4d06f;border-radius:50%;background:#0c2418}.timer strong{color:#ffe091}.timer small{font-size:.42rem}.hud-actions{display:flex;justify-content:flex-end;gap:7px}.ghost,.primary{border:1px solid #f4d06f55;border-radius:10px;padding:8px 10px;color:#f8e8b5;background:#ffffff08}.primary{color:#14251b;background:#dfbd52}.table{position:relative;min-height:460px;margin-top:9px;overflow:hidden;border:13px solid #55331d;border-radius:46%/20%;background:radial-gradient(ellipse,#187348,#0c5739 50%,#083825 76%);box-shadow:inset 0 0 90px #0007,0 18px 32px #0007}.felt-mark{position:absolute;inset:0;display:grid;place-items:center;color:#f4d06f0c;font:900 8rem Georgia}.opponent{position:absolute;z-index:12;display:flex;align-items:center;gap:8px;min-width:170px;padding:8px 10px;border:1px solid #ffffff1f;border-radius:15px;background:#061f17e8}.opponent.current{border-color:#f4d06fcc;box-shadow:0 0 28px #f4d06f42}.seat-top{top:16px;left:50%;transform:translateX(-50%)}.seat-left{left:16px;top:46%;transform:translateY(-50%)}.seat-right{right:16px;top:46%;transform:translateY(-50%)}.avatar{width:40px;height:40px;display:grid;place-items:center;border:1px solid #f4d06f66;border-radius:50%;background:#104b34;color:#ffe49a;font-size:.68rem;font-weight:900}.opponent>div:nth-of-type(2){display:grid}.opponent strong{font-size:.78rem}.opponent small{font-size:.58rem;opacity:.5}.opponent span{color:#f4d06f;font-size:.62rem}.opponent em{position:absolute;right:7px;top:-8px;padding:2px 6px;border-radius:999px;background:#b9422f;color:#fff5d7;font-size:.48rem;font-style:normal;font-weight:900}.chat{position:absolute;bottom:calc(100% + 6px);left:50%;transform:translateX(-50%);padding:6px 9px;border-radius:10px;background:#03170ff7;color:#fff0b3;font-size:.6rem;white-space:nowrap}.reaction{position:absolute;left:50%;top:50%;font-size:2.3rem;animation:throwFx 1.1s both}.center-zone{position:absolute;left:50%;top:55%;transform:translate(-50%,-50%);width:min(500px,54%);min-height:180px;display:grid;place-items:center;align-content:center;border:1px dashed #f4d06f33;border-radius:40px;background:#0002}.center-title{position:absolute;top:12px;display:flex;gap:8px}.center-title small{color:#f4d06f88;font-size:.5rem}.center-title strong{color:#f4d06f;font-size:.65rem}.center-cards{display:flex;padding-top:24px}.center-cards :deep(.playing-card + .playing-card){margin-left:-18px}.empty-center{display:grid;place-items:center;gap:4px;opacity:.66}.empty-center>span{font-size:2.5rem}.empty-center small{font-size:.6rem}.declare-overlay,.result-overlay{position:absolute;z-index:30;inset:70px 20%;display:grid;place-items:center;align-content:center;text-align:center;padding:20px;border:1px solid #f4d06f88;border-radius:24px;background:#09180ff2;box-shadow:0 20px 50px #0009}.declare-overlay small{color:#f4d06f;letter-spacing:.15em}.declare-overlay h2,.result-overlay h2{margin:8px}.declare-overlay p{max-width:430px;opacity:.72}.declare-overlay button{padding:12px 20px;border:1px solid #ffda72;border-radius:12px;background:#c33d28;color:#fff7d8;font-weight:900}.declare-overlay span{margin-top:8px;font-size:.7rem;opacity:.55}.result-overlay>span{font-size:2.6rem;color:#f4d06f}.result-overlay p{color:#f4d06f}.result-actions{display:flex;gap:8px}.self-panel{display:grid;grid-template-columns:190px 1fr 250px;gap:12px;align-items:center;margin-top:9px;padding:10px 12px;border:1px solid #ffffff14;border-radius:16px;background:#09281ce8}.self{display:flex;gap:9px;align-items:center}.self>div:last-child{display:grid}.self small{color:#f4d06f;font-size:.5rem}.self span{font-size:.6rem;opacity:.6}.selection{display:flex;justify-content:center;gap:8px;font-size:.7rem}.selection button{border:0;background:transparent;color:#f4d06f}.actions{display:flex;justify-content:flex-end;gap:7px}.play,.pass{min-height:42px;border-radius:11px;font-weight:900}.play{border:1px solid #f4d06f;background:#e7c85d;color:#14251b}.pass{border:1px solid #ffffff22;background:#263d34;color:#eee}.play:disabled,.pass:disabled{opacity:.35}.hand{margin-top:7px;padding:8px 12px;border:1px solid #ffffff10;border-radius:15px;background:#0002}.hand-head{display:flex;justify-content:space-between;color:#c7aa54;font-size:.56rem}.hand-head small{opacity:.6}.hand-cards{min-height:110px;display:flex;align-items:flex-end;justify-content:center;overflow-x:auto;padding:18px 24px 4px}.hand-cards :deep(.playing-card + .playing-card){margin-left:-22px}.game-error{position:absolute;z-index:60;left:50%;transform:translateX(-50%);width:min(650px,90%);padding:9px;border-radius:10px;background:#6b2226;color:#ffd6d6}.loading{min-height:100vh;display:grid;place-items:center;background:#08130d;color:#f4d06f}@keyframes throwFx{0%{opacity:0;transform:translate(-120px,-120px) scale(.5)}65%{opacity:1;transform:translate(-50%,-50%) scale(1.2)}100%{opacity:0;transform:translate(20px,20px)}}@media(max-width:850px){.sam-hud{grid-template-columns:1fr auto}.phase{grid-column:1/-1;grid-row:2;justify-content:center}.self-panel{grid-template-columns:1fr 1fr}.actions{grid-column:1/-1;justify-content:center}}@media(max-width:680px){.sam-page{padding:5px}.sam-shell{padding:6px}.sam-hud{display:flex;flex-wrap:wrap}.phase{order:3;width:100%}.table{min-height:380px;border-width:8px}.opponent{min-width:125px;padding:6px}.opponent .avatar{width:33px;height:33px}.seat-left{left:3px}.seat-right{right:3px}.center-zone{width:60%;min-height:145px}.declare-overlay,.result-overlay{inset:55px 7%}.self-panel{grid-template-columns:1fr}.hand-head small{display:none}.hand-cards{justify-content:flex-start}.hand-cards :deep(.playing-card + .playing-card){margin-left:-17px}}
</style>
