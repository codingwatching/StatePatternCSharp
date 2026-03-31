# IStateMachine Facade + IState<T> Extraction

> **Status:** Complete
> **Completed:** 2026-03-29
> **Walkthrough:** `2603281630-istatemachine-and-istate-extraction-walkthrough.md`
> **Created:** 2026-03-28
> **Author:** Claude (agent)
> **Source:** `2603281600-statemachine-driver-interface-proposal.md` (Option C)
> **Related Projex:** `2603281430-statemachine-as-interface-eval.md`, `2603201530-statemachines-as-states-imagine.md`, `2603251600-attach-detach-parallel-machines-proposal.md`, `20260311-monobehaviour-statemachine-proposal.md`
> **Worktree:** Yes

---

## Summary

Extract `StateMachine<T>.IState` and `StateMachine<T>.IPopupState` to top-level `IState<T>` and `IPopupState<T>` interfaces. Introduce `IStateMachine` / `IStateMachine<T>` facade interfaces with clean mutual references covering state transitions, popup management, events, and observation. All state lifecycle methods receive `IStateMachine<T>` instead of `StateMachine<T>`, decoupling states from the concrete class. `StateMachine<T>` implements `IStateMachine<T>`.

**Scope:** Interface definitions + mechanical reference updates across all source files
**Estimated Changes:** 3 new files, 2 deleted files, 12 modified files

---

## Objective

### Problem / Gap / Need

`IState` is nested inside `StateMachine<T>`, making it impossible to define an `IStateMachine<T>` interface without referencing the concrete class. The current proposal's Option B (facade that references `StateMachine<T>.IState`) works but creates semantic coupling — the interface "knows about" the class it abstracts. Option C eliminates this by extracting `IState<T>` to the namespace level, enabling clean mutual references between `IState<T>` and `IStateMachine<T>`.

Additionally, `FixedUpdate`/`LateUpdate` on `StateMachine<T>` take `(IStateMachine<T> machine, T subject)` parameters (matching the nested `IState` signature) while `Update()` is parameterless. The params are unused internally — the methods use `this` and `this.Subject`. This asymmetry must be normalized for `IStateMachine` to declare parameterless tick methods.

### Success Criteria

- [ ] Top-level `IState<T>` interface exists with `IStateMachine<T>` machine parameter
- [ ] `IStateMachine` (non-generic) interface exists with `Update()`, `FixedUpdate()`, `LateUpdate()`, `UpdatePaused`
- [ ] `IStateMachine<T>` (generic) interface exists with `CurrentState`, `ChangeState`, `SendEvent`, events
- [ ] `StateMachine<T>` implements `IStateMachine<T>` (and therefore `IStateMachine`)
- [ ] Nested `StateMachine<T>.IState` is removed — all references use top-level `IState<T>`
- [ ] `IEventReceiverState<T, E>` machine parameter uses `IStateMachine<T>`
- [ ] `MonoBehaviourStateMachine<T>` type checks use `IState<T>` instead of `StateMachine<T>.IState`
- [ ] Top-level `IPopupState<T>` interface exists with `IStateMachine<T>` machine parameter
- [ ] Nested `StateMachine<T>.IPopupState` is removed — all references use top-level `IPopupState<T>`
- [ ] `IStateMachine<T>` includes popup methods (`Popup`, `EndPopupState`, events) and `SendEvent<E>`
- [ ] `IStateMachine.UpdatePaused` is read-only (`{ get; }`) — setter remains on `StateMachine<T>` only
- [ ] Project compiles with no errors (verified via `dotnet build` and `dotnet build /p:DefineConstants=UNITY_2017_1_OR_NEWER`)

### Out of Scope

- Adding component-delivery methods to `IStateMachine<T>` — keep the facade focused on state + popup operations
- Machines-as-states implementation (`StateMachine<T> : IState<T>`) — separate plan
- Changing `MonoBehaviourStateMachine<T>.Machine` property type to `IStateMachine<T>` — the wrapper needs full class API internally for `CacheByType`, component registration, etc.

---

## Context

### Current State

**IState is nested (`StateMachine.IState.cs`):**
```csharp
public partial class StateMachine<T>
{
    public interface IState
    {
        void OnEntered(IStateMachine<T> machine, IState previous, T subject, object parameter = null);
        void Update(IStateMachine<T> machine, T subject);
        void OnLeaving(IStateMachine<T> machine, IState next, T subject, object parameter = null);
        void Reset();
        void FixedUpdate(IStateMachine<T> machine, T subject) {}  // default impl
        void LateUpdate(IStateMachine<T> machine, T subject) {}   // default impl
    }
}
```

**FixedUpdate/LateUpdate asymmetry (`StateMachine.cs:270-311`):**
```csharp
public virtual void Update() { ... }                                        // parameterless
public virtual void FixedUpdate(IStateMachine<T> machine, T subject) { ... } // params unused
public virtual void LateUpdate(IStateMachine<T> machine, T subject) { ... }  // params unused
```

**No `IStateMachine` abstraction exists.** All consumers reference `StateMachine<T>` directly.

### Key Files

> Quick reference — detailed changes are in Implementation steps below.

| File | Role | Change Summary |
|------|------|----------------|
| New: `IState.cs` | Top-level IState<T> | Define extracted interface |
| New: `IStateMachine.cs` | Facade interfaces | Define IStateMachine + IStateMachine<T> |
| New: `IPopupState.cs` | Top-level IPopupState<T> | Define extracted popup interface |
| `StateMachine.IState.cs` | Nested IState | Delete file |
| `StateMachine.IPopupState.cs` | Nested IPopupState | Delete file |
| `StateMachine.cs` | Core machine class | Implement IStateMachine<T>, update ~16 IState refs + ~11 IPopupState refs, normalize FixedUpdate/LateUpdate |
| `StateMachine.NoOpState.cs` | Null-object state | Update base + signatures |
| `StateMachine.ObserverTransitionState.cs` | Rx bridge state | Update base + constraints + signatures |
| `StateMachine.MainStateChangedEvent.cs` | Event struct | Update IState field types |
| `StateMachine.NewPopupStateEvent.cs` | Event struct | Update IPopupState field types |
| `StateMachine.PopupStateEndedEvent.cs` | Event struct | Update IPopupState field types |
| `MultiTrackStateMachine.cs` | Multi-track subclass | Update ~11 IState refs |
| `MultiTrackStateMachine.SideTrackStateChangedEvent.cs` | Event struct | Update field types |
| `IEventReceiverState.cs` | Event receiver interface | Machine param → IStateMachine<T> |
| `IStateResolver.cs` | State resolver interface | Update doc comment |
| `unity/MonoBehaviourStateMachine.cs` | Unity wrapper | Update type checks |
| `unity/StateMachine.TimedPopupState.cs` | Timed popup | Update base + machine param |

### Dependencies

- **Requires:** None — purely additive/refactoring
- **Blocks:** MonoBehaviour wrapper flexibility, attach/detach proposal, machines-as-states

### Constraints

- C# 8 / .NET Standard 2.1 required for default interface methods (`FixedUpdate`/`LateUpdate` on `IState<T>`)
- No external consumers — breaking changes to IState signatures are safe
- `IPopupState<T>` is extracted alongside `IState<T>` — both use `IStateMachine<T>` machine params

### Assumptions

- `FixedUpdate(IStateMachine<T> machine, T subject)` and `LateUpdate(...)` on `StateMachine<T>` have their params genuinely unused — verified by reading L287-311
- No user-defined state implementations exist outside this repository (library has no external consumers yet)
- `ObserverTransitionState.Update()` only calls `machine.ChangeState()` — which is on `IStateMachine<T>`
- `TimedPopupState.Update()` only calls `machine.EndPopupState(this)` — which is on `IStateMachine<T>` after facade widening

### Impact Analysis

- **Direct:** All source files in the project (3 new, 12 modified, 2 deleted)
- **Downstream:** None — all `IPopupState` and `IState` consumers are updated in this plan.

---

## Implementation

### Overview

Create three new interface files (`IStateMachine.cs`, `IState.cs`, `IPopupState.cs`), then mechanically replace all `StateMachine<T>.IState` / `IState` and `StateMachine<T>.IPopupState` / `IPopupState` references with top-level `IState<T>` / `IPopupState<T>`, normalize FixedUpdate/LateUpdate signatures, and add `IStateMachine<T>` to the class declaration. Every step is a textual replacement with a clear before/after pattern.

> **Replacement discipline — prefix collision avoidance**
>
> `IState` is a prefix of `IStateMachine` and `IStateResolver`. `IPopupState` contains `IState`. Naive find-and-replace will corrupt these tokens. Apply replacements in this order with these exact patterns:
>
> **Pass 1 — Qualified forms (literal string, no regex):**
> ```
> StateMachine<T>.IState      →  IState<T>
> StateMachine<T>.IPopupState →  IPopupState<T>
> ```
>
> **Pass 2 — Unqualified forms (regex, longest match first):**
> ```
> \bIPopupState\b(?!<)  →  IPopupState<T>    # longer token first
> \bIState\b(?!<)       →  IState<T>
> ```
>
> Why this is safe:
> - `\b` (word boundary) prevents matching inside `IStateMachine` or `IStateResolver` — no boundary exists between `e` and `M`/`R` (both word characters)
> - `(?!<)` (negative lookahead) prevents double-conversion — already-converted `IState<T>` has `<` immediately after and is skipped
> - Longest-first ordering prevents `IState` from matching inside `IPopupState` (though `\b` already prevents this since there is no word boundary before `State` within `IPopupState`)
>
> **Scope:** Apply per-file within the files listed in each sub-step. Exclude `.projex/` directory.

### Step 1: Create `IStateMachine.cs`

**Objective:** Define the driving facade interfaces
**Confidence:** High
**Depends on:** None

**Files:**
- New: `IStateMachine.cs`

**Changes:**

```csharp
// New file: IStateMachine.cs
using System;
using System.Collections.Generic;

namespace BAStudio.StatePattern
{
    /// <summary>
    /// Minimal contract for driving a state machine forward each frame.
    /// Implemented by StateMachine&lt;T&gt;, wrappers, and adapters.
    /// </summary>
    public interface IStateMachine
    {
        void Update();
        bool UpdatePaused { get; }
        bool IsChangingState { get; }
        bool IsUpdating { get; }

#if UNITY_2017_1_OR_NEWER
        bool IsFixedUpdating { get; }
        bool IsLateUpdating { get; }
        void FixedUpdate();
        void LateUpdate();
#endif
    }

    /// <summary>
    /// Typed facade for state machine interaction: state transitions, popup states, events, and observation.
    /// <para>States receive this interface in their lifecycle methods. For operations beyond this facade
    /// (component delivery, caching, debug), cast to <c>StateMachine&lt;T&gt;</c>.</para>
    /// </summary>
    public interface IStateMachine<T> : IStateMachine
    {
        T Subject { get; }
        IState<T> CurrentState { get; }

        void ChangeState(IState<T> state, object parameter = null);
        void ChangeState<S>(object parameter = null) where S : IState<T>;

        bool SendEvent<E>(E ev);

        void Popup(IPopupState<T> state, object parameter = null);
        S Popup<S>(object parameter = null) where S : IPopupState<T>, new();
        void EndPopupState(IPopupState<T> state, object parameter = null);
        IReadOnlyCollection<IPopupState<T>>? ViewPopupStates();

        event Action<IState<T>, IState<T>> OnStateChanging;
        event Action<IState<T>, IState<T>> OnStateChanged;
        event Action<IPopupState<T>> PopupStateStarted;
        event Action<IPopupState<T>> PopupStateEnded;
    }
}
```

**Rationale:** Two-level design. Non-generic `IStateMachine` enables heterogeneous collections (`List<IStateMachine>`) and type-erased wrappers. Generic `IStateMachine<T>` adds typed state and popup operations for contexts that need them. `UpdatePaused` is read-only on the interface (setter remains on `StateMachine<T>`) — consumers can observe pause state but only the owner can mutate it. Component delivery, caching, and debug are excluded to keep the facade focused on state lifecycle.

**Verification:** File exists. Does not compile alone (forward-references `IState<T>` and `IPopupState<T>`) — compiles after Steps 2 and 2b.

> **Atomicity:** Steps 1, 2, and 2b are an atomic group — apply all three before any compilation check.

**If this fails:** Delete file, no side effects.

---

### Step 2: Create `IState.cs`

**Objective:** Extract `StateMachine<T>.IState` to a top-level `IState<T>` with `IStateMachine<T>` machine parameter
**Confidence:** High
**Depends on:** Step 1 (IStateMachine<T> referenced in method signatures)

**Files:**
- New: `IState.cs`

**Changes:**

```csharp
// New file: IState.cs
namespace BAStudio.StatePattern
{
    /// <summary>
    /// Lifecycle contract for a state within a state machine.
    /// Extracted from StateMachine&lt;T&gt;.IState to enable clean mutual references with IStateMachine&lt;T&gt;.
    /// </summary>
    public interface IState<T>
    {
        void OnEntered(IStateMachine<T> machine, IState<T> previous, T subject, object parameter = null);

        /// <summary>
        /// <para>Only happens in StateMachine&lt;T&gt;.Update().</para>
        /// <para>Not guaranteed between OnEntered and OnLeaving — those can trigger ChangeState too.</para>
        /// </summary>
        void Update(IStateMachine<T> machine, T subject);

        void OnLeaving(IStateMachine<T> machine, IState<T> next, T subject, object parameter = null);

        /// <summary>
        /// Called after the machine completes the full ChangeState sequence.
        /// Used to reset cached/reused state instances.
        /// </summary>
        void Reset();

#if UNITY_2017_1_OR_NEWER
        void FixedUpdate(IStateMachine<T> machine, T subject) {}
        void LateUpdate(IStateMachine<T> machine, T subject) {}
#endif
    }
}
```

**Rationale:** The `machine` parameter is typed as `IStateMachine<T>` instead of `StateMachine<T>`. States interact with the machine through the facade which covers transitions, popups, events, and observation. States needing the full class API (component delivery, caching, debug) can cast — this is an intentional friction point that keeps states decoupled.

**Verification:** Together with Step 1, both files reference each other and form a clean mutual dependency. Does not compile in isolation — compiles once `StateMachine<T>` implements `IStateMachine<T>` and nested `IState` references are resolved.

**If this fails:** Delete file, no side effects.

---

### Step 2b: Create `IPopupState.cs`

**Objective:** Extract `StateMachine<T>.IPopupState` to a top-level `IPopupState<T>` with `IStateMachine<T>` machine parameter
**Confidence:** High
**Depends on:** Step 1 (IStateMachine<T> referenced in method signatures)

**Files:**
- New: `IPopupState.cs`

**Changes:**

```csharp
// New file: IPopupState.cs
namespace BAStudio.StatePattern
{
    /// <summary>
    /// Lifecycle contract for a popup state within a state machine.
    /// Popup states are self-contained, self-terminating, and not cached.
    /// Extracted from StateMachine&lt;T&gt;.IPopupState to enable clean references with IStateMachine&lt;T&gt;.
    /// </summary>
    public interface IPopupState<T>
    {
        void OnStarting(IStateMachine<T> machine, T subject, object parameter = null);
        void OnEnding(IStateMachine<T> machine, T subject, object parameter = null);
        void Update(IStateMachine<T> machine, T subject);

#if UNITY_2017_1_OR_NEWER
        void FixedUpdate(IStateMachine<T> machine, T subject) {}
        void LateUpdate(IStateMachine<T> machine, T subject) {}
#endif
    }
}
```

**Rationale:** Mirrors the `IState<T>` extraction. The `machine` parameter is `IStateMachine<T>` — popup states that call `machine.EndPopupState(this)` now go through the facade, which exposes this method after the Step 1 widening. No more asymmetry between main states and popup states.

**Verification:** Together with Steps 1 and 2, all three interface files form a clean mutual dependency. Compiles once `StateMachine<T>` implements `IStateMachine<T>` and nested references are resolved.

**If this fails:** Delete file, no side effects.

---

### Step 3: Normalize FixedUpdate/LateUpdate on StateMachine<T>

**Objective:** Make `FixedUpdate()` and `LateUpdate()` parameterless (matching `Update()`) so `StateMachine<T>` can satisfy `IStateMachine.FixedUpdate()` and `IStateMachine.LateUpdate()`
**Confidence:** High — parameters are verifiably unused (methods use `this` and `Subject` internally)
**Depends on:** None (independent of Steps 1-2)

**Files:**
- `StateMachine.cs`
- `unity/MonoBehaviourStateMachine.cs` (call site update)

**Changes:**

In `StateMachine.cs`:

```csharp
// Before (L287-298):
public virtual void FixedUpdate(IStateMachine<T> machine, T subject)
{
    SelfDiagnosticOnUpdate();
    if (UpdatePaused) return;
    if (Subject == null) throw new System.NullReferenceException("Target is null.");
    IsFixedUpdating = true;
    FixedUpdateMainState();
    FixedUpdatePopStates();
    IsFixedUpdating = false;
}

// After:
public virtual void FixedUpdate()
{
    SelfDiagnosticOnUpdate();
    if (UpdatePaused) return;
    if (Subject == null) throw new System.NullReferenceException("Target is null.");
    IsFixedUpdating = true;
    FixedUpdateMainState();
    FixedUpdatePopStates();
    IsFixedUpdating = false;
}
```

```csharp
// Before (L300-311):
public virtual void LateUpdate(IStateMachine<T> machine, T subject)
{
    // ... identical pattern ...
}

// After:
public virtual void LateUpdate()
{
    // ... identical pattern ...
}
```

In `unity/MonoBehaviourStateMachine.cs`:

```csharp
// Before (L101):
Machine.FixedUpdate(Machine, Machine.Subject);
// After:
Machine.FixedUpdate();

// Before (L107):
Machine.LateUpdate(Machine, Machine.Subject);
// After:
Machine.LateUpdate();
```

**Rationale:** The `machine` and `subject` parameters are never referenced in the method bodies — both methods use `this` and `this.Subject` directly. Removing the params makes them consistent with `Update()` and satisfies `IStateMachine.FixedUpdate()` / `IStateMachine.LateUpdate()`. The only callers are `MonoBehaviourStateMachine.cs:101,107` which pass `Machine` and `Machine.Subject` — redundant since the machine uses its own `this` and `Subject` internally.

**Verification:** After change, `grep -rn 'FixedUpdate(.*,\|LateUpdate(.*,' --include='*.cs'` returns only `IState<T>` / `IPopupState` method calls on states (which retain their params). No machine-level parameterized calls remain.

**If this fails:** Restore original signatures in `StateMachine.cs` and call sites in `MonoBehaviourStateMachine.cs`.

---

### Step 4: Delete nested IState and update all references

**Objective:** Remove `StateMachine<T>.IState` and `StateMachine<T>.IPopupState`, replace all references with top-level `IState<T>` / `IPopupState<T>` across the entire codebase. Update machine parameter types from `StateMachine<T>` to `IStateMachine<T>` in state implementations.
**Confidence:** High — mechanical replacement, no behavioral changes
**Depends on:** Steps 1, 2, 2b (target types must exist)

> **Two replacement patterns:** Most references are unqualified (`IState`, `IPopupState` — nested type shorthand within `StateMachine<T>`). Two locations use the qualified form `StateMachine<T>.IState` (L395 in `StateMachine.cs` and `MultiTrackStateMachine.SideTrackStateChangedEvent.cs`). Both patterns become `IState<T>` / `IPopupState<T>`.

**Files:**
- Delete: `StateMachine.IState.cs`
- Delete: `StateMachine.IPopupState.cs`
- Modify: `StateMachine.cs`
- Modify: `StateMachine.NoOpState.cs`
- Modify: `StateMachine.ObserverTransitionState.cs`
- Modify: `StateMachine.MainStateChangedEvent.cs`
- Modify: `StateMachine.NewPopupStateEvent.cs`
- Modify: `StateMachine.PopupStateEndedEvent.cs`
- Modify: `MultiTrackStateMachine.cs`
- Modify: `MultiTrackStateMachine.SideTrackStateChangedEvent.cs`
- Modify: `IStateResolver.cs` (doc comment only)
- Modify: `unity/StateMachine.TimedPopupState.cs`

**Changes per file:**

#### 4a. Delete `StateMachine.IState.cs` and `StateMachine.IPopupState.cs`

Both files are entirely replaced by the new top-level `IState.cs` and `IPopupState.cs`. Remove them.

#### 4b. `StateMachine.cs` — replace all `IState` with `IState<T>`

Apply the following replacements throughout the file. Within `StateMachine<T>`, unqualified `IState` refers to the nested type — all become `IState<T>`.

| Line | Before | After |
|------|--------|-------|
| L28 | `public IState CurrentState { get; protected set; }` | `public IState<T> CurrentState { get; protected set; }` |
| L52 | `public event Action<IState, IState> OnStateChanging;` | `public event Action<IState<T>, IState<T>> OnStateChanging;` |
| L53 | `public event Action<IState, IState> OnStateChanged;` | `public event Action<IState<T>, IState<T>> OnStateChanged;` |
| L54 | `protected Dictionary<Type, IState> AutoStateCache { get; set; }` | `protected Dictionary<Type, IState<T>> AutoStateCache { get; set; }` |
| L125 | `public virtual void ChangeState(IState state, ...)` | `public virtual void ChangeState(IState<T> state, ...)` |
| L141 | `... where S : IState` | `... where S : IState<T>` |
| L143 | `AutoStateCache = new Dictionary<Type, IState>()` | `AutoStateCache = new Dictionary<Type, IState<T>>()` |
| L146 | `state = (IState) StateResolver.Resolve(typeof(S));` | `state = (IState<T>) StateResolver.Resolve(typeof(S));` |
| L166 | `protected virtual void DeliverComponents(IState state)` | `protected virtual void DeliverComponents(IState<T> state)` |
| L225 | `protected virtual void PreStateChange(IState fromState, IState toState, ...)` | `protected virtual void PreStateChange(IState<T> fromState, IState<T> toState, ...)` |
| L236 | `protected virtual void PostStateChange(IState fromState)` | `protected virtual void PostStateChange(IState<T> fromState)` |
| L252 | `public void Cache<S> (S state) where S : IState` | `public void Cache<S> (S state) where S : IState<T>` |
| L255 | `AutoStateCache = new Dictionary<Type, IState>()` | `AutoStateCache = new Dictionary<Type, IState<T>>()` |
| L263 | `public void CacheByType(IState state)` | `public void CacheByType(IState<T> state)` |
| L265 | `AutoStateCache = new Dictionary<Type, IState>()` | `AutoStateCache = new Dictionary<Type, IState<T>>()` |
| L395 | `... where S : StateMachine<T>.IState` | `... where S : IState<T>` |

Additionally, replace all `IPopupState` with `IPopupState<T>` in `StateMachine.cs` (11 references):

| Line | Before | After |
|------|--------|-------|
| L55 | `protected List<IPopupState> PopupStates` | `protected List<IPopupState<T>> PopupStates` |
| L56 | `protected List<IPopupState> PopupStatesToEnd` | `protected List<IPopupState<T>> PopupStatesToEnd` |
| L57 | `public event Action<IPopupState> PopupStateStarted` | `public event Action<IPopupState<T>> PopupStateStarted` |
| L58 | `public event Action<IPopupState> PopupStateEnded` | `public event Action<IPopupState<T>> PopupStateEnded` |
| L65 | `Popup(IPopupState s, ...)` | `Popup(IPopupState<T> s, ...)` |
| L68 | `new List<IPopupState>()` | `new List<IPopupState<T>>()` |
| L80 | `where S : IPopupState, new()` | `where S : IPopupState<T>, new()` |
| L83 | `new List<IPopupState>()` | `new List<IPopupState<T>>()` |
| L93 | `EndPopupState(IPopupState s, ...)` | `EndPopupState(IPopupState<T> s, ...)` |
| L96 | `new List<IPopupState>()` | `new List<IPopupState<T>>()` |
| L108 | `IReadOnlyCollection<IPopupState>?` | `IReadOnlyCollection<IPopupState<T>>?` |

#### 4c. `StateMachine.NoOpState.cs`

```csharp
// Before:
public partial class StateMachine<T>
{
    public sealed class NoOpState : IState
    {
        public void OnEntered(IStateMachine<T> machine, IState previous, T subject, object parameter = null) {}
        public void Update(IStateMachine<T> machine, T subject) {}
        public void OnLeaving(IStateMachine<T> machine, IState next, T subject, object parameter = null) {}
        public void Reset() {}
    }
}

// After:
public partial class StateMachine<T>
{
    public sealed class NoOpState : IState<T>
    {
        public void OnEntered(IStateMachine<T> machine, IState<T> previous, T subject, object parameter = null) {}
        public void Update(IStateMachine<T> machine, T subject) {}
        public void OnLeaving(IStateMachine<T> machine, IState<T> next, T subject, object parameter = null) {}
        public void Reset() {}
    }
}
```

#### 4d. `StateMachine.ObserverTransitionState.cs`

```csharp
// Before:
public abstract class ObserverTransitionState<H, FROM, TO> : IState, IObserver<H>
    where FROM : IState
    where TO : class, IState

    public void OnEntered(IStateMachine<T> machine, IState previous, T subject, object parameter = null) { ... }
    public void OnLeaving(IStateMachine<T> machine, IState next, T subject, object parameter = null) { ... }
    public void Update(IStateMachine<T> machine, T subject) { ... }

// After:
public abstract class ObserverTransitionState<H, FROM, TO> : IState<T>, IObserver<H>
    where FROM : IState<T>
    where TO : class, IState<T>

    public void OnEntered(IStateMachine<T> machine, IState<T> previous, T subject, object parameter = null) { ... }
    public void OnLeaving(IStateMachine<T> machine, IState<T> next, T subject, object parameter = null) { ... }
    public void Update(IStateMachine<T> machine, T subject) { ... }
```

Method bodies unchanged — `machine.ChangeState(...)` and `machine.ChangeState<TO>(...)` are both on `IStateMachine<T>`.

#### 4e. `StateMachine.MainStateChangedEvent.cs`

```csharp
// Before:
public IState from, to;
public MainStateChangedEvent(IState from, IState to)

// After:
public IState<T> from, to;
public MainStateChangedEvent(IState<T> from, IState<T> to)
```

#### 4f. `MultiTrackStateMachine.cs`

Replace all `IState` with `IState<T>` (11 references):

| Location | Before | After |
|----------|--------|-------|
| L24 | `new IState[...]` | `new IState<T>[...]` |
| L27 | `public IState[] SideTracks` | `public IState<T>[] SideTracks` |
| L28 | `event Action<TRACK, IState, IState>` | `event Action<TRACK, IState<T>, IState<T>>` |
| L29 | `event Action<TRACK, IState, IState>` | `event Action<TRACK, IState<T>, IState<T>>` |
| L30 | `Dictionary<(TRACK, Type), IState>` | `Dictionary<(TRACK, Type), IState<T>>` |
| L37 | `ChangeSideTrackState(TRACK track, IState state, ...)` | `ChangeSideTrackState(TRACK track, IState<T> state, ...)` |
| L52 | `where S : IState` | `where S : IState<T>` |
| L54 | `new Dictionary<(TRACK, Type), IState>()` | `new Dictionary<(TRACK, Type), IState<T>>()` |
| L58 | `(IState) StateResolver.Resolve(typeof(S))` | `(IState<T>) StateResolver.Resolve(typeof(S))` |
| L64 | `PreSideTrackStateChange(IState fromState, IState toState, ...)` | `PreSideTrackStateChange(IState<T> fromState, IState<T> toState, ...)` |
| L71 | `PostSideTrackStateChange(IState fromState, IState toState, ...)` | `PostSideTrackStateChange(IState<T> fromState, IState<T> toState, ...)` |

#### 4g. `MultiTrackStateMachine.SideTrackStateChangedEvent.cs`

```csharp
// Before:
public StateMachine<T>.IState from, to;
public SideTrackStateChangedEvent(TRACK track, StateMachine<T>.IState from, StateMachine<T>.IState to)

// After:
public IState<T> from, to;
public SideTrackStateChangedEvent(TRACK track, IState<T> from, IState<T> to)
```

#### 4h. `StateMachine.NewPopupStateEvent.cs`

```csharp
// Before:
public IPopupState popupState;
public NewPopupStateEvent(IPopupState popupState)

// After:
public IPopupState<T> popupState;
public NewPopupStateEvent(IPopupState<T> popupState)
```

#### 4i. `StateMachine.PopupStateEndedEvent.cs`

```csharp
// Before:
public IPopupState popupState;
public PopupStateEndedEvent(IPopupState popupState)

// After:
public IPopupState<T> popupState;
public PopupStateEndedEvent(IPopupState<T> popupState)
```

#### 4j. `unity/StateMachine.TimedPopupState.cs`

```csharp
// Before:
public abstract class TimedPopupState : IPopupState
{
    public abstract void OnEnding(IStateMachine<T> machine, T subject, object parameter = null);
    public void OnStarting(IStateMachine<T> machine, T subject, object parameter = null) { ... }
    public void Update(IStateMachine<T> machine, T subject) { ... }
}

// After:
public abstract class TimedPopupState : IPopupState<T>
{
    public abstract void OnEnding(IStateMachine<T> machine, T subject, object parameter = null);
    public void OnStarting(IStateMachine<T> machine, T subject, object parameter = null) { ... }
    public void Update(IStateMachine<T> machine, T subject) { ... }
}
```

Method bodies unchanged — `machine.EndPopupState(this)` is now on `IStateMachine<T>` (Step 1 widening), so the call compiles through the interface.

#### 4k. `IStateResolver.cs` — update doc comment

```csharp
// Before (L13):
/// The returned instance must implement IState for the machine's T.

// After:
/// The returned instance must implement IState<T> for the machine's T.
```

#### 4-body. Body call sites confirmation (no textual changes needed)

The following method-body lines pass `this` as the machine parameter to `IState<T>` / `IPopupState<T>` / `IEventReceiverState<T,E>` methods. After this plan, `this` is `StateMachine<T> : IStateMachine<T>` (or `MultiTrackStateMachine<T,TRACK>` which inherits it), satisfying the new parameter types via implicit upcast. **No textual changes are needed at these sites.**

**`StateMachine.cs` — main state lifecycle:**

| Line | Call | New Parameter Type |
|------|------|--------------------|
| L360 | `CurrentState.Update(this, Subject)` | `IStateMachine<T>` ✓ |
| L319 | `CurrentState.FixedUpdate(this, Subject)` | `IStateMachine<T>` ✓ |
| L335 | `CurrentState.LateUpdate(this, Subject)` | `IStateMachine<T>` ✓ |

**`StateMachine.cs` — popup state lifecycle:**

| Line | Call | New Parameter Type |
|------|------|--------------------|
| L72 | `s.OnStarting(this, Subject, parameter)` | `IStateMachine<T>` ✓ |
| L88 | `s.OnStarting(this, Subject, parameter)` | `IStateMachine<T>` ✓ |
| L98 | `s.OnEnding(this, Subject, parameter)` | `IStateMachine<T>` ✓ |
| L327 | `ps.FixedUpdate(this, Subject)` | `IStateMachine<T>` ✓ |
| L371 | `ps.Update(this, Subject)` | `IStateMachine<T>` ✓ |

**`StateMachine.cs` — event dispatch:**

| Line | Call | New Parameter Type | Depends On |
|------|------|--------------------|-----------|
| L411 | `ers.ReceiveEvent(this, Subject, ev)` | `IStateMachine<T>` ✓ | Step 6 |
| L421 | `ers.ReceiveEvent(this, Subject, ev)` | `IStateMachine<T>` ✓ | Step 6 |

**`MultiTrackStateMachine.cs` — side-track lifecycle + events:**

| Line | Call | New Parameter Type | Depends On |
|------|------|--------------------|-----------|
| L44 | `state.OnEntered(this, prev, Subject, parameter)` | `IStateMachine<T>` ✓ | — |
| L67 | `fromState?.OnLeaving(this, toState, Subject)` | `IStateMachine<T>` ✓ | — |
| L101 | `SideTracks[i].Update(this, Subject)` | `IStateMachine<T>` ✓ | — |
| L109 | `ers.ReceiveEvent(this, Subject, ev)` | `IStateMachine<T>` ✓ | **Step 6** |

> **Ordering constraint:** L109 and L411/L421 call `IEventReceiverState<T,E>.ReceiveEvent` whose machine parameter changes in Step 6. Step 6 must not be deferred independently of Step 4.

**Rationale:** Every change is a type-name substitution with no behavioral effect. The nested `IState` is replaced by `IState<T>` (same methods, same semantics). The `machine` parameter on `IState<T>` implementations changes from `StateMachine<T>` to `IStateMachine<T>` — `StateMachine<T>` implements this interface, so all existing calls (`toState.OnEntered(this, ...)`) continue to work via implicit upcast.

**Verification:**
- `grep -rn 'StateMachine<T>\.IState\b' *.cs` returns zero results (all qualified IState references gone)
- `grep -rn 'StateMachine<T>\.IPopupState\b' *.cs` returns zero results (all qualified IPopupState references gone)
- `grep -rn '\bIState\b' *.cs` — all matches are `IState<T>`, `IStateMachine<T>`, or inside `IState.cs`
- `grep -rn '\bIPopupState\b' *.cs` — all matches are `IPopupState<T>` or inside `IPopupState.cs`
- `dotnet build` succeeds
- `dotnet build /p:DefineConstants=UNITY_2017_1_OR_NEWER` succeeds

**If this fails:** Restore `StateMachine.IState.cs` and `StateMachine.IPopupState.cs` from git, revert all replacements. Since every change is a name substitution, partial failure is easy to diagnose from compile errors pointing to unreplaced references.

---

### Step 5: StateMachine<T> implements IStateMachine<T>

**Objective:** Add interface implementation to the class declaration
**Confidence:** High — all required members already exist as public members with matching signatures (after Steps 3-4)
**Depends on:** Steps 3 (FixedUpdate/LateUpdate normalized) and 4 (IState<T> references in place)

**Files:**
- `StateMachine.cs`

**Changes:**

```csharp
// Before (L11):
public partial class StateMachine<T>

// After:
public partial class StateMachine<T> : IStateMachine<T>
```

No other changes needed — the class already has all the members required by `IStateMachine<T>`:

**`IStateMachine` (non-generic):**
- `void Update()` ✓ (L270)
- `bool UpdatePaused { get; }` ✓ (L35 — class has `{ get; set; }`, public getter satisfies `{ get; }`)
- `bool IsChangingState { get; }` ✓ (L63)
- `bool IsUpdating { get; }` ✓ (L62)
- `void FixedUpdate()` ✓ (after Step 3)
- `void LateUpdate()` ✓ (after Step 3)

**`IStateMachine<T>` (generic):**
- `T Subject { get; }` ✓ (L27)
- `IState<T> CurrentState { get; }` ✓ (after Step 4 — class has `protected set`, getter satisfies)
- `void ChangeState(IState<T> state, object parameter)` ✓ (after Step 4)
- `void ChangeState<S>(object parameter) where S : IState<T>` ✓ (after Step 4)
- `bool SendEvent<E>(E ev)` ✓ (L381)
- `void Popup(IPopupState<T> state, object parameter)` ✓ (L65, after Step 4 IPopupState update)
- `S Popup<S>(object parameter) where S : IPopupState<T>, new()` ✓ (L80, after Step 4)
- `void EndPopupState(IPopupState<T> state, object parameter)` ✓ (L93, after Step 4)
- `IReadOnlyCollection<IPopupState<T>>? ViewPopupStates()` ✓ (L108, after Step 4)
- `event Action<IState<T>, IState<T>> OnStateChanging` ✓ (after Step 4)
- `event Action<IState<T>, IState<T>> OnStateChanged` ✓ (after Step 4)
- `event Action<IPopupState<T>> PopupStateStarted` ✓ (L57, after Step 4)
- `event Action<IPopupState<T>> PopupStateEnded` ✓ (L58, after Step 4)

Note: Properties with `protected set` (`CurrentState`, `IsFixedUpdating`, `IsLateUpdating`) satisfy get-only interface declarations via their public getter. ✓

**Rationale:** Purely declarative — the class already satisfies the contract, this makes it explicit.

**Verification:** `dotnet build` succeeds. `typeof(IStateMachine<object>).GetInterfaces()` includes `IStateMachine<object>` and `IStateMachine`.

**If this fails:** Remove `: IStateMachine<T>` from declaration. Compile errors will indicate which member is missing or has a signature mismatch.

---

### Step 6: Update IEventReceiverState<T, E>

**Objective:** Change the `machine` parameter from `StateMachine<T>` to `IStateMachine<T>` for consistency
**Confidence:** High
**Depends on:** Step 1 (IStateMachine<T> must exist)

**Files:**
- `IEventReceiverState.cs`

**Changes:**

```csharp
// Before:
public interface IEventReceiverState<T, E>
{
    void ReceiveEvent(IStateMachine<T> machine, T subject, E ev);
}

// After:
public interface IEventReceiverState<T, E>
{
    void ReceiveEvent(IStateMachine<T> machine, T subject, E ev);
}
```

**Rationale:** Consistency with `IState<T>` — both state-related interfaces now receive the machine as `IStateMachine<T>`. Callers in `StateMachine.cs` (`SendEventToCurrentState`, `SendEventToPopupStates`) pass `this`, which is `StateMachine<T>` implementing `IStateMachine<T>` — implicit upcast.

**Verification:** Callers in `StateMachine.cs` (L410-422) and `MultiTrackStateMachine.cs` (L106-109) pass `this` as the machine — still compiles via upcast. No external implementations exist.

**If this fails:** Revert the single line. Risk: an undiscovered implementation outside this repo — unlikely per eval findings.

---

### Step 7: Update MonoBehaviourStateMachine<T>

**Objective:** Replace `StateMachine<T>.IState` type checks with `IState<T>`
**Confidence:** High
**Depends on:** Step 2 (IState<T> must exist)

**Files:**
- `unity/MonoBehaviourStateMachine.cs`

**Changes:**

```csharp
// In RegisterComponentStates():
// Before:
if (component is StateMachine<T>.IState state)
// After:
if (component is IState<T> state)

// In RegisterScriptableObjectStates():
// Before:
if (stateAsset is not StateMachine<T>.IState)
// After:
if (stateAsset is not IState<T>)

// Before:
if (clone is StateMachine<T>.IState clonedState)
// After:
if (clone is IState<T> clonedState)
```

Also update the tooltip/warning strings that mention `StateMachine<T>.IState`:

```csharp
// Before:
[Tooltip("ScriptableObject assets implementing StateMachine<T>.IState. Assign in the Inspector.")]
// After:
[Tooltip("ScriptableObject assets implementing IState<T>. Assign in the Inspector.")]

// Before:
"does not implement StateMachine<T>.IState. Skipping."
// After:
"does not implement IState<T>. Skipping."
```

**Rationale:** Type checks work identically — `IState<T>` is the same contract that `StateMachine<T>.IState` was. The `is` pattern match behavior is preserved since all implementations now directly implement `IState<T>`.

**Verification:** MonoBehaviourStateMachine compiles. Type check semantics unchanged.

**If this fails:** Revert the pattern matches. Only risk: a type that implemented the nested `IState` but not `IState<T>` — impossible since the nested interface no longer exists.

---

## Verification Plan

### Automated Checks

- [ ] `dotnet build` succeeds with zero errors and zero warnings (verifies non-Unity code)
- [ ] `dotnet build /p:DefineConstants=UNITY_2017_1_OR_NEWER` succeeds (verifies Unity-conditional members: `FixedUpdate`, `LateUpdate`, `IsFixedUpdating`, `IsLateUpdating` on both interfaces and all implementations)
- [ ] No references to `StateMachine<T>.IState` remain: `grep -rn 'StateMachine<T>\.IState\b' --include='*.cs'` returns empty (excluding `.projex/`)
- [ ] No references to `StateMachine<T>.IPopupState` remain: `grep -rn 'StateMachine<T>\.IPopupState\b' --include='*.cs'` returns empty (excluding `.projex/`)
- [ ] No unqualified `IState` (without `<T>`) remains in source files (excluding comments/strings): verified by grep
- [ ] No unqualified `IPopupState` (without `<T>`) remains in source files (excluding comments/strings): verified by grep
- [ ] `StateMachine.IState.cs` file no longer exists
- [ ] `StateMachine.IPopupState.cs` file no longer exists

### Manual Verification

- [ ] `IState<T>`, `IPopupState<T>`, and `IStateMachine<T>` have clean mutual references (no `StateMachine<T>` in any interface)
- [ ] `IEventReceiverState<T, E>` references `IStateMachine<T>`, not `StateMachine<T>`
- [ ] `StateMachine<T>` class declaration includes `: IStateMachine<T>`
- [ ] `FixedUpdate()` and `LateUpdate()` on `StateMachine<T>` are parameterless
- [ ] `IStateMachine.UpdatePaused` is `{ get; }` only (no setter on interface)

### Acceptance Criteria Validation

| Criterion | How to Verify | Expected Result |
|-----------|---------------|-----------------|
| Top-level `IState<T>` exists | File `IState.cs` exists with correct content | Interface with `IStateMachine<T>` machine params |
| `IStateMachine` + `IStateMachine<T>` exist | File `IStateMachine.cs` exists | Both interfaces defined |
| `StateMachine<T> : IStateMachine<T>` | Read class declaration | Includes `: IStateMachine<T>` |
| Nested IState removed | `StateMachine.IState.cs` does not exist | File deleted |
| All IState refs updated | grep for unqualified IState | Zero matches in source |
| All IPopupState refs updated | grep for unqualified IPopupState | Zero matches in source |
| Nested IPopupState removed | `StateMachine.IPopupState.cs` does not exist | File deleted |
| Build succeeds (non-Unity) | `dotnet build` | Exit code 0 |
| Build succeeds (Unity defines) | `dotnet build /p:DefineConstants=UNITY_2017_1_OR_NEWER` | Exit code 0 |

---

## Rollback Plan

Per-step rollback is noted in each implementation step above. If the overall implementation must be abandoned:

1. `git checkout -- .` to restore all modified files
2. Delete `IState.cs`, `IStateMachine.cs`, and `IPopupState.cs`
3. Restore `StateMachine.IState.cs` and `StateMachine.IPopupState.cs` from git
4. Verify `dotnet build` succeeds

---

## Notes

### Risks

- **States needing full class API:** If a user-defined state calls `machine.SetComponent(...)`, `machine.Cache(...)`, or accesses debug facilities, the `IStateMachine<T>` machine parameter won't expose those methods. The state must cast to `StateMachine<T>`. This is intentional friction — component delivery, caching, and debug are setup/configuration concerns, not state-lifecycle concerns.

### Open Questions

None — all questions from the proposal were resolved during research:
- `ChangeState<S>` has no `new()` constraint — expressible on interface ✓
- FixedUpdate/LateUpdate params are unused — safe to remove ✓
- No external consumers — breaking changes are safe ✓
