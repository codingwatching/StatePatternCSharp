# Patch: Remove Host — Component Snapshot at Entry

> **Date:** 2026-03-31
> **Author:** Claude (agent)
> **Directive:** Patch `2603291500-machines-as-states-plan.md` per `2603311500-remove-host-component-snapshot-proposal.md`
> **Source Plan:** `2603291500-machines-as-states-plan.md`
> **Result:** Success

---

## Summary

Removed all `Host` property references from the machines-as-states plan and replaced the component prototype chain (Step 5) with entry-time component snapshot delivery via `IComponentUser`. Sub-machines receive parent components as a snapshot at entry — no parent reference needed.

---

## Changes

### Plan Metadata

**File:** `.projex/2603291500-machines-as-states-plan.md`
**Change Type:** Modified
**What Changed:**
- Added revision line for 2026-03-31 with proposal reference
- Added `2603311500-remove-host-component-snapshot-proposal.md` to Related Projex

### Summary & Scope

**What Changed:**
- Removed `Host` from summary, replaced with "snapshot at entry time via `IComponentUser`"
- Scope: removed `Host property (hierarchy-only)` and `component prototype chain`, added `IComponentUser on StateMachine<T> for entry-time component snapshot`

### Success Criteria

**What Changed:**
- Replaced `Host property on StateMachine<T>` criterion with `StateMachine<T> implements IComponentUser` criterion
- Replaced `Component prototype chain walks Host` criterion with snapshot-based language

### Constraints

**What Changed:**
- Replaced two `Host`-specific constraints (typing, facade exclusion) with no-Host constraint and `[DisableAutoComponents]` constraint

### Problem / Gap / Need

**What Changed:**
- Removed `Host is correct here` / `Host is wrong here` framing
- Replaced with component snapshot (hierarchy) and no-sharing (peers) descriptions

### Key Files Table

**What Changed:**
- `StateMachine.cs`: replaced `component chain helpers` with `IComponentUser impl, [DisableAutoComponents]`
- `StateMachine.AsState.cs`: removed `Host property (hierarchy-only)` from description
- `IStateMachine.cs`: simplified note (removed `Host stays on concrete class`)

### Implementation Overview

**What Changed:**
- Step 3 description: `IState<T> implementation + Host property` → `IState<T> implementation (no Host)`
- Step 5 description: `component prototype chain (hierarchy-only)` → `component snapshot at entry via IComponentUser`

### Step 3: IState<T> Implementation

**What Changed:**
- Removed `Host` property declaration from code block
- Removed `Host = machine;` from `OnEntered`
- Removed `Host = null;` from `OnLeaving`
- Updated doc comment: `Subject and Host are already set` → `Subject is already set`
- Updated rationale: removed Host justification, added "no Host — a nested machine is just a state"
- Updated verification: `sub.Host == parent` → `sub.Subject == parent.Subject`

### Step 5: Replaced Entirely

**What Changed:**
- Removed: `TryResolveComponent` (Host chain-walking resolution), `DeliverAllChainedComponents` (HashSet slow path), all `DeliverComponents` modifications
- Added: `StateMachine<T>` implements `IComponentUser`, `[DisableAutoComponents]` attribute, virtual `OnComponentSupplied` that calls `SetComponent`
- Objective, confidence, depends-on, rationale, verification, rollback all rewritten for snapshot approach

### Impact Analysis

**What Changed:**
- `Direct` line: `DeliverComponents` → `IComponentUser`

### Communication Parity Matrix

**What Changed:**
- Component row: added nested sub-machine snapshot delivery alongside peer isolation

### Verification Plan

**What Changed:**
- Manual: `Components` check reworded for snapshot semantics
- Acceptance criteria: replaced `Host property (nested/peer)` rows with `Component snapshot (nested)` and `Component isolation (peer)` rows; added `Component snapshot staleness` row

### Rollback Plan

**What Changed:**
- Step 5 rollback: `Revert DeliverComponents changes, remove chain helpers` → `Remove IComponentUser implementation and [DisableAutoComponents]`

### Risks

**What Changed:**
- Removed: `HashSet allocation in component chain` risk
- Added: `Stale components after parent SetComponent` risk (Medium likelihood, Low impact)

---

## Verification

**Method:** Manual review — all `Host` references in the plan searched and confirmed removed or replaced. Plan internal consistency checked (steps reference each other correctly, verification matches implementation).

**Status:** PASS

---

## Impact on Related Projex

| Document | Relationship | Update Made |
|----------|-------------|-------------|
| `2603291500-machines-as-states-plan.md` | Target plan | All Host references removed, Step 5 replaced with IComponentUser snapshot |
| `2603311500-remove-host-component-snapshot-proposal.md` | Source proposal | No changes needed — proposal references the plan, not vice versa |

---

## Notes

- The proposal's open question about `[DisableAutoComponents]` vs auto-detection was resolved in favor of the attribute (already noted as resolved in the proposal).
- The proposal's open question about future `Host` uses (event bubbling, debug tree) remains open — documented in the proposal, not the plan (out of scope).
