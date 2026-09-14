import { defineStore } from 'pinia'
import type { HubConnection } from '@microsoft/signalr'
import { api } from '@/core/http/api'
import { createMediaHub } from '@/core/realtime/mediaHub'
export interface MediaTrack { videoId:string; title:string; channelTitle:string; thumbnail:string }
export interface MediaRoomState { roomId:string; current:MediaTrack|null; queue:MediaTrack[]; playing:boolean; positionSeconds:number; startedAtUtc:string|null; revision:number }
export const useMediaStore=defineStore('media',{state:()=>({hub:null as HubConnection|null,state:null as MediaRoomState|null,currentRoomId:null as string|null,error:''}),actions:{
  async ensureHub(){if(this.hub)return;const hub=createMediaHub();hub.on('MediaStateUpdated',(state:MediaRoomState)=>{if(!this.currentRoomId||state.roomId===this.currentRoomId)this.state=state});hub.onreconnected(async()=>{if(this.currentRoomId)try{this.state=await hub.invoke<MediaRoomState>('JoinRoom',this.currentRoomId)}catch{/* optional */}});await hub.start();this.hub=hub},
  async joinRoom(roomId:string){await this.ensureHub();if(this.currentRoomId&&this.currentRoomId!==roomId&&this.hub?.state==='Connected')try{await this.hub.invoke('LeaveRoom',this.currentRoomId)}catch{/* ignore */}this.state=await this.hub!.invoke<MediaRoomState>('JoinRoom',roomId);this.currentRoomId=roomId;this.error=''},
  async leaveRoom(){if(this.currentRoomId&&this.hub?.state==='Connected')try{await this.hub.invoke('LeaveRoom',this.currentRoomId)}catch{/* ignore */}this.currentRoomId=null;this.state=null},
  async search(q:string,shorts=false){const{data}=await api.get<MediaTrack[]>(shorts?'/api/media/shorts':'/api/media/search',{params:{q}});return data},
  async select(track:MediaTrack){if(this.currentRoomId)await this.hub?.invoke('Select',this.currentRoomId,track)},async queue(track:MediaTrack){if(this.currentRoomId)await this.hub?.invoke('Queue',this.currentRoomId,track)},async toggle(playing:boolean,position:number){if(this.currentRoomId)await this.hub?.invoke('Toggle',this.currentRoomId,playing,position)},async seek(position:number){if(this.currentRoomId)await this.hub?.invoke('Seek',this.currentRoomId,position)},async next(){if(this.currentRoomId)await this.hub?.invoke('Next',this.currentRoomId)},async clearQueue(){if(this.currentRoomId)await this.hub?.invoke('ClearQueue',this.currentRoomId)}
}})
