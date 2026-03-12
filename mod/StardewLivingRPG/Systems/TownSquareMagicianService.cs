using System.Globalization;
using System.Text;
using StardewModdingAPI;
using StardewLivingRPG.State;
using StardewLivingRPG.Utils;
using StardewValley;

namespace StardewLivingRPG.Systems;

public sealed class TownSquareMagicianService
{
    public const string MagicianNpcName = "Morrow";
    public const int MaxBubbleTextLength = 50;
    private const int RarePityThreshold = 7;
    private const int GrandPityThreshold = 120;
    private const int GrandTrinketUnlockYear = 2;
    private const int BottomOfMinesLevel = 120;

    private readonly IMonitor _monitor;
    private readonly IModHelper _helper;
    private readonly List<TownSquareMagicianRoundDefinition> _rounds;
    private readonly List<TownSquareMagicianRewardDefinition> _rewardPool;
    private readonly Dictionary<string, Dictionary<string, TownSquareMagicianRoundLocaleEntry>> _roundLocaleOverlays = new(StringComparer.OrdinalIgnoreCase);

    public TownSquareMagicianService(IModHelper helper, IMonitor monitor)
    {
        _helper = helper;
        _monitor = monitor;
        _rounds = LoadCatalog(helper);
        _rewardPool = LoadRewardPool(helper);
        ValidateLocaleOverlayCoverage("es");
        ValidateLocaleOverlayCoverage("pt-br");
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
        return round is null ? null : BuildRoundView(state.MiniGames.TownSquareMagician, LocalizeRound(round));
    }

    public TownSquareMagicianRoundView? PeekUpcomingRound(SaveState state)
    {
        var featuredRound = EnsureRoundForToday(state, resetProgressIfRoundChanged: true);
        if (featuredRound is null)
            return null;

        var progress = state.MiniGames.TownSquareMagician;
        var finishedRound = string.Equals(progress.LastOutcome, "won", StringComparison.OrdinalIgnoreCase)
            || string.Equals(progress.LastOutcome, "lost", StringComparison.OrdinalIgnoreCase);

        var round = GetRoundById(progress.RoundId) ?? featuredRound;
        if (progress.RewardClaimedToday && finishedRound)
            round = SelectNextBonusRound(state, featuredRound) ?? round;
        else if (string.IsNullOrWhiteSpace(progress.RoundId))
            round = featuredRound;

        var previewProgress = new TownSquareMagicianState
        {
            Day = progress.Day,
            Season = progress.Season,
            FeaturedRoundId = progress.FeaturedRoundId,
            RoundId = round.Id,
            RoundMode = round.Mode,
            RewardClaimedToday = progress.RewardClaimedToday,
            SolvedToday = progress.SolvedToday,
            LastFeedback = progress.LastFeedback,
            LastOutcome = "fresh"
        };

        return BuildRoundView(previewProgress, LocalizeRound(round));
    }

    public TownSquareMagicianRoundView? BeginRoundSession(SaveState state)
    {
        var round = PrepareRoundForSession(state);
        if (round is null)
            return null;

        var progress = state.MiniGames.TownSquareMagician;
        var isBonusRound = IsBonusRound(progress, round);
        if (progress.SessionsStartedToday == 0)
            AwardArcProgress(progress, 1);
        if (isBonusRound)
        {
            progress.LifetimeBonusRoundsPlayed += 1;
            AwardArcProgress(progress, 1);
        }
        progress.SessionsStartedToday += 1;
        state.Telemetry.Daily.TownSquareMagicianSessions += 1;
        progress.LastPlayStyleTag = ResolvePlayStyleTag(progress, round, solved: false);
        return BuildRoundView(progress, LocalizeRound(round));
    }

    public TownSquareMagicianGuessResult SubmitGuess(SaveState state, string? rawGuess)
    {
        var round = GetActiveRound(state);
        if (round is null)
        {
            return new TownSquareMagicianGuessResult
            {
                Accepted = false,
                Feedback = ClampBubbleText(I18n.Get("magician.feedback.unavailable", "No riddle is ready today."))
            };
        }

        var progress = state.MiniGames.TownSquareMagician;
        var normalizedGuess = (rawGuess ?? string.Empty).Trim();
        if (progress.AttemptsUsed >= round.MaxAttempts)
        {
            progress.LastOutcome = "lost";
            progress.LastFeedback = ClampBubbleText(I18n.Get("magician.feedback.session_locked", "That round is spent. Ask again if you want another go."));
            return BuildResult(progress, round, false, false, false, true, null, progress.LastFeedback);
        }

        if (IsHintCommand(normalizedGuess))
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
        progress.LastPlayStyleTag = "steady";
        progress.PlayedRoundIdsToday.Clear();
    }

    public string BuildDebugSummary(SaveState state)
    {
        var round = EnsureRoundForToday(state, resetProgressIfRoundChanged: true);
        if (round is null)
            return "Magician | no round loaded";

        var progress = state.MiniGames.TownSquareMagician;
        return $"Magician | npc={MagicianNpcName}, featured={progress.FeaturedRoundId}, active={round.Id}, mode={round.Mode}, attempts={progress.AttemptsUsed}/{round.MaxAttempts}, solved={progress.SolvedToday}, rewardClaimed={progress.RewardClaimedToday}, rarePity={progress.RareDryRewardDays}, grandPity={progress.GrandDryRewardDays}, lastOutcome={progress.LastOutcome}, arc={progress.ArcStageId}, style={progress.LastPlayStyleTag}";
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
                rewardGrant: null,
                feedback: ClampBubbleText(I18n.Get("magician.feedback.invalid_number", "Speak a whole number, not smoke.")));
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
                rewardGrant: null,
                feedback: ClampBubbleText(I18n.Get(
                    "magician.feedback.out_of_range",
                    $"Keep it between {round.MinNumber} and {round.MaxNumber}.",
                    new { min = round.MinNumber, max = round.MaxNumber })));
        }

        progress.AttemptsUsed += 1;

        var answerNumber = int.TryParse(round.Answer, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedAnswer)
            ? parsedAnswer
            : round.MinNumber;

        if (guessedNumber == answerNumber)
            return ResolveWin(state, progress, round);

        var clue = GetClueForAttempt(round, progress.AttemptsUsed + progress.HintsUsed - 1);
        var directionKey = guessedNumber < answerNumber ? "magician.feedback.number.higher" : "magician.feedback.number.lower";
        var fallback = guessedNumber < answerNumber ? "Higher." : "Lower.";

        if (progress.AttemptsUsed >= round.MaxAttempts)
        {
            var answerText = I18n.Get(
                "magician.feedback.loss_number",
                $"The number was {answerNumber}. Come back if you want another crack at it.",
                new { answer = answerNumber });
            MarkRoundPlayed(progress, round.Id);
            RecordLoss(progress, round);
            progress.LastOutcome = "lost";
            progress.LastFeedback = answerText;
            return BuildResult(progress, round, true, false, false, true, null, answerText);
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
        return BuildResult(progress, round, true, false, false, false, null, hintText);
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
            return BuildResult(progress, round, true, false, false, false, null, noHintText);
        }

        var clue = GetClueForAttempt(round, clueIndex);
        progress.HintsUsed += 1;
        progress.LifetimeHintsUsed += 1;
        AwardArcProgress(progress, 1);
        progress.LastOutcome = progress.AttemptsUsed > 0 || progress.HintsUsed > 0 ? "in_progress" : "fresh";
        progress.LastPlayStyleTag = ResolvePlayStyleTag(progress, round, solved: false);
        progress.LastFeedback = I18n.Get("magician.feedback.hint.clue", $"Hint: {clue}", new { clue });
        return BuildResult(progress, round, true, false, false, false, null, progress.LastFeedback);
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
                rewardGrant: null,
                feedback: ClampBubbleText(I18n.Get("magician.feedback.invalid_word", "Give me a word, plain and clear.")));
        }

        progress.AttemptsUsed += 1;
        if (GetAcceptedAnswers(round).Any(answer => string.Equals(guess, NormalizeWordGuess(answer), StringComparison.Ordinal)))
            return ResolveWin(state, progress, round);

        if (progress.AttemptsUsed >= round.MaxAttempts)
        {
            var revealAnswer = GetRevealAnswer(round);
            var answerText = I18n.Get(
                "magician.feedback.loss_word",
                $"The word was {revealAnswer}. Ask again if you want another try.",
                new { answer = revealAnswer });
            MarkRoundPlayed(progress, round.Id);
            RecordLoss(progress, round);
            progress.LastOutcome = "lost";
            progress.LastFeedback = answerText;
            return BuildResult(progress, round, true, false, false, true, null, answerText);
        }

        var clue = GetClueForAttempt(round, progress.AttemptsUsed + progress.HintsUsed - 1);
        var hintText = string.IsNullOrWhiteSpace(clue)
            ? I18n.Get("magician.feedback.word.retry", "Not that one. Listen close and try again.")
            : I18n.Get("magician.feedback.word.clue", $"Not quite. Clue: {clue}", new { clue });

        progress.LastOutcome = "in_progress";
        progress.LastFeedback = hintText;
        return BuildResult(progress, round, true, false, false, false, null, hintText);
    }

    private TownSquareMagicianGuessResult ResolveWin(
        SaveState state,
        TownSquareMagicianState progress,
        TownSquareMagicianRoundDefinition round)
    {
        var rewardGrant = !progress.RewardClaimedToday ? ResolveRewardGrant(state, progress, round) : null;
        var rewardGranted = rewardGrant is not null;
        if (rewardGranted)
        {
            progress.RewardClaimedToday = true;
            progress.LifetimeRewardClaims += 1;
            state.Telemetry.Daily.TownSquareMagicianRewardClaims += 1;
            UpdateRewardPity(progress, rewardGrant!);
        }

        MarkRoundPlayed(progress, round.Id);
        progress.SolvedToday = true;
        progress.LifetimeWins += 1;
        progress.ConsecutiveWins += 1;
        progress.ConsecutiveLosses = 0;
        progress.LastPlayStyleTag = ResolvePlayStyleTag(progress, round, solved: true);
        AwardArcProgress(progress, rewardGranted ? 3 : 2);
        progress.LastOutcome = "won";
        state.Telemetry.Daily.TownSquareMagicianWins += 1;

        var rewardText = BuildRewardWinText(rewardGrant);
        var rewardSuffix = BuildRewardWinSuffix(rewardGrant);
        var combinedRewardText = string.IsNullOrWhiteSpace(round.VictoryLine)
            ? rewardText
            : $"{round.VictoryLine} {rewardSuffix}";
        var feedback = rewardGranted
            ? (combinedRewardText.Length <= MaxBubbleTextLength ? combinedRewardText : rewardText)
            : I18n.Get("magician.feedback.win_no_reward", "Prize is claimed, but you still got it.");

        progress.LastFeedback = feedback;
        return BuildResult(progress, round, true, true, rewardGranted, true, rewardGrant, feedback);
    }

    private TownSquareMagicianGuessResult BuildResult(
        TownSquareMagicianState progress,
        TownSquareMagicianRoundDefinition round,
        bool accepted,
        bool solved,
        bool rewardGranted,
        bool sessionEnded,
        TownSquareMagicianRewardGrant? rewardGrant,
        string feedback)
    {
        return new TownSquareMagicianGuessResult
        {
            Accepted = accepted,
            Solved = solved,
            RewardGranted = rewardGranted,
            SessionEnded = sessionEnded,
            RewardGrant = rewardGrant,
            Feedback = ClampBubbleText(feedback),
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
            Prompt = BuildPromptText(round),
            OpeningLine = ClampBubbleText(round.OpeningLine),
            RewardLabel = _rewardPool.Count > 0
                ? I18n.Get("magician.reward.item", "A small trinket")
                : I18n.Get("magician.reward.gold", $"+{round.RewardGold}g", new { reward = round.RewardGold }),
            AttemptsUsed = progress.AttemptsUsed,
            AttemptsRemaining = Math.Max(0, round.MaxAttempts - progress.AttemptsUsed),
            MaxAttempts = round.MaxAttempts,
            RewardClaimedToday = progress.RewardClaimedToday,
            SolvedToday = progress.SolvedToday,
            LastFeedback = ClampBubbleText(progress.LastFeedback)
        };
    }

    private TownSquareMagicianRoundDefinition? EnsureRoundForToday(SaveState state, bool resetProgressIfRoundChanged)
    {
        var progress = state.MiniGames.TownSquareMagician;
        var today = Math.Max(1, state.Calendar.Day);
        var season = (state.Calendar.Season ?? string.Empty).Trim().ToLowerInvariant();
        var featuredRound = ResolveRoundForToday(state);
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
        progress.LastPlayStyleTag = "steady";
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
            return LocalizeRound(round);

        progress.RoundId = featuredRound.Id;
        progress.RoundMode = featuredRound.Mode;
        return LocalizeRound(featuredRound);
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
        var orderedRounds = GetOrderedRoundsForDay(state);

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

    private static bool IsBonusRound(TownSquareMagicianState progress, TownSquareMagicianRoundDefinition round)
    {
        return progress.RewardClaimedToday
            && !string.IsNullOrWhiteSpace(progress.FeaturedRoundId)
            && !string.Equals(progress.FeaturedRoundId, round.Id, StringComparison.OrdinalIgnoreCase);
    }

    private static void RecordLoss(TownSquareMagicianState progress, TownSquareMagicianRoundDefinition round)
    {
        progress.LifetimeLosses += 1;
        progress.ConsecutiveLosses += 1;
        progress.ConsecutiveWins = 0;
        progress.LastPlayStyleTag = ResolvePlayStyleTag(progress, round, solved: false);
        AwardArcProgress(progress, 1);
    }

    private static string ResolvePlayStyleTag(TownSquareMagicianState progress, TownSquareMagicianRoundDefinition round, bool solved)
    {
        if (progress.HintsUsed >= 2)
            return "careful";
        if (solved && progress.AttemptsUsed <= 1)
            return "sharp";
        if (progress.AttemptsUsed >= Math.Max(1, round.MaxAttempts - 1))
            return "dogged";
        if (IsBonusRound(progress, round))
            return "restless";
        return "steady";
    }

    private static void AwardArcProgress(TownSquareMagicianState progress, int amount)
    {
        if (amount <= 0)
            return;

        progress.ArcProgressPoints += amount;
        progress.ArcStageId = ResolveArcStageId(progress.ArcProgressPoints);
    }

    private static string ResolveArcStageId(int points)
    {
        if (points >= 24)
            return "town_mirror";
        if (points >= 16)
            return "old_roads";
        if (points >= 9)
            return "house_of_cards";
        if (points >= 4)
            return "keen_eye";
        return "street_smoke";
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

    private TownSquareMagicianRoundDefinition? ResolveRoundForToday(SaveState state)
    {
        if (_rounds.Count == 0)
            return null;

        return GetOrderedRoundsForDay(state).FirstOrDefault();
    }

    private static string GetClueForAttempt(TownSquareMagicianRoundDefinition round, int clueIndex)
    {
        if (round.Clues.Count == 0 || clueIndex < 0 || clueIndex >= round.Clues.Count)
            return string.Empty;

        return ClampBubbleText((round.Clues[clueIndex] ?? string.Empty).Trim());
    }

    private static string BuildPromptText(TownSquareMagicianRoundDefinition round)
    {
        if (string.Equals(round.Mode, "guess_number", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(round.Prompt)
                && !string.Equals(round.Prompt, "Guess the number.", StringComparison.OrdinalIgnoreCase))
            {
                return ClampBubbleText(round.Prompt);
            }

            return ClampBubbleText(I18n.Get(
                "magician.prompt.number",
                $"Guess a number from {round.MinNumber} to {round.MaxNumber}.",
                new
                {
                    min = round.MinNumber,
                    max = round.MaxNumber
            }));
        }

        if (!string.IsNullOrWhiteSpace(round.Prompt)
            && !string.Equals(round.Prompt, "Guess the hidden word.", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(round.Prompt, "Name the hidden word.", StringComparison.OrdinalIgnoreCase))
        {
            return ClampBubbleText(round.Prompt);
        }

        return ClampBubbleText(I18n.Get("magician.prompt.word", "Guess the hidden word."));
    }

    public static string ClampBubbleText(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return string.Empty;

        var value = rawText
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
        while (value.Contains("  ", StringComparison.Ordinal))
            value = value.Replace("  ", " ", StringComparison.Ordinal);

        if (value.Length <= MaxBubbleTextLength)
            return value;

        return value[..(MaxBubbleTextLength - 3)].TrimEnd() + "...";
    }

    private static string NormalizeWordGuess(string? rawGuess)
    {
        if (string.IsNullOrWhiteSpace(rawGuess))
            return string.Empty;

        var normalized = rawGuess.Trim().Normalize(NormalizationForm.FormD);
        var chars = normalized
            .Trim()
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();
        return new string(chars);
    }

    private bool IsHintCommand(string? rawGuess)
    {
        var normalized = NormalizeWordGuess(rawGuess);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        var aliases = I18n.Get("magician.command.hint_aliases", "hint")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeWordGuess)
            .Where(alias => !string.IsNullOrWhiteSpace(alias));

        foreach (var alias in aliases)
        {
            if (string.Equals(normalized, alias, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private IReadOnlyList<string> GetAcceptedAnswers(TownSquareMagicianRoundDefinition round)
    {
        if (round.AcceptedAnswers.Count > 0)
            return round.AcceptedAnswers;

        return string.IsNullOrWhiteSpace(round.Answer)
            ? Array.Empty<string>()
            : new[] { round.Answer };
    }

    private static string GetRevealAnswer(TownSquareMagicianRoundDefinition round)
    {
        if (!string.IsNullOrWhiteSpace(round.RevealAnswer))
            return round.RevealAnswer;
        if (round.AcceptedAnswers.Count > 0 && !string.IsNullOrWhiteSpace(round.AcceptedAnswers[0]))
            return round.AcceptedAnswers[0];
        return round.Answer;
    }

    private TownSquareMagicianRoundDefinition LocalizeRound(TownSquareMagicianRoundDefinition round)
    {
        var overlay = ResolveLocaleOverlay(round.Id);
        if (overlay is null)
            return EnsureAcceptedAnswers(round);

        var localized = new TownSquareMagicianRoundDefinition
        {
            Id = round.Id,
            Season = round.Season,
            Day = round.Day,
            Mode = round.Mode,
            Prompt = string.IsNullOrWhiteSpace(overlay.Prompt) ? round.Prompt : overlay.Prompt.Trim(),
            Answer = round.Answer,
            MinNumber = round.MinNumber,
            MaxNumber = round.MaxNumber,
            MaxAttempts = round.MaxAttempts,
            RewardGold = round.RewardGold,
            OpeningLine = string.IsNullOrWhiteSpace(overlay.OpeningLine) ? round.OpeningLine : overlay.OpeningLine.Trim(),
            VictoryLine = string.IsNullOrWhiteSpace(overlay.VictoryLine) ? round.VictoryLine : overlay.VictoryLine.Trim(),
            Clues = overlay.Clues.Count > 0 ? overlay.Clues.Where(static clue => !string.IsNullOrWhiteSpace(clue)).Select(clue => clue.Trim()).ToList() : new List<string>(round.Clues),
            RevealAnswer = string.IsNullOrWhiteSpace(overlay.RevealAnswer) ? round.RevealAnswer : overlay.RevealAnswer.Trim()
        };

        if (overlay.Answers.Count > 0)
        {
            localized.AcceptedAnswers = overlay.Answers
                .Where(static answer => !string.IsNullOrWhiteSpace(answer))
                .Select(answer => answer.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else if (round.AcceptedAnswers.Count > 0)
        {
            localized.AcceptedAnswers = new List<string>(round.AcceptedAnswers);
        }
        else if (!string.IsNullOrWhiteSpace(round.Answer))
        {
            localized.AcceptedAnswers = new List<string> { round.Answer };
        }

        if (string.IsNullOrWhiteSpace(localized.RevealAnswer))
            localized.RevealAnswer = GetRevealAnswer(localized);

        return localized;
    }

    private TownSquareMagicianRoundDefinition EnsureAcceptedAnswers(TownSquareMagicianRoundDefinition round)
    {
        if (round.AcceptedAnswers.Count > 0 || string.IsNullOrWhiteSpace(round.Answer))
            return round;

        return new TownSquareMagicianRoundDefinition
        {
            Id = round.Id,
            Season = round.Season,
            Day = round.Day,
            Mode = round.Mode,
            Prompt = round.Prompt,
            Answer = round.Answer,
            MinNumber = round.MinNumber,
            MaxNumber = round.MaxNumber,
            MaxAttempts = round.MaxAttempts,
            RewardGold = round.RewardGold,
            OpeningLine = round.OpeningLine,
            VictoryLine = round.VictoryLine,
            Clues = new List<string>(round.Clues),
            AcceptedAnswers = new List<string> { round.Answer },
            RevealAnswer = string.IsNullOrWhiteSpace(round.RevealAnswer) ? round.Answer : round.RevealAnswer
        };
    }

    private TownSquareMagicianRoundLocaleEntry? ResolveLocaleOverlay(string roundId)
    {
        if (string.IsNullOrWhiteSpace(roundId))
            return null;

        TownSquareMagicianRoundLocaleEntry? merged = null;
        foreach (var candidate in BuildLocaleFallback(I18n.GetCurrentLocaleCode()).Reverse())
        {
            var catalog = GetLocaleOverlayCatalog(candidate);
            if (catalog.Count == 0 || !catalog.TryGetValue(roundId, out var overlay))
                continue;

            merged = MergeLocaleOverlayEntry(merged, overlay);
        }

        return merged;
    }

    private Dictionary<string, TownSquareMagicianRoundLocaleEntry> GetLocaleOverlayCatalog(string locale)
    {
        if (_roundLocaleOverlays.TryGetValue(locale, out var cached))
            return cached;

        var path = $"assets/town-square-magician-rounds.{locale}.json";
        try
        {
            var catalog = _helper.Data.ReadJsonFile<TownSquareMagicianRoundLocaleCatalog>(path);
            var loaded = catalog?.Rounds is not null
                ? new Dictionary<string, TownSquareMagicianRoundLocaleEntry>(catalog.Rounds, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, TownSquareMagicianRoundLocaleEntry>(StringComparer.OrdinalIgnoreCase);
            _roundLocaleOverlays[locale] = loaded;
            return loaded;
        }
        catch (Exception ex)
        {
            _monitor.Log($"Failed to load magician locale overlay '{path}': {ex.Message}", LogLevel.Warn);
            var empty = new Dictionary<string, TownSquareMagicianRoundLocaleEntry>(StringComparer.OrdinalIgnoreCase);
            _roundLocaleOverlays[locale] = empty;
            return empty;
        }
    }

    private static TownSquareMagicianRoundLocaleEntry MergeLocaleOverlayEntry(
        TownSquareMagicianRoundLocaleEntry? original,
        TownSquareMagicianRoundLocaleEntry overlay)
    {
        var merged = new TownSquareMagicianRoundLocaleEntry();
        if (original is not null)
        {
            merged.Prompt = original.Prompt;
            merged.OpeningLine = original.OpeningLine;
            merged.VictoryLine = original.VictoryLine;
            merged.RevealAnswer = original.RevealAnswer;
            merged.Clues = new List<string>(original.Clues);
            merged.Answers = new List<string>(original.Answers);
        }

        if (!string.IsNullOrWhiteSpace(overlay.Prompt))
            merged.Prompt = overlay.Prompt.Trim();
        if (!string.IsNullOrWhiteSpace(overlay.OpeningLine))
            merged.OpeningLine = overlay.OpeningLine.Trim();
        if (!string.IsNullOrWhiteSpace(overlay.VictoryLine))
            merged.VictoryLine = overlay.VictoryLine.Trim();
        if (!string.IsNullOrWhiteSpace(overlay.RevealAnswer))
            merged.RevealAnswer = overlay.RevealAnswer.Trim();
        if (overlay.Clues.Count > 0)
            merged.Clues = overlay.Clues.Where(static clue => !string.IsNullOrWhiteSpace(clue)).Select(clue => clue.Trim()).ToList();
        if (overlay.Answers.Count > 0)
            merged.Answers = overlay.Answers.Where(static answer => !string.IsNullOrWhiteSpace(answer)).Select(answer => answer.Trim()).ToList();

        return merged;
    }

    private IEnumerable<string> BuildLocaleFallback(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            yield break;

        var normalized = locale.Trim().Replace('_', '-').ToLowerInvariant();
        yield return normalized;

        var dash = normalized.IndexOf('-', StringComparison.Ordinal);
        if (dash > 0)
            yield return normalized[..dash];
    }

    private List<TownSquareMagicianRoundDefinition> LoadCatalog(IModHelper helper)
    {
        try
        {
            var catalog = helper.Data.ReadJsonFile<TownSquareMagicianRoundCatalog>("assets/town-square-magician-rounds.json");
            if (catalog?.Rounds is not null && catalog.Rounds.Count > 0)
            {
                ValidateCatalogTextLengths(catalog.Rounds);
                ValidateCatalogCount(catalog.Rounds);
                return catalog.Rounds;
            }
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

    private List<TownSquareMagicianRewardDefinition> LoadRewardPool(IModHelper helper)
    {
        try
        {
            var pool = helper.Data.ReadJsonFile<TownSquareMagicianRewardCatalog>("assets/town-square-magician-rewards.json");
            if (pool?.Rewards is not null && pool.Rewards.Count > 0)
            {
                return pool.Rewards
                    .Where(reward =>
                        !string.IsNullOrWhiteSpace(reward.Id)
                        && !string.IsNullOrWhiteSpace(reward.QualifiedItemId)
                        && reward.Weight > 0)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _monitor.Log($"Failed to load magician reward pool: {ex.Message}", LogLevel.Warn);
        }

        return new List<TownSquareMagicianRewardDefinition>();
    }

    private TownSquareMagicianRewardGrant? ResolveRewardGrant(
        SaveState state,
        TownSquareMagicianState progress,
        TownSquareMagicianRoundDefinition round)
    {
        var itemReward = ResolveItemReward(state, progress, round);
        if (itemReward is not null)
            return itemReward;

        if (round.RewardGold <= 0)
            return null;

        return new TownSquareMagicianRewardGrant
        {
            RewardType = "gold",
            GoldAmount = round.RewardGold,
            DisplayName = $"{round.RewardGold}g",
            Tier = "common"
        };
    }

    private TownSquareMagicianRewardGrant? ResolveItemReward(
        SaveState state,
        TownSquareMagicianState progress,
        TownSquareMagicianRoundDefinition round)
    {
        if (_rewardPool.Count == 0)
            return null;

        var key = $"{BuildDayKey(state)}:{round.Id}:reward";
        var targetTier = ResolveRewardTier(progress, key);
        var grandRewardUnlocked = IsGrandRewardUnlocked(state);
        TownSquareMagicianRewardDefinition? reward = null;
        foreach (var tier in GetRewardTierFallbacks(targetTier))
        {
            reward = ResolveWeightedRewardForTier(key, tier, grandRewardUnlocked);
            if (reward is not null)
                break;
        }
        if (reward is null)
            return null;

        return new TownSquareMagicianRewardGrant
        {
            RewardType = "item",
            QualifiedItemId = reward.QualifiedItemId,
            Stack = Math.Max(1, reward.Stack),
            DisplayName = string.IsNullOrWhiteSpace(reward.DisplayName) ? reward.QualifiedItemId : reward.DisplayName,
            Tier = reward.Tier
        };
    }

    private static string BuildRewardWinText(TownSquareMagicianRewardGrant? rewardGrant)
    {
        if (rewardGrant is null)
            return string.Empty;

        if (string.Equals(rewardGrant.RewardType, "item", StringComparison.OrdinalIgnoreCase))
        {
            var key = string.Equals(rewardGrant.Tier, "grand", StringComparison.OrdinalIgnoreCase)
                ? "magician.feedback.win_reward_grand"
                : "magician.feedback.win_reward_item";
            return I18n.Get(
                key,
                $"Take this: {BuildRewardDisplayText(rewardGrant)}.",
                new { reward = BuildRewardDisplayText(rewardGrant) });
        }

        return I18n.Get(
            "magician.feedback.win_reward",
            $"Well struck. Prize: {rewardGrant.GoldAmount}g.",
            new { reward = rewardGrant.GoldAmount });
    }

    private static string BuildRewardWinSuffix(TownSquareMagicianRewardGrant? rewardGrant)
    {
        if (rewardGrant is null)
            return string.Empty;

        if (string.Equals(rewardGrant.RewardType, "item", StringComparison.OrdinalIgnoreCase))
        {
            var key = string.Equals(rewardGrant.Tier, "grand", StringComparison.OrdinalIgnoreCase)
                ? "magician.feedback.win_reward_grand_suffix"
                : "magician.feedback.win_reward_item_suffix";
            return I18n.Get(
                key,
                $"Take this: {BuildRewardDisplayText(rewardGrant)}.",
                new { reward = BuildRewardDisplayText(rewardGrant) });
        }

        return I18n.Get(
            "magician.feedback.win_reward_suffix",
            $"Prize: {rewardGrant.GoldAmount}g.",
            new { reward = rewardGrant.GoldAmount });
    }

    public static string BuildRewardDisplayText(TownSquareMagicianRewardGrant? rewardGrant)
    {
        if (rewardGrant is null)
            return string.Empty;

        var name = (rewardGrant.DisplayName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            name = string.Equals(rewardGrant.RewardType, "gold", StringComparison.OrdinalIgnoreCase)
                ? $"{rewardGrant.GoldAmount}g"
                : rewardGrant.QualifiedItemId;

        return rewardGrant.Stack > 1 ? $"{rewardGrant.Stack}x {name}" : name;
    }

    private void ValidateCatalogTextLengths(IEnumerable<TownSquareMagicianRoundDefinition> rounds)
    {
        foreach (var round in rounds)
        {
            if (round is null)
                continue;

            WarnIfTooLong(round.Id, "Prompt", round.Prompt);
            WarnIfTooLong(round.Id, "OpeningLine", round.OpeningLine);
            WarnIfTooLong(round.Id, "VictoryLine", round.VictoryLine);
            foreach (var clue in round.Clues)
                WarnIfTooLong(round.Id, "Clue", clue);
        }
    }

    private void ValidateCatalogCount(IEnumerable<TownSquareMagicianRoundDefinition> rounds)
    {
        var count = rounds.Count();
        if (count < 1000)
            _monitor.Log($"Magician catalog has {count} rounds; target is 1000.", LogLevel.Warn);
    }

    private void ValidateLocaleOverlayCoverage(string locale)
    {
        var catalog = GetLocaleOverlayCatalog(locale);
        if (catalog.Count == 0)
        {
            _monitor.Log($"Magician locale overlay '{locale}' is missing or empty.", LogLevel.Warn);
            return;
        }

        var knownIds = new HashSet<string>(_rounds.Select(round => round.Id), StringComparer.OrdinalIgnoreCase);
        foreach (var roundId in catalog.Keys)
        {
            if (!knownIds.Contains(roundId))
                _monitor.Log($"Magician locale overlay '{locale}' has unknown round id '{roundId}'.", LogLevel.Warn);
        }

        foreach (var round in _rounds)
        {
            if (!catalog.TryGetValue(round.Id, out var overlay))
            {
                _monitor.Log($"Magician locale overlay '{locale}' is missing round '{round.Id}'.", LogLevel.Warn);
                continue;
            }

            ValidateLocaleOverlayEntry(locale, round, overlay);
        }
    }

    private void ValidateLocaleOverlayEntry(string locale, TownSquareMagicianRoundDefinition round, TownSquareMagicianRoundLocaleEntry overlay)
    {
        WarnIfTooLong($"{locale}:{round.Id}", "Prompt", overlay.Prompt);
        WarnIfTooLong($"{locale}:{round.Id}", "OpeningLine", overlay.OpeningLine);
        WarnIfTooLong($"{locale}:{round.Id}", "VictoryLine", overlay.VictoryLine);
        WarnIfTooLong($"{locale}:{round.Id}", "RevealAnswer", overlay.RevealAnswer);
        foreach (var clue in overlay.Clues)
            WarnIfTooLong($"{locale}:{round.Id}", "Clue", clue);

        if (string.Equals(round.Mode, "guess_word", StringComparison.OrdinalIgnoreCase) && overlay.Answers.Count == 0)
            _monitor.Log($"Magician locale overlay '{locale}' round '{round.Id}' has no localized answers.", LogLevel.Warn);

        if (overlay.Clues.Count > 0 && overlay.Clues.Count != round.Clues.Count)
            _monitor.Log($"Magician locale overlay '{locale}' round '{round.Id}' has {overlay.Clues.Count} clues; expected {round.Clues.Count}.", LogLevel.Warn);
    }

    private List<TownSquareMagicianRoundDefinition> GetOrderedRoundsForDay(SaveState state)
    {
        var dayKey = BuildDayKey(state);
        return _rounds
            .OrderBy(round => ComputeStableRoundWeight(dayKey, round.Id))
            .ThenBy(round => round.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildDayKey(SaveState state)
    {
        return $"{Math.Max(1, state.Calendar.Year)}:{(state.Calendar.Season ?? string.Empty).Trim().ToLowerInvariant()}:{Math.Max(1, state.Calendar.Day)}";
    }

    private string ResolveRewardTier(TownSquareMagicianState progress, string rewardKey)
    {
        if (progress.GrandDryRewardDays >= GrandPityThreshold - 1)
            return "grand";

        var baseTier = ResolveRewardTierFromOdds(rewardKey);
        if (progress.RareDryRewardDays >= RarePityThreshold - 1 && GetRewardTierRank(baseTier) < GetRewardTierRank("rare"))
            return "rare";

        return baseTier;
    }

    private string ResolveRewardTierFromOdds(string rewardKey)
    {
        var roll = ComputeStablePositiveValue(rewardKey, "tier") % 100;
        if (roll == 0)
            return "grand";
        if (roll < 5)
            return "rare";
        if (roll < 25)
            return "uncommon";
        return "common";
    }

    private static void UpdateRewardPity(TownSquareMagicianState progress, TownSquareMagicianRewardGrant rewardGrant)
    {
        var tier = (rewardGrant.Tier ?? string.Empty).Trim().ToLowerInvariant();
        if (tier == "grand")
        {
            progress.RareDryRewardDays = 0;
            progress.GrandDryRewardDays = 0;
            return;
        }

        if (tier == "rare")
        {
            progress.RareDryRewardDays = 0;
            progress.GrandDryRewardDays += 1;
            return;
        }

        progress.RareDryRewardDays += 1;
        progress.GrandDryRewardDays += 1;
    }

    private static int GetRewardTierRank(string tier)
    {
        return (tier ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "grand" => 4,
            "rare" => 3,
            "uncommon" => 2,
            _ => 1
        };
    }

    private static bool IsGrandRewardUnlocked(SaveState state)
    {
        var yearUnlocked = Math.Max(1, state.Calendar.Year) >= GrandTrinketUnlockYear;
        var minesUnlocked = Game1.player?.deepestMineLevel >= BottomOfMinesLevel;
        return yearUnlocked && minesUnlocked;
    }

    private static IEnumerable<string> GetRewardTierFallbacks(string targetTier)
    {
        return targetTier.ToLowerInvariant() switch
        {
            "grand" => new[] { "grand", "rare", "uncommon", "common" },
            "rare" => new[] { "rare", "uncommon", "common" },
            "uncommon" => new[] { "uncommon", "common" },
            _ => new[] { "common" }
        };
    }

    private TownSquareMagicianRewardDefinition? ResolveWeightedRewardForTier(string rewardKey, string tier, bool grandRewardUnlocked)
    {
        var candidates = _rewardPool
            .Where(entry =>
                string.Equals(entry.Tier, tier, StringComparison.OrdinalIgnoreCase)
                && entry.Weight > 0
                && (grandRewardUnlocked || !entry.RequiresGrandUnlock))
            .OrderBy(entry => ComputeStableRoundWeight(rewardKey, entry.Id))
            .ThenBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (candidates.Count == 0)
            return null;

        var totalWeight = candidates.Sum(entry => Math.Max(1, entry.Weight));
        var roll = ComputeStablePositiveValue(rewardKey, $"{tier}:pick") % totalWeight;
        var cursor = 0;
        foreach (var candidate in candidates)
        {
            cursor += Math.Max(1, candidate.Weight);
            if (roll < cursor)
                return candidate;
        }

        return candidates[^1];
    }

    private static int ComputeStablePositiveValue(string key, string salt)
    {
        return unchecked((int)((uint)ComputeStableRoundWeight(key, salt) & 0x7FFFFFFF));
    }

    private void WarnIfTooLong(string roundId, string fieldName, string? text)
    {
        var normalized = ClampBubbleText(text);
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length <= MaxBubbleTextLength)
            return;

        _monitor.Log(
            $"Magician round '{roundId}' field '{fieldName}' exceeds {MaxBubbleTextLength} chars and will be clamped: '{normalized}'",
            LogLevel.Debug);
    }
}

public sealed class TownSquareMagicianRoundCatalog
{
    public List<TownSquareMagicianRoundDefinition> Rounds { get; set; } = new();
}

public sealed class TownSquareMagicianRewardCatalog
{
    public List<TownSquareMagicianRewardDefinition> Rewards { get; set; } = new();
}

public sealed class TownSquareMagicianRoundLocaleCatalog
{
    public Dictionary<string, TownSquareMagicianRoundLocaleEntry> Rounds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class TownSquareMagicianRoundLocaleEntry
{
    public string Prompt { get; set; } = string.Empty;
    public string OpeningLine { get; set; } = string.Empty;
    public string VictoryLine { get; set; } = string.Empty;
    public List<string> Clues { get; set; } = new();
    public List<string> Answers { get; set; } = new();
    public string RevealAnswer { get; set; } = string.Empty;
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
    public List<string> AcceptedAnswers { get; set; } = new();
    public string RevealAnswer { get; set; } = string.Empty;
}

public sealed class TownSquareMagicianRewardDefinition
{
    public string Id { get; set; } = string.Empty;
    public string QualifiedItemId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int Stack { get; set; } = 1;
    public string Tier { get; set; } = "common";
    public int Weight { get; set; } = 1;
    public bool RequiresGrandUnlock { get; set; }
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
    public TownSquareMagicianRewardGrant? RewardGrant { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public TownSquareMagicianRoundView CurrentRound { get; set; } = new();
}

public sealed class TownSquareMagicianRewardGrant
{
    public string RewardType { get; set; } = string.Empty;
    public string QualifiedItemId { get; set; } = string.Empty;
    public int Stack { get; set; } = 1;
    public string DisplayName { get; set; } = string.Empty;
    public int GoldAmount { get; set; }
    public string Tier { get; set; } = "common";
}
