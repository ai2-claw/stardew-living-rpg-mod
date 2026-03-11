# Group System Evaluation

## Summary

The current repo does not implement a hard faction system. It implements a soft-interest layer that tracks shifting town priorities across four community groups:

- `farmers_circle`
- `shopkeepers_guild`
- `adventurers_club`
- `nature_keepers`

This system is real and wired into simulation, but it is only lightly surfaced to the player. Its strongest current benefit is that it makes the town feel more alive and internally coherent. Its weakest area is player legibility: the player can influence the world indirectly, but the game does not yet make group standing, momentum, or consequences visible enough to feel like a meaningful long-term progression system.

## 1. What Exists Today

The canonical design intent is explicit in `ARCHITECTURE.md`: community groups are modeled as soft interests, not hard faction warfare. The design rule is that interests should usually be bridgeable, with opportunities for multi-group wins. That matters because it defines the evaluation target. This system is not supposed to create hostile alignment lock-in or binary faction choice. It is supposed to create living social pressure inside Stardew's tone.

The persistent data model in `DATA_MODEL.md` places the system inside `SocialState`. Each interest has:

- `influence`
- `trust`
- `priorities`

The example state shows concrete priorities such as `stable_prices`, `seed_access`, `foot_traffic`, `margin_stability`, `biodiversity`, and `forest_health`. That means the system is not just naming groups for flavor. It is intended to hold directional social pressure and preference data that can shape downstream systems.

On the mutation side, the core command is `shift_interest_influence`. In `NpcIntentResolver.cs`, this command is schema-bounded, restricted to known interests, limited to `-5..5`, and applied deterministically to `state.Social.Interests[interest].Influence`. This confirms the system is not a loose storytelling tag. It is treated as authoritative game state.

## 2. Verified Purpose

The current purpose of the group system is to translate ambient town activity into bounded social momentum.

The clearest proof is the event-first flow in `AMBIENT_CONSEQUENCE_PIPELINE.md`:

1. ambient lines and town events are recorded
2. signal snapshots are built
3. auto hooks synthesize bounded intents
4. `shift_interest_influence` becomes one of the main outputs
5. downstream surfaces render the consequences

In `ModEntry.cs`, the automatic hook `TryRunAutoInterestShiftHooks()` reinforces this design. Recent events are summarized into repeated signals, mapped to one of the four interests, and then converted into a small automatic influence shift. The mapping is intentionally broad:

- market-heavy signals push `shopkeepers_guild`
- nature-heavy signals push `nature_keepers`
- incident or mine signals push `adventurers_club`
- social and harvest signals push `farmers_circle`

This means the system exists to answer a town-level question: what kind of concerns are currently gaining social weight in Pelican Town?

That is a valid purpose for this mod. It supports the stated product direction of "Stardew, but alive" by letting social priorities move in response to world conditions without abandoning cozy constraints.

## 3. How It Is Actually Used

### Simulation and safety

This is the layer where the system is strongest.

- `NPC_COMMAND_SCHEMA.json` constrains valid interest names.
- `NpcIntentResolver.cs` validates and applies bounded changes.
- `AMBIENT_CONSEQUENCE_PIPELINE.md` treats interest shifts as a first-class downstream consequence of ambient events.
- `ModEntry.cs` applies daily cadence, cooldown, and budget checks before auto-shifting influence.

The result is a safe, deterministic, replayable simulation signal rather than freeform AI narrative drift.

### Manual NPC interaction

The system is also reachable through player conversation, but in a narrow way.

In `ModEntry.cs`, the text router detects phrases like:

- `town groups`
- `who has influence`
- `town priorities`
- group names such as `shopkeepers guild` and `nature keepers`

Those prompts set the context tag to `manual_interest`.

From there, the prompt rules instruct NPCs to use `shift_interest_influence` only when the conversation clearly concerns town groups or priorities, and to defer or decline if evidence is weak. `NpcAskGateService.cs` also gives `manual_interest` a modest personality-aware acceptance path rather than making it universally available.

This confirms the feature is not only automatic background simulation. It is also a conversational inquiry path. The player can ask about group dynamics, but the output is still tightly constrained.

### Quest and reward linkage

The system is partially connected to quests.

`QUEST_TEMPLATE_LIBRARY.md` shows that quest rewards can include small interest influence changes, with examples such as:

- gather crop: gold + small reputation + interest influence
- mine resource: gold + adventurer influence
- community drive: multi-interest influence + anchor-event trigger chance

`DATA_MODEL.md` also includes `interest_influence` inside quest rewards.

This is important because it gives the group system a path toward player agency. The player's work can push town priorities. However, this linkage currently reads stronger in the design model than in player-facing explanation.

### Player-facing UI surfacing

This is the layer where the system is weakest.

The repo has UI menus for:

- market board
- newspaper
- rumor board
- NPC chat

But a search across `mod/StardewLivingRPG/UI` shows no explicit group or influence display. There is no visible standings board, no group ledger, and no dedicated "who matters right now" view.

The main direct player-facing confirmation I found is the generic HUD copy in `i18n/default.json` and `ModEntry.cs`:

- `Town groups shifted their focus.`

That line confirms the system changed, but it does not tell the player:

- which group changed
- why it changed
- whether the player caused it
- what practical effect follows

So the current player-facing expression is mostly indirect.

## 4. User-Facing Aspects That Enhance the Experience

The current enhancement comes from indirect texture, not explicit faction gameplay.

### What helps today

1. NPC conversations can acknowledge town-group dynamics.
The player can ask about who has influence or what town priorities are. That gives NPC dialogue more civic texture than pure small talk.

2. Requests, rumors, and market/news systems can share a common social logic.
Because interest shifts come from the same town-event pipeline as market and quest consequences, the world feels more joined-up. Shortages, requests, chatter, and local concerns can all point in the same direction.

3. Quest rewards can imply that actions matter beyond gold.
Even if the player does not see a formal standing meter, the idea that helping with crops or mine supply changes who has momentum in town adds narrative weight.

4. The design avoids anti-Stardew faction harshness.
The soft-interest model supports town texture without turning the game into political warfare, reputation lockouts, or a loyalty grind. That is aligned with the repo's cozy philosophy.

### What is missing today

1. Weak readability.
The player is not clearly shown which groups currently have influence, what they want, or how recent actions changed that.

2. Weak ownership.
The player can affect the system, but the game rarely frames outcomes as "because of what you did, this town tendency strengthened."

3. Weak long-term progression.
There is no visible multi-day arc of soft alignment. The player does not get a strong sense of building trusted standing with a group over a season.

4. Weak reward identity.
The current system supports simulation better than fantasy. "Shopkeepers' Guild" and "Farmers' Circle" sound like meaningful affiliations, but the player experience does not yet fully cash in that promise.

## 5. Benefit Assessment

### Living-world texture: Strong

This is the clearest win. The system gives the town a way to collectively lean toward trade, farming, safety, or nature in response to events. That supports the mod's core fantasy better than isolated quest generation would.

### Narrative immersion: Moderate to strong

The system helps NPC talk feel grounded in community concerns instead of random AI flavor. It creates the sense that the town has moods and priorities, not just individuals.

### Strategic depth: Moderate

There is some depth because quests, market conditions, and community concerns can align. But the player is not shown enough of the underlying state to make deliberate medium-term strategy around it.

### Player agency: Moderate

The model allows agency through quests and conversation, but the feedback loop is weak. The player can influence the system more than they can understand it.

### Long-term progression clarity: Weak

This is the biggest gap. The system tracks influence and trust, but the player does not receive a strong, persistent sense of progress with any group.

## 6. Gap Versus a Stronger "Farmer's Guild" Style Direction

If the design goal is a stronger but still cozy soft-alignment experience, the current system is only halfway there.

What exists now is a hidden or semi-hidden social simulation layer. What a stronger "Farmer's Guild" style experience would need is not hard membership. It would need legibility and continuity.

The main gaps are:

- clearer visibility of current group priorities
- clearer attribution of changes to player actions
- a stronger sense that repeated help for a group produces recognizable momentum
- downstream effects that feel distinct without becoming punitive

The current system already has the right foundation for this direction:

- persistent state
- bounded influence shifts
- quest reward linkage
- NPC inquiry hooks
- market/news/request downstream surfaces

So the missing piece is not architecture. It is expression.

## 7. Recommendations

These recommendations stay inside the repo's stated "soft interests, bridgeable outcomes" philosophy.

### 1. Make current priorities legible

Add a lightweight player-facing summary somewhere the player already looks, such as newspaper or board copy:

- which groups are gaining momentum
- what they currently care about
- a short in-world explanation of why

This should feel like town chatter, not a strategy spreadsheet.

### 2. Attribute change to action

When a request or community action affects a group, communicate it in natural language:

- the growers appreciated that delivery
- merchants are breathing easier after that restock
- the town is leaning harder toward forest preservation

This would strengthen ownership without needing explicit meters everywhere.

### 3. Build soft seasonal continuity

Let repeated helpful actions create a mild sense of standing or favor over time, but keep it non-exclusive.

The target should be:

- the player can become known as reliable to multiple groups
- one group can currently like the player's help more than another
- no hard lockout is required

### 4. Differentiate downstream flavor

If a group has rising influence, let that slightly bias:

- request themes
- rumor topics
- newspaper tone
- market recommendations

The player should be able to feel the town leaning in a direction even before seeing a number.

### 5. Use names carefully

If labels such as `Shopkeepers' Guild` and `Farmers' Circle` are kept, the player-facing game should deliver enough identity to justify those names. Otherwise, the terminology risks promising a stronger affiliation fantasy than the current UX delivers.

## Conclusion

The current group system is useful, real, and well-aligned with the mod's architecture. Its purpose is valid: it gives the town a soft, deterministic way to shift collective priorities in response to events and conversations. That already improves the game by making requests, rumors, markets, and NPC dialogue feel more connected.

The main limitation is not purpose or safety. It is visibility. Right now the system behaves more like internal world logic than a fully felt player system. If the project wants a stronger "Farmer's Guild" style payoff, the best next step is not hard factions. It is clearer player-facing expression of the soft-interest system that already exists.
