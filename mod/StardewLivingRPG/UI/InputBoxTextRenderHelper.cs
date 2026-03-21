using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Runtime.InteropServices;

namespace StardewLivingRPG.UI;

internal readonly struct InputBoxTextLayout
{
    public InputBoxTextLayout(SpriteFont font, string visibleText)
    {
        Font = font;
        VisibleText = visibleText ?? string.Empty;
    }

    public SpriteFont Font { get; }
    public string VisibleText { get; }
}

internal static class InputBoxTextRenderHelper
{
    private const ushort PrimaryLangChinese = 0x04;
    private const ushort PrimaryLangJapanese = 0x11;
    private const ushort PrimaryLangKorean = 0x12;

    private static SpriteFont? _japaneseSmallFont;
    private static SpriteFont? _koreanSmallFont;
    private static SpriteFont? _chineseSmallFont;

    public static InputBoxTextLayout CreateLayout(string? text, float availableWidth)
    {
        var resolvedFont = ResolveFont(text);
        var visibleText = TrimToTrailingWidth(resolvedFont, text ?? string.Empty, availableWidth);
        return new InputBoxTextLayout(resolvedFont, visibleText);
    }

    private static SpriteFont ResolveFont(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return Game1.smallFont;

        if (ContainsHangul(text))
            return GetKoreanFont();

        if (ContainsJapaneseKana(text))
            return GetJapaneseFont();

        if (ContainsHan(text))
        {
            var language = ResolvePreferredHanLanguage();
            return language switch
            {
                LocalizedContentManager.LanguageCode.ko => GetKoreanFont(),
                LocalizedContentManager.LanguageCode.zh => GetChineseFont(),
                _ => GetJapaneseFont()
            };
        }

        return Game1.smallFont;
    }

    private static LocalizedContentManager.LanguageCode ResolvePreferredHanLanguage()
    {
        var keyboardLanguage = TryGetWindowsKeyboardLanguage();
        if (keyboardLanguage.HasValue)
            return keyboardLanguage.Value;

        var currentLanguage = LocalizedContentManager.CurrentLanguageCode;
        return currentLanguage switch
        {
            LocalizedContentManager.LanguageCode.ja => LocalizedContentManager.LanguageCode.ja,
            LocalizedContentManager.LanguageCode.ko => LocalizedContentManager.LanguageCode.ko,
            LocalizedContentManager.LanguageCode.zh => LocalizedContentManager.LanguageCode.zh,
            _ => LocalizedContentManager.LanguageCode.ja
        };
    }

    private static LocalizedContentManager.LanguageCode? TryGetWindowsKeyboardLanguage()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return null;

        try
        {
            var keyboardLayout = GetKeyboardLayout(0);
            var languageId = unchecked((ushort)((long)keyboardLayout & 0xFFFF));
            var primaryLanguage = (ushort)(languageId & 0x03FF);
            return primaryLanguage switch
            {
                PrimaryLangJapanese => LocalizedContentManager.LanguageCode.ja,
                PrimaryLangKorean => LocalizedContentManager.LanguageCode.ko,
                PrimaryLangChinese => LocalizedContentManager.LanguageCode.zh,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static SpriteFont GetJapaneseFont()
    {
        _japaneseSmallFont ??= LoadSmallFont(LocalizedContentManager.LanguageCode.ja);
        return _japaneseSmallFont;
    }

    private static SpriteFont GetKoreanFont()
    {
        _koreanSmallFont ??= LoadSmallFont(LocalizedContentManager.LanguageCode.ko);
        return _koreanSmallFont;
    }

    private static SpriteFont GetChineseFont()
    {
        _chineseSmallFont ??= LoadSmallFont(LocalizedContentManager.LanguageCode.zh);
        return _chineseSmallFont;
    }

    private static SpriteFont LoadSmallFont(LocalizedContentManager.LanguageCode language)
    {
        try
        {
            return Game1.content.Load<SpriteFont>("Fonts/SmallFont", language);
        }
        catch
        {
            return Game1.smallFont;
        }
    }

    private static string TrimToTrailingWidth(SpriteFont font, string text, float availableWidth)
    {
        if (string.IsNullOrEmpty(text) || availableWidth <= 0f)
            return string.Empty;

        if (font.MeasureString(text).X <= availableWidth)
            return text;

        var trimmed = text;
        while (trimmed.Length > 0 && font.MeasureString(trimmed).X > availableWidth)
            trimmed = trimmed[1..];

        return trimmed;
    }

    private static bool ContainsJapaneseKana(string text)
    {
        foreach (var ch in text)
        {
            if ((ch >= '\u3040' && ch <= '\u309F')
                || (ch >= '\u30A0' && ch <= '\u30FF')
                || (ch >= '\u31F0' && ch <= '\u31FF')
                || (ch >= '\uFF66' && ch <= '\uFF9D'))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsHangul(string text)
    {
        foreach (var ch in text)
        {
            if ((ch >= '\u1100' && ch <= '\u11FF')
                || (ch >= '\u3130' && ch <= '\u318F')
                || (ch >= '\uAC00' && ch <= '\uD7AF'))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsHan(string text)
    {
        foreach (var ch in text)
        {
            if ((ch >= '\u3400' && ch <= '\u4DBF')
                || (ch >= '\u4E00' && ch <= '\u9FFF')
                || (ch >= '\uF900' && ch <= '\uFAFF'))
            {
                return true;
            }
        }

        return false;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);
}
