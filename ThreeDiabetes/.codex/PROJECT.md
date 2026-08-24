# ThreeDiabetes Project Guidance

## Project Identity

- Engine: Unity `6000.0.23f1c1`.
- Target: offline, single-player, portrait mobile puzzle game.
- Reference resolution: `750 x 1334`.
- Current prototype: a color collection puzzle in the match-3 family. It is not a networked service and has no advertising, account, analytics, or remote configuration requirements.
- General reusable guidance: use `game-developer` for Unity engineering and `match3-level-production` for match-3 style level rules, cascades, collection slots, tunnels, and playability validation.
- The standard workflow for adding or tuning levels is [LEVEL_PRODUCTION.md](LEVEL_PRODUCTION.md). Follow it before changing catalog, pattern, pool, rack, or special-level data.

## Project-Specific Source Layout

Keep the YarnMatch implementation split by responsibility under `Assets/Scripts/YarnMatch`:

```text
Core/                         Pure gameplay models and rules
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

Presentation ownership is kept behind `YarnMatchPresentation`, which delegates to focused UI, animation, effect, and overlay services. `YarnMatchVisualFactory` owns procedural visual resources. Do not reintroduce a monolithic `YarnMatchGame` or presentation class to solve a local issue.

## Runtime Resource Contract

- All visible text uses TextMeshPro components.
- The packaged project font is `Assets/Resources/Fonts/SimHei.ttf`, loaded through project resources. Runtime code must not depend on `C:/Windows/Fonts` or any other developer machine path.
- TMP Essential Resources are present under `Assets/Text Mesh Pro` and must remain available to runtime UI creation.
- Font lookup must tolerate missing TMP settings and provide a useful error rather than dereferencing an uninitialized global setting.
- New Chinese UI copy must be stored as UTF-8 and checked for glyph coverage in the actual TMP font asset.

## Current Gameplay Contract

The authoritative rules are in [GAME_RULES.md](GAME_RULES.md). The short version is:

- The upper board has eight columns and up to eighteen cells per column. Each level provides a complete seeded color pattern and a column-height silhouette. Index `0` in each column is the exposed front cell; collecting it causes remaining cells to reindex and fall toward the front.
- Every selected lower spool gets its own rack slot, even when another spool has the same color. Each slot collects exposed board cells of its own color, up to three cells for that spool. Multiple spools and multiple cells may resolve concurrently.
- The lower pool scales from a smaller early-level layout to the final `8 x 6` layout. The first row is initially selectable. Selecting a spool permanently leaves its source position empty, unlocks its four orthogonal neighbors, and does not force the player into a single path.
- A tunnel is attached to a target pool cell. It is a visual, non-clickable directional source with a level-configured queue. When its target becomes empty, the next queued token may replenish that exact cell after the presentation delay. The pipe shape, direction, and remaining queue count are visible.
- A rack entry occupies one slot until it reaches three collected cells. Full entries play a completion pulse and leave the rack. Same-color entries remain separate and never merge.
- Seven rack slots begin unlocked. The eighth slot unlocks after at least half of the board has been collected.
- The player wins when all board cells are collected. The player loses when the rack has no usable capacity and no selectable color can make progress.
- Refresh is a once-per-round utility. It rearranges unused visible tokens and hidden tunnel queues without creating or destroying tokens or changing consumed progress.
- The main menu opens level selection. Levels are generated without a global cap in 50-level chapters. Only the next level is unlocked after a win; the highest unlocked level is persisted with PlayerPrefs for this offline prototype.

## Presentation Contract

- Keep the bright, clean, white mobile UI and strong color separation from the current reference direction. Do not introduce dark desktop panels or low-contrast placeholder colors.
- Selection, source removal, tunnel emergence, board settling, collection, rack impact, completion, win, loss, restart, hint, and refresh each need visible feedback.
- Selection flight and board collection run in parallel. Each selected spool owns an independent collection job. The scheduler repeatedly scans the current exposed front row, so a red job may collect, a green job may expose a new red cell, and the red job can continue without being restarted.
- Each cell remains visible while its inner strands retract one by one along a soft curved path into the matching receiver. Do not replace this with a disappearing tile and a separate fake line.
- A receiver that reaches three visibly tightens, scales up, pulses, and exits upward. Do not represent collection as an instant disappear followed by a delayed fake spawn.
- All buttons and tappable visual elements need a press/hover feedback path without allowing feedback components to own gameplay rules.

## Safe Change Protocol

1. Read the relevant project document in `.codex` and the affected core/presentation owner before editing.
2. Keep gameplay mutations in `Core` and orchestration in `YarnMatchGame`; keep coroutines and visual timing in presentation services.
3. Preserve public contracts and serialized/runtime bootstrap behavior unless the change explicitly requires a contract change.
4. Validate a cold start, start from the main menu, restart, reset during an active effect, win, loss, and scene teardown when touching lifecycle code.
5. Compile `Assembly-CSharp.Player.csproj` with no warnings or errors when possible. Run the additional checks in [VALIDATION.md](VALIDATION.md).
