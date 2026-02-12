  Summary: Newspaper Article Sources

  Article Generation Hierarchy

  Daily Newspaper Build (OnDayStarted)
  │
  ├── Headline & Market Outlook  (Auto from EconomyService)
  ├── Predictive Hints           (Auto from EconomyService)
  │
  ├── Filler Articles (2)        (Auto from seasonal templates)
  │   └── Source: "The Pelican Times"
  │
  ├── Event Articles (0-2)        (Auto from TownMemory events)
  │   └── Source: "Town Report"
  │   └── Examples: Fainting in mines, achievements
  │
  └── Dynamic Articles (0-2)        (From Player2 NPCs)
      ├── publish_article → Source: NPC name
      └── publish_rumor → Source: NPC name

  Changes Made

  1. NewspaperService.GenerateFillerArticles() - 2 seasonal filler articles
  daily
    - 32 articles (8 per season) based on Stardew canon
    - Deterministic based on day number
  2. NewspaperService.GenerateEventArticles() - Converts town events to news
    - Reads from TownMemory.Events (yesterday's events)
    - Fainting in mines → "Rescue at Mines" article
    - Achievements → "Community Achievement" articles
    - Maximum 2 event articles per day
  3. Fixed publish_rumor (NpcIntentResolver.cs)
    - Now adds an article to today's issue (not a full issue)
    - Category: "social", expires in 3 days
    - Headline: "Rumor Mill"
  4. Article limits: 5 total per day
    - 2 filler (templates)
    - 2 event (town memory)
    - 1 rumor (NPC command)

  When Do Articles Appear?
  Source: Filler
  Timing: On day start (before newspaper opens)
  Example: "Pierre's Stock Alert"
  ────────────────────────────────────────
  Source: Events
  Timing: On day start (processes yesterday)
  Example: "Rescue at Mines"
  ────────────────────────────────────────
  Source: NPC Articles
  Timing: During conversations
  Example: "Lewis Announces Policy Change"
  ────────────────────────────────────────
  Source: Rumors
  Timing: During conversations
  Example: "Word on the street..."
  Events like fainting are captured in real-time via TryCaptureTownIncidents(),
  stored in town memory, and appear as news the next day.

-------------------------------------------------------------
 

 Fixed: No Overlaps, Real News Takes Priority

  New Article Generation Order

  BuildIssue (empty slate)
      ↓
  GenerateEventArticles (real news from yesterday)
      ↓ Adds: "Rescue at Mines", "Community Achievement"
      ↓
  FillRemainingSlots (filler only if needed)
      ↓ Calculates: 2 - current_article_count
      ↓ Adds: Filler articles to reach minimum 2
      ↓
  NPC/Rumor articles (during day)
      ↓ Can add up to 2 more

  How It Works Now
  ┌────────────────────┬───────────────┬─────────────┬─────────────────────────┐
  │      Scenario      │    Event      │   Filler    │         Result          │
  │                    │   Articles    │    Added    │                         │
  ├────────────────────┼───────────────┼─────────────┼─────────────────────────┤
  │ Quiet day          │ 0             │ 2           │ 2 filler articles       │
  ├────────────────────┼───────────────┼─────────────┼─────────────────────────┤
  │ Fainting in mines  │ 1             │ 1           │ 1 event + 1 filler      │
  ├────────────────────┼───────────────┼─────────────┼─────────────────────────┤
  │ Busy day (2        │ 2             │ 0           │ 2 events, no filler     │
  │ events)            │               │             │                         │
  ├────────────────────┼───────────────┼─────────────┼─────────────────────────┤
  │ + NPC conversation │ 2             │ 0           │ 2 events + 1 NPC        │
  │                    │               │             │ article                 │
  └────────────────────┴───────────────┴─────────────┴─────────────────────────┘
  Key Changes

  1. GenerateEventArticles() runs FIRST - Adds real news from town memory
  2. FillRemainingSlots() runs SECOND - Only adds filler to reach minimum 2
  articles
  3. No more overlap - Real news displaces filler, never competes with it

  Example Day With Fainting

  Day 5: Player faints in mines
    → Event recorded in TownMemory

  Day 6: Newspaper builds
    → GenerateEventArticles: Adds "Rescue at Mines" (1 article)
    → FillRemainingSlots: 2 - 1 = 1 filler article needed
    → Result: 1 real article + 1 seasonal article

  The newspaper now prioritizes actual in-game events over generic filler
  content.

--------------------------------   

   Newspaper Layout

  ┌─────────────────────────────────────┐
  │   THE PELICAN TIMES               │  <- Masthead
  │   Vol. 1 • Spring 5, Year 1      │
  ├─────────────────────────────────────┤
  │  HEADLINE                         │  <- Headline (1 item)
  │  "Parsnip Slips While Melon Climbs" │
  │                                   │
  │  Market: parsnip softened 15%...   │  <- Sections (market updates)
  │  Market: melon rose 12%...        │
  │                                   │
  │  Market Outlook:                   │  <- Predictive Hints
  │  - Supply outlook: parsnip...      │
  │  - Season watch: ...                │
  ├─────────────────────────────────────┤
  │  COMMUNITY NEWS                   │  <- Articles Section (0-4 items)
  │  ┌────────────┬──────────────┐     │
  │  │ [Portrait]  │ [Portrait]    │     │
  │  │ Community    │ Social        │     │  <- Each article has:
  │  │ ...          │ ...           │     │  - Portrait (if NPC)
  │  │ Title        │ Title         │     │  - Category badge
  │  │ Content...   │ Content...     │     │  - Title + Content
  │  └─────────────┴──────────────┘     │
  └─────────────────────────────────────┘

  Article Count

  With current implementation:
  ┌─────────────────┬─────┬─────┬───────────────────────────────────┐
  │     Source      │ Min │ Max │               Notes               │
  ├─────────────────┼─────┼─────┼───────────────────────────────────┤
  │ Baseline filler │ 2   │ 2   │ FillRemainingSlots ensures min 2  │
  ├─────────────────┼─────┼─────┼───────────────────────────────────┤
  │ Event articles  │ 0   │ 2   │ From town memory (fainting, etc.) │
  ├─────────────────┼─────┼─────┼───────────────────────────────────┤
  │ NPC articles    │ 0   │ 2   │ From Player2 conversations        │
  ├─────────────────┼─────┼─────┼───────────────────────────────────┤
  │ Rumors          │ 0   │ 1   │ From Player2, marked "social"     │
  └─────────────────┴─────┴─────┴───────────────────────────────────┘
  Total Articles: 2-5 items minimum 2, maximum 5

  The headline is separate from Articles - it's the main story based on market
  movements. Articles are the "Community News" section with portraits.

  Is this what you expected, or should the structure be different?

> Headline should not only about market movments, this is not a market-only
   news paper. Pick headline from events and rumors.

______________________________________________________________________________________________
  
  Final: Headline from Most Important News

  Headline Selection Priority (SelectHeadline)

  Priority 1: High-severity incidents
    → "Rescue at Mines"
    → "Storm Damage Reported"

  Priority 2: Market/nature articles
    → "Pierre's Stock Alert"
    → "Salmon Run Begins"

  Priority 3: Any article
    → First available article

  Fallback (no articles):
    → "Quiet Day at Pelican Town"

  Complete Flow

  BuildIssue()
    → Creates empty issue with "Quiet Day" placeholder headline
    ↓
  GenerateEventArticles()
    → Adds 0-2 event articles (fainting, achievements)
    ↓
  FillRemainingSlots()
    → Adds 0-2 filler articles to reach minimum 2
    ↓
  SelectHeadline()
    → Picks most important article as headline
    → If "Rescue at Mines" exists → that's the headline
    → Otherwise, first available article becomes headline
    ↓
  Issue added to state

  Example Outcomes
  ┌─────────────────────┬────────┬──────────────┬────────────────────────┐
  │         Day         │ Events │ Filler Added │        Headline        │
  ├─────────────────────┼────────┼──────────────┼────────────────────────┤
  │ Quiet day           │ 0      │ 2            │ First seasonal article │
  ├─────────────────────┼────────┼──────────────┼────────────────────────┤
  │ Fainting only       │ 1      │ 1            │ "Rescue at Mines"      │
  ├─────────────────────┼────────┼──────────────┼────────────────────────┤
  │ Busy day (2 events) │ 2      │ 0            │ First event article    │
  └─────────────────────┴────────┴──────────────┴────────────────────────┘
  The newspaper now prioritizes real events and notable news over generic market
   movements for the headline.

-------------------------------------------------------------

  Sensationalized Headline System

  The headline is now tabloid-style exaggerated, different from article titles:
  Article Content: Fainting in mines
  Headline Generated: "DRAMA IN THE DEPTHS: FARMER COLLAPSES IN MINE SHAFT!"
  ────────────────────────────────────────
  Article Content: Rescue operation
  Headline Generated: "DARING RESCUE: LOCAL FARMER PULLED FROM DARKNESS!"
  ────────────────────────────────────────
  Article Content: Salmon run begins
  Headline Generated: "SILVER RUSH: RIVERS TEEMING WITH MIGRATING SALMON!"
  ────────────────────────────────────────
  Article Content: Seed shortage
  Headline Generated: "SEED CRISIS: PIERRE RATIONS RARE STOCKS!"
  ────────────────────────────────────────
  Article Content: Price changes
  Headline Generated: "MARKET SHOCK: PRICES SEND TRADERS INTO FRENZY!"
  ────────────────────────────────────────
  Article Content: Festival
  Headline Generated: "FAVORITE FESTIVAL RETURNS: VENDORS PREPARE FOR INFLUX!"
  ────────────────────────────────────────
  Article Content: Library event
  Headline Generated: "BOOK FRENZY: RESIDENTS RUSHING FOR KNOWLEDGE!"
  ────────────────────────────────────────
  Article Content: Generic filler
  Headline Generated: "SHOCKING: [TITLE]!" or "ALARMING: LOCAL DEVELOPMENTS!"
  Code Structure

  SelectHeadline()
    → Picks most important article (rescues > incidents > market > any)
    ↓
  GenerateSensationalHeadline()
    → Category-specific patterns (mining, fishing, farming, social)
    → Generic fallback with rotating sensational prefixes

  The headline now has that small-town tabloid feel - exaggerated and exciting,
  while article titles remain factual and understated.
  
  --------------------------
  
   To clarify the current system:

  Article Sources:
  Source: Filler (2/day)
  When: Always
  How: Static seasonal templates (we added)
  ────────────────────────────────────────
  Source: Events (0-2/day)
  When: Day start
  How: From town memory (fainting, etc.)
  ────────────────────────────────────────
  Source: NPC Articles (0-2/day)
  When: NPC conversations
  How: Player2 publish_article command
  ────────────────────────────────────────
  Source: Rumors (0-1/day)
  When: NPC conversations
  How: Player2 publish_rumor command
  Player2 does NOT auto-generate filler articles. Filler articles are hardcoded
  templates.

  If you want Player2 to generate articles automatically (via an API call), that
   would require a different implementation. Should we:
  1. Keep the current static filler templates (faster, no Player2 dependency),
  OR
  2. Add a Player2 API call to generate dynamic filler articles each day?

✻ Baked for 1m 56s

> add a Player2 API call to genrate dynamic filler aritcles each day to
  supplement the hardcoded fillers to avoid repeating articles and
  headlines. Generated articles must follow current story progression
  (estimated on the day and year)

● I'll implement a Player2 API call to generate dynamic filler articles. The
  approach: create a dedicated newspaper editor NPC that we call during issue
  generation.

