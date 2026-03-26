# IStateResolver & ChangeState\<S\> Parity

> **Status:** In Progress
> **Created:** 2026-03-12
> **Author:** Claude (agent)
> **Source:** `20260311-serializable-states-and-changestate-parity-proposal.md` (Option B accepted)
> **Related Projex:** `20260312-istate-extraction-plan.md` (prerequisite), `20260312-monobehaviour-statemachine-plan.md` (builds on this)
> **Worktree:** No
> **Reviewed:** 2026-03-26 — `2603261200-istate-resolver-plan-review.md`
> **Review Outcome:** Valid. One accuracy fix needed in Step 5 Before sample (`class` constraint omitted). Commit plan before executing.

---

## Summary

Drop the `new()` constraint from `ChangeState<S>()` and `ChangeSideTrackState<S>()`. Introduce `IStateResolver` as a pluggable fallback for cache misses — defaulting to `ActivatorStateResolver` (preserves current auto-create behaviour). Add `CacheByType(IState)` for runtime-typed registration. This enables `ScriptableObject`, `MonoBehaviour`, and constructor-configured states to participate in the type-based `ChangeState<S>()` API without holding instance references at call sites.

**Scope:** Core machine files + 2 new files. No Unity-specific code in this plan.
**Estimated Changes:** 3 files modified, 2 files created.

---

## Objective

### Problem / Gap / Need

`ChangeState<S>()` requires `where S : IState, new()`. The `new()` constraint prevents type-based transitions for states that cannot be default-constructed (ScriptableObjects, MonoBehaviours, constructor-injected states). The instance overload `ChangeState(IState state)` works but forces callers to hold references, defeating the ergonomic type-based API.

### Success Criteria

- [ ] `ChangeState<S>()` compiles without `new()` on `S`
- [ ] `ChangeSideTrackState<S>()` compiles without `new()` on `S`
- [ ] `ObserverTransitionState<H, FROM, TO>` compiles without `new()` on `TO`
- [ ] Default behaviour unchanged: POCO states with parameterless constructors still auto-create and cache on first `ChangeState<S>()` call
- [ ] `IStateResolver` interface exists with `Resolve(Type)` method
- [ ] `ActivatorStateResolver` is the default resolver on `StateMachine<T>`
- [ ] `CacheByType(IState)` exists for runtime-typed registration
- [ ] Pre-cached states (via `Cache<S>()` or `CacheByType()`) are used without hitting the resolver
- [ ] If a state type has no parameterless constructor and is not cached, `ActivatorStateResolver` throws a descriptive `MissingMethodException`

### Out of Scope

- Unity-specific resolvers (e.g., `SerializedStateResolver`) — separate plan / MB machine plan
- `MonoBehaviourStateMachine<T>` — separate plan
- Changes to `IPopupState` or popup state lifecycle

---

## Context

### Current State

After Plan 1 (`IState` extraction), the relevant signatures are:

```csharp
// StateMachine<T>
public virtual void ChangeState(IState state, object parameter = null)     // instance-based
public virtual void ChangeState<S>(object parameter = null) where S : IState, new()  // generic, auto-create
public void Cache<S>(S state) where S : IState                             // pre-register

// MultiTrackStateMachine<T, TRACK>
public virtual void ChangeSideTrackState(TRACK track, IState state, object parameter = null)
public virtual void ChangeSideTrackState<S>(TRACK track, object parameter = null) where S : IState, new()

// ObserverTransitionState<H, FROM, TO>
where TO : IState, new()
```

`ChangeState<S>()` does: check cache → miss → `new S()` → cache → transition.
`Cache<S>()` accepts compile-time typed `S` only.

### Key Files

| File | Role | Change Summary |
|------|------|----------------|
| `IStateResolver.cs` (new) | Resolver interface | `IState Resolve(Type stateType)` |
| `ActivatorStateResolver.cs` (new) | Default resolver | `Activator.CreateInstance(stateType)` |
| `StateMachine.cs` | Core machine | Add `StateResolver` property, drop `new()`, add `CacheByType`, update `ChangeState<S>` body |
| `MultiTrackStateMachine.cs` | Multi-track machine | Drop `new()` from `ChangeSideTrackState<S>`, use resolver |
| `StateMachine.ObserverTransitionState.cs` | Rx bridge | Drop `new()` from `TO` constraint |

### Dependencies

- **Requires:** `20260312-istate-extraction-plan.md` (IState must exist)
- **Blocks:** `20260312-monobehaviour-statemachine-plan.md` (uses `CacheByType` and resolver)

### Constraints

- Default resolver must preserve exact current behaviour for POCO states
- `Activator.CreateInstance` throws `MissingMethodException` for types without parameterless constructors — this is acceptable and descriptive
- `Cache<S>()` (compile-time typed) is kept alongside `CacheByType()` (runtime typed) — both are useful

### Assumptions

- Plan 1 has been executed: all references already use `IState`
- `System.Activator` is available in .NET Standard 2.1 (confirmed)

### Impact Analysis

- **Direct:** `StateMachine.cs`, `MultiTrackStateMachine.cs`, `ObserverTransitionState`
- **Adjacent:** `Cache<S>()` unchanged — still works. Instance-based `ChangeState(IState)` unchanged
- **Downstream:** User code calling `ChangeState<S>()` on POCO states — zero impact (resolver auto-creates). User code with custom states — can now drop `new()` from their own generic constraints if desired

---

## Implementation

### Overview

Create the resolver interface and default implementation first (Steps 1-2), then wire them into the machines (Steps 3-4), then relax `ObserverTransitionState` constraints (Step 5).

### Step 1: Create `IStateResolver`

**Objective:** Define the resolver contract.
**Confidence:** High
**Depends on:** None

**Files:**
- `IStateResolver.cs` (new)

**Changes:**

```csharp
using System;

namespace BAStudio.StatePattern
{
    /// <summary>
    /// Resolves state instances by type when a cache miss occurs in ChangeState&lt;S&gt;().
    /// The resolved instance is cached by the machine for future transitions.
    /// </summary>
    public interface IStateResolver
    {
        /// <summary>
        /// Create or retrieve a state instance for the given type.
        /// The returned instance must implement IState for the machine's T.
        /// </summary>
        object Resolve(Type stateType);
    }
}
```

Note: `Resolve` returns `object` rather than a generic `IState` because `IStateResolver` is not generic over `T` — this allows a single resolver to serve multiple machine types. The machine casts the result.

**Rationale:** Non-generic interface allows resolvers to be shared, stored in non-generic contexts, and implemented by Unity `ScriptableObject` (which can't be generic).

**Verification:** File exists, compiles.

**If this fails:** Delete file.

---

### Step 2: Create `ActivatorStateResolver`

**Objective:** Provide default resolver that preserves current auto-create behaviour.
**Confidence:** High
**Depends on:** Step 1

**Files:**
- `ActivatorStateResolver.cs` (new)

**Changes:**

```csharp
using System;

namespace BAStudio.StatePattern
{
    /// <summary>
    /// Default resolver that creates state instances via Activator.CreateInstance.
    /// Equivalent to `new S()` but without requiring the new() constraint at compile time.
    /// Throws MissingMethodException if the type has no parameterless constructor.
    /// </summary>
    public class ActivatorStateResolver : IStateResolver
    {
        public static readonly ActivatorStateResolver Instance = new ActivatorStateResolver();

        public object Resolve(Type stateType)
        {
            return Activator.CreateInstance(stateType);
        }
    }
}
```

**Rationale:** Singleton instance avoids allocation per machine. `Activator.CreateInstance` is the runtime equivalent of `new()` — same behaviour, same exceptions, no compile-time constraint.

**Verification:** `ActivatorStateResolver.Instance.Resolve(typeof(NoOpState))` returns a `NoOpState` instance.

**If this fails:** Delete file.

---

### Step 3: Wire resolver into `StateMachine<T>` and update `ChangeState<S>()`

**Objective:** Add `StateResolver` property, drop `new()`, add `CacheByType`, rewrite `ChangeState<S>()` to use resolver on cache miss.
**Confidence:** High
**Depends on:** Steps 1, 2

**Files:**
- `StateMachine.cs`

**Changes:**

**3a. Add `StateResolver` property** (after `DeliverOnlyOnceForCachedStates` property, ~line 50):

```csharp
// Before (after line 50):
public event Action<IState, IState> OnStateChanging;

// After:
public IStateResolver StateResolver { get; set; } = ActivatorStateResolver.Instance;
public event Action<IState, IState> OnStateChanging;
```

**3b. Drop `new()` and rewrite `ChangeState<S>()`** (~line 140):

```csharp
// Before:
public virtual void ChangeState<S>(object parameter = null) where S : IState, new()
{
    if (AutoStateCache == null) AutoStateCache = new Dictionary<Type, IState>();
    if (!AutoStateCache.ContainsKey(typeof(S)))
    {
        S newS = new S();
        AutoStateCache.Add(typeof(S), newS);
        if (DeliverOnlyOnceForCachedStates) DeliverComponents(newS);
    }

    var prev = CurrentState;
    var state = AutoStateCache[typeof(S)];
    PreStateChange(CurrentState, state, parameter);
    CurrentState = state;
    if (!DeliverOnlyOnceForCachedStates)
        DeliverComponents(state); // Though maybe not useful, calling this here give prev a chance to provide components
    state.OnEntered(this, prev, Subject, parameter);
    PostStateChange(prev);

    prev?.Reset();
}

// After:
public virtual void ChangeState<S>(object parameter = null) where S : IState
{
    if (AutoStateCache == null) AutoStateCache = new Dictionary<Type, IState>();
    if (!AutoStateCache.TryGetValue(typeof(S), out var state))
    {
        state = (IState) StateResolver.Resolve(typeof(S));
        AutoStateCache.Add(typeof(S), state);
        if (DeliverOnlyOnceForCachedStates) DeliverComponents(state);
    }

    var prev = CurrentState;
    PreStateChange(CurrentState, state, parameter);
    CurrentState = state;
    if (!DeliverOnlyOnceForCachedStates)
        DeliverComponents(state);
    state.OnEntered(this, prev, Subject, parameter);
    PostStateChange(prev);

    prev?.Reset();
}
```

Key changes:
- `where S : IState, new()` → `where S : IState`
- `new S()` → `(IState) StateResolver.Resolve(typeof(S))`
- `ContainsKey` + indexer → `TryGetValue` (single lookup instead of two)

**3c. Add `CacheByType(IState)`** (after existing `Cache<S>()`, ~line 257):

```csharp
// Add after Cache<S>():

/// <summary>
/// Cache the provided state instance, keyed by its runtime type.
/// Useful when the compile-time type is not known (e.g., auto-discovered MonoBehaviour states).
/// </summary>
public void CacheByType(IState state)
{
    if (AutoStateCache == null) AutoStateCache = new Dictionary<Type, IState>();
    AutoStateCache[state.GetType()] = state;
    if (DeliverOnlyOnceForCachedStates) DeliverComponents(state);
}
```

**Rationale:** `Cache<S>()` requires compile-time `S`. `GetComponents<IState>()` returns runtime-typed instances. `CacheByType` bridges this gap for bulk registration.

**Verification:**
- `ChangeState<NoOpState>()` still works (resolver creates via `Activator.CreateInstance`)
- `Cache<S>(instance); ChangeState<S>();` still works (cache hit, resolver not called)
- `CacheByType(instance); ChangeState<TypeOfInstance>()` works (cache hit)

**If this fails:** Revert `StateMachine.cs` via git.

---

### Step 4: Wire resolver into `MultiTrackStateMachine<T, TRACK>`

**Objective:** Drop `new()` from `ChangeSideTrackState<S>()`, use resolver.
**Confidence:** High
**Depends on:** Step 3

**Files:**
- `MultiTrackStateMachine.cs`

**Changes:**

```csharp
// Before:
public virtual void ChangeSideTrackState<S>(TRACK track, object parameter = null) where S : IState, new()
{
    if (AutoSideTrackStateCache == null) AutoSideTrackStateCache = new Dictionary<(TRACK, Type), IState>();
    (TRACK track, Type) key = (track, typeof(S));
    if (!AutoSideTrackStateCache.ContainsKey(key)) AutoSideTrackStateCache.Add(key, new S());
    ChangeSideTrackState(track, AutoSideTrackStateCache[key], parameter);
}

// After:
public virtual void ChangeSideTrackState<S>(TRACK track, object parameter = null) where S : IState
{
    if (AutoSideTrackStateCache == null) AutoSideTrackStateCache = new Dictionary<(TRACK, Type), IState>();
    (TRACK track, Type) key = (track, typeof(S));
    if (!AutoSideTrackStateCache.TryGetValue(key, out var state))
    {
        state = (IState) StateResolver.Resolve(typeof(S));
        AutoSideTrackStateCache.Add(key, state);
    }
    ChangeSideTrackState(track, state, parameter);
}
```

Key changes:
- `where S : IState, new()` → `where S : IState`
- `new S()` → `(IState) StateResolver.Resolve(typeof(S))`
- `ContainsKey` + indexer → `TryGetValue`
- `StateResolver` is inherited from `StateMachine<T>` — no additional property needed

**Rationale:** Side track state creation should follow the same resolver pattern as main state.

**Verification:** `ChangeSideTrackState<SomeState>(track)` creates and caches via resolver. Pre-cached side track states bypass resolver.

**If this fails:** Revert `MultiTrackStateMachine.cs` via git.

---

### Step 5: Drop `new()` from `ObserverTransitionState<H, FROM, TO>`

**Objective:** Remove the `new()` constraint on `TO` so non-constructible target states work.
**Confidence:** High
**Depends on:** Step 3 (ChangeState<S> no longer requires new())

**Files:**
- `StateMachine.ObserverTransitionState.cs`

**Changes:**

```csharp
// Before:
public abstract class ObserverTransitionState<H, FROM, TO> : IState, IObserver<H>
    where FROM : IState
    where TO : class, IState, new()

// After:
public abstract class ObserverTransitionState<H, FROM, TO> : IState, IObserver<H>
    where FROM : IState
    where TO : class, IState  // class constraint retained — states are reference types
```

The `Update()` method already handles both paths:

```csharp
if (instancedNext != null) machine.ChangeState(instancedNext, parameter);
else machine.ChangeState<TO>(parameter);
```

With `new()` removed from `TO`, `machine.ChangeState<TO>()` now works because `ChangeState<S>` itself no longer requires `new()` — it uses the resolver.

**Rationale:** The `new()` constraint on `TO` was only needed because `ChangeState<S>()` required it. With the resolver in place, the constraint is unnecessary. Target states can be pre-cached or resolver-provided.

**Verification:** `ObserverTransitionState<H, SomeState, SomeSOState>` compiles where `SomeSOState` has no parameterless constructor (as long as it's cached or resolver-provided).

**If this fails:** Revert the single line change via git.

---

## Verification Plan

### Automated Checks

- [ ] `dotnet build` succeeds
- [ ] No remaining `new()` constraints on state-related generic methods (grep for `new()` in `StateMachine.cs`, `MultiTrackStateMachine.cs`, `ObserverTransitionState`)

### Manual Verification

- [ ] `StateResolver` property exists on `StateMachine<T>`, defaults to `ActivatorStateResolver.Instance`
- [ ] `ChangeState<S>()` signature has `where S : IState` (no `new()`)
- [ ] `CacheByType(IState)` method exists
- [ ] `IStateResolver.cs` and `ActivatorStateResolver.cs` exist and compile

### Acceptance Criteria Validation

| Criterion | How to Verify | Expected Result |
|-----------|---------------|-----------------|
| POCO auto-create preserved | Read `ChangeState<S>` body — resolver fallback uses `Activator.CreateInstance` | Same runtime behaviour as `new S()` |
| Cache-first | Read `ChangeState<S>` body — `TryGetValue` before resolver | Cache hit skips resolver |
| `CacheByType` works | Read method — keys by `state.GetType()` | Runtime-typed registration |
| `new()` gone | Grep constraints | Zero `new()` on state methods |
| Compiles | `dotnet build` | Success |

---

## Rollback Plan

1. Delete `IStateResolver.cs` and `ActivatorStateResolver.cs`
2. `git checkout -- StateMachine.cs MultiTrackStateMachine.cs StateMachine.ObserverTransitionState.cs`

---

## Notes

### Risks

- **`Activator.CreateInstance` performance**: ~10x slower than `new()` for the first call. Amortized to zero by caching — each type is created exactly once. Not a practical concern.
- **Runtime vs compile-time error**: Previously, calling `ChangeState<S>()` on a type without a parameterless constructor was a compile error. Now it's a runtime `MissingMethodException`. This is the deliberate trade-off for flexibility. The exception message is descriptive.

### Open Questions

(None)
