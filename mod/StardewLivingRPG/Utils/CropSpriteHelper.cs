using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace StardewLivingRPG.Utils;

public static class CropSpriteHelper
{
    // Maps crop names (lowercase) to their sprite indices in Game1.objectSpriteSheet
    private static readonly Dictionary<string, int> CropSpriteIndices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["parsnip"] = 16,
        ["potato"] = 121,
        ["cauliflower"] = 304,
        ["blueberry"] = 395,
        ["melon"] = 396,
        ["pumpkin"] = 272,
        ["cranberry"] = 416,
        ["corn"] = 86,
        ["wheat"] = 262,
        ["tomato"] = 392
    };

    public static bool TryGetSpriteIndex(string cropName, out int index)
        => CropSpriteIndices.TryGetValue(cropName, out index);

    public static void DrawCropSprite(SpriteBatch b, string cropName, Vector2 position, float scale = 1f)
    {
        if (!TryGetSpriteIndex(cropName, out var index))
            return;

        var sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, index, 16, 16);
        var destinationRect = new Rectangle((int)position.X, (int)position.Y, (int)(16 * scale), (int)(16 * scale));

        b.Draw(
            Game1.objectSpriteSheet,
            destinationRect,
            sourceRect,
            Color.White,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            0.85f
        );
    }

    public static void DrawCropSpriteWithBorder(SpriteBatch b, string cropName, Vector2 position, float scale = 2f)
    {
        var size = 16 * scale;
        var borderRect = new Rectangle((int)position.X - 2, (int)position.Y - 2, (int)size + 4, (int)size + 4);

        // Draw border
        b.Draw(Game1.staminaRect, borderRect, new Color(60, 40, 20));

        // Draw inner background
        var innerRect = new Rectangle(borderRect.X + 1, borderRect.Y + 1, borderRect.Width - 2, borderRect.Height - 2);
        b.Draw(Game1.staminaRect, innerRect, new Color(250, 228, 187));

        // Draw sprite
        var spritePos = new Vector2(position.X, position.Y);
        DrawCropSprite(b, cropName, spritePos, scale);
    }
}
