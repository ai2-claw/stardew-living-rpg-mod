# Full Transcript Persistence Plan, Revised for Correctness

## Summary
Add a full player-to-NPC transcript archive that persists across save/load and Player2 session resets, but keep runtime recall fast by separating:

- `ImportantMemories` for durable facts like promises, secrets, preferences, and major personal events
- `RecentTurns` for short-term continuity
- `TranscriptArchive` for full long-form player chat history

The mod remains the only query authority. It searches persisted memory locally, ranks the best matches, and injects only compact results into `game_state_info`.

## Key Changes

### 1. Add a transcript archive plus stable lifecycle links
Add `SaveState.TranscriptArchive` with per-NPC archives.

Persisted transcript types:
- `TranscriptArchiveState`
- `NpcTranscriptArchive`
- `TranscriptSession`
- `TranscriptExchange`
- `TranscriptChunkHeader`
- `TranscriptChunkPayload`

`TranscriptExchange` must include:
- `ExchangeId`
- `RequestToken`
- `NpcId`
- `Day`, `TimeOfDay`, `Season`, `Year`
- `LocationName`
- `ContextTag`
- `PlayerText`
- `NpcText`
- `Keywords`
- `Importance`
- `Visibility`
- `CompletionState`: `complete`, `player_only`, `fallback_completed`, `timed_out`
- `SourceRefKind`: `quest`, `chat`, `system`
- `SourceRefId`
- `LinkedImportantMemoryIds`

`ImportantMemory` must include:
- `MemoryId`
- `Category`: `promise`, `secret`, `preference`, `relationship`, `event`
- `Summary`
- `Keywords`
- `Importance`
- `Visibility`
- `Status`: `active`, `kept`, `broken`, `resolved`
- `SourceRefKind`
- `SourceRefId`
- `SourceExchangeId`
- `CreatedDay`, `LastUpdatedDay`, `LastReferencedDay`

This removes text-only matching for promise lifecycle updates. Quest accept/complete/fail must update memories by stable `questId` link, not by summary text.

### 2. Add request-safe transcript capture
The archive must not rely on NPC-only routing.

Runtime behavior:
- allocate a new `RequestToken` when the player sends a chat line
- open one pending exchange for that NPC with full player-side metadata
- serialize unresolved player chat per NPC
- do not allow multiple unresolved outbound requests for the same NPC to race; queue later sends behind the first
- complete the pending exchange only when a final routed, sanitized player-facing reply is accepted
- retries, history fallback, and no-response fallback must all resolve the same pending exchange

Archive rules:
- do not archive rejected ambient bleed, malformed lines, or pre-sanitized raw responses
- archive only the final delivered player-facing line
- if no reply arrives, keep the player text as `player_only` rather than dropping it

### 3. Make transcript recall part of the actual continuity path
The archive must feed the places that currently create the "forgot after restart" symptom.

Update these behaviors to consult `ImportantMemories` and transcript recall before falling back to generic facts:
- first-interaction detection
- memory-based greeting generation
- low-information opener grounding
- prompt injection before Player2 send

Precedence:
1. active/broken/resolved important memories
2. transcript recall snippets
3. recent turns
4. generic facts
5. town memory

Stale transcript snippets must be suppressed when a linked important memory has a newer resolved state. Example: an old "I'll do it" transcript should not outrank a newer `promise=broken` or `promise=kept` memory.

### 4. Build a bounded local query engine
Do not inject raw transcript history. Query it.

Query pipeline:
- same-NPC search only
- detect high-signal intents such as `promise`, `secret`, `remember`, `last time`, `you said`, `between us`, `don't tell`
- score by:
  - keyword overlap
  - exact category-intent match
  - importance
  - recency
  - linked important-memory boost
  - active-status boost for unresolved promises
- return 1-3 short transcript snippets plus any top important memories

Indexing:
- store normalized keywords on each exchange and chunk header
- keep per-NPC transient postings rebuilt on load
- use a candidate cap before decompression
- decompress at most a small fixed number of warm chunks per query

Keyword strategy:
- whitespace-token keywords for normal Latin-script text
- preserve exact short phrases for explicit cues like `between us` and `don't tell`
- add character n-gram fallback for no-whitespace scripts so localized chats still retrieve reasonably

### 5. Use tiered retention and maintenance-only compression
Keep hot data raw, warm data compressed, and never compress on the critical chat path.

Retention:
- hot raw tier: keep last 14 in-game days or last 120 exchanges per NPC raw
- warm tier: roll older sessions into gzip-compressed chunks
- cold pruning: if caps are exceeded, prune oldest compressed chunks first after preserving summaries and linked important memories

Compression and pruning must run only at:
- `OnSaving`
- `OnDayStarted`
- optional explicit maintenance/debug commands

Never run chunk recompression during player chat send/receive.

Chunk design:
- uncompressed sidecar metadata remains queryable without decompression
- compressed payload stores full exchanges for recall and debugging
- unchanged chunks should not be recompressed repeatedly

Guardrails:
- per-NPC compressed cap
- total archive compressed cap
- preserve important-memory summaries indefinitely even when old transcript chunks are pruned

### 6. Protect privacy and debug surfaces
Private memories must stay private across all retrieval surfaces.

Rules:
- `private` secrets are only queryable for the owning NPC
- they never appear in `TOWN_MEMORY`, rumor blocks, or other NPC prompt blocks
- transcript recall must not surface private content for another NPC, even if keywords overlap
- archive/debug commands must redact private content by default unless explicitly scoped to the owning NPC in a developer-only command

### 7. Save compatibility and diagnostics
Add a new save version and normalization path.

Migration:
- initialize empty transcript archive for old saves
- do not attempt full transcript reconstruction from old `RecentTurns`
- keep existing `NpcMemory` and `Facts` intact
- optionally promote obvious old high-signal facts into `ImportantMemories` only when category and meaning are clear

Diagnostics:
- extend `slrpg_memory_debug <npc>` to show:
  - important memory count
  - raw exchange count
  - compressed chunk count
  - oldest/newest transcript day
- add a developer-only transcript debug command for one NPC with redaction-safe output

## Public Interfaces / Types
Add:
- `SaveState.TranscriptArchive`
- `NpcTranscriptArchiveService`
- `TranscriptArchiveState`
- `NpcTranscriptArchive`
- `TranscriptSession`
- `TranscriptExchange`
- `TranscriptChunkHeader`
- `TranscriptQueryResult`

Add methods:
- `BeginPendingExchange(...)`
- `CompletePendingExchange(...)`
- `FinalizeTimedOutExchange(...)`
- `UpsertImportantMemory(...)`
- `ResolveImportantMemoryStatus(...)`
- `QueryRelevantTranscriptSnippets(...)`
- `BuildTranscriptRecallBlock(...)`
- `RollWarmChunks(...)`
- `PruneArchiveIfNeeded(...)`
- `RebuildTransientIndexes(...)`

## Test Plan
- Save/reload continuity:
  - talk to an NPC, restart, open chat again, and verify greeting/grounding reflect prior chat
- Promise lifecycle:
  - promise something, restart, ask about it, then complete/fail the linked quest and verify memory status updates correctly
- Secret privacy:
  - share a secret with NPC A, restart, verify A recalls it and NPC B does not
- Retry/fallback correctness:
  - force ambient-bleed retry and history fallback paths and verify the archive records only one final resolved exchange
- Pending-send serialization:
  - send multiple messages quickly to the same NPC and verify requests resolve in order without reply misattachment
- Compression:
  - age data into warm chunks and verify transcript recall still works with limited decompression
- Pruning:
  - exceed per-NPC and global caps and verify summaries and important memories survive
- Localization retrieval:
  - verify keyword recall still works for non-whitespace or localized text via fallback indexing
- Regression:
  - existing `RecentTurns`, town memory, and normal Player2 chat remain functional

## Assumptions
- Scope is player-to-NPC chats only
- No player-facing transcript browser is included
- The mod, not Player2, performs all archive queries
- One unresolved player chat per NPC is serialized at runtime for archive correctness
- Compression uses gzip chunk payloads stored in save data, with queryable metadata kept uncompressed
