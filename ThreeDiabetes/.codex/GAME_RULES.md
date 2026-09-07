# YarnMatch Game Rules

## Runtime Levels

- Normal rounds are generated on entry and restart using a fresh seed.
- The level number selects a difficulty profile, not a saved layout.
- Normal colors: levels 1-3 use 4, 4-6 use 5, 7-9 use 6,
  10-12 use 7, 13-15 use 8, and later levels use 9.
- Normal board dimensions grow from 5 x 5 using min(50, max(5, global level)).
  From level 15, each entry has a 25% chance of a double-height rectangle.
  Square boards fit fully; rectangular boards descend through the viewport.
- Normal square boards cap at 50 x 50 (2,500 cells); double-height boards cap at
  50 x 100 (5,000 cells). Normal lower pools cap at 12 x 8 (96 total slots,
  including pipes). High-tier difficulty does not reset at level 51.
- Lower pool sizes by ten-level stage: 8 x 6, 10 x 6, 10 x 7, 10 x 8,
  11 x 8, then 12 x 8 from level 51 onward.
- Normal pipe ranges stop increasing after the first 50-level difficulty curve:
  later levels request 25-35 pipes, subject to feasible distinct pipe/output pairs.
  Queue allocation still consumes the exact remaining spool budget.
- Static art lives in Patterns/PatternTemplates.json and 50 matching PNGs.
  Runtime samples one template, maps it to the configured palette size, and
  preserves the subject. It does not generate images online or save layouts.
- Special challenge is independent of normal level 110: its upper board retains
  the Excel-derived 48-column, 40-row matrix, while its lower pool is randomized.

## Quantity Conservation

The board's actual per-color counts are authoritative. For each color:

    spool count = ceil(cell count / 3)
    sum(spool capacities) = cell count

The last spool of a color can have capacity 1 or 2. All other spools have capacity 3.
Collection, refresh, tunnel replenishment, progress bars and completion must use
the actual capacity. A token exists either in one pool cell, one tunnel queue,
or as a consumed token. Never copy a token to create assistance.

## Selection Access

- Row 0's existing spools start unlocked.
- Pipe cells are unlocked and unlock their four immediate neighbors.
- Selecting a spool clears its source immediately and permanently unlocks four
  orthogonal neighbors. Empty cells do not propagate access.
- Generation grows occupied positions from legal entrances, so holes never
  isolate a locked group. Runtime never unlocks everything to repair a layout.
- A pipe occupies its own cell and feeds only its adjacent directional output.
  Removing its output spool dequeues at most one token after the presentation delay.
- Visible slots are populated from the real spool budget; remaining tokens are
  allocated to pipes. Pipe count and queue lengths vary within feasible budgets.
  Exact total quantities take precedence when an early board cannot supply a
  configured minimum, or a large board exceeds nominal queue ranges.

## Frozen and Chained Cells

- Mechanics begin at level 10. The requested total increases by about 7 every
  5 levels, with random variation. Caps: 15 freezes and 8 two-spool chains.
- Selecting any of a frozen cell's eight neighbors deals one thaw hit.
  Three hits thaw it and make it accessible.
- Each generated freeze has at least four distinct neighboring attack sources
  reachable without thawing any freeze. Its chained partner cannot count as
  an independent source. Adjacent freezes are not generated.
- Chain selection requires both endpoints unlocked and thawed, plus two rack
  slots. Both tokens are consumed atomically and fly concurrently.
- Generation simulates this access rule when choosing chains and freezes.
  Impossible candidates are omitted; actual mechanic counts may be below target.
- A chain ends when its pair is consumed. Pipe refills are ordinary spools.
- The generation planner proves access and thaw dependencies, not that every
  player choice wins under limited rack space.

## Collection and Results

- Each upper column exposes row 0 only. Reserve a cell before its animation.
- Every selected spool owns a separate rack entry and collection job.
- Concurrent jobs collect matching exposed cells and rescan after settling.
- Rack completion uses actual capacity and frees the slot after its animation.
- Seven rack slots start open; the eighth unlocks at half board progress.
- Defer terminal evaluation while arrivals, collections, settling, completion
  or pipe replenishments are in progress.
- Win requires no remaining board cells, selection tokens or rack entries.
- Fail only when the settled rack is full and cannot make further progress.
- Failure provides replay, board preview and home; normal success provides
  next level and home. Special success does not change normal progression.

## Assistance and Progress

- Refresh has no usage limit. It arranges an available target color without
  changing capacities or removing occupied locked/frozen source positions.
  Visible rearrangement swaps tokens; queue exchanges preserve token identity.
- Hint considers selectable spools with an exposed match and enough rack slots.
- One scrolling list contains all revealed chapters; old chapters remain visible.
  Completing level 50 unlocks 51 and reveals 51-100, and so on.
- Only highest unlocked progression and today's temporary unlock state are saved.
  Music enabled/volume preferences are also local.
- Daily unlock applies to all revealed chapter levels, expires on local date change,
  and does not unlock infinitely many future chapters.

## Presentation

750 x 1334 portrait UI, Chinese TextMeshPro text, bright opaque cells.
Continuous bottom-to-top strand collection preserves cell width and top edge.
Flights and collection are concurrent. Completed spools pulse before leaving.
Local instrumental BGM uses an independent looping source and continues across
menu, level selection, gameplay and result transitions.
