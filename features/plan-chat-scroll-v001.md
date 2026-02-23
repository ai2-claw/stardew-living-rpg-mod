# Feature Plan: Chat Scroll (v001)

## Overview
Add scroll functionality to the NPC chat menu so that longer NPC responses are contained within the chat region bounds rather than clipping outside the parchment container. Includes a visual scrollbar with click/drag interaction following Stardew Valley UI conventions.

## Goals
- Prevent NPC responses from rendering outside the chat region bounds
- Add visual scrollbar with thumb drag and track click interaction
- Support mouse wheel as secondary scroll input
- Preserve existing chat layout, styling, and interaction patterns
- Maintain cozy, legible UX with minimal visual friction

## Non-Goals
- Full conversation history with multiple turns (currently single exchange)
- Touch/gamepad scroll support

## Current State
- `NpcChatInputMenu.cs` renders conversation text via `DrawConversationText()`
- Text wrapping via `TextWrapHelper.WrapText()` produces line arrays
- All lines are drawn sequentially without bounds checking or clipping
- `_chatRegion` defines the visible area but is not used for clipping
- No scroll state exists; Y position increments until exhausted
- When lines exceed `_chatRegion.Height`, they render below parchment border

Key code locations:
- Layout: `RecalculateLayout()` defines `_chatRegion` (lines 166-171)
- Rendering: `DrawConversationText()` draws player + NPC lines (lines 445-496)

## Changes Needed

### 1. Add scroll state fields
```csharp
private int _chatScrollOffset = 0;      // pixels scrolled (0 = top)
private int _chatContentHeight = 0;     // total wrapped content height
```

### 2. Implement `receiveScrollWheelAction`
Override to handle mouse wheel input when cursor is over `_chatRegion`:
```csharp
public override void receiveScrollWheelAction(int direction)
{
    base.receiveScrollWheelAction(direction);
    if (_chatRegion.Contains(Game1.getMouseX(), Game1.getMouseY()))
    {
        int scrollAmount = Game1.smallFont.LineSpacing * 2;
        int maxScroll = Math.Max(0, _chatContentHeight - _chatRegion.Height);
        _chatScrollOffset = Math.Clamp(_chatScrollOffset - direction * scrollAmount, 0, maxScroll);
    }
}
```

### 3. Update `DrawConversationText` with clipping
- Calculate total content height before rendering
- Store in `_chatContentHeight` for scroll bounds
- Apply `_chatScrollOffset` to starting Y position
- Use `SpriteBatch.Begin` with `RasterizerState.ScissorTestEnable` and `GraphicsDevice.ScissorRectangle` for clipping

### 4. Reset scroll on new message
When `_lastNpcMessage` is updated in `update()`, reset scroll to bottom (show latest):
```csharp
_chatScrollOffset = Math.Max(0, _chatContentHeight - _chatRegion.Height);
```

### 5. Add visual scrollbar with click/drag interaction
Stardew Valley UI patterns favor clickable/draggable scrollbars over mouse wheel. Add:

**Layout**: Thin vertical scrollbar on right edge of `_chatRegion`
- Track: Full height of chat region
- Thumb: Proportional to visible/content ratio
- Up/Down arrow buttons at track ends (optional, vanilla style)

**Fields**:
```csharp
private Rectangle _scrollTrackRegion;
private Rectangle _scrollThumbRegion;
private bool _scrollThumbHeld = false;
private int _scrollThumbDragOffset = 0;
```

**Interaction**:
- Click on track above/below thumb: page up/down
- Drag thumb: direct scroll position
- Click arrow buttons (if added): scroll by one line
- Mouse wheel: still supported as secondary input

**Drawing**:
- Use `Game1.staminaRect` for track and thumb
- Thumb color: `Color.BurlyWood` or similar parchment-tone
- Track color: Semi-transparent dark line

## Tasks
1. ✓ Add `_chatScrollOffset` and `_chatContentHeight` fields to `NpcChatInputMenu`
2. ✓ Add scrollbar fields: `_scrollTrackRegion`, `_scrollThumbRegion`, `_scrollThumbHeld`, `_scrollThumbDragOffset`
3. ✓ Implement `receiveScrollWheelAction` override with region hit-test
4. ✓ Refactor `DrawConversationText` to calculate total content height first
5. ✓ Add scissor clipping to `DrawConversationText` using `_chatRegion` bounds
6. ✓ Apply scroll offset to Y positions in `DrawConversationText`
7. ✓ Calculate and position `_scrollTrackRegion` and `_scrollThumbRegion` in `RecalculateLayout`
8. ✓ Add `DrawScrollbar()` method with track and thumb rendering
9. ✓ Implement thumb drag in `receiveLeftClick` and `leftClickHeld`
10. ✓ Implement track click (page up/down) in `receiveLeftClick`
11. ✓ Reset scroll to bottom when new NPC message arrives
12. ✓ Build and verify in-game with long NPC response

## Dependencies
- Existing `TextWrapHelper.WrapText()` for line calculation
- `_chatRegion` rectangle already computed in `RecalculateLayout()`
- XNA `RasterizerState.ScissorTestEnable` for clipping

## Risks
- Scissor rectangle must be set within `SpriteBatch.Begin/End` scope
- Need to handle UI scale/viewport changes gracefully
- Scroll reset timing must avoid fighting with user scroll during streaming
- Thumb drag must account for UI scaling and viewport offset
- Click detection must not conflict with existing input region handling
