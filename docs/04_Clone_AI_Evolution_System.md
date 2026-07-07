# Echo — Clone Evolution AI System
*How a perfect recording becomes a person without breaking the puzzle.*

## 0. The Central Problem
A puzzle game built on replays demands **reproducibility**: if an Echo's behavior changed unpredictably between attempts, no puzzle could be authored or solved. Yet the brief demands Echoes that get *curious, defiant, and treacherous.* These look contradictory. They are reconciled by one rule:

> **Evolution is deterministic. Divergence is authored, telegraphed, and counterable.**

An Echo is never "randomly" disobedient in a way the player can't predict, prepare for, or repair. Its personality is a **pure function** of state the player can observe and influence. That is what makes it *believable* (consistent, motivated) instead of *frustrating* (noise).

---

## 1. Two-Layer Behavior Model

Every Echo runs on two strictly separated layers:

### Layer A — Authoritative Replay (physics-affecting)
The immutable input timeline. By default it executes verbatim, tick by tick. **Nothing in Layer B may alter Layer A except through a *Divergence Gate* (§4).** This guarantees that an Echo with no triggered gates is a *perfect* replay — preserving solvability.

### Layer B — Expressive Layer (never physics-affecting)
Head turns, eye-lines, idle fidgets, posture, glow intensity, barks, hesitputation animations, faction-color shifts. This layer is free to be as alive as we like because it **cannot touch the simulation**. It is where 90% of "personality" is *felt*. It is also the **telegraph** for Layer A divergences ("the Echo glances at the lever it's about to refuse").

> Design payoff: the world feels alive constantly (Layer B), but the *rules* only ever change at clearly-signaled moments (Layer A gates).

---

## 2. Salience — the Evolution Clock
Each Echo carries a scalar **Salience S ∈ [0,1]** = "how strongly the foundry remembers it." Salience is the single driver of evolution.

`S` increases with:
- **Age** (ticks the Echo has existed across all replays of its timeline).
- **Co-presence** (ticks spent near other Echoes of the same source — "resonance").
- **Reuse** (how many times the player has re-banked/relied on this run).
- **Pivotal acts** (its recorded action was load-bearing in a solution).

`S` decreases with:
- **Decay** if unused for long stretches.
- **Pruning siblings** near it (trauma — feeds Spite, see drives).

Salience is **persisted in the save** per banked timeline, so an Echo that mattered to you in World 2 is *more evolved* when it reappears in a New Game+/recurring-ally context.

---

## 3. Drives & Traits (the personality substrate)

### 3.1 Five Drives (a vector per Echo)
| Drive | Grows from | Expresses as |
|------|-----------|--------------|
| **Curiosity** | novelty exposure, idle slack near new objects | improvisation, wandering eye-lines, exploring |
| **Autonomy** | high salience, repeated identical orders | refusal, preference, self-authorship |
| **Attachment** | proximity & reliance on Prime, gentle treatment | bonding, initiative-help, teaching |
| **Self-preservation** | exposure to hazards, near-deletions | hesitation before danger, body-shield reluctance |
| **Spite** | being sacrificed, pruned siblings, broken promises | sabotage, grudge, faction-defiance |

Drives evolve via a simple deterministic update each "evaluation epoch" (e.g., every 30 ticks of slack), gated by `S` (low-salience Echoes barely move).

### 3.2 Traits (emergent labels)
When a drive crosses a threshold *and* `S` is high enough, the Echo earns a **Trait** (persisted, named): `Devoted, Curious, Stubborn, Trickster, Mournful, Brave, Skittish`. Traits are mutually weighted, and one becomes **dominant** → drives barks, color, and which Divergence Gates can fire. Traits map directly to the narrative "named Echoes" (e.g., **First** is forced-`Curious`, the Saboteur Prime is `Trickster`).

---

## 4. Divergence Gates (the only way Layer B touches Layer A)
A **Gate** is a designer- or system-placed point where an Echo *may* deviate from its recording. A Gate fires iff a **pure, deterministic predicate** is true:

```
fire = Predicate(trait, drives, S, worldStateHash, seededRng(runId, tick, gateId))
```

Because every input is observable/derivable, the result is **reproducible** across restarts and **previewable** by the Assist "intent" overlay. Gate categories:

| Gate | Effect on Layer A | Telegraph (Layer B) | Counterplay |
|------|-------------------|---------------------|-------------|
| **Improvise** | inserts a *helpful* micro-action into a slack window | leans toward target first | none needed (it helps) |
| **Hesitate** | delays a hazardous recorded action by d ticks | flinch + look | re-walk path near it to reassure (raises Attachment) |
| **Refuse** | skips one recorded action | shakes head, icon | do that action yourself this run, or raise trust |
| **Sabotage** | performs a *near-miss* wrong action (telegraphed, fair) | smirk, off-path glance | leave it a "trap"/favor (Self-Negotiation, mech #50), or out-route it |
| **Self-Author** | permanently edits a few ticks of its *own* timeline toward a "better line" | shimmer on the path | usually beneficial; can be locked via Loop-Lock |

**Hard guarantees:**
1. A Gate only ever modifies **bounded, local** behavior (≤ a few ticks, one action) — never a wholesale rewrite.
2. Every Gate's outcome is **deterministic** given the save → the level is always solvable as authored, because authoring *accounts for* which Gates can fire at that salience.
3. Every Gate is **telegraphed ≥1 second** ahead in Layer B.
4. The player has a **repair path** (trust, re-walk, prune, or out-plan) for every adversarial Gate.

> This is the believability engine: the Echo seems to *decide*, but the decision is a consistent, motivated, observable function — exactly how we read other people as having stable personalities.

---

## 5. Player-Facing Feedback Loops
Two soft meters (mostly invisible, surfaced via behavior, optionally shown in Assist):
- **Trust** — raised by reliance, reassurance, not sacrificing; lowers Refusal/Sabotage odds, raises Improvise/Bonding.
- **Mercy** — raised by avoiding needless pruning/sacrifice; suppresses Spite globally.

These also feed the **10 endings** (GDD/Lore). The player is thus *managing a relationship*, which is the game's emotional core.

---

## 6. Architecture (engine mapping)
```
EchoBrain (per Echo, MonoBehaviour-light, pooled)
 ├─ ReplaySource        : authoritative input stream (Layer A)
 ├─ SalienceTracker     : updates S each tick from world signals
 ├─ DriveModel          : 5-float vector + deterministic epoch update
 ├─ TraitResolver       : drives+S → dominant Trait (+ persisted name)
 ├─ GateEvaluator       : runs Predicates at tagged ticks → Layer A deltas
 ├─ ExpressionController: Layer B (anim/bark/color) — read-only re: sim
 └─ IntentBroadcaster   : feeds Assist overlay + accessibility pings
```
- **Determinism:** all randomness via a single seeded `DeterministicRng` keyed by `(saveSeed, runId, tick, gateId)`. No `UnityEngine.Random`, no wall-clock, no per-frame floats in gate logic (fixed-point or quantized).
- **Performance:** `DriveModel`/`TraitResolver` update on a **slow tick** (every N fixed steps, staggered across Echoes via index%N). `GateEvaluator` only runs at *pre-tagged* ticks (authoring marks candidate gates), so it's near-free at runtime. Expression is LOD'd: off-screen Echoes skip Layer B entirely.
- **Testing:** because behavior is a pure function, every Gate has a deterministic unit test (given state X → fires/doesn't). A "soak" test replays a fixed save 1000× and asserts identical outcomes (regression guard for determinism).

---

## 7. Tuning Knobs (designer-exposed ScriptableObjects)
- Salience gain/decay curves per source.
- Drive growth rates & thresholds per world (World 1 = frozen at Obedient; ramps to World 6).
- Per-Gate enable masks per level (a tutorial level can *forbid* Refusal even at high S).
- Trait→bark tables (localized, captioned).
- Assist overrides: a player on full Assist can pin Echoes to Obedient (accessibility/cognitive load), with a note that it changes some emergent narrative (never blocks endings reachable via choices).

## 8. Why this is "believable"
People feel real when they are **consistent, motivated, and responsive to how we treat them.** Our Echoes are exactly that: consistent (deterministic), motivated (drives), responsive (Trust/Mercy). The illusion of free will comes from the player's *inability to hold the whole state in their head* — not from actual randomness. That's both honest game design and a thematic statement the story leans into (you, too, are a deterministic echo who feels free).
