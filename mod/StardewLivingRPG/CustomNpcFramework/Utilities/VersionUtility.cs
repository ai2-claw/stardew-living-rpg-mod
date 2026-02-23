namespace StardewLivingRPG.CustomNpcFramework.Utilities;

internal static class VersionUtility
{
    public static bool TryParse(string? value, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!Version.TryParse(value.Trim(), out var parsed) || parsed is null)
            return false;

        version = parsed;
        return true;
    }

    public static bool IsFrameworkVersionCompatible(string frameworkVersion, string minimumRequired)
    {
        if (!TryParse(frameworkVersion, out var current))
            return false;
        if (!TryParse(minimumRequired, out var required))
            return false;
        return current >= required;
    }
}

