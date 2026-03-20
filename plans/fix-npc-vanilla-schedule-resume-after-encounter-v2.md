# Plan v2: Fix NPC Vanilla Schedule Resume After Encounter (No Warp)

## TL;DR

After an encounter, NPCs warp/teleport instantly to their schedule destination instead of walking. Two root causes: (1) `HasVanillaResumeState` success gate is too strict — demands immediate controller/movement, but vanilla won't provide that until the next 10-minute tick; (2) warp fallbacks fire when pathfinding doesn't produce instant results. Fix by trusting vanilla's schedule system: load schedule + set `followSchedule=true` = success. Remove all warp code. For same-map cases, keep direct PathFindController as an optimization only.

## Root Cause

The actual schedule loading works fine — `TryLoadSchedule()` succeeds and `npc.Schedule` gets populated. But the code demands INSTANT proof of movement via `HasVanillaResumeState` (requires `controller != null || isMoving() || temporaryController != null`). Vanilla SDV doesn't create a PathFindController until the next `checkSchedule()` call during the 10-minute game time tick. Since the NPC isn't moving yet, the code thinks it failed.

**Two warp paths that fire:**
1. **Path C in `TryRebindVanillaScheduleAtCurrentTime` (line ~15121):** `TryPathfindToScheduleDestination` — when NPC is on a different map than schedule destination, delegates to `TryWarpToScheduleDestination` (instant teleport)
2. **Max-attempt fallback in `TryProcessPendingVanillaEncounterResumes` (line ~15041):** After 10 failed attempts, explicitly calls `TryWarpToScheduleDestination`

**Why the same-map PathFindController sometimes fails:** SDV's `PathFindController` constructor returns null-equivalent when no path exists (e.g., target tile unreachable from current position due to obstacles, or the target is stale from the pre-computed route).

**Core insight:** Vanilla SDV NPCs don't need a PathFindController to resume schedules. With `followSchedule = true` and a loaded `Schedule` dict, vanilla's internal `checkSchedule()` (called at 10-minute intervals) will naturally pathfind the NPC to their next destination, including walking to map edges and using doors/warps between maps. We just need to load the schedule and get out of the way.

## Steps

### Phase 1: Relax the success gate (core fix)

1. **In `TryRebindVanillaScheduleAtCurrentTime` (line 15064):** After `npc.TryLoadSchedule()` succeeds and schedule is populated, treat that as success immediately — return `"ScheduleLoaded"` — even if `HasVanillaResumeState` is false. The schedule is loaded, `followSchedule=true`, vanilla will handle the rest on the next game time tick. Remove the `HasVanillaResumeState` gates that block success after Path A and Path B.

2. **Keep the same-map direct PathFindController attempt** as a BONUS (not required for success). If the NPC is on the same map as their schedule destination, try creating a PathFindController. If it works, great — the NPC starts walking immediately. If it doesn't, that's fine — the schedule is loaded and vanilla will process it.

### Phase 2: Remove all warp code

3. **Delete `TryWarpToScheduleDestination` method entirely** (lines ~15310-15335). It's the source of all instant teleporting.

4. **In `TryPathfindToScheduleDestination` (line ~15337):** Remove the cross-map branch that calls `TryWarpToScheduleDestination`. For cross-map destinations, return empty string — vanilla's schedule system handles cross-map movement naturally via map-edge warps and door transitions.

5. **In `TryProcessPendingVanillaEncounterResumes` (line ~15041):** Remove the entire max-attempts warp fallback block that calls `TryWarpToScheduleDestination`. Replace with a simple log-only failure message. NPCs won't be stranded because Phase 1 ensures the schedule is loaded on attempt 1.

### Phase 3: Simplify retry logic

6. **Reduce retry complexity:** Since success is now determined by "schedule loaded" rather than "NPC immediately moving", attempt 1 should succeed in almost all cases. Keep the retry loop (attempts 2-10 check `HasVanillaResumeState` as a "did vanilla catch up?" bonus) but it's no longer critical. The NPC is already on track after attempt 1.

## Relevant Files

- [mod/StardewLivingRPG/ModEntry.cs](mod/StardewLivingRPG/ModEntry.cs) — All changes:
  - Lines 15064-15135: `TryRebindVanillaScheduleAtCurrentTime` — relax success gate (step 1-2)
  - Lines 15310-15335: Delete `TryWarpToScheduleDestination` (step 3)
  - Lines 15337-15370: `TryPathfindToScheduleDestination` — remove cross-map warp branch (step 4)
  - Lines 14962-15062: `TryProcessPendingVanillaEncounterResumes` — remove warp fallback (step 5), simplify retries (step 6)

## Verification

1. `dotnet build` — compiles clean
2. In-game: trigger NPC encounter, let it complete. Verify NPCs do NOT teleport. They should either start walking immediately (PathFindController succeeded) or stand briefly then walk at the next 10-minute tick (vanilla schedule kicked in).
3. SMAPI console: look for `"returned {npc} to vanilla schedule"` with `method=ScheduleLoaded` or `method=PathFindController(direct)` — no `"warped"` messages, no `"failed to return"` messages
4. Verify no `Game1.warpCharacter` calls remain in the encounter-resume code path (grep for `warpCharacter` in the methods)
5. Cross-map edge case: encounter in Town when NPC's next schedule slot is in their house. NPC should walk to the map edge and transition naturally.

## Decisions

- **No warping, ever.** Vanilla schedule handles cross-map movement naturally. Worst case is the NPC stands still for up to 10 game-minutes before vanilla kicks in.
- **Schedule loaded = success.** Don't demand immediate movement proof. Vanilla will process the schedule.
- **Keep PathFindController as optimization only.** Same-map direct pathfinding makes NPC start walking sooner, but it's not required.
- **Scope:** Only encounter-resume flow. Not touching encounter staging, face-to-face, or schedule override.
