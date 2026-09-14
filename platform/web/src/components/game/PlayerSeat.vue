<script setup lang="ts">
import { computed } from 'vue'
import type { MatchPlayerView } from '@/stores/tienlen'

const props = defineProps<{
  player: MatchPlayerView
  position: 'top' | 'left' | 'right'
  current: boolean
}>()

const initials = computed(() => {
  const words = props.player.displayName.trim().split(/\s+/).filter(Boolean)
  return (words.length > 1 ? `${words[0][0]}${words[words.length - 1][0]}` : words[0]?.slice(0, 2) || '?').toUpperCase()
})
</script>

<template>
  <article class="table-seat" :class="[`seat-${position}`, { current, finished: player.hasFinished }]">
    <div class="seat-card-backs" aria-hidden="true" v-if="!player.hasFinished && player.cardCount > 0">
      <span /><span /><span />
    </div>
    <div class="seat-profile">
      <div class="seat-avatar">{{ player.isBot ? '🤖' : initials }}</div>
      <div class="seat-copy">
        <div class="seat-name-row">
          <strong>{{ player.displayName }}</strong>
          <span v-if="player.isBot" class="mini-badge bot">BOT</span>
          <span v-if="current" class="mini-badge turn">TURN</span>
        </div>
        <small>{{ player.isBot ? 'Server Bot' : `@${player.username}` }}</small>
        <span class="seat-count">{{ player.hasFinished ? `#${player.finishPosition} VỀ` : `${player.cardCount} lá` }}</span>
      </div>
    </div>
  </article>
</template>

<style scoped>
.table-seat {
  position: absolute;
  z-index: 12;
  display: flex;
  align-items: center;
  gap: 9px;
  min-width: 172px;
  padding: 8px 10px;
  border: 1px solid rgba(255,255,255,.13);
  border-radius: 16px;
  background: linear-gradient(145deg, rgba(3,28,19,.94), rgba(5,43,29,.9));
  box-shadow: 0 12px 24px rgba(0,0,0,.3), inset 0 0 0 1px rgba(255,255,255,.025);
  transition: border-color .2s ease, box-shadow .2s ease, transform .2s ease;
}
.table-seat.current {
  border-color: rgba(244,208,111,.82);
  box-shadow: 0 0 0 2px rgba(244,208,111,.12), 0 0 28px rgba(244,208,111,.25), 0 12px 24px rgba(0,0,0,.32);
  animation: turnPulse 1.25s ease-in-out infinite;
}
.table-seat.finished { opacity: .72; }
.seat-top { top: 18px; left: 50%; transform: translateX(-50%); }
.seat-left { left: 18px; top: 46%; transform: translateY(-50%); }
.seat-right { right: 18px; top: 46%; transform: translateY(-50%); flex-direction: row-reverse; }
.seat-profile { display: flex; align-items: center; gap: 8px; }
.seat-right .seat-profile { flex-direction: row-reverse; text-align: right; }
.seat-avatar {
  width: 40px;
  height: 40px;
  display: grid;
  place-items: center;
  flex: 0 0 40px;
  border: 1px solid rgba(244,208,111,.42);
  border-radius: 50%;
  color: #ffe6a0;
  background: radial-gradient(circle at 35% 28%, #177a50, #083b29 65%, #041f16);
  font-size: .8rem;
  font-weight: 900;
  box-shadow: inset 0 0 13px rgba(255,255,255,.05);
}
.seat-copy { display: grid; gap: 2px; min-width: 0; }
.seat-copy strong { max-width: 102px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-size: .82rem; }
.seat-copy small { color: rgba(223,235,229,.5); font-size: .64rem; }
.seat-count { color: #f5d975; font-size: .68rem; font-weight: 800; }
.seat-name-row { display: flex; align-items: center; gap: 5px; }
.seat-right .seat-name-row { justify-content: flex-end; }
.mini-badge { padding: 2px 5px; border-radius: 999px; font-size: .49rem; line-height: 1.2; font-weight: 900; letter-spacing: .05em; }
.mini-badge.bot { color: #e4e7ff; background: #4d4db5; }
.mini-badge.turn { color: #142313; background: #f4d06f; }
.seat-card-backs { position: relative; width: 35px; height: 46px; flex: 0 0 35px; }
.seat-card-backs span {
  position: absolute;
  inset: 0;
  border: 1px solid rgba(255,255,255,.65);
  border-radius: 5px;
  background:
    repeating-linear-gradient(45deg, rgba(255,255,255,.08) 0 3px, transparent 3px 6px),
    linear-gradient(145deg, #8b1f2a, #53121d);
  box-shadow: 0 4px 8px rgba(0,0,0,.3);
}
.seat-card-backs span:nth-child(1) { transform: rotate(-10deg) translateX(-4px); }
.seat-card-backs span:nth-child(2) { transform: rotate(0); }
.seat-card-backs span:nth-child(3) { transform: rotate(10deg) translateX(4px); }
@keyframes turnPulse {
  0%, 100% { filter: brightness(1); }
  50% { filter: brightness(1.12); }
}
@media (max-width: 760px) {
  .table-seat { min-width: 130px; padding: 6px 7px; border-radius: 13px; }
  .seat-avatar { width: 33px; height: 33px; flex-basis: 33px; font-size: .67rem; }
  .seat-card-backs { display: none; }
  .seat-copy strong { max-width: 76px; font-size: .72rem; }
  .seat-left { left: 5px; }
  .seat-right { right: 5px; }
  .seat-top { top: 8px; }
}
</style>
