import { defineStore } from 'pinia'
import { api } from '@/core/http/api'

export interface PlayerStats { userId:string; gameSlug:string; username:string; displayName:string; gamesPlayed:number; wins:number; losses:number; winRate:number; rating:number; lastPlayedAtUtc:string|null }
export interface LeaderboardEntry extends Omit<PlayerStats,'gameSlug'> { rank:number }

export const useStatsStore = defineStore('stats', {
  state: () => ({ me: null as PlayerStats|null, leaderboard: [] as LeaderboardEntry[], loading: false, error: '' }),
  actions: {
    async load(game='tien-len') {
      this.loading = true; this.error = ''
      try {
        const [me, board] = await Promise.all([
          api.get<PlayerStats>('/api/stats/me', { params: { game } }),
          api.get<LeaderboardEntry[]>('/api/stats/leaderboard', { params: { game, limit: 10 } })
        ])
        this.me = me.data; this.leaderboard = board.data
      } catch (e) { this.error = e instanceof Error ? e.message : 'Không tải được thống kê.'; throw e }
      finally { this.loading = false }
    }
  }
})
