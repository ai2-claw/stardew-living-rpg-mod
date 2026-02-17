using StardewValley;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace StardewLivingRPG.Utils;

public sealed class VanillaCropEntry
{
    public int ObjectId { get; init; }
    public int BasePrice { get; init; }
}

public static class VanillaCropCatalog
{
    private const int VegetableCategory = -75;
    private const int FruitCategory = -79;
    private const int EggCategory = -5;
    private const int MilkCategory = -6;

    private static readonly object Sync = new();
    private static Dictionary<string, VanillaCropEntry>? _cachedEntries;

    // Safe fallback when runtime object data is unavailable.
    private static readonly Dictionary<string, VanillaCropEntry> FallbackEntries = new(StringComparer.OrdinalIgnoreCase)
    {
        ["parsnip"] = new() { ObjectId = 24, BasePrice = 35 },
        ["potato"] = new() { ObjectId = 192, BasePrice = 80 },
        ["cauliflower"] = new() { ObjectId = 190, BasePrice = 175 },
        ["blueberry"] = new() { ObjectId = 258, BasePrice = 50 },
        ["melon"] = new() { ObjectId = 254, BasePrice = 250 },
        ["pumpkin"] = new() { ObjectId = 276, BasePrice = 320 },
        ["cranberry"] = new() { ObjectId = 282, BasePrice = 75 },
        ["corn"] = new() { ObjectId = 270, BasePrice = 50 },
        ["wheat"] = new() { ObjectId = 262, BasePrice = 25 },
        ["tomato"] = new() { ObjectId = 256, BasePrice = 60 },
        ["egg"] = new() { ObjectId = 176, BasePrice = 50 },
        ["large_egg"] = new() { ObjectId = 174, BasePrice = 95 },
        ["brown_egg"] = new() { ObjectId = 180, BasePrice = 50 },
        ["large_brown_egg"] = new() { ObjectId = 182, BasePrice = 95 },
        ["duck_egg"] = new() { ObjectId = 442, BasePrice = 95 },
        ["void_egg"] = new() { ObjectId = 305, BasePrice = 65 },
        ["milk"] = new() { ObjectId = 184, BasePrice = 125 },
        ["large_milk"] = new() { ObjectId = 186, BasePrice = 190 },
        ["goat_milk"] = new() { ObjectId = 436, BasePrice = 225 },
        ["large_goat_milk"] = new() { ObjectId = 438, BasePrice = 345 }
    };

    public static IReadOnlyDictionary<string, VanillaCropEntry> GetEntries()
    {
        if (_cachedEntries is not null)
            return _cachedEntries;

        lock (Sync)
        {
            if (_cachedEntries is not null)
                return _cachedEntries;

            var resolved = BuildFromRuntimeObjectData();
            if (resolved.Count == 0)
            {
                _cachedEntries = new Dictionary<string, VanillaCropEntry>(FallbackEntries, StringComparer.OrdinalIgnoreCase);
                return _cachedEntries;
            }

            foreach (var (name, fallback) in FallbackEntries)
            {
                if (!resolved.ContainsKey(name))
                    resolved[name] = fallback;
            }

            _cachedEntries = resolved;
            return _cachedEntries;
        }
    }

    public static string NormalizeCropKey(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return string.Empty;

        var key = rawName.Trim().ToLowerInvariant();
        key = Regex.Replace(key, @"[^a-z0-9]+", "_", RegexOptions.CultureInvariant);
        return key.Trim('_');
    }

    private static Dictionary<string, VanillaCropEntry> BuildFromRuntimeObjectData()
    {
        var resolved = new Dictionary<string, VanillaCropEntry>(StringComparer.OrdinalIgnoreCase);
        var objectData = TryGetGame1ObjectData();
        if (objectData is not IDictionary dictionary)
            return resolved;

        foreach (DictionaryEntry entry in dictionary)
        {
            if (entry.Value is null)
                continue;

            if (!TryReadIntMember(entry.Value, "Category", out var category))
                continue;

            if (category != VegetableCategory
                && category != FruitCategory
                && category != EggCategory
                && category != MilkCategory)
                continue;

            if (!TryReadIntMember(entry.Value, "Price", out var basePrice) || basePrice <= 0)
                continue;

            var cropName = TryReadStringMember(entry.Value, "Name", "DisplayName");
            if (string.IsNullOrWhiteSpace(cropName))
                continue;

            var keyText = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
            if (!int.TryParse(keyText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var objectId))
                continue;

            var cropKey = NormalizeCropKey(cropName);
            if (string.IsNullOrWhiteSpace(cropKey) || resolved.ContainsKey(cropKey))
                continue;

            resolved[cropKey] = new VanillaCropEntry
            {
                ObjectId = objectId,
                BasePrice = basePrice
            };
        }

        return resolved;
    }

    private static object? TryGetGame1ObjectData()
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.IgnoreCase;
        var prop = typeof(Game1).GetProperty("objectData", flags);
        if (prop is not null)
            return prop.GetValue(null);

        var field = typeof(Game1).GetField("objectData", flags);
        return field?.GetValue(null);
    }

    private static bool TryReadIntMember(object source, string memberName, out int value)
    {
        value = 0;
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;

        var prop = source.GetType().GetProperty(memberName, flags);
        if (prop is not null)
        {
            var obj = prop.GetValue(source);
            if (obj is not null && int.TryParse(Convert.ToString(obj, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return true;
        }

        var field = source.GetType().GetField(memberName, flags);
        if (field is not null)
        {
            var obj = field.GetValue(source);
            if (obj is not null && int.TryParse(Convert.ToString(obj, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return true;
        }

        return false;
    }

    private static string? TryReadStringMember(object source, params string[] memberNames)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;
        foreach (var memberName in memberNames)
        {
            var prop = source.GetType().GetProperty(memberName, flags);
            if (prop is not null)
            {
                var text = Convert.ToString(prop.GetValue(source), CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }

            var field = source.GetType().GetField(memberName, flags);
            if (field is not null)
            {
                var text = Convert.ToString(field.GetValue(source), CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }

        return null;
    }
}
