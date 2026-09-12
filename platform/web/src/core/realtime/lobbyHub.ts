import * as signalR from '@microsoft/signalr'
import { ACCESS_TOKEN_KEY } from '@/core/http/api'

const baseURL = window.__APP_CONFIG__?.apiBaseUrl || 'http://localhost:8888'

export function createLobbyHub() {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${baseURL}/hubs/lobby`, {
      accessTokenFactory: () => localStorage.getItem(ACCESS_TOKEN_KEY) || ''
    })
    .withAutomaticReconnect()
    .build()
}
