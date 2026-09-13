# YarnMatch Level Production

1. Read GAME_RULES.md before changing rules. Missing chapters are generated in
   50-level batches from catalog profiles and saved as complete initial layouts.
   Stored levels are reused for previews, replays and future app sessions.
2. Author meaningful local art in .codex/tools/pattern-art and compile it into
   Assets/Resources/YarnMatch/Patterns/PatternTemplates.json. Each template has
   an id, title, minimum usable board size, RGB palette, equal-length rows and a
   same-size binary mask. Symbols index the palette; mask 1 means a real yarn cell.
   Author the mask explicitly: white subject areas must survive, genuine holes must not.
3. Export a matching PNG for asset review. Gameplay and selection thumbnails both
   use the same saved PatternLayout; never choose
   an independent static PNG for a playable item. The existing special image stays separate.
4. Crop to mask bounds, preserve the natural aspect ratio and sample the subject
   using categorical occupied-area coverage. Select distinct source color regions
   before mapping them to spool colors. Count only occupied cells. Do not use borders
   to invent extra colors. Author meaningful internal regions in higher-detail art.
5. Create spools from those counts, including a capacity-1/2 tail where needed.
   Never derive board colors from independently randomized lower spools.
6. Allocate pipe/output pairs and visible positions from the real token budget.
   Grow all visible positions from row 0 or pipe-neighbor entrances using four
   directions. Do not use empty cells as access bridges.
7. Allocate every remaining token into the actual pipe queues. Keep queues varied,
   with total capacities matching the board exactly.
8. Plan chains and freezes against access simulation: chains need both endpoints;
   freezes require four independent nearby source cells, and thaw after three hits.
   Reject an individual impossible placement rather than relaxing live rules.
9. Keep presentation independent: four-column reusable thumbnail rows without level
   numbers, local art, independent audio and separate animation ownership. Entry
   restores the initial saved snapshot; replay uses the same layout. Static
   columns use batched visible meshes; only collecting tiles use pooled UI objects.
   Load Unity resources asynchronously, decode their plain data and sample grids in
   workers, and queue Unity thumbnail uploads with a 3 ms per-frame budget. Pure
   board/pool/mechanic data builds once in a worker and is persisted with ordered
   queues, capacities and mechanism links. Later entry only restores fresh models;
   lower UI construction uses 4 ms. File IO stays off the Unity thread.
10. Compile the Player assembly and check Unity asset import. The user requested
    manual gameplay testing, no added test scripts, and no APK or commit unless asked.

## Current Verification

- Player compilation is the basic engineering check.
- Quantity and reachability are enforced by construction and the generation planner.
- Do not claim a complete puzzle solver, exhaustive verification, or mobile playtest
  based on compilation.
- Normal board caps are 50 x 50 (square) and 50 x 100 (long image).
  Normal lower pools cap at 12 x 8, including pipe cells. The level list grows
  indefinitely while high-tier rules and fresh layouts continue within these caps.

## Local Resource Production

The active pack contains 200 distinct 64 x 64 transparent PNGs and matching grid
data: 40 food, 50 nature, 50 animals, 50 objects and 10 Chinese idioms. The original
50 subjects have been redrawn in place; their IDs and .meta GUIDs are preserved.
Do not use the historical .codex/previews/patterns-50.png as the current review sheet.

Offline ownership under .codex/tools/pattern-art:

- shapes.cjs: shared vector drawing primitives and the existing game RGB palette.
- food/nature/animals/objects/idioms.cjs: distinct subject artwork and authored
  minimum sampling detail. This is source art, not game-side procedural generation.
- proof.cjs: cropped, occupied-area sampling proofs for asset review only.
- profiles.cjs: per-readable-width occupied counts, color counts and natural
  interruption pressure for all six normal color budgets. Matches the runtime
  representative-color selection and does not rearrange source pixels.
- bake.cjs: opaque palette quantization, transparent masks, PNG/JSON export,
  stable Unity asset metadata and contact sheets. Requires Node.js and Sharp;
  Chinese glyphs are rasterized offline with SimHei. No API keys or network calls.

Run from the project root, with Sharp resolvable by Node:

```powershell
node .codex/tools/pattern-art/bake.cjs
node .codex/tools/pattern-art/bake.cjs --publish
```

The first command creates previews only. Review all five category sheets and
their corresponding *-detail-sheet.png in .codex/previews/local-patterns before
publishing. review-sheet.png is a compact selection; manifest.json lists every
subject, minimum detail, source color count, occupied pixel count and pixel hash.
On this workstation Sharp is bundled at
C:/Users/admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules;
set NODE_PATH to that directory when it is not installed locally. This path is
an offline tooling prerequisite, never a runtime dependency.

Publishing exports into Assets/Resources/YarnMatch/Patterns and currently requires
200 unique subjects, all previous IDs represented, and at least four foreground
source colors. PNG rows and binary masks share the exact same quantized pixels:
alpha is either 0 or 255, and white inside a subject stays occupied. Do not hand-edit
the compiled JSON independently of the artwork. For future pack expansion, update
the publication count deliberately along with the subject inventory.

Minimum-detail proofs use the same 25% occupied-area rule as the runtime sampler,
without reducing the source palette. The compiler raises the authored threshold
if a source color would disappear below four colors at a nominal width through 50.
Faces start at 22 columns, fine leaves/objects at up to 24 and idioms at 44.
These proofs are not playable thumbnails or a gameplay test: actual level color
budgets (4 to 9), early no-pipe dimension limits and game palette mapping still
apply. Eligibility now uses final dimensions, so no selected art drops below its
minimum. Actual color count can remain below the later cap for simpler subjects;
do not add arbitrary colored padding to manufacture a ninth color.

The pack now has 6,637 size profiles. Difficulty selection scores actual subject
size against a rising target, missing colors against the level's color budget,
and natural interruptions against rising pressure. Category preference and bounded
score jitter add variation. No-repeat remains scoped to eligible subjects.
This is difficulty ranking, not a complete puzzle solver or strict monotonically
increasing occupied-cell count across arbitrary replays.

YarnMatchOpeningPlanner can position a partial opening and its supporting spool
from level 10 when the image itself permits it. It only swaps existing tokens into
plain occupied cells, retaining source topology, capacities and mechanic positions.
It never inserts blockers or recolors the subject to force a combination.

Runtime only loads the prepared template data, selects an eligible subject and
samples it at the round's detail. It never executes the art compiler or synthesizes
a new subject image. Gameplay thumbnails still use their exact saved PatternLayout,
not a separately selected source PNG. Restart Play Mode after resource publication
if the current session already cached the previous template pack.

YarnMatchLevelStore awaits YarnMatchPatternResources.LoadAsync only when generating
missing normal chapters. Resource objects/text extraction stay on the Unity thread;
sampling, initial mechanic planning, chapter IO and live-model restoration run off-thread.
The store caches at most two chapters; recycled thumbnails release their pending
snapshot task and never install stale results. Existing chapter files bypass the
artwork/generation path. Publish saves with a temporary file and same-directory rename.
Keep initial snapshots independent of live mutation; refresh never writes saves.

Background music is Assets/Resources/YarnMatch/Audio/QuietStitches.wav.
Its deterministic offline source is .codex/tools/BakeBackgroundMusic.cs; it is not
compiled into the game. Music is a 53.33-second, 72-BPM original instrumental loop.
