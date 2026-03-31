# Walkthrough: IStateMachine Facade + IState<T> Extraction

> **Execution Date:** 2026-03-29
> **Completed By:** Claude (agent)
> **Source Plan:** `2603281630-istatemachine-and-istate-extraction-plan.md`
> **Result:** Success

---

## Summary

Extracted `StateMachine<T>.IState` and `StateMachine<T>.IPopupState` to top-level `IState<T>` and `IPopupState<T>` interfaces, introduced `IStateMachine`/`IStateMachine<T>` facade interfaces, and updated all 14 source files accordingly. `StateMachine<T>` now implements `IStateMachine<T>` and all state lifecycle methods receive `IStateMachine<T>` instead of `StateMachine<T>`. Builds produce the same pre-existing failures as master — no regressions introduced.

---

## Objectives Completion

| Objective | Status | Notes |
|-----------|--------|-------|
| Top-level `IState<T>` with `IStateMachine<T>` machine param | Complete | `IState.cs` created |
| Top-level `IPopupState<T>` with `IStateMachine<T>` machine param | Complete | `IPopupState.cs` created |
| `IStateMachine` + `IStateMachine<T>` facade interfaces | Complete | `IStateMachine.cs` created |
| `StateMachine<T> : IStateMachine<T>` | Complete | Class declaration updated |
| Nested `StateMachine<T>.IState` removed | Complete | `StateMachine.IState.cs` deleted |
| Nested `StateMachine<T>.IPopupState` removed | Complete | `StateMachine.IPopupState.cs` deleted |
| All IState refs updated to `IState<T>` | Complete | grep confirms zero unqualified refs |
| All IPopupState refs updated to `IPopupState<T>` | Complete | grep confirms zero unqualified refs |
| `IEventReceiverState<T,E>` machine param updated | Complete | `IEventReceiverState.cs` updated |
| `MonoBehaviourStateMachine` type checks updated | Complete | 3 pattern matches + strings updated |
| `FixedUpdate`/`LateUpdate` normalized to parameterless | Complete | `StateMachine.cs` + MonoBehaviour call sites |
| Build passes (no new errors) | Complete | Pre-existing `EnumExtension.cs` error unchanged |

---

## Execution Detail

### Steps 1 + 2 + 2b (atomic group): Create IStateMachine.cs, IState.cs, IPopupState.cs

**Planned:** Create three new interface files as an atomic group before any compilation check.

**Actual:** All three created as new files. `IStateMachine.cs` defines non-generic `IStateMachine` (tick methods, pause/update flags, #if Unity guards) and generic `IStateMachine<T>` (transitions, popup management, events). `IState.cs` and `IPopupState.cs` define the extracted interfaces with `IStateMachine<T>` machine params and default `FixedUpdate`/`LateUpdate` implementations under `#if UNITY_2017_1_OR_NEWER`.

**Deviation:** Plan had `IReadOnlyCollection<IPopupState<T>>?` (nullable) in `IStateMachine<T>.ViewPopupStates()`. Actual source (`StateMachine.cs:108`) already used the nullable `?` form, so the interface was written to match. No functional deviation.

**Files Changed:**
| File | Change Type | Planned? |
|------|-------------|----------|
| `IStateMachine.cs` | Created | Yes |
| `IState.cs` | Created | Yes |
| `IPopupState.cs` | Created | Yes |

---

### Step 3: Normalize FixedUpdate/LateUpdate on StateMachine<T>

**Planned:** Remove `(IStateMachine<T> machine, T subject)` params from `FixedUpdate` and `LateUpdate` in `StateMachine.cs`. Update call sites in `unity/MonoBehaviourStateMachine.cs`.

**Actual:** Exact as planned. `StateMachine.cs` L287/L300 signatures changed to `public virtual void FixedUpdate()` / `public virtual void LateUpdate()`. `MonoBehaviourStateMachine.cs` L101/L107 changed from `Machine.FixedUpdate(Machine, Machine.Subject)` to `Machine.FixedUpdate()`.

**Deviation:** None.

**Files Changed:**
| File | Change Type | Planned? |
|------|-------------|----------|
| `StateMachine.cs` | Modified | Yes |
| `unity/MonoBehaviourStateMachine.cs` | Modified | Yes |

---

### Step 4: Delete nested IState/IPopupState and update all references

**Planned:** Delete two nested interface files and replace all `IState`/`IPopupState` references across 11 source files.

**Actual:** Applied replacement discipline from plan (qualified form first, then unqualified). All 11 files updated. Deletions staged via `del-n-stage.ps1`. Key changes: machine params on all `IState<T>` / `IPopupState<T>` method signatures changed from `StateMachine<T>` to `IStateMachine<T>`; all `Dictionary<Type, IState>` / `List<IPopupState>` field types updated; all event types updated.

**Deviation:** None.

**Files Changed:**
| File | Change Type | Planned? |
|------|-------------|----------|
| `StateMachine.IState.cs` | Deleted | Yes |
| `StateMachine.IPopupState.cs` | Deleted | Yes |
| `StateMachine.cs` | Modified | Yes |
| `StateMachine.NoOpState.cs` | Modified | Yes |
| `StateMachine.ObserverTransitionState.cs` | Modified | Yes |
| `StateMachine.MainStateChangedEvent.cs` | Modified | Yes |
| `StateMachine.NewPopupStateEvent.cs` | Modified | Yes |
| `StateMachine.PopupStateEndedEvent.cs` | Modified | Yes |
| `MultiTrackStateMachine.cs` | Modified | Yes |
| `MultiTrackStateMachine.SideTrackStateChangedEvent.cs` | Modified | Yes |
| `IStateResolver.cs` | Modified | Yes (doc comment) |
| `unity/StateMachine.TimedPopupState.cs` | Modified | Yes |

---

### Steps 5 + 6 + 7: Class declaration, IEventReceiverState, MonoBehaviourStateMachine

**Planned:** Three independent changes.

**Actual:** Step 5 — added `: IStateMachine<T>` to `public partial class StateMachine<T>` in `StateMachine.cs`. Step 6 — changed `IEventReceiverState<T,E>.ReceiveEvent` machine param from `StateMachine<T>` to `IStateMachine<T>`. Step 7 — replaced all `StateMachine<T>.IState` pattern matches in `unity/MonoBehaviourStateMachine.cs` with `IState<T>`, and updated two strings (tooltip + warning message).

**Deviation:** None.

**Files Changed:**
| File | Change Type | Planned? |
|------|-------------|----------|
| `StateMachine.cs` | Modified | Yes |
| `IEventReceiverState.cs` | Modified | Yes |
| `unity/MonoBehaviourStateMachine.cs` | Modified | Yes |

---

## Complete Change Log

> Derived from `git diff --stat master..HEAD` — 19 files, 209 insertions, 121 deletions.

### Files Created
| File | Purpose | In Plan? |
|------|---------|----------|
| `IStateMachine.cs` | Non-generic + generic facade interfaces | Yes |
| `IState.cs` | Top-level IState<T> | Yes |
| `IPopupState.cs` | Top-level IPopupState<T> | Yes |

### Files Modified
| File | Changes | In Plan? |
|------|---------|----------|
| `StateMachine.cs` | +IStateMachine<T> impl, parameterless FixedUpdate/LateUpdate, all IState/IPopupState refs updated | Yes |
| `MultiTrackStateMachine.cs` | All IState/IPopupState refs updated | Yes |
| `StateMachine.NoOpState.cs` | Base → IState<T>, machine params → IStateMachine<T> | Yes |
| `StateMachine.ObserverTransitionState.cs` | Base + constraints → IState<T>, machine params → IStateMachine<T> | Yes |
| `StateMachine.MainStateChangedEvent.cs` | Field types → IState<T> | Yes |
| `StateMachine.NewPopupStateEvent.cs` | Field types → IPopupState<T> | Yes |
| `StateMachine.PopupStateEndedEvent.cs` | Field types → IPopupState<T> | Yes |
| `MultiTrackStateMachine.SideTrackStateChangedEvent.cs` | Field types → IState<T> (was qualified form) | Yes |
| `IEventReceiverState.cs` | Machine param → IStateMachine<T> | Yes |
| `IStateResolver.cs` | Doc comment updated | Yes |
| `unity/MonoBehaviourStateMachine.cs` | FixedUpdate/LateUpdate call sites + 3 type pattern matches + 2 strings | Yes |
| `unity/StateMachine.TimedPopupState.cs` | Base → IPopupState<T>, machine params → IStateMachine<T> | Yes |

### Files Deleted
| File | Reason | In Plan? |
|------|--------|----------|
| `StateMachine.IState.cs` | Replaced by top-level `IState.cs` | Yes |
| `StateMachine.IPopupState.cs` | Replaced by top-level `IPopupState.cs` | Yes |

### Planned But Not Changed
None — all planned files were addressed.

---

## Success Criteria Verification

| Criterion | Method | Result | Evidence |
|-----------|--------|--------|----------|
| Top-level `IState<T>` exists with `IStateMachine<T>` machine param | File read | PASS | `IState.cs` created, all 4 methods use `IStateMachine<T>` |
| `IStateMachine` (non-generic) with tick methods + flags | File read | PASS | `IStateMachine.cs` L5-22 |
| `IStateMachine<T>` (generic) with transitions, popups, events | File read | PASS | `IStateMachine.cs` L29-51 |
| `StateMachine<T> : IStateMachine<T>` | grep | PASS | `StateMachine.cs:11` confirms |
| Nested `StateMachine<T>.IState` removed | File existence check | PASS | `ls` returns no such file |
| `IEventReceiverState<T,E>` uses `IStateMachine<T>` | File read | PASS | `IEventReceiverState.cs:10` |
| `MonoBehaviourStateMachine` uses `IState<T>` | File read | PASS | 3 pattern matches updated |
| Top-level `IPopupState<T>` exists | File read | PASS | `IPopupState.cs` created |
| Nested `StateMachine<T>.IPopupState` removed | File existence check | PASS | `ls` returns no such file |
| `IStateMachine<T>` includes popup methods | File read | PASS | `IStateMachine.cs` L40-46 |
| `IStateMachine.UpdatePaused` is `{ get; }` only | File read | PASS | `IStateMachine.cs:L7` |
| Zero stale qualified `StateMachine<T>.IState` refs | grep | PASS | Empty result |
| Zero unqualified `IState` (without `<T>`) refs | grep | PASS | Empty result |
| `dotnet build` — no new errors | Build run | PASS | Pre-existing `EnumExtension.cs` Unity guard error unchanged on master and branch |
| `dotnet build /p:DefineConstants=UNITY_2017_1_OR_NEWER` — no new errors | Build run | PASS | Same pre-existing failures as master |

**Overall: 15/15 criteria passed**

---

## Deviations from Plan

### Deviation 1: Steps 1/2/2b committed together with Step 3
- **Planned:** Steps 1/2/2b were marked as an atomic group but implied separate commit from Step 3.
- **Actual:** All four were committed in a single commit `14149b7` — interface files were created and immediately committed alongside the Step 3 signature changes.
- **Reason:** More efficient; all three new interface files had no standalone compilation anyway.
- **Impact:** None — all changes are present.

### Deviation 2: `projex-worktree.ps1` required a fix
- **Planned:** Script used as-is.
- **Actual:** Script had a PowerShell `$ErrorActionPreference = "Stop"` + `2>&1` issue causing `NativeCommandError` on the branch-existence check. Fixed by wrapping line 40 with temporary `SilentlyContinue`.
- **Impact:** Minor — one extra fix before worktree creation. Script now more robust.

---

## Issues Encountered

### Issue 1: `projex-worktree.ps1` PowerShell error action preference
- **Description:** `2>&1 | Out-Null` on a git command with non-zero exit code threw a terminating `NativeCommandError` under `$ErrorActionPreference = "Stop"`.
- **Severity:** Low
- **Resolution:** Temporarily set `$ErrorActionPreference = "SilentlyContinue"` around the branch-existence check.

### Issue 2: `del-n-stage.ps1` files already staged — can't re-add in projex-commit
- **Description:** `projex-commit` tried to `git add` the deleted files, which were already staged by `del-n-stage.ps1`, causing a `pathspec did not match any files` error.
- **Severity:** Low
- **Resolution:** Omitted the deleted files from `projex-commit` arguments — already-staged deletions don't need re-adding.

---

## Key Insights

### Lessons Learned

1. **Replacement discipline prevents token collisions**
   - `IState` is a prefix of `IStateMachine` and `IStateResolver`. Applying qualified forms first and using `\b` word-boundary patterns ensures clean substitution without corrupting sibling tokens.

2. **Implicit upcast handles body call sites**
   - None of the method bodies that pass `this` to state lifecycle calls needed textual changes — `StateMachine<T>` satisfies `IStateMachine<T>` via implicit upcast. Recognizing this up-front kept the scope tight.

3. **Atomic interface groups simplify execution**
   - Steps 1/2/2b (three mutually-referencing interface files) are meaningless in isolation. Treating them as a single commit unit was simpler than staged partial compilation checks.

### Gotchas / Pitfalls

1. **PowerShell `$ErrorActionPreference = "Stop"` + native command stderr**
   - Trap: `2>&1 | Out-Null` on a git command that writes stderr (even for expected non-zero exit) throws a terminating exception before `$LASTEXITCODE` is checked.
   - Avoidance: Wrap the native command in a `$ErrorActionPreference = "SilentlyContinue"` scope, or use `$null = git ... 2>&1`.

2. **del-n-stage.ps1 stages deletions immediately — don't re-add in projex-commit**
   - Trap: Passing deleted file paths to `projex-commit` causes a pathspec error since they're already staged and no longer on disk.
   - Avoidance: Only pass modified/created files to `projex-commit`; let the pre-staged deletions ride along automatically.

---

## Recommendations

### Immediate Follow-ups
- [ ] Machines-as-states (`StateMachine<T> : IState<T>`) — now unblocked, see `2603201530-statemachines-as-states-imagine.md`
- [ ] Attach/detach parallel machines — now unblocked, see `2603251600-attach-detach-parallel-machines-proposal.md`

### Future Considerations
- `EnumExtension.cs` uses `Unity.Collections.LowLevel.Unsafe` without `#if UNITY_2017_1_OR_NEWER` guard — causes `dotnet build` failure on non-Unity SDK. Wrapping it in the guard would fix the baseline build.
- CS8625 nullable warnings on `object parameter = null` defaults are cosmetic but pervasive. Changing to `object? parameter = null` project-wide would clean them up.

---

## Related Projex

### Source
- `2603281600-statemachine-driver-interface-proposal.md` (Option C implemented)
- `2603281700-istatemachine-and-istate-extraction-plan-redteam.md` (red team addressed before execution)

### Unblocked
- `2603201530-statemachines-as-states-imagine.md`
- `2603251600-attach-detach-parallel-machines-proposal.md`
- `20260311-monobehaviour-statemachine-proposal.md`
