# ECHO

> *Every step you take is remembered. Every self you leave behind has a will of its own.*

**Echo** is a 2.5D puzzle-platformer about collaborating — and eventually negotiating — with recordings of your past selves. Every action you perform is permanently recorded. When you restart a level, your previous runs replay as **ghost clones** ("Echoes"). Early Echoes obey perfectly. Later ones get *curious, stubborn, and occasionally treacherous.* You don't just solve puzzles with your past — you have to manage it.

---

## The One-Line Pitch
*"Braid meets a co-op game where the only other player is everyone you used to be."*

## Design Pillars
1. **Your past is a resource.** Each run is a tool you bank for future runs.
2. **The clones are characters, not conveyor belts.** Their AI evolves; trust becomes a mechanic.
3. **Readable determinism.** The player must always be able to predict a clone — until the game deliberately, fairly breaks that promise.
4. **Respect the player's time and body.** Deep accessibility, generous undo, no busywork.

---

## Repository Map

| Path | What's inside |
|------|---------------|
| [`docs/01_Game_Design_Document.md`](docs/01_Game_Design_Document.md) | Core loop, mechanics, difficulty curve, controls, accessibility, save system |
| [`docs/02_Lore_Story_Characters_World.md`](docs/02_Lore_Story_Characters_World.md) | Story, lore, characters, world, 10 alternate endings |
| [`docs/03_Content_Bible.md`](docs/03_Content_Bible.md) | 50 puzzle mechanics · 50 levels · 20 bosses · 20 hazards · 15 time mechanics · 15 clone-evolution mechanics · 10 secrets |
| [`docs/04_Clone_AI_Evolution_System.md`](docs/04_Clone_AI_Evolution_System.md) | The believable clone-evolution AI: drives, traits, defiance, sabotage |
| [`docs/05_Technical_Design_Document.md`](docs/05_Technical_Design_Document.md) | Determinism, replay format, physics, rendering, netcode-free sync |
| [`docs/06_Architecture_and_Folder_Structure.md`](docs/06_Architecture_and_Folder_Structure.md) | Module map, component architecture, object pooling, folder layout |
| [`docs/07_Development_Roadmap.md`](docs/07_Development_Roadmap.md) | Milestones from prototype → vertical slice → launch |
| [`docs/08_UI_UX_Mockups.md`](docs/08_UI_UX_Mockups.md) | Wireframes (ASCII), HUD, timeline scrubber, accessibility menu |
| [`docs/09_Performance_Testing_Release.md`](docs/09_Performance_Testing_Release.md) | Perf plan, test plan, Steam/Google Play/App Store checklists |
| [`docs/10_Marketing_Monetization_Store.md`](docs/10_Marketing_Monetization_Store.md) | Marketing, monetization, trailer concept, store page copy, assets |
| [`Assets/Scripts/`](Assets/Scripts/) | Unity C# implementation (core slice built first) |

## Build Status
- [x] Full design package
- [x] Core architecture + folder structure
- [x] **Phase 1 — deterministic core** (Fix64 sim, recorder/replayer, clone-evolution AI, pooling)
- [x] **Phase 2 — playable core loop** (game feel, Held-Plate puzzle #1, Unity presentation/driver, content pipeline)
- [x] **Phase 3+ (parallel pass)** — **48 of 50 Content Bible mechanics** (only #27 Blind Record remains — a camera/fog-of-war presentation feature with no sim-layer analog, see `docs/STATUS_ALL_PHASES.md`); time mechanics **Echo-Delay, Reverse-Replay, Slow-Field, Fast-Field, Conductor/Phase-Shift, Pause-Self**; **Pre-Echo** assist preview; **Trust/Grievance** persisted per-Echo and live-wired to reliance/sacrifice; run-cloning; **all 10 endings** (deterministic resolver + live `EventBus` wiring); **save/serialization** (RLE+Deflate+CRC, autosave); undo/prune; hint system; achievements/analytics; mobile touch + prune UI — all authorable via `LevelDefinition`
- [x] **All 6 worlds authored** — 44 levels (`Assets/Scripts/Unity/SampleLevels.cs`), each geometry ported from a passing headless test; W1–W2 redesigned around resource-scarcity constraints + false-obvious solutions, W3–W6 hardened with a progressive difficulty pass
- [x] **Unity 6000.0.78f1 installed and verified** — whole codebase (48 mechanics, ~40 gameplay modules) compiles with 0 errors against real `UnityEngine`; `.asmdef` boundaries enforce the Core/Gameplay/Unity dependency direction at compile time; an in-editor `Echo.Tests.EditMode` NUnit suite passes independently of the headless harness
- [x] **Full game loop** — menus, save/progression (schema-versioned, backward-compatible), 6 world intros, 10 narrative endings, zero-asset synthesized audio (SFX + generative music), gamepad support, per-world visual identity, onboarding (diegetic hints + mechanic-primer glossary), and an automated cheese-audit bot (6 echo-free bots × all 44 levels, catches solve-without-echoes exploits before a human ever sees them)
- [ ] Human playtesting of Worlds 3–6 (only W1–2 have been played beyond the automated harness) · ports · cert · marketing — *see [`docs/STATUS_ALL_PHASES.md`](docs/STATUS_ALL_PHASES.md)*

> **Verified, not just asserted: 376/376 headless tests pass**, plus a 0-issue cheese-audit sweep across all 44 levels. The entire `Core`/`Infra`/`Services`/`Gameplay` layer is pure C# (no UnityEngine), compiled with .NET 8 and run for real. Record→replay is bit-identical across 90-second runs; an 8-run braid reproduces identically across sessions; forty-eight mechanics are solvable by collaborating with a past self; all 10 endings resolve deterministically; a saved timeline replays bit-identically after encode→decode. Reproduce with `dotnet run --project echoharness`. Full status: [`docs/STATUS_ALL_PHASES.md`](docs/STATUS_ALL_PHASES.md).
>
> **This is a free playtest build**, not a finished commercial release — Worlds 3–6 have only been validated by automated testing, never by a human beyond the developer. Feedback (confusing levels, bugs, anything that felt off) is exactly what this release is for.

## Glossary
- **Run** — one attempt at a level, from spawn to exit/restart.
- **Echo / Clone** — a deterministic replay of a previous run, now an in-world agent.
- **Timeline** — the recorded data for one run (inputs + checkpoints).
- **Braid** — the stack of all Echoes co-existing in a single level attempt.
- **Tick** — one fixed simulation step (the atomic unit of recording).
