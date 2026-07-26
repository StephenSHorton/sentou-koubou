using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace MpTeammateView;

/// <summary>
/// One host per <see cref="NMultiplayerPlayerState"/> row.
/// Holds potion icons always; hand mini-cards when that player is in combat.
/// Attach on player-state ready (not only combat setup) so UI is not missed.
/// </summary>
public partial class TeammateViewHost : Control
{
    public const string NodeName = "MpTeammateViewHost";

    private NMultiplayerPlayerState? _state;
    private Control? _spacer;
    private HBoxContainer? _potions;
    private HBoxContainer? _hands;
    private CardPile? _subscribedHand;
    private Action? _handChangedHandler;
    private float _retryTimer;
    private const float RetryInterval = 0.35f;

    public static void Attach(NMultiplayerPlayerState state)
    {
        if (state.GetNodeOrNull(NodeName) != null)
            return;
        var host = new TeammateViewHost();
        host.Name = NodeName;
        state.AddChild(host);
        host.Bootstrap(state);
    }

    public static void Detach(NMultiplayerPlayerState state)
    {
        var existing = state.GetNodeOrNull<TeammateViewHost>(NodeName);
        existing?.CleanupAndFree();
    }

    private void Bootstrap(NMultiplayerPlayerState state)
    {
        _state = state;
        MouseFilter = MouseFilterEnum.Ignore;

        _spacer = new Control
        {
            Name = "MpTeammateSpacer",
            CustomMinimumSize = Vector2.Zero,
            MouseFilter = MouseFilterEnum.Ignore,
        };

        try
        {
            var top = state.GetNode<HBoxContainer>("TopInfoContainer");
            top.AddChild(_spacer);
        }
        catch (Exception e)
        {
            MainFile.Logger.Warn($"TopInfoContainer missing on player state: {e.Message}");
        }

        _potions = new HBoxContainer
        {
            Name = "PotionRow",
            MouseFilter = MouseFilterEnum.Stop,
        };
        _potions.AddThemeConstantOverride("separation", (int)DisplayConfig.PotionSeparation);
        AddChild(_potions);

        _hands = new HBoxContainer
        {
            Name = "HandRow",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _hands.AddThemeConstantOverride("separation", (int)DisplayConfig.CardSpacing);
        AddChild(_hands);

        SubscribePotionEvents();
        RefreshPotions();
        TrySubscribeHand();
        RefreshHands();
    }

    public override void _Process(double delta)
    {
        if (_state == null || !GodotObject.IsInstanceValid(_state) || _spacer == null)
            return;

        // Keep host aligned with the layout spacer inside TopInfoContainer.
        if (_spacer.IsInsideTree())
        {
            GlobalPosition = _spacer.GlobalPosition + new Vector2(0f, DisplayConfig.PotionYNudge);
        }

        // Hand pile may not exist when the row first appears — retry until combat starts.
        _retryTimer += (float)delta;
        if (_retryTimer < RetryInterval)
            return;
        _retryTimer = 0f;

        if (_subscribedHand == null && CombatManager.Instance.IsInProgress)
            TrySubscribeHand();
    }

    private void SubscribePotionEvents()
    {
        var player = _state?.Player;
        if (player == null)
            return;
        player.PotionProcured += OnPotionChanged;
        player.PotionDiscarded += OnPotionChanged;
        player.UsedPotionRemoved += OnPotionChanged;
    }

    private void UnsubscribePotionEvents()
    {
        var player = _state?.Player;
        if (player == null)
            return;
        try
        {
            player.PotionProcured -= OnPotionChanged;
            player.PotionDiscarded -= OnPotionChanged;
            player.UsedPotionRemoved -= OnPotionChanged;
        }
        catch
        {
            // player may already be torn down
        }
    }

    private void OnPotionChanged(PotionModel _)
    {
        RefreshPotions();
    }

    private void TrySubscribeHand()
    {
        var player = _state?.Player;
        if (player == null || LocalContext.IsMe(player))
            return;

        var pcs = player.PlayerCombatState;
        if (pcs?.Hand == null)
            return;

        if (ReferenceEquals(_subscribedHand, pcs.Hand))
            return;

        UnsubscribeHand();
        _subscribedHand = pcs.Hand;
        _handChangedHandler = () =>
        {
            // Defer so batch draw/play settles.
            Callable.From(RefreshHands).CallDeferred();
        };
        _subscribedHand.ContentsChanged += _handChangedHandler;
        RefreshHands();
        MainFile.Logger.Info($"Hand subscribed for player {player.NetId}");
    }

    private void UnsubscribeHand()
    {
        if (_subscribedHand != null && _handChangedHandler != null)
        {
            try
            {
                _subscribedHand.ContentsChanged -= _handChangedHandler;
            }
            catch
            {
                // ignore
            }
        }
        _subscribedHand = null;
        _handChangedHandler = null;
    }

    public void RefreshPotions()
    {
        if (_potions == null || _state?.Player == null)
            return;

        foreach (var child in _potions.GetChildren())
            child.QueueFree();

        int count = 0;
        foreach (var potion in _state.Player.PotionSlots)
        {
            if (potion == null)
                continue;
            _potions.AddChild(PotionSlot.Create(potion));
            count++;
        }

        float width = DisplayConfig.PotionContentWidth(count);
        _potions.CustomMinimumSize = new Vector2(width, DisplayConfig.PotionSlotPx + 4f);
        _potions.Visible = count > 0;
        UpdateSpacer();
    }

    public void RefreshHands()
    {
        if (_hands == null || _state?.Player == null)
            return;

        if (!CombatManager.Instance.IsInProgress || LocalContext.IsMe(_state.Player))
        {
            ClearHandsUi();
            return;
        }

        var hand = _state.Player.PlayerCombatState?.Hand;
        if (hand == null)
        {
            ClearHandsUi();
            return;
        }

        // Ensure subscription even if combat started after attach.
        if (!ReferenceEquals(_subscribedHand, hand))
            TrySubscribeHand();

        var cards = hand.Cards;
        // Rebuild simply — hand size is small.
        foreach (var child in _hands.GetChildren())
            child.QueueFree();

        foreach (var card in cards)
        {
            try
            {
                _hands.AddChild(MiniHandCard.Create(card));
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"Mini card create failed: {e.Message}");
            }
        }

        float width = DisplayConfig.HandContentWidth(cards.Count);
        _hands.CustomMinimumSize = new Vector2(width, DisplayConfig.ScaledCardSize.Y);
        _hands.Visible = cards.Count > 0;
        // Place hands to the right of potions
        _hands.Position = new Vector2(_potions?.CustomMinimumSize.X + 6f ?? 0f, DisplayConfig.PotionSlotPx + 4f);
        UpdateSpacer();
    }

    private void ClearHandsUi()
    {
        if (_hands == null)
            return;
        foreach (var child in _hands.GetChildren())
            child.QueueFree();
        _hands.Visible = false;
        _hands.CustomMinimumSize = Vector2.Zero;
        UpdateSpacer();
    }

    private void UpdateSpacer()
    {
        if (_spacer == null)
            return;
        float potionW = _potions is { Visible: true } ? _potions.CustomMinimumSize.X : 0f;
        float handW = _hands is { Visible: true } ? _hands.CustomMinimumSize.X : 0f;
        float w = Math.Max(potionW, handW);
        _spacer.CustomMinimumSize = new Vector2(w, 0f);
    }

    public void OnCombatEnded()
    {
        UnsubscribeHand();
        ClearHandsUi();
    }

    public void OnCombatRefresh()
    {
        TrySubscribeHand();
        RefreshHands();
    }

    public void CleanupAndFree()
    {
        UnsubscribePotionEvents();
        UnsubscribeHand();
        if (_spacer != null && GodotObject.IsInstanceValid(_spacer))
            _spacer.QueueFree();
        _spacer = null;
        QueueFree();
    }
}
