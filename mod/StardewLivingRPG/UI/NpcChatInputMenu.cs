using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using StardewLivingRPG.Utils;
using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace StardewLivingRPG.UI;

public sealed class NpcChatInputMenu : IClickableMenu
{
    private enum PortraitEmotion
    {
        Neutral,
        Happy,
        Content,
        Blush,
        Sad,
        Angry,
        Surprised,
        Worried
    }

    private const int PortraitFrameSize = 64;
    // Default portrait expression indices used when NPC-specific indices are unavailable.
    private const int DefaultPortraitIndexNeutral = 0;
    private const int DefaultPortraitIndexHappy = 1;
    private const int DefaultPortraitIndexSad = 2;
    private const int DefaultPortraitIndexContent = 3;
    private const int DefaultPortraitIndexBlush = 4;
    private const int DefaultPortraitIndexAngry = 5;
    private const int DefaultPortraitIndexSurprised = 7;
    private static readonly BindingFlags NpcPortraitIndexBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly FieldInfo? NpcPortraitNeutralIndexField = typeof(NPC).GetField("portrait_neutral_index", NpcPortraitIndexBindingFlags);
    private static readonly FieldInfo? NpcPortraitHappyIndexField = typeof(NPC).GetField("portrait_happy_index", NpcPortraitIndexBindingFlags);
    private static readonly FieldInfo? NpcPortraitSadIndexField = typeof(NPC).GetField("portrait_sad_index", NpcPortraitIndexBindingFlags);
    private static readonly FieldInfo? NpcPortraitAngryIndexField = typeof(NPC).GetField("portrait_angry_index", NpcPortraitIndexBindingFlags);
    private static readonly FieldInfo? NpcPortraitCustomIndexField = typeof(NPC).GetField("portrait_custom_index", NpcPortraitIndexBindingFlags);
    private static readonly FieldInfo? NpcPortraitBlushIndexField = typeof(NPC).GetField("portrait_blush_index", NpcPortraitIndexBindingFlags);
    private static readonly TimeSpan LivePortraitRefreshInterval = TimeSpan.FromMilliseconds(350);
    private static readonly TimeSpan EmotionNeutralDecay = TimeSpan.FromSeconds(8);
    private static readonly string[] HappyEmotionTokens =
    {
        "happy", "glad", "great", "wonderful", "excited", "delighted", "thanks", "thank you", "haha", "heh"
    };
    private static readonly string[] ContentEmotionTokens =
    {
        "content", "satisfied", "at ease", "comfortable"
    };
    private static readonly string[] BlushEmotionTokens =
    {
        "blush", "blushes", "blushing", "bashful", "shy", "flustered"
    };
    private static readonly string[] SadEmotionTokens =
    {
        "sad", "sorry", "down", "unhappy", "regret", "sigh", "disappointed"
    };
    private static readonly string[] AngryEmotionTokens =
    {
        "angry", "mad", "furious", "annoyed", "frustrated", "irritated", "grr", "ugh"
    };
    private static readonly string[] SurprisedEmotionTokens =
    {
        "wow", "whoa", "really", "what", "surprised", "unexpected"
    };
    private static readonly string[] WorriedEmotionTokens =
    {
        "worried", "concerned", "nervous", "anxious", "unsure", "not sure", "uncertain", "hmm",
        "pensive", "thoughtful", "contemplative"
    };
    private readonly struct PortraitFrameProfile
    {
        public PortraitFrameProfile(int neutral, int happy, int content, int blush, int sad, int angry, int surprised, int worried)
        {
            Neutral = neutral;
            Happy = happy;
            Content = content;
            Blush = blush;
            Sad = sad;
            Angry = angry;
            Surprised = surprised;
            Worried = worried;
        }

        public int Neutral { get; }
        public int Happy { get; }
        public int Content { get; }
        public int Blush { get; }
        public int Sad { get; }
        public int Angry { get; }
        public int Surprised { get; }
        public int Worried { get; }
    }
    private static readonly PortraitFrameProfile DefaultPortraitFrameProfile = new(
        neutral: DefaultPortraitIndexNeutral,
        happy: DefaultPortraitIndexHappy,
        content: DefaultPortraitIndexContent,
        blush: DefaultPortraitIndexBlush,
        sad: DefaultPortraitIndexSad,
        angry: DefaultPortraitIndexAngry,
        surprised: DefaultPortraitIndexSurprised,
        worried: DefaultPortraitIndexSad);

    private readonly string _npcName;
    private readonly string _portraitAssetName;
    private readonly int _heartLevel;
    private readonly Action<string> _onSend;
    private readonly Func<string?>? _pollIncoming;
    private readonly Func<bool>? _isThinking;
    private readonly Func<NPC?>? _resolveLiveNpc;
    private readonly Func<NPC?, string?, string, int?>? _resolveProfilePortraitIndex;

    private string? _lastNpcMessage;
    private string? _lastPlayerMessage;
    private bool _hasReceivedNpcReplyInSession;

    private int _thinkFrame;

    private readonly TextBox _input;
    private bool _inputHasFocus = true;

    private Rectangle _sendButtonBounds;
    private bool _sendButtonHovered;
    private readonly ClickableTextureComponent _closeButton;

    // Portrait Data
    private Texture2D? _fallbackPortraitTexture;
    private Texture2D? _activePortraitTexture;
    private NPC? _activeLiveNpc;
    private Rectangle _portraitSource = new(0, 0, PortraitFrameSize, PortraitFrameSize);
    private PortraitEmotion _currentPortraitEmotion = PortraitEmotion.Neutral;
    private DateTime _lastEmotionUpdateUtc = DateTime.UtcNow;
    private DateTime _nextPortraitRefreshUtc;
    private static readonly Rectangle EmptyHeartSource = new Rectangle(218, 428, 7, 6);
    private static readonly Rectangle FilledHeartSource = new Rectangle(211, 428, 7, 6);

    // Layout constants
    private const int MenuWidth = 880;
    private const int MenuHeight = 620;
    private const int MainPadding = 50;

    // Regions
    private Rectangle _parchmentRegion;
    private Rectangle _portraitRegion;
    private Rectangle _heartRowRegion;
    private Rectangle _chatRegion;
    private Rectangle _inputRegion;

    // Scroll state
    private int _chatScrollOffset = 0;
    private int _chatContentHeight = 0;
    private Rectangle _scrollTrackRegion;
    private Rectangle _scrollThumbRegion;
    private bool _scrollThumbHeld = false;
    private int _scrollThumbDragOffset = 0;
    private string? _lastNpcMessageForScroll;

    public NpcChatInputMenu(
        string npcName,
        Action<string> onSend,
        Func<string?>? pollIncoming = null,
        Func<bool>? isThinking = null,
        int heartLevel = 0,
        string? initialPlayerMessage = null,
        bool autoSendInitialPlayerMessage = false,
        string? portraitAssetName = null,
        Func<NPC?>? resolveLiveNpc = null,
        Func<NPC?, string?, string, int?>? resolveProfilePortraitIndex = null)
        : base(
            Game1.uiViewport.Width / 2 - (MenuWidth / 2),
            Game1.uiViewport.Height / 2 - (MenuHeight / 2),
            MenuWidth,
            MenuHeight,
            true)
    {
        _npcName = npcName;
        _portraitAssetName = string.IsNullOrWhiteSpace(portraitAssetName) ? npcName : portraitAssetName.Trim();
        _heartLevel = Math.Max(0, heartLevel);
        _onSend = onSend;
        _pollIncoming = pollIncoming;
        _isThinking = isThinking;
        _resolveLiveNpc = resolveLiveNpc;
        _resolveProfilePortraitIndex = resolveProfilePortraitIndex;
        _nextPortraitRefreshUtc = DateTime.UtcNow;

        _activePortraitTexture = TryResolveLivePortrait();
        if (_activePortraitTexture is null)
            TryEnsureFallbackPortraitTextureLoaded();
        if (_activePortraitTexture is null)
            _activePortraitTexture = _fallbackPortraitTexture;
        RefreshPortraitTexture(force: true);

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

        if (!string.IsNullOrWhiteSpace(initialPlayerMessage))
        {
            var initial = initialPlayerMessage.Trim();
            _lastPlayerMessage = initial;
            _lastNpcMessage = null;

            if (autoSendInitialPlayerMessage)
                _onSend(initial);
        }
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

		// Chat region (right of portrait, with scrollbar space)
		int scrollBarWidth = 24;
		int chatX = _portraitRegion.Right + 32;
		_chatRegion = new Rectangle(
			chatX,
			_parchmentRegion.Y + 32,
			_parchmentRegion.Right - chatX - 32 - scrollBarWidth - 8,
			_parchmentRegion.Height - 64
		);

		// Scrollbar track region (right of chat region)
		_scrollTrackRegion = new Rectangle(
			_chatRegion.Right + 8,
			_chatRegion.Y,
			scrollBarWidth,
			_chatRegion.Height
		);

		_heartRowRegion = new Rectangle(
			_portraitRegion.X,
			_portraitRegion.Bottom + 12,
			_portraitRegion.Width,
			32
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

        // Scrollbar thumb drag start
        if (_scrollThumbRegion.Contains(x, y) && CanScroll())
        {
            _scrollThumbHeld = true;
            _scrollThumbDragOffset = y - _scrollThumbRegion.Y;
            return;
        }

        // Scrollbar track click (page up/down)
        if (_scrollTrackRegion.Contains(x, y) && CanScroll())
        {
            if (y < _scrollThumbRegion.Y)
            {
                // Click above thumb: page up
                ScrollBy(-_chatRegion.Height / 2);
            }
            else if (y > _scrollThumbRegion.Bottom)
            {
                // Click below thumb: page down
                ScrollBy(_chatRegion.Height / 2);
            }
            return;
        }

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

    public override void leftClickHeld(int x, int y)
    {
        base.leftClickHeld(x, y);

        if (_scrollThumbHeld && CanScroll())
        {
            int trackHeight = _scrollTrackRegion.Height - _scrollThumbRegion.Height;
            int newThumbY = Math.Clamp(y - _scrollThumbDragOffset, _scrollTrackRegion.Y, _scrollTrackRegion.Y + trackHeight);
            float scrollPercent = trackHeight > 0 ? (float)(newThumbY - _scrollTrackRegion.Y) / trackHeight : 0f;
            int maxScroll = Math.Max(0, _chatContentHeight - _chatRegion.Height);
            _chatScrollOffset = (int)(scrollPercent * maxScroll);
        }
    }

    public override void releaseLeftClick(int x, int y)
    {
        base.releaseLeftClick(x, y);
        _scrollThumbHeld = false;
    }

    public override void receiveScrollWheelAction(int direction)
    {
        base.receiveScrollWheelAction(direction);
        if (_chatRegion.Contains(Game1.getMouseX(), Game1.getMouseY()) || _scrollTrackRegion.Contains(Game1.getMouseX(), Game1.getMouseY()))
        {
            ScrollBy(-direction * Game1.smallFont.LineSpacing * 2);
        }
    }

    private bool CanScroll() => _chatContentHeight > _chatRegion.Height;

    private void ScrollBy(int delta)
    {
        int maxScroll = Math.Max(0, _chatContentHeight - _chatRegion.Height);
        _chatScrollOffset = Math.Clamp(_chatScrollOffset + delta, 0, maxScroll);
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
        RefreshPortraitTexture();

        if (_input.Selected != _inputHasFocus)
            _input.Selected = _inputHasFocus;

        if (_inputHasFocus)
        {
            if (Game1.keyboardDispatcher.Subscriber != _input)
                Game1.keyboardDispatcher.Subscriber = _input;
        }

        _thinkFrame++;
        if (_currentPortraitEmotion != PortraitEmotion.Neutral
            && DateTime.UtcNow - _lastEmotionUpdateUtc >= EmotionNeutralDecay
            && !IsThinking())
        {
            SetPortraitEmotion(PortraitEmotion.Neutral);
        }

        if (_pollIncoming is not null)
        {
            var next = _pollIncoming();
            if (!string.IsNullOrWhiteSpace(next))
            {
                var (clean, explicitEmotion) = ParseIncomingNpcMessage(next);
                var isFirstNpcReplyInSession = !_hasReceivedNpcReplyInSession;
                var inferredEmotion = isFirstNpcReplyInSession
                    ? PortraitEmotion.Neutral
                    : explicitEmotion ?? InferEmotionFromText(clean);
                SetPortraitEmotion(inferredEmotion);
                _hasReceivedNpcReplyInSession = true;
                if (clean != _lastNpcMessageForScroll)
                {
                    _lastNpcMessage = clean;
                    _lastNpcMessageForScroll = clean;
                    // Scroll to bottom on new message
                    _chatScrollOffset = Math.Max(0, _chatContentHeight - _chatRegion.Height);
                }
            }
        }
    }

    private (string Message, PortraitEmotion? ExplicitEmotion) ParseIncomingNpcMessage(string raw)
    {
        var clean = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(clean))
            return (string.Empty, null);

        PortraitEmotion? explicitEmotion = null;

        while (true)
        {
            var tagMatch = Regex.Match(clean, @"^\<(?<tag>[^>]+)\>\s*", RegexOptions.CultureInvariant);
            if (!tagMatch.Success || tagMatch.Length <= 0)
                break;
            var tagText = tagMatch.Groups["tag"].Value;
            if (TryParseEmotionTag(tagText, out var parsedEmotion))
                explicitEmotion = parsedEmotion;
            clean = clean[tagMatch.Length..].TrimStart();
        }

        clean = StripInlineEmotionTags(clean, ref explicitEmotion);
        if (!string.IsNullOrWhiteSpace(_npcName))
        {
            var label = _npcName.Trim();
            if (!string.IsNullOrWhiteSpace(label)
                && clean.StartsWith(label + ":", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean[(label.Length + 1)..].TrimStart();
            }
        }

        return (clean, explicitEmotion);
    }

    private void RefreshPortraitTexture(bool force = false)
    {
        var now = DateTime.UtcNow;
        if (!force && now < _nextPortraitRefreshUtc)
            return;

        _nextPortraitRefreshUtc = now + LivePortraitRefreshInterval;
        var livePortrait = TryResolveLivePortrait();
        if (livePortrait is null)
            TryEnsureFallbackPortraitTextureLoaded();

        _activePortraitTexture = livePortrait ?? _activePortraitTexture ?? _fallbackPortraitTexture;
        UpdatePortraitSourceRect();
    }

    private Texture2D? TryResolveLivePortrait()
    {
        if (_resolveLiveNpc is null)
        {
            _activeLiveNpc = null;
            return null;
        }

        try
        {
            _activeLiveNpc = _resolveLiveNpc();
            return _activeLiveNpc?.Portrait;
        }
        catch
        {
            _activeLiveNpc = null;
            return null;
        }
    }

    private void TryEnsureFallbackPortraitTextureLoaded()
    {
        if (_fallbackPortraitTexture is not null || string.IsNullOrWhiteSpace(_portraitAssetName))
            return;

        try
        {
            _fallbackPortraitTexture = Game1.content.Load<Texture2D>($"Portraits\\{_portraitAssetName}");
        }
        catch
        {
            _fallbackPortraitTexture = null;
        }
    }

    private void SetPortraitEmotion(PortraitEmotion emotion)
    {
        if (_currentPortraitEmotion == emotion)
        {
            _lastEmotionUpdateUtc = DateTime.UtcNow;
            return;
        }

        _currentPortraitEmotion = emotion;
        _lastEmotionUpdateUtc = DateTime.UtcNow;
        UpdatePortraitSourceRect();
    }

    private void UpdatePortraitSourceRect()
    {
        var texture = _activePortraitTexture ?? _fallbackPortraitTexture;
        if (texture is null)
        {
            _portraitSource = new Rectangle(0, 0, PortraitFrameSize, PortraitFrameSize);
            return;
        }

        var framesAcross = Math.Max(1, texture.Width / PortraitFrameSize);
        var framesDown = Math.Max(1, texture.Height / PortraitFrameSize);
        var frameCount = Math.Max(1, framesAcross * framesDown);
        var portraitProfile = ResolvePortraitFrameProfile(_activeLiveNpc);
        var desiredIndex = ResolveDesiredPortraitFrameIndex(_currentPortraitEmotion, portraitProfile);
        // Do not clamp to the last frame when a specific emotion frame is missing.
        // Fall back to neutral frame 0 for unsupported expressions.
        var neutralFallbackIndex = portraitProfile.Neutral >= 0 && portraitProfile.Neutral < frameCount
            ? portraitProfile.Neutral
            : 0;
        var resolvedIndex = desiredIndex >= 0 && desiredIndex < frameCount
            ? desiredIndex
            : neutralFallbackIndex;
        var sourceX = (resolvedIndex % framesAcross) * PortraitFrameSize;
        var sourceY = (resolvedIndex / framesAcross) * PortraitFrameSize;
        if (sourceX + PortraitFrameSize > texture.Width || sourceY + PortraitFrameSize > texture.Height)
        {
            sourceX = 0;
            sourceY = 0;
        }

        _portraitSource = new Rectangle(sourceX, sourceY, PortraitFrameSize, PortraitFrameSize);
    }

    private static int GetPortraitFrameIndex(PortraitEmotion emotion, PortraitFrameProfile profile)
    {
        return emotion switch
        {
            PortraitEmotion.Happy => profile.Happy,
            PortraitEmotion.Content => profile.Content,
            PortraitEmotion.Blush => profile.Blush,
            PortraitEmotion.Sad => profile.Sad,
            PortraitEmotion.Worried => profile.Worried,
            PortraitEmotion.Angry => profile.Angry,
            PortraitEmotion.Surprised => profile.Surprised,
            _ => profile.Neutral
        };
    }

    private int ResolveDesiredPortraitFrameIndex(PortraitEmotion emotion, PortraitFrameProfile fallbackProfile)
    {
        if (_resolveProfilePortraitIndex is not null)
        {
            try
            {
                var profileIndex = _resolveProfilePortraitIndex(_activeLiveNpc, _portraitAssetName, NormalizeEmotionKey(emotion));
                if (profileIndex.HasValue && profileIndex.Value >= 0)
                    return profileIndex.Value;
            }
            catch
            {
                // Profile resolution should never block portrait rendering.
            }
        }

        return GetPortraitFrameIndex(emotion, fallbackProfile);
    }

    private static string NormalizeEmotionKey(PortraitEmotion emotion)
    {
        return emotion switch
        {
            PortraitEmotion.Neutral => "neutral",
            PortraitEmotion.Happy => "happy",
            PortraitEmotion.Content => "content",
            PortraitEmotion.Blush => "blush",
            PortraitEmotion.Sad => "sad",
            PortraitEmotion.Angry => "angry",
            PortraitEmotion.Surprised => "surprised",
            PortraitEmotion.Worried => "worried",
            _ => "neutral"
        };
    }

    private static PortraitFrameProfile ResolvePortraitFrameProfile(NPC? npc)
    {
        if (npc is null)
            return DefaultPortraitFrameProfile;

        var neutral = ResolvePortraitIndexOrDefault(npc, NpcPortraitNeutralIndexField, DefaultPortraitIndexNeutral, allowZero: true);
        var happy = ResolvePortraitIndexOrDefault(npc, NpcPortraitHappyIndexField, DefaultPortraitIndexHappy, allowZero: false);
        var sad = ResolvePortraitIndexOrDefault(npc, NpcPortraitSadIndexField, DefaultPortraitIndexSad, allowZero: false);
        var angry = ResolvePortraitIndexOrDefault(npc, NpcPortraitAngryIndexField, DefaultPortraitIndexAngry, allowZero: false);
        var custom = ResolvePortraitIndexOrDefault(npc, NpcPortraitCustomIndexField, DefaultPortraitIndexContent, allowZero: false);
        var blush = ResolvePortraitIndexOrDefault(npc, NpcPortraitBlushIndexField, DefaultPortraitIndexBlush, allowZero: false);

        var worried = custom > 0 ? custom : sad;
        var content = custom > 0 ? custom : blush;
        var blushExpression = blush > 0 ? blush : custom;

        return new PortraitFrameProfile(
            neutral: neutral,
            happy: happy,
            content: content,
            blush: blushExpression,
            sad: sad,
            angry: angry,
            surprised: DefaultPortraitIndexSurprised,
            worried: worried);
    }

    private static int ResolvePortraitIndexOrDefault(NPC npc, FieldInfo? field, int fallbackIndex, bool allowZero)
    {
        if (field is null)
            return fallbackIndex;

        try
        {
            if (field.GetValue(npc) is int value)
            {
                if (value > 0 || (allowZero && value == 0))
                    return value;
            }
        }
        catch
        {
            // Preserve fallback indices if reflection fails.
        }

        return fallbackIndex;
    }

    private static bool TryParseEmotionTag(string rawTag, out PortraitEmotion emotion)
    {
        emotion = PortraitEmotion.Neutral;
        if (string.IsNullOrWhiteSpace(rawTag))
            return false;

        var normalized = rawTag.Trim().ToLowerInvariant();
        if (normalized.StartsWith("emotion:", StringComparison.Ordinal))
            normalized = normalized["emotion:".Length..].Trim();
        else
            return false;

        normalized = normalized.Replace("_", string.Empty, StringComparison.Ordinal);
        emotion = normalized switch
        {
            "neutral" or "calm" => PortraitEmotion.Neutral,
            "happy" or "glad" or "cheerful" or "smile" => PortraitEmotion.Happy,
            "content" => PortraitEmotion.Content,
            "blush" or "blushes" or "blushing" or "bashful" or "shy" or "flustered" => PortraitEmotion.Blush,
            "sad" or "down" or "unhappy" => PortraitEmotion.Sad,
            "angry" or "mad" or "annoyed" or "frustrated" => PortraitEmotion.Angry,
            "surprised" or "shock" => PortraitEmotion.Surprised,
            "worried" or "concerned" or "nervous" or "anxious" or "unsure" or "pensive" or "thoughtful" or "contemplative" => PortraitEmotion.Worried,
            _ => PortraitEmotion.Neutral
        };

        return true;
    }

    private static string StripInlineEmotionTags(string raw, ref PortraitEmotion? explicitEmotion)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var resolvedEmotion = explicitEmotion;
        var withoutTags = Regex.Replace(
            raw,
            @"<\s*emotion\s*:\s*(?<value>[a-zA-Z_]+)\s*>",
            match =>
            {
                var value = match.Groups["value"].Value;
                if (TryParseEmotionTag($"emotion:{value}", out var parsed))
                    resolvedEmotion = parsed;
                return string.Empty;
            },
            RegexOptions.CultureInvariant);
        explicitEmotion = resolvedEmotion;

        return Regex.Replace(withoutTags, @"\s{2,}", " ", RegexOptions.CultureInvariant).Trim();
    }

    private static PortraitEmotion InferEmotionFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return PortraitEmotion.Neutral;

        var normalized = text.Trim().ToLowerInvariant();
        if (ContainsAnyToken(normalized, AngryEmotionTokens))
            return PortraitEmotion.Angry;
        if (ContainsAnyToken(normalized, SadEmotionTokens))
            return PortraitEmotion.Sad;
        if (ContainsAnyToken(normalized, WorriedEmotionTokens))
            return PortraitEmotion.Worried;
        if (ContainsAnyToken(normalized, BlushEmotionTokens))
            return PortraitEmotion.Blush;
        if (ContainsAnyToken(normalized, ContentEmotionTokens))
            return PortraitEmotion.Content;
        if (ContainsAnyToken(normalized, HappyEmotionTokens))
            return PortraitEmotion.Happy;
        if (ContainsAnyToken(normalized, SurprisedEmotionTokens) || normalized.Contains("?!", StringComparison.Ordinal))
            return PortraitEmotion.Surprised;

        return PortraitEmotion.Neutral;
    }

    private static bool ContainsAnyToken(string normalizedText, string[] tokens)
    {
        foreach (var token in tokens)
        {
            if (normalizedText.Contains(token, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public override void draw(SpriteBatch b)
    {
        // 1. Main Background Box
        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        // 2. Parchment Area
        DrawParchment(b);

        // 3. NPC header and portrait
        DrawNpcHeader(b);
        DrawPortrait(b);
        DrawHeartRow(b);

        // 4. Conversation Text
        DrawConversationText(b);

        // 4b. Scrollbar
        DrawScrollbar(b);

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
        string btnText = I18n.Get("npc_chat.button.send", "SEND");
        Vector2 textSize = Game1.smallFont.MeasureString(btnText);
        Vector2 textPos = new Vector2(
            _sendButtonBounds.X + press + (_sendButtonBounds.Width - textSize.X) / 2f,
            _sendButtonBounds.Y + press + (_sendButtonBounds.Height - Game1.smallFont.LineSpacing) / 2f
        );

        Color btnTextColor = _sendButtonHovered ? Game1.textColor : (Game1.textColor * 0.85f);
        Utility.drawTextWithShadow(b, btnText, Game1.smallFont, textPos, btnTextColor);

        drawMouse(b);
    }

    private void DrawNpcHeader(SpriteBatch b)
    {
        var nameLabel = string.IsNullOrWhiteSpace(_npcName)
            ? I18n.Get("npc_chat.npc.fallback_name", "Villager")
            : _npcName;
        var textSize = Game1.smallFont.MeasureString(nameLabel);

        float x = _portraitRegion.X + (_portraitRegion.Width - textSize.X) / 2f;
        float y = Math.Max(_parchmentRegion.Y + 10f, _portraitRegion.Y - Game1.smallFont.LineSpacing - 12f);
        Utility.drawTextWithShadow(b, nameLabel, Game1.smallFont, new Vector2(x, y), new Color(72, 46, 24));

        int lineWidth = Math.Max(120, (int)textSize.X + 12);
        int lineX = _portraitRegion.X + (_portraitRegion.Width - lineWidth) / 2;
        int lineY = (int)(y + Game1.smallFont.LineSpacing + 2f);
        b.Draw(Game1.staminaRect, new Rectangle(lineX, lineY, lineWidth, 1), Color.BurlyWood * 0.9f);
    }

    private void DrawHeartRow(SpriteBatch b)
    {
        const int maxHearts = 10;
        const int scale = 3;
        const int spacing = 3;
        int filledHearts = Math.Clamp(_heartLevel, 0, maxHearts);

        int heartWidth = EmptyHeartSource.Width * scale;
        int heartHeight = EmptyHeartSource.Height * scale;
        int totalWidth = (heartWidth * maxHearts) + (spacing * (maxHearts - 1));

        int startX = _heartRowRegion.X + (_heartRowRegion.Width - totalWidth) / 2;
        int y = _heartRowRegion.Y + (_heartRowRegion.Height - heartHeight) / 2;

        for (int i = 0; i < maxHearts; i++)
        {
            var source = i < filledHearts ? FilledHeartSource : EmptyHeartSource;
            b.Draw(
                Game1.mouseCursors,
                new Rectangle(startX + ((heartWidth + spacing) * i), y, heartWidth, heartHeight),
                source,
                Color.White);
        }
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

        var portraitTexture = _activePortraitTexture ?? _fallbackPortraitTexture;
        if (portraitTexture != null)
        {
            b.Draw(portraitTexture, _portraitRegion, _portraitSource, Color.White);
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

        // Calculate wrapped lines and total content height
        var (playerLines, npcLines) = GetWrappedLines(w, out int separatorSpace);
        _chatContentHeight = CalculateContentHeight(playerLines, npcLines, separatorSpace);

        // Update scrollbar thumb position
        UpdateScrollThumbRegion();

        // Apply scroll offset
        y -= _chatScrollOffset;

        // Begin scissor clipping
        var oldScissor = Game1.graphics.GraphicsDevice.ScissorRectangle;
        var rasterizer = new RasterizerState { ScissorTestEnable = true };
        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, rasterizer);
        Game1.graphics.GraphicsDevice.ScissorRectangle = _chatRegion;

        // 1. Player Message
        if (playerLines != null)
        {
            string label = I18n.Get("npc_chat.label.you", "You: ");
            Vector2 labelSize = Game1.smallFont.MeasureString(label);

            b.DrawString(Game1.smallFont, label, new Vector2(x, y), playerColor);

            for (int i = 0; i < playerLines.Length; i++)
            {
                float drawX = (i == 0) ? x + labelSize.X : x;
                b.DrawString(Game1.smallFont, playerLines[i], new Vector2(drawX, y), playerColor);
                y += Game1.smallFont.LineSpacing;
            }
            y += separatorSpace;
        }

        // Separator
        if (playerLines != null && (npcLines != null || IsThinking()))
        {
            b.Draw(Game1.staminaRect, new Rectangle((int)x, (int)y - 8, (int)w, 1), Color.BurlyWood * 0.8f);
        }

        // 2. NPC Message
        if (npcLines != null)
        {
            foreach (var line in npcLines)
            {
                b.DrawString(Game1.smallFont, line, new Vector2(x, y), npcColor);
                y += Game1.smallFont.LineSpacing + 4;
            }
        }
        else if (IsThinking())
        {
            int dots = (_thinkFrame / 20) % 4;
            string text = I18n.Get("npc_chat.label.thinking", "Thinking") + new string('.', dots);
            b.DrawString(Game1.smallFont, text, new Vector2(x, y), npcColor * 0.6f);
        }

        // End scissor clipping
        b.End();
        b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
        Game1.graphics.GraphicsDevice.ScissorRectangle = oldScissor;
    }

    private (string[]? playerLines, string[]? npcLines) GetWrappedLines(float width, out int separatorSpace)
    {
        separatorSpace = 16;
        string[]? playerLines = null;
        string[]? npcLines = null;

        if (_lastPlayerMessage != null)
        {
            string label = I18n.Get("npc_chat.label.you", "You: ");
            Vector2 labelSize = Game1.smallFont.MeasureString(label);
            playerLines = TextWrapHelper.WrapText(Game1.smallFont, _lastPlayerMessage, width - labelSize.X);
        }

        if (_lastNpcMessage != null)
        {
            npcLines = TextWrapHelper.WrapText(Game1.smallFont, _lastNpcMessage, width);
        }

        return (playerLines, npcLines);
    }

    private int CalculateContentHeight(string[]? playerLines, string[]? npcLines, int separatorSpace)
    {
        int height = 0;

        if (playerLines != null)
        {
            height += playerLines.Length * Game1.smallFont.LineSpacing;
            height += separatorSpace;
        }

        if (npcLines != null)
        {
            height += npcLines.Length * (Game1.smallFont.LineSpacing + 4);
        }
        else if (IsThinking())
        {
            height += Game1.smallFont.LineSpacing;
        }

        return height;
    }

    private void UpdateScrollThumbRegion()
    {
        if (_chatContentHeight <= _chatRegion.Height)
        {
            _scrollThumbRegion = Rectangle.Empty;
            return;
        }

        int maxScroll = _chatContentHeight - _chatRegion.Height;
        float thumbRatio = (float)_chatRegion.Height / _chatContentHeight;
        int thumbHeight = Math.Max(20, (int)(_scrollTrackRegion.Height * thumbRatio));
        float scrollPercent = maxScroll > 0 ? (float)_chatScrollOffset / maxScroll : 0f;
        int thumbY = _scrollTrackRegion.Y + (int)((_scrollTrackRegion.Height - thumbHeight) * scrollPercent);

        _scrollThumbRegion = new Rectangle(
            _scrollTrackRegion.X + 4,
            thumbY,
            _scrollTrackRegion.Width - 8,
            thumbHeight
        );
    }

    private void DrawScrollbar(SpriteBatch b)
    {
        if (!CanScroll())
            return;

        // Track background
        b.Draw(Game1.staminaRect, _scrollTrackRegion, new Color(139, 90, 43) * 0.3f);

        // Thumb
        Color thumbColor = _scrollThumbHeld ? new Color(221, 148, 25) : new Color(191, 118, 15);
        b.Draw(Game1.staminaRect, _scrollThumbRegion, thumbColor);
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
        _lastNpcMessageForScroll = null;
        _chatScrollOffset = 0;

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
