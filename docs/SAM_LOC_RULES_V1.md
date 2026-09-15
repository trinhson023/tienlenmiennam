# Sâm Lốc V1 — Server Rule Profile

This file freezes the rules implemented by `SamLocService.Domain` so later realtime/persistence/UI work does not silently change gameplay semantics.

## Core table rules

- 2–4 players.
- Standard 52-card deck; each player receives exactly 10 cards. Undealt cards are unused for that match.
- Suit never affects play strength. Rank order for singles/pairs/triples/four-of-a-kind is `3 < 4 < 5 < 6 < 7 < 8 < 9 < 10 < J < Q < K < A < 2`.
- Legal combinations: single, pair, triple, straight of 3+ cards, four-of-a-kind.
- Sâm straights use the Sâm-specific sequence: `A-2-3` is the smallest 3-card straight, then `2-3-4`, `3-4-5`, ... and `Q-K-A` is the largest 3-card straight. `K-A-2` is invalid; there is no wrap from high Ace back to 2.
- Longer straights follow the same window rule, e.g. `A-2-3-4`, `2-3-4-5`, ... up to the highest sequence ending in Ace.
- Normal blocking requires the same combination type and same card count, with a strictly higher Sâm straight/window strength or rank strength as appropriate.
- One four-of-a-kind may chop one single 2. A higher four-of-a-kind may beat a lower four-of-a-kind.
- One four-of-a-kind does **not** chop a pair of 2s in this V1 baseline.
- A player may not finish the match with a play containing any rank 2 in this V1 baseline; settlement/"thối 2" details remain deferred.
- Passing removes that player from the current trick. When only the last player remains active, that last player opens the next trick.
- The match ends as soon as the first player empties their hand.

## Báo Sâm baseline

- Every match starts in a `Declaring` phase.
- The first accepted `DeclareSam` owns the opening turn and closes the declaration window.
- If nobody declares before the application-layer deadline, the application must call `CloseDeclaration(starterPlayerId)`; the policy for selecting that starter lives outside the pure domain.
- A declarer is marked failed once another player legally blocks a play made by the declarer.
- A declarer is marked successful only when they become the winner without ever being blocked.
- Money/settlement consequences for successful or failed Báo Sâm are intentionally out of scope for this V1 domain slice.

## White-win detection

The domain recognizes the standard immediate-win shapes so later match orchestration can decide how/when to resolve them:

1. 10-card straight (`DragonStraight`) using the same Sâm straight ordering, including valid sequences containing 2 such as `A-2-3-4-5-6-7-8-9-10`
2. Four 2s
3. Ten cards of one color (all red or all black)
4. At least three triples inside the ten-card hand
5. Five pairs

Priority order is encoded by `WhiteWinType`: `DragonStraight > FourTwos > SameColor > ThreeTriples > FivePairs`.

## Deferred to later M9 phases

- declaration timeout and random/previous-winner starter policy
- báo-one guarding/penalty settlement
- cóng / thối / monetary settlement
- whether a final straight containing 2 is treated differently from finishing on a standalone/pair 2 under the target room rule profile
- bot strategy
- persistence, reconnect, authoritative turn timer
- SignalR API and Vue gameplay board
- publishing `MatchCompleted` into the shared statistics pipeline
