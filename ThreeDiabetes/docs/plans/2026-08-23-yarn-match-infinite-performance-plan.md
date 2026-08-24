# YarnMatch Infinite Levels and Performance Plan

## Goal

Keep the offline YarnMatch prototype playable indefinitely while improving the runtime stability and mobile performance needed for an Android showcase APK.

## Scope

- Generate levels by global level number in 50-level chapters.
- Use a deterministic chapter seed so levels 51-100 and later are new layouts rather than repeats of the first chapter.
- Show the active chapter in the level selector and unlock the next level only after a win.
- Persist the highest unlocked level locally with PlayerPrefs.
- Prevent loss evaluation while tunnel replenishment is pending.
- Reuse lower-pool token buttons and images instead of destroying/recreating them on every refresh or replenishment.
- Keep source positions, tunnel queues, rack entries, and concurrent collection jobs unchanged in meaning.
- Configure an Android command-line build entry point that writes an APK below the project root `build` directory.

## Constraints

- Offline single-player only.
- Fixed 750 x 1334 portrait reference resolution.
- No network, advertising, analytics, or account systems.
- No automated test suite added for this pass; use compilation, static checks, and the final Android build as verification.
- Keep responsibilities separated under `Assets/Scripts/YarnMatch`.

## Phases

- [ ] Refactor level catalog and progression for unlimited 50-level chapters.
- [ ] Stabilize terminal evaluation and pending tunnel replenishment.
- [ ] Reuse pool presentation objects and reduce runtime allocations.
- [ ] Add Android build entry point and verify project settings.
- [ ] Compile, inspect the final diff, and build the showcase APK into `build`.

## Decisions

- Global level numbers are 1-based. Chapter index is `(level - 1) / 50`; chapter level is `(level - 1) % 50 + 1`.
- The chapter seed is derived from the chapter index and is combined with the local level seed, so a new chapter gets a new deterministic map family.
- The selector displays exactly one 50-level chapter at a time, centered on the highest unlocked chapter.
- The eighth rack slot unlock rule and the existing four-direction pool rule remain unchanged.
