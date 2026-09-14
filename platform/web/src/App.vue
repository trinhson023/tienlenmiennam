<script setup lang="ts">
import GlobalMediaDock from '@/components/media/GlobalMediaDock.vue'
import GlobalStatsDock from '@/components/stats/GlobalStatsDock.vue'
</script>

<template>
  <RouterView />
  <GlobalMediaDock />
  <GlobalStatsDock />
</template>
