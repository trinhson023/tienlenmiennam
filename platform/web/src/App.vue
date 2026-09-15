<script setup lang="ts">
import GlobalMediaDock from '@/components/media/GlobalMediaDock.vue'
import GlobalStatsDock from '@/components/stats/GlobalStatsDock.vue'
import ActiveMatchExit from '@/components/game/ActiveMatchExit.vue'
</script>

<template>
  <RouterView />
  <ActiveMatchExit />
  <GlobalMediaDock />
  <GlobalStatsDock />
</template>
