# Yarn Match Levels and Pattern Design

**Goal:** Add an offline level-selection flow and a scalable, seeded color-pattern generator while preserving the existing independent collection and tunnel rules.

**Architecture:** `YarnMatchLevelCatalog` owns level data, `YarnMatchBoardPatternGenerator` produces the complete upper-board color layout, and `YarnMatchPoolModel` accepts the selected pool dimensions and tunnel parameters. The game controller only selects a config and coordinates model/presentation resets; UI classes render the selection overlay and current level.

## Level progression

Ten runtime configurations increase the board silhouette from 68 to 112 cells, colors from four to eight, pool rows from four to six, and tunnel queue depth from two to three. Every level has eight pool columns and exactly `sum(ceil(colorCells / 3))` spool tokens, so every color has enough capacity and clearing the board cannot leave spare selection spools. The highest unlocked level is kept in memory for this single-player prototype and advances only after a win.

## Pattern generation

The generator uses a deterministic seed per round. It keeps the configured column-height silhouette, cycles a shuffled palette through the exposed front row, and fills the remaining cells with contiguous two-by-two color blocks plus sparse accents. This creates a complete, readable multi-color pattern rather than an unstructured per-cell shuffle while still changing between restarts.

## UI flow

`开始游戏` opens `选择关卡`. Each level button shows its color and token summary; locked entries remain visible but disabled. Selecting a level creates the board and pool, hides overlays, and updates the header. The result overlay supports both `再来一局` and `选择关卡`.

## Validation

- Compile `Assembly-CSharp.Player.csproj` with no warnings or errors.
- Check that the generated project includes all three new core scripts.
- Verify the existing tunnel, empty-source, independent rack, and recursive front-cell collection contracts remain unchanged.
- Compare low, middle, and final-level layouts at the fixed `750×1334` reference resolution.
