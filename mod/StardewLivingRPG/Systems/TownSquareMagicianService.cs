using System.Globalization;
using StardewModdingAPI;
using StardewLivingRPG.State;
using StardewLivingRPG.Utils;

namespace StardewLivingRPG.Systems;

public sealed class TownSquareMagicianService
{
    public const string MagicianNpcName = "Morrow";

    private readonly IMonitor _monitor;
    private readonly List<TownSquareMagicianRoundDefinition> _rounds;

    public TownSquareMagicianService(IModHelper helper, IMonitor monitor)
    {
        _monitor = monitor;
        _rounds = LoadCatalog(helper);
    }

    public bool IsMagicianNpc(string? rawName)
    {
        return !string.IsNullOrWhiteSpace(rawName)
            && rawName.Trim().Equals(MagicianNpcName, StringComparison.OrdinalIgnoreCase);
    }

    public void SyncForToday(SaveState state)
    {
        _ = EnsureRoundForToday(state, resetProgressIfRoundChanged: true);
    }

    public TownSquareMagicianRoundView? PeekCurrentRound(SaveState state)
    {
        var round = EnsureRoundForToday(state, resetProgressIfRoundChanged: true);
        return round is null ? null : BuildRoundView(state.MiniGames.TownSquareMagician, round);
    }

    public TownSquareMagicianRoundView? BeginRoundSession(SaveState state)
    {
        var round = PrepareRoundForSession(state);
        if (round is null)
            return null;

        var progress = state.MiniGames.TownSquareMagician;
        progress.SessionsStartedToday += 1;
        state.Telemetry.Daily.TownSquareMagicianSessions += 1;
        return BuildRoundView(progress, round);
    }

    public TownSquareMagicianGuessResult SubmitGuess(SaveState state, string? rawGuess)
    {
        var round = GetActiveRound(state);
        if (round is null)
        {
            return new TownSquareMagicianGuessResult
            {
                Accepted = false,
                Feedback = I18n.Get("magician.feedback.unavailable", "No riddle is ready today.")
            };
        }

        var progress = state.MiniGames.TownSquareMagician;
        var normalizedGuess = (rawGuess ?? string.Empty).Trim();
        if (progress.AttemptsUsed >= round.MaxAttempts)
        {
            progress.LastOutcome = "lost";
            progress.LastFeedback = I18n.Get("magician.feedback.session_locked", "That round is spent. Ask again if you want another go.");
            return BuildResult(progress, round, false, false, false, true, 0, progress.LastFeedback);
        }

        if (string.Equals(normalizedGuess, "hint", StringComparison.OrdinalIgnoreCase))
            return SubmitHintRequest(progress, round);

        if (string.Equals(round.Mode, "guess_number", StringComparison.OrdinalIgnoreCase))
            return SubmitNumberGuess(state, progress, round, rawGuess);

        return SubmitWordGuess(state, progress, round, rawGuess);
    }

    public void ResetTodayProgress(SaveState state)
    {
        var round = EnsureRoundForToday(state, resetProgressIfRoundChanged: true);
        if (round is null)
            return;

        var progress = state.MiniGames.TownSquareMagician;
        progress.FeaturedRoundId = round.Id;
        progress.RoundId = round.Id;
        progress.RoundMode = round.Mode;
        progress.AttemptsUsed = 0;
        progress.HintsUsed = 0;
        progress.SolvedToday = false;
        progress.RewardClaimedToday = false;
        progress.LastOutcome = "fresh";
        progress.LastFeedback = string.Empty;
        progress.SessionsStartedToday = 0;
        progress.PlayedRoundIdsToday.Clear();
    }

    public string BuildDebugSummary(SaveState state)
    {
        var round = EnsureRoundForToday(state, resetProgressIfRoundChanged: true);
        if (round is null)
            return "Magician | no round loaded";

        var progress = state.MiniGames.TownSquareMagician;
        return $"Magician | npc={MagicianNpcName}, featured={progress.FeaturedRoundId}, active={round.Id}, mode={round.Mode}, attempts={progress.AttemptsUsed}/{round.MaxAttempts}, solved={progress.SolvedToday}, rewardClaimed={progress.RewardClaimedToday}, lastOutcome={progress.LastOutcome}";
    }

    private TownSquareMagicianGuessResult SubmitNumberGuess(
        SaveState state,
        TownSquareMagicianState progress,
        TownSquareMagicianRoundDefinition round,
        string? rawGuess)
    {
        var guessText = (rawGuess ?? string.Empty).Trim();
        if (!int.TryParse(guessText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var guessedNumber))
        {
            return BuildResult(
                progress,
                round,
                accepted: false,
                solved: false,
                rewardGranted: false,
                sessionEnded: false,
                rewardGoldGranted: 0,
                feedback: I18n.Get("magician.feedback.invalid_number", "Speak a whole number, not smoke."));
        }

        if (guessedNumber < round.MinNumber || guessedNumber > round.MaxNumber)
        {
            return BuildResult(
                progress,
                round,
                accepted: false,
                solved: false,
                rewardGranted: false,
                sessionEnded: false,
                rewardGoldGranted: 0,
                feedback: I18n.Get(
                    "magician.feedback.out_of_range",
                    $"Keep it between {round.MinNumber} and {round.MaxNumber}.",
                    new { min = round.MinNumber, max = round.MaxNumber }));
        }

        progress.AttemptsUsed += 1;

        var answerNumber = int.TryParse(round.Answer, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedAnswer)
            ? parsedAnswer
            : round.MinNumber;

        if (guessedNumber == answerNumber)
            return ResolveWin(state, progress, round);

        var clue = GetClueForAttempt(round, progress.AttemptsUsed - 1);
        var directionKey = guessedNumber < answerNumber ? "magician.feedback.number.higher" : "magician.feedback.number.lower";
        var fallback = guessedNumber < answerNumber ? "Higher." : "Lower.";

        if (progress.AttemptsUsed >= round.MaxAttempts)
        {
            var answerText = I18n.Get(
                "magician.feedback.loss_number",
                $"The number was {answerNumber}. Come back if you want another crack at it.",
                new { answer = answerNumber });
            MarkRoundPlayed(progress, round.Id);
            progress.LastOutcome = "lost";
            progress.LastFeedback = answerText;
            return BuildResult(progress, round, true, false, false, true, 0, answerText);
        }

        var hintText = string.IsNullOrWhiteSpace(clue)
            ? I18n.Get(directionKey, fallback)
            : I18n.Get(
                "magician.feedback.number.with_clue",
                $"{I18n.Get(directionKey, fallback)} {clue}",
                new
                {
                    direction = I18n.Get(directionKey, fallback),
                    clue
                });

        progress.LastOutcome = "in_progress";
        progress.LastFeedback = hintText;
        return BuildResult(progress, round, true, false, false, false, 0, hintText);
    }

    private TownSquareMagicianGuessResult SubmitHintRequest(
        TownSquareMagicianState progress,
        TownSquareMagicianRoundDefinition round)
    {
        var clueIndex = progress.AttemptsUsed + progress.HintsUsed;
        if (round.Clues.Count == 0 || clueIndex >= round.Clues.Count)
        {
            var noHintText = I18n.Get("magician.feedback.hint.none", "That is all the free smoke I can spare.");
            progress.LastOutcome = progress.AttemptsUsed > 0 ? "in_progress" : "fresh";
            progress.LastFeedback = noHintText;
            return BuildResult(progress, round, true, false, false, false, 0, noHintText);
        }

        var clue = GetClueForAttempt(round, clueIndex);
        progress.HintsUsed += 1;
        progress.LastOutcome = progress.AttemptsUsed > 0 || progress.HintsUsed > 0 ? "in_progress" : "fresh";
        progress.LastFeedback = I18n.Get("magician.feedback.hint.clue", $"Hint: {clue}", new { clue });
        return BuildResult(progress, round, true, false, false, false, 0, progress.LastFeedback);
    }

    private TownSquareMagicianGuessResult SubmitWordGuess(
        SaveState state,
        TownSquareMagicianState progress,
        TownSquareMagicianRoundDefinition round,
        string? rawGuess)
    {
        var guess = NormalizeWordGuess(rawGuess);
        if (string.IsNullOrWhiteSpace(guess))
        {
            return BuildResult(
                progress,
                round,
                accepted: false,
                solved: false,
                rewardGranted: false,
                sessionEnded: false,
                rewardGoldGranted: 0,
                feedback: I18n.Get("magician.feedback.invalid_word", "Give me a word, plain and clear."));
        }

        progress.AttemptsUsed += 1;
        if (string.Equals(guess, NormalizeWordGuess(round.Answer), StringComparison.Ordinal))
            return ResolveWin(state, progress, round);

        if (progress.AttemptsUsed >= round.MaxAttempts)
        {
            var answerText = I18n.Get(
                "magician.feedback.loss_word",
                $"The word was {round.Answer}. Ask again if you want another try.",
                new { answer = round.Answer });
            MarkRoundPlayed(progress, round.Id);
            progress.LastOutcome = "lost";
            progress.LastFeedback = answerText;
            return BuildResult(progress, round, true, false, false, true, 0, answerText);
        }

        var clue = GetClueForAttempt(round, progress.AttemptsUsed - 1);
        var hintText = string.IsNullOrWhiteSpace(clue)
            ? I18n.Get("magician.feedback.word.retry", "Not that one. Listen close and try again.")
            : I18n.Get("magician.feedback.word.clue", $"Not quite. Clue: {clue}", new { clue });

        progress.LastOutcome = "in_progress";
        progress.LastFeedback = hintText;
        return BuildResult(progress, round, true, false, false, false, 0, hintText);
    }

    private TownSquareMagicianGuessResult ResolveWin(
        SaveState state,
        TownSquareMagicianState progress,
        TownSquareMagicianRoundDefinition round)
    {
        var rewardGranted = !progress.RewardClaimedToday && round.RewardGold > 0;
        if (rewardGranted)
        {
            progress.RewardClaimedToday = true;
            progress.LifetimeRewardClaims += 1;
            state.Telemetry.Daily.TownSquareMagicianRewardClaims += 1;
        }

        MarkRoundPlayed(progress, round.Id);
        progress.SolvedToday = true;
        progress.LifetimeWins += 1;
        progress.LastOutcome = "won";
        state.Telemetry.Daily.TownSquareMagicianWins += 1;

        var feedback = rewardGranted
            ? string.IsNullOrWhiteSpace(round.VictoryLine)
                ? I18n.Get("magician.feedback.win_reward", $"Well struck. Your prize is {round.RewardGold}g.", new { reward = round.RewardGold })
                : $"{round.VictoryLine} {I18n.Get("magician.feedback.win_reward_suffix", $"Your prize is {round.RewardGold}g.", new { reward = round.RewardGold })}"
            : I18n.Get("magician.feedback.win_no_reward", "You have already claimed today's reward, but the trick still bowed to you.");

        progress.LastFeedback = feedback;
        return BuildResult(progress, round, true, true, rewardGranted, true, rewardGranted ? round.RewardGold : 0, feedback);
    }

    private TownSquareMagicianGuessResult BuildResult(
        TownSquareMagicianState progress,
        TownSquareMagicianRoundDefinition round,
        bool accepted,
        bool solved,
        bool rewardGranted,
        bool sessionEnded,
        int rewardGoldGranted,
        string feedback)
    {
        return new TownSquareMagicianGuessResult
        {
            Accepted = accepted,
            Solved = solved,
            RewardGranted = rewardGranted,
            SessionEnded = sessionEnded,
            RewardGoldGranted = rewardGoldGranted,
            Feedback = feedback,
            CurrentRound = BuildRoundView(progress, round)
        };
    }

    private TownSquareMagicianRoundView BuildRoundView(TownSquareMagicianState progress, TownSquareMagicianRoundDefinition round)
    {
        return new TownSquareMagicianRoundView
        {
            RoundId = round.Id,
            Mode = round.Mode,
            ModeLabel = string.Equals(round.Mode, "guess_number", StringComparison.OrdinalIgnoreCase)
                ? I18n.Get("magician.mode.number", "Guess the Number")
                : I18n.Get("magician.mode.word", "Guess the Word"),
            Prompt = round.Prompt,
            OpeningLine = round.OpeningLine,
            RewardLabel = I18n.Get("magician.reward.gold", $"+{round.RewardGold}g", new { reward = round.RewardGold }),
            AttemptsUsed = progress.AttemptsUsed,
            AttemptsRemaining = Math.Max(0, round.MaxAttempts - progress.AttemptsUsed),
            MaxAttempts = round.MaxAttempts,
            RewardClaimedToday = progress.RewardClaimedToday,
            SolvedToday = progress.SolvedToday,
            LastFeedback = progress.LastFeedback
        };
    }

    private TownSquareMagicianRoundDefinition? EnsureRoundForToday(SaveState state, bool resetProgressIfRoundChanged)
    {
        var progress = state.MiniGames.TownSquareMagician;
        var today = Math.Max(1, state.Calendar.Day);
        var season = (state.Calendar.Season ?? string.Empty).Trim().ToLowerInvariant();
        var featuredRound = ResolveRoundForToday(today, season);
        if (featuredRound is null)
            return null;

        var roundChanged = progress.Day != today
            || !string.Equals(progress.Season, season, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(progress.FeaturedRoundId, featuredRound.Id, StringComparison.OrdinalIgnoreCase);
        if (!roundChanged || !resetProgressIfRoundChanged)
            return featuredRound;

        progress.Day = today;
        progress.Season = season;
        progress.FeaturedRoundId = featuredRound.Id;
        progress.RoundId = featuredRound.Id;
        progress.RoundMode = featuredRound.Mode;
        progress.AttemptsUsed = 0;
        progress.HintsUsed = 0;
        progress.SolvedToday = false;
        progress.RewardClaimedToday = false;
        progress.LastOutcome = "fresh";
        progress.LastFeedback = string.Empty;
        progress.SessionsStartedToday = 0;
        progress.PlayedRoundIdsToday.Clear();
        return featuredRound;
    }

    private TownSquareMagicianRoundDefinition? PrepareRoundForSession(SaveState state)
    {
        var featuredRound = EnsureRoundForToday(state, resetProgressIfRoundChanged: true);
        if (featuredRound is null)
            return null;

        var progress = state.MiniGames.TownSquareMagician;
        var finishedRound = string.Equals(progress.LastOutcome, "won", StringComparison.OrdinalIgnoreCase)
            || string.Equals(progress.LastOutcome, "lost", StringComparison.OrdinalIgnoreCase);
        var previousRoundId = progress.RoundId;

        var round = GetRoundById(progress.RoundId) ?? featuredRound;
        if (progress.RewardClaimedToday && finishedRound)
            round = SelectNextBonusRound(state, featuredRound) ?? round;
        else if (string.IsNullOrWhiteSpace(progress.RoundId))
            round = featuredRound;

        progress.RoundId = round.Id;
        progress.RoundMode = round.Mode;

        if (finishedRound || !string.Equals(previousRoundId, round.Id, StringComparison.OrdinalIgnoreCase))
        {
            progress.AttemptsUsed = 0;
            progress.HintsUsed = 0;
            progress.LastOutcome = "fresh";
            progress.LastFeedback = string.Empty;
        }

        return round;
    }

    private TownSquareMagicianRoundDefinition? GetActiveRound(SaveState state)
    {
        var featuredRound = EnsureRoundForToday(state, resetProgressIfRoundChanged: true);
        if (featuredRound is null)
            return null;

        var progress = state.MiniGames.TownSquareMagician;
        var round = GetRoundById(progress.RoundId);
        if (round is not null)
            return round;

        progress.RoundId = featuredRound.Id;
        progress.RoundMode = featuredRound.Mode;
        return featuredRound;
    }

    private TownSquareMagicianRoundDefinition? GetRoundById(string? roundId)
    {
        if (string.IsNullOrWhiteSpace(roundId))
            return null;

        return _rounds.FirstOrDefault(round => string.Equals(round.Id, roundId, StringComparison.OrdinalIgnoreCase));
    }

    private TownSquareMagicianRoundDefinition? SelectNextBonusRound(SaveState state, TownSquareMagicianRoundDefinition featuredRound)
    {
        var progress = state.MiniGames.TownSquareMagician;
        var usedIds = new HashSet<string>(progress.PlayedRoundIdsToday.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);
        var dayKey = $"{Math.Max(1, state.Calendar.Year)}:{progress.Season}:{progress.Day}";
        var orderedRounds = _rounds
            .OrderBy(round => ComputeStableRoundWeight(dayKey, round.Id))
            .ThenBy(round => round.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var nextRound = orderedRounds.FirstOrDefault(round => !usedIds.Contains(round.Id));
        if (nextRound is not null)
            return nextRound;

        var currentRound = GetRoundById(progress.RoundId);
        return currentRound ?? featuredRound;
    }

    private static void MarkRoundPlayed(TownSquareMagicianState progress, string roundId)
    {
        if (string.IsNullOrWhiteSpace(roundId))
            return;

        if (progress.PlayedRoundIdsToday.Any(existing => string.Equals(existing, roundId, StringComparison.OrdinalIgnoreCase)))
            return;

        progress.PlayedRoundIdsToday.Add(roundId);
    }

    private static int ComputeStableRoundWeight(string dayKey, string roundId)
    {
        unchecked
        {
            var hash = 17;
            foreach (var ch in $"{dayKey}:{roundId}")
                hash = (hash * 31) + ch;
            return hash;
        }
    }

    private TownSquareMagicianRoundDefinition? ResolveRoundForToday(int day, string season)
    {
        if (_rounds.Count == 0)
            return null;

        var exact = _rounds.FirstOrDefault(round =>
            round.Day == day
            && (string.IsNullOrWhiteSpace(round.Season) || string.Equals(round.Season, season, StringComparison.OrdinalIgnoreCase)));
        if (exact is not null)
            return exact;

        var byDay = _rounds.FirstOrDefault(round => round.Day == day);
        if (byDay is not null)
            return byDay;

        var index = (Math.Max(1, day) - 1) % _rounds.Count;
        return _rounds[index];
    }

    private static string GetClueForAttempt(TownSquareMagicianRoundDefinition round, int clueIndex)
    {
        if (round.Clues.Count == 0)
            return string.Empty;

        var safeIndex = Math.Clamp(clueIndex, 0, round.Clues.Count - 1);
        return (round.Clues[safeIndex] ?? string.Empty).Trim();
    }

    private static string NormalizeWordGuess(string? rawGuess)
    {
        if (string.IsNullOrWhiteSpace(rawGuess))
            return string.Empty;

        var chars = rawGuess
            .Trim()
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();
        return new string(chars);
    }

    private List<TownSquareMagicianRoundDefinition> LoadCatalog(IModHelper helper)
    {
        try
        {
            var catalog = helper.Data.ReadJsonFile<TownSquareMagicianRoundCatalog>("assets/town-square-magician-rounds.json");
            if (catalog?.Rounds is not null && catalog.Rounds.Count > 0)
                return catalog.Rounds;
        }
        catch (Exception ex)
        {
            _monitor.Log($"Failed to load magician round catalog: {ex.Message}", LogLevel.Warn);
        }

        return new List<TownSquareMagicianRoundDefinition>
        {
            new()
            {
                Id = "fallback_01",
                Day = 1,
                Mode = "guess_number",
                Prompt = "I am thinking of a whole number between 1 and 10.",
                Answer = "4",
                MinNumber = 1,
                MaxNumber = 10,
                MaxAttempts = 4,
                RewardGold = 90,
                OpeningLine = "A small number today. Listen to the shape of it.",
                VictoryLine = "You caught it cleanly.",
                Clues = new List<string> { "It is even.", "It rests below five." }
            },
            new()
            {
                Id = "fallback_02",
                Day = 2,
                Mode = "guess_word",
                Prompt = "Name the hidden word.",
                Answer = "crow",
                MaxAttempts = 4,
                RewardGold = 90,
                OpeningLine = "A black-winged thing from the fields.",
                VictoryLine = "You heard the right featherbeat.",
                Clues = new List<string> { "It has feathers.", "Farmers chase it from crops.", "It starts with C." }
            }
        };
    }
}

public sealed class TownSquareMagicianRoundCatalog
{
    public List<TownSquareMagicianRoundDefinition> Rounds { get; set; } = new();
}

public sealed class TownSquareMagicianRoundDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Season { get; set; } = string.Empty;
    public int Day { get; set; }
    public string Mode { get; set; } = "guess_number";
    public string Prompt { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int MinNumber { get; set; } = 1;
    public int MaxNumber { get; set; } = 20;
    public int MaxAttempts { get; set; } = 4;
    public int RewardGold { get; set; }
    public string OpeningLine { get; set; } = string.Empty;
    public string VictoryLine { get; set; } = string.Empty;
    public List<string> Clues { get; set; } = new();
}

public sealed class TownSquareMagicianRoundView
{
    public string RoundId { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public string ModeLabel { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string OpeningLine { get; set; } = string.Empty;
    public string RewardLabel { get; set; } = string.Empty;
    public int AttemptsUsed { get; set; }
    public int AttemptsRemaining { get; set; }
    public int MaxAttempts { get; set; }
    public bool SolvedToday { get; set; }
    public bool RewardClaimedToday { get; set; }
    public string LastFeedback { get; set; } = string.Empty;
}

public sealed class TownSquareMagicianGuessResult
{
    public bool Accepted { get; set; }
    public bool Solved { get; set; }
    public bool RewardGranted { get; set; }
    public bool SessionEnded { get; set; }
    public int RewardGoldGranted { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public TownSquareMagicianRoundView CurrentRound { get; set; } = new();
}
