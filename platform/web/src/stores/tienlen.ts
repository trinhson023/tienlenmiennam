import { defineStore } from 'pinia'
import type { HubConnection } from '@microsoft/signalr'
import { api } from '@/core/http/api'
import { createTienLenHub } from '@/core/realtime/tienLenHub'

export interface MatchPlayerView {
  userId: string
  seatNumber: number
  username: string
  displayName: string
  cardCount: number
  hasFinished: boolean
  finishPosition: number | null
  isSelf: boolean
  isBot?: boolean
}

export interface MatchStateView {
  matchId: string
  roomId: string
  version: number
  status: string
  currentPlayerUserId: string | null
  currentPlayerIsBot?: boolean
  turnDeadlineUtc?: string | null
  isOpeningPlay: boolean
  centerType: string | null
  center: string[]
  hand: string[]
  players: MatchPlayerView[]
  winnerOrder: string[]
}

export const useTienLenStore = defineStore('tienlen', {
  state: () => ({
    match: null as MatchStateView | null,
    hub: null as HubConnection | null,
    currentMatchId: null as string | null,
    busy: false,
    error: ''
  }),
  actions: {
    async ensureHub() {
      if (this.hub) return
      const hub = createTienLenHub()
      hub.on('MatchStateUpdated', (state: MatchStateView) => {
        if (!this.currentMatchId || state.matchId === this.currentMatchId) {
          this.match = state
          this.currentMatchId = state.matchId
          this.error = ''
        }
      })
      hub.onreconnected(async () => {
        if (!this.currentMatchId) return
        try { await hub.invoke('JoinMatch', this.currentMatchId) } catch { /* UI keeps latest snapshot */ }
      })
      await hub.start()
      this.hub = hub
    },
    async initialize(matchId: string) {
      this.currentMatchId = matchId
      this.error = ''
      await this.ensureHub()
      try {
        await this.hub?.invoke('JoinMatch', matchId)
      } catch {
        const { data } = await api.get<MatchStateView>(`/api/tienlen/matches/${matchId}`)
        this.match = data
        throw new Error('SignalR join failed')
      }
    },
    async playCards(cardCodes: string[]) {
      if (!this.match || !this.currentMatchId) return
      this.busy = true
      this.error = ''
      try {
        await this.hub?.invoke('PlayCards', this.currentMatchId, cardCodes, this.match.version)
      } catch (e) {
        this.error = e instanceof Error ? e.message : 'Không đánh được bài.'
        try { const { data } = await api.get<MatchStateView>(`/api/tienlen/matches/${this.currentMatchId}`); this.match = data } catch { /* ignore */ }
        throw e
      } finally { this.busy = false }
    },
    async passTurn() {
      if (!this.match || !this.currentMatchId) return
      this.busy = true
      this.error = ''
      try {
        await this.hub?.invoke('Pass', this.currentMatchId, this.match.version)
      } catch (e) {
        this.error = e instanceof Error ? e.message : 'Không bỏ lượt được.'
        try { const { data } = await api.get<MatchStateView>(`/api/tienlen/matches/${this.currentMatchId}`); this.match = data } catch { /* ignore */ }
        throw e
      } finally { this.busy = false }
    },
    async leaveView() {
      if (this.hub?.state === 'Connected' && this.currentMatchId) {
        try { await this.hub.invoke('LeaveMatch', this.currentMatchId) } catch { /* ignore */ }
      }
      this.match = null
      this.currentMatchId = null
      this.error = ''
    }
  }
})
