using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewLivingRPG.State;
using StardewLivingRPG.Systems;

namespace StardewLivingRPG.UI;

public sealed class ChickenRaceMenu : IClickableMenu
{
    private const int MenuWidth = 800;
    private const int MenuHeight = 600;

    private const int RosterLeftX = 32;
    private const int RosterTopY = 80;
    private const int RosterWidth = 360;
    private const int RosterRowHeight = 48;
    private const int RosterRowGap = 4;

    private const int TrackLeftX = 400;
    private const int TrackTopY = 80;
    private const int TrackWidth = 380;
    private const int TrackLaneHeight = 56;

    private const int BetPanelTopY = 420;
    private const int ResultPanelTopY = 500;

    private readonly SaveState _state;
    private readonly ChickenRaceService _service;
    private readonly IMonitor _monitor;
    private readonly Action? _onClose;

    private RaceSession? _session;
    private int _selectedChickenIndex = -1;
    private int _betAmount;
    private int _raceNumber = 1;

    private readonly List<Rectangle> _chickenRowRects = new();
    private Rectangle _bet100Button;
    private Rectangle _bet500Button;
    private Rectangle _bet1000Button;
    private Rectangle _betMaxButton;
    private Rectangle _placeBetButton;
    private Rectangle _startRaceButton;
    private Rectangle _claimButton;
    private Rectangle _newRaceButton;
    private readonly ClickableTextureComponent _closeButton;

    private string _statusMessage = string.Empty;
    private long _lastPayout;
    private int _animationTick;

    private static readonly Color[] ChickenColors =
    {
        new(255, 255, 255),
        new(255, 200, 150),
        new(139, 69, 19),
        new(255, 215, 0),
        new(192, 192, 192),
        new(144, 238, 144)
    };

    public ChickenRaceMenu(SaveState state, ChickenRaceService service, IMonitor monitor, Action? onClose = null)
        : base(
            Game1.uiViewport.Width / 2 - (MenuWidth / 2),
            Game1.uiViewport.Height / 2 - (MenuHeight / 2),
            MenuWidth,
            MenuHeight,
            true)
    {
        _state = state;
        _service = service;
        _monitor = monitor;
        _onClose = onClose;

        _closeButton = new ClickableTextureComponent(
            new Rectangle(xPositionOnScreen + width - 68, yPositionOnScreen + 20, 48, 48),
            Game1.mouseCursors,
            new Rectangle(337, 494, 12, 12),
            4f);

        _service.SyncForToday(_state);
        InitializeNewRace();
        BuildLayout();
    }

    private void InitializeNewRace()
    {
        _session = _service.CreateNewRace(_state, _raceNumber);
        _selectedChickenIndex = -1;
        _betAmount = 100;
        _statusMessage = "Place your bet on a chicken!";
        _lastPayout = 0;
        _animationTick = 0;
    }

    private void BuildLayout()
    {
        _chickenRowRects.Clear();

        if (_session is null)
            return;

        var y = yPositionOnScreen + RosterTopY;
        foreach (var _ in _session.Racers)
        {
            _chickenRowRects.Add(new Rectangle(
                xPositionOnScreen + RosterLeftX,
                y,
                RosterWidth,
                RosterRowHeight));
            y += RosterRowHeight + RosterRowGap;
        }

        var buttonY = yPositionOnScreen + BetPanelTopY + 20;
        var buttonWidth = 80;
        var buttonHeight = 36;
        var buttonSpacing = 8;
        var startX = xPositionOnScreen + RosterLeftX;

        _bet100Button = new Rectangle(startX, buttonY, buttonWidth, buttonHeight);
        _bet500Button = new Rectangle(startX + buttonWidth + buttonSpacing, buttonY, buttonWidth, buttonHeight);
        _bet1000Button = new Rectangle(startX + (buttonWidth + buttonSpacing) * 2, buttonY, buttonWidth, buttonHeight);
        _betMaxButton = new Rectangle(startX + (buttonWidth + buttonSpacing) * 3, buttonY, buttonWidth, buttonHeight);

        _placeBetButton = new Rectangle(startX, buttonY + 44, 120, buttonHeight);
        _startRaceButton = new Rectangle(startX + 130, buttonY + 44, 120, buttonHeight);
        _claimButton = new Rectangle(startX, buttonY + 44, 120, buttonHeight);
        _newRaceButton = new Rectangle(startX + 130, buttonY + 44, 120, buttonHeight);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (_closeButton.containsPoint(x, y))
        {
            exitThisMenu(playSound);
            _onClose?.Invoke();
            return;
        }

        if (_session is null)
            return;

        if (_session.RaceFinished)
        {
            if (_session.PlayerBetIndex >= 0 && _session.WinnerIndex == _session.PlayerBetIndex && _claimButton.Contains(x, y))
            {
                _lastPayout = _service.ClaimWinnings(_session, _state);
                if (_lastPayout > 0)
                    _statusMessage = $"You won {_lastPayout}g!";
                Game1.playSound("money");
            }
            else if (_newRaceButton.Contains(x, y))
            {
                if (_service.CanRaceToday(_state))
                {
                    _raceNumber++;
                    InitializeNewRace();
                    BuildLayout();
                    Game1.playSound("smallSelect");
                }
                else
                {
                    _statusMessage = "No more races today. Come back tomorrow!";
                    Game1.playSound("cancel");
                }
            }
            return;
        }

        if (_session.RaceInProgress)
            return;

        for (var i = 0; i < _chickenRowRects.Count; i++)
        {
            if (_chickenRowRects[i].Contains(x, y))
            {
                _selectedChickenIndex = i;
                Game1.playSound("smallSelect");
                return;
            }
        }

        if (_bet100Button.Contains(x, y))
        {
            _betAmount = Math.Max(100, _betAmount);
            Game1.playSound("smallSelect");
        }
        else if (_bet500Button.Contains(x, y))
        {
            _betAmount = 500;
            Game1.playSound("smallSelect");
        }
        else if (_bet1000Button.Contains(x, y))
        {
            _betAmount = 1000;
            Game1.playSound("smallSelect");
        }
        else if (_betMaxButton.Contains(x, y))
        {
            _betAmount = Math.Min(5000, (int)Game1.player.Money);
            Game1.playSound("smallSelect");
        }

        if (_placeBetButton.Contains(x, y) && _selectedChickenIndex >= 0)
        {
            if (_service.PlaceBet(_session, _selectedChickenIndex, _betAmount, _state))
            {
                _statusMessage = $"Bet {_betAmount}g on {_session.Racers[_selectedChickenIndex].Name}!";
                Game1.playSound("money");
            }
            else
            {
                _statusMessage = "Not enough gold!";
                Game1.playSound("cancel");
            }
        }

        if (_startRaceButton.Contains(x, y) && _session.PlayerBetIndex >= 0)
        {
            _service.StartRace(_session, _state);
            _statusMessage = "The race is on!";
            Game1.playSound("drumkit6");
        }
    }

    public override void update(GameTime time)
    {
        _animationTick++;

        if (_session is not null && _session.RaceInProgress && !_session.RaceFinished)
        {
            if (_animationTick % 2 == 0)
            {
                var finished = _service.UpdateRace(_session);
                if (finished && _session.RaceFinished)
                {
                    var winner = _session.Racers[_session.WinnerIndex];
                    if (_session.WinnerIndex == _session.PlayerBetIndex)
                    {
                        _statusMessage = $"{winner.Name} wins! Claim your winnings!";
                        Game1.playSound("reward");
                    }
                    else
                    {
                        _statusMessage = $"{winner.Name} wins! Better luck next time.";
                        Game1.playSound("cancel");
                    }
                }
            }
        }
    }

    public override void draw(SpriteBatch b)
    {
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        var title = "Chicken Race Track";
        SpriteText.drawString(b, title, xPositionOnScreen + 32, yPositionOnScreen + 24);

        DrawRaceInfo(b);

        if (_session is null)
            return;

        DrawRoster(b);
        DrawTrack(b);
        DrawBetPanel(b);
        DrawResultPanel(b);

        _closeButton.draw(b);
        drawMouse(b);
    }

    private void DrawRaceInfo(SpriteBatch b)
    {
        var info = $"Race #{_raceNumber} | Day {Game1.dayOfMonth} {Game1.currentSeason}";
        var infoX = xPositionOnScreen + TrackLeftX;
        var infoY = yPositionOnScreen + 24;
        SpriteText.drawString(b, info, infoX, infoY);

        var remaining = _service.RacesRemainingToday(_state);
        var racesInfo = $"Races left: {remaining}";
        SpriteText.drawString(b, racesInfo, infoX, infoY + 30);
    }

    private void DrawRoster(SpriteBatch b)
    {
        if (_session is null)
            return;

        var headerY = yPositionOnScreen + RosterTopY - 24;
        SpriteText.drawString(b, "Roster", xPositionOnScreen + RosterLeftX, headerY);

        for (var i = 0; i < _session.Racers.Count; i++)
        {
            var racer = _session.Racers[i];
            var rect = _chickenRowRects[i];

            var isSelected = i == _selectedChickenIndex;
            var hasBet = i == _session.PlayerBetIndex;

            if (hasBet)
                b.Draw(Game1.staminaRect, rect, Color.Green * 0.3f);
            else if (isSelected)
                b.Draw(Game1.staminaRect, rect, Color.Yellow * 0.3f);

            DrawBorder(b, rect, 2, Color.Black * 0.5f);

            var chickenColor = ChickenColors[racer.ColorVariant % ChickenColors.Length];
            var chickenRect = new Rectangle(rect.X + 8, rect.Y + 8, 32, 32);
            DrawChickenIcon(b, chickenRect, chickenColor);

            var nameX = rect.X + 48;
            var nameY = rect.Y + 4;
            Utility.drawTextWithShadow(b, racer.Name, Game1.smallFont, new Vector2(nameX, nameY), Game1.textColor);

            var oddsText = $"{racer.Odds:F1}x";
            var oddsX = rect.X + rect.Width - 60;
            Utility.drawTextWithShadow(b, oddsText, Game1.smallFont, new Vector2(oddsX, nameY), Color.Gold);
        }
    }

    private void DrawTrack(SpriteBatch b)
    {
        if (_session is null)
            return;

        var trackX = xPositionOnScreen + TrackLeftX;
        var trackY = yPositionOnScreen + TrackTopY;
        var trackWidth = TrackWidth;

        b.Draw(Game1.staminaRect, new Rectangle(trackX, trackY, trackWidth, TrackLaneHeight * _session.Racers.Count + 8),
            Color.SaddleBrown * 0.8f);

        for (var i = 0; i < _session.Racers.Count; i++)
        {
            var racer = _session.Racers[i];
            var laneY = trackY + i * TrackLaneHeight;

            var laneRect = new Rectangle(trackX, laneY, trackWidth, TrackLaneHeight - 4);
            b.Draw(Game1.staminaRect, laneRect, Color.BurlyWood * 0.6f);

            var progress = _session.Positions[i];
            var chickenX = trackX + (int)(progress * (trackWidth - 40));
            var chickenRect = new Rectangle(chickenX, laneY + 8, 32, 32);

            var chickenColor = ChickenColors[racer.ColorVariant % ChickenColors.Length];
            DrawChickenIcon(b, chickenRect, chickenColor);

            if (i == _session.WinnerIndex && _session.RaceFinished)
            {
                Utility.drawTextWithShadow(b, "WINNER!", Game1.smallFont, new Vector2(trackX + trackWidth - 100, laneY + 12), Color.Gold);
            }
        }

        DrawBorder(b, new Rectangle(trackX, trackY, trackWidth, TrackLaneHeight * _session.Racers.Count + 8),
            3, Color.Black * 0.7f);
    }

    private void DrawChickenIcon(SpriteBatch b, Rectangle rect, Color color)
    {
        b.Draw(Game1.staminaRect, rect, color);

        b.Draw(Game1.staminaRect,
            new Rectangle(rect.X + 20, rect.Y + 2, 8, 8),
            Color.OrangeRed);

        b.Draw(Game1.staminaRect,
            new Rectangle(rect.X + 4, rect.Y + rect.Height - 4, 6, 6),
            Color.Orange);
    }

    private void DrawBetPanel(SpriteBatch b)
    {
        if (_session is null || _session.RaceInProgress || _session.RaceFinished)
            return;

        var panelY = yPositionOnScreen + BetPanelTopY;

        Utility.drawTextWithShadow(b, $"Bet: {_betAmount}g | Gold: {Game1.player.Money}g",
            Game1.smallFont, new Vector2(xPositionOnScreen + RosterLeftX, panelY), Game1.textColor);

        DrawButton(b, _bet100Button, "100g", _bet100Button.Contains(Game1.getMouseX(), Game1.getMouseY()));
        DrawButton(b, _bet500Button, "500g", _bet500Button.Contains(Game1.getMouseX(), Game1.getMouseY()));
        DrawButton(b, _bet1000Button, "1000g", _bet1000Button.Contains(Game1.getMouseX(), Game1.getMouseY()));
        DrawButton(b, _betMaxButton, "Max", _betMaxButton.Contains(Game1.getMouseX(), Game1.getMouseY()));

        var canBet = _selectedChickenIndex >= 0 && _session.PlayerBetIndex < 0;
        DrawButton(b, _placeBetButton, "Place Bet", canBet && _placeBetButton.Contains(Game1.getMouseX(), Game1.getMouseY()),
            enabled: canBet);

        var canStart = _session.PlayerBetIndex >= 0;
        DrawButton(b, _startRaceButton, "Start Race", canStart && _startRaceButton.Contains(Game1.getMouseX(), Game1.getMouseY()),
            enabled: canStart);
    }

    private void DrawResultPanel(SpriteBatch b)
    {
        if (_session is null)
            return;

        var panelY = yPositionOnScreen + ResultPanelTopY;

        Utility.drawTextWithShadow(b, _statusMessage, Game1.smallFont,
            new Vector2(xPositionOnScreen + RosterLeftX, panelY), Game1.textColor);

        if (_session.RaceFinished)
        {
            if (_session.PlayerBetIndex >= 0 && _session.WinnerIndex == _session.PlayerBetIndex && _lastPayout == 0)
            {
                DrawButton(b, _claimButton, "Claim Winnings",
                    _claimButton.Contains(Game1.getMouseX(), Game1.getMouseY()));
            }

            if (_service.CanRaceToday(_state))
            {
                DrawButton(b, _newRaceButton, "New Race",
                    _newRaceButton.Contains(Game1.getMouseX(), Game1.getMouseY()));
            }
            else
            {
                Utility.drawTextWithShadow(b, "No more races today!", Game1.smallFont,
                    new Vector2(xPositionOnScreen + RosterLeftX + 130, panelY + 44), Color.Gray);
            }
        }
    }

    private void DrawButton(SpriteBatch b, Rectangle rect, string text, bool hovered, bool enabled = true)
    {
        var bgColor = enabled
            ? (hovered ? Color.Wheat : Color.SaddleBrown)
            : Color.Gray;

        b.Draw(Game1.staminaRect, rect, bgColor * 0.8f);
        DrawBorder(b, rect, 2, Color.Black * 0.6f);

        var textColor = enabled ? Color.White : Color.DarkGray;
        Utility.drawTextWithShadow(b, text, Game1.smallFont,
            new Vector2(rect.X + 8, rect.Y + (rect.Height / 2) - 8), textColor);
    }

    private static void DrawBorder(SpriteBatch b, Rectangle rect, int thickness, Color color)
    {
        b.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        b.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        b.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        b.Draw(Game1.staminaRect, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    public override void performHoverAction(int x, int y)
    {
        _closeButton.tryHover(x, y);
    }
}
