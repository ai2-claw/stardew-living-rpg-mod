# Feature Plan: Vanilla NPC Canon Lore Guardrails (v001)

## Overview
Add a first-party vanilla canon lore dataset using the same shape as `custom_npc_pack_template/content/lore.json`, then inject it into Player2 prompt construction for vanilla NPC chats. The goal is to reduce lore hallucinations (for example, Pierre denying Abigail as his daughter) while allowing intentional character tuning (for example, Elliott as a writer and editor of `The Valley Times`).

Assumptions:
- Scope is prompt-grounding and validation only (no gameplay/system state mutation).
- The existing custom-NPC lore pipeline remains intact and additive.
- Canon guardrails should be strong by default but configurable for advanced users.

## Goals
- Add a structured vanilla lore source with the same schema style as `content/lore.json`.
- Inject vanilla lore into Player2 prompts for the active speaker and referenced vanilla NPCs.
- Prevent high-impact canon contradictions using explicit boundary and forbidden-claim rules.
- Support selective character enrichment where intended by the mod author.
- Provide debug visibility to inspect exactly what vanilla lore is loaded and injected.

## Non-Goals
- Replacing Stardew vanilla dialogue or heart events.
- Building a full knowledge graph or retrieval database.
- Refactoring the entire custom NPC framework in this iteration.
- Adding new mutation commands or changing resolver safety boundaries.

## Current State
- Prompt construction already includes broad canon rules (`CANON_WORLD`, `CANON_TOWN`, `CANON_NPCS`) in `ModEntry.cs`.
- Custom NPC lore injection exists via `_customNpcRegistry.BuildLorePromptBlock(...)` and `BuildReferencedNpcLorePromptBlock(...)`.
- Custom lore packs validate against canon baselines and forbidden patterns via `NpcPackLoader` and `CanonBaselineService`.
- There is no equivalent first-party vanilla lore registry or vanilla-specific prompt block.
- Result: vanilla NPC replies can still drift into relationship and role hallucinations.

## Proposed Architecture
1. Vanilla lore data source
- Add `mod/StardewLivingRPG/assets/vanilla-canon-lore.json`.
- Use the same top-level structure as `NpcLoreFile` (`Npcs`, optional `Locations`) and `NpcLoreEntry` fields (`Role`, `Persona`, `Speech`, `Ties`, `Boundaries`, `TimelineAnchors`, `KnownLocations`, `TiesToNpcs`, `ForbiddenClaims`).
- Seed with high-risk/high-traffic NPCs first (Pierre, Abigail, Caroline, Elliott, Lewis, Robin), then expand.

2. Loader and validation layer
- Add a lightweight `VanillaCanonLoreService` (or equivalent) that:
- loads + normalizes tokens from `assets/vanilla-canon-lore.json`
- validates NPC keys against `CanonBaselineService.CanonNpcTokens`
- validates `KnownLocations` and `TimelineAnchors` against baseline allowlists
- applies style and minimum-content checks similar to current custom lore validation
- supports optional locale overlay file(s), for example `i18n/vanilla-canon-lore.<locale>.json`

3. Prompt injection path
- In `ModEntry` prompt assembly, inject vanilla lore block before custom lore block:
- active speaker block: `VANILLA_NPC_LORE[...]`
- referenced NPC block(s): `VANILLA_NPC_REFERENCE_LORE[...]` (cap to small count, for example 2)
- Add explicit anti-contradiction rule:
- do not contradict `ForbiddenClaims`
- do not negate direct relationship facts expressed in lore ties/boundaries
- if uncertain, remain partial but non-contradictory

4. Config and diagnostics
- Add config toggles:
- `EnableVanillaCanonLoreInjection` (default `true`)
- `LogVanillaCanonLoreInjectionPreview` (default `false`)
- Add debug commands:
- `slrpg_vanilla_lore_validate`
- `slrpg_vanilla_lore_dump <npc>`

5. Layering behavior
- Vanilla lore is baseline.
- Custom NPC lore remains separate and additive.
- If both systems provide guidance for a referenced NPC, keep deterministic precedence:
- active speaker lore first
- referenced lore second
- custom lore remains constrained to custom NPC identities only

## Changes Needed
- `mod/StardewLivingRPG/assets/vanilla-canon-lore.json` (new)
- `mod/StardewLivingRPG/Config/ModConfig.cs` (new config toggles)
- `mod/StardewLivingRPG/ModEntry.cs` (loader wiring, prompt injection, debug commands)
- `mod/StardewLivingRPG/CustomNpcFramework/Models/*` (reuse existing models where possible)
- `mod/StardewLivingRPG/CustomNpcFramework/Services/CanonBaselineService.cs` (reuse validation baselines)
- `mod/StardewLivingRPG/CustomNpcFramework/Services/*` (new vanilla lore service/registry)
- `mod/StardewLivingRPG/README.md` (document config + commands)
- `CHANGELOG.md` (feature entry)

## Tasks (numbered)
1. Define authoritative seed NPC set and write acceptance criteria for contradiction prevention.
2. Add `assets/vanilla-canon-lore.json` scaffold using the same schema style as custom lore.
3. Populate seed entries with high-value canon constraints (including Pierre/Abigail and Elliott/newspaper role).
4. Implement `VanillaCanonLoreService` to load, normalize, and query lore by NPC name/token.
5. Implement validation pass against canonical NPC/location/timeline baselines.
6. Add style warnings and required-field checks mirroring custom lore quality rules.
7. Add optional locale overlay merge support for vanilla lore.
8. Wire service initialization into `ModEntry` lifecycle.
9. Inject active-speaker vanilla lore block into prompt assembly.
10. Inject referenced-vanilla-NPC lore block into prompt assembly with low cap (for example, 2).
11. Add anti-contradiction prompt rules tied to `ForbiddenClaims` and boundaries.
12. Add config toggles for enable/log behavior.
13. Add debug commands for validate and dump output.
14. Add targeted regression checks for known hallucination prompts (Pierre-Abigail, Elliott-editor, other seed cases).
15. Update docs and changelog; run build + regression command set.

## Dependencies
- Stable canonical baseline data in `CanonBaselineService`.
- Existing prompt build flow in `ModEntry` remains the integration point.
- Existing lore model classes (`NpcLoreFile`, `NpcLoreEntry`) remain suitable for reuse.
- Maintainer-provided canonical seed text quality and coverage.

## Risks
- Over-constraining prompts may make dialogue feel rigid or repetitive.
- Under-specified lore entries may still allow soft hallucinations.
- Lore drift between mod updates and Stardew canon can create stale constraints.
- Ambiguous alias resolution may attach lore to the wrong referenced NPC.
- Large lore blocks can increase token pressure and reduce response quality if not capped.
