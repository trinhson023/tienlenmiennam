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
- M8 — Shared Social + Media + Stats ⏳ Phase 3 acceptance
- M9 — Sâm Lốc service
- M10 — Cờ Tướng service (optional for first V3 merge)
- M11 — Hardening
- M12 — CI/CD and release readiness

## M7 — Vue gameplay migration — CLOSED

Final scope completed on `feat/dotnet-vue-microservices`: premium Vue gameplay table, SVG cards, authoritative timer, bots, selection/sorting, VFX/SFX, social parity, same-room rematch, return-to-lobby lifecycle and recovery polish.

## M8 — Shared Social + Media + Stats

Phase 1 — Shared SocialService ✅ CLOSED (`1939cd6340ad8f1f616fc37d7544c580951fbdea`)
- room chat / quick taunts / reactions
- SignalR `/hubs/social`
- room-authoritative sender + receiver authorization
- persistent history added in Phase 2

Phase 2 — Persistent Social + MediaService ✅ CLOSED (`0ee0ede7180a1f366dee0b5188bd5704f7e9b2a3`)
- PostgreSQL social history with 50-message retention
- YouTube server-side search proxy with room-scoped quota protection
- room-owned Music state / queue / seek / playback recovery
- Shorts Lounge
- persistent Media state across restart/rematch

Phase 3 — StatisticsService ⏳ acceptance
- Tiến Lên writes `MatchCompleted` into a transactional DB outbox on the same save that marks the match Completed
- MassTransit publishes the outbox to RabbitMQ
- StatisticsService consumes `MatchCompleted` idempotently using `processed_matches`
- human-only games/wins/losses/win-rate aggregation; bots do not get player stats
- rating-ready per-game schema (`Rating` starts at 1000; no rating algorithm yet)
- authenticated self stats / user stats / leaderboard endpoints
- compact lobby Stats dock

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

- idempotent event handling ✅ introduced for Statistics `MatchCompleted`
- outbox/inbox if event reliability requires it ✅ producer outbox introduced for Tiến Lên stats events
- structured Serilog logs
- correlation IDs
- health/readiness checks
- rate limits for social/media APIs
- CORS hardening
- secret handling
- database migrations in CI/CD
- integration tests with Testcontainers
- WebSocket proxy tests through Ocelot
- replace single-instance room/media lifecycle locking with DB/distributed coordination before horizontal scale
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
- [x] Music Room
- [x] Shorts Lounge
- [x] persistent Tiến Lên match history/state
- [ ] stats (Phase 3 runtime acceptance pending)
- [x] Docker Compose core stack
- [x] core Tiến Lên unit/integration tests green
- [ ] Sâm playable end-to-end
- [ ] LAN test from at least two devices
- [x] hidden-card projection reviewed through M5–M7 acceptance
- [x] legacy boardgame.io no longer required for the V3 Tiến Lên runtime

Cờ Tướng is desirable but not required for the first V3 merge; it may ship as V3.1.
