# Walkthrough: Extract IState Interface from IState<T>

> **Execution Date:** 2026-03-25
> **Completed By:** Claude (agent)
> **Source Plan:** `20260312-istate-extraction-plan.md`
> **Result:** Success

---

## Summary

Converted `StateMachine<T>.State` from an abstract class to `StateMachine<T>.IState` interface across 7 source files, renamed `StateMachine.State.cs` → `StateMachine.IState.cs`, and verified the build is clean (1 pre-existing Unity error in `EnumExtension.cs`, 0 new errors). Two deviations from plan were required: `ObserverTransitionState` needed an explicit `public abstract void Reset()` declaration (plan incorrectly assumed implicit abstract), and a `class` constraint on `TO` was needed to preserve `null` as a valid default parameter.

---

## Objectives Completion

| Objective | Status | Notes |
|-----------|--------|-------|
| `State` → `IState`, rename file | Complete | `git mv` + rewrite |
| Default interface methods for Unity | Complete | `void FixedUpdate(...) {}` syntax, no `virtual` |
| `NoOpState` implements `IState`, no `override` | Complete | |
| `ObserverTransitionState` implements `IState`, constraints updated | Complete | Extra: `abstract void Reset()` + `class` constraint on `TO` |
| Event structs use `IState` fields | Complete | |
| All `StateMachine<T>` / `MultiTrackStateMachine<T,TRACK>` signatures | Complete | |
| Build clean (non-Unity) | Complete | 1 pre-existing error unrelated to this work |

---

## Execution Detail

### Step 1: Convert StateMachine.State.cs → StateMachine.IState.cs

**Planned:** `git mv` file, rewrite `abstract class State` → `interface IState`, remove `public abstract`/`public virtual` modifiers, update `State` params to `IState`.

**Actual:** Used `move-n-stage.sh` for the rename, then `Write` tool to rewrite content. Exact transformations matched plan.

**Deviation:** None.

**Files Changed:**
| File | Change Type | Planned? | Details |
|------|-------------|----------|---------|
| `StateMachine.IState.cs` | Renamed + Modified | Yes | `abstract class State` → `interface IState`; 4 abstract methods → interface members; Unity block uses default interface method syntax |
| `StateMachine.State.cs` | Deleted (via rename) | Yes | Removed from git tracking |

---

### Step 2: Update StateMachine.cs

**Planned:** ~20 `State` → `IState` replacements across properties, events, dictionaries, method signatures, generic constraints.

**Actual:** 9 targeted `Edit` calls covering all 14 listed occurrences.

**Deviation:** None.

**Files Changed:**
| File | Change Type | Planned? | Details |
|------|-------------|----------|---------|
| `StateMachine.cs` | Modified | Yes | `CurrentState`, `OnStateChanging/Changed` events, `AutoStateCache`, `ChangeState(State)`, `ChangeState<S>` constraint + dict init, `DeliverComponents`, `PreStateChange`, `PostStateChange`, `Cache<S>` constraint + dict init, `SendEvent<S,E>` constraint |

---

### Step 3: Update StateMachine.NoOpState.cs

**Planned:** `: State` → `: IState`, remove `override`, update param types.

**Actual:** Full file rewrite (cleaner than 4 separate edits for a 12-line file).

**Deviation:** None.

**Files Changed:**
| File | Change Type | Planned? | Details |
|------|-------------|----------|---------|
| `StateMachine.NoOpState.cs` | Modified | Yes | `: IState`, all 4 methods without `override`, `IState previous/next` params |

---

### Step 4: Update StateMachine.ObserverTransitionState.cs

**Planned:** Base type + constraints update, remove `override` from 3 methods. Plan stated no change needed for `Reset`.

**Actual:** 4 `Edit` calls for base type/constraints/method signatures. Two additional changes required beyond plan:
1. Added `public abstract void Reset()` — C# requires explicit abstract declaration; compiler raises CS0535 otherwise (plan's claim of implicit abstract was incorrect).
2. Added `class` constraint to `TO` — original `where TO : State` implicitly restricted TO to reference types, making `null` valid as a default parameter value for `TO instance = null`. `where TO : IState` alone permits structs, breaking this.

**Deviation:** Two unplanned additions — both required by C# semantics.

**Files Changed:**
| File | Change Type | Planned? | Details |
|------|-------------|----------|---------|
| `StateMachine.ObserverTransitionState.cs` | Modified | Yes | `: IState`, `FROM : IState`, `TO : class, IState, new()`, removed `override` from 3 methods, `IState previous/next` params, added `public abstract void Reset()` |

---

### Step 5: Update StateMachine.MainStateChangedEvent.cs

**Planned:** Replace `State from, to` fields and constructor params.

**Actual:** Used `replace_all` on bare `State` → `IState`. This corrupted `StatePattern` → `IStatePattern`, `StateMachine<T>` → `IStateMachine<T>`, and `MainStateChangedEvent` → `MainIStateChangedEvent` in the namespace/class/struct declarations. Build verification caught this. File was rewritten from scratch with correct content.

**Deviation:** `replace_all` over-reach corrupted the file. Required a full rewrite instead of a targeted edit.

**Files Changed:**
| File | Change Type | Planned? | Details |
|------|-------------|----------|---------|
| `StateMachine.MainStateChangedEvent.cs` | Modified | Yes | `IState from, to` fields and constructor params; namespace/class/struct names restored after corruption |

---

### Step 6: Update MultiTrackStateMachine.cs

**Planned:** ~15 `State` → `IState` replacements.

**Actual:** 6 targeted `Edit` calls (some batched adjacent lines). All 10 occurrences from plan covered.

**Deviation:** None.

**Files Changed:**
| File | Change Type | Planned? | Details |
|------|-------------|----------|---------|
| `MultiTrackStateMachine.cs` | Modified | Yes | Array init, `SideTracks` type, both events, `AutoSideTrackStateCache` type, `ChangeSideTrackState(TRACK, State)` param, `ChangeSideTrackState<S>` constraint + dict init, `PreSideTrackStateChange` + `PostSideTrackStateChange` params |

---

### Step 7: Update MultiTrackStateMachine.SideTrackStateChangedEvent.cs

**Planned:** Replace `State from, to` fields and constructor params.

**Actual:** Same `replace_all` mistake as Step 5 — corrupted namespace, class, and struct names. Caught at build. Rewritten with `StateMachine<T>.IState` qualification (necessary since this file is inside `MultiTrackStateMachine<T,TRACK>`, not `StateMachine<T>`, so the nested type needs explicit qualification).

**Deviation:** `replace_all` over-reach. Required rewrite. Also: uses `StateMachine<T>.IState` (qualified) while `MainStateChangedEvent` uses unqualified `IState` — both correct, style inconsistency noted.

**Files Changed:**
| File | Change Type | Planned? | Details |
|------|-------------|----------|---------|
| `MultiTrackStateMachine.SideTrackStateChangedEvent.cs` | Modified | Yes | `StateMachine<T>.IState from, to` fields and constructor params |

---

### Verification

**Build:** `dotnet build StatePattern.csproj` — 18 warnings (pre-existing nullability), 1 error (`EnumExtension.cs:3` Unity reference — pre-existing), 0 new errors.

**Grep check:** `grep -rn "\bState\b" *.cs` filtered for type references → zero hits. One string literal `"internally a State[{0}]"` in `MultiTrackStateMachine.cs:23` intentionally left (log message, not a type reference).

---

## Complete Change Log

> Derived from `git diff --stat master..projex/20260312-istate-extraction`

### Files Created
| File | Purpose | In Plan? |
|------|---------|----------|
| `StateMachine.IState.cs` | `IState` interface definition (renamed from `StateMachine.State.cs`) | Yes |
| `.projex/20260312-istate-extraction-log.md` | Execution log | Yes (projex process) |

### Files Modified
| File | Changes | In Plan? |
|------|---------|----------|
| `StateMachine.cs` | 14 `State` → `IState` type references | Yes |
| `StateMachine.NoOpState.cs` | `: IState`, removed `override`, updated params | Yes |
| `StateMachine.ObserverTransitionState.cs` | Base type, constraints, params, `abstract Reset()` | Yes + deviation |
| `StateMachine.MainStateChangedEvent.cs` | Field and constructor param types | Yes |
| `MultiTrackStateMachine.cs` | 10 `State` → `IState` type references | Yes |
| `MultiTrackStateMachine.SideTrackStateChangedEvent.cs` | Field and constructor param types (qualified) | Yes |
| `.projex/20260312-istate-extraction-plan.md` | Status → Complete; corrected C# assumption notes | Projex process |

### Files Deleted
| File | Reason | In Plan? |
|------|--------|----------|
| `StateMachine.State.cs` | Renamed to `StateMachine.IState.cs` via `git mv` | Yes |

---

## Success Criteria Verification

| Criterion | Method | Result | Evidence |
|-----------|--------|--------|----------|
| `State` → `IState` everywhere | grep + build | Pass | Zero type-ref matches; 0 new build errors |
| `StateMachine.IState.cs` exists, `State.cs` does not | File read + git status | Pass | `public interface IState` confirmed |
| Default interface methods in Unity block | File read | Pass | `void FixedUpdate(...) {}` without `virtual` |
| `NoOpState : IState`, no `override` | File read + grep | Pass | |
| `ObserverTransitionState : IState`, constraints updated | File read | Pass | `where TO : class, IState, new()` |
| Event structs use `IState` | File read | Pass | `MainStateChangedEvent`: `IState`; `SideTrackStateChangedEvent`: `StateMachine<T>.IState` |
| Build clean (non-Unity) | `dotnet build StatePattern.csproj` | Pass | 1 pre-existing error only |

**Overall: 7/7 criteria passed**

---

## Deviations from Plan

### Deviation 1: `replace_all` corrupted event files (Steps 5 & 7)
- **Planned:** Replace `State` field/param types with `IState`
- **Actual:** `replace_all` on bare `State` matched substrings in namespace (`StatePattern`→`IStatePattern`), class name (`StateMachine`→`IStateMachine`), and struct name (`StateChangedEvent`→`IStateChangedEvent`). Both files rewritten from scratch.
- **Reason:** `replace_all` is not safe for short tokens that appear as substrings in other identifiers
- **Impact:** Required rewrites instead of targeted edits; caught during build verification; no functional impact on final output
- **Recommendation:** Avoid `replace_all` on short tokens. Use targeted `Edit` with surrounding context.

### Deviation 2: `ObserverTransitionState` needed explicit `abstract void Reset()`
- **Planned:** No change for `Reset` — plan stated abstract class leaves it "implicitly abstract"
- **Actual:** Added `public abstract void Reset()` — compiler raised CS0535 without it
- **Reason:** Plan's C# assumption was incorrect. C# does not make unimplemented interface members implicitly abstract in abstract classes.
- **Impact:** One additional line added; correct behavior preserved
- **Recommendation:** Plan Notes section corrected to reflect actual C# behavior.

### Deviation 3: `class` constraint added to `TO` in `ObserverTransitionState`
- **Planned:** `where TO : IState, new()`
- **Actual:** `where TO : class, IState, new()`
- **Reason:** `where TO : State` previously enforced reference-type-only; `where TO : IState` alone allows value types, making `TO instance = null` an invalid default parameter
- **Impact:** Narrows TO to class types only (structs excluded) — matches original intent
- **Recommendation:** Plan Step 4 updated to include this constraint.

---

## Issues Encountered

### Issue 1: `replace_all` token collision (Steps 5 & 7)
- **Description:** Bare `State` matched inside `StatePattern`, `StateMachine`, and event struct names
- **Severity:** Medium (caught before commit; build verification is the gate)
- **Resolution:** Full file rewrites with targeted content
- **Prevention:** Use `Edit` with sufficient surrounding context for short tokens

### Issue 2: Plan's C# abstract/interface assumption
- **Description:** Plan stated abstract classes implicitly make unimplemented interface members abstract — this is false
- **Severity:** Low (caught at build; easy fix)
- **Resolution:** Added `public abstract void Reset()` explicitly; plan Notes corrected

---

## Key Insights

### Lessons Learned

1. **`replace_all` on short tokens is unsafe**
   - Context: Steps 5 and 7 used `replace_all` on `State` (4 chars) which appears in many surrounding identifiers
   - Insight: Short tokens that are substrings of other words in the file require surrounded-context edits or `stage-by-pattern` for safe replacement
   - Application: Always check whether the replacement token appears as a substring in non-target identifiers before using `replace_all`

2. **Abstract class + interface: explicit abstract required**
   - Context: `ObserverTransitionState` is abstract and implements `IState` but doesn't implement `Reset()`
   - Insight: C# does NOT implicitly abstract unimplemented interface members in abstract classes. CS0535 is raised. Must declare `public abstract void Reset()`.
   - Application: Any future abstract class implementing an interface must explicitly declare all unimplemented members as `abstract`

3. **Class-constraining generic type parameters when converting from class to interface**
   - Context: `where TO : State, new()` → `where TO : IState, new()` broke `null` as default parameter
   - Insight: A base-class constraint implicitly restricts the type parameter to reference types. Replacing with an interface constraint removes this restriction. Add explicit `class` constraint when the null-ability of the parameter matters.
   - Application: Review all generic constraints when converting class → interface; check if null-as-default is used anywhere

### Gotchas / Pitfalls

1. **Qualified vs unqualified nested type access from derived classes**
   - Trap: `IState` inside `MultiTrackStateMachine<T,TRACK>` (derived from `StateMachine<T>`) couldn't be resolved without `StateMachine<T>.IState` qualification
   - How encountered: `SideTrackStateChangedEvent.cs` had `IState not found` error
   - Avoidance: When using nested types from a base class in a derived class's nested types, use the fully qualified form

---

## Recommendations

### Immediate Follow-ups
- [ ] Run `/close-projex` and squash-merge to `master`
- [ ] Review and start `20260311-monobehaviour-statemachine-proposal.md` (now unblocked)
- [ ] Review and start `20260311-serializable-states-and-changestate-parity-proposal.md` (now unblocked)

### Future Considerations
- `TO : class` constraint in `ObserverTransitionState` could be relaxed in future if there's a need for struct states, but would require redesigning the null-instance pattern
- `SideTrackStateChangedEvent` uses qualified `StateMachine<T>.IState` while `MainStateChangedEvent` uses unqualified `IState` — could be aligned in a future cleanup pass

### Plan Improvements
- Step 4 should have included `class` constraint on `TO` from the start
- Step 4 should have included `public abstract void Reset()` from the start
- Steps 5 and 7 should use targeted `Edit` calls instead of `replace_all`

---

## Related Projex Updates

| Document | Update |
|----------|--------|
| `20260312-istate-extraction-plan.md` | Status → Complete; Notes corrected; walkthrough linked |
| `20260311-serializable-states-and-changestate-parity-proposal.md` | Blocking dependency resolved |
| `20260311-monobehaviour-statemachine-proposal.md` | Blocking dependency resolved |
