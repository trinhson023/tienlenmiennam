# Royal Game Platform V3 — Migration & Delivery Plan

> Long-lived branch: `feat/dotnet-vue-microservices`
>
> Goal: rebuild the current Tiến Lên application into a multi-game platform using .NET 8 microservices, Clean Architecture + DDD, Ocelot, SignalR, RabbitMQ/MassTransit, EF Core, PostgreSQL/SQL Server, Vue 3 + TypeScript + PrimeVue + Pinia + Vite.

## Non-negotiable rules

1. `main` remains the stable production/reference branch.
2. The V3 branch is not merged until the migration acceptance checklist is green.
3. The current Node/React/boardgame.io implementation remains available as a behavioral reference until Tiến Lên parity is achieved.
4. Each bounded context owns its business rules and persistence boundary.
5. Domain/Application projects cannot depend on EF Core, SignalR, Ocelot, RabbitMQ, or UI frameworks.
6. Game services never trust client-side rules. Every move is validated server-side.
7. Realtime gameplay uses SignalR. RabbitMQ is for cross-service asynchronous integration, not per-card/per-move transport.
8. Room lifetime and Match lifetime are separated: a Room can contain many Matches.
9. Sensitive player state is projected per viewer. Opponents never receive hidden hands.
10. Database provider is swappable in Infrastructure: PostgreSQL and SQL Server are both supported.

## Milestones

- M0 — Branch & architecture baseline ✅
- M1 — Buildable platform skeleton ✅
- M2 — Identity ✅
- M3 — Lobby / Room bounded context ✅
- M4 — Native .NET Tiến Lên domain ✅
- M5 — Tiến Lên application + realtime vertical slice ✅
- M6 — Timer, bot, reconnect, persistence ✅
- M7 — Vue gameplay migration ✅
- M8 — Shared Social + Media + Stats ⏭️
- M9 — Sâm Lốc service
- M10 — Cờ Tướng service (optional for first V3 merge)
- M11 — Hardening
- M12 — CI/CD and release readiness

## M7 — Vue gameplay migration — CLOSED

Final scope completed on `feat/dotnet-vue-microservices`:
- premium Vue gameplay table for 2–4 players
- SVG card assets and responsive hand/center rendering
- server-authoritative timer HUD
- bot seats / turn presentation
- staging, sorting and selection controls
- card/chop VFX + WebAudio SFX
- Quick Chat / custom taunts
- throwable reactions
- same-room rematch using a new Match under the same Room
- completed-match return-to-lobby lifecycle
- `LastCompletedMatchId` room semantics
- account-level social throttle within a service instance
- room lifecycle serialization within a service instance
- no center-card replay animation on initial load/reconnect
- reduced-motion accessibility behavior

M7 final acceptance baseline is the latest commit after the final polish pass. Phase 2 concurrency acceptance baseline before polish: `891aa33a76bdf19f4c13f8b674f7058e47d7f363`.

## M8 — Shared Social + Media + Stats

SocialService:
- move shared room social concerns out of game-specific runtime where appropriate
- room text chat
- quick taunts
- bomb/tomato/poop reactions
- cooldown / anti-spam
- SignalR `/hubs/social`

MediaService:
- YouTube search proxy
- room music queue/state
- Shorts search/state
- media state belongs to Room, not Match

StatisticsService:
- consumes `MatchCompleted` via RabbitMQ/MassTransit
- games played / wins / losses / win rate
- per-game rating-ready schema

## M9 — Sâm Lốc service

- 10-card deal
- legal combination model
- báo Sâm flow
- 2/chop rules
- turn/round semantics
- timer
- bot
- match history
- Vue board module

## M10 — Cờ Tướng service

- 9x10 board
- pieces and ownership
- legal move generation
- palace/river rules
- cannon capture
- horse blocking
- elephant restrictions
- kings-facing rule
- check/checkmate/stalemate policy

First release target: PvP before AI.

## M11 — Hardening

- idempotent event handling
- outbox/inbox if event reliability requires it
- structured Serilog logs
- correlation IDs
- health/readiness checks
- rate limits for social/media APIs
- CORS hardening
- secret handling
- database migrations in CI/CD
- integration tests with Testcontainers
- WebSocket proxy tests through Ocelot
- replace single-instance room lifecycle locking with DB/distributed coordination before horizontal scale
- replace in-memory social throttle with a distributed limiter before horizontal scale

## M12 — CI/CD and release readiness

CI:
- build every changed .NET service
- unit/integration tests
- Vue lint/type-check/build
- Docker image builds

CD later:
- service-by-service deployment
- migration gate
- health gate
- rollback strategy

## Merge gate into `main`

The V3 branch cannot replace the current app until all critical items below pass:

- [ ] Vue login/guest flow
- [x] Multi-game lobby foundation
- [x] Native .NET Tiến Lên with no boardgame.io runtime dependency
- [x] 2–4 players
- [x] Server-side turn timer
- [x] Bots
- [x] reconnect/recovery
- [x] same-room rematch
- [x] Social reactions/quick chat parity for Tiến Lên
- [ ] Music Room
- [ ] Shorts Lounge
- [x] persistent Tiến Lên match history/state
- [ ] stats
- [x] Docker Compose core stack
- [x] core Tiến Lên unit/integration tests green
- [ ] Sâm playable end-to-end
- [ ] LAN test from at least two devices
- [x] hidden-card projection reviewed through M5–M7 acceptance
- [x] legacy boardgame.io no longer required for the V3 Tiến Lên runtime

Cờ Tướng is desirable but not required for the first V3 merge; it may ship as V3.1.
