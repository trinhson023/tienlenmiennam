import { defineStore } from 'pinia'
import type { HubConnection } from '@microsoft/signalr'
import { api } from '@/core/http/api'
import { createSocialHub } from '@/core/realtime/socialHub'
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
  lastPlayedCards: string[]
  hand: string[]
  players: MatchPlayerView[]
  winnerOrder: string[]
}

export interface MatchQuickChatEvent {
  eventId: string
  roomId?: string
  senderUserId: string
  senderDisplayName: string
  text: string
  kind?: string
  sentAtUtc: string
}

export interface MatchThrowEvent {
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
  const hubIndex = raw.lastIndexOf(marker)
  const cleaned = (hubIndex >= 0 ? raw.slice(hubIndex + marker.length) : raw)
    .replace(/^\s*An unexpected error occurred invoking ['"][^'"]+['"] on the server\.\s*/i, '')
    .trim()
  return cleaned || fallback
}

export const useTienLenStore = defineStore('tienlen', {
  state: () => ({
    match: null as MatchStateView | null,
    hub: null as HubConnection | null,
    socialHub: null as HubConnection | null,
    currentMatchId: null as string | null,
    currentRoomId: null as string | null,
    chatEvents: [] as MatchQuickChatEvent[],
    latestThrow: null as MatchThrowEvent | null,
    rematchMatchId: null as string | null,
    busy: false,
    error: ''
  }),
  actions: {
    async ensureHub() {
      if (this.hub) return
      const hub = createTienLenHub()
      hub.on('MatchStateUpdated', (state: MatchStateView) => {
        if (!this.currentMatchId || state.matchId === this.currentMatchId) {
          state.lastPlayedCards ||= []
          this.match = state
          this.currentMatchId = state.matchId
          this.error = ''
        }
      })
      hub.on('RematchStarted', (newMatchId: string) => { this.rematchMatchId = String(newMatchId) })
      hub.onreconnected(async () => {
        if (!this.currentMatchId) return
        try { await hub.invoke('JoinMatch', this.currentMatchId) } catch { /* UI keeps latest snapshot */ }
      })
      await hub.start()
      this.hub = hub
    },
    async ensureSocialHub() {
      if (this.socialHub) return
      const hub = createSocialHub()
      hub.on('QuickChatReceived', (event: MatchQuickChatEvent) => {
        if (!this.currentRoomId || (event.roomId && event.roomId !== this.currentRoomId)) return
        this.chatEvents = [...this.chatEvents, event].slice(-8)
        window.setTimeout(() => { this.chatEvents = this.chatEvents.filter(x => x.eventId !== event.eventId) }, 5000)
      })
      hub.on('ThrowReactionReceived', (event: MatchThrowEvent) => {
        if (!this.currentRoomId || (event.roomId && event.roomId !== this.currentRoomId)) return
        this.latestThrow = event
        window.setTimeout(() => { if (this.latestThrow?.eventId === event.eventId) this.latestThrow = null }, 1400)
      })
      hub.onreconnected(async () => {
        if (!this.currentRoomId) return
        try { await hub.invoke('JoinRoom', this.currentRoomId) } catch { /* gameplay is independent from social reconnect */ }
      })
      await hub.start()
      this.socialHub = hub
    },
    async joinSocialRoom(roomId: string) {
      await this.ensureSocialHub()
      if (this.currentRoomId && this.currentRoomId !== roomId && this.socialHub?.state === 'Connected') {
        try { await this.socialHub.invoke('LeaveRoom', this.currentRoomId) } catch { /* ignore stale group cleanup */ }
      }
      await this.socialHub?.invoke('JoinRoom', roomId)
      this.currentRoomId = roomId
    },
    async initialize(matchId: string) {
      this.currentMatchId = matchId
      this.rematchMatchId = null
      this.error = ''
      await this.ensureHub()
      let matchJoinFailed = false
      try { await this.hub?.invoke('JoinMatch', matchId) }
      catch { matchJoinFailed = true }
      if (!this.match || this.match.matchId !== matchId) {
        const { data } = await api.get<MatchStateView>(`/api/tienlen/matches/${matchId}`)
        data.lastPlayedCards ||= []
        this.match = data
      }
      if (this.match) {
        try { await this.joinSocialRoom(this.match.roomId) }
        catch (e) { this.error = cleanHubError(e, 'Không kết nối được SocialService.') }
      }
      if (matchJoinFailed) throw new Error('SignalR join failed')
    },
    async playCards(cardCodes: string[]) {
      if (!this.match || !this.currentMatchId) return
      this.busy = true; this.error = ''
      try { await this.hub?.invoke('PlayCards', this.currentMatchId, cardCodes, this.match.version) }
      catch (e) {
        this.error = cleanHubError(e, 'Không đánh được bài.')
        try { const { data } = await api.get<MatchStateView>(`/api/tienlen/matches/${this.currentMatchId}`); data.lastPlayedCards ||= []; this.match = data } catch { /* ignore */ }
        throw e
      } finally { this.busy = false }
    },
    async passTurn() {
      if (!this.match || !this.currentMatchId) return
      this.busy = true; this.error = ''
      try { await this.hub?.invoke('Pass', this.currentMatchId, this.match.version) }
      catch (e) {
        this.error = cleanHubError(e, 'Không bỏ lượt được.')
        try { const { data } = await api.get<MatchStateView>(`/api/tienlen/matches/${this.currentMatchId}`); data.lastPlayedCards ||= []; this.match = data } catch { /* ignore */ }
        throw e
      } finally { this.busy = false }
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
