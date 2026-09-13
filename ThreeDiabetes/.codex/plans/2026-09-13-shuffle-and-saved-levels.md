# Global Shuffle and Saved Levels

Approved scope: edit this project in place, no test scripts, commits or APK.
This replaces the earlier per-entry randomization and targeted refresh decisions.

1. [complete] Shuffle all unconsumed visible/queued tokens together. Preserve
   every empty cell, slot mechanic, queue length, token identity/color/capacity.
   Remove the targeted-completion refresh path. Retain stable-scene gating.
2. [complete] Add plain initial-layout snapshots under Core/Persistence and an
   asynchronous local chapter store under Infrastructure/Persistence. Save full
   upper colors/mask, lower cells/tokens, pipe queues, freezes and chains.
   Publish each completed chapter with a temporary file and rename. Never silently
   regenerate a saved chapter on load errors. Special challenge has a separate file.
3. [complete] Generate missing 50-level chapters at startup and when progression
   reveals another chapter. Cache only two chapters; load existing layouts without
   sampling art or running mechanic/opening generation. Rebuild mutable round models
   from the immutable initial snapshot. Feed thumbnails from the same snapshot.
4. [complete] Review conservation and reference restoration paths, compile through
   Unity, and update current project rules. User performs gameplay/device testing.

Verification: Unity 6000.0.23f1c1 batch import/compile exited 0 after final edits;
no C# warnings/errors in Logs/saved-levels-compile.log. Scoped diff check passed.
No gameplay tests, device timing, APK build or commit were performed.
