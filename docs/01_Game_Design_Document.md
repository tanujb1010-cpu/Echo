# Echo — Game Design Document (GDD)
*Version 0.1 — living document*

## 1. Overview
- **Genre:** Single-player 2.5D puzzle-platformer with self-cooperative ("temporal co-op") mechanics.
- **Camera:** Side-on, parallax 2.5D. Orthographic-feel with subtle perspective depth.
- **Sessions:** 5–20 minute puzzle sittings; full game ~8–12 h main path, ~20 h completionist.
- **Tone:** Melancholy-wonder. Lonely but warm; the clones make you feel accompanied.

## 2. Core Fantasy
*You are never alone in a room you've been in before.* You leave pieces of yourself behind, and they leave the room changed.

## 3. Core Mechanic — Recording & Echoes
1. The player controls **Echo-Prime** (the live character).
2. Every input is recorded each fixed tick (see TDD §Replay Format).
3. On **restart**, all prior runs spawn simultaneously as **Echoes**, each replaying its own timeline from tick 0.
4. Echoes are solid, interactable, and affect the world (stand on plates, carry blocks, block lasers, give boosts).
5. The player solves a puzzle by **choreographing across runs**: do task A this run knowing a future you must do task B while past-you holds a door.

### 3.1 What gets recorded
Position-deterministic **inputs**, plus interaction events and a periodic **keyframe** snapshot for resync. Not raw transforms (see TDD for why this matters).

### 3.2 Echo limits
- Soft cap of concurrent Echoes per level (default 6, tunable per level) keeps cognitive load + perf sane.
- "**Prune**" UI lets the player delete a banked run before restarting (undo a bad recording).
- Some levels *require* a minimum number of Echoes; some *cap* it to force efficiency.

## 4. Gameplay Loop

```
        ┌─────────────────────────────────────────────┐
        │  OBSERVE puzzle → PLAN a division of labor    │
        │  across N runs → PERFORM run k (recorded)     │
        │  → RESTART → past runs replay as Echoes        │
        │  → integrate live-you with the braid          │
        │  → SOLVE → bank insight → next room           │
        └─────────────────────────────────────────────┘
```

- **Micro-loop (seconds):** move, jump, grab, press — all recorded.
- **Meso-loop (a level):** layer 2–6 runs into a working "machine of selves."
- **Macro-loop (a world):** new mechanic introduced → taught → twisted → combined → boss → narrative beat.

## 5. Difficulty Curve
We use a **teach → test → twist → combine → subvert** cadence per mechanic, and a global ramp across 6 worlds.

| World | Echoes expected | New axis | Clone AI state | Feel |
|------|------|---------|---------|------|
| 1 — Atrium | 1–2 | Recording basics, plates, doors | Fully obedient | "Oh, neat." |
| 2 — Workshop | 2–3 | Object manipulation, carrying, stacking | Obedient + idle curiosity flavor | "I'm a team." |
| 3 — Clockworks | 3–4 | Time manipulation (slow/echo-delay) | First curious Echoes (cosmetic divergence) | "Wait, did it…?" |
| 4 — The Fault | 4–5 | Hazards, irreversibility, sacrifice runs | Some Echoes hesitate/refuse | "I can't fully trust them." |
| 5 — The Choir | 5–6 | Mass coordination, swarm logic | Personalities, factions | "They have opinions." |
| 6 — The Origin | variable | Everything + sabotage | Saboteurs + allies; you negotiate | "Who am I to them?" |

**Difficulty is layered, not gated:** every hard puzzle has an *intended* solution and usually 1+ emergent ones. We measure difficulty by **runs-to-solve** and **plan depth** (how many runs ahead you must think), tracked via analytics.

A separate **Assist** axis (independent of difficulty) can: slow time globally, show Echo ghost-trails/intent, extend coyote time, and reduce required precision. Assist never blocks achievements.

## 6. Puzzle Progression Philosophy
- **One new idea at a time.** A level either *introduces* exactly one mechanic or *combines* known ones — never both.
- **Telegraph the solution space, hide the solution.** The room shows you what's possible; you find the order.
- **Failure is cheap, restart is the verb.** Restarting is not punishment — it's the primary tool.
- **Every mechanic has a "click" moment** authored to produce an audible/visual payoff.

(Full list: 50 mechanics + 50 levels in `03_Content_Bible.md`.)

## 7. Controls

### 7.1 PC (keyboard + mouse / gamepad)
| Action | Keyboard | Gamepad |
|--------|----------|---------|
| Move | A/D or ←/→ | Left stick |
| Jump | Space | A / Cross |
| Grab/Release | E / LMB | X / Square |
| Interact (press, throw) | F / RMB | B / Circle |
| Restart run (bank current) | R (hold to confirm) | Y hold |
| Rewind/scrub run preview | Q (hold) | LB hold |
| Prune Echo menu | Tab | View / Select |
| Timeline scrubber | T | RB |
| Pause/menu | Esc | Start |
| Quick-undo (last discrete action) | Z / Ctrl+Z | LB tap |

- Full **rebinding**, both schemes simultaneously active, no exclusive lock.
- Mouse for menus/timeline; not required for play.

### 7.2 Mobile (touch)
- **Left thumb:** virtual stick *or* tap-zones (toggle in options) for move/jump.
- **Right thumb:** context **action button** (grab/interact merges with proximity), plus a **jump** button.
- **Restart:** dedicated corner button with hold-to-confirm + haptic.
- **Timeline scrubber:** drag a bottom-edge ribbon; pinch to zoom the timeline.
- **One-handed mode:** all critical actions reachable in a thumb arc; auto-grab nearest interactable.
- **Layout editor:** drag/resize/opacity for every on-screen control. Safe-area aware (notches).
- Haptics on record-start, record-stop, Echo-spawn, and "Echo defied you."

### 7.3 Shared input rules
- Input is captured through a single **InputCommand** abstraction (see Architecture), so PC, gamepad, and touch all produce identical recordable commands — the recorder never knows or cares which device was used.

## 8. Save System
- **Autosave** on: level complete, banked run, world transition, and every 30 s of activity (debounced).
- **Save model:** profile → world → level → best solution + all banked timelines (compressed).
- **Slots:** 3 local profiles. **Cloud save** via Steam Cloud / Google Play Saved Games / iCloud (see TDD).
- **Conflict resolution:** last-write-wins with a manual "keep which?" prompt on detected divergence.
- **Integrity:** checksummed; corrupt save falls back to last good autosave; never hard-blocks the player.
- **Undo system** (in-level): a bounded command stack lets you undo discrete recorded actions before banking; restart re-derives state deterministically.

## 9. Hint System (non-punishing, opt-in)
Three escalating tiers, per puzzle, always optional, never penalized:
1. **Nudge** — restates the goal + which Echoes are involved.
2. **Frame** — highlights the key interactable / the first run's job.
3. **Choreography** — a ghost preview of one valid run sequence (not the full solution).
- Hints are gated behind a short cooldown (anti-spoiler), surfaced automatically if analytics detect a stuck player (>N failed runs) — surfaced, never forced.

## 10. Accessibility (first-class, not a checkbox)
- **Motor:** full remap, hold→toggle for every hold, adjustable input buffering, coyote time, auto-grab, no-fail Assist, slow-time, single-button play mode on mobile.
- **Visual:** colorblind palettes (Echoes distinguished by shape/pattern + label, never color alone), scalable UI/text, high-contrast mode, reduce-flash, reduce-parallax/motion, screen-reader labels for menus, Echo audio-pings for the low-vision.
- **Hearing:** full subtitles + speaker tags, captioned SFX for puzzle-critical audio (e.g., "[plate clicks]"), visual substitutes for every audio cue.
- **Cognitive:** Assist mode, intent indicators on Echoes, optional objective tracker, hint system, no time pressure on standard mode, "explain this mechanic" replay.
- **Vestibular:** disable screen shake, camera smoothing, parallax/zoom.
- Targets **conformance with common a11y guidelines** and console cert a11y where applicable.

## 11. Economy of Mechanics (anti-bloat rule)
With 50 mechanics planned, we enforce: a mechanic must either (a) recombine with ≥3 others, or (b) anchor a memorable one-off level/boss. Anything that does neither gets cut. Tracked in the Content Bible's "combos" column.

## 12. Win/Loss & Fail States
- No "game over." Death of Echo-Prime = restart the *current run* (timeline discarded), not the level.
- Hazards can destroy an Echo mid-replay; the puzzle must account for that (intended in hazard worlds).
- The only true loss is narrative (some endings are "bad" but valid — see `02_…`).
