using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewLivingRPG.Utils;
using System;

namespace StardewLivingRPG.UI;

public sealed class Player2DeviceAuthMenu : IClickableMenu
{
    private readonly Func<string> _getVerificationUrl;
    private readonly Func<string> _getUserCode;
    private readonly Func<string> _getStatus;
    private readonly Action _onOpenBrowser;
    private readonly Action _onCopyCode;
    private readonly Action _onCancel;
    private readonly ClickableTextureComponent _closeButton;

    private Rectangle _openBrowserButton;
    private Rectangle _copyCodeButton;
    private Rectangle _cancelButton;
    private bool _openHovered;
    private bool _copyHovered;
    private bool _cancelHovered;

    public Player2DeviceAuthMenu(
        Func<string> getVerificationUrl,
        Func<string> getUserCode,
        Func<string> getStatus,
        Action onOpenBrowser,
        Action onCopyCode,
        Action onCancel)
        : base(
            Game1.uiViewport.Width / 2 - 360,
            Game1.uiViewport.Height / 2 - 220,
            720,
            440,
            true)
    {
        _getVerificationUrl = getVerificationUrl;
        _getUserCode = getUserCode;
        _getStatus = getStatus;
        _onOpenBrowser = onOpenBrowser;
        _onCopyCode = onCopyCode;
        _onCancel = onCancel;

        _closeButton = new ClickableTextureComponent(
            new Rectangle(xPositionOnScreen + width - 60, yPositionOnScreen + 24, 48, 48),
            Game1.mouseCursors,
            new Rectangle(337, 494, 12, 12),
            4f);

        RebuildLayout();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        xPositionOnScreen = Game1.uiViewport.Width / 2 - 360;
        yPositionOnScreen = Game1.uiViewport.Height / 2 - 220;
        width = 720;
        height = 440;
        RebuildLayout();
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        _openHovered = _openBrowserButton.Contains(x, y);
        _copyHovered = _copyCodeButton.Contains(x, y);
        _cancelHovered = _cancelButton.Contains(x, y);
        _closeButton.tryHover(x, y);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (_closeButton.containsPoint(x, y) || _cancelButton.Contains(x, y))
        {
            _onCancel();
            exitThisMenuNoSound();
            Game1.playSound("bigDeSelect");
            return;
        }

        if (_openBrowserButton.Contains(x, y))
        {
            _onOpenBrowser();
            Game1.playSound("smallSelect");
            return;
        }

        if (_copyCodeButton.Contains(x, y))
        {
            _onCopyCode();
            Game1.playSound("smallSelect");
        }
    }

    public override void receiveKeyPress(Microsoft.Xna.Framework.Input.Keys key)
    {
        if (key == Microsoft.Xna.Framework.Input.Keys.Escape)
        {
            _onCancel();
            exitThisMenuNoSound();
            Game1.playSound("bigDeSelect");
            return;
        }

        base.receiveKeyPress(key);
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        SpriteText.drawStringWithScrollCenteredAt(
            b,
            I18n.Get("player2_auth.title", "Local Insight Sign-In"),
            xPositionOnScreen + (width / 2),
            yPositionOnScreen + 18);

        var bodyX = xPositionOnScreen + 56;
        var bodyY = yPositionOnScreen + 98;
        var bodyWidth = width - 112;

        b.DrawString(
            Game1.smallFont,
            I18n.Get("player2_auth.instructions.open_browser", "Open this page in your browser:"),
            new Vector2(bodyX, bodyY),
            Game1.textColor);
        bodyY += 34;

        DrawBoxedLines(b, TextWrapHelper.WrapText(Game1.smallFont, _getVerificationUrl(), bodyWidth - 24), bodyX, bodyY, bodyWidth, 66);
        bodyY += 92;

        var userCode = _getUserCode();
        b.DrawString(
            Game1.smallFont,
            I18n.Get("player2_auth.instructions.enter_code", $"Enter code: {userCode}", new { code = userCode }),
            new Vector2(bodyX, bodyY),
            new Color(62, 44, 18));
        bodyY += 42;

        DrawBoxedLines(b, TextWrapHelper.WrapText(Game1.smallFont, _getStatus(), bodyWidth - 24), bodyX, bodyY, bodyWidth, 74);

        DrawButton(b, _openBrowserButton, I18n.Get("player2_auth.button.open_browser", "Open Browser"), _openHovered);
        DrawButton(b, _copyCodeButton, I18n.Get("player2_auth.button.copy_code", "Copy Code"), _copyHovered);
        DrawButton(b, _cancelButton, I18n.Get("player2_auth.button.cancel", "Cancel"), _cancelHovered);
        _closeButton.draw(b);

        drawMouse(b);
    }

    private void RebuildLayout()
    {
        _closeButton.bounds = new Rectangle(xPositionOnScreen + width - 60, yPositionOnScreen + 24, 48, 48);

        var buttonY = yPositionOnScreen + height - 74;
        const int buttonWidth = 180;
        const int gap = 18;
        var totalWidth = (buttonWidth * 3) + (gap * 2);
        var startX = xPositionOnScreen + (width - totalWidth) / 2;

        _openBrowserButton = new Rectangle(startX, buttonY, buttonWidth, 46);
        _copyCodeButton = new Rectangle(_openBrowserButton.Right + gap, buttonY, buttonWidth, 46);
        _cancelButton = new Rectangle(_copyCodeButton.Right + gap, buttonY, buttonWidth, 46);
    }

    private static void DrawBoxedLines(SpriteBatch b, string[] lines, int x, int y, int width, int minHeight)
    {
        var lineHeight = Game1.smallFont.LineSpacing + 2;
        var contentHeight = Math.Max(minHeight, (lines.Length * lineHeight) + 20);
        IClickableMenu.drawTextureBox(
            b,
            Game1.mouseCursors,
            new Rectangle(384, 396, 15, 15),
            x,
            y,
            width,
            contentHeight,
            Color.White,
            4f,
            false);

        var textY = y + 10;
        foreach (var line in lines)
        {
            b.DrawString(Game1.smallFont, line, new Vector2(x + 12, textY), Game1.textColor);
            textY += lineHeight;
        }
    }

    private static void DrawButton(SpriteBatch b, Rectangle bounds, string label, bool hovered)
    {
        var tint = hovered ? new Color(255, 255, 255) : Color.White;
        IClickableMenu.drawTextureBox(
            b,
            Game1.mouseCursors,
            new Rectangle(432, 439, 9, 9),
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            tint,
            4f,
            false);

        var size = Game1.smallFont.MeasureString(label);
        var pos = new Vector2(
            bounds.X + (bounds.Width - size.X) / 2f,
            bounds.Y + (bounds.Height - size.Y) / 2f);
        b.DrawString(Game1.smallFont, label, pos, Game1.textColor);
    }
}
