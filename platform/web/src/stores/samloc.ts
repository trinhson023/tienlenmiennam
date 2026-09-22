import { defineStore } from 'pinia'
import type { HubConnection } from '@microsoft/signalr'
import { api } from '@/core/http/api'
import { createSamLocHub } from '@/core/realtime/samLocHub'
import { createSocialHub } from '@/core/realtime/socialHub'

export interface SamPlayerView {
  userId: string
  seatNumber: number
  username: string
  displayName: string
  cardCount: number
  hasPlayedAny: boolean
  isSelf: boolean
  isBot?: boolean
}

export interface SamStateView {
  matchId: string
  roomId: string
  version: number
  status: string
  currentPlayerUserId: string | null
  currentPlayerIsBot?: boolean
  declarationDeadlineUtc?: string | null
  turnDeadlineUtc?: string | null
  declarationState: string
  samDeclarerUserId: string | null
  centerType: string | null
  center: string[]
  hand: string[]
  players: SamPlayerView[]
  winnerUserId: string | null
}

export interface SamQuickChatEvent {
  eventId: string
  roomId?: string
  senderUserId: string
  senderDisplayName: string
  text: string
  sentAtUtc: string
}

export interface SamThrowEvent {
  eventId: string
  roomId?: string
  senderUserId: string
  senderDisplayName: string
  targetUserId: string
  type: string
  emoji: string
  sentAtUtc: string
}

function cleanHubError(error: unknown, fallback: string) {
  const raw = error instanceof Error ? error.message : String(error || '')
  const marker = 'HubException:'
  const index = raw.lastIndexOf(marker)
  const cleaned = (index >= 0 ? raw.slice(index + marker.length) : raw)
    .replace(/^\s*An unexpected error occurred invoking ['"][^'"]+['"] on the server\.\s*/i, '')
    .trim()
  return cleaned || fallback
}

export const useSamLocStore = defineStore('samloc', {
  state: () => ({
    match: null as SamStateView | null,
    hub: null as HubConnection | null,
    socialHub: null as HubConnection | null,
    currentMatchId: null as string | null,
    currentRoomId: null as string | null,
    chatEvents: [] as SamQuickChatEvent[],
    latestThrow: null as SamThrowEvent | null,
    rematchMatchId: null as string | null,
    busy: false,
    error: ''
  }),
  actions: {
    async ensureHub() {
      if (this.hub) return
      const hub = createSamLocHub()
      hub.on('MatchStateUpdated', (state: SamStateView) => {
        if (!this.currentMatchId || state.matchId === this.currentMatchId) {
          this.match = state
          this.currentMatchId = state.matchId
          this.error = ''
        }
      })
      hub.on('RematchStarted', (newMatchId: string) => { this.rematchMatchId = String(newMatchId) })
      hub.onreconnected(async () => {
        if (!this.currentMatchId) return
        try { await hub.invoke('JoinMatch', this.currentMatchId) } catch { /* HTTP recovery remains available */ }
      })
      await hub.start()
      this.hub = hub
    },

    async ensureSocialHub() {
      if (this.socialHub) return
      const hub = createSocialHub()
      hub.on('QuickChatReceived', (event: SamQuickChatEvent) => {
        if (!this.currentRoomId || (event.roomId && event.roomId !== this.currentRoomId)) return
        this.chatEvents = [...this.chatEvents, event].slice(-8)
        window.setTimeout(() => {
          this.chatEvents = this.chatEvents.filter(x => x.eventId !== event.eventId)
        }, 5000)
      })
      hub.on('ThrowReactionReceived', (event: SamThrowEvent) => {
        if (!this.currentRoomId || (event.roomId && event.roomId !== this.currentRoomId)) return
        this.latestThrow = event
        window.setTimeout(() => {
          if (this.latestThrow?.eventId === event.eventId) this.latestThrow = null
        }, 1400)
      })
      hub.onreconnected(async () => {
        if (!this.currentRoomId) return
        try { await hub.invoke('JoinRoom', this.currentRoomId) } catch { /* gameplay remains independent */ }
      })
      await hub.start()
      this.socialHub = hub
    },

    async joinSocialRoom(roomId: string) {
      await this.ensureSocialHub()
      if (this.currentRoomId && this.currentRoomId !== roomId && this.socialHub?.state === 'Connected') {
        try { await this.socialHub.invoke('LeaveRoom', this.currentRoomId) } catch { /* ignore stale cleanup */ }
      }
      await this.socialHub?.invoke('JoinRoom', roomId)
      this.currentRoomId = roomId
    },

    async initialize(matchId: string) {
      this.currentMatchId = matchId
      this.rematchMatchId = null
      this.error = ''
      await this.ensureHub()

      let hubJoinFailed = false
      try { await this.hub?.invoke('JoinMatch', matchId) }
      catch { hubJoinFailed = true }

      if (!this.match || this.match.matchId !== matchId) {
        const { data } = await api.get<SamStateView>(`/api/samloc/matches/${matchId}`)
        this.match = data
      }

      if (this.match) {
        try { await this.joinSocialRoom(this.match.roomId) }
        catch (e) { this.error = cleanHubError(e, 'Không kết nối được SocialService.') }
      }

      if (hubJoinFailed && !this.match) throw new Error('SignalR join failed')
    },

    async declareSam() {
      if (!this.match || !this.currentMatchId) return
      this.busy = true
      this.error = ''
      try {
        await this.hub?.invoke('DeclareSam', this.currentMatchId, this.match.version)
      } catch (e) {
        this.error = cleanHubError(e, 'Không báo Sâm được.')
        await this.refreshState()
        throw e
      } finally {
        this.busy = false
      }
    },

    async playCards(cardCodes: string[]) {
      if (!this.match || !this.currentMatchId) return
      this.busy = true
      this.error = ''
      try {
        await this.hub?.invoke('PlayCards', this.currentMatchId, cardCodes, this.match.version)
      } catch (e) {
        this.error = cleanHubError(e, 'Không đánh được bài.')
        await this.refreshState()
        throw e
      } finally {
        this.busy = false
      }
    },

    async passTurn() {
      if (!this.match || !this.currentMatchId) return
      this.busy = true
      this.error = ''
      try {
        await this.hub?.invoke('Pass', this.currentMatchId, this.match.version)
      } catch (e) {
        this.error = cleanHubError(e, 'Không bỏ lượt được.')
        await this.refreshState()
        throw e
      } finally {
        this.busy = false
      }
    },

    async refreshState() {
      if (!this.currentMatchId) return
      try {
        const { data } = await api.get<SamStateView>(`/api/samloc/matches/${this.currentMatchId}`)
        this.match = data
      } catch { /* retain latest snapshot */ }
    },

    async sendQuickChat(text: string) {
      if (!this.currentRoomId) return
      this.error = ''
      try { await this.socialHub?.invoke('SendQuickChat', this.currentRoomId, text) }
      catch (e) { this.error = cleanHubError(e, 'Không gửi được.'); throw e }
    },

    async throwReaction(type: string, targetUserId: string) {
      if (!this.currentRoomId) return
      this.error = ''
      try { await this.socialHub?.invoke('ThrowReaction', this.currentRoomId, type, targetUserId) }
      catch (e) { this.error = cleanHubError(e, 'Không ném được.'); throw e }
    },

    async leaveView() {
      if (this.hub?.state === 'Connected' && this.currentMatchId) {
        try { await this.hub.invoke('LeaveMatch', this.currentMatchId) } catch { /* ignore */ }
      }
      if (this.socialHub?.state === 'Connected' && this.currentRoomId) {
        try { await this.socialHub.invoke('LeaveRoom', this.currentRoomId) } catch { /* ignore */ }
      }

      this.match = null
      this.currentMatchId = null
      this.currentRoomId = null
      this.chatEvents = []
      this.latestThrow = null
      this.rematchMatchId = null
      this.error = ''
    }
  }
})
