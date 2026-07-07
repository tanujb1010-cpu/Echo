# Echo — UI/UX & Mockups
*Wireframes are ASCII (engine-agnostic, reviewable in text). Built in Unity UI Toolkit, fully themeable, accessibility-first.*

## 1. UX Principles
- **The braid is always legible.** You can tell at a glance how many Echoes exist, which run you're recording, and what each Echo is about to do.
- **The verb is "restart."** Restart UI is the most prominent, most reassuring control (hold-to-confirm, never accidental).
- **Diegetic where possible.** Salience = glow; faction = tint; intent = a small ghost-arc. Less HUD, more world.
- **Everything optional is toggleable.** Power users hide the HUD; new players get more scaffolding.

## 2. In-Game HUD (PC)
```
┌───────────────────────────────────────────────────────────────────────┐
│ ECHOES ◍◍◍○○○  (3/6)        WORLD 3 · "Out of Phase"        ⟳ R to restart│
│                                                                         │
│                                                                         │
│                         [ gameplay viewport ]                           │
│                                                                         │
│        ☼Prime          ◍Echo-1(Devoted)     ◍Echo-2(Curious↗)           │
│                                          └ intent arc ↗ (about to jump)  │
│                                                                         │
│ ┌─ Timeline ───────────────────────────────────────────────[T] expand ┐│
│ │ ▮▮▮▮▮▮▮▮▮▮▮▮▮▮│▮▮▮▮▮▮▮  tick 612/1080   ▶ live   ⟲ scrub  ✎ prune     ││
│ └───────────────────────────────────────────────────────────────────────┘│
│ [E] grab   [F] interact   [Z] undo   [Tab] prune   [?] hint              │
└───────────────────────────────────────────────────────────────────────┘
```
- **Echo dots** fill/empty as runs are banked/pruned; each dot tinted by trait.
- **Intent arc**: a faint predicted-path on Echoes about to do something notable (Assist on by default for first 2 worlds).
- HUD elements individually toggleable in settings.

## 3. Timeline Scrubber (expanded, [T])
```
┌─ TIMELINE — drag to preview · click a run to prune/keep ─────────────────┐
│ Run 1 (Devoted)   ▮▮▮▮▮▮▮▮▮▮▮▮▮▮▮▮▮▮▮▮▮▮▮▮  grab@120 throw@300            │
│ Run 2 (Curious)   ▮▮▮▮▮▮▮▮▮▮░░░░▮▮▮▮▮▮▮▮▮▮  ⚠ may improvise @410          │
│ Run 3 (live)      ▮▮▮▮▮▮▮▮▮▮▮▮▮▮▮▮▮●────────  ◀ you are here (612)         │
│                   └ keyframes ▲   ▲   ▲   ▲                               │
│  [◀◀]  [◀ step]  [▮▮ pause]  [step ▶]  [▶▶]     speed ×0.5 ×1 ×2          │
└──────────────────────────────────────────────────────────────────────────┘
```
- Event markers (grab/throw/press) on each run; ⚠ marks a *possible* Divergence Gate (telegraph, deterministic).
- Scrubbing shows a translucent world preview without committing.

## 4. Prune Menu ([Tab])
```
┌─ YOUR ECHOES ──────────────────────────────────────────────┐
│ ▣ Run 1  "First"   Devoted   salience ███████░  ★ load-bearing│
│ ▣ Run 2            Curious   salience ████░░░░               │
│ ▢ Run 3            (pruned)                                  │
│                                                             │
│  Keep ☑  Prune ☐    [Restart with kept runs ⟳]              │
│  ⚠ Pruning high-salience Echoes lowers Mercy.               │
└─────────────────────────────────────────────────────────────┘
```

## 5. Hint UI (opt-in, three taps)
```
[?] →  Tier 1 NUDGE:  "Two plates must be held at once. You have 3 runs."
[?] →  Tier 2 FRAME:  highlights the far plate + Run-1's idle window.
[?] →  Tier 3 CHOREO: plays a translucent partial run. (cooldown 30s)
```

## 6. Mobile HUD (touch)
```
┌───────────────────────────────────────────────┐
│ ◍◍◍○○○                         ⟳(hold) ☰        │
│                                                │
│            [ gameplay viewport ]               │
│                                                │
│   ╭────╮                            ╭────╮     │
│   │ ▲  │  move pad / tap-zones      │GRAB│     │
│   ╰────╯                            ╰────╯     │
│   one-handed? swap → all in thumb arc  ╭────╮  │
│ ───── timeline ribbon (drag) ─────────  │JUMP│  │
│                                         ╰────╯  │
└───────────────────────────────────────────────┘
```
- Layout editor: long-press any control → drag/resize/opacity. Safe-area aware.
- Restart is a corner hold-to-confirm with haptic ramp (prevents fat-finger restarts).

## 7. Menus
- **Main:** Continue / Worlds / Settings / Recursion(NG+, locked) / Quit. Background = a live braid solving itself.
- **Settings tabs:** Controls (rebind, both schemes) · Accessibility · Graphics · Audio · Gameplay (assist, hint auto-surface, intent overlay) · Data (analytics opt-in, cloud save) · Language.

## 8. Accessibility Menu (first-class, its own top-level tab)
```
┌─ ACCESSIBILITY ───────────────────────────────────────────┐
│ MOTOR     Hold→Toggle [all]  Input buffer [▮▮▮░]  Coyote+   │
│           Auto-grab ☑   Single-button mode ☑ (mobile)       │
│ VISUAL    Colorblind [Deuter ▾]  Echo shapes ☑  Text size + │
│           High contrast ☑  Reduce flashing ☑  Reduce motion │
│ HEARING   Subtitles ☑  Speaker tags ☑  Captioned SFX ☑      │
│ COGNITIVE Assist mode ☑  Intent overlay ☑  Pin Echoes Obedi.│
│           Objective tracker ☑  Hint auto-surface ☑          │
│ VESTIB.   Screen shake ☐  Camera smooth ▮▮░  Parallax ☐     │
│  [Restore defaults]   [Preview]                             │
└────────────────────────────────────────────────────────────┘
```
- "**Pin Echoes Obedient**" disables adversarial Gates for players who need lower cognitive load — never blocks choice-based endings.

## 9. Visual/Art Direction Notes
- **Palette:** cool, desaturated stone & glass up top → warmer, glowing depths below. Echoes are the brightest things on screen (your past literally lights the way).
- **Echo silhouette** distinct from Prime (Prime = solid + warm rim light; Echoes = translucent + cool glow scaled by salience; trait adds an accent shape/tint, never color-only).
- **Readable motion language**: every Echo telegraph uses the same 3-frame "wind-up" so players learn to read intent.
- **Type:** large, high-contrast, dyslexia-friendly font option.
