using StardewModdingAPI;
using StardewLivingRPG.Config;
using StardewLivingRPG.State;

namespace StardewLivingRPG.Systems;

public sealed class DailyTickService
{
    private readonly IMonitor _monitor;
    private readonly ModConfig _config;

    public DailyTickService(IMonitor monitor, ModConfig config)
    {
        _monitor = monitor;
        _config = config;
    }

    public void Run(SaveState state)
    {
        // Sync config only; live calendar is sourced from game state in ModEntry.
        state.ApplyConfig(_config);

        // Basic telemetry heartbeat for now.
        state.Telemetry.Daily.WorldMutations += 1;

        _monitor.Log("DailyTickService.Run executed (scaffold).", LogLevel.Trace);
    }
}
