using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System.Reflection;

namespace StardewLivingRPG.Systems;

internal static class NpcBubbleDrawPatcher
{
    private static readonly FieldInfo? TextAboveHeadField =
        AccessTools.Field(typeof(NPC), "textAboveHead");

    [ThreadStatic]
    private static string? _savedTextAboveHead;

    public static void Apply(string uniqueId)
    {
        var harmony = new Harmony(uniqueId);
        var drawMethod = typeof(NPC).GetMethod(
            "draw",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(SpriteBatch), typeof(float) },
            modifiers: null);

        if (drawMethod is null)
            return;

        harmony.Patch(
            original: drawMethod,
            prefix: new HarmonyMethod(typeof(NpcBubbleDrawPatcher), nameof(DrawPrefix)),
            postfix: new HarmonyMethod(typeof(NpcBubbleDrawPatcher), nameof(DrawPostfix)));
    }

    private static void DrawPrefix(NPC __instance)
    {
        _savedTextAboveHead = null;
        var text = TextAboveHeadField?.GetValue(__instance) as string;
        if (text is null || !text.Contains('\n'))
            return;

        _savedTextAboveHead = text;
        TextAboveHeadField?.SetValue(__instance, null);
    }

    private static void DrawPostfix(NPC __instance, SpriteBatch b)
    {
        if (_savedTextAboveHead is null)
            return;

        TextAboveHeadField?.SetValue(__instance, _savedTextAboveHead);
        var lines = _savedTextAboveHead.Split('\n');
        var box = __instance.GetBoundingBox();
        var local = Game1.GlobalToLocal(
            Game1.viewport,
            new Vector2(box.Center.X, box.Top - 128f));

        var lineHeight = SpriteText.getHeightOfString("A") + 4;
        var totalHeight = lines.Length * lineHeight;
        var startY = (int)local.Y - (totalHeight / 2);

        for (var i = 0; i < lines.Length; i++)
        {
            var lineY = startY + (i * lineHeight);
            SpriteText.drawStringWithScrollCenteredAt(
                b,
                lines[i],
                (int)local.X,
                lineY);
        }

        _savedTextAboveHead = null;
    }
}
