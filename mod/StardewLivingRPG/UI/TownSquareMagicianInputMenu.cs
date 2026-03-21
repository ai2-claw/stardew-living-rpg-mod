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
    private readonly Func<string, bool> _onSubmit;
    private readonly Action<bool> _onClose;
    private readonly DeferredTextBoxActionGate _textActionGate = new();
    private readonly TextBox _input;

    private bool _inputHasFocus;
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
        SetInputBounds(new Rectangle(-10000, -10000, 1, 1));
        SetInputFocus(true);
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        width = newBounds.Width;
        height = newBounds.Height;
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
    }

    public override void performHoverAction(int x, int y)
    {
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

        if (_input.Selected != _inputHasFocus)
            _input.Selected = _inputHasFocus;

        if (_inputHasFocus)
        {
            if (Game1.keyboardDispatcher.Subscriber != _input)
                Game1.keyboardDispatcher.Subscriber = _input;
        }
        else if (Game1.keyboardDispatcher.Subscriber == _input)
        {
            Game1.keyboardDispatcher.Subscriber = null;
        }

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
        _onClose(_playSoundOnClose);
    }

    public override void draw(SpriteBatch b)
    {
        drawMouse(b);
    }

    internal string Text
    {
        get => _input.Text ?? string.Empty;
        set => _input.Text = value ?? string.Empty;
    }

    internal bool HasFocus => _inputHasFocus;

    internal bool SessionEnded => _sessionEnded;

    internal void SetInputBounds(Rectangle bounds)
    {
        _input.X = bounds.X;
        _input.Y = bounds.Y;
        _input.Width = bounds.Width;
        _input.Height = bounds.Height;
    }

    internal void SubmitFromHud()
    {
        Submit();
    }

    internal void SetHudFocus(bool focused)
    {
        SetInputFocus(focused);
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
