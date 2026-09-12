<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import axios from 'axios'

const baseUrl = window.__APP_CONFIG__?.apiBaseUrl || 'http://localhost:8888'
const results = ref<Record<string,string>>({})
const loading = ref(true)
const allHealthy = computed(() => ['identity','lobby','tienlen'].every(x => results.value[x] === 'Healthy'))

onMounted(async () => {
  await Promise.all(['identity','lobby','tienlen'].map(async service => {
    try { results.value[service] = (await axios.get(`${baseUrl}/${service}/health`)).data }
    catch { results.value[service] = 'Unavailable' }
  }))
  loading.value = false
})
</script>

<template>
  <main class="shell">
    <section class="hero">
      <span class="eyebrow">V3 FOUNDATION</span>
      <h1>Royal Game Platform</h1>
      <p>.NET 8 microservices + Vue 3 + SignalR</p>
      <div class="status" :class="{ ok: allHealthy && !loading }">{{ loading ? 'Checking services…' : allHealthy ? 'Foundation healthy' : 'Some services are unavailable' }}</div>
    </section>
    <section class="grid">
      <article v-for="name in ['identity','lobby','tienlen']" :key="name" class="card">
        <strong>{{ name }}</strong><span>{{ results[name] || 'Checking…' }}</span>
      </article>
    </section>
    <p class="note">This is the migration shell. The current casino UI remains in the legacy app until the Vue gameplay parity milestone.</p>
  </main>
</template>
