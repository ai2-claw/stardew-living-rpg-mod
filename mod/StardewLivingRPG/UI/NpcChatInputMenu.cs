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
    private readonly TextBox _input;
    private readonly Rectangle _sendButton;
    private readonly Rectangle _cancelButton;

    public NpcChatInputMenu(string npcName, Action<string> onSend)
        : base(
            Game1.uiViewport.Width / 2 - 360,
            Game1.uiViewport.Height / 2 - 140,
            720,
            280,
            true)
    {
        _npcName = npcName;
        _onSend = onSend;

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
    }

    public override void draw(SpriteBatch b)
    {
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        b.DrawString(Game1.dialogueFont, _npcName, new Vector2(xPositionOnScreen + 24, yPositionOnScreen + 24), Game1.textColor);
        b.DrawString(Game1.smallFont, "I was just thinking about my next project, but I can always make time for a friend.", new Vector2(xPositionOnScreen + 24, yPositionOnScreen + 66), Game1.textColor);
        b.DrawString(Game1.smallFont, "What's on your mind?", new Vector2(xPositionOnScreen + 24, yPositionOnScreen + 88), Game1.textColor);

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

        _onSend(text);
        Game1.playSound("smallSelect");
        CloseMenu();
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
