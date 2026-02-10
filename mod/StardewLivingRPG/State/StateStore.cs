using StardewModdingAPI;

namespace StardewLivingRPG.State;

public static class StateStore
{
    private const string DataKey = "mx146323.StardewLivingRPG/SaveState";

    public static SaveState LoadOrCreate(IModHelper helper, IMonitor monitor)
    {
        try
        {
            return helper.Data.ReadSaveData<SaveState>(DataKey) ?? SaveState.CreateDefault();
        }
        catch (Exception ex)
        {
            monitor.Log($"Failed to load save state, using defaults: {ex.Message}", LogLevel.Warn);
            return SaveState.CreateDefault();
        }
    }

    public static void Save(IModHelper helper, SaveState state, IMonitor monitor)
    {
        try
        {
            helper.Data.WriteSaveData(DataKey, state);
        }
        catch (Exception ex)
        {
            monitor.Log($"Failed to save state: {ex.Message}", LogLevel.Error);
        }
    }
}
