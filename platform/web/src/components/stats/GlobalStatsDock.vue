<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useLobbyStore } from '@/stores/lobby'
import { useStatsStore } from '@/stores/stats'

const route = useRoute()
const auth = useAuthStore()
const lobby = useLobbyStore()
const stats = useStatsStore()
const open = ref(false)

const visible = computed(() => auth.isAuthenticated && route.path === '/')
const gameSlug = computed(() => lobby.selectedGame || 'tien-len')
const gameName = computed(() => lobby.games.find(x => x.slug === gameSlug.value)?.displayName || gameSlug.value)

watch(open, value => { if (value) void refresh() })
watch(gameSlug, () => { if (open.value) void refresh() })

async function refresh() {
  try { await stats.load(gameSlug.value) } catch { /* error shown in panel */ }
}
</script>

<template>
  <aside v-if="visible" class="stats-dock">
    <button class="stats-toggle" type="button" @click="open = !open">🏆 Stats</button>
    <section v-if="open" class="stats-panel">
      <header><div><small>{{ gameName.toUpperCase() }}</small><b>THỐNG KÊ CỦA BẠN</b></div><button type="button" @click="open=false">×</button></header>
      <p v-if="stats.error" class="error" role="alert">{{ stats.error }}</p>
      <template v-if="stats.me">
        <div class="my-grid"><div><strong>{{ stats.me.gamesPlayed }}</strong><span>Ván</span></div><div><strong>{{ stats.me.wins }}</strong><span>Thắng</span></div><div><strong>{{ stats.me.losses }}</strong><span>Thua</span></div><div><strong>{{ stats.me.winRate }}%</strong><span>Win rate</span></div></div>
        <div class="rating">Rating-ready <b>{{ stats.me.rating }}</b></div>
      </template>
      <div class="board-head"><b>BXH {{ gameName.toUpperCase() }}</b><button type="button" :disabled="stats.loading" @click="refresh">↻</button></div>
      <ol class="leaderboard">
        <li v-for="row in stats.leaderboard.slice(0,5)" :key="row.userId"><span>#{{ row.rank }}</span><b>{{ row.displayName || row.username }}</b><small>{{ row.wins }}W · {{ row.gamesPlayed }} ván · {{ row.winRate }}%</small></li>
        <li v-if="!stats.loading && !stats.leaderboard.length" class="empty">Chưa có dữ liệu xếp hạng.</li>
      </ol>
      <p v-if="stats.loading" class="loading">Đang tải thống kê...</p>
    </section>
  </aside>
</template>

<style scoped>
.stats-dock{position:fixed;left:14px;bottom:14px;z-index:79;font-family:Inter,system-ui,sans-serif}.stats-toggle,.stats-panel button{border:1px solid rgba(244,208,111,.4);background:#102c26;color:#f7e8b5;border-radius:10px;padding:8px 11px}.stats-panel{position:absolute;left:0;bottom:46px;width:min(330px,calc(100vw - 20px));background:rgba(6,24,21,.98);border:1px solid rgba(244,208,111,.45);box-shadow:0 20px 50px #0009;border-radius:16px;padding:13px;color:#f8efd2}.stats-panel header,.board-head{display:flex;align-items:center;justify-content:space-between;gap:10px}.stats-panel header div{display:grid}.stats-panel header small{font-size:10px;letter-spacing:.16em;opacity:.65}.my-grid{display:grid;grid-template-columns:repeat(4,1fr);gap:6px;margin:12px 0}.my-grid div{display:grid;text-align:center;background:#071c18;border:1px solid #ffffff12;border-radius:10px;padding:8px 4px}.my-grid strong{font-size:18px;color:#ffe18a}.my-grid span{font-size:10px;opacity:.65}.rating{display:flex;justify-content:space-between;border-bottom:1px solid #ffffff14;padding-bottom:10px;font-size:12px}.board-head{margin-top:10px}.board-head button{padding:4px 8px}.leaderboard{list-style:none;margin:8px 0 0;padding:0;display:grid;gap:6px}.leaderboard li{display:grid;grid-template-columns:34px 1fr;gap:2px 7px;background:#071c18;border-radius:9px;padding:7px}.leaderboard li span{grid-row:1/3;color:#ffe18a;font-weight:800}.leaderboard li small{opacity:.62}.leaderboard .empty{display:block;text-align:center;opacity:.6}.loading,.error{font-size:12px}.error{color:#ff9b91}@media(max-width:600px){.stats-dock{left:8px;bottom:8px}.stats-panel{width:calc(100vw - 16px)}.my-grid{grid-template-columns:repeat(2,1fr)}}
</style>
