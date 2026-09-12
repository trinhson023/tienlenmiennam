import { defineStore } from 'pinia'
import { api, ACCESS_TOKEN_KEY, REFRESH_TOKEN_KEY } from '@/core/http/api'

export interface UserProfile {
  id: string
  username: string
  displayName: string
  roles: string[]
  permissions: string[]
}

interface AuthSession {
  accessToken: string
  accessTokenExpiresAtUtc: string
  refreshToken: string
  refreshTokenExpiresAtUtc: string
  user: UserProfile
}

export const useAuthStore = defineStore('auth', {
  state: () => ({ user: null as UserProfile | null, initialized: false, busy: false }),
  getters: { isAuthenticated: state => !!state.user },
  actions: {
    saveSession(session: AuthSession) {
      localStorage.setItem(ACCESS_TOKEN_KEY, session.accessToken)
      localStorage.setItem(REFRESH_TOKEN_KEY, session.refreshToken)
      this.user = session.user
    },
    clearSession() {
      localStorage.removeItem(ACCESS_TOKEN_KEY)
      localStorage.removeItem(REFRESH_TOKEN_KEY)
      this.user = null
    },
    async login(username: string, password: string) {
      this.busy = true
      try {
        const { data } = await api.post<AuthSession>('/api/auth/login', { username, password })
        this.saveSession(data)
      } finally { this.busy = false }
    },
    async register(username: string, displayName: string, password: string) {
      this.busy = true
      try {
        const { data } = await api.post<AuthSession>('/api/auth/register', { username, displayName, password })
        this.saveSession(data)
      } finally { this.busy = false }
    },
    async initialize() {
      if (this.initialized) return
      try {
        if (localStorage.getItem(ACCESS_TOKEN_KEY) || localStorage.getItem(REFRESH_TOKEN_KEY)) {
          const { data } = await api.get<UserProfile>('/api/auth/me')
          this.user = data
        }
      } catch { this.clearSession() }
      finally { this.initialized = true }
    },
    async logout() {
      const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY)
      try { if (refreshToken) await api.post('/api/auth/logout', { refreshToken }) } catch { /* local logout still wins */ }
      this.clearSession()
    }
  }
})
