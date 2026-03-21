using StardewModdingAPI;
using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class ChickenRaceService
{
    private const int MinRacers = 4;
    private const int MaxRacers = 6;
    private const float MinOdds = 1.5f;
    private const float MaxOdds = 10.0f;
    private const float BaseSpeedFactor = 0.002f;

    private static readonly string[] NamePrefixes =
    {
        "Lucky", "Speedy", "Golden", "Swift", "Thunder",
        "Stormy", "Flash", "Blaze", "Fierce", "Mighty"
    };

    private static readonly string[] NameRoots =
    {
        "Cluck", "Pecker", "Nugget", "Wings", "Feathers",
        "Bawk", "Eggbert", "Henrietta", "Daisy", "Maple"
    };

    private static readonly string[] NameSuffixes =
    {
        " Jr.", " Sr.", " III", " the Great", " the Fast", ""
    };

    private readonly IMonitor _monitor;
    private readonly ModConfig _config;

    public ChickenRaceService(IMonitor monitor, ModConfig config)
    {
        _monitor = monitor;
        _config = config;
    }

    public void SyncForToday(SaveState state)
    {
        var today = Game1.dayOfMonth;
        var season = Game1.currentSeason ?? "spring";
        var year = Game1.year;

        var progress = state.MiniGames.ChickenRace;
        if (progress.LastRaceDay != today)
        {
            progress.RacesToday = 0;
            progress.LastRaceDay = today;
        }
    }

    public bool CanRaceToday(SaveState state)
    {
        SyncForToday(state);
        return state.MiniGames.ChickenRace.RacesToday < _config.MaxChickenRacesPerDay;
    }

    public int RacesRemainingToday(SaveState state)
    {
        SyncForToday(state);
        return Math.Max(0, _config.MaxChickenRacesPerDay - state.MiniGames.ChickenRace.RacesToday);
    }

    public RaceSession CreateNewRace(SaveState state, int raceNumber)
    {
        var day = Game1.dayOfMonth;
        var season = Game1.currentSeason ?? "spring";
        var year = Game1.year;

        var seed = ComputeRaceSeed(year, season, day, raceNumber);
        var random = new Random(seed);

        var racerCount = random.Next(MinRacers, MaxRacers + 1);
        var racers = new List<ChickenRacer>();

        for (var i = 0; i < racerCount; i++)
        {
            var racer = GenerateRacer(random, i);
            racers.Add(racer);
        }

        NormalizeOdds(racers);

        var session = new RaceSession
        {
            Racers = racers,
            RaceInProgress = false,
            RaceFinished = false,
            WinnerIndex = -1,
            RaceSeed = seed,
            RaceNumber = raceNumber,
            PlayerBetIndex = -1,
            BetAmount = 0
        };
        session.InitializePositions();
        return session;
    }

    public bool PlaceBet(RaceSession session, int chickenIndex, int amount, SaveState state)
    {
        if (session.RaceInProgress || session.RaceFinished)
            return false;

        if (chickenIndex < 0 || chickenIndex >= session.Racers.Count)
            return false;

        if (amount < _config.MinBetAmount || amount > _config.MaxBetAmount)
            return false;

        if (Game1.player.Money < amount)
            return false;

        Game1.player.Money -= amount;
        session.PlayerBetIndex = chickenIndex;
        session.BetAmount = amount;

        var progress = state.MiniGames.ChickenRace;
        progress.RacesEntered++;
        progress.TotalGoldLost += amount;

        return true;
    }

    public void StartRace(RaceSession session, SaveState state)
    {
        if (session.RaceInProgress || session.RaceFinished)
            return;

        session.RaceInProgress = true;
        session.RaceFinished = false;
        session.WinnerIndex = -1;
        session.InitializePositions();

        var progress = state.MiniGames.ChickenRace;
        progress.RacesToday++;
    }

    public bool UpdateRace(RaceSession session)
    {
        if (!session.RaceInProgress || session.RaceFinished)
            return false;

        var random = new Random(session.RaceSeed);
        for (var tick = 0; tick < 60; tick++)
            random.Next();

        for (var i = 0; i < session.Racers.Count; i++)
        {
            var racer = session.Racers[i];
            var position = session.Positions[i];

            if (position >= 1.0f)
                continue;

            var speed = racer.BaseSpeed;

            if (position > 0.6f)
                speed *= racer.Stamina;

            var fluctuation = ((float)random.NextDouble() - 0.5f) * 0.1f * racer.Luck;
            var movement = (speed + fluctuation) * BaseSpeedFactor;

            session.Positions[i] = Math.Min(1.0f, position + movement);
        }

        var winnerIndex = -1;
        for (var i = 0; i < session.Positions.Length; i++)
        {
            if (session.Positions[i] >= 1.0f)
            {
                if (winnerIndex < 0 || session.Positions[i] > session.Positions[winnerIndex])
                    winnerIndex = i;
            }
        }

        if (winnerIndex >= 0)
        {
            session.WinnerIndex = winnerIndex;
            session.RaceFinished = true;
            session.RaceInProgress = false;
            return true;
        }

        return false;
    }

    public long ClaimWinnings(RaceSession session, SaveState state)
    {
        if (!session.RaceFinished || session.PlayerBetIndex < 0)
            return 0;

        if (session.WinnerIndex != session.PlayerBetIndex)
            return 0;

        var racer = session.Racers[session.PlayerBetIndex];
        var payout = (int)(session.BetAmount * racer.Odds);

        Game1.player.Money += payout;

        var progress = state.MiniGames.ChickenRace;
        progress.RacesWon++;
        progress.TotalGoldWon += payout;

        var netWin = payout - session.BetAmount;
        if (netWin > 0)
            progress.TotalGoldLost -= session.BetAmount;

        session.PlayerBetIndex = -1;
        session.BetAmount = 0;

        return (long)payout;
    }

    public string BuildDebugSummary(SaveState state)
    {
        var progress = state.MiniGames.ChickenRace;
        var remaining = RacesRemainingToday(state);
        return $"ChickenRace | racesToday={progress.RacesToday}/{_config.MaxChickenRacesPerDay}, remaining={remaining}, " +
               $"won={progress.RacesWon}, entered={progress.RacesEntered}, " +
               $"goldWon={progress.TotalGoldWon}g, goldLost={progress.TotalGoldLost}g";
    }

    private ChickenRacer GenerateRacer(Random random, int index)
    {
        var prefix = NamePrefixes[random.Next(NamePrefixes.Length)];
        var root = NameRoots[random.Next(NameRoots.Length)];
        var suffix = NameSuffixes[random.Next(NameSuffixes.Length)];

        var name = $"{prefix} {root}{suffix}";

        var baseSpeed = 0.8f + (float)random.NextDouble() * 0.4f;
        var stamina = 0.7f + (float)random.NextDouble() * 0.6f;
        var luck = 0.9f + (float)random.NextDouble() * 0.2f;
        var colorVariant = random.Next(6);

        var racer = new ChickenRacer
        {
            Name = name,
            ColorVariant = colorVariant,
            BaseSpeed = baseSpeed,
            Stamina = stamina,
            Luck = luck,
            Odds = 2.0f
        };

        return racer;
    }

    private void NormalizeOdds(List<ChickenRacer> racers)
    {
        foreach (var racer in racers)
        {
            var score = racer.CalculateScore();
            var odds = 2.0f / score;
            racer.Odds = Math.Clamp(odds, MinOdds, MaxOdds);
        }
    }

    private static int ComputeRaceSeed(int year, string season, int day, int raceNumber)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + year;
            hash = hash * 31 + day;
            hash = hash * 31 + raceNumber;
            foreach (var ch in season)
                hash = hash * 31 + ch;
            return hash;
        }
    }
}
