using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewLivingRPG.Utils;
using System;
namespace StardewLivingRPG.UI;

internal sealed class TownSquareMagicianInputMenu : IClickableMenu
{
    private const int InputWidth = 360;
    private const int InputHeight = 64;
    private const int SubmitWidth = 120;
    private const int Gap = 12;
    private const int Margin = 24;
    private const int BottomOffset = 140;
    private const int TextPaddingX = 16;
    private const int TextPaddingY = 14;
    private readonly Func<string, bool> _onSubmit;
    private readonly Action<bool> _onClose;
    private readonly DeferredTextBoxActionGate _textActionGate = new();
    private readonly TextBox _input;

    private Rectangle _inputBounds;
    private Rectangle _submitButtonBounds;
    private bool _submitButtonHovered;
    private bool _sessionEnded;
    private bool _closeHandled;
    private bool _playSoundOnClose;

    public TownSquareMagicianInputMenu(Func<string, bool> onSubmit, Action<bool> onClose)
        : base(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height, false)
    {
        _onSubmit = onSubmit;
        _onClose = onClose;
        _input = new TextBox(
            Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
            null,
            Game1.smallFont,
            Color.Black)
        {
            Selected = true
        };
        _input.limitWidth = false;
        RecalculateLayout();
        SetInputFocus(true);
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        width = newBounds.Width;
        height = newBounds.Height;
        RecalculateLayout();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);

        if (_sessionEnded)
        {
            SetInputFocus(false);
            return;
        }

        if (_submitButtonBounds.Contains(x, y))
        {
            Submit();
            return;
        }

        if (_inputBounds.Contains(x, y))
            SetInputFocus(true);
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        _submitButtonHovered = !_sessionEnded && _submitButtonBounds.Contains(x, y);
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Enter && !_sessionEnded)
        {
            _textActionGate.ArmSubmit(_input.Text);
            return;
        }

        if (key == Keys.Escape)
        {
            _textActionGate.ArmClose(_input.Text);
            return;
        }

    }

    public override void update(GameTime time)
    {
        base.update(time);
        _input.Update();

        if (_input.Selected && Game1.keyboardDispatcher.Subscriber != _input)
            Game1.keyboardDispatcher.Subscriber = _input;

        switch (_textActionGate.Update(_input.Text))
        {
            case DeferredTextAction.Submit:
                Submit();
                break;
            case DeferredTextAction.Close:
                CloseMenu(playSound: true);
                break;
        }
    }

    protected override void cleanupBeforeExit()
    {
        if (Game1.keyboardDispatcher.Subscriber == _input)
            Game1.keyboardDispatcher.Subscriber = null;
        base.cleanupBeforeExit();

        if (_closeHandled)
            return;

        _closeHandled = true;
        _textActionGate.Clear();
        SetInputFocus(false);
        _onClose(_playSoundOnClose);
    }

    public override void draw(SpriteBatch b)
    {
        var mousePoint = new Point(Game1.getMouseX(), Game1.getMouseY());
        DrawInputBox(b, _inputBounds, !_sessionEnded);
        DrawSubmitButton(
            b,
            _submitButtonBounds,
            I18n.Get("magician.menu.button.submit", "Submit"),
            _submitButtonBounds.Contains(mousePoint) && _submitButtonHovered,
            !_sessionEnded);
        drawMouse(b);
    }

    private void RecalculateLayout()
    {
        var totalWidth = InputWidth + Gap + SubmitWidth;
        var x = Math.Max(Margin, (Game1.uiViewport.Width - totalWidth) / 2);
        var bottomAnchor = Game1.uiViewport.Height - BottomOffset;
        var y = Math.Max(Margin, bottomAnchor - InputHeight);

        _inputBounds = new Rectangle(x, y, InputWidth, InputHeight);
        _submitButtonBounds = new Rectangle(_inputBounds.Right + Gap, y, SubmitWidth, InputHeight);
        _input.X = _inputBounds.X;
        _input.Y = _inputBounds.Y;
        _input.Width = _inputBounds.Width;
        _input.Height = _inputBounds.Height;
    }

    private void Submit()
    {
        _textActionGate.Clear();

        var submittedText = _input.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(submittedText.Trim()))
        {
            Game1.playSound("cancel");
            return;
        }

        _sessionEnded = _onSubmit(submittedText);
        _input.Text = string.Empty;
        SetInputFocus(!_sessionEnded);

        if (_sessionEnded)
            CloseMenu(playSound: false);
    }

    private void CloseMenu(bool playSound)
    {
        _textActionGate.Clear();
        _playSoundOnClose = playSound;
        if (Game1.keyboardDispatcher.Subscriber == _input)
            Game1.keyboardDispatcher.Subscriber = null;
        exitThisMenuNoSound();
    }

    private void SetInputFocus(bool focused)
    {
        if (_sessionEnded)
            focused = false;

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

    private void DrawInputBox(SpriteBatch spriteBatch, Rectangle bounds, bool enabled)
    {
        var tint = enabled ? Color.White : Color.Gray * 0.9f;
        IClickableMenu.drawTextureBox(
            spriteBatch,
            Game1.menuTexture,
            new Rectangle(0, 256, 60, 60),
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            tint,
            1f,
            false);

        var availableWidth = Math.Max(0, bounds.Width - (TextPaddingX * 2));
        var layout = InputBoxTextRenderHelper.CreateLayout(_input.Text, availableWidth);
        var textX = bounds.X + TextPaddingX;
        var textY = bounds.Y + TextPaddingY;
        var color = enabled ? Game1.textColor : new Color(92, 92, 92);

        spriteBatch.DrawString(layout.Font, layout.VisibleText, new Vector2(textX, textY), color);

        if (enabled && _input.Selected && DateTime.UtcNow.Millisecond < 500)
        {
            var cursorX = textX + layout.Font.MeasureString(layout.VisibleText).X + 2f;
            var cursorY = textY + 2f;
            var cursorHeight = Math.Max(8, layout.Font.LineSpacing - 2);
            spriteBatch.Draw(Game1.staminaRect, new Rectangle((int)cursorX, (int)cursorY, 2, cursorHeight), color);
        }
    }

    private static void DrawSubmitButton(SpriteBatch spriteBatch, Rectangle bounds, string label, bool hovered, bool enabled)
    {
        var tint = enabled
            ? (hovered ? new Color(255, 255, 255) : Color.White)
            : Color.Gray * 0.9f;
        IClickableMenu.drawTextureBox(
            spriteBatch,
            Game1.mouseCursors,
            new Rectangle(432, 439, 9, 9),
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            tint,
            4f,
            false);

        var color = enabled ? Game1.textColor : new Color(92, 92, 92);
        var size = Game1.smallFont.MeasureString(label);
        var pos = new Vector2(
            bounds.X + (bounds.Width - size.X) / 2f,
            bounds.Y + (bounds.Height - size.Y) / 2f);
        spriteBatch.DrawString(Game1.smallFont, label, pos, color);
    }

}
