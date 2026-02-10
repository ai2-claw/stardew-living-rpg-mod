namespace StardewLivingRPG.Systems;

public sealed class SalesIngestionService
{
    private readonly Dictionary<string, int> _pendingSales = new(StringComparer.OrdinalIgnoreCase);

    public void AddSale(string cropId, int count)
    {
        if (string.IsNullOrWhiteSpace(cropId) || count <= 0)
            return;

        _pendingSales.TryGetValue(cropId, out var prev);
        _pendingSales[cropId] = prev + count;
    }

    public Dictionary<string, int> DrainPendingSales()
    {
        var copy = new Dictionary<string, int>(_pendingSales, StringComparer.OrdinalIgnoreCase);
        _pendingSales.Clear();
        return copy;
    }
}
