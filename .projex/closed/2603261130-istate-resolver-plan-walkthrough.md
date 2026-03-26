# Walkthrough: IStateResolver & ChangeState<S> Parity

> **Execution Date:** 2026-03-26
> **Completed By:** Claude (agent)
> **Source Plan:** `20260312-istate-resolver-plan.md`
> **Duration:** ~30 min (single session)
> **Result:** Success

---

## Summary

Dropped the `new()` constraint from `ChangeState<S>()`, `ChangeSideTrackState<S>()`, and `ObserverTransitionState<H, FROM, TO>`. Introduced `IStateResolver` / `ActivatorStateResolver` as a pluggable resolver layer that preserves existing POCO auto-create behaviour. Added `CacheByType(IState)` for runtime-typed registration. All 9 acceptance criteria passed; no deviations from plan.

---

## Objectives Completion

| Objective | Status | Notes |
|-----------|--------|-------|
| Drop `new()` from `ChangeState<S>()` | Complete | Constraint now `where S : IState` |
| Drop `new()` from `ChangeSideTrackState<S>()` | Complete | Constraint now `where S : IState` |
| Drop `new()` from `ObserverTransitionState TO` | Complete | Constraint now `where TO : class, IState` |
| Introduce `IStateResolver` interface | Complete | `object Resolve(Type)` |
| Introduce `ActivatorStateResolver` as default | Complete | Singleton, wired as default on `StateMachine<T>` |
| Add `CacheByType(IState)` | Complete | Keys by `state.GetType()` |
| Preserve POCO auto-create behaviour | Complete | `Activator.CreateInstance` is runtime equivalent of `new()` |

---

## Execution Detail

### Step 1: Create IStateResolver

**Planned:** New file `IStateResolver.cs` with `object Resolve(Type stateType)` in `BAStudio.StatePattern`.

**Actual:** Created exactly as planned.

**Deviation:** None.

**Files Changed:**
| File | Change Type | Planned? | Details |
|------|-------------|----------|---------|
| `IStateResolver.cs` | Created | Yes | 17 lines — interface with single `Resolve(Type)` method |

**Verification:** File created, committed cf23c57.

---

### Step 2: Create ActivatorStateResolver

**Planned:** New file `ActivatorStateResolver.cs` with singleton `Instance` and `Resolve` delegating to `Activator.CreateInstance`.

**Actual:** Created exactly as planned.

**Deviation:** None.

**Files Changed:**
| File | Change Type | Planned? | Details |
|------|-------------|----------|---------|
| `ActivatorStateResolver.cs` | Created | Yes | 19 lines — singleton, `Activator.CreateInstance(stateType)` |

**Verification:** File created, committed 64539ec.

---

### Step 3: Wire resolver into StateMachine.cs

**Planned:** (3a) Add `StateResolver` property after `DeliverOnlyOnceForCachedStates`. (3b) Drop `new()` from `ChangeState<S>()`, replace `new S()` with resolver call, replace `ContainsKey`+indexer with `TryGetValue`. (3c) Add `CacheByType(IState)` after `Cache<S>()`.

**Actual:** All three sub-changes applied exactly as planned.

**Deviation:** None.

**Files Changed:**
| File | Change Type | Planned? | Details |
|------|-------------|----------|---------|
| `StateMachine.cs` | Modified | Yes | +1 property (line ~51), `ChangeState<S>` rewritten (line ~140), +`CacheByType` method (line ~259) — 23 net change |

**Verification:** Code review of modified sections confirmed correct. Committed 510d8db.

---

### Step 4: Wire resolver into MultiTrackStateMachine.cs

**Planned:** Drop `new()` from `ChangeSideTrackState<S>()`, replace `ContainsKey`+indexer with `TryGetValue`, use `StateResolver.Resolve`.

**Actual:** Applied exactly as planned. `StateResolver` is inherited — no additional property needed.

**Deviation:** None.

**Files Changed:**
| File | Change Type | Planned? | Details |
|------|-------------|----------|---------|
| `MultiTrackStateMachine.cs` | Modified | Yes | Lines 52–58: method expanded from 3 to 7 lines, `new S()` replaced, TryGetValue used |

**Verification:** Code review confirmed. Committed 6fed84f.

---

### Step 5: Drop new() from ObserverTransitionState

**Planned:** Remove `new()` from `TO` constraint, retain `class`.

**Actual:** Single-line change. The review (2603261200) had flagged the plan's "Before" sample as missing the `class` constraint — actual code already had it, so the fix was applied correctly: only `new()` removed, `class` retained.

**Deviation:** None.

**Files Changed:**
| File | Change Type | Planned? | Details |
|------|-------------|----------|---------|
| `StateMachine.ObserverTransitionState.cs` | Modified | Yes | Line 7: `where TO : class, IState, new()` → `where TO : class, IState` |

**Verification:** Confirmed. Committed a9f4e82.

---

## Complete Change Log

> **Derived from:** `git diff --stat master..HEAD`
> **Total:** 7 files, 103 insertions, 11 deletions

### Files Created
| File | Purpose | Lines | In Plan? |
|------|---------|-------|----------|
| `IStateResolver.cs` | Resolver contract — `object Resolve(Type)` | 17 | Yes |
| `ActivatorStateResolver.cs` | Default resolver via `Activator.CreateInstance`; singleton | 19 | Yes |

### Files Modified
| File | Changes | Lines Affected | In Plan? |
|------|---------|----------------|----------|
| `StateMachine.cs` | `StateResolver` property added; `ChangeState<S>` constraint relaxed + body rewritten; `CacheByType` added | ~51, ~140–160, ~259–268 | Yes |
| `MultiTrackStateMachine.cs` | `ChangeSideTrackState<S>` constraint relaxed + body rewritten | 52–67 | Yes |
| `StateMachine.ObserverTransitionState.cs` | `TO` constraint: dropped `new()` | 7 | Yes |
| `.projex/20260312-istate-resolver-plan.md` | Status: Ready → In Progress → Complete; completion metadata added | header | Yes (lifecycle) |
| `.projex/20260312-istate-resolver-plan-log.md` | Execution log created and updated per step | all | Yes (lifecycle) |

### Files Deleted
_(none)_

### Planned But Not Changed
_(none — all planned files were changed as expected)_

---

## Success Criteria Verification

| Criterion | Method | Result | Evidence |
|-----------|--------|--------|----------|
| `ChangeState<S>()` — no `new()` | Code inspection | PASS | `where S : IState` in `StateMachine.cs` |
| `ChangeSideTrackState<S>()` — no `new()` | Code inspection | PASS | `where S : IState` in `MultiTrackStateMachine.cs` |
| `ObserverTransitionState TO` — no `new()` | Code inspection | PASS | `where TO : class, IState` in `ObserverTransitionState.cs` |
| Default behaviour preserved | Code inspection | PASS | `ActivatorStateResolver` uses `Activator.CreateInstance` — same as `new()` at runtime |
| `IStateResolver` with `Resolve(Type)` | File existence + inspection | PASS | `IStateResolver.cs` created |
| `ActivatorStateResolver` is default | Code inspection | PASS | `StateMachine.cs:~51` — `StateResolver { get; set; } = ActivatorStateResolver.Instance` |
| `CacheByType(IState)` exists | Code inspection | PASS | `StateMachine.cs:~259` |
| Pre-cached states bypass resolver | Code inspection | PASS | `TryGetValue` exits before resolver call |
| Uncached + no ctor → `MissingMethodException` | Logical inference | PASS | `Activator.CreateInstance` documented behaviour |
| No new build errors introduced | `dotnet build StatePattern.csproj` | PASS | 1 pre-existing Unity error in `EnumExtension.cs` confirmed pre-existing |
| No remaining `new()` on state methods | `grep new() *.cs` | PASS | Only remaining instance: `Popup<S>() where S : IPopupState, new()` — out of scope |

**Overall: 9/9 criteria passed**

---

## Deviations from Plan

None.

---

## Issues Encountered

### Unity Build Error (pre-existing)
- **Description:** `dotnet build StatePattern.csproj` produces `error CS0246: 'Unity' namespace not found` in `EnumExtension.cs`.
- **Severity:** Low (pre-existing, unrelated to this plan)
- **Resolution:** Confirmed pre-existing by verifying the error existed on `master` before our changes.
- **Prevention:** N/A — file requires Unity runtime to compile; standalone `dotnet build` will always fail on this file.

---

## Key Insights

### Pattern Discoveries

1. **Resolver-as-pluggable-factory**
   - Observed in: `StateMachine<T>.StateResolver`
   - Description: Non-generic `IStateResolver` with `object Resolve(Type)` allows a single resolver instance to serve multiple generic machine types, and is implementable by Unity ScriptableObject (which can't be generic).
   - Reuse potential: Same pattern applies to `MonoBehaviourStateMachine` resolver discovery (see `20260312-monobehaviour-statemachine-plan.md`).

2. **TryGetValue over ContainsKey+indexer**
   - Description: Replaced two-lookup pattern (`ContainsKey` + `[]`) with single `TryGetValue` call in both `ChangeState<S>` and `ChangeSideTrackState<S>`.
   - Reuse potential: Any future cache-miss pattern in this codebase.

### Technical Insights

- The `new()` constraint on `ChangeState<S>` was purely a mechanism for `new S()` — once the resolver absorbs instance creation, the constraint has no remaining purpose.
- `class` constraint on `ObserverTransitionState TO` is independently meaningful (states are reference types) and was correctly retained even after removing `new()`.
- Runtime error (`MissingMethodException`) vs compile error (`new()` constraint violation) is the deliberate trade-off — the flexibility to use non-constructible states justifies the shift.

---

## Recommendations

### Immediate Follow-ups
- [ ] Execute `20260312-monobehaviour-statemachine-plan.md` — uses `CacheByType` and `IStateResolver` from this plan

### Future Considerations
- `SerializedStateResolver` (Unity-specific, auto-populates from `GetComponents<IState>()`) is noted as out-of-scope here; belongs in the MB machine plan.

---

## References

- Source plan: `20260312-istate-resolver-plan.md`
- Review: `2603261200-istate-resolver-plan-review.md`
- Source proposal: `20260311-serializable-states-and-changestate-parity-proposal.md`
- Dependent plan: `20260312-monobehaviour-statemachine-plan.md`
- Commits: cf23c57 → 64539ec → 510d8db → 6fed84f → a9f4e82 → 60ad0c4
