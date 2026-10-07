# Implementation Progress

## 2026-10-07 Level Volume and Cell Contrast

- User reported too few levels and white upper-board cells blending into the light
  background. Private .cc-switch skills were left untouched; only project files changed.
- YarnMatchLevelCatalog now reveals ten chapters (500 levels) in the four-column grid.
  The grid still generates chapter snapshots lazily through thumbnail or round
  requests, and startup only prepares the chapters up to the player's progress.
- Daily unlock now opens every shown level for today, matching the documented rule.
  FinishGame advances the saved frontier only when the exact frontier level is won,
  and AdvanceToNextLevel no longer writes progress by itself, so replaying or
  skipping ahead through daily-unlocked levels cannot skip the unlock frontier.
- The shared knit pattern bakes a soft dark cell edge, and the board area gained a
  light blue-grey backdrop panel, so white and cream cells stay readable.
- Player compilation passed with zero warnings/errors; scoped diff check passed.
  APK rebuilt for device testing (build/YarnMatch.apk, 38,601,079 bytes, dated
  2026-10-07 21:40); no commit, test scripts or device profiling.

## 2026-10-07 Board Fit and Spool Contrast

- User reported that white and cream spools blended into the near-white background
  and that tall boards zoomed past the screen, hiding the colors still to be
  collected. Private .cc-switch skills were left untouched; only project files changed.
- ConfigureBoardLayout now always fits the complete board (columns x initial rows)
  into the 700 x 500 viewport. The old FitToViewport gate no longer lets tall normal
  boards or the special challenge render at full cell size beyond the visible area.
- YarnMatchVisualFactory.CreateSpoolTexture bakes a dark silhouette rim; colors with
  luminance >= 0.72f receive a 3 px stroke instead of 2 px. Pool tiles, rack slots and
  the capacity-badge edge were tinted and strengthened for contrast.
- Player compilation passed with zero warnings/errors; scoped diff check passed.
  Android APK built by batchmode (build/YarnMatch.apk, 38,603,437 bytes, dated
  2026-10-07) for user device testing. No commit, test scripts or device profiling.
  User owns gameplay verification.

## 2026-09-13 Global Shuffle and Saved Chapters

- Refresh now shuffles visible and queued tokens in one pool, including frozen and
  chained positions. Empty slots, unlock/thaw/chain state, each queue length and
  token identity/color/capacity are preserved. Removed targeted assistance helpers.
- Core/Persistence contains initial board/pool snapshots. Pool restoration creates
  independent tokens/cells and reconnects pipe outputs, queues and chain endpoints.
- Infrastructure/Persistence/YarnMatchLevelStore generates missing 50-level batches
  in workers and saves complete chapter JSON under persistentDataPath/YarnMatchLevels.
  Temporary-file publication, two-chapter cache and separate special.json; gameplay
  never writes the initial snapshots. Saved chapter errors are not silently regenerated.
- Startup prepares revealed chapters; chapter completion begins the next batch.
  Entry/restart and virtualized thumbnails use saved snapshots, avoiding repeated
  image sampling and mechanic/opening generation. Existing progress/preferences remain.
- Final Unity import/compilation exited 0, no C# warnings/errors; scoped diff check
  passed. No test scripts, APK, commits or device-performance claims. Previous APK
  remains the September 11 wave-texture build until another build is requested.

This supersedes earlier per-entry fresh seeds and guaranteed/targeted-refresh plans.

## 2026-09-09 Board CanvasRenderer Fix

- User paused the feature follow-up and reported MissingComponentException on entry.
- Editor.log first fails in YarnMatchBoardSurface.Configure, then repeatedly in
  Unity clipping/raycast code. CreateChild supplies only RectTransform; UGUI Graphic
  requires RectTransform, but unlike Image it does not require CanvasRenderer.
- Added RequireComponent(CanvasRenderer) to both custom MaskableGraphic subclasses:
  YarnMatchBoardSurface and YarnMatchBoardColumnGraphic. No null guards, gameplay
  changes, test scripts or APK. Restart Play Mode to rebuild the runtime hierarchy;
  changing RequireComponent does not repair already-created component instances.

## 2026-09-09 Difficulty and Loading Follow-Up

- User approved the four follow-up items. Preserved the existing dirty worktree,
  special Excel picture, mechanics, source PNGs, and APK.
- Final-dimension eligibility replaces selecting at nominal width and then scaling
  below an asset's readable minimum. The early 96-spool cap includes color tails.
- Added 6,637 offline per-detail profiles to the 200-template pack. Local ranking
  uses occupied count, actual colors, natural interruptions, category variation,
  recent history and seeded score jitter. JSON is now 3,389,828 bytes.
- Added a generation-time opening planner: when the intact subject has a partial
  opening/support opportunity, exchange existing tokens into plain selectable or
  adjacent source positions. No color, capacity, mechanic or occupancy mutation.
- Resource loading is asynchronous; JSON/template metadata and cell sampling run
  in workers. Preview and entry share a round task. UI thumbnail uploads run in a
  3 ms queue; stale task references are dropped on rebinding. Lower UI keeps 4 ms slicing.
- Player compilation passed with zero warnings/errors using existing package outputs
  and isolated Temp intermediate files. An initial attempt to override all output
  paths failed because it redirected package reference lookup; no package edits.
- Refresh implementation is not changed yet. Asked the user whether inherently
  partial-only states should retain the upper board and offer explicit partial help,
  or allow upper-cell swaps to guarantee a fill. Await that design decision.
- No test scripts, gameplay tests, mobile profiling, APK, commits or cleanup scripts.

## 2026-09-09 Local Artwork Publication

- User clarified local offline image creation; this supersedes the API-key blocker
  in the earlier preflight below. No credentials, image API or runtime artwork calls.
- Authored and published 200 distinct native 64 x 64 PNGs: 40 food, 50 nature,
  50 animals, 50 objects and 10 Chinese idioms. Replaced all old 50 subjects with
  newly authored detail, retaining existing resource IDs and Unity metadata.
- Separated shared shapes, themed subject definitions, sampling proofs and export
  responsibilities in .codex/tools/pattern-art. This directory is outside Assets.
- PNGs and JSON palette-index rows / occupancy masks come from the same RGBA data.
  Fully opaque occupied pixels; genuine transparency outside subjects and in holes.
- Inspected all five source sheets and minimum-density proof sheets. Corrected
  overflowing pineapple/candy texture, crescent silhouette and lock keyhole.
  Raised minimum detail for faces, leaf veins, instrument detail and Chinese text.
- Resource inventory: 200 templates, 200 PNGs, 200 unique IDs, matching 64 x 64
  masks, no missing referenced PNG/metas and no unreferenced old PNGs. JSON is
  2,022,471 bytes; the 200 source PNGs total 108,910 bytes.
- Current compact review: .codex/previews/local-patterns/review-sheet.png.
  Existing shared preview/entry sampling consumes the pack without runtime C# edits.
  Special Excel art, mechanics and prior gameplay work remain unchanged.
- No gameplay tests, device profiling, repeat compilation, APK or commit in this
  artwork phase. APK remains 38,149,003 bytes, dated 2026-09-07 22:32:32.

## 2026-09-09 Artwork Continuation Preflight

- User requested continuation of the remaining artwork phase.
- Read imagegen fallback instructions and checked available tools. No built-in image
  generator is exposed; OPENAI_API_KEY and OPENAI_BASE_URL are unset in Process,
  User and Machine environment scopes. Checked presence only, never secret values.
- At that preflight no API requests, new images, gameplay edits, tests, builds or
  commits occurred. The later local-authoring decision above removed this blocker;
  do not request key configuration to continue artwork production.
- Preserved existing worktree changes, including the newly observed solution-file change.

## 2026-09-09 Subject Boards and Performance

- Preserved existing dirty work, capacity badges, backgrounds, special image, and APK.
- Added occupancy masks, cropped sampling, shared pattern layouts and no-repeat reservations.
- Added a separate fair collection scheduler and independent busy-column/drop ownership.
- Replaced per-cell board hierarchies with pooled column meshes and active collection tiles.
- Split board animation from spool animation. Collection runs continuously for 1.2 seconds,
  with fixed tile width/top edge and additive spool thickening/progress.
- Added async plain-data round preparation and 4 ms sliced lower UI construction.
- Accounted for increased no-pipe spool budgets, preserved same-color background/subject
  regions explicitly, and included refill animation lifetimes in terminal deferral.
- Removed obsolete per-cell hover code and the competing rack-impact animation.
- Final Player compilation passed with zero warnings/errors. Editor incremental build
  initially reused the Player UGUI dependency (DefaultControls.factory lacked its
  editor-only setter). Non-incremental Editor compilation passed with zero errors and
  16 Unity package warnings; no package source edits. Scoped diff whitespace check passed.
- Static ownership review: reserve before animation, keep each column busy until drop
  completes, commit each cell once, preserve tails, defer completion during refills.
- No runtime gameplay or device profiling was performed. Restored only our generated
  tracked obj binaries; existing APK, scene/editor state and previous edits preserved.
- At the end of this earlier gameplay phase, artwork was still pending. It has since
  been completed by local authoring in the publication phase above, without an API.

## 2026-09-08 Pattern and Selection Fix

- Read current generation/rendering owners and preserved earlier background/badge edits.
- Located independent preview/runtime template selection and lossy palette merging.
- Added memory-only pending seeds consumed on entry; replay prepares a fresh seed.
- Board and preview share BoardModel.GeneratePattern. Source symbols remain distinct
  until palette selection; area sampling handles small grids and border accents avoid subjects.
- Added four-column virtual rows and exact-cell thumbnail textures, removing level numbers
  and old full-width descriptions. Scrolling one row rebinds four thumbnail textures.
- Player compilation passed with zero warnings/errors; focused diff checks passed.
- Current Unity editor was left open. No runtime/mobile visual verification was performed.
- User owns gameplay testing; no test scripts, APK or commit requested.

## 2026-09-07

- Resumed approved work and read current models and UI integration.
- Existing dirty worktree preserved and extended in scope.
- Current resources contain 10 pixel-grid templates and freeze/chain sprites.
- Implemented runtime profiles and new round seeds; removed fixed Chapter01 layout loading.
- Added connected visible growth and generation-time chain/freeze access planning.
- Added 40 authored static pixel templates alongside the 10 accepted templates.
- Added palette-matrix sampling, ten reusable scrolling chapter rows, and separate special entry.
- Added a local 53.33-second instrumental loop, independent source, toggle and volume.
- Fixed consumed chains persisting on refills and refresh moving occupied locked sources.
- Reduced spool-capacity allocation from repeated token scans to per-color counters.
- Player assembly compiled successfully; Unity batch import exited successfully.
- No gameplay tests, APK, or Git commit.

## Approved Geometry Caps

- User approved normal squares up to 50 x 50, long boards up to 50 x 100,
  and lower pools up to 12 x 8.
- Capped generation dimensions at source. Pool growth uses 8/10/11/12 columns
  and at most eight rows, reaching 96 slots from level 51.
- Capped normal pipe difficulty growth at the first 50-level high tier, preserving
  later random ranges. Freeze count progression also saturates before multiplication.
- Player compilation passed with zero warnings and errors; scoped diff check passed.
  Static layout review: capped 12 x 8 normal pools use 45.25-unit cells and fit
  their 700 x 390 slot viewport. No added tests, APK, or Git commit.
