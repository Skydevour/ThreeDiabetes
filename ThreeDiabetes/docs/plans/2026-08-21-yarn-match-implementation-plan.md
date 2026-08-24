# Yarn Match Prototype Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create a playable, mouse-driven yarn spool puzzle in the existing Unity 6 2D project.

**Architecture:** A runtime bootstrap creates one self-contained `YarnMatchGame` under the existing sample scene. The game builds a responsive Canvas UI, stores the board as per-column stacks, and uses cached procedural sprites for cells, spools, rack slots, and panels. The resolving state animates up to three collected cells before evaluating win, unlock, and deadlock rules.

**Tech Stack:** Unity 6000.0.23f1c1, C#, UnityEngine.UI, URP 2D, Input System-compatible pointer buttons.

---

### Task 1: Add the runtime game implementation

**Files:**
- Create: `Assets/Scripts/YarnMatch/Core/YarnMatchGame.cs`

**Step 1: Write the core state and data model**

Implement the play states, yarn colors, per-column stacks, spool tokens, rack entries, and board progress calculations. Keep all gameplay transitions in explicit methods so they can be tested independently of animation.

**Step 2: Build the Canvas UI**

Create the header, upper board, rack, lower nine-column spool grid, feedback label, and restart/hint buttons at runtime. Use procedural sprites and cached component references.

**Step 3: Implement selection and rack rules**

Allow only unused lower-grid spools to be clicked. Consume exposed same-color cells up to three at a time, merge same-color rack entries, unlock the eighth slot at 50% progress, and trigger loss when the rack is full with no useful move.

**Step 4: Add feedback animation**

Animate collected cells along a curved yarn trail into the rack entry, update progress bars, pulse successful matches, and show clear win/loss overlays.

### Task 2: Add automatic scene bootstrap

**Files:**
- Create: `Assets/Scripts/YarnMatch/Infrastructure/YarnMatchBootstrap.cs`

**Step 1: Spawn the game after scene load**

Use `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)` and guard against duplicate game instances. The existing `SampleScene` remains usable as the host scene.

### Task 3: Verify the Unity prototype

**Files:**
- Inspect: `Assets/Scripts/YarnMatch/Core/YarnMatchGame.cs`
- Inspect: `Assets/Scripts/YarnMatch/Infrastructure/YarnMatchBootstrap.cs`

**Step 1: Run a static compile-oriented check**

Confirm the scripts use only Unity 6-compatible APIs, have no missing namespaces, and avoid per-frame allocations in `Update`.

**Step 2: Open or batch-run the project in Unity**

Run the existing scene and verify the Canvas, board, spool grid, selection flow, rack limit, unlock state, restart, hint, win, and loss overlays.

**Step 3: Review the first playable pass**

Check the layout at the portrait reference resolution and at a wide editor viewport. Fix only blockers to the requested basic loop before adding polish.

## Follow-up Revision: 2026-08-22

The next pass keeps the runtime bootstrap but changes the implementation details above: front cells are taken from the bottom of each column, board cells animate into lower rows, the lower pool is an eight-lane four-row tunnel layout, visible spool buttons are replenished from hidden per-lane queues, UI copy is Chinese, and the refresh tool preserves the multiset of unused spools. The collection animation now includes rack rotation and tightening rather than only a short flight.
