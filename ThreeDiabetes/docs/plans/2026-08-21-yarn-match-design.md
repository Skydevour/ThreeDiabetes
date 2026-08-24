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

- `YarnMatchGame` owns the play-state machine, model orchestration, and transition rules; board, pool, and rack data live in their dedicated models.
- `YarnMatchPresentation` delegates static UI construction, dynamic rendering, overlays, animation, and effects to focused presentation services.
- `YarnMatchVisualFactory` creates cached procedural sprites and common UI textures at startup.
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

## 2026-08-22 Polish Revision

- Collection direction is bottom-to-top: index `0` in each column is the exposed front cell; removing it reindexes the remaining cells and animates them downward.
- The lower control area is a four-row by eight-lane layout. Each lane has a non-clickable tunnel marker, one visible spool, and a hidden spool queue. Removing the visible spool leaves its slot empty until the next spool slides out of the tunnel.
- All player-facing copy is Chinese and uses a desktop Chinese dynamic font when available.
- A once-per-round refresh rearranges only unused spools and preserves the remaining spool count.
- Collection feedback now combines a curved yarn trail, a rotating rack spool impact, gradual progress tightening, and a completion pulse.
