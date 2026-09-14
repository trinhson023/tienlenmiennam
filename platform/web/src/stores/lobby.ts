import { defineStore } from 'pinia'
import { api } from '@/core/http/api'
import { createLobbyHub } from '@/core/realtime/lobbyHub'
import type { HubConnection } from '@microsoft/signalr'

export interface GameCatalogItem { slug: string; displayName: string; icon: string; minPlayers: number; maxPlayers: number; isEnabled: boolean }
export interface RoomMember { userId: string; username: string; displayName: string; seatNumber: number; isHost: boolean; isBot: boolean; joinedAtUtc: string }
export interface RoomDetails { id: string; name: string; gameSlug: string; gameName: string; gameIcon: string; minPlayers: number; maxPlayers: number; status: string; hostUserId: string; activeMatchId: string | null; members: RoomMember[]; createdAtUtc: string; updatedAtUtc: string }
export interface RoomSummary { id: string; name: string; gameSlug: string; gameName: string; gameIcon: string; playerCount: number; maxPlayers: number; status: string; hostUserId: string; updatedAtUtc: string }

export const useLobbyStore = defineStore('lobby', {
  state: () => ({ games: [] as GameCatalogItem[], rooms: [] as RoomSummary[], currentRoom: null as RoomDetails | null, selectedGame: 'tien-len', busy: false, hub: null as HubConnection | null }),
  actions: {
    async loadGames() { const { data } = await api.get<GameCatalogItem[]>('/api/lobby/games'); this.games = data; if (!this.games.some(x => x.slug === this.selectedGame && x.isEnabled)) this.selectedGame = this.games.find(x => x.isEnabled)?.slug || this.games[0]?.slug || 'tien-len' },
    async loadRooms() { const { data } = await api.get<RoomSummary[]>('/api/lobby/rooms', { params: { game: this.selectedGame } }); this.rooms = data },
    async loadRoom(roomId: string) { const { data } = await api.get<RoomDetails>(`/api/lobby/rooms/${roomId}`); this.currentRoom = data; return data },
    async createRoom(name: string, maxPlayers?: number) { this.busy = true; try { const { data } = await api.post<RoomDetails>('/api/lobby/rooms', { gameSlug: this.selectedGame, name: name || null, maxPlayers: maxPlayers || null }); this.currentRoom = data; await this.subscribeRoom(data.id); await this.loadRooms(); return data } finally { this.busy = false } },
    async joinRoom(roomId: string) { this.busy = true; try { const { data } = await api.post<RoomDetails>(`/api/lobby/rooms/${roomId}/join`); this.currentRoom = data; await this.subscribeRoom(roomId); await this.loadRooms(); return data } finally { this.busy = false } },
    async addBot() { if (!this.currentRoom) return null; this.busy = true; try { const { data } = await api.post<RoomDetails>(`/api/lobby/rooms/${this.currentRoom.id}/bots`); this.currentRoom = data; return data } finally { this.busy = false } },
    async fillBots() { if (!this.currentRoom) return null; this.busy = true; try { const { data } = await api.post<RoomDetails>(`/api/lobby/rooms/${this.currentRoom.id}/bots/fill`); this.currentRoom = data; return data } finally { this.busy = false } },
    async removeBot(botUserId: string) { if (!this.currentRoom) return null; this.busy = true; try { const { data } = await api.delete<RoomDetails>(`/api/lobby/rooms/${this.currentRoom.id}/bots/${botUserId}`); this.currentRoom = data; return data } finally { this.busy = false } },
    async startCurrentMatch() { if (!this.currentRoom) return null; this.busy = true; try { const { data } = await api.post<RoomDetails>(`/api/lobby/rooms/${this.currentRoom.id}/start`); this.currentRoom = data; return data } finally { this.busy = false } },
    async leaveCurrentRoom() { if (!this.currentRoom) return; const roomId = this.currentRoom.id; this.busy = true; try { await api.post(`/api/lobby/rooms/${roomId}/leave`); await this.unsubscribeRoom(roomId); this.currentRoom = null; await this.loadRooms() } finally { this.busy = false } },
    async changeGame(slug: string) { this.selectedGame = slug; await this.loadRooms() },
    async ensureHub() {
      if (this.hub) return
      const hub = createLobbyHub()
      hub.on('RoomChanged', async () => { await this.loadRooms() })
      hub.on('RoomRemoved', async (roomId: string) => { if (this.currentRoom?.id === roomId) this.currentRoom = null; await this.loadRooms() })
      hub.on('RoomUpdated', (room: RoomDetails) => { if (this.currentRoom?.id === room.id) this.currentRoom = room })
      hub.on('MatchStarted', (matchId: string) => { if (this.currentRoom) this.currentRoom.activeMatchId = matchId })
      hub.onreconnected(async () => { try { await hub.invoke('SubscribeLobby'); if (this.currentRoom?.id) { await hub.invoke('SubscribeRoom', this.currentRoom.id); await this.loadRoom(this.currentRoom.id) } await this.loadRooms() } catch { /* ignore */ } })
      await hub.start(); await hub.invoke('SubscribeLobby'); this.hub = hub
    },
    async subscribeRoom(roomId: string) { await this.ensureHub(); await this.hub?.invoke('SubscribeRoom', roomId) },
    async unsubscribeRoom(roomId: string) { if (this.hub?.state === 'Connected') await this.hub.invoke('UnsubscribeRoom', roomId) },
    async loadCurrentRoom() { try { const { data } = await api.get<RoomDetails | null>('/api/lobby/rooms/me'); this.currentRoom = data; if (data?.id) await this.subscribeRoom(data.id) } catch { this.currentRoom = null } },
    async initialize() { await this.loadGames(); await this.loadRooms(); await this.ensureHub(); await this.loadCurrentRoom() }
  }
})
