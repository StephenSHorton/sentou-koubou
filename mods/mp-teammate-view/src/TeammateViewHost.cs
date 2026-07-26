using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MpTeammateView.Utils;

namespace MpTeammateView;

/// <summary>
/// One host per <see cref="NMultiplayerPlayerState"/> row.
/// Potions always; hand mini-cards in combat (toggleable).
/// Attach on player-state ready + retry hand subscription for reliability.
/// </summary>
public partial class TeammateViewHost : Control
{
    public const string NodeName = "MpTeammateViewHost";

    private static bool _handsHidden;

    private NMultiplayerPlayerState? _state;
    private Control? _spacer;
    private HBoxContainer? _potions;
    private HBoxContainer? _hands;
    private CardPile? _subscribedHand;
    private Action? _handChangedHandler;
    private float _retryTimer;
    private const float RetryInterval = 0.35f;

    private Vector2 _dragStartMouse;
    private Vector2 _dragStartOffset;
    private bool _isDragging;
    private int _lastSnapshotVersion = -1;

    public static bool HandsHidden
    {
        get => _handsHidden;
        set
        {
            if (_handsHidden == value) return;
            _handsHidden = value;
            RefreshAllFromSettings();
        }
    }

    public static void ToggleHandsVisibility() => HandsHidden = !HandsHidden;

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

    public static void RefreshAllFromSettings()
    {
        foreach (var host in EnumerateHosts())
        {
            try
            {
                host.RefreshPotions();
                host.OnCombatRefresh();
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"Host settings refresh: {e.Message}");
            }
        }
    }

    internal static IEnumerable<TeammateViewHost> EnumerateHosts()
    {
        var run = NRun.Instance;
        var container = run?.GlobalUi?.MultiplayerPlayerContainer;
        if (container == null)
            yield break;

        for (int i = 0; i < container.GetChildCount(); i++)
        {
            if (container.GetChild(i) is not NMultiplayerPlayerState ps)
                continue;
            var host = ps.GetNodeOrNull<TeammateViewHost>(NodeName);
            if (host != null)
                yield return host;
        }
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
        AddChild(_potions);

        _hands = new HBoxContainer
        {
            Name = "HandRow",
            MouseFilter = MouseFilterEnum.Ignore,
        };
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

        var snapshot = LayoutSettingsSnapshot.Current;
        if (snapshot.Version != _lastSnapshotVersion)
        {
            _lastSnapshotVersion = snapshot.Version;
            ApplyThemeFromSettings();
        }

        if (_spacer.IsInsideTree())
        {
            var potionOffset = PotionDisplaySettings.GetAutoOffset() + PotionDisplaySettings.GetUserOffset();
            if (_potions != null)
            {
                // Host root follows spacer + potion offsets; hands use relative layout below potions.
                GlobalPosition = _spacer.GlobalPosition + potionOffset;
            }

            if (snapshot.ManualPositioningEnabled && _hands != null && !_isDragging && _hands.Visible)
            {
                var handGlobal = ResolveHandGlobalPosition(snapshot);
                _hands.GlobalPosition = handGlobal;
            }
        }

        _retryTimer += (float)delta;
        if (_retryTimer < RetryInterval)
            return;
        _retryTimer = 0f;

        if (_subscribedHand == null && CombatManager.Instance.IsInProgress)
            TrySubscribeHand();
    }

    private void ApplyThemeFromSettings()
    {
        if (_potions != null)
            _potions.AddThemeConstantOverride("separation", (int)PotionDisplaySettings.GetSeparation());
        if (_hands != null)
            _hands.AddThemeConstantOverride("separation", (int)HandDisplaySettings.GetCardSpacing());
        RefreshPotions();
        RefreshHands();
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

    private void OnPotionChanged(PotionModel _) => RefreshPotions();

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
        _handChangedHandler = () => Callable.From(RefreshHands).CallDeferred();
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
            _potions.AddChild(PotionSlot.Create(_state.Player, potion));
            count++;
        }

        float width = PotionDisplaySettings.GetContentWidth(count);
        _potions.CustomMinimumSize = new Vector2(width, PotionDisplaySettings.GetContainerHeight());
        _potions.AddThemeConstantOverride("separation", (int)PotionDisplaySettings.GetSeparation());
        _potions.Visible = count > 0;
        _potions.Position = Vector2.Zero;
        UpdateSpacer();
    }

    public void RefreshHands()
    {
        if (_hands == null || _state?.Player == null)
            return;

        if (HandsHidden || !CombatManager.Instance.IsInProgress || LocalContext.IsMe(_state.Player))
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

        if (!ReferenceEquals(_subscribedHand, hand))
            TrySubscribeHand();

        var cards = hand.Cards;
        foreach (var child in _hands.GetChildren())
            child.QueueFree();

        var player = _state.Player;
        foreach (var card in cards)
        {
            try
            {
                _hands.AddChild(MiniHandCard.Create(card, player, HandleHandDragInput));
            }
            catch (Exception e)
            {
                MainFile.Logger.Warn($"Mini card create failed: {e.Message}");
            }
        }

        var snapshot = LayoutSettingsSnapshot.Current;
        float width = snapshot.GetContentWidth(cards.Count);
        _hands.CustomMinimumSize = new Vector2(width, snapshot.ScaledCardSize.Y);
        _hands.AddThemeConstantOverride("separation", (int)snapshot.CardSpacing);
        _hands.Visible = cards.Count > 0;
        _hands.MouseFilter = snapshot.ManualPositioningEnabled
            ? MouseFilterEnum.Stop
            : MouseFilterEnum.Ignore;

        if (!snapshot.ManualPositioningEnabled)
        {
            float potionW = _potions is { Visible: true } ? _potions.CustomMinimumSize.X : 0f;
            _hands.Position = new Vector2(potionW + 6f, PotionDisplaySettings.GetContainerHeight())
                              + snapshot.UserOffset + snapshot.GetSlotOffset(GetSlotIndex());
        }

        UpdateSpacer();
    }

    private void HandleHandDragInput(InputEvent @event)
    {
        if (!CanReceiveMouseInput() || !HandDisplaySettings.IsManualPositioningEnabled() ||
            _state == null || _hands == null || !_hands.Visible)
        {
            _isDragging = false;
            return;
        }

        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseButton:
                if (mouseButton.Pressed)
                {
                    _isDragging = true;
                    _dragStartMouse = mouseButton.GlobalPosition;
                    _dragStartOffset = HandDisplaySettings.GetSlotOffset(GetSlotIndex());
                    if (_dragStartOffset == Vector2.Zero)
                        _dragStartOffset = _hands.GlobalPosition;
                    GetViewport().SetInputAsHandled();
                }
                else if (_isDragging)
                {
                    _isDragging = false;
                    GetViewport().SetInputAsHandled();
                }

                break;
            case InputEventMouseMotion mouseMotion when _isDragging:
                var deltaPosition = mouseMotion.GlobalPosition - _dragStartMouse;
                HandDisplaySettings.SetSlotOffset(GetSlotIndex(), _dragStartOffset + deltaPosition);
                _hands.GlobalPosition = ResolveHandGlobalPosition(LayoutSettingsSnapshot.Current);
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    private Vector2 ResolveHandGlobalPosition(LayoutSettingsSnapshot snapshot)
    {
        if (_hands == null)
            return GlobalPosition;

        if (snapshot.ManualPositioningEnabled)
        {
            var slotOffset = snapshot.GetSlotOffset(GetSlotIndex());
            if (slotOffset == Vector2.Zero)
            {
                float potionW = _potions is { Visible: true } ? _potions.CustomMinimumSize.X : 0f;
                slotOffset = GlobalPosition
                             + new Vector2(potionW + 6f, PotionDisplaySettings.GetContainerHeight())
                             + snapshot.UserOffset;
            }

            return ClampToViewport(slotOffset, snapshot);
        }

        float potionWidth = _potions is { Visible: true } ? _potions.CustomMinimumSize.X : 0f;
        return GlobalPosition
               + new Vector2(potionWidth + 6f, PotionDisplaySettings.GetContainerHeight())
               + snapshot.UserOffset
               + snapshot.GetSlotOffset(GetSlotIndex());
    }

    private Vector2 ClampToViewport(Vector2 desiredPosition, LayoutSettingsSnapshot snapshot)
    {
        var viewport = GetViewport().GetVisibleRect();
        var contentWidth = snapshot.GetContentWidth(_hands?.GetChildCount() ?? 0);
        var contentHeight = snapshot.ScaledCardSize.Y;

        var minY = viewport.Position.Y;
        var topBar = NRun.Instance?.GlobalUi?.TopBar;
        if (topBar != null && GodotObject.IsInstanceValid(topBar) && topBar.Visible)
            minY = Mathf.Max(minY, topBar.GlobalPosition.Y + 80f);

        return new(
            Mathf.Clamp(desiredPosition.X, viewport.Position.X,
                Mathf.Max(viewport.Position.X, viewport.End.X - contentWidth)),
            Mathf.Clamp(desiredPosition.Y, minY,
                Mathf.Max(minY, viewport.End.Y - contentHeight)));
    }

    private int GetSlotIndex() => _state?.GetIndex() ?? -1;

    private static bool CanReceiveMouseInput()
    {
        if (_handsHidden || !CombatManager.Instance.IsInProgress)
            return false;
        var combatRoom = NRun.Instance?.CombatRoom;
        return combatRoom != null && ActiveScreenContext.Instance.GetCurrentScreen() == combatRoom;
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

        var snapshot = LayoutSettingsSnapshot.Current;
        float potionW = _potions is { Visible: true } ? _potions.CustomMinimumSize.X : 0f;
        float handW = 0f;
        if (_hands is { Visible: true } && snapshot.ReserveOriginalWidth && !HandsHidden)
            handW = _hands.CustomMinimumSize.X;

        // When reserve is on, spacer takes max of potion width and hand width contribution.
        // Hands often sit below potions so layout width is mostly potions; reserve still
        // keeps horizontal room for hand row when configured.
        float w = Math.Max(potionW, snapshot.ReserveOriginalWidth ? handW : potionW);
        if (!snapshot.ReserveOriginalWidth)
            w = potionW;
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
