# Plan: Player2 Chat Concurrency Fix

Fix player chat fallback timeouts caused by overlapping player and ambient NPC chat traffic.

## Overview

Player chat currently uses a per-message request path with history polling fallback, while ambient NPC chat uses stream-routed requests. These flows share routing/pending state and can interfere when both are active, which matches the observed log: "Player chat history poll timed out with no fresh NPC response."

Success criteria:
- A player-initiated NPC chat does not timeout solely because ambient chat is running.
- Ambient chat never consumes or blocks player chat response tracking.
- Logs/telemetry make lane conflicts visible when they occur.

Assumptions:
- Some Player2 models may behave poorly with simultaneous in-flight chats.
- Preventing overlap is acceptable if it prioritizes player chat reliability.

## Current State

- `SendPlayer2ChatPerMessage` (`mod/StardewLivingRPG/ModEntry.cs:5115`) sends player chat, then falls back to `StartPlayerChatHistoryFallback` (`mod/StardewLivingRPG/ModEntry.cs:9400`) when no immediate payload is usable.
- `StartPlayerChatHistoryFallback` waits up to 18s and can pause when `_npcResponseRoutingById` has a queued `false` route entry for that NPC (`mod/StardewLivingRPG/ModEntry.cs:9417`).
- Ambient chat is triggered by `TryTriggerAmbientNpcConversation` (`mod/StardewLivingRPG/ModEntry.cs:1809`) and sent through `SendPlayer2ChatInternal(... captureForPlayerChat: false)` (`mod/StardewLivingRPG/ModEntry.cs:1875`), which enqueues `false` routing entries (`mod/StardewLivingRPG/ModEntry.cs:5090-5092`).
- Ambient triggering currently checks `_player2PendingResponseCount` but does not guard against per-message player chat pending state in `_npcUiPendingById` (`mod/StardewLivingRPG/ModEntry.cs:1829`).

## Changes Needed

- Add a simple lane-serialization guard so ambient sends are skipped/deferred while player chat is pending.
- Decouple player fallback polling from ambient routing queue state so ambient entries cannot block fallback progress.
- Ensure routing bookkeeping is lane-specific enough that ambient events cannot decrement or satisfy player pending counters.
- Add focused diagnostics for overlap suppression and fallback outcomes.

## Tasks

- [x] 1. Define lane-serialization rule: player chat has priority; ambient sends are deferred when any player chat is pending.
- [x] 2. Implement a minimal pending-player-chat check and apply it in `TryTriggerAmbientNpcConversation` before ambient send.
- [x] 3. Update `StartPlayerChatHistoryFallback` to stop waiting on ambient routing queue entries (`false`) as a readiness condition.
- [x] 4. Audit `CaptureNpcUiMessage` and routing queue usage to ensure ambient routing cannot consume or block player UI pending flow.
- [x] 5. Add trace logs/counters for deferred ambient sends and for fallback success vs timeout reasons.
- [~] 6. Run verification:
- [ ] 7. Manual in-game scenario: keep ambient enabled, send repeated player chats, confirm no timeout log and responses appear.
- [~] 8. Regression checks: compile-only verification completed (`dotnet msbuild ... /t:CoreCompile`); in-game command validation still pending.

## Dependencies

- `mod/StardewLivingRPG/ModEntry.cs` (primary logic for chat lanes, fallback, routing, and ambient trigger)
- `mod/StardewLivingRPG/Integrations/Player2Client.cs` (chat and history APIs already in use)
- Existing in-game debug commands for validation (`slrpg_p2_health`, `slrpg_p2_status`, `slrpg_intent_smoketest`)
