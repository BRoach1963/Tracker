# Pro Cohere CR Fix Plan (Staged, Incremental, No-Style-Drift)

**Audience:** Claude (implementation), Brian (review)  
**Product:** Pro Cohere (Avalonia Desktop)  
**Purpose:** Restore intended surface semantics (Briefing, Pulse, Me, Circle, Browse, Projects) without degrading performance, introducing constant loading states, or changing the existing visual styling.

---

## 0) Absolute Guardrails (Non‑Negotiable)

### 0.1 Styling must NOT change
- Preserve existing layout, spacing, typography, colors, and visual hierarchy.
- No redesigns, no theme changes, no new visual language.
- Allowed: **small, subtle UI affordances only** (chips, badges, micro‑animations).

### 0.2 No constant reloads
- Do NOT reload full data sets on every navigation.
- Cached data must render immediately.

### 0.3 No field‑level refresh logic
- Refresh decisions are **surface‑level only**.

### 0.4 No reliance on manual refresh
- Refresh buttons may remain, but correctness must be automatic.

### 0.5 Incremental and reversible
- Each step must:
  - compile independently
  - be testable in isolation
  - avoid architectural rewrites
  - build toward the final state

---

## 1) Shared Concept (Referenced Throughout)
### Surface Refresh Policy (Conceptual Only)
This is the agreed solution direction. **Do not over‑implement early.**

For each surface:
1. Render cached data immediately.
2. On surface activation, perform a cheap freshness check.
3. If unchanged → do nothing.
4. If changed → refresh in background.
5. Show a non‑blocking “Updating…” indicator **only if noticeable**.
6. In‑app edits mark affected surfaces “dirty.”

---

## 2) Execution Order (Primary Phases)

1. **Briefing** (P0 – narrative anchor)  
2. **Pulse** (P0 – differentiation surface)  
3. **Me** (P1 – personal trust surface)  
4. **Circle** (P1 – hierarchy & visibility)  
5. **Browse Pages** (P2 – consistency & destinations)  
6. **Projects** (P2 – operational completeness)  
7. **Cross‑cutting polish** (micro‑interactions only)

---

# Phase 1 — Briefing (P0)

## Intent
- Briefing is a **time‑scoped, prioritized work queue**
- “Today” is the default mental model
- It answers: *what should I work on now?*

## Current Drift
- Acts like a persistent alert dashboard
- Time scope sticks across sessions
- No semantic “enter Briefing” moment

## Incremental Work Plan

### Step 1 — Surface activation contract
- Add `OnSurfaceActivated()` to `BriefingViewModel`
- This becomes the single entry point for refresh logic

**Acceptance**
- Method is idempotent and safe to call repeatedly

---

### Step 2 — Time scope reassertion
Rules:
- Default to **Today**
- Preserve explicit user choice within a session
- Reassert Today if:
  - calendar day changed, OR
  - last refresh exceeds threshold (30–60 min)

**Acceptance**
- Briefing resets naturally the next day

---

### Step 3 — Non‑blocking refresh indicator
- Add a subtle header chip:
  - Idle / Updating… / Updated
- Show Updating only after ~400ms

**Acceptance**
- Cached data always renders immediately

---

### Step 4 — Queue prioritization
Ensure stable ordering:
1. Overdue tasks
2. Due‑soon tasks
3. Goals needing attention
4. Metrics needing attention

**Acceptance**
- Two users agree on “what’s next”

---

### Step 5 — Dirty triggers
- Completing/editing tasks, goals, metrics marks Briefing dirty
- On activation, dirty → background refresh

**Acceptance**
- No manual refresh required after edits

---

### Optional polish
- 200–300ms fade for “Updated” chip
- Gentle list settle animation on item removal

---

# Phase 2 — Pulse (P0)

## Intent
Pulse is **synthesis**, not a list:
- Attention Required
- What Changed
- Recent Discussions (narrative continuity)
- Actions Taken

## Current Drift
- Loads once, can go stale
- Discussions are itemized, not grouped
- Role‑aware time window not enforced

## Incremental Work Plan

### Step 1 — Surface activation hook
- Add `OnSurfaceActivated()` to `PulseViewModel`

---

### Step 2 — Non‑blocking refresh indicator
- Same header chip pattern as Briefing

---

### Step 3 — Reduce overlap with Briefing
- Attention Required = fewer, higher‑confidence items
- Only include Briefing‑like items when Pulse adds context

---

### Step 4 — Narrative grouping for discussions
- Group by `(linked_entity_type, linked_entity_id)`
- Show:
  - entity title
  - count
  - most recent discussion
  - last 1–3 agenda items

**Acceptance**
- Reads like a story, not a feed

---

### Step 5 — Role‑aware time window
- Implement existing TODO
- Managers see longer continuity by default

---

### Optional polish
- Subtle expand/collapse animation for groups

---

# Phase 3 — Me (P1)

## Intent
Me is the **personal workspace**: everything I own.

## Current Drift
- Based on org‑wide snapshot + filtering
- Refresh tied to profile events
- Sticky state can mask staleness

## Incremental Work Plan

### Step 1 — Formalize ownership rules
- Tasks: owner OR creator
- Goals: owner
- Meetings: explicitly define attendee vs creator rule

---

### Step 2 — Surface activation refresh
- Add `OnSurfaceActivated()`
- Apply non‑blocking refresh logic

---

### Step 3 — Dirty‑driven correctness
- Edits elsewhere mark Me dirty

---

### Optional polish
- Gentle fade when switching Me tabs

---

# Phase 4 — Circle (P1)

## Intent
Circle is **visibility context**, not a management console.

## Current Drift
- RPC correctness issues surface here
- 5‑minute cache hides changes
- Client repairs meeting attendees

## Incremental Work Plan

### Step 1 — Hierarchy diagnostics
- Validate RPC visibility results
- Log depth/relation counts internally

---

### Step 2 — Refresh on activation
- Apply surface refresh semantics

---

### Step 3 — Reduce silent data repair
- Fetch authoritative attendees on drill‑in
- Avoid global mutation

---

### Optional polish
- Minimal transition between calendar views

---

# Phase 5 — Browse Pages (P2)

## Intent
Browse pages are **destinations** and source of truth.

## Current Drift
- Always alive
- Refresh depends on attach timing

## Incremental Work Plan

### Step 1 — Destination activation hook
- Add `OnSurfaceActivated()` to each browse VM

---

### Step 2 — Dirty triggers
- Edits in flyouts mark parent browse dirty

---

### Optional polish
- Subtle list update fade

---

# Phase 6 — Projects (P2)

## Intent
Projects organize work and signals efficiently.

## Current Drift
- Batch signal capability exists but unused

## Incremental Work Plan

### Step 1 — Wire batch signal counts
- Use existing batch RPC
- Show minimal count badges only

---

### Step 2 — Drill‑in consistency
- Dirty triggers ensure freshness

---

### Optional polish
- Small transition on project detail open

---

# Phase 7 — Cross‑Cutting Polish (Allowed)

## Allowed
- 200–300ms fades
- Header chip state transitions
- List insert/remove settles
- Pulse group expand/collapse

## Not Allowed
- Layout changes
- Theme changes
- Full‑screen loading unless first run

---

## Definition of Done
- Briefing feels current and focused
- Pulse explains context and continuity
- Me and Circle are trustworthy
- Browse pages act like destinations
- Projects show efficient signals
- Styling remains unchanged
