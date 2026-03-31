# Walkthrough: 2603291500-machines-as-states

> **Plan:** `2603291500-machines-as-states-plan.md`
> **Branch:** `projex/2603291500-machines-as-states`
> **Base:** `master`
> **Executed:** 2026-04-01
> **Closed:** 2026-04-01

---

## Summary

Unified hierarchical and parallel machine composition under `StateMachine<T>`. The machine now implements `IState<T>` for vertical nesting (machines as states) and provides `Link()`/`Unlink()` for horizontal composition (symmetric peer ring with lateral event broadcast). Sub-machines receive parent components as an entry-time snapshot via `IComponentUser`. `MultiTrackStateMachine` is deprecated in favor of the new peer ring model.

9 commits, 9 files changed, 354 insertions, 3 deletions.

---

## Step-by-Step

### Step 1: Foundation -- Subject settability + parameterless constructor
- `StateMachine.cs`: `Subject` changed from `{ get; }` to `{ get; protected set; }`.
- Added `protected StateMachine()` parameterless constructor (enables `ActivatorStateResolver` creation of sub-machines).
- Commit: `5eacda3`

### Step 2: Link/Unlink Peer Ring
- Created `StateMachine.Link.cs` (new partial): `List<StateMachine<T>> peers`, `Peers` property, static `Link(a, b)` and `Unlink(a, b)` methods.
- Mutation guard (`isPeerDispatching`) prevents list modification during event iteration.
- Null cleanup on empty lists keeps fast-path free.
- Commit: `d96d423`

### Step 3: Implement IState<T> on StateMachine<T>
- Created `StateMachine.AsState.cs` (new partial): explicit `IState<T>` implementation (OnEntered, Update, OnLeaving, Reset, FixedUpdate, LateUpdate).
- Virtual hooks: `OnNestedEntry` (set initial state on nested entry), `ResetInternalState` (cleanup on parent transition away).
- `GetStatePath()` debug method returns full nested path (e.g., `"SubMachine > LeafState"`).
- Commit: `6cf3566`

### Step 4: Event Bridging -- Direct Lateral Broadcast
- `StateMachine.cs`: Added `SendEventToPeers(ev)` in `SendEvent`, `NotifyPeersOfStateChange` in `PostStateChange`.
- `StateMachine.Link.cs`: Added `peerEventForwardingDepth` guard (max 8), `SendEventToPeers`, `NotifyPeersOfStateChange`, `ReceiveFromPeer`.
- Created `StateMachine.PeerStateChangedEvent.cs`: struct with `source`, `from`, `to`.
- Created `StateMachine.ForwardedEvent.cs`: generic envelope `ForwardedEvent<E>` with `source` and `inner`.
- Design: NO re-broadcast in `ReceiveFromPeer` -- source broadcasts to all its own peers directly, preventing duplication and loops.
- Commit: `bf64503`

### Step 5: Component Snapshot -- IComponentUser on StateMachine
- `StateMachine.cs`: Added `[DisableAutoComponents]` attribute and `IComponentUser` to class declaration.
- Added virtual `OnComponentSupplied(Type, object)` that stores components locally via `SetComponent`.
- Parent delivers components to sub-machine at entry time; sub-machine's child states receive them through normal `DeliverComponents`.
- Commit: `e7da838`

### Step 6: Deprecation
- `MultiTrackStateMachine.cs`: Added `[System.Obsolete("Use StateMachine<T> with Link() for parallel composition.")]`.
- `MultiTrackStateMachine.SideTrackStateChangedEvent.cs`: Added `[System.Obsolete("Use StateMachine<T>.PeerStateChangedEvent instead.")]`.
- Commit: `8294e5a`

### Step 7: Code Review Fixes (4 issues)
1. **(Critical)** Added `isPeerDispatching = true` guard with try/finally in `NotifyPeersOfStateChange` -- prevents peer list corruption during `PeerStateChangedEvent` dispatch.
2. **(Critical)** Added null guards in `DeliverComponents` -- early return if `Components` is null/empty; lazy-init `TypesDisabledAutoComponents` if null. Prevents NPE when parent has no components.
3. **(Important)** Added `SendEventToPeers(ev)` in `MultiTrackStateMachine.SendEvent` override -- ensures peer events forwarded even on deprecated class.
4. **(Important)** Added `peerEventForwardingDepth` increment/check with try/finally in `ReceiveFromPeer` -- prevents infinite A-B ping-pong loops.
- Commit: `c0f85d2`

### Step 8: Review Round 2 Fixes (3 issues)
1. Replaced `isPeerDispatching` boolean with `peerDispatchingDepth` counter -- prevents re-entry clobber when nested dispatch resets the flag.
2. Added `peerEventForwardingDepth` tracking to `NotifyPeersOfStateChange` for sender-side depth limiting.
3. Reset peer dispatch counters (`peerDispatchingDepth`, `peerEventForwardingDepth`) in `ResetInternalState` -- cleans up poisoned counters on sub-machine exit.
- Commit: `27d5a22`

---

## Change Log

| File | Status | Lines |
|------|--------|-------|
| `StateMachine.cs` | Modified | +27 -3 |
| `StateMachine.AsState.cs` | Created | +81 |
| `StateMachine.Link.cs` | Created | +140 |
| `StateMachine.PeerStateChangedEvent.cs` | Created | +18 |
| `StateMachine.ForwardedEvent.cs` | Created | +22 |
| `MultiTrackStateMachine.cs` | Modified | +2 |
| `MultiTrackStateMachine.SideTrackStateChangedEvent.cs` | Modified | +1 |
| `.projex/2603291500-machines-as-states-plan.md` | Modified | +1 -1 |
| `.projex/2603291500-machines-as-states-log.md` | Created | +64 |
| **Total** | | **+354 -3** |

---

## Success Criteria Verification

| Criterion | Status | Notes |
|-----------|--------|-------|
| `StateMachine<T>` implements `IState<T>` | Pass | Explicit implementation in `StateMachine.AsState.cs` |
| `StateMachine<T>` implements `IComponentUser` | Pass | `OnComponentSupplied` stores via `SetComponent` |
| `Link(a, b)` / `Unlink(a, b)` symmetric peer ring | Pass | Static methods, bidirectional, null cleanup |
| Peers broadcast events laterally | Pass | `SendEventToPeers` + `ForwardedEvent<E>` envelope |
| Peers do NOT tick each other | Pass | No auto-ticking; caller ticks independently |
| Peers do NOT share components | Pass | Each machine has its own; no cross-peer delivery |
| Root `Update()` cascades through nested machines | Pass | Via `IState<T>.Update` -> `Update()` delegation |
| `ForwardedEvent<E>` envelope | Pass | Generic struct with `source` and `inner` |
| `PeerStateChangedEvent` | Pass | Struct with `source`, `from`, `to` |
| Event re-entry guard | Pass | `peerEventForwardingDepth` max 8 + depth counter pattern |
| Sub-machine states access parent components via snapshot | Pass | `IComponentUser.OnComponentSupplied` -> `SetComponent` |
| `GetStatePath()` debug output | Pass | Returns full nested path |
| `MultiTrackStateMachine` marked `[Obsolete]` | Pass | With migration message |
| Parameterless constructor + deferred subject | Pass | `protected StateMachine()` + `Subject { get; protected set; }` |
| Existing root machine usage unchanged | Pass | No behavioral change when no peers/nesting |

---

## Deviations

- **No .csproj build verification possible:** This is a Unity-only project with no standalone .csproj. Build verification was done via `dotnet build` on a temporary project excluding Unity-dependent files (`EnumExtension.cs`, `MultiTrackStateMachine*.cs`). 0 errors, 5 pre-existing nullable warnings.

---

## Issues Encountered

### Review Round 1 (Step 7) -- 4 issues
1. `NotifyPeersOfStateChange` lacked dispatch guard -- peer list could be corrupted if state handler called `Link()`/`Unlink()` during dispatch.
2. `DeliverComponents` NPE when parent machine has no components and enters a sub-machine -- `Components` null and `TypesDisabledAutoComponents` null.
3. `MultiTrackStateMachine.SendEvent` override did not call `SendEventToPeers` -- peer events silently dropped.
4. `ReceiveFromPeer` did not increment `peerEventForwardingDepth` on the receiver -- only sender's counter incremented, allowing A-B ping-pong.

### Review Round 2 (Step 8) -- 3 issues
1. `isPeerDispatching` boolean clobbered on re-entry -- nested dispatch resets flag prematurely. Fixed by replacing with `peerDispatchingDepth` counter.
2. `NotifyPeersOfStateChange` lacked sender-side depth limiting -- added `peerEventForwardingDepth` check.
3. Peer dispatch counters not reset on sub-machine exit -- `ResetInternalState` now zeroes both counters.

---

## Key Insights

### Boolean vs. depth counter for re-entrancy guards
The initial implementation used `bool isPeerDispatching` as a mutation guard. This breaks under re-entrant dispatch: the inner dispatch's finally block resets the flag to `false`, leaving the outer dispatch unguarded. The fix replaced it with `int peerDispatchingDepth` (increment/decrement), matching the existing `stateChangingDepth` pattern already used in `StateMachine.cs`. Lesson: any guard protecting a loop that can trigger callbacks must be a depth counter, not a boolean.

### Snapshot semantics for component delivery
Components are delivered to sub-machines as a one-time snapshot at entry, not as a live reference chain. This means parent `SetComponent` calls after entry do not propagate to the sub-machine. This is consistent with how components work for regular states (states receive components at entry, not continuously) and avoids the complexity of a parent reference or chain-walking.

### Direct broadcast vs. hub-and-spoke for peer events
The design uses direct lateral broadcast (each machine sends to all its own peers) rather than hub-and-spoke. `ReceiveFromPeer` delivers to own states only with NO re-broadcast, preventing duplication in fully-connected graphs and loops in chains. This is simpler and more predictable than routing through a central hub.
