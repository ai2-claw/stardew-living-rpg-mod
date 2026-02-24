# Plan: Newspaper Menu Scroll v001

## Overview
Add vertical scrolling to `NewspaperMenu` using the same interaction model as `NpcChatInputMenu` (mouse wheel, draggable thumb, track click paging, scissor clipping), so long newspaper content stays within the paper container and no text overflows outside the menu.

## Current State
- `NewspaperMenu` draws all sections and articles in a single forward draw pass (`DrawContent` -> `DrawArticles` -> `DrawArticle`).
- There is no clipping region for content text, so long issues overflow beyond the paper area.
- There is no scroll offset, no scrollbar state, and no scroll input handlers (`receiveScrollWheelAction`, drag, or page click).
- Layout currently uses fixed paper margins and masthead height, with all body text sharing one large width.

## Changes Needed
- Add content viewport and scrollbar rectangles to `NewspaperMenu` layout.
- Add scroll state (offset, content height, thumb rect, drag state).
- Refactor content rendering so total content height can be measured independently from drawing.
- Draw content through a scissor-clipped viewport using scroll offset.
- Add wheel, track-click, and thumb-drag input handling.
- Draw a visual scrollbar track/thumb consistent with existing NPC chat menu styling.
- Clamp and maintain scroll state across redraw/resize.

## Tasks (numbered)
1. - [x] Define scroll layout rectangles in `NewspaperMenu`:
   - content viewport (paper body region below masthead)
   - scrollbar track and thumb gutter
   - content text width adjusted to avoid overlap with scrollbar.
2. - [x] Add scroll state fields and helpers:
   - `_contentScrollOffset`, `_contentHeight`, `_scrollTrackRegion`, `_scrollThumbRegion`, `_scrollThumbHeld`, `_scrollThumbDragOffset`
   - `CanScroll`, `ScrollBy`, `UpdateScrollThumbRegion`, and max-scroll clamp logic.
3. - [x] Refactor content rendering into measurement + draw flow:
   - implement a measure path that computes full content height
   - keep draw path deterministic and re-usable with a starting Y offset.
4. - [x] Add scissor-clipped drawing for newspaper body content:
   - set scissor to content viewport
   - render content using `y - _contentScrollOffset`
   - restore previous `SpriteBatch` and scissor state safely.
5. - [x] Implement scroll inputs:
   - mouse wheel scroll in viewport/track
   - thumb drag in `leftClickHeld`
   - track click page up/down in `receiveLeftClick`
   - drag release reset in `releaseLeftClick`.
6. - [x] Draw scrollbar visuals (track + thumb) after content draw, matching current in-game style conventions used by `NpcChatInputMenu`.
7. - [x] Handle menu resize and redraw consistency:
   - recalc regions on `gameWindowSizeChanged`
   - clamp offset if content shrinks
   - ensure close button interaction remains unchanged.
8. - [~] Validate behavior with long newspaper payloads:
   - long headline + multiple sections
   - many generated articles
   - no text overflow outside paper
   - smooth wheel/drag/track interactions.

## Dependencies
- `mod/StardewLivingRPG/UI/NewspaperMenu.cs`
- Existing scroll behavior reference: `mod/StardewLivingRPG/UI/NpcChatInputMenu.cs`
- `TextWrapHelper` for line wrapping consistency.
- MonoGame/SMAPI rendering path (`SpriteBatch`, rasterizer scissor clipping, input events).
