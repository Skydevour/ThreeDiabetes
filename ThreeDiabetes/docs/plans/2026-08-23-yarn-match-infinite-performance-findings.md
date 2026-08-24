# Findings

- The runtime currently exposes a fixed `YarnMatchLevelCatalog.All` list of 50 entries and clamps `Get(level)` to the last entry.
- `YarnMatchGame` uses `YarnMatchLevelCatalog.All.Count` as a hard maximum in next-level and result logic.
- The level selector indexes the fixed list directly and therefore cannot show levels after 50.
- `YarnMatchGame.TryEvaluateTerminal` checks only collection jobs, not delayed tunnel replenishment coroutines.
- `YarnMatchUiRenderer.RenderPool` clears `PoolButtonsRoot` and creates a new Button/Image pair for every visible token on each render.
- The project uses runtime-generated UI and bootstraps from `AfterSceneLoad`; the build scene is `Assets/Scenes/SampleScene.unity`.
- The project has `Assets/Resources/Fonts/SimHei.ttf`, TMP resources, and a fixed 750 x 1334 CanvasScaler.
- There are no project-owned automated test scripts or asmdefs. This pass will not add them per request.
