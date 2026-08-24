# YarnMatch Game Rules

## Board Resolution

The upper board is represented as eight independent vertical columns. Each column stores its front cell at index `0`; the early levels start with twelve cells per column and later chapter levels use up to eighteen cells per column.

```text
front / exposed -> [0] [1] [2] ... [n]
```

Only index `0` can be collected. Removing it must:

1. Mark the removed cell inactive.
2. Remove it from the column.
3. Reindex the remaining cells from `0`.
4. Animate the remaining cells toward the front position.
5. Re-scan all columns for newly exposed cells before resolving the next matching cell.

A selected spool owns an independent collection job and may collect up to three exposed cells of its color. Multiple jobs may run at the same time. Reserve a cell before starting its animation so two jobs cannot collect the same cell, then commit removal and schedule another scan when the animation completes.

## Lower Selection Pool

The selection area contains 8 columns and 6 rows. Cells are indexed by `(column, row)` and source positions are stable after a token is removed.

- Row `0` is initially unlocked.
- Selecting a valid token consumes that token and clears its source cell immediately.
- The empty source cell remains empty. Never compact the whole grid or shift unrelated tokens into it.
- Unlocking is persistent for the round and spreads to the four orthogonal neighbors of a selected source: up, right, down, and left.
- Diagonal cells do not unlock from this rule.
- Unlocking all reachable neighbors is a set expansion, not a single linear path. Any unlocked token may be selected.
- A tunnel marker is not a selectable token. It describes a directional queue attached to one target cell.

## Tunnels

Each tunnel has:

- One target pool cell.
- One direction used by the emergence animation and visual marker.
- An ordered queue of hidden tokens. Each level calculates a queue depth that can contain the exact unconsumed spool budget derived from the board.

When a target cell is consumed:

1. Keep the target empty while the selected token flies away.
2. After the configured delay, dequeue at most one token.
3. Attach that token to the same target cell.
4. Play an emergence animation from the tunnel direction.
5. Update the displayed remaining count.

Do not let tunnel markers receive pointer input. Do not generate bonus tokens during replenishment. A refresh may reorder tunnel queues, but it must preserve the multiset and total count of unconsumed tokens.

## Collection Rack

The rack stores entries by selected spool. Every selected spool needs a free unlocked slot, even when another entry has the same color. Each entry has progress from `0` to `3`.

- Same-color entries never merge. Partial progress remains in the rack and blocks its own slot.
- At progress `3`, play completion feedback, remove the entry, and free the slot.
- Begin with seven unlocked slots and unlock the final slot at half board progress, rounded up.
- Check the win condition before the loss condition after every completed resolution.
- A loss requires both no free usable rack slot and no selectable token that either matches an existing entry or can enter a free slot.

## Refresh and Hint

Refresh is optional assistance, not a second source of tokens. It can be used once per round and must:

- Reorder unused visible tokens.
- Reorder hidden tunnel queues.
- Preserve every token identity and color count.
- Preserve consumed board cells, rack entries, progress, unlocks, and empty source positions.

Hint evaluates currently selectable tokens only. Prefer an existing rack color, then the color with the greatest number of currently exposed board cells. Hint feedback must not mutate state.

## State Machine

```text
MainMenu -> Playing -> Resolving -> Playing
                         |             |
                         +-> Won       +-> Lost
Playing --restart------> Resolving -> Playing
```

## Level Selection and Progression

- The main menu opens a level-selection overlay before any round is created.
- Level configurations are centralized in `YarnMatchLevelCatalog`; the game controller does not contain per-level layout literals.
- A level increases difficulty through a larger upper-board silhouette, more colors, more lower-pool rows, more tunnel targets, and deeper tunnel queues.
- The color curve starts at one color for levels 1-3, adds one color every three levels, and caps at the available palette size.
- Tunnels start at level 10 with one target and add one target every four levels, capped by reachable lower-pool capacity.
- Levels 1-9 use seeded balanced random color mixing; from level 10 onward, the board uses a seeded 8-column pixel-art template with contiguous color blocks.
- Board size grows in four stages: 3, 4, 5, then 6 spool groups per column, while every group still contains exactly three cells.
- The upper board is generated as a seeded, complete color-block pattern. The seed changes on each restart while preserving the configured silhouette and color count.
- Levels are generated without a global cap in chapters of 50. Only the next level is unlocked after a win, and the highest unlocked level is persisted with PlayerPrefs for this offline prototype.
- The pool owns exactly the per-color spool budget derived from the board: each color gets `ceil(colorCells / 3)` spools. A level must never be mathematically unwinnable because it ran out of a specific color, and clearing the board must not leave spare selection spools.

Input is accepted in `Playing`, including while other spool and collection jobs are animating. Each source token is consumed immediately to prevent duplicate selection. `Resolving` is reserved for the one-frame restart transition and terminal cleanup. Restart must invalidate old jobs and safely rebuild the models and UI.



## Presentation and Access Invariants

- Render each upper-board cell as a stable square. The visual grid must use one shared cell size and one shared horizontal/vertical step for both backplates and live cells; never use a larger background tile that overlaps the live cell.
- A collecting cell remains visible while its inner strands retract from bottom to top. Keep the cell width unchanged, reduce only its height, and anchor the top edge while the lower rows disappear. Build the strand count from one shared theme constant.
- A hint is valid only when a currently selectable lower token has a currently exposed, unclaimed board cell of the same color and the rack has capacity. Highlight both the source token and the first matching board cell; otherwise report no collectible move.
- Daily full-level access is temporary convenience state. Store its local date and enabled flag separately from the persistent highest-completed progression. Reset only the daily flag when the local date changes.
