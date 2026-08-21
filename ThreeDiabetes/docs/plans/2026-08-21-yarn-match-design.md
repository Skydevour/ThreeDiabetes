# Yarn Match Prototype Design

## Goal

Build a single-player Unity prototype inspired by the supplied yarn-spool puzzle screenshot. The player selects colored spools from a lower grid, uses each spool to collect up to three exposed yarn cells of the same color, and manages a limited rack of in-progress spools.

## Core Loop

1. Yarn cells are arranged in nine vertical columns on the upper board.
2. Only the topmost remaining cell in each column is exposed and can be collected.
3. Clicking a lower-grid spool consumes up to three exposed cells of that color. A same-color spool already in the rack is reused; otherwise a free rack slot is required.
4. A rack entry clears after collecting three cells. Partial entries remain and occupy a slot.
5. Seven rack slots start unlocked. The eighth slot unlocks after the board is half cleared.
6. The player wins when all yarn cells are collected. The player loses when the rack is full and no selectable spool can make progress.

## Presentation

- Portrait-oriented 2D UI with a dark plum board area and a lighter lavender control area, matching the supplied reference.
- Procedural yarn-cell and spool sprites so the prototype has no external art dependency.
- The lower spool grid uses the same nine-column rhythm as the upper board.
- Removal is shown as a short yarn trail that pulls the cell toward the matching rack spool, followed by a small scale/fade finish.
- Header exposes level, remaining yarn, and clear progress. Restart and hint actions are included for iteration.

## Architecture

- `YarnMatchGame` owns the play-state machine, board data, rack state, pool tokens, UI creation, and transition rules.
- `YarnVisualFactory` creates cached procedural sprites and common UI textures at startup.
- `YarnMatchBootstrap` starts the game after any loaded scene, so the existing template scene needs no fragile serialized references.
- Runtime-only visual objects are reused where practical; animation objects are short-lived and bounded by the three-cell collection limit.

## State and Validation

- States: `Playing`, `Resolving`, `Won`, `Lost`.
- Every selection is validated against state, token availability, rack capacity, and board progress.
- After each resolved move, the game checks win first, then unlock progress, then deadlock/loss.
- The deterministic initial layout keeps the prototype reproducible while still requiring the player to manage partial rack entries.

## Future Extension Points

- Move level data into ScriptableObjects once the first playable loop is approved.
- Add multiple board masks, color palettes, and spool queues without changing the rack rules.
- Add audio, haptic feedback, and richer yarn physics after the core interaction is stable.
