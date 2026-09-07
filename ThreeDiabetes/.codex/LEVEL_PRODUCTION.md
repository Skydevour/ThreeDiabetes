# YarnMatch Level Production

1. Read GAME_RULES.md before changing rules. Runtime layouts are not authored or
   stored per level; the catalog computes a profile and creates a new round seed.
2. Author or update a meaningful pixel template in
   Assets/Resources/YarnMatch/Patterns/PatternTemplates.json. Each template has
   an id, title, minimum usable board size, RGB palette and equal-length rows.
   Symbols index the local palette. Keep readable silhouettes and a real background.
3. Export a matching PNG for the level-list artwork. The bitmap is a static asset;
   gameplay samples the palette matrix so it does not need readable textures.
   The current pack contains 50 templates. The existing special image stays separate.
4. Sample the selected template at the configured board size. Map to the allowed
   number of normal colors, preserve large subject regions, then count real cells.
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
9. Keep presentation independent: reusable scrolling chapter rows, local art,
   independent audio, and separate animation ownership.
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

The 50-template review sheet is .codex/previews/patterns-50.png.
Background music is Assets/Resources/YarnMatch/Audio/QuietStitches.wav.
Its deterministic offline source is .codex/tools/BakeBackgroundMusic.cs; it is not
compiled into the game. Music is a 53.33-second, 72-BPM original instrumental loop.
