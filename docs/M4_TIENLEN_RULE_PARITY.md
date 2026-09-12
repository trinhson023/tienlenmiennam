# M4 — Tiến Lên rule parity contract

M4 ports the **behavior that exists in the legacy Node/boardgame.io game** into pure C# domain code before any SignalR gameplay is added.

## Card order

- Rank: `3 < 4 < 5 < 6 < 7 < 8 < 9 < T < J < Q < K < A < 2`.
- Suit: `S < C < D < H` (Bích < Tép < Rô < Cơ).
- Legacy card ids such as `3S`, `TC`, `AH`, `2H` are preserved by `CardCode`.

## Legal combinations

- single
- pair
- triple
- straight, length >= 3, never containing rank 2
- four-of-a-kind, except four 2s which the legacy validator rejects
- three consecutive pairs, never containing rank 2
- four consecutive pairs, never containing rank 2

## Chop parity

- single 2: three consecutive pairs, four-of-a-kind, or four consecutive pairs
- pair of 2s: four-of-a-kind or four consecutive pairs
- three consecutive pairs: four-of-a-kind or four consecutive pairs
- four-of-a-kind: four consecutive pairs

Normal combinations of the same type/size still compare by the highest card.

## Opening / finishing parity

- If 3♠ is dealt, its holder starts and the first accepted play must include 3♠.
- In 2/3-player games where 3♠ can be in the undealt cards, the holder of the lowest dealt card starts, matching the legacy setup fallback.
- A player may not finish the match with a play containing rank 2 (legacy `Thối Heo` rule).

## Trick / pass parity

- A player who passes is out for the rest of the current trick.
- Once only the last player who played remains active, the table is cleared and that player opens the next trick.
- If that trick winner has already finished, the next non-finished seat clockwise receives the new opening turn (`hưởng sái`).
- Match winner order is recorded as players finish; once `playerCount - 1` players have finished, the final remaining player is appended and the match ends.

## Explicitly not invented in M4

The legacy runtime does **not** currently implement a scoring/economy model, instant `tới trắng` declarations, or a separate `cóng` settlement system. M4 deliberately does not invent variant-specific rules that were not present in the reference implementation. Those can be added later behind an explicit rule profile after the team decides the exact house rules.

## M4 exit test

`TienLenService.Api/Dockerfile` runs `dotnet test TienLenService.Tests` during image build. Therefore `docker compose up --build` cannot produce the Tiến Lên image if the rule parity test suite is red.
