using StardewModdingAPI;

namespace StardewLivingRPG.State;

public static class StateStore
{
    private const string DataKey = "mx146323.StardewLivingRPG.SaveState";
    private const string LegacyDataKey = "mx146323.StardewLivingRPG/SaveState";

    public static SaveState LoadOrCreate(IModHelper helper, IMonitor monitor)
    {
        try
        {
            var state = helper.Data.ReadSaveData<SaveState>(DataKey);
            if (state is not null)
                return state;

            // Legacy migration fallback (older invalid key format with slash).
            try
            {
                state = helper.Data.ReadSaveData<SaveState>(LegacyDataKey);
                if (state is not null)
                    return state;
            }
            catch
            {
                // Ignore invalid legacy key format on newer SMAPI validation.
            }

            return SaveState.CreateDefault();
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
