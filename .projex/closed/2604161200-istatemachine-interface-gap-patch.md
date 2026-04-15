# Patch: Promote T1/T2 Members + SendEvent<S,E> to IStateMachine<T>

> **Date:** 2026-04-16
> **Author:** BA
> **Directive:** add t1, t2, sendevent<S,E> — promote Tier 1, Tier 2, and targeted SendEvent overload from StateMachine<T> to IStateMachine<T>
> **Source Plan:** Direct (from conversation proposal)
> **Result:** Success

---

## Summary

Closed the gap between `IStateMachine<T>` and `StateMachine<T>` for setup/config members that previously required a concrete cast. Five members promoted: two setup operations (cache + component injection), two behavioral config flags, and one targeted event-dispatch overload.

---

## Changes

### IStateMachine<T> — promoted members

**File:** `IStateMachine.cs`
**Change Type:** Modified

**What Changed:**
- Doc comment: removed "component delivery, caching" from cast-to-concrete note; only "debug" remains
- Line 38+: added `bool SendEvent<S, E>(E ev, bool shouldThrow) where S : IState<T>;` after base `SendEvent<E>`
- After events block, added:
  - `void Cache<S>(S state) where S : IState<T>;`
  - `void SetComponent<PT, CT>(CT obj) where CT : PT;`
  - `IStateResolver StateResolver { get; set; }`
  - `bool DeliverOnlyOnceForCachedStates { get; set; }`

**Why:** Consumers holding `IStateMachine<T>` (e.g. states calling back on their machine, test doubles, wrappers) could not access caching, component injection, resolver config, or targeted event dispatch without casting to the concrete `StateMachine<T>`. These are all user-facing operations with no implementation-specific reason to hide behind the concrete type.

---

## Verification

**Method:** Manual review — members exist on `StateMachine<T>` with matching signatures; no other implementors of `IStateMachine<T>` exist in the codebase (confirmed during gap research).

**Status:** PASS — interface compiles; `StateMachine<T>` satisfies all new members without modification.

---

## Impact on Related Projex

| Document | Relationship | Update Made |
|----------|-------------|-------------|
| `2603251200-statepatterncsharp-roadmap-nav.md` | Phase 4 addendum | Noted patch under Phase 4 history |

---

## Notes

Excluded from this patch (intentionally not promoted):
- `CacheByType(IState<T>)` — redundant with generic `Cache<S>`
- `Peers` / `Link` / `Unlink` — `Link`/`Unlink` are static; peer view promotion deferred
- `DebugOutput` / `DebugFlags` — implementation detail, not a contract concern
- Protected hooks (`PreStateChange`, `PostStateChange`) — subclass surface only
