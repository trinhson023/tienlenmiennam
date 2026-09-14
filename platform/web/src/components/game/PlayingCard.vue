<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  code: string
  selected?: boolean
  compact?: boolean
  disabled?: boolean
}>(), { selected: false, compact: false, disabled: false })

const emit = defineEmits<{ select: [code: string] }>()
const cardAssets = import.meta.glob('../../assets/cards/*.svg', { eager: true, query: '?url', import: 'default' }) as Record<string, string>
const normalizedCode = computed(() => props.code.toUpperCase())
const imageSrc = computed(() => cardAssets[`../../assets/cards/${normalizedCode.value}.svg`] || '')
const suitCode = computed(() => normalizedCode.value.slice(-1))
const rankCode = computed(() => normalizedCode.value.slice(0, -1))
const rank = computed(() => rankCode.value === 'T' ? '10' : rankCode.value)
const suit = computed(() => ({ S: '♠', C: '♣', D: '♦', H: '♥' }[suitCode.value] || '?'))
const isRed = computed(() => suitCode.value === 'D' || suitCode.value === 'H')
const label = computed(() => `${rank.value}${suit.value}`)

function choose() { if (!props.disabled) emit('select', props.code) }
</script>

<template>
  <button
    type="button"
    class="playing-card"
    :class="{ selected, compact, red: isRed, black: !isRed, asset: !!imageSrc }"
    :disabled="disabled"
    :aria-label="label"
    :aria-pressed="selected"
    @click="choose"
  >
    <img v-if="imageSrc" :src="imageSrc" :alt="label" draggable="false" />
    <template v-else>
      <span class="corner corner-top"><b>{{ rank }}</b><i>{{ suit }}</i></span>
      <span class="card-suit">{{ suit }}</span>
      <span class="corner corner-bottom"><b>{{ rank }}</b><i>{{ suit }}</i></span>
    </template>
  </button>
</template>

<style scoped>
.playing-card {
  position: relative; flex: 0 0 auto; width: 72px; height: 101px; padding: 0; overflow: hidden;
  border: 1px solid rgba(18,24,21,.22); border-radius: 9px;
  background: linear-gradient(145deg, rgba(255,255,255,.98), rgba(244,241,228,.98));
  box-shadow: 0 8px 17px rgba(0,0,0,.34), inset 0 0 0 1px rgba(255,255,255,.72);
  color: #151918; cursor: pointer; transform-origin: 50% 100%;
  transition: transform .16s ease, box-shadow .16s ease, border-color .16s ease, filter .16s ease; user-select: none;
}
.playing-card.asset { background: #fff; }
.playing-card img { width: 100%; height: 100%; display: block; object-fit: fill; pointer-events: none; }
.playing-card:hover:not(:disabled) { transform: translateY(-7px); box-shadow: 0 14px 25px rgba(0,0,0,.4), 0 0 0 1px rgba(244,208,111,.52); }
.playing-card.selected { transform: translateY(-17px); border-color: #f4d06f; box-shadow: 0 19px 30px rgba(0,0,0,.45), 0 0 22px rgba(244,208,111,.36); }
.playing-card:disabled { cursor: default; }
.playing-card.compact { width: 62px; height: 87px; }
.playing-card.red { color: #c92e37; }
.corner { position: absolute; display: grid; justify-items: center; line-height: .9; font-family: Georgia, 'Times New Roman', serif; }
.corner b { font-size: 17px; }.corner i { margin-top: 3px; font-size: 15px; font-style: normal; }
.corner-top { top: 7px; left: 7px; }.corner-bottom { right: 7px; bottom: 7px; transform: rotate(180deg); }
.card-suit { position: absolute; left: 50%; top: 51%; transform: translate(-50%,-50%); font-family: Georgia, 'Times New Roman', serif; font-size: 39px; line-height: 1; }
.compact .corner b { font-size: 14px; }.compact .corner i { font-size: 12px; }.compact .card-suit { font-size: 32px; }
@media (max-width: 720px) {
  .playing-card { width: 58px; height: 82px; border-radius: 7px; }.playing-card.compact { width: 49px; height: 69px; }
  .corner { transform: scale(.84); transform-origin: top left; }.corner-bottom { transform: rotate(180deg) scale(.84); transform-origin: center; }.card-suit { font-size: 31px; }
}
</style>
