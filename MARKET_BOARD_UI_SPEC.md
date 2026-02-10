# MARKET_BOARD_UI_SPEC.md

Related docs: [DOC_INDEX](./DOC_INDEX.md) · [ARCHITECTURE](./ARCHITECTURE.md) · [VERTICAL_SLICE_CHECKLIST](./VERTICAL_SLICE_CHECKLIST.md)

UI/UX specification for Pierre's Market Board (non-AI gameplay anchor).

## 1) Goals
- Make economy simulation legible in 5-10 seconds each morning
- Support planner play without spreadsheet overhead
- Preserve Stardew cozy readability (low cognitive load)

---

## 2) World Placement
- Primary board object: Pierre's Shop (near counter)
- Optional late unlock: craftable Farm Ledger object for home use

Interaction:
- `Use` opens Market Board UI
- hotkey closes back to game instantly

---

## 3) Screen Layout

## Header
- "Pelican Market Board"
- current day/season
- mode badge (Cozy Canon / Story Depth / Living Chaos)

## Crop List Panel (left)
Per row:
- crop icon + name
- `price_today`
- trend arrow (up/down/flat)
- 3-day sparkline
- volatility indicator (low/med/high)

## Detail Panel (right)
Selected crop shows:
- base price
- current multipliers:
  - demand factor
  - supply pressure factor
  - sentiment factor
- projected direction (1-3 day hint, not exact number)
- short reason tags (e.g., "Festival demand", "Oversupply", "Weather shortage")

## Footer
- "Tomorrow Outlook" (top 3 likely risers/fallers)
- button: "Read Daily Newspaper"

---

## 4) Data Binding

Required fields from EconomyState:
- `crops[*].price_today`
- `crops[*].price_yesterday`
- `crops[*].trend_ema`
- `crops[*].demand_factor`
- `crops[*].supply_pressure_factor`
- `crops[*].sentiment_factor`
- `crops[*].flags`

Derived UI values:
- trend arrow from `price_today - price_yesterday`
- volatility from absolute rolling delta
- outlook rank from bounded projected score

---

## 5) UX Behavior

Morning ritual target:
- open board -> identify 1-2 profitable crops -> close in <10s

Rules:
- no hidden interactions
- no pagination for v1 (top crops only + scroll)
- default sort: highest positive trend, then stable earners

Accessibility:
- color + icon dual coding for arrows
- simple text labels (avoid jargon)

---

## 6) Failure/Fallback States

- If economy unavailable: show last known prices + "stale data" badge
- If no deltas yet (new save): show "collecting market data" helper text
- If mode is Cozy Canon: display reassurance tooltip ("Price swings are intentionally gentle")

---

## 7) Telemetry Hooks

Track per day:
- board opens
- avg session duration on board
- crop rows expanded/clicked
- % days player opens board before selling

Success KPI:
- board opened on >=60% of in-game days by active users
