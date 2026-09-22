import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import LobbyView from '@/views/LobbyView.vue'
import WaitingTableView from '@/views/WaitingTableView.vue'
import GameView from '@/views/GameView.vue'
import SamGameView from '@/views/SamGameView.vue'
import FoundationView from '@/views/FoundationView.vue'
import LoginView from '@/views/LoginView.vue'
import RegisterView from '@/views/RegisterView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: LobbyView, meta: { requiresAuth: true } },
    { path: '/rooms/:roomId/table', component: WaitingTableView, meta: { requiresAuth: true } },
    { path: '/games/tien-len/:matchId', component: GameView, meta: { requiresAuth: true } },
    { path: '/games/sam-loc/:matchId', component: SamGameView, meta: { requiresAuth: true } },
    { path: '/foundation', component: FoundationView, meta: { requiresAuth: true } },
    { path: '/login', component: LoginView, meta: { guestOnly: true } },
    { path: '/register', component: RegisterView, meta: { guestOnly: true } }
  ]
})

router.beforeEach(async to => {
  const auth = useAuthStore()
  await auth.initialize()
  if (to.meta.requiresAuth && !auth.isAuthenticated) return { path: '/login', query: { redirect: to.fullPath } }
  if (to.meta.guestOnly && auth.isAuthenticated) return { path: '/' }
  return true
})

export default router
