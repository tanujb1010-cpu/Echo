# Echo — Performance, Testing & Release

# PART I — Performance Optimization Plan

## 1. Targets
| Platform | Resolution | FPS target | Frame budget |
|----------|-----------|-----------|--------------|
| PC (min spec) | 1080p | 60 | 16.6 ms |
| PC (recommended) | 1440p/4K | 120+ | ≤8.3 ms |
| Mid-range Android | 1080p | 60 (30 floor) | 16.6 / 33 ms |
| iOS (A13+) | native | 60 | 16.6 ms |
| Steam Deck | 800p | 60 | 16.6 ms, ≤8 W ideal |

## 2. Frame Budget (60 Hz, mobile-conservative)
| Stage | Budget |
|-------|--------|
| Fixed sim (kinematics, gates, events) | ≤ 2.0 ms |
| Recording/keyframe (amortized) | ≤ 0.3 ms |
| Echo expression (LOD'd) | ≤ 1.0 ms |
| Rendering (instanced Echoes, 2D lights) | ≤ 8.0 ms |
| UI/audio/misc | ≤ 2.0 ms |
| Headroom | ≥ 3.3 ms |

## 3. Key Optimizations
- **Zero-GC hot loop:** struct events, pooled everything, no LINQ/closures in sim; pre-sized buffers; `Span`/`NativeArray` where hot.
- **Object pooling:** Echoes, props, VFX, audio one-shots, UI toasts. Prewarm from level manifest; assert zero runtime instantiate in profiler.
- **GPU instancing:** one Echo material, `MaterialPropertyBlock` per-instance (glow/tint/alpha). SRP Batcher for environment.
- **Draw-call cap:** ≤150 mobile / ≤500 PC; sprite atlasing; static batching.
- **Sim/render decoupling:** sim 60 Hz, render interpolated; mobile can render 30 while sim stays 60 (replays stay exact).
- **Expression LOD:** off-screen/distant Echoes skip Layer B entirely; barks/anim culled by frustum + distance.
- **Streaming:** Addressables stream worlds; unload aggressively; texture mips + ASTC on mobile.
- **Save I/O & LZ4 compression** off the main thread (jobs/async); never stall a frame.
- **Memory:** texture budget per platform; Echo timeline data is tiny (KB); cap concurrent loaded levels to 1 + neighbors.
- **Thermal (mobile/Deck):** dynamic resolution + optional 30 Hz cap to hold thermals; profile sustained, not burst.

## 4. Profiling Cadence
- Unity Profiler + Frame Debugger weekly on min-spec + a real mid-range phone (not just editor).
- Automated perf PlayMode tests assert frame-time budgets on a fixed scene in CI (fail the build on regression).
- Memory snapshot diff each milestone (leak guard, esp. pool release on level unload).

---

# PART II — Testing Plan

## 5. Test Pyramid
- **EditMode unit (fast, deterministic):**
  - `Fix64` math identities; `DeterministicRng` reproducibility.
  - **Replay equivalence:** a recorded timeline replays to a bit-identical `StateHash` (the crown-jewel test).
  - Gate predicates: given state X → fires/doesn't (every Gate).
  - Save codec round-trip + schema migration (old save → loads).
- **PlayMode integration:** full level solve via scripted inputs; pooling zero-alloc assertions; UI flows.
- **Soak/regression:** replay a fixed save **1000×**, assert identical outcomes → determinism regression guard. Run nightly in CI.
- **Perf tests:** frame-budget assertions on reference scenes.
- **Property/fuzz:** random valid input sequences → record→replay must match (finds determinism edge cases).
- **Manual/exploratory:** designer "break my puzzle" sessions; emergent-solution log.
- **Accessibility QA:** colorblind sim, screen-reader pass, single-button playthrough, no-audio playthrough (captions sufficient?), reduce-motion playthrough.
- **Device matrix:** low/mid/high Android, A13/A15 iPhone & iPad, Steam Deck, min/rec PC, ultrawide, 144 Hz.
- **Localization:** pseudo-loc (string expansion, RTL) before real translation.
- **Beta:** closed beta → analytics funnel + difficulty re-tune + crash burndown.

## 6. Bug Severity & Gates
Sev-1 (determinism desync, save corruption, crash, soft-lock) block release. Sev-2 (puzzle unintended-unsolvable, perf below floor) block the affected world. Tracked in issue tracker; release gate = zero open Sev-1.

---

# PART III — Release Checklists

## 7. Steam Release Checklist
- [ ] Steamworks app created, depots configured (Win/Mac/Linux + Deck verified).
- [ ] Store page: capsule art (all sizes), 5+ screenshots, trailer, short+long description, tags, system reqs.
- [ ] Steam Cloud auto-cloud config (save paths) + tested cross-device.
- [ ] Achievements + stats defined and firing; Rich Presence (optional).
- [ ] Steam Input config (gamepad glyphs, Deck layout); **Deck Verified** submission.
- [ ] Build branches: default + beta; depot upload via SteamPipe; smoke-test the *store build* (not editor).
- [ ] Age rating / content survey; regional pricing; release date + wishlist campaign live ≥1 month prior.
- [ ] EULA, privacy policy (analytics disclosure), credits, third-party licenses.
- [ ] Day-1 patch staged; rollback plan; review-key distribution to press/creators.

## 8. Google Play Release Checklist
- [ ] Play Console app, package name, **AAB** signed (Play App Signing).
- [ ] Target latest required API level; 64-bit; tested on min API.
- [ ] Store listing: icon, feature graphic, phone+tablet screenshots, short/full description, trailer.
- [ ] **Data safety form** (analytics: what's collected, opt-in), privacy policy URL.
- [ ] Content rating (IARC) questionnaire; ads/IAP declarations (none if premium).
- [ ] Saved Games (Play Games Services) + achievements wired and tested.
- [ ] Pre-launch report clean (crashes/a11y warnings); closed→open testing tracks used.
- [ ] Pricing/countries; tax/payment profile; refund policy noted.
- [ ] Touch controls, one-handed mode, layout editor verified on phones+tablets; back-button handling.

## 9. Apple App Store Release Checklist
- [ ] App Store Connect record, bundle ID, signing/provisioning, TestFlight beta passed.
- [ ] iOS/iPadOS builds; notch/Dynamic-Island safe areas; ProMotion 120 Hz handled.
- [ ] Screenshots for required device sizes; preview video; description, keywords, subtitle.
- [ ] **App Privacy "nutrition label"** (analytics opt-in), privacy policy URL, ATT prompt only if needed (prefer not to track).
- [ ] Game Center: achievements + iCloud saved games tested cross-device.
- [ ] Age rating; export-compliance (encryption) answers; accessibility (VoiceOver menu labels, Dynamic Type).
- [ ] Review notes + demo guidance (explain the ghost mechanic to reviewers); no private APIs; IDFA stance documented.
- [ ] Phased release enabled; rollback build ready.

## 10. Cross-platform pre-ship gate (all stores)
- [ ] Zero Sev-1 bugs · determinism soak green on all targets · save/cloud cross-device verified · full a11y pass · localization complete · legal docs in place · trailer + assets final · analytics opt-in compliant.
