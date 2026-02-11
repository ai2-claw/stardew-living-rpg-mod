using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewLivingRPG.Utils;
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

    private readonly ClickableTextureComponent _sendButton;
    private readonly ClickableTextureComponent _closeButton;

    // Layout constants
    private const int Padding = 48;
    private const int TextTopMargin = 110;
    private const int InputHeight = 48;

    public NpcChatInputMenu(string npcName, Action<string> onSend, Func<string?>? pollIncoming = null, Func<bool>? isThinking = null)
        : base(
            Game1.uiViewport.Width / 2 - 400,
            Game1.uiViewport.Height / 2 - 200,
            800,
            400,
            true)
    {
        _npcName = npcName;
        _onSend = onSend;
        _pollIncoming = pollIncoming;
        _isThinking = isThinking;

        int bottomAreaY = yPositionOnScreen + height - InputHeight - 32;

        _input = new TextBox(
            Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
            null,
            Game1.smallFont,
            Game1.textColor)
        {
            X = xPositionOnScreen + Padding,
            Y = bottomAreaY,
            Width = width - (Padding * 2) - 100, // Reduced width slightly to give button more room
            Height = InputHeight,
            Selected = true
        };

        Game1.keyboardDispatcher.Subscriber = _input;

        // --- FIX 1: CLOSE BUTTON POSITION ---
        // Moved to (Width - 20, Y - 20) to hang off the top-right corner properly
        _closeButton = new ClickableTextureComponent(
            new Rectangle(xPositionOnScreen + width - 60, yPositionOnScreen + 80, 48, 48),
            Game1.mouseCursors,
            new Rectangle(337, 494, 12, 12),
            4f);

        // --- FIX 2: BLANK SEND BUTTON ---
        // Switched source rect to (294, 428, 21, 11). 
        // This is a generic blank button texture used in the co-op menus.
        _sendButton = new ClickableTextureComponent(
            new Rectangle(xPositionOnScreen + width - 140, bottomAreaY - 1, 84, 44), // Adjusted size/pos
            Game1.mouseCursors,
            new Rectangle(294, 428, 21, 11), 
            4f); // Scale 4x to make it chunky
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (_sendButton.containsPoint(x, y))
        {
            Submit();
            return;
        }

        if (_closeButton.containsPoint(x, y))
        {
            CloseMenu();
            Game1.playSound("bigDeSelect");
            return;
        }

        _input.SelectMe();
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        _sendButton.tryHover(x, y);
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
        _thinkFrame++;

        if (_pollIncoming is not null)
        {
            var next = _pollIncoming();
            if (!string.IsNullOrWhiteSpace(next))
            {
                var clean = Regex.Replace(next, @"^(\<[^>]+>|[^:]+:)\s*", "").Trim();
                _lastNpcMessage = clean;
            }
        }
    }

    public override void draw(SpriteBatch b)
    {
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        var x = xPositionOnScreen + Padding;
        var y = (float)(yPositionOnScreen + TextTopMargin);
        var messageAreaBottom = _input.Y - 20;
        var maxWidth = width - (Padding * 2);

        // --- DRAW PLAYER MESSAGE ---
        if (_lastPlayerMessage is not null)
        {
            DrawMessageLines(b, _lastPlayerMessage!, true, x, ref y, maxWidth, messageAreaBottom);
            y += 16; 
        }

        // --- FIX 3: HORIZONTAL SEPARATOR ---
        // Only draw if we have both a player message AND an NPC message (or thinking)
        if (_lastPlayerMessage is not null && (_lastNpcMessage is not null || _isThinking?.Invoke() == true))
        {
            // Draw a subtle dark line
            b.Draw(Game1.staminaRect, 
                new Rectangle((int)x, (int)y - 8, (int)maxWidth, 2), 
                Game1.textColor * 0.2f);
            
            y += 8; // Extra spacing after line
        }

        // --- DRAW NPC MESSAGE ---
        if (_lastNpcMessage is not null)
        {
            DrawMessageLines(b, _lastNpcMessage!, false, x, ref y, maxWidth, messageAreaBottom);
            y += 12;
        }

        // --- DRAW THINKING ---
        if (_lastNpcMessage == null && _isThinking is not null && _isThinking() && y + Game1.smallFont.LineSpacing <= messageAreaBottom)
        {
            var dots = (_thinkFrame / 20) % 4;
            var thinkingText = string.Concat("Thinking", new string('.', dots));
            b.DrawString(Game1.smallFont, thinkingText, new Vector2(x, y), Game1.textColor * 0.7f);
        }

        _input.Draw(b);

        _closeButton.draw(b);
        _sendButton.draw(b);

        drawMouse(b);
    }

    private void DrawMessageLines(SpriteBatch b, string message, bool isPlayer, float x, ref float y, float maxWidth, float maxY)
    {
        var wrapped = TextWrapHelper.WrapText(Game1.smallFont, message, maxWidth);
        
        // Player text is slightly dimmer to differentiate
        Color color = isPlayer ? Game1.textColor * 0.75f : Game1.textColor;

        foreach (var line in wrapped)
        {
            if (y + Game1.smallFont.LineSpacing > maxY)
                break;

            b.DrawString(Game1.smallFont, line, new Vector2(x, y), color);
            y += Game1.smallFont.LineSpacing;
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
}