# ThreeDiabetes Project Guidance

## Project Identity

- Engine: Unity `6000.0.23f1c1`.
- Target: offline, single-player, portrait mobile puzzle game.
- Gameplay layout reference resolution: `750 x 1334`.
- Background reference resolution: `750 x 1624`. Screen backgrounds use centered
  aspect-preserving cover sizing against the actual Canvas. Full-screen overlay
  roots stretch to the Canvas; decorative bands stretch horizontally. Keep these
  separate from the fixed centered gameplay/control layout.
- Current prototype: a color collection puzzle in the match-3 family. It is not a networked service and has no advertising, account, analytics, or remote configuration requirements.
- General reusable guidance: use `game-developer` for Unity engineering and `match3-level-production` for match-3 style level rules, cascades, collection slots, tunnels, and playability validation.
- The standard workflow for adding or tuning levels is [LEVEL_PRODUCTION.md](LEVEL_PRODUCTION.md). Follow it before changing catalog, pattern, pool, rack, or special-level data.

## Project-Specific Source Layout

Keep the YarnMatch implementation split by responsibility under `Assets/Scripts/YarnMatch`:

```text
Core/                         Pure gameplay models and rules
Core/Generation/              Subject masks, sampling, template deck, round data preparation
Infrastructure/               Runtime bootstrap and scene integration
Presentation/UI/              UI construction, rendering, overlays, references
Presentation/Animation/       Gameplay-independent presentation timelines
Presentation/Effects/         Pooled transient visuals and trails
Audio/                        Sound playback policy and clip handling
Visuals/                      Procedural sprites and visual asset factories
```

The core models are:

- `YarnMatchBoardModel`: per-column board stacks, exposed-cell checks, removal, progress.
- `YarnMatchBoardPatternGenerator` and `YarnMatchLevelCatalog`: seeded complete color patterns and centralized difficulty progression.
- `YarnMatchPoolModel`: the level-configured lower selection area, selectable cells, orthogonal unlocks, and tunnel queues.
- `YarnMatchRackModel`: in-progress same-color collection slots, capacity, progress, and final-slot unlock.
- `YarnMatchGame`: command orchestration and game-state transitions only. It may coordinate services, but it must not become a UI builder or animation implementation.
- `YarnMatchCollectionScheduler`: fair receiver reservations, exposed-column lookup,
  and independent busy-column lifetime across collection and settling.
- `YarnMatchLevelDifficulty`: final legal dimensions and profile-based subject ranking.
- `YarnMatchOpeningPlanner`: existing-token arrangement for natural two-color openings.
- `YarnMatchPatternResources`: async local pack loading and worker metadata preparation.

Presentation ownership is kept behind `YarnMatchPresentation`, which delegates to focused UI, animation, effect, and overlay services. `YarnMatchVisualFactory` owns procedural visual resources. Do not reintroduce a monolithic `YarnMatchGame` or presentation class to solve a local issue.

## Runtime Resource Contract

- All visible text uses TextMeshPro components.
- The packaged project font is `Assets/Resources/Fonts/SimHei.ttf`, loaded through project resources. Runtime code must not depend on `C:/Windows/Fonts` or any other developer machine path.
- TMP Essential Resources are present under `Assets/Text Mesh Pro` and must remain available to runtime UI creation.
- Font lookup must tolerate missing TMP settings and provide a useful error rather than dereferencing an uninitialized global setting.
- New Chinese UI copy must be stored as UTF-8 and checked for glyph coverage in the actual TMP font asset.
- Android launcher artwork lives in Assets/Art/YarnMatch/AppIcon. YarnMatchAndroidIcons
  exports the game's existing tile/spool sprites and configures legacy, round and
  adaptive icons. The APK builder applies these settings before every build.

## Current Gameplay Contract

The authoritative rules are in [GAME_RULES.md](GAME_RULES.md). Current implementation:

- Normal previews reserve a fresh seed from a difficulty profile; entry consumes
  that seed and restart prepares a new one. No per-level layout is loaded or saved.
- Two hundred local 64 x 64 pixel-art templates supply normal board subjects. The special challenge
  keeps its Excel-derived 48 x 40 image and independently randomizes its lower pool.
- Normal boards use explicit subject masks and preserve initial sparse coordinates.
  Source selection uses a difficulty-scored no-repeat deck and recent-history preference. Offline
  artwork under .codex/tools/pattern-art exports PNGs and matching palette/mask
  data. No API dependency or runtime subject-art generation is required. Current
  review sheets and the resource manifest live in .codex/previews/local-patterns.
- Actual per-color board counts determine spool counts and capacities, including
  capacity-1/2 tails. Each spool owns a separate concurrent rack job.
- Visible lower positions grow from legal entrances. Four-direction selection access
  and eight-neighbor thaw damage are separate rules.
- Generation plans chains and freezes against reachable attack sources; consumed
  chains do not persist on pipe refills, and refresh retains occupied source topology.
- Chapters append 50 thumbnails to a four-column reusable scrolling grid. Previous chapters remain
  accessible. Special challenge does not consume normal level 110 or normal progress.
- Independent local music loops through menus and rounds; toggle/volume are persisted.
- Approved normal geometry caps: 50 x 50 square boards, 50 x 100 long boards,
  and 12 x 8 lower pools (including pipes). Later chapters retain high-tier
  difficulty and fresh layouts within these limits.

## Presentation Contract

- Keep the bright, clean, white mobile UI and strong color separation from the current reference direction. Do not introduce dark desktop panels or low-contrast placeholder colors.
- Selection, source removal, tunnel emergence, board settling, collection, rack impact, completion, win, loss, restart, hint, and refresh each need visible feedback.
- Selection flight and board collection run in parallel. Each selected spool owns an independent collection job. The scheduler repeatedly scans the current exposed front row, so a red job may collect, a green job may expose a new red cell, and the red job can continue without being restarted.
- Each cell remains visible while its inner strands retract one by one along a soft curved path into the matching receiver. Do not replace this with a disappearing tile and a separate fake line.
- `YarnMatchBoardSurface` owns reusable column graphics and active-tile pooling.
  `YarnMatchBoardColumnGraphic` emits visible quads plus one-cell overscan only.
  A single board hit surface maps pointer coordinates to the current column/row.
- `YarnMatchBoardAnimationController` handles continuous bottom-up row removal,
  soft threads, additive receiver progress and independent column drops. The spool
  animation controller keeps arrival, emergence and completion behavior separate.
- Round preparation uses a worker for plain board/pool/mechanic data. Unity resources
  load asynchronously; plain JSON preparation and grid sampling also run in workers.
  Unity object access and UI stay on the main thread. Thumbnail uploads use a 3 ms
  budget; lower UI creation yields after a 4 ms slice. No frame-rate claim without profiling.
- A receiver that reaches three visibly tightens, scales up, pulses, and exits upward. Do not represent collection as an instant disappear followed by a delayed fake spawn.
- All buttons and tappable visual elements need a press/hover feedback path without allowing feedback components to own gameplay rules.

## Safe Change Protocol

1. Read the relevant project document in `.codex` and the affected core/presentation owner before editing.
2. Keep gameplay mutations in `Core` and orchestration in `YarnMatchGame`; keep coroutines and visual timing in presentation services.
3. Preserve public contracts and serialized/runtime bootstrap behavior unless the change explicitly requires a contract change.
4. Validate a cold start, start from the main menu, restart, reset during an active effect, win, loss, and scene teardown when touching lifecycle code.
5. Compile `Assembly-CSharp.Player.csproj` with no warnings or errors when possible. Run the additional checks in [VALIDATION.md](VALIDATION.md).
