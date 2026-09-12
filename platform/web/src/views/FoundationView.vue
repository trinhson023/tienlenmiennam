<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { api } from '@/core/http/api'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const results = ref<Record<string,string>>({})
const loading = ref(true)
const allHealthy = computed(() => ['identity','lobby','tienlen'].every(x => results.value[x] === 'Healthy'))

onMounted(async () => {
  await Promise.all(['identity','lobby','tienlen'].map(async service => {
    try { results.value[service] = (await api.get(`/${service}/health`)).data }
    catch { results.value[service] = 'Unavailable' }
  }))
  loading.value = false
})

async function logout() {
  await auth.logout()
  await router.push('/login')
}
</script>

<template>
  <main class="shell">
    <header class="topline">
      <div><strong>{{ auth.user?.displayName }}</strong><span>@{{ auth.user?.username }}</span></div>
      <button class="ghost" @click="logout">Đăng xuất</button>
    </header>
    <section class="hero">
      <span class="eyebrow">M2 IDENTITY</span>
      <h1>Royal Game Platform</h1>
      <p>.NET 8 microservices + Vue 3 + SignalR</p>
      <div class="status" :class="{ ok: allHealthy && !loading }">{{ loading ? 'Checking services…' : allHealthy ? 'Foundation healthy · Authenticated' : 'Some services are unavailable' }}</div>
    </section>
    <section class="grid">
      <article v-for="name in ['identity','lobby','tienlen']" :key="name" class="card"><strong>{{ name }}</strong><span>{{ results[name] || 'Checking…' }}</span></article>
    </section>
    <section class="identity-summary">
      <span>Roles</span><b>{{ auth.user?.roles.join(', ') || '—' }}</b>
      <span>Permissions</span><b>{{ auth.user?.permissions.join(', ') || '—' }}</b>
    </section>
    <p class="note">M2 proves persistent account identity. Lobby/game migration continues after this checkpoint.</p>
  </main>
</template>
