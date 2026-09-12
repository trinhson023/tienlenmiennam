# Royal Game Platform V3 — Migration & Delivery Plan

> Long-lived branch: `feat/dotnet-vue-microservices`
>
> Goal: rebuild the current Tiến Lên application into a multi-game platform using the same architectural style as `xxtkidxx/DataManagementSPCSoftware`: .NET 8 microservices, Clean Architecture + DDD, Ocelot, SignalR, RabbitMQ/MassTransit, EF Core, PostgreSQL/SQL Server, Vue 3 + TypeScript + PrimeVue + Pinia + Vite.

## 1. Non-negotiable rules

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

## 2. Target topology

```text
Vue 3 Web
   |
   | HTTP + SignalR
   v
Ocelot API Gateway
   |
   +-- IdentityService
   +-- LobbyService
   +-- SocialService
   +-- MediaService
   +-- StatisticsService
   +-- TienLenService
   +-- SamService
   +-- CoTuongService

Cross-service events -> RabbitMQ / MassTransit
Persistence          -> PostgreSQL or SQL Server via EF Core
Containers           -> Docker Compose
```

## 3. Repository layout

```text
platform/
  ApiGateway/
  IdentityService/
  LobbyService/
  SocialService/            # milestone M5
  MediaService/             # milestone M5
  StatisticsService/        # milestone M8
  TienLenService/
  SamService/               # milestone M9
  CoTuongService/           # milestone M10+
  Shared/
  web/
  docker-compose.yml
  docker-compose.dev.yml
  .env.example
```

Every substantial service follows:

```text
<Service>/
  <Service>.Api/
  <Service>.Application/
  <Service>.Domain/
  <Service>.Infrastructure/
  <Service>.Tests/
  <Service>.sln
```

## 4. Milestones

### M0 — Branch & architecture baseline

Deliverables:
- Long-lived branch created from latest `main`.
- This plan committed.
- ADRs for architecture, database strategy, realtime strategy, Room-vs-Match semantics.
- Legacy app left untouched.

Exit criteria:
- Team agrees on service boundaries and dependency rules.

### M1 — Buildable platform skeleton

Deliverables:
- `ApiGateway` (.NET 8 + Ocelot).
- `IdentityService` Clean Architecture skeleton.
- `LobbyService` Clean Architecture skeleton.
- `TienLenService` Clean Architecture skeleton.
- Vue 3/TypeScript/Vite/Pinia/PrimeVue frontend shell.
- Root platform Docker Compose with PostgreSQL + RabbitMQ.
- `/health` on each .NET service.
- Runtime frontend config via `public/config.js`.

Exit criteria:
- `docker compose -f platform/docker-compose.yml up --build` builds and starts foundation containers.
- Gateway health route can reach downstream health endpoints.
- Vue shell loads through browser.

### M2 — Identity

Domain:
- User aggregate.
- Role, Permission.
- RefreshToken.
- Guest identity policy.

Application:
- Register.
- Login.
- Refresh.
- Logout.
- GetCurrentUser.
- UpdateProfile.

Infrastructure:
- EF Core DbContext.
- Postgres + SQL Server provider switch.
- PBKDF2 password hashing.
- JWT token generation.
- Refresh token persistence/revocation.
- Migrations.

API:
- `/api/auth/register`
- `/api/auth/login`
- `/api/auth/refresh`
- `/api/auth/logout`
- `/api/users/me`

Frontend:
- auth Pinia store.
- login/register pages.
- token refresh interceptor.
- protected routes.

Tests:
- password hash verification.
- refresh token rotation.
- expired/revoked token rejection.
- provider-agnostic repository tests.

### M3 — Lobby / Room bounded context

Domain:
- GameCatalogEntry.
- Room aggregate.
- RoomMember.
- RoomStatus.
- Host ownership.

Core invariants:
- Capacity 2–4 depending on game.
- Seat uniqueness.
- One active room membership per user initially.
- Room is independent from game Match.
- Rematch creates a new Match under the same Room.

API:
- list games.
- list rooms by game.
- create room.
- join/leave room.
- kick player (host).
- ready/unready.
- start match.

Realtime:
- LobbyHub groups.
- room list updates.
- player joined/left.
- ready-state updates.

Persistence:
- Rooms, RoomMembers, RoomMatchRefs.

### M4 — Native .NET Tiến Lên domain

Port the current JS rules into pure C# Domain code.

Value objects:
- Card.
- Rank.
- Suit.
- Combination.
- PlayerId / SeatNumber.

Aggregate:
- TienLenMatch.

Rule engine:
- single.
- pair.
- triple.
- straight.
- consecutive pairs.
- four-of-kind.
- two chopping rules.
- three-of-spades opening rule.
- pass and round-reset semantics.
- winner ordering.

Required unit tests before networking:
- card order matrix.
- every valid combination.
- every invalid malformed combination.
- higher/lower comparison.
- chop scenarios.
- opening 3♠.
- round reset.
- player finishing.
- 2/3/4 player game-over calculation.

### M5 — Tiến Lên application + realtime vertical slice

Application commands:
- CreateMatch.
- JoinMatch.
- StartMatch.
- PlayCards.
- PassTurn.
- Rematch.

Realtime:
- `/hubs/tienlen`.
- JoinMatchGroup.
- PlayCards.
- Pass.
- MatchStateUpdated.
- TurnChanged.
- MatchCompleted.

Security:
- authenticated identity mapped to seat.
- command authorization per match.
- no hidden cards in opponent projections.
- optimistic version number to reject stale commands.

First playable target:
- two real browser clients.
- no bot yet.
- server authoritative.

### M6 — Timer, bot, reconnect, persistence

Timer:
- 60-second server authoritative deadline.
- timeout worker.
- auto-pass if legal.
- otherwise lowest legal opening move.

Reconnect:
- SignalR automatic reconnect.
- current match snapshot retrieval.
- seat recovery using authenticated identity.

Bot:
- server-side bot engine, not a fake SignalR client.
- legal-action generator.
- simple heuristic policy first.
- bot delay for natural pacing.

Persistence:
- match snapshot checkpoints.
- completed match records.
- crash/restart recovery rules.

### M7 — Vue gameplay migration

Port the current casino UX concept from React to Vue, not a pixel-for-pixel component translation.

Modules:
- `modules/games/tienlen`.
- game Pinia store.
- SignalR adapter.
- cards / hand / center / seats.
- timer HUD.
- staging / sorting controls.
- game-over + rematch.

Parity checklist:
- 2–4 players.
- bots.
- card VFX.
- SFX.
- no center-card false replay animation.
- same-room rematch.

### M8 — Shared Social + Media + Stats

SocialService:
- room text chat.
- quick taunts.
- bomb/tomato/poop reactions.
- cooldown / anti-spam.
- SignalR `/hubs/social`.

MediaService:
- YouTube search proxy.
- room music queue/state.
- Shorts search/state.
- media state belongs to Room, not Match.

StatisticsService:
- consumes `MatchCompleted` via RabbitMQ/MassTransit.
- games played / wins / losses / win rate.
- per-game rating-ready schema.

### M9 — Sâm Lốc service

Reuse shared card primitives only where they are truly generic; Sâm rules remain inside SamService.

Deliverables:
- 10-card deal.
- legal combination model.
- báo Sâm flow.
- 2/chop rules.
- turn/round semantics.
- timer.
- bot.
- match history.
- Vue board module.

### M10 — Cờ Tướng service

Domain:
- 9x10 board.
- pieces and ownership.
- legal move generation.
- palace/river rules.
- cannon capture.
- horse blocking.
- elephant restrictions.
- kings-facing rule.
- check/checkmate/stalemate policy.

First release target:
- PvP before AI.

### M11 — Hardening

- idempotent event handling.
- outbox/inbox if event reliability requires it.
- structured Serilog logs.
- correlation IDs.
- health/readiness checks.
- rate limits for social/media APIs.
- CORS hardening.
- secret handling.
- database migrations in CI/CD.
- integration tests with Testcontainers.
- WebSocket proxy tests through Ocelot.

### M12 — CI/CD and release readiness

CI:
- build every changed .NET service.
- unit/integration tests.
- Vue lint/type-check/build.
- Docker image builds.

CD later:
- service-by-service deployment.
- migration gate.
- health gate.
- rollback strategy.

## 5. Database strategy

Default recommendation: PostgreSQL.

Provider selection remains in Infrastructure:

```json
"Database": {
  "Provider": "Postgres"
}
```

Supported values:
- `Postgres` / `PostgreSQL`
- `SqlServer` / `Mssql`

Development may run all schemas/databases in one server instance. Ownership is still isolated by service. No service is allowed to query another service's EF DbContext directly.

Initial logical ownership:
- IdentityService -> `identity`
- LobbyService -> `lobby`
- TienLenService -> `tienlen`
- SamService -> `sam`
- SocialService -> `social`
- MediaService -> `media`
- StatisticsService -> `statistics`

## 6. Communication rules

Use HTTP when:
- caller needs an immediate answer.
- command/query is directly user driven.

Use SignalR when:
- browser needs realtime room/game/social updates.

Use RabbitMQ when:
- publishing cross-service facts such as `MatchCompleted`.
- producer should not wait on consumer.

Never use RabbitMQ for every card click.

## 7. Testing pyramid

Domain tests are mandatory and fastest.

Application tests cover use cases with mocked ports.

Infrastructure tests cover EF mappings and provider behavior.

Integration tests cover API + DB + SignalR.

End-to-end tests cover two browser clients joining and playing.

## 8. Merge gate into `main`

The V3 branch cannot replace the current app until all critical items below pass:

- [ ] Vue login/guest flow.
- [ ] Multi-game lobby.
- [ ] Native .NET Tiến Lên with no boardgame.io runtime dependency.
- [ ] 2–4 human players.
- [ ] Server-side 60s timer.
- [ ] Bots.
- [ ] reconnect/recovery.
- [ ] same-room rematch.
- [ ] Social reactions/chat.
- [ ] Music Room.
- [ ] Shorts Lounge.
- [ ] persistent match history.
- [ ] stats.
- [ ] Docker Compose full stack.
- [ ] core unit/integration tests green.
- [ ] Sâm playable end-to-end.
- [ ] LAN test from at least two devices.
- [ ] security review of hidden-card projection.
- [ ] legacy boardgame.io no longer required for the V3 runtime.

Cờ Tướng is desirable but not required for the first V3 merge; it may ship as V3.1.

## 9. Local workflow

Recommended worktree:

```cmd
cd F:\TIENLENMIENNAM\tien-len
git fetch origin
git worktree add F:\TIENLENMIENNAM\royal-game-platform feat/dotnet-vue-microservices
```

Then all V3 work happens in:

```text
F:\TIENLENMIENNAM\royal-game-platform
```

while the stable/reference app remains in the original folder.

## 10. Current implementation status

- [x] M0 branch created.
- [x] Architecture plan committed.
- [ ] M1 platform skeleton.
- [ ] M2 Identity.
- [ ] M3 Lobby.
- [ ] M4 Tiến Lên Domain.
- [ ] M5 Tiến Lên realtime vertical slice.
- [ ] M6 timer/bot/reconnect.
- [ ] M7 Vue gameplay parity.
- [ ] M8 shared platform services.
- [ ] M9 Sâm.
- [ ] M10 Cờ Tướng.
- [ ] M11 hardening.
- [ ] M12 CI/CD.
