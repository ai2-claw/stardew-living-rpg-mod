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
        // M0 scaffold: sync config + advance day counter.
        state.ApplyConfig(_config);
        state.Calendar.Day += 1;

        // Basic telemetry heartbeat for now.
        state.Telemetry.Daily.WorldMutations += 1;

        _monitor.Log("DailyTickService.Run executed (scaffold).", LogLevel.Trace);
    }
}
