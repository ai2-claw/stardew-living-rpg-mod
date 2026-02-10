# DATA_MODEL.md

Authoritative state model for the Stardew Living RPG Mod.

Design goals:
- deterministic world simulation
- AI intent decoupled from world mutation
- cozy-safe bounds by default
- replay/debug friendliness

---

## 1) Top-Level Save State

```json
{
  "version": "0.1.0",
  "config": {
    "mode": "cozy_canon",
    "price_floor_pct": 0.80,
    "price_ceiling_pct": 1.40,
    "daily_price_delta_cap_pct": 0.10
  },
  "calendar": {
    "day": 1,
    "season": "spring",
    "year": 1
  },
  "economy": { "...": "see EconomyState" },
  "social": { "...": "see SocialState" },
  "quests": { "...": "see QuestState" },
  "facts": { "...": "see FactTable" },
  "newspaper": { "...": "see NewspaperState" },
  "telemetry": { "...": "see TelemetryState" }
}
```

---

## 2) EconomyState

```json
{
  "crops": {
    "blueberry": {
      "base_price": 50,
      "price_today": 52,
      "price_yesterday": 50,
      "rolling_sell_volume_7d": 320,
      "demand_factor": 1.04,
      "supply_pressure_factor": 0.96,
      "sentiment_factor": 1.00,
      "scarcity_bonus": 0.00,
      "trend_ema": 0.03,
      "flags": ["in_season"]
    }
  },
  "market_events": [
    {
      "id": "evt_market_001",
      "type": "festival_demand",
      "crop": "pumpkin",
      "delta_pct": 0.12,
      "start_day": 15,
      "end_day": 21
    }
  ]
}
```

Constraints (Cozy Canon):
- `price_today` clamp to `[base * floor_pct, base * ceiling_pct]`
- per-day delta cap (default 10%)
- oversupply penalties are diminishing, not linear runaway

---

## 3) SocialState

```json
{
  "interests": {
    "farmers_circle": {
      "influence": 55,
      "trust": 10,
      "priorities": ["stable_prices", "seed_access"]
    },
    "shopkeepers_guild": {
      "influence": 48,
      "trust": 5,
      "priorities": ["foot_traffic", "margin_stability"]
    },
    "nature_keepers": {
      "influence": 42,
      "trust": 0,
      "priorities": ["biodiversity", "forest_health"]
    }
  },
  "npc_reputation": {
    "lewis": 12,
    "pierre": 8,
    "linus": 15
  },
  "npc_relationships": {
    "lewis:pierre": { "stance": "aligned", "trust": 6 },
    "linus:wizard": { "stance": "neutral", "trust": 2 }
  },
  "town_sentiment": {
    "economy": 5,
    "community": 12,
    "environment": 3
  }
}
```

Ranges:
- trust/reputation/sentiment: `[-100, 100]`
- influence: `[0, 100]`

---

## 4) QuestState

```json
{
  "active": [
    {
      "quest_id": "quest_herb_supply_01",
      "template_id": "gather_crop",
      "status": "active",
      "source": "rumor_mill",
      "issuer": "lewis",
      "objective": {
        "type": "deliver_item",
        "item": "parsnip",
        "count": 20
      },
      "rewards": {
        "gold": 500,
        "interest_influence": { "farmers_circle": 2 },
        "reputation": { "lewis": 4 }
      },
      "expires_day": 9
    }
  ],
  "completed": [],
  "failed": []
}
```

Quest objectives/rewards must come from validated templates.
AI may provide context flavor; not structural objective logic.

---

## 5) FactTable (Memory Lock)

Single source of truth to prevent duplicate/contradictory AI outputs.

```json
{
  "facts": {
    "quest:quest_herb_supply_01:accepted": {
      "value": true,
      "set_day": 3,
      "ttl_days": null,
      "source": "system"
    },
    "npc:lewis:promised_discount": {
      "value": true,
      "set_day": 4,
      "ttl_days": 7,
      "source": "npc_command"
    }
  },
  "processed_intents": {
    "intent_7f5a8c": {
      "day": 4,
      "npc_id": "lewis",
      "command": "propose_quest",
      "status": "applied"
    }
  }
}
```

Rules:
- Every accepted/resolved quest writes a lock fact.
- Every applied command writes an idempotency key.
- Replayed intent IDs are ignored.

---

## 6) NewspaperState

```json
{
  "issues": [
    {
      "day": 5,
      "headline": "Blueberry Boom Crowds Out Cauliflower",
      "sections": [
        {
          "kind": "market",
          "text": "Blueberry prices softened 6%. Cauliflower demand expected to rise before the festival."
        },
        {
          "kind": "community",
          "text": "Shopkeepers request crop diversity incentives."
        }
      ],
      "predictive_hints": [
        "Festival in 3 days: pumpkins likely to rise"
      ]
    }
  ]
}
```

---

## 7) TelemetryState

```json
{
  "daily": {
    "market_board_opens": 3,
    "rumor_board_accepts": 2,
    "rumor_board_completions": 1,
    "anchor_events_triggered": 0,
    "world_mutations": 14
  }
}
```

---

## 8) Determinism & Migration

- All simulation-changing writes occur in daily tick transaction.
- NPC stream outputs are first-class intents, not direct state writes.
- Versioned migrations are required for save compatibility (`version` field).
