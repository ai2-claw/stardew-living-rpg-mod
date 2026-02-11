using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace StardewLivingRPG.UI;

public sealed class NpcChatInputMenu : IClickableMenu
{
    private readonly string _npcName;
    private readonly Action<string> _onSend;
    private readonly Func<string?>? _pollIncoming;
    private readonly Func<bool>? _isThinking;
    private readonly List<string> _lines = new();
    private int _thinkFrame;
    private readonly TextBox _input;
    private readonly Rectangle _sendButton;
    private readonly Rectangle _cancelButton;

    public NpcChatInputMenu(string npcName, Action<string> onSend, Func<string?>? pollIncoming = null, Func<bool>? isThinking = null)
        : base(
            Game1.uiViewport.Width / 2 - 360,
            Game1.uiViewport.Height / 2 - 140,
            720,
            280,
            true)
    {
        _npcName = npcName;
        _onSend = onSend;
        _pollIncoming = pollIncoming;
        _isThinking = isThinking;

        _input = new TextBox(
            Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
            null,
            Game1.smallFont,
            Game1.textColor)
        {
            X = xPositionOnScreen + 26,
            Y = yPositionOnScreen + 112,
            Width = width - 52,
            Height = 44,
            Selected = true
        };

        Game1.keyboardDispatcher.Subscriber = _input;

        _sendButton = new Rectangle(xPositionOnScreen + width - 220, yPositionOnScreen + height - 54, 90, 34);
        _cancelButton = new Rectangle(xPositionOnScreen + width - 118, yPositionOnScreen + height - 54, 90, 34);

        _lines.Add($"{_npcName}: I'm listening.");
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (_sendButton.Contains(x, y))
        {
            Submit();
            return;
        }

        if (_cancelButton.Contains(x, y))
        {
            CloseMenu();
            Game1.playSound("bigDeSelect");
            return;
        }

        _input.SelectMe();
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

        // Text input is handled through keyboard dispatcher subscriber.
    }

    public override void update(GameTime time)
    {
        base.update(time);
        _input.Update();

        _thinkFrame++;

        if (_pollIncoming is null)
            return;

        var safety = 0;
        while (safety < 4)
        {
            var next = _pollIncoming();
            if (string.IsNullOrWhiteSpace(next))
                break;

            _lines.Add($"{_npcName}: {next}");
            if (_lines.Count > 10)
                _lines.RemoveAt(0);
            safety++;
        }
    }

    public override void draw(SpriteBatch b)
    {
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        b.DrawString(Game1.dialogueFont, _npcName, new Vector2(xPositionOnScreen + 24, yPositionOnScreen + 24), Game1.textColor);
        b.DrawString(Game1.smallFont, "Let's just talk. What's on your mind?", new Vector2(xPositionOnScreen + 24, yPositionOnScreen + 66), Game1.textColor);

        var lineY = yPositionOnScreen + 88;
        foreach (var line in _lines.TakeLast(4))
        {
            var wrapped = line.Length > 100 ? line[..100] + "..." : line;
            b.DrawString(Game1.smallFont, wrapped, new Vector2(xPositionOnScreen + 24, lineY), Game1.textColor * 0.9f);
            lineY += 20;
        }

        if (_isThinking is not null && _isThinking())
        {
            var dots = (_thinkFrame / 20) % 3 + 1;
            var thinking = "Thinking" + new string('.', dots);
            b.DrawString(Game1.smallFont, thinking, new Vector2(xPositionOnScreen + 24, yPositionOnScreen + 152), Color.LightGoldenrodYellow);
        }

        _input.Y = yPositionOnScreen + 172;
        _input.Draw(b);

        DrawButton(b, _sendButton, "Send", enabled: true);
        DrawButton(b, _cancelButton, "Cancel", enabled: true);

        drawMouse(b);
    }

    private void Submit()
    {
        var text = _input.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            Game1.playSound("cancel");
            return;
        }

        _lines.Add($"You: {text}");
        if (_lines.Count > 10)
            _lines.RemoveAt(0);

        _onSend(text);
        _input.Text = string.Empty;
        Game1.playSound("smallSelect");
    }

    private static void DrawButton(SpriteBatch b, Rectangle rect, string text, bool enabled)
    {
        var bg = enabled ? Color.DarkSlateBlue * 0.7f : Color.DimGray * 0.45f;
        b.Draw(Game1.staminaRect, rect, bg);
        var size = Game1.smallFont.MeasureString(text);
        b.DrawString(Game1.smallFont, text, new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f), Color.White * (enabled ? 1f : 0.7f));
    }

    private void CloseMenu()
    {
        if (Game1.keyboardDispatcher.Subscriber == _input)
            Game1.keyboardDispatcher.Subscriber = null;

        base.exitThisMenuNoSound();
    }
}
