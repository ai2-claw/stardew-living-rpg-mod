using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewLivingRPG.Utils;
using System;
using System.Text.RegularExpressions;

namespace StardewLivingRPG.UI;

public sealed class NpcChatInputMenu : IClickableMenu
{
    private readonly string _npcName;
    private readonly Action<string> _onSend;
    private readonly Func<string?>? _pollIncoming;
    private readonly Func<bool>? _isThinking;

    private string? _lastNpcMessage;
    private string? _lastPlayerMessage;

    private int _thinkFrame;

    private readonly TextBox _input;
    private bool _inputHasFocus = true;

    private Rectangle _sendButtonBounds;
    private bool _sendButtonHovered;
    private readonly ClickableTextureComponent _closeButton;

    // Portrait Data
    private readonly Texture2D? _portraitTexture;
    private readonly Rectangle _portraitSource = new Rectangle(0, 0, 64, 64);

    // Layout constants
    private const int MenuWidth = 880;
    private const int MenuHeight = 620;
    private const int MainPadding = 50;

    // Regions
    private Rectangle _parchmentRegion;
    private Rectangle _portraitRegion;
    private Rectangle _chatRegion;
    private Rectangle _inputRegion;

    public NpcChatInputMenu(
        string npcName,
        Action<string> onSend,
        Func<string?>? pollIncoming = null,
        Func<bool>? isThinking = null)
        : base(
            Game1.uiViewport.Width / 2 - (MenuWidth / 2),
            Game1.uiViewport.Height / 2 - (MenuHeight / 2),
            MenuWidth,
            MenuHeight,
            true)
    {
        _npcName = npcName;
        _onSend = onSend;
        _pollIncoming = pollIncoming;
        _isThinking = isThinking;

        // --- LOAD PORTRAIT ---
        try
        {
            _portraitTexture = Game1.content.Load<Texture2D>($"Portraits\\{_npcName}");
        }
        catch
        {
            _portraitTexture = null;
        }

        // --- COMPONENTS (created once; positioned in RecalculateLayout) ---
        _input = new TextBox(
            Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
            null,
            Game1.smallFont,
            Color.Black)
        {
            Selected = true
        };
        _input.limitWidth = false;

        _closeButton = new ClickableTextureComponent(
            new Rectangle(0, 0, 48, 48), // temporary; positioned in RecalculateLayout
            Game1.mouseCursors,
            new Rectangle(337, 494, 12, 12),
            4f);

        RecalculateLayout();
        SetInputFocus(true);
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        RecalculateLayout();
    }

	private void RecalculateLayout()
	{
		// Recenter menu
		xPositionOnScreen = Game1.uiViewport.Width / 2 - (MenuWidth / 2);
		yPositionOnScreen = Game1.uiViewport.Height / 2 - (MenuHeight / 2);
		width = MenuWidth;
		height = MenuHeight;

		int gapBetweenParchmentAndInput = 14;
		int inputHeight = 68;
		int sendButtonWidth = 100;
		int gap = 20;

		// Move parchment DOWN only (adjust 12..32 to taste)
		int parchmentYOffset = 60;

		// Normal inner bounds (no FrameInset)
		int innerX = xPositionOnScreen + MainPadding;
		int innerY = yPositionOnScreen + MainPadding;
		int innerWidth = width - (MainPadding * 2);
		int innerHeight = height - (MainPadding * 2);

		// Parchment height fills top portion, accounting for the downward shift
		int parchmentHeight =
			innerHeight
			- inputHeight
			- gapBetweenParchmentAndInput
			- parchmentYOffset;

		_parchmentRegion = new Rectangle(
			innerX,
			innerY + parchmentYOffset,
			innerWidth,
			parchmentHeight
		);

		// Portrait (left inside parchment)
		int portraitSize = 256;
		int portraitMargin = 32;
		_portraitRegion = new Rectangle(
			_parchmentRegion.X + portraitMargin,
			_parchmentRegion.Y + (_parchmentRegion.Height - portraitSize) / 2,
			portraitSize,
			portraitSize
		);

		// Chat region (right of portrait)
		int chatX = _portraitRegion.Right + 32;
		_chatRegion = new Rectangle(
			chatX,
			_parchmentRegion.Y + 32,
			_parchmentRegion.Right - chatX - 32,
			_parchmentRegion.Height - 64
		);

		// Input region below parchment
		_inputRegion = new Rectangle(
			innerX,
			_parchmentRegion.Bottom + gapBetweenParchmentAndInput,
			innerWidth - sendButtonWidth - gap,
			inputHeight
		);

		// Update TextBox bounds
		_input.X = _inputRegion.X;
		_input.Y = _inputRegion.Y;
		_input.Width = _inputRegion.Width;
		_input.Height = _inputRegion.Height;

		// Close button (top-right, slightly outside like vanilla)
		_closeButton.bounds = new Rectangle(
			xPositionOnScreen + width - 48,
			yPositionOnScreen + 64, 
			48,
			48
		);

		// Send button bounds
		_sendButtonBounds = new Rectangle(
			_inputRegion.Right + gap,
			_inputRegion.Y,
			sendButtonWidth,
			_inputRegion.Height
		);
	}

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (_sendButtonBounds.Contains(x, y))
        {
            Submit();
            SetInputFocus(true);
            return;
        }

        if (_closeButton.containsPoint(x, y))
        {
            CloseMenu();
            Game1.playSound("bigDeSelect");
            return;
        }

        if (_inputRegion.Contains(x, y))
            SetInputFocus(true);
        else
            SetInputFocus(false);
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        _sendButtonHovered = _sendButtonBounds.Contains(x, y);
        _closeButton.tryHover(x, y);
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Enter)
        {
            Submit();
            return;
        }

        if (key == Keys.Escape)
        {
            CloseMenu();
            Game1.playSound("bigDeSelect");
            return;
        }
    }

    public override void update(GameTime time)
    {
        base.update(time);
        _input.Update();

        if (_input.Selected != _inputHasFocus)
            _input.Selected = _inputHasFocus;

        if (_inputHasFocus)
        {
            if (Game1.keyboardDispatcher.Subscriber != _input)
                Game1.keyboardDispatcher.Subscriber = _input;
        }

        _thinkFrame++;

        if (_pollIncoming is not null)
        {
            var next = _pollIncoming();
            if (!string.IsNullOrWhiteSpace(next))
            {
                var clean = CleanIncomingNpcMessage(next);
                _lastNpcMessage = clean;
            }
        }
    }

    private string CleanIncomingNpcMessage(string raw)
    {
        var clean = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(clean))
            return string.Empty;

        while (true)
        {
            var tagMatch = Regex.Match(clean, @"^\<[^>]+\>\s*", RegexOptions.CultureInvariant);
            if (!tagMatch.Success || tagMatch.Length <= 0)
                break;
            clean = clean[tagMatch.Length..].TrimStart();
        }

        if (!string.IsNullOrWhiteSpace(_npcName))
        {
            var label = _npcName.Trim();
            if (!string.IsNullOrWhiteSpace(label)
                && clean.StartsWith(label + ":", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean[(label.Length + 1)..].TrimStart();
            }
        }
        return clean;
    }

    public override void draw(SpriteBatch b)
    {
        // 1. Main Background Box
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        // 2. Parchment Area
        DrawParchment(b);

        // 3. Portrait
        DrawPortrait(b);

        // 4. Conversation Text
        DrawConversationText(b);

        // 5. Input Field
        DrawInputBox(b);

        // 6. Buttons
        _closeButton.draw(b);

        // SEND button (texture box) with a 1px "press" effect when hovered
        int press = _sendButtonHovered ? 1 : 0;

        IClickableMenu.drawTextureBox(
            b,
            Game1.menuTexture,
            new Rectangle(0, 256, 60, 60),
            _sendButtonBounds.X + press,
            _sendButtonBounds.Y + press,
            _sendButtonBounds.Width,
            _sendButtonBounds.Height,
            Color.White,
            1f,
            drawShadow: false
        );

        // SEND label (centered, also offset by press)
        string btnText = "SEND";
        Vector2 textSize = Game1.smallFont.MeasureString(btnText);
        Vector2 textPos = new Vector2(
            _sendButtonBounds.X + press + (_sendButtonBounds.Width - textSize.X) / 2f,
            _sendButtonBounds.Y + press + (_sendButtonBounds.Height - Game1.smallFont.LineSpacing) / 2f
        );

        Color btnTextColor = _sendButtonHovered ? Game1.textColor : (Game1.textColor * 0.85f);
        Utility.drawTextWithShadow(b, btnText, Game1.smallFont, textPos, btnTextColor);

        drawMouse(b);
    }

    private void DrawParchment(SpriteBatch b)
    {
        // Paper color
        b.Draw(Game1.staminaRect, _parchmentRegion, new Color(250, 227, 180));

        // Decorative Border
        int border = 2;
        Color borderColor = new Color(104, 46, 41);
        b.Draw(Game1.staminaRect, new Rectangle(_parchmentRegion.X, _parchmentRegion.Y, _parchmentRegion.Width, border), borderColor);
        b.Draw(Game1.staminaRect, new Rectangle(_parchmentRegion.X, _parchmentRegion.Bottom - border, _parchmentRegion.Width, border), borderColor);
        b.Draw(Game1.staminaRect, new Rectangle(_parchmentRegion.X, _parchmentRegion.Y, border, _parchmentRegion.Height), borderColor);
        b.Draw(Game1.staminaRect, new Rectangle(_parchmentRegion.Right - border, _parchmentRegion.Y, border, _parchmentRegion.Height), borderColor);
    }

    private void DrawPortrait(SpriteBatch b)
    {
        // Dark background behind portrait
        b.Draw(Game1.staminaRect, _portraitRegion, new Color(133, 89, 56));

        if (_portraitTexture != null)
        {
            b.Draw(_portraitTexture, _portraitRegion, _portraitSource, Color.White);
        }

        // Portrait Frame
        int t = 4;
        Color frameColor = new Color(221, 148, 25); // Gold

        // Outer dark frame
        b.Draw(Game1.staminaRect, new Rectangle(_portraitRegion.X - t, _portraitRegion.Y - t, _portraitRegion.Width + t * 2, t), Color.SaddleBrown);
        b.Draw(Game1.staminaRect, new Rectangle(_portraitRegion.X - t, _portraitRegion.Bottom, _portraitRegion.Width + t * 2, t), Color.SaddleBrown);
        b.Draw(Game1.staminaRect, new Rectangle(_portraitRegion.X - t, _portraitRegion.Y - t, t, _portraitRegion.Height + t * 2), Color.SaddleBrown);
        b.Draw(Game1.staminaRect, new Rectangle(_portraitRegion.Right, _portraitRegion.Y - t, t, _portraitRegion.Height + t * 2), Color.SaddleBrown);

        // Inner gold frame
        int inset = -2;
        b.Draw(Game1.staminaRect, new Rectangle(_portraitRegion.X + inset, _portraitRegion.Y + inset, _portraitRegion.Width - inset * 2, 2), frameColor);
        b.Draw(Game1.staminaRect, new Rectangle(_portraitRegion.X + inset, _portraitRegion.Bottom - inset - 2, _portraitRegion.Width - inset * 2, 2), frameColor);
    }

    private void DrawConversationText(SpriteBatch b)
    {
        float x = _chatRegion.X;
        float y = _chatRegion.Y;
        float w = _chatRegion.Width;

        Color npcColor = new Color(60, 40, 20);      // Dark Ink
        Color playerColor = new Color(110, 110, 110); // Faded gray for history

        // 1. Player Message
        if (_lastPlayerMessage != null)
        {
            string label = "You: ";
            Vector2 labelSize = Game1.smallFont.MeasureString(label);

            b.DrawString(Game1.smallFont, label, new Vector2(x, y), playerColor);

            float contentWidth = w - labelSize.X;
            var playerLines = TextWrapHelper.WrapText(Game1.smallFont, _lastPlayerMessage, contentWidth);

            for (int i = 0; i < playerLines.Length; i++)
            {
                float drawX = (i == 0) ? x + labelSize.X : x;
                b.DrawString(Game1.smallFont, playerLines[i], new Vector2(drawX, y), playerColor);
                y += Game1.smallFont.LineSpacing;
            }
            y += 16;
        }

        // Separator
        if (_lastPlayerMessage != null && (_lastNpcMessage != null || IsThinking()))
        {
            b.Draw(Game1.staminaRect, new Rectangle((int)x, (int)y - 8, (int)w, 1), Color.BurlyWood * 0.8f);
        }

        // 2. NPC Message
        if (_lastNpcMessage != null)
        {
            var npcLines = TextWrapHelper.WrapText(Game1.smallFont, _lastNpcMessage, w);
            foreach (var line in npcLines)
            {
                b.DrawString(Game1.smallFont, line, new Vector2(x, y), npcColor);
                y += Game1.smallFont.LineSpacing + 4;
            }
        }
        else if (IsThinking())
        {
            int dots = (_thinkFrame / 20) % 4;
            string text = "Thinking" + new string('.', dots);
            b.DrawString(Game1.smallFont, text, new Vector2(x, y), npcColor * 0.6f);
        }
    }

    private bool IsThinking()
    {
        return _isThinking != null && _isThinking();
    }

    private void DrawInputBox(SpriteBatch b)
    {
        IClickableMenu.drawTextureBox(
            b,
            Game1.menuTexture,
            new Rectangle(0, 256, 60, 60),
            _inputRegion.X,
            _inputRegion.Y,
            _inputRegion.Width,
            _inputRegion.Height,
            Color.White,
            1f,
            drawShadow: false
        );

        string text = _input.Text ?? "";

        float textWidth = _inputRegion.Width - 32;
        var lines = TextWrapHelper.WrapText(Game1.smallFont, text, textWidth);

        int maxLines = 1;
        int startLine = Math.Max(0, lines.Length - maxLines);

        float textX = _inputRegion.X + 16;
        float textY = _inputRegion.Y + 14;

        for (int i = startLine; i < lines.Length; i++)
        {
            b.DrawString(Game1.smallFont, lines[i], new Vector2(textX, textY), Color.Black);
            textY += Game1.smallFont.LineSpacing;
        }

        // Caret / Cursor
        if (_inputHasFocus && (_thinkFrame / 30) % 2 == 0)
        {
            string lastLine = lines.Length > 0 ? lines[^1] : "";
            float cursorX = textX + Game1.smallFont.MeasureString(lastLine).X + 2;

            float cursorY = textY - Game1.smallFont.LineSpacing + 2;
            if (lines.Length == 0)
                cursorY = _inputRegion.Y + 12;

            b.Draw(Game1.staminaRect, new Rectangle((int)cursorX, (int)cursorY, 2, Game1.smallFont.LineSpacing - 2), Color.Black);
        }
    }

    private void Submit()
    {
        var text = _input.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            Game1.playSound("cancel");
            return;
        }

        _lastPlayerMessage = text;
        _lastNpcMessage = null;

        _onSend(text);
        _input.Text = string.Empty;
        Game1.playSound("smallSelect");
    }

    private void CloseMenu()
    {
        if (Game1.keyboardDispatcher.Subscriber == _input)
            Game1.keyboardDispatcher.Subscriber = null;

        base.exitThisMenuNoSound();
    }

    private void SetInputFocus(bool focused)
    {
        _inputHasFocus = focused;
        _input.Selected = focused;
        if (focused)
        {
            _input.SelectMe();
            if (Game1.keyboardDispatcher.Subscriber != _input)
                Game1.keyboardDispatcher.Subscriber = _input;
        }
        else if (Game1.keyboardDispatcher.Subscriber == _input)
        {
            Game1.keyboardDispatcher.Subscriber = null;
        }
    }
}
