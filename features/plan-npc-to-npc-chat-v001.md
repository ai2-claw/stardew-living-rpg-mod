# NPC-to-NPC Chat

## Overview

Implement actual multi-turn conversations between two AI NPCs. Instead of a single "offscreen conversation" prompt, create a genuine back-and-forth dialogue that extracts meaningful thoughts, opinions, schemes, and plots from character interactions.

## Current State

### Existing Ambient System (`ModEntry.cs:2310-2479`)

- **Single message**: Speaker NPC sends one prompt, gets one response
- **No exchange**: Listener NPC never responds
- **Command-focused**: Responses primarily generate world-state commands (`record_town_event`, `publish_rumor`, etc.)
- **Narrative framing**: "{Speaker}, you had a brief offscreen conversation with {Listener}..."

### Player2 Infrastructure (`Integrations/Player2Client.cs`)

- `SpawnNpcAsync` - Creates NPC sessions with grounding prompts
- `SendChatAsync` - Sends message to specific NPC, streams response
- NPCs maintain session state across messages
- Each NPC has unique `npc_id` for targeting

### Relevant Constraints

- **3 ambient conversations per day** limit
- **8-minute cooldown** per NPC
- Commands filtered by context (`npc_to_npc_ambient` policy)
- Stream-based response handling with JSON command extraction

## Changes Needed

### 1. Multi-Turn Conversation Loop

Replace single-prompt with exchange:

```
Turn 1: Speaker -> Listener (topic seed)
Turn 2: Listener -> Speaker (response/reaction)
Turn 3: Speaker -> Listener (elaboration/debate)
Turn 4: Listener -> Speaker (conclusion/agreement)
```

### 2. Conversation Depth

- Extract opinions on recent town events
- Reveal personal schemes or plots
- Express feelings about other NPCs (gossip)
- Debate town issues (e.g., Joja vs community)
- Share secrets or rumors

### 3. Context Building

Each NPC needs context about:
- Their relationship with the other NPC
- Recent interactions between them
- Shared history/events
- Their individual personality/archetype

### 4. Command Extraction

After conversation completes, extract commands from full transcript:
- `record_town_event` for significant revelations
- `publish_rumor` for gossip shared
- `record_memory_fact` for personal opinions learned
- Do not emit `publish_article` from ambient NPC-to-NPC multi-turn chats

### 5. Rate Limiting

- Increase cooldown for multi-turn conversations
- Consider reducing daily limit (more expensive)
- Add per-pair cooldown to prevent same NPCs repeatedly

## Implementation Plan

### Phase 1: Core Conversation Engine

1. [x] Create `NpcConversationService.cs` to manage multi-turn dialogues
2. [x] Implement `StartNpcConversation(speakerId, listenerId, topic)` method
3. [x] Implement conversation turn loop with configurable depth (2-4 turns)
4. [x] Build per-NPC grounding context (relationship, recent interactions)

### Phase 2: Conversation Prompts

5. [x] Design topic seeding prompts (what to discuss)
6. [x] Design response prompts that encourage opinions/schemes
7. [x] Add personality-specific conversation styles via `NpcSpeechStyleService`

### Phase 3: Integration

8. [x] Replace `TryTriggerAmbientNpcConversation()` with new service call
9. [x] Update cooldown logic for multi-turn conversations
10. [x] Add conversation transcript logging for debugging

### Phase 4: Command Extraction

11. [x] Parse full conversation for command-worthy content
12. [x] Extract gossip as `publish_rumor` with source attribution
13. [x] Extract opinions as `record_memory_fact` within bounded command budget

### Phase 5: Polish

14. [x] Add config options for conversation depth
15. [x] Add config options for daily conversation limit
16. [x] Add debug command to trigger specific NPC pair conversations
17. [x] Add overhear moment gate rules (public place/time only, low frequency, no private content)

## Dependencies

- `Player2Client` - Existing NPC chat infrastructure
- `NpcSpeechStyleService` - Personality/archetype weights
- `TownMemoryService` - Shared context building
- `NpcMemoryService` - Per-NPC memory
- `CommandPolicyService` - Command filtering

## Risks

- **API latency/cost blow-up (4 turns = 4x calls)**
  - **Mitigation**: Start with default depth `2` turns (max `4`), enforce per-turn timeout, and abort conversation after one failed retry.
  - **Detection**: Add telemetry for turn count, total duration, timeout count, and abort reasons.
  - **Rollback**: Keep a feature flag to fall back to the current single-prompt ambient path.

- **Player chat starvation during ambient exchange**
  - **Mitigation**: Before each turn, re-check pending player responses and suspend/reschedule ambient chat when player flow is active.
  - **Detection**: Track ambient deferrals caused by player-priority checks.
  - **Rollback**: If deferral rate spikes, reduce ambient daily cap and increase cooldown.

- **Mid-conversation invalid/empty NPC replies**
  - **Mitigation**: Validate each turn (non-empty, parseable speaker output), allow one repair retry, then abort with no command extraction.
  - **Detection**: Emit explicit reject/abort reasons (for example `E_AMBIENT_TURN_EMPTY`, `E_AMBIENT_TURN_INVALID`).
  - **Rollback**: Automatically downgrade to a shorter conversation depth for the rest of the day.

- **Command over-application from full transcript parsing**
  - **Mitigation**: Run extraction only once at conversation end under `npc_to_npc_ambient` policy, with per-conversation command budget caps.
  - **Detection**: Telemetry counters for attempted vs applied commands per ambient conversation.
  - **Rollback**: Clamp to event-only mode (`record_town_event`/`record_memory_fact`) when publish/mutation reject rates increase.

- **Repetitive pair loops and tone drift**
  - **Mitigation**: Add per-pair cooldown (`speaker|listener`) plus short topic dedupe window using town event tags/summary fingerprints.
  - **Detection**: Track pair diversity ratio and repeated-topic frequency over rolling 7 days.
  - **Rollback**: Increase pair cooldown and reduce same-pair selection weight.

- **Race conditions with player-initiated NPC interaction**
  - **Mitigation**: Use cancellation tokens per turn; if either NPC enters player chat, cancel ambient run and clear in-flight state in `finally`.
  - **Detection**: Count cancellation reasons (`player_interrupt`, `timeout`, `stream_error`).
  - **Rollback**: Disable same-tick ambient starts for NPCs recently touched by player chat.

- **Transcript storage bloat / persistence noise**
  - **Mitigation**: Keep full transcript logging debug-only; store only concise derived facts/events in save state.
  - **Detection**: Monitor `state.json` growth and debug log volume after 7-day simulation runs.
  - **Rollback**: Disable transcript persistence and retain only last-N in-memory debug entries.

## Risk Mitigation Acceptance Criteria

- No ambient transcript lines leak into player chat surfaces.
- Ambient conversation abort rate stays below a defined threshold in 7-day simulation runs.
- Per-conversation command budget is never exceeded.
- Same NPC pair does not dominate ambient traffic over a rolling week.
- Save-state size growth remains stable when debug transcript capture is off.

## Locked Decisions (v001)

1. **Default depth by mode**
   - `cozy_canon`: 2 turns
   - `story_depth`: 3 turns
   - `living_chaos`: 4 turns
   - Hard maximum: 4 turns
2. **Failure fallback policy**
   - Retry failed turn once.
   - If retry fails, abort the full conversation and apply no transcript-derived commands.
3. **Per-conversation command budget**
   - Maximum 3 commands total:
     - Either 1 `record_town_event`
     - Or up to 2 `record_memory_fact` (max 1 per participant NPC)
     - Optional 1 `publish_rumor`
   - `publish_article` is disallowed in this ambient lane.
4. **Pair cooldown**
   - Enforce 2 in-game days for the same unordered pair (`A|B` == `B|A`).
5. **Transcript retention**
   - Full transcript remains debug-only and in-memory.
   - Persist only derived structured outcomes (event/memory/rumor facts).
6. **Conversation visibility**
   - Enable overhear moments.
   - Gate rules:
     - Public place/time only (for example saloon/town square daytime/evening windows)
     - 1-2 line snippet only
      - Deterministic low cadence (minimum in-game day gap from config)
     - Exclude private/sensitive content

## Technical Notes

- Each turn requires separate Player2 API call
- Need to manage concurrent conversation state carefully
- Consider conversation "suspension" if player initiates chat mid-conversation
- Transcripts could be stored for newspaper "overheard in town" features
