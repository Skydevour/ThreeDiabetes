# YarnMatch Single-Responsibility Refactor Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Split YarnMatch presentation code into focused Unity services without changing the confirmed offline gameplay or portrait UI behavior.

**Architecture:** `YarnMatchPresentation` becomes a thin facade. Static hierarchy creation, dynamic view rendering, overlays, animation playback, effect pooling, UI primitives, and theme/layout constants move into dedicated modules. `YarnMatchGame` and the pure data models remain the only owners of gameplay state.

**Tech Stack:** Unity 6, C#, UnityEngine.UI, TextMeshPro, procedural sprites, coroutines, pooled UI effects.

---

### Task 1: Extract shared presentation contracts

**Files:**
- Create: `Assets/Scripts/YarnMatch/Presentation/UI/YarnMatchUiTheme.cs`
- Create: `Assets/Scripts/YarnMatch/Presentation/UI/YarnMatchUiViews.cs`
- Create: `Assets/Scripts/YarnMatch/Presentation/UI/YarnMatchUiReferences.cs`
- Create: `Assets/Scripts/YarnMatch/Presentation/UI/YarnMatchUiPrimitives.cs`

Move theme constants, view records, UI references, and primitive creation helpers out of the presentation facade. These files must not contain gameplay decisions or animation loops.

### Task 2: Extract UI construction and dynamic rendering

**Files:**
- Create: `Assets/Scripts/YarnMatch/Presentation/UI/YarnMatchUiBuilder.cs`
- Create: `Assets/Scripts/YarnMatch/Presentation/UI/YarnMatchUiRenderer.cs`

Keep static hierarchy construction in the builder and model-driven updates in the renderer. Preserve the current public operations required by animation and game flow.

### Task 3: Extract overlays, effects, and animation

**Files:**
- Create: `Assets/Scripts/YarnMatch/Presentation/UI/YarnMatchOverlayController.cs`
- Create: `Assets/Scripts/YarnMatch/Presentation/Effects/YarnMatchEffectPool.cs`
- Create: `Assets/Scripts/YarnMatch/Presentation/Animation/YarnMatchAnimationController.cs`

Move toast/result/menu behavior, pooled flying/trail visuals, and all presentation coroutines into their respective owners. Animation code must not mutate gameplay models.

### Task 4: Replace the facade and verify

**Files:**
- Modify: `Assets/Scripts/YarnMatch/Presentation/UI/YarnMatchPresentation.cs`

Retain the game-facing API as delegation only. Compile the Player project, run whitespace and legacy UI scans, and review file sizes and dependencies for responsibility leaks.
