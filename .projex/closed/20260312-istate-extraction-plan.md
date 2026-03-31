# Extract `IState` Interface from `StateMachine<T>.State`

> **Status:** Complete
> **Completed:** 2026-03-25
> **Walkthrough:** `20260325-istate-extraction-walkthrough.md`
> **Audit:** `20260325-istate-extraction-audit.md`
> **Created:** 2026-03-12
> **Author:** Claude (agent)
> **Source:** `20260311-state-to-istate-interface-proposal.md` (Option A accepted)
> **Related Projex:** `20260311-serializable-states-and-changestate-parity-proposal.md`, `20260311-monobehaviour-statemachine-proposal.md`
> **Worktree:** No

---

## Summary

Convert the nested `abstract class State` inside `StateMachine<T>` to `interface IState`. Purely mechanical — rename the type, change syntax from class to interface, update all references across 7 files. No semantic changes to transition logic, caching, or event dispatch.

**Scope:** All `.cs` files in the library that reference the `State` type. Excludes `IPopupState` (already an interface, left as-is per resolved question).
**Estimated Changes:** 7 files modified, 1 file renamed.

---

## Objective

### Problem / Gap / Need

`State` is an abstract class with zero fields and zero implementation (aside from empty-body virtual methods under `#if UNITY`). It consumes the single-inheritance slot, preventing states from extending `ScriptableObject`, `MonoBehaviour`, or any domain base class. Converting to `IState` removes this barrier.

### Success Criteria

- [ ] `StateMachine<T>.State` no longer exists; `StateMachine<T>.IState` is an interface
- [ ] `FixedUpdate` and `LateUpdate` are default interface methods (C# 8) under `#if UNITY_2017_1_OR_NEWER`
- [ ] `NoOpState` implements `IState` (sealed class, not inheriting abstract class)
- [ ] `ObserverTransitionState<H, FROM, TO>` implements `IState` with updated generic constraints
- [ ] All event structs (`MainStateChangedEvent`, `SideTrackStateChangedEvent`) use `IState` field types
- [ ] All `StateMachine<T>` and `MultiTrackStateMachine<T, TRACK>` signatures use `IState`
- [ ] Project compiles without errors (both Unity and non-Unity configurations)

### Out of Scope

- `IPopupState` — left as-is
- `IEventReceiverState`, `IComponentUser`, attributes — no references to `State`; unaffected
- `IStateResolver` / `ChangeState<S>` constraint changes — separate plan
- Popup state events (`NewPopupStateEvent`, `PopupStateEndedEvent`) — use `IPopupState`, unaffected

---

## Context

### Current State

`State` is defined in `StateMachine.State.cs` as:

```csharp
public abstract class State
{
    public abstract void OnEntered(IStateMachine<T> machine, State previous, T subject, object parameter = null);
    public abstract void Update(IStateMachine<T> machine, T subject);
    public abstract void OnLeaving(IStateMachine<T> machine, State next, T subject, object parameter = null);
    public abstract void Reset();
#if UNITY_2017_1_OR_NEWER
    public virtual void FixedUpdate(IStateMachine<T> machine, T subject) {}
    public virtual void LateUpdate(IStateMachine<T> machine, T subject) {}
#endif
}
```

All methods are either abstract (no body) or virtual with empty bodies. Zero fields, zero properties.

### Key Files

| File | Role | Change Summary |
|------|------|----------------|
| `StateMachine.State.cs` | State type definition | `abstract class` → `interface`, rename file to `StateMachine.IState.cs` |
| `StateMachine.cs` | Core machine logic | ~20 references: `State` → `IState` in properties, methods, events, caches |
| `StateMachine.NoOpState.cs` | Null-object state | `: State` → `: IState`, `override` → (remove keyword) |
| `StateMachine.ObserverTransitionState.cs` | Rx bridge state | `: State` → `: IState`, generic constraints updated |
| `StateMachine.MainStateChangedEvent.cs` | Transition event | `State from, to` → `IState from, to` |
| `MultiTrackStateMachine.cs` | Multi-track machine | ~15 references: `State` → `IState` in arrays, caches, methods, events |
| `MultiTrackStateMachine.SideTrackStateChangedEvent.cs` | Side track event | `State from, to` → `IState from, to` |

### Dependencies

- **Requires:** None — this is the foundation
- **Blocks:** `20260311-serializable-states-and-changestate-parity-proposal.md`, `20260311-monobehaviour-statemachine-proposal.md`

### Constraints

- C# 8 required for default interface methods (`FixedUpdate`, `LateUpdate` under `#if UNITY`)
- Interface methods cannot use `public` access modifier (all interface members are implicitly public)
- Interface methods cannot use `abstract` or `override` keywords — implementors simply define the method

### Assumptions

- The project targets .NET Standard 2.1 / C# 8+ (verified: `obj/Debug/netstandard2.1/` exists)
- No external consumers of the `State` type exist outside this repository
- `#if UNITY_2017_1_OR_NEWER` guard is the correct conditional for Unity builds

### Impact Analysis

- **Direct:** 7 files listed above
- **Adjacent:** None — `IPopupState`, `IEventReceiverState`, `IComponentUser`, attributes all use independent types
- **Downstream:** Any user project extending `State` must rename to `IState` — acceptable since the library has no known external consumers

---

## Implementation

### Overview

Seven steps, one per file. Steps 1-2 are the definition changes; Steps 3-7 are mechanical reference updates. Order matters only for Step 1 (defines the type) — the rest are independent.

### Step 1: Convert `State` to `IState` and rename file

**Objective:** Transform the type definition from abstract class to interface.
**Confidence:** High
**Depends on:** None

**Files:**
- `StateMachine.State.cs` → rename to `StateMachine.IState.cs`

**Changes:**

```csharp
// Before:
public abstract class State
{
    public abstract void OnEntered(IStateMachine<T> machine, State previous, T subject, object parameter = null);
    public abstract void Update(IStateMachine<T> machine, T subject);
    public abstract void OnLeaving(IStateMachine<T> machine, State next, T subject, object parameter = null);
    public abstract void Reset ();

#if UNITY_2017_1_OR_NEWER
    public virtual void FixedUpdate(IStateMachine<T> machine, T subject) {}
    public virtual void LateUpdate(IStateMachine<T> machine, T subject) {}
#endif
}

// After:
public interface IState
{
    void OnEntered(IStateMachine<T> machine, IState previous, T subject, object parameter = null);
    void Update(IStateMachine<T> machine, T subject);
    void OnLeaving(IStateMachine<T> machine, IState next, T subject, object parameter = null);
    void Reset();

#if UNITY_2017_1_OR_NEWER
    void FixedUpdate(IStateMachine<T> machine, T subject) {}
    void LateUpdate(IStateMachine<T> machine, T subject) {}
#endif
}
```

Key transformations:
- `abstract class` → `interface`
- Remove `public abstract` from method declarations (interface members are implicitly public, cannot be abstract)
- Remove `public virtual` from Unity methods (default interface methods need no modifier)
- `State` → `IState` in parameter types (`OnEntered`, `OnLeaving`)
- File renamed via `git mv`

**Rationale:** Interface is the correct abstraction — zero implementation, pure contract. Default interface methods (C# 8) replace the virtual empty-body pattern.

**Verification:** File exists as `StateMachine.IState.cs` with interface syntax. Old file gone from git tracking.

**If this fails:** `git mv` rollback restores original filename.

---

### Step 2: Update `StateMachine.cs`

**Objective:** Replace all `State` type references with `IState` in the core machine.
**Confidence:** High
**Depends on:** Step 1

**Files:**
- `StateMachine.cs`

**Changes (each occurrence):**

| Line(s) | Before | After |
|----------|--------|-------|
| 28 | `public State CurrentState { get; protected set; }` | `public IState CurrentState { get; protected set; }` |
| 51 | `public event Action<State, State> OnStateChanging;` | `public event Action<IState, IState> OnStateChanging;` |
| 52 | `public event Action<State, State> OnStateChanged;` | `public event Action<IState, IState> OnStateChanged;` |
| 53 | `protected Dictionary<Type, State> AutoStateCache { get; set; }` | `protected Dictionary<Type, IState> AutoStateCache { get; set; }` |
| 124 | `public virtual void ChangeState(State state, object parameter = null)` | `public virtual void ChangeState(IState state, object parameter = null)` |
| 140 | `public virtual void ChangeState<S>(object parameter = null) where S : State, new()` | `public virtual void ChangeState<S>(object parameter = null) where S : IState, new()` |
| 142 | `AutoStateCache = new Dictionary<Type, State>();` | `AutoStateCache = new Dictionary<Type, IState>();` |
| 150 | `var prev = CurrentState;` | (unchanged — `CurrentState` type change propagates) |
| 166 | `protected virtual void DeliverComponents(State state)` | `protected virtual void DeliverComponents(IState state)` |
| 225 | `protected virtual void PreStateChange(State fromState, State toState, object parameter = null)` | `protected virtual void PreStateChange(IState fromState, IState toState, object parameter = null)` |
| 236 | `protected virtual void PostStateChange(State fromState)` | `protected virtual void PostStateChange(IState fromState)` |
| 252 | `public void Cache<S> (S state) where S : State` | `public void Cache<S> (S state) where S : IState` |
| 255 | `AutoStateCache = new Dictionary<Type, State>();` | `AutoStateCache = new Dictionary<Type, IState>();` |
| 384 | `where S : IState<T>` | `where S : StateMachine<T>.IState` |

**Rationale:** Mechanical find-and-replace. Every `State` type reference in this file refers to the nested class being renamed.

**Verification:** `grep -n "\\bState\\b" StateMachine.cs` returns zero matches (excluding `StateMachine`, `CurrentState` as part of identifiers, string literals, and comments).

**If this fails:** Revert the file via git.

---

### Step 3: Update `StateMachine.NoOpState.cs`

**Objective:** Change `NoOpState` from extending abstract class to implementing interface.
**Confidence:** High
**Depends on:** Step 1

**Files:**
- `StateMachine.NoOpState.cs`

**Changes:**

```csharp
// Before:
public sealed class NoOpState : State
{
    public override void OnEntered(IStateMachine<T> machine, State previous, T subject, object parameter = null) {}
    public void Update(IStateMachine<T> machine, T subject) {}
    public void OnLeaving(IStateMachine<T> machine, State next, T subject, object parameter = null) {}
    public void Reset() {}
}

// After:
public sealed class NoOpState : IState
{
    public void OnEntered(IStateMachine<T> machine, IState previous, T subject, object parameter = null) {}
    public void Update(IStateMachine<T> machine, T subject) {}
    public void OnLeaving(IStateMachine<T> machine, IState next, T subject, object parameter = null) {}
    public void Reset() {}
}
```

Key transformations:
- `: State` → `: IState`
- Remove `override` keyword from all methods (interface implementation, not override)
- `State previous` → `IState previous`, `State next` → `IState next`

**Rationale:** Interface implementors don't use `override`.

**Verification:** Compiles. `NoOpState` still satisfies `is IState` and `is not NoOpState` checks.

**If this fails:** Revert via git.

---

### Step 4: Update `StateMachine.ObserverTransitionState.cs`

**Objective:** Change base type and generic constraints from `State` to `IState`.
**Confidence:** High
**Depends on:** Step 1

**Files:**
- `StateMachine.ObserverTransitionState.cs`

**Changes:**

```csharp
// Before:
public abstract class ObserverTransitionState<H, FROM, TO> : State, IObserver<H>
    where FROM : State
    where TO : State, new()

// After:
public abstract class ObserverTransitionState<H, FROM, TO> : IState, IObserver<H>
    where FROM : IState
    where TO : class, IState, new()
```

Method signature changes:

```csharp
// Before:
public override void OnEntered(IStateMachine<T> machine, State previous, T subject, object parameter = null)
public void OnLeaving(IStateMachine<T> machine, State next, T subject, object parameter = null)
public void Update(IStateMachine<T> machine, T subject)

// After:
public void OnEntered(IStateMachine<T> machine, IState previous, T subject, object parameter = null)
public void OnLeaving(IStateMachine<T> machine, IState next, T subject, object parameter = null)
public void Update(IStateMachine<T> machine, T subject)
```

Key transformations:
- `: State,` → `: IState,` (base type)
- `where FROM : State` → `where FROM : IState`
- `where TO : State, new()` → `where TO : class, IState, new()` (`class` constraint added — `where TO : State` previously enforced reference-type-only, making `null` a valid default parameter; `where TO : IState` alone allows structs and breaks that guarantee)
- Remove `override` from `OnEntered`, `OnLeaving`, `Update`
- `State previous` → `IState previous`, `State next` → `IState next`
- Add `public abstract void Reset()` — C# requires abstract classes to explicitly declare any unimplemented interface member as `abstract`; the compiler does not make them implicitly abstract (CS0535 is raised otherwise).

**Rationale:** Same mechanical transformation. Generic constraints reference the type being renamed.

**Verification:** Compiles. `ObserverTransitionState` declares `Reset` abstract, leaving it for concrete subclasses to implement.

**If this fails:** Revert via git.

---

### Step 5: Update `StateMachine.MainStateChangedEvent.cs`

**Objective:** Change field types from `State` to `IState`.
**Confidence:** High
**Depends on:** Step 1

**Files:**
- `StateMachine.MainStateChangedEvent.cs`

**Changes:**

```csharp
// Before:
public struct MainStateChangedEvent
{
    public State from, to;
    public MainStateChangedEvent(State from, State to)

// After:
public struct MainStateChangedEvent
{
    public IState from, to;
    public MainStateChangedEvent(IState from, IState to)
```

**Rationale:** Fields and constructor parameters reference the renamed type.

**Verification:** Compiles. Callers in `StateMachine.cs` (`PostStateChange`) pass `IState` values.

**If this fails:** Revert via git.

---

### Step 6: Update `MultiTrackStateMachine.cs`

**Objective:** Replace all `State` references with `IState` in the multi-track machine.
**Confidence:** High
**Depends on:** Step 1

**Files:**
- `MultiTrackStateMachine.cs`

**Changes (each occurrence):**

| Line(s) | Before | After |
|----------|--------|-------|
| 24 | `SideTracks = new State[minMax.max + 1];` | `SideTracks = new IState[minMax.max + 1];` |
| 27 | `public State[] SideTracks { get; protected set; }` | `public IState[] SideTracks { get; protected set; }` |
| 28 | `public event Action<TRACK, State, State> OnSideTrackStateChanging;` | `public event Action<TRACK, IState, IState> OnSideTrackStateChanging;` |
| 29 | `public event Action<TRACK, State, State> OnSideTrackStateChanged;` | `public event Action<TRACK, IState, IState> OnSideTrackStateChanged;` |
| 30 | `protected Dictionary<(TRACK, Type), State> AutoSideTrackStateCache` | `protected Dictionary<(TRACK, Type), IState> AutoSideTrackStateCache` |
| 37 | `public virtual void ChangeSideTrackState(TRACK track, State state, ...)` | `public virtual void ChangeSideTrackState(TRACK track, IState state, ...)` |
| 52 | `where S : State, new()` | `where S : IState, new()` |
| 54 | `AutoSideTrackStateCache = new Dictionary<(TRACK, Type), State>();` | `AutoSideTrackStateCache = new Dictionary<(TRACK, Type), IState>();` |
| 60 | `protected virtual void PreSideTrackStateChange (State fromState, State toState, TRACK sideTrack)` | `protected virtual void PreSideTrackStateChange (IState fromState, IState toState, TRACK sideTrack)` |
| 67 | `protected virtual void PostSideTrackStateChange (State fromState, State toState, TRACK sideTrack)` | `protected virtual void PostSideTrackStateChange (IState fromState, IState toState, TRACK sideTrack)` |

**Rationale:** Mirrors the `StateMachine.cs` changes for the multi-track extension.

**Verification:** Compiles. Side track logic is functionally identical.

**If this fails:** Revert via git.

---

### Step 7: Update `MultiTrackStateMachine.SideTrackStateChangedEvent.cs`

**Objective:** Change field types from `State` to `IState`.
**Confidence:** High
**Depends on:** Step 1

**Files:**
- `MultiTrackStateMachine.SideTrackStateChangedEvent.cs`

**Changes:**

```csharp
// Before:
public struct SideTrackStateChangedEvent
{
    public TRACK track;
    public State from, to;
    public SideTrackStateChangedEvent(TRACK track, State from, State to)

// After:
public struct SideTrackStateChangedEvent
{
    public TRACK track;
    public IState from, to;
    public SideTrackStateChangedEvent(TRACK track, IState from, IState to)
```

**Rationale:** Same as Step 5.

**Verification:** Compiles. Callers in `MultiTrackStateMachine.cs` pass `IState` values.

**If this fails:** Revert via git.

---

## Verification Plan

### Automated Checks

- [ ] `dotnet build` succeeds (non-Unity configuration, .NET Standard 2.1)
- [ ] No remaining references to `abstract class State` or unqualified `State` as a type in any source file (excluding comments, strings, and compound identifiers like `CurrentState`, `StateMachine`, `NoOpState`)

### Manual Verification

- [ ] `StateMachine.IState.cs` exists, `StateMachine.State.cs` does not
- [ ] `IState` is declared as `interface` (not `class`)
- [ ] `NoOpState : IState` — no `override` keywords
- [ ] `ObserverTransitionState<H, FROM, TO> : IState` — constraints use `IState`
- [ ] Unity `#if` block in `IState` uses default interface method syntax (no `virtual` keyword, body present)

### Acceptance Criteria Validation

| Criterion | How to Verify | Expected Result |
|-----------|---------------|-----------------|
| `State` → `IState` everywhere | `grep -rn "\bState\b" *.cs` excluding false positives | Zero type-reference matches |
| Interface, not class | Read `StateMachine.IState.cs` | `public interface IState` |
| Default interface methods | Read `#if UNITY` block in IState | `void FixedUpdate(...) {}` without `virtual` |
| Compiles | `dotnet build` | Success, 0 errors |

---

## Rollback Plan

1. `git checkout -- .` to restore all files
2. If file was renamed: `git mv StateMachine.IState.cs StateMachine.State.cs`
3. Alternatively: `git stash` or `git reset HEAD~1` if changes were committed

---

## Notes

### Risks

- **Abstract class with interface gap**: `ObserverTransitionState` is an abstract class implementing `IState`. It declares `Reset()` as `abstract`, leaving it for concrete subclasses. C# does **not** make unimplemented interface members implicitly abstract — the compiler raises CS0535 if they are not explicitly declared. The `class` constraint on `TO` is also required: `where TO : State` previously enforced reference-type-only (making `null` a valid default parameter); `where TO : IState` alone allows structs and must be explicitly constrained with `class`.

### Open Questions

(None — all resolved in proposal)
