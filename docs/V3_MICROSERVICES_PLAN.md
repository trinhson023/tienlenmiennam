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
- M8 — Shared Social + Media + Stats ✅
- M8.1 — Room/table UX hotfix ✅ CLOSED (`6a311cb616a7d21358d11124e4e87a39f6cd59ea`)
- M9 — Sâm Lốc service ✅ CLOSED (`e1ef14945220df9cae30067420bc4e7e52f7c1d9`)
- M10 — Cờ Tướng service (optional for first V3 merge)
- M11 — Hardening
- M12 — CI/CD and release readiness

## M7 — Vue gameplay migration — CLOSED

Final scope completed on `feat/dotnet-vue-microservices`: premium Vue gameplay table, SVG cards, authoritative timer, bots, selection/sorting, VFX/SFX, social parity, same-room rematch, return-to-lobby lifecycle and recovery polish.

## M8 — Shared Social + Media + Stats — CLOSED

Phase 1 — Shared SocialService ✅ CLOSED (`1939cd6340ad8f1f616fc37d7544c580951fbdea`)
Phase 2 — Persistent Social + MediaService ✅ CLOSED (`0ee0ede7180a1f366dee0b5188bd5704f7e9b2a3`)
Phase 3 — StatisticsService ✅ CLOSED (`f202266257bf32767f56a560c4243dc6c0272d76`)
M8 final accepted feature baseline: `f202266257bf32767f56a560c4243dc6c0272d76`.

## M8.1 — Room / table UX hotfix

- room member can explicitly enter `/rooms/{roomId}/table` before a match exists
- one player may sit at the waiting table; host can start once the game minimum player count is reached
- leaving the table view does not relinquish the room seat
- host can kick human members while the room is Open
- host can delete an Open room
- a human may abandon an in-progress Tiến Lên match and immediately leave the Lobby room
- abandoned Tiến Lên seats become server-automated for the rest of that match; the abandoned account can no longer read hidden state, play/pass, or receive subsequent match broadcasts
- remaining humans keep playing; if the host leaves, Lobby host ownership transfers to the next human member
- Social/Media behavior is unchanged; it already follows Room membership and needs no M8.1 changes

Acceptance must verify wait-table navigation, kick/delete authorization, host transfer, mid-match abandon security, bot takeover automation, persistence/restart of abandoned-seat state, and ability for the departed user to join another room immediately.

## M9 — Sâm Lốc service

Phase 1 — Native Sâm domain ✅ CLOSED (`e63a0ca4d8f8b7407c2c305674fd4570869c8691`)
- 52-card model, 10-card deal, 2–4 players
- suit-independent rank comparison (`3 ... A 2`)
- single / pair / triple / straight / four-of-a-kind
- Sâm straight ordering supports `A23 < 234 < ... < QKA`; `KA2` is invalid
- four-of-a-kind chops one single 2 and higher four-of-a-kind beats lower
- no finishing with rank 2 remains a V1 rule-profile decision to validate before realtime work
- trick pass/reset semantics
- explicit Báo Sâm declaration state
- standard white-win shape detector
- snapshot/restore boundary for later persistence

Phase 2 — Application/API/realtime/persistence ✅ CLOSED (`a4591c426e96d222ed443075a4f52445b84c9d4c`)
- Lobby launcher routes `sam-loc` into SamLocService
- declaration timer, authoritative turn timer and bot automation
- PostgreSQL recovery, rematch catch-up and abandon/bot takeover
- REST + SignalR exposed through Gateway and Docker

Phase 3 — Vue + shared integration ✅ CLOSED (`e1ef14945220df9cae30067420bc4e7e52f7c1d9`)
- Vue Sâm board + responsive table parity
- room-level Social/Media available during Sâm gameplay
- Sâm `MatchCompleted` transactional outbox into StatisticsService
- lobby catalog enabled only once Vue route exists
- stats dock switches between Tiến Lên and Sâm by selected game

Rule decisions are frozen in `docs/SAM_LOC_RULES_V1.md` before realtime work proceeds.

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
- producer outbox ✅ introduced for Tiến Lên and Sâm stats events
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
- extract shared integration-contract package before additional game services publish `MatchCompleted`
- verify broker topology/queue bootstrap for first-deploy scenarios where a consumer queue has never existed before a producer publishes
- define competitive/statistics policy for abandoned seats (current casual behavior keeps the original human identity for match result aggregation while server automation controls the abandoned seat)

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
- [x] stats
- [x] Docker Compose core stack
- [x] core Tiến Lên unit/integration tests green
- [x] Sâm playable end-to-end
- [ ] LAN test from at least two devices
- [x] hidden-card projection reviewed through M5–M7 acceptance
- [x] legacy boardgame.io no longer required for the V3 Tiến Lên runtime

Cờ Tướng is desirable but not required for the first V3 merge; it may ship as V3.1.
