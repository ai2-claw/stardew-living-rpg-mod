# Plan: NpcChatInputMenu UI Redesign

Fix text wrapping and styling for the NPC chat input menu.

## Overview

The `NpcChatInputMenu` UI has hardcoded text truncation at 100 characters (line 127) with no proper word-wrapping, despite a `TextWrapHelper` utility being used in other menus (`MarketBoardMenu`, `NewspaperMenu`). The UI needs a full redesign using the `/frontend-design` skill to create a polished, Stardew-native chat interface.

## Current State

**File**: `mod/StardewLivingRPG/UI/NpcChatInputMenu.cs` (182 lines)

**Current Issues**:
1. **No word-wrapping**: Line 127 uses crude truncation `line[..100] + "..."`
2. **Inconsistent styling**: Doesn't use `TextWrapHelper` like other menus
3. **Basic button styling**: `DrawButton` uses simple color fills without Stardew-style borders
4. **Fixed layout**: Hardcoded positions don't adapt to content
5. **No visual distinction**: Player and NPC messages use same styling
6. **Thinking indicator**: Basic dots animation, could be more polished

**Existing Infrastructure**:
- `TextWrapHelper.WrapText()` - Already used in MarketBoardMenu.cs:45 and NewspaperMenu.cs:43,52
- `Game1.drawDialogueBox()` - Standard Stardew dialog background
- `Game1.dialogueFont`, `Game1.smallFont` - Consistent typography

## Changes Needed

1. Use `/frontend-design` skill to create new chat UI design spec
2. Implement proper word-wrapping using existing `TextWrapHelper`
3. Add visual distinction between player and NPC messages
4. Improve button styling to match Stardew conventions
5. Add scrollable message history (current: 10 lines max, last 4 shown)
6. Polish the thinking indicator animation

## Plan

1. ✓ Design new chat UI using /frontend-design skill
2. ✓ Implement word-wrapping with TextWrapHelper
3. ✓ Add message type styling (player vs NPC distinction)
4. ✓ Improve button styling with proper borders/hover states
5. ✓ Add scrollable message history
6. ✗ Test in-game with various message lengths

## Dependencies

- `Utils/TextWrapHelper.cs` - Already exists, use as-is
- `ModEntry.cs` - Integration point for opening the menu
- SMAPI `IClickableMenu` - Base class interface
- XNA/MonoGame `SpriteBatch`, `SpriteFont` - Rendering primitives
