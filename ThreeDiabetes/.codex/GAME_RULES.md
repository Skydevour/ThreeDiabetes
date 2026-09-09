# YarnMatch Game Rules

## Runtime Levels

- Normal rounds use fresh seeds. Visible thumbnails reserve a template plus a seed;
  entry consumes that reservation and restart creates a fresh one. Reservations are memory-only.
- The level number selects a difficulty profile, not a saved layout.
- Normal color budgets: levels 1-3 allow 4, 4-6 allow 5, 7-9 allow 6,
  10-12 allow 7, 13-15 allow 8, and later levels allow 9. Actual colors come
  only from the subject. Local replacement art has at least four foreground source
  colors; sampled colors remain subject- and detail-dependent. Do not insert arbitrary
  colored padding to meet a count.
- Normal detail starts at width 12, grows by one per level, and caps at 50.
  Template eligibility uses the final no-pipe budget-limited width, never the
  nominal width followed by a second resize below the authored minimum.
  Crop explicit mask bounds and preserve the subject aspect ratio, up to 100 rows.
  No random double-height padding. Subjects with height <= 1.25 x width fit fully;
  taller subjects descend through the viewport. Cells remain square and opaque.
- Normal boards cap at 50 x 100 bounding positions (5,000 maximum occupied cells).
  Mask holes are not cells. Normal lower pools cap at 12 x 8 (96 slots including pipes).
  Before level 10, constrain image detail to the no-pipe spool budget; expand visible
  positions when necessary, within that cap. High-tier difficulty does not reset at 51.
- Lower pool sizes by ten-level stage: 8 x 6, 10 x 6, 10 x 7, 10 x 8,
  11 x 8, then 12 x 8 from level 51 onward.
- Normal pipe ranges stop increasing after the first 50-level difficulty curve:
  later levels request 25-35 pipes, subject to feasible distinct pipe/output pairs.
  Queue allocation still consumes the exact remaining spool budget.
- Static art lives in Patterns/PatternTemplates.json with 200 local 64 x 64 PNGs.
  Each template includes a separate occupancy mask, independent of its RGB palette.
  A scored eligible-template deck avoids repeats until exhausted, with an eight-subject
  recent-history preference across refills. Difficulty favors a rising occupied-cell
  target, more actual colors and natural short-run interruptions; subject-category
  variation and bounded score jitter keep replays varied. These are difficulty bands,
  not a guarantee that every consecutive random image has more occupied cells.
  Never mirror text. The subjects are
  authored offline (40 food, 50 nature, 50 animals, 50 objects and 10 idioms), not
  runtime-generated images. Faces require detail 22; idioms require detail 44.
- Small boards use categorical area sampling; large boards retain source pixel regions.
  Distinct retained source colors receive distinct existing spool colors, including
  contrasting neutrals when needed. Only occupied pixels contribute to the palette.
  No additional colors are injected into border or background cells.
- From level 10, when the intact picture has a one/two-cell opening whose third
  cell can be freed with one supporting color, place the two existing capacity-3
  spools at plain selectable/adjacent positions. A conservative two-color prefix
  condition ensures support fits one spool regardless of column scheduling order.
  No matching opportunity means no forced alteration. Board pixels, occupied lower
  positions, mechanics, token identities and capacities remain unchanged.
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

- Each upper column exposes its lowest remaining occupied cell, not necessarily row 0.
  Preserve original coordinates/holes at entry. After removal that column moves down
  one cell height; unrelated columns do not move or wait. Reserve the column before animation.
- Every selected spool owns a separate rack entry and collection job.
- Ready receivers reserve one target per turn, round-robin. Per-color column cursors
  start at seeded offsets, avoiding left-first bias. A collecting/falling column is
  unavailable until settled; other columns continue independently.
- Concurrent incoming visual progress is additive for each receiver. Full receivers
  start exiting immediately, without waiting for unrelated columns to fall.
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
- One four-column scrolling thumbnail grid contains all revealed chapters; old chapters remain visible.
  Completing level 50 unlocks 51 and reveals 51-100, and so on.
- Items do not show level numbers. Their thumbnails use the exact generated board
  colors, dimensions and orientation from the pending seed, not a separate PNG.
  Locked items retain their picture and a lock-state label. Only visible rows and
  overscan own textures; recycled thumbnail components release their old textures.
  Thumbnail sampling runs in a worker; Unity texture uploads run in a main-thread
  queue with a 3 ms between-upload budget. An item cannot enter before its picture
  is ready. Rebinding drops the old task reference so stale results cannot publish.
- Only highest unlocked progression and today's temporary unlock state are saved.
  Music enabled/volume preferences are also local.
- Daily unlock applies to all revealed chapter levels, expires on local date change,
  and does not unlock infinitely many future chapters.

## Presentation

750 x 1334 centered gameplay layout, Chinese TextMeshPro text, bright opaque cells.
Backgrounds use a 750 x 1624 reference and cover the actual screen without gaps.
Menu, level selection and result roots stretch to the Canvas, including their
input-blocking backgrounds; gameplay controls retain their existing geometry.
Visible pool spools show their actual capacity (1/2/3) in a compact white circular
badge at the lower right, including locked, frozen and chained spools. Badges stay
within their cells and above mechanic artwork. Capacity follows the token through
refresh, pipe refills and flight; it is not a prediction of immediately exposed matches.
Rack progress appears on arrival and is hidden before the completion animation.
Continuous bottom-to-top strand collection preserves cell width and top edge.
Flights and collection are concurrent. Completed spools pulse before leaving.
Local instrumental BGM uses an independent looping source and continues across
menu, level selection, gameplay and result transitions.
Local pattern loading uses Resources.LoadAsync; plain JSON decoding, source metadata
preparation and grid sampling run in workers. Unity object access stays on the main
thread. Resource preparation is awaited before catalog selection; no runtime art
creation or network calls are involved.
