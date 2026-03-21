namespace StardewLivingRPG.State;

public sealed class ChickenRaceState
{
    public int RacesToday { get; set; }
    public int LastRaceDay { get; set; }
    public long TotalGoldWon { get; set; }
    public long TotalGoldLost { get; set; }
    public int RacesWon { get; set; }
    public int RacesEntered { get; set; }
}

public sealed class ChickenRacer
{
    public string Name { get; set; } = string.Empty;
    public int ColorVariant { get; set; }
    public float BaseSpeed { get; set; }
    public float Stamina { get; set; }
    public float Luck { get; set; }
    public float Odds { get; set; }

    public float CalculateScore()
    {
        return (BaseSpeed * 0.4f) + (Stamina * 0.35f) + (Luck * 0.25f);
    }
}

public sealed class RaceSession
{
    public List<ChickenRacer> Racers { get; set; } = new();
    public int PlayerBetIndex { get; set; } = -1;
    public int BetAmount { get; set; }
    public float[] Positions { get; set; } = Array.Empty<float>();
    public bool RaceInProgress { get; set; }
    public bool RaceFinished { get; set; }
    public int WinnerIndex { get; set; } = -1;
    public int RaceSeed { get; set; }
    public int RaceNumber { get; set; }

    public void InitializePositions()
    {
        Positions = new float[Racers.Count];
        Array.Clear(Positions, 0, Positions.Length);
    }
}
