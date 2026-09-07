# Runtime Level Generation Implementation

Approved scope: runtime randomized rounds, reachable selection pool, thawable freezes,
chain combinations, persistent scrolling chapter list, 50 static pattern templates,
and gentle local background music. No APK, commits, or test scripts.

## Stages

- [complete] Replace fixed level loading with rule profiles and per-entry seeds.
- [complete] Grow visible positions from legal entrances; plan mechanics against reachability.
- [complete] Sample static patterns at level resolution; prepare 50 meaningful templates.
- [complete] Reuse scrolling list items for all unlocked chapters; preserve daily access.
- [complete] Add looping local BGM and persisted music controls.
- [complete] Final compilation, static review and documentation; no gameplay playtest.
- [complete] Applied approved normal-level geometry caps: 50 x 50 squares,
  50 x 100 long boards, and 12 x 8 lower pools; compilation and static review passed.

## Decisions

- Board counts are authoritative; each color's spool capacities sum to its cell count.
- Freeze placements reserve at least four distinct reachable neighboring source cells.
- Reachability planning runs at generation time; live selection rules remain authoritative.
- Special challenge retains its Excel-derived image and regenerates its lower layout.
- Normal level 110 must remain a normal level, independent of the special entry.
- Normal geometry stops growing at the approved caps. Later levels retain high-tier
  colors/mechanics and fresh random layouts; chapters and level numbers keep increasing.
