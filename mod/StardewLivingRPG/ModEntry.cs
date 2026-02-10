using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewLivingRPG.Config;
using StardewLivingRPG.State;
using StardewLivingRPG.Systems;

namespace StardewLivingRPG;

public sealed class ModEntry : Mod
{
    private ModConfig _config = new();
    private SaveState _state = SaveState.CreateDefault();
    private DailyTickService? _dailyTickService;

    public override void Entry(IModHelper helper)
    {
        _config = helper.ReadConfig<ModConfig>();
        _dailyTickService = new DailyTickService(Monitor, _config);

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayStarted += OnDayStarted;

        Monitor.Log("Stardew Living RPG loaded.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _state = StateStore.LoadOrCreate(Helper, Monitor);
        Monitor.Log($"State loaded (version={_state.Version}, mode={_state.Config.Mode}).", LogLevel.Info);
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        StateStore.Save(Helper, _state, Monitor);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (_dailyTickService is null)
            return;

        _dailyTickService.Run(_state);
        Monitor.Log($"Daily tick complete for day {_state.Calendar.Day} ({_state.Calendar.Season} Y{_state.Calendar.Year}).", LogLevel.Debug);
    }
}
