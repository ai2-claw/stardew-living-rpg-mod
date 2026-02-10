using Microsoft.Xna.Framework.Graphics;

namespace StardewLivingRPG.Utils;

public static class TextWrapHelper
{
    public static string[] WrapText(SpriteFont font, string text, float maxWidth)
    {
        if (font is null)
            throw new ArgumentNullException(nameof(font));

        if (string.IsNullOrWhiteSpace(text) || maxWidth <= 0)
            return new[] { text ?? string.Empty };

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = string.Empty;

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(current) ? word : current + " " + word;
            if (font.MeasureString(candidate).X <= maxWidth)
            {
                current = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(current))
            {
                lines.Add(current);
                current = word;
                continue;
            }

            // Very long token fallback: hard-split by characters.
            var chunk = string.Empty;
            foreach (var ch in word)
            {
                var next = chunk + ch;
                if (font.MeasureString(next).X > maxWidth && chunk.Length > 0)
                {
                    lines.Add(chunk);
                    chunk = ch.ToString();
                }
                else
                {
                    chunk = next;
                }
            }

            current = chunk;
        }

        if (!string.IsNullOrEmpty(current))
            lines.Add(current);

        return lines.Count == 0 ? new[] { string.Empty } : lines.ToArray();
    }
}
