# Findings

## 2026-09-09 Local Artwork

- Offline source definitions remove the previous image-service dependency. The game
  receives only static PNGs and precompiled palette/mask data, not drawing scripts.
- The existing 50 source subjects are all represented among the new 200; publication
  preserves their filenames and meta GUIDs instead of leaving orphaned old artwork.
- Native source size is now 64 x 64. Increasing board detail reveals authored facial
  features, veins, seeds, lettering and object structure, not enlarged 16 x 16 blocks.
- Color count alone is not a readability criterion. Source proofs showed lost faces
  at 12 columns, so faces start at 22; idioms remain at 44. Early simple silhouettes
  retain their own lower thresholds. Final game palette reduction is separate.
- Transparent space is explicit mask data. No background padding or alpha threshold
  is inferred at runtime, and subject white is retained. The shared PatternLayout
  remains authoritative for both selection thumbnails and the playable board.

## 2026-09-09 Subject and Scheduling Review

- Dense sampling included background as yarn and injected border colors. Added explicit
  masks to all 50 source templates. Manually retained the cat's cream face/body, which
  shares a palette symbol with its background; runtime never infers emptiness from RGB.
- Original board construction compacted every column. Sparse PatternLayout now preserves
  initial rows and holes, and only the removed column descends by one cell height.
- Scheduler previously scanned left first and waited for all collection jobs before
  dropping the entire board. Column reservations and receiver round-robin replace both.
- Per-cell Images, strand children, CanvasGroups and hover behaviours scaled to thousands
  of objects. Column meshes now draw visible cells plus overscan; only active collection
  tiles allocate pooled Images. One transparent hit mesh maps pointer coordinates to cells.
- Several collection coroutines overwrote the same spool progress. Animated contributions
  are now additive, and separate impact coroutines no longer compete for transforms.
- Increasing detail before pipes could strand unplaced tokens beyond 48 visible slots.
  Early geometry now reserves capacity for tails and the pool expands within 12 x 8.
- API image generation was unavailable during the gameplay phase. The later user
  decision was local offline authoring, now published as 200 native 64 x 64 subjects.
  Later color budgets are still caps, not a promise of nine colors in every subject.

## 2026-09-08 Preview Mismatch

- LevelList chooses a PNG by level modulo 50; TemplateSampler independently chooses
  a random template and mirror using the round seed. These paths cannot match.
- Sampling collapses source colors into nine game colors before choosing a palette,
  irreversibly merging source regions; small colors are then lost again by frequency.
- Source templates are 16 x 16 even when minSize is 5. Small boards necessarily
  simplify detail; their previews must show that same simplification.
- BoardModel's normal runtime path uses TemplateSampler directly, not the legacy
  strategic weaver. The observed mismatch is not caused by the weaver removing cells.
- Preview/runtime now share a generated-cell entry point and pending seed. Texture
  coordinates follow column-major, bottom-up board cells, with aspect-preserving display.
- The existing palette already includes neutral/sky spool colors; normal templates may
  use these to retain outlines/background separation while keeping the 4-to-9 count cap.

- Catalog prioritizes fixed Chapter01 JSON; seeds otherwise depend only on level number.
- ChooseVisibleCells shuffles non-front cells without a connected-growth constraint.
- Freeze planner checks spacing only; it does not model chain-blocked attack sources.
- UI shows only the latest chapter, using 50 fixed buttons and chapter-relative callbacks.
- Special challenge is currently hardwired to normal level 110.
- No BGM source exists; current audio synthesizes short sound effects only.
- Existing image templates are simple 16x16 pixel-grid artwork suitable for static
  palette-index templates and deterministic offline bitmap exports.
- Chains previously retained cell ChainId after consumption, so a single refill
  could wait forever for a consumed partner. Consumed endpoints now clear ChainId.
- Refresh previously vacated locked/frozen source cells. It now swaps visible
  tokens or exchanges a queue token, retaining the generated occupied structure.
- Global linear size growth exceeded mobile display/performance budgets because
  the lower-cell layout clamps cell size to 22 units. The user approved generation
  caps of 50 x 50 / 50 x 100 upper cells and 12 x 8 lower slots for normal levels.
  These bounds keep normal lower cells above 45 reference-resolution units.
