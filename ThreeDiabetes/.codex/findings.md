# Findings

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
