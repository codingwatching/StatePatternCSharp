# NoOpState Default

> **Status:** In Progress
> **Created:** 2026-03-28
> **Author:** Claude (agent)
> **Source:** `2603272256-monobehaviour-statemachine-plan-redteam.md` (Finding 1)
> **Related Projex:** `20260312-monobehaviour-statemachine-plan.md`
> **Worktree:** Yes

---

## Summary

Default `CurrentState` to `NoOpState` in the `StateMachine<T>` constructor and fill `SideTracks` with `NoOpState` in `MultiTrackStateMachine`. Eliminates the null-state crash path that exists from construction until the first `ChangeState()` call.

**Scope:** Core constructors only
**Estimated Changes:** 2 files

---

## Objective

### Problem / Gap / Need

`StateMachine<T>` starts with `CurrentState = null`. Any `Update()` call before the first `ChangeState()` throws `NullReferenceException`. The update methods already recognize `NoOpState` as a skip-condition (`is not NoOpState` checks in `UpdateMainState`, `FixedUpdateMainState`, `LateUpdateMainState`, `UpdateSideTracks`) — but the constructors never set it.

`NoOpState` exists (`StateMachine.NoOpState.cs`) as a sealed class with empty method bodies. It is never instantiated anywhere in the codebase — only used as a type-check sentinel. This plan makes it the actual default.

### Success Criteria

- [ ] `StateMachine<T>` constructor sets `CurrentState = new NoOpState()`
- [ ] `MultiTrackStateMachine<T, TRACK>` constructor fills `SideTracks` with `NoOpState` instances
- [ ] Calling `Update()` on a freshly constructed machine does not throw
- [ ] Breaking change documented: first `ChangeState()` delivers `NoOpState` as `previous` instead of `null`

### Out of Scope

- MB wrapper changes (separate: `20260312-monobehaviour-statemachine-plan.md`)
- Changes to `ObserverTransitionState` (validates its own FROM type — unaffected)
- Any new API surface

---

## Context

### Current State

**Core constructor (`StateMachine.cs:28-32`):**
```csharp
public StateMachine(T subject)
{
    Subject = subject;
    UpdatePaused = false;
}
// CurrentState is left null
```

**Update methods already skip NoOpState (`StateMachine.cs:355-362`):**
```csharp
protected void UpdateMainState()
{
    if (CurrentState is not NoOpState)
    {
        if (CurrentState == null)
            throw new NullReferenceException("CurrentState is null. Did you set a state after instantiate this controller?");
        else CurrentState.Update(this, Subject);
    }
}
```

All three update paths (`UpdateMainState`, `FixedUpdateMainState`, `LateUpdateMainState`) share this pattern. `UpdateSideTracks()` in `MultiTrackStateMachine` has the identical pattern.

**MultiTrack constructor (`MultiTrackStateMachine.cs:17-25`):**
```csharp
SideTracks = new IState[minMax.max + 1];
// Array elements default to null
```

### Key Files

| File | Role | Change Summary |
|------|------|----------------|
| `StateMachine.cs` | Core state machine | Constructor: set `CurrentState = new NoOpState()` |
| `MultiTrackStateMachine.cs` | Multi-track variant | Constructor: fill `SideTracks` with `NoOpState` |

### Dependencies

- **Requires:** None — `NoOpState` already exists
- **Blocks:** Nothing directly

### Constraints

- `CurrentState` must be set **before** `UpdatePaused = false` in the constructor. `UpdatePaused`'s setter calls `SendEvent()`, which pattern-matches `CurrentState is IEventReceiverState`. Already safe with null (current behavior), but cleaner with NoOpState set first.

### Assumptions

- `NoOpState` has an implicit parameterless constructor (verified)
- No existing consumer code depends on `CurrentState` being null at construction — null CurrentState is a crash path, not a feature (the error message itself says "Did you set a state?")

### Impact Analysis

- **Direct:** Two constructor changes
- **Adjacent:** First `ChangeState()` now passes `NoOpState` as `previous` instead of `null` to: `IState.OnEntered`, `OnStateChanging`/`OnStateChanged` events, `MainStateChangedEvent`. Consumer code checking `previous == null` for "first entry" needs `previous is NoOpState` instead.
- **Downstream:** All `StateMachine<T>` consumers benefit from crash-safe default.

---

## Implementation

### Overview

One line in each constructor. The `is not NoOpState` infrastructure is already in place — this plan just activates it.

### Step 1: Default NoOpState in StateMachine\<T\> Constructor

**Objective:** Eliminate the null-state crash path.
**Confidence:** High
**Depends on:** None

**Files:**
- `StateMachine.cs`

**Changes:**

```csharp
// Before (StateMachine.cs:28-32):
public StateMachine(T subject)
{
    Subject = subject;
    UpdatePaused = false;
}

// After:
public StateMachine(T subject)
{
    Subject = subject;
    CurrentState = new NoOpState();
    UpdatePaused = false;
}
```

**Rationale:** `NoOpState` is already the recognized "no state" sentinel. One line aligns initialization with the existing invariant. No new types, no new flags.

**Breaking change:** First `ChangeState()` passes `NoOpState` (not `null`) as `previous`. The `is not NoOpState` pattern for detecting this is already established in the codebase.

**Verification:** Construct a `StateMachine<T>`, call `Update()` without setting a state — silently returns. Then `ChangeState<SomeState>()` and verify `previous` in `OnEntered` is `NoOpState`.

**If this fails:** Revert the single line.

---

### Step 2: Default NoOpState in MultiTrackStateMachine Side Tracks

**Objective:** Apply the same safety to side track slots.
**Confidence:** High
**Depends on:** Step 1 (same pattern)

**Files:**
- `MultiTrackStateMachine.cs`

**Changes:**

```csharp
// Before (MultiTrackStateMachine.cs:24):
SideTracks = new IState[minMax.max + 1];

// After:
SideTracks = new IState[minMax.max + 1];
for (int i = 0; i < SideTracks.Length; i++)
    SideTracks[i] = new NoOpState();
```

**Rationale:** `UpdateSideTracks()` has the same `is not NoOpState` → throw-if-null pattern. Filling with NoOpState makes unset tracks safe.

**Verification:** Construct a `MultiTrackStateMachine`, call `Update()` without setting side tracks — silently skips all tracks.

**If this fails:** Revert the loop.

---

## Verification Plan

### Automated Checks

- [ ] Solution compiles
- [ ] Existing tests pass

### Manual Verification

- [ ] `Update()` on fresh `StateMachine<T>` — no throw
- [ ] `Update()` on fresh `MultiTrackStateMachine<T, TRACK>` — no throw
- [ ] First `ChangeState()` delivers `NoOpState` as `previous`

### Acceptance Criteria Validation

| Criterion | How to Verify | Expected Result |
|-----------|---------------|-----------------|
| NoOpState default | Read constructor | `CurrentState = new NoOpState()` present |
| SideTracks filled | Read MultiTrack constructor | Initialization loop present |
| Update-safe | Call `Update()` before `ChangeState()` | No exception |

---

## Rollback Plan

1. Revert `StateMachine.cs` constructor (one line)
2. Revert `MultiTrackStateMachine.cs` constructor (one loop)

---

## Notes

### Risks

- **Breaking change — NoOpState as previous:** Consumer states checking `previous == null` on first entry will silently miss the condition. Mitigation: this library is primarily consumed by the same author, and `is NoOpState` / `is not NoOpState` is already the established pattern throughout the update methods.

### Open Questions

(None)
