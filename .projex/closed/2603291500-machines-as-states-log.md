# Execution Log: 2603291500-machines-as-states

> **Plan:** `.projex/2603291500-machines-as-states-plan.md`
> **Branch:** `projex/2603291500-machines-as-states`
> **Started:** 2026-04-01
> **Status:** Complete

---

## Steps

### Step 1: Foundation — Subject settability + parameterless constructor
- **Status:** Done
- **Files:** `StateMachine.cs`
- **Changes:** `Subject` changed from `{ get; }` to `{ get; protected set; }`. Added `protected StateMachine()` parameterless constructor before existing one.

### Step 2: Link/Unlink Peer Ring
- **Status:** Done
- **Files:** `StateMachine.Link.cs` (new)
- **Changes:** Created `StateMachine.Link.cs` with peer ring implementation: `List<StateMachine<T>> peers`, `isPeerDispatching` guard, `Peers` property, static `Link(a, b)` and `Unlink(a, b)` methods.

### Step 3: Implement IState<T> on StateMachine<T>
- **Status:** Done
- **Files:** `StateMachine.AsState.cs` (new)
- **Changes:** Created `StateMachine.AsState.cs` with explicit `IState<T>` implementation (OnEntered, Update, OnLeaving, Reset, FixedUpdate, LateUpdate), virtual hooks (OnNestedEntry, ResetInternalState), and `GetStatePath()` debug method.

### Step 4: Event Bridging — Direct Lateral Broadcast
- **Status:** Done
- **Files:** `StateMachine.cs`, `StateMachine.Link.cs`, `StateMachine.PeerStateChangedEvent.cs` (new), `StateMachine.ForwardedEvent.cs` (new)
- **Changes:** Added `SendEventToPeers(ev)` call in `SendEvent`, added `NotifyPeersOfStateChange` call in `PostStateChange`. Added `peerEventForwardingDepth` guard, `SendEventToPeers`, `NotifyPeersOfStateChange`, `ReceiveFromPeer` to Link partial. Created `PeerStateChangedEvent` and `ForwardedEvent<E>` structs.

### Step 5: Component Snapshot — IComponentUser on StateMachine
- **Status:** Done
- **Files:** `StateMachine.cs`
- **Changes:** Added `[DisableAutoComponents]` attribute and `IComponentUser` to class declaration. Added `OnComponentSupplied(Type, object)` virtual method that stores components locally for sub-machine delivery.
- **Note:** Pre-existing potential NPE in `DeliverComponents` if `TypesDisabledAutoComponents` is null when a state is `IComponentUser` — not addressed here (out of scope).

### Step 6: Deprecation
- **Status:** Done
- **Files:** `MultiTrackStateMachine.cs`, `MultiTrackStateMachine.SideTrackStateChangedEvent.cs`
- **Changes:** Added `[System.Obsolete("Use StateMachine<T> with Link() for parallel composition.")]` to `MultiTrackStateMachine` class. Added `[System.Obsolete("Use StateMachine<T>.PeerStateChangedEvent instead.")]` to `SideTrackStateChangedEvent` struct.

---

## Log Entries

(entries will be appended below)

- **2026-04-01** All 6 steps executed and committed. Build verification passed (0 errors, 5 pre-existing nullable warnings). Pre-existing Unity-dependent files (`EnumExtension.cs`, `MultiTrackStateMachine*.cs`) excluded from dotnet verification as they depend on Unity assemblies not available outside the editor.
- **Note:** Pre-existing potential NPE in `DeliverComponents` — `TypesDisabledAutoComponents` dictionary may be null when accessed. Not addressed (out of scope).

### Step 7: Code Review Fixes
- **Status:** Done
- **Files:** `StateMachine.Link.cs`, `StateMachine.cs`, `MultiTrackStateMachine.cs`
- **Changes:**
  1. **(Critical)** Added `isPeerDispatching = true` guard with try/finally in `NotifyPeersOfStateChange` — prevents peer list corruption if a state handler calls `Link()`/`Unlink()` during `PeerStateChangedEvent` dispatch.
  2. **(Critical)** Added null guards in `DeliverComponents` — early return if `Components` is null or empty; lazy-init `TypesDisabledAutoComponents` if null. Prevents `NullReferenceException` when parent machine has no components and enters a sub-machine.
  3. **(Important)** Added `SendEventToPeers(ev)` call in `MultiTrackStateMachine.SendEvent` override — ensures peer events are forwarded even when using the deprecated multi-track class.
  4. **(Important)** Added `peerEventForwardingDepth` increment/check with try/finally in `ReceiveFromPeer` — prevents infinite A-B ping-pong loops where only the sender's depth counter was incrementing.

### [20260401 15:00] - Step 8: Review Round 2 Fixes
**Action:** Fixed 3 issues: (1) replaced isPeerDispatching boolean with peerDispatchingDepth counter to prevent re-entry clobber, (2) added peerEventForwardingDepth tracking to NotifyPeersOfStateChange for sender-side depth limiting, (3) reset peer dispatch counters in ResetInternalState
**Result:** Dispatch guards now re-entry safe. Sub-machine exit cleans up poisoned counters.
**Status:** Success
