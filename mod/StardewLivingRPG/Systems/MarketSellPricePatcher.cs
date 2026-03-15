using HarmonyLib;
using SObject = StardewValley.Object;

namespace StardewLivingRPG.Systems;

internal static class MarketSellPricePatcher
{
    public static void Apply(string uniqueId)
    {
        var harmony = new Harmony(uniqueId);

        harmony.Patch(
            original: AccessTools.Method(typeof(SObject), nameof(SObject.salePrice), new[] { typeof(bool) }),
            postfix: new HarmonyMethod(typeof(MarketSellPricePatcher), nameof(ObjectSalePricePostfix)));

        harmony.Patch(
            original: AccessTools.Method(typeof(SObject), nameof(SObject.sellToStorePrice), new[] { typeof(long) }),
            postfix: new HarmonyMethod(typeof(MarketSellPricePatcher), nameof(ObjectSellToStorePricePostfix)));
    }

    private static void ObjectSalePricePostfix(SObject __instance, ref int __result)
    {
        if (ModEntry.Current is null)
            return;

        __result = ModEntry.Current.ResolveAdjustedSellPrice(__instance, __result);
    }

    private static void ObjectSellToStorePricePostfix(SObject __instance, ref int __result)
    {
        if (ModEntry.Current is null)
            return;

        __result = ModEntry.Current.ResolveAdjustedSellPrice(__instance, __result);
    }
}
