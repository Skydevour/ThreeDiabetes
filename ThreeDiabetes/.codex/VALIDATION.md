# YarnMatch Validation

## Static Checks

Run from the project root:

```powershell
dotnet build Assembly-CSharp.Player.csproj --no-restore --nologo
git diff --check
```

Review the final file list and ignore Unity-generated `Library`, `Temp`, `obj`, solution, and project-file churn unless the task explicitly changes generated metadata or project settings.

## Runtime Smoke Pass

Use Unity 6 and a portrait Game view at `750 x 1334`.

1. Enter Play Mode from a cold editor state.
2. Confirm the main menu renders Chinese copy with TextMeshPro and no missing-glyph warnings.
3. Start a game and confirm the upper board is eight columns with the level-configured twelve-to-eighteen cell depth, while the first row of the level-configured lower pool is selectable.
4. Select two spools quickly, including two of the same color, and confirm both source cells become empty immediately, both spools fly independently, and the rack uses two separate slots.
5. Confirm four-direction neighbors unlock, diagonal cells do not, and the selection is not restricted to a single route.
6. Confirm exposed board cells resolve from the front upward. Select a red and green spool in quick succession and verify each job keeps scanning after another job exposes a new matching cell.
7. Confirm multiple cells can be collecting at once; each source tile remains visible while its inner strands retract one by one, then the receiver rotates, tightens, pulses, and exits at three cells.
8. Confirm a tunnel target remains empty before replenishment, then receives one queued token from the marked pipe direction and decrements its visible count from two to one to zero.
9. Use refresh once. Verify no token count or consumed progress changes and the second use is rejected.
10. Verify the final rack slot unlocks at half board progress.
11. Verify both win and loss overlays, restart, hint, and input gating.
12. Restart while an effect is active and inspect for null references, orphaned pooled visuals, duplicate event systems, or stale click handlers.

## Failure Triage

- For `NullReferenceException`, read the full stack trace and inspect the first project-owned frame. Check initialization order, reset order, scene unload, and destroyed UI roots before adding a guard.
- For missing Chinese glyphs, verify the project font asset, TMP settings resource, atlas population mode, and actual runtime resource path. Never fix this by reading an OS font path at runtime.
- For a collection mismatch, log the committed token color, exposed-cell coordinate, receiver progress, and board total before inspecting animation timing.
- For tunnel duplication/loss, compare token identity counts before and after consume, replenish, refresh, and restart.
- For stale visuals, verify every effect pool `Get`, `Release`, and `Clear` path across initialization and teardown.
## Level and Pattern Checks

- Open the main menu and confirm `开始游戏` opens `选择关卡` instead of starting a round immediately.
- Confirm level buttons show Chinese labels, color count, board-token count, and lock state.
- Select level 1 and confirm the board is a complete multi-color pattern with fewer cells than the final levels.
- Complete or simulate a win and confirm only the next level becomes available; locked levels remain non-interactable.
- Compare level 1, a middle level, level 50, and level 51: the chapter seed changes, board depth and color/tunnel pressure progress, and the pool remains within the mobile layout budget, and the pool must progress from fewer rows to the full `8×6` layout.
- Restart the same level twice and confirm the silhouette is stable while the color pattern seed changes.
- Confirm empty pool sources remain empty, tunnel counts decrement, and tunnel emergence stays aligned with the configured pool row count.


- Confirm every live board cell and its backplate are square and use the same theme size; check the maximum 8 x 18 board for overlap.
- During collection, confirm the cell X scale remains constant, the Y scale decreases from bottom to top, and all ten internal strands are positioned inside the square.
- Use hint in a state with selectable tokens but no exposed matching color and confirm it reports no collectible move without mutating state.
- Use today's full-level unlock once, confirm the current 50-level chapter is accessible and the button becomes unavailable, then simulate a date change and confirm ordinary progression remains while the daily button returns.
