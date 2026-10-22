using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Component.GUI;
using RepeatBuy.Localization;

namespace RepeatBuy.Windows;

public partial class MainWindow
{
    private enum BuyAttempt
    {
        Waiting,
        Sent,
        Failed
    }

    private enum PurchasePhase
    {
        Idle,
        WaitingForDelivery
    }

    private enum PurchaseStatus
    {
        Running,
        Waiting,
        Confirming
    }

    private sealed class PurchaseSession
    {
        public volatile bool Running;
        public PurchasePhase Phase;
        public PurchaseStatus Status;
        public StopMode Mode;
        public int FixedCount;
        public int UntilOwned;
        public long GilFloor;

        public uint ItemId;
        public int RealIndex;
        public int VisibleIndex;
        public long UnitPrice;
        public string ItemName = string.Empty;
        public bool IsHQ;
        public bool IsUnique;
        public int ShopStack;

        public int Delivered;
        public int DeliveredAtSend;
        public int PendingQty;
        public int InitialOwned;
        public int OwnedAtSend;
        public long StartingGil;
        public long GilAtSend;
        public DateTime NextActionAt = DateTime.MinValue;
        public DateTime WaitDeadline = DateTime.MinValue;
        public DateTime FlagClearedAt = DateTime.MinValue;
        public bool CancelRequested;
        public bool YesNoWasOpenBeforeSend;
        public bool YesNoOpenedByUs;
        public bool YesNoAccepted;
        public volatile bool InventoryEventSinceSend;
    }

    private readonly PurchaseSession purchaseSession = new();

    private static readonly TimeSpan TransactionTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan FlagClearGrace = TimeSpan.FromMilliseconds(750);

    private bool openConfirmRequested;

    private int inputFixedCount;
    private int inputUntilOwned;
    private long inputGilFloor;

    private void OnInventoryChangedRaw(IReadOnlyCollection<InventoryEventArgs> events)
    {
        var session = purchaseSession;
        if (!session.Running || session.Phase != PurchasePhase.WaitingForDelivery)
            return;

        foreach (var change in events)
        {
            uint id = change.Item.BaseItemId;
            if (id == session.ItemId || id == GilItemId)
            {
                session.InventoryEventSinceSend = true;
                return;
            }
        }
    }

    internal bool HoldOpenDuringPurchase()
    {
        if (!purchaseSession.Running)
            return false;
        if (purchaseSession.Phase != PurchasePhase.WaitingForDelivery && !purchaseSession.YesNoOpenedByUs)
            return false;
        return IsShopAgentActive();
    }

    private void DrawIdleState()
    {
        if (inputFixedCount <= 0) inputFixedCount = configuration.LastFixedCount;
        if (inputUntilOwned <= 0) inputUntilOwned = configuration.LastUntilOwned;
        if (inputGilFloor < 0) inputGilFloor = configuration.LastGilFloor;

        bool hasItem = chosen.HasValue;
        var mode = (StopMode)configuration.SelectedMode;

        ImGui.TextUnformatted(Strings.ModeHeader);

        int modeIndex = configuration.SelectedMode;
        if (ImGui.RadioButton(Strings.ModeFixed + "##mode0", modeIndex == 0)) modeIndex = 0;
        ImGui.SameLine();
        if (ImGui.RadioButton(Strings.ModeUntilOwned + "##mode1", modeIndex == 1)) modeIndex = 1;
        if (ImGui.RadioButton(Strings.ModeGilFloor + "##mode2", modeIndex == 2)) modeIndex = 2;
        ImGui.SameLine();
        if (ImGui.RadioButton(Strings.ModeInventoryFull + "##mode3", modeIndex == 3)) modeIndex = 3;
        if (modeIndex != configuration.SelectedMode)
        {
            configuration.SelectedMode = modeIndex;
            configuration.Save();
            mode = (StopMode)modeIndex;
        }

        ImGui.Spacing();

        switch (mode)
        {
            case StopMode.FixedCount:
                ImGui.SetNextItemWidth(180f);
                if (ImGui.InputInt(Strings.LabelCount + "##count", ref inputFixedCount, 1, 100))
                {
                    if (inputFixedCount < 1) inputFixedCount = 1;
                    if (inputFixedCount > 9_999_999) inputFixedCount = 9_999_999;
                    configuration.LastFixedCount = inputFixedCount;
                    configuration.Save();
                }
                break;
            case StopMode.UntilOwned:
                ImGui.SetNextItemWidth(180f);
                if (ImGui.InputInt(Strings.LabelTarget + "##target", ref inputUntilOwned, 1, 100))
                {
                    if (inputUntilOwned < 1) inputUntilOwned = 1;
                    if (inputUntilOwned > 9_999_999) inputUntilOwned = 9_999_999;
                    configuration.LastUntilOwned = inputUntilOwned;
                    configuration.Save();
                }
                break;
            case StopMode.GilFloor:
                ImGui.SetNextItemWidth(220f);
                int g = (int)Math.Min(int.MaxValue, inputGilFloor);
                if (ImGui.InputInt(Strings.LabelGilFloor + "##gilfloor", ref g, 1000, 10000))
                {
                    if (g < 0) g = 0;
                    inputGilFloor = g;
                    configuration.LastGilFloor = g;
                    configuration.Save();
                }
                break;
            case StopMode.InventoryFull:
                ImGui.TextDisabled(Strings.ModeInventoryFull);
                break;
        }

        ImGui.Spacing();

        string? blockReason = BuyBlockReason(mode);
        bool canBuy = blockReason == null;
        if (!canBuy) ImGui.BeginDisabled();
        if (ImGui.Button(Strings.EstimateBuy, new Vector2(120f, 0)))
            RequestConfirmModal(chosen!.Value, mode);
        if (!canBuy)
        {
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.TextDisabled(blockReason);
        }

        ImGui.SameLine();
        if (ImGui.SmallButton("Settings##cfg"))
            toggleConfigUi();
    }

    private string? BuyBlockReason(StopMode mode)
    {
        if (!chosen.HasValue)
            return Strings.BuyBlockedNoSelection;
        if (chosen.Value.UnitPrice <= 0)
            return Strings.BuyBlockedNoPrice;

        var row = chosen.Value;
        long gil = GetCurrentGil();
        int owned = CountOwned(row.ItemId, row.IsHQ);
        long room = MeasureBagSpace(row.ItemId, row.IsHQ).Capacity;
        var limits = PurchaseMath.Compute(
            mode,
            inputFixedCount,
            inputUntilOwned,
            owned,
            delivered: 0,
            gil,
            inputGilFloor,
            row.UnitPrice,
            room,
            shopStack: 0,
            maxPerTransaction: int.MaxValue);
        if (limits.Result <= 0)
            return Strings.BuyBlockedNothing;
        return null;
    }

    private void DrawRunningState()
    {
        string status = purchaseSession.Status switch
        {
            PurchaseStatus.Waiting => Strings.StatusWaiting,
            PurchaseStatus.Confirming => Strings.StatusConfirming,
            _ => Strings.Running
        };
        ImGui.TextUnformatted(status);
        ImGui.TextUnformatted($"{purchaseSession.ItemName}");
        ImGui.TextUnformatted(string.Format(Strings.Bought, purchaseSession.Delivered));
        long spent = Math.Max(0, purchaseSession.StartingGil - GetCurrentGil());
        ImGui.TextUnformatted(string.Format(Strings.GilSpent, spent.ToString("N0")));

        if (ImGui.Button(Strings.Stop, new Vector2(120f, 0)))
            CancelPurchase();
    }

    private void RequestConfirmModal(ShopItemInfo info, StopMode mode)
    {
        purchaseSession.Mode = mode;
        purchaseSession.FixedCount = inputFixedCount;
        purchaseSession.UntilOwned = inputUntilOwned;
        purchaseSession.GilFloor = inputGilFloor;
        CopyListing(purchaseSession, info);
        openConfirmRequested = true;
    }

    private static void CopyListing(PurchaseSession session, ShopItemInfo info)
    {
        session.ItemId = info.ItemId;
        session.RealIndex = info.RealIndex;
        session.VisibleIndex = info.VisibleIndex;
        session.UnitPrice = info.UnitPrice;
        session.ItemName = info.Name;
        session.IsHQ = info.IsHQ;
        session.IsUnique = info.IsUnique;
        session.ShopStack = info.ShopStack;
    }

    private void DrawConfirmModal()
    {
        if (openConfirmRequested)
        {
            ImGui.OpenPopup("##RepeatBuyConfirm");
            openConfirmRequested = false;
        }

        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (!ImGui.BeginPopupModal("##RepeatBuyConfirm", ImGuiWindowFlags.AlwaysAutoResize))
            return;

        var s = purchaseSession;
        long gil = GetCurrentGil();
        int owned = CountOwned(s.ItemId, s.IsHQ);
        long room = MeasureBagSpace(s.ItemId, s.IsHQ).Capacity;
        var limits = PurchaseMath.Compute(
            s.Mode,
            s.FixedCount,
            s.UntilOwned,
            owned,
            delivered: 0,
            gil,
            s.GilFloor,
            s.UnitPrice,
            room,
            shopStack: 0,
            maxPerTransaction: int.MaxValue);
        long estimatedQty = limits.Result;
        long estimatedTotal = estimatedQty * s.UnitPrice;
        long requestedCost = limits.ByMode * s.UnitPrice;
        bool cannotAfford = requestedCost > gil;

        ImGui.TextUnformatted(Strings.ConfirmTitle);
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted($"{Strings.ConfirmItem} {s.ItemName}");
        ImGui.TextUnformatted($"{Strings.ConfirmUnit} {s.UnitPrice:N0}");
        ImGui.TextUnformatted($"{Strings.ConfirmAmount} {estimatedQty:N0}");
        ImGui.TextUnformatted($"{Strings.ConfirmTotal} {estimatedTotal:N0}");
        ImGui.TextUnformatted($"{Strings.ConfirmHaveGil} {gil:N0}");

        if (limits.WasCapped)
        {
            ImGui.Spacing();
            ImGui.TextDisabled(Strings.EstimateCapped);
        }

        if (cannotAfford)
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1f, 0.35f, 0.35f, 1f), Strings.ConfirmCannotAfford);
        }

        if (s.Mode != StopMode.FixedCount)
            ImGui.TextDisabled(Strings.ConfirmEstimateOnly);

        ImGui.Spacing();
        if (estimatedQty <= 0)
            ImGui.BeginDisabled();
        if (ImGui.Button(Strings.ConfirmStart, new Vector2(120f, 0)))
        {
            ImGui.CloseCurrentPopup();
            BeginPurchase();
        }
        if (estimatedQty <= 0)
            ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button(Strings.Cancel, new Vector2(120f, 0)))
            ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
    }

    private void BeginPurchase()
    {
        if (purchaseSession.Running) return;

        var rows = ReadShopItems();
        var snapshot = new ShopItemInfo(
            purchaseSession.RealIndex,
            purchaseSession.VisibleIndex,
            purchaseSession.ItemId,
            purchaseSession.UnitPrice,
            purchaseSession.ItemName,
            0,
            purchaseSession.IsHQ,
            purchaseSession.IsUnique,
            purchaseSession.ShopStack);
        if (!TryMatchListing(rows, snapshot, out var row))
        {
            Plugin.ChatGui.PrintError($"[RepeatBuy] {Strings.Reasons_RowGone}");
            return;
        }

        chosen = row;
        CopyListing(purchaseSession, row);
        purchaseSession.Running = true;
        purchaseSession.Phase = PurchasePhase.Idle;
        purchaseSession.Status = PurchaseStatus.Running;
        purchaseSession.CancelRequested = false;
        purchaseSession.Delivered = 0;
        purchaseSession.PendingQty = 0;
        purchaseSession.InitialOwned = CountOwned(row.ItemId, row.IsHQ);
        purchaseSession.StartingGil = GetCurrentGil();
        purchaseSession.NextActionAt = DateTime.UtcNow;
        purchaseSession.YesNoOpenedByUs = false;
        purchaseSession.InventoryEventSinceSend = false;
        Plugin.Log.Information(
            "[RepeatBuy] start item={ItemId} hq={Hq} index={Index} price={Price} owned={Owned} gil={Gil}",
            row.ItemId, row.IsHQ, row.RealIndex, row.UnitPrice, purchaseSession.InitialOwned, purchaseSession.StartingGil);
    }

    public void CancelPurchase()
    {
        purchaseSession.CancelRequested = true;
    }

    private void OnFrameworkUpdate(Dalamud.Plugin.Services.IFramework _)
    {
        var s = purchaseSession;
        if (!s.Running) return;

        try
        {
            if (s.CancelRequested)
            {
                DismissOurYesNo();
                StopPurchase(Strings.Reasons_Cancelled);
                return;
            }

            if (!IsShopSessionAlive())
            {
                DismissOurYesNo();
                StopPurchase(Strings.Reasons_ShopClosed);
                return;
            }

            var now = DateTime.UtcNow;
            if (s.Phase == PurchasePhase.WaitingForDelivery)
            {
                HandleInFlight(s, now);
                return;
            }

            if (now < s.NextActionAt)
                return;

            string? context = ShopBlockReason();
            if (context != null)
            {
                StopPurchase(context);
                return;
            }

            if (!TryResolveCurrentRow(s))
            {
                StopPurchase(Strings.Reasons_RowGone);
                return;
            }

            long gil = GetCurrentGil();
            int owned = CountOwned(s.ItemId, s.IsHQ);
            s.Delivered = Math.Max(s.Delivered, Math.Max(0, owned - s.InitialOwned));
            var space = MeasureBagSpace(s.ItemId, s.IsHQ);

            if (PurchaseMath.GoalMet(s.Mode, s.FixedCount, s.UntilOwned, owned, s.Delivered, gil, s.GilFloor, space.Capacity))
            {
                StopPurchase(ReasonForCompletion(s));
                return;
            }

            if (s.IsUnique && owned > 0)
            {
                StopPurchase(Strings.Reasons_Unique);
                return;
            }

            if (gil < s.UnitPrice)
            {
                StopPurchase(Strings.Reasons_NotEnoughGil);
                return;
            }

            if (space.Capacity <= 0)
            {
                StopPurchase(Strings.Reasons_InventoryFull);
                return;
            }

            int batch = PurchaseMath.Compute(
                s.Mode,
                s.FixedCount,
                s.UntilOwned,
                owned,
                s.Delivered,
                gil,
                s.GilFloor,
                s.UnitPrice,
                space.Capacity,
                s.ShopStack).Result;
            if (batch <= 0)
            {
                StopPurchase(ReasonForCompletion(s));
                return;
            }

            bool yesNoBefore = IsSelectYesnoOpen();
            var attempt = TryExecuteBuy(s.RealIndex, batch);
            switch (attempt)
            {
                case BuyAttempt.Waiting:
                    s.Status = PurchaseStatus.Waiting;
                    if (s.FlagClearedAt == DateTime.MinValue)
                        s.FlagClearedAt = now;
                    else if (now - s.FlagClearedAt >= TransactionTimeout)
                    {
                        Plugin.Log.Warning("[RepeatBuy] shop stayed busy index={Index} qty={Qty}", s.RealIndex, batch);
                        StopPurchase(Strings.Reasons_NotReceived);
                    }
                    return;
                case BuyAttempt.Failed:
                    StopPurchase(Strings.Reasons_Unexpected);
                    return;
                case BuyAttempt.Sent:
                    s.Phase = PurchasePhase.WaitingForDelivery;
                    s.Status = PurchaseStatus.Waiting;
                    s.PendingQty = batch;
                    s.DeliveredAtSend = s.Delivered;
                    s.OwnedAtSend = owned;
                    s.GilAtSend = gil;
                    s.WaitDeadline = now + TransactionTimeout;
                    s.FlagClearedAt = DateTime.MinValue;
                    s.YesNoWasOpenBeforeSend = yesNoBefore;
                    s.YesNoOpenedByUs = !yesNoBefore && IsSelectYesnoOpen();
                    s.YesNoAccepted = false;
                    s.InventoryEventSinceSend = false;
                    return;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[RepeatBuy] purchase loop crashed");
            DismissOurYesNo();
            StopPurchase(Strings.Reasons_Unexpected);
        }
    }

    private void HandleInFlight(PurchaseSession s, DateTime now)
    {
        bool yesOpen = IsSelectYesnoOpen();
        if (yesOpen && !s.YesNoWasOpenBeforeSend)
            s.YesNoOpenedByUs = true;

        if (s.YesNoOpenedByUs && yesOpen && !s.YesNoAccepted && configuration.AutoAcceptShopConfirm)
        {
            if (AcceptSelectYesno())
                s.YesNoAccepted = true;
        }

        s.Status = s.YesNoOpenedByUs && yesOpen
            ? PurchaseStatus.Confirming
            : PurchaseStatus.Waiting;

        int owned = CountOwned(s.ItemId, s.IsHQ);
        long gil = GetCurrentGil();
        int observed = Math.Max(0, owned - s.InitialOwned);
        int batchGil = 0;
        if (s.UnitPrice > 0)
        {
            long spent = s.GilAtSend - gil;
            if (spent > 0)
                batchGil = (int)Math.Min(s.PendingQty, spent / s.UnitPrice);
        }

        s.Delivered = Math.Max(s.Delivered, Math.Max(observed, s.DeliveredAtSend + batchGil));

        bool progressed = s.Delivered > s.DeliveredAtSend;
        bool stillWaiting = IsTransactionWaiting();
        if (progressed && !stillWaiting && !yesOpen)
        {
            FinishBatch(s, now);
            return;
        }

        if (progressed && now < s.WaitDeadline)
            return;

        if (!stillWaiting && !yesOpen)
        {
            if (s.FlagClearedAt == DateTime.MinValue)
                s.FlagClearedAt = now;
            else if (now - s.FlagClearedAt >= FlagClearGrace)
            {
                if (progressed)
                    FinishBatch(s, now);
                else
                    FailDelivery(s, owned, gil);
                return;
            }
        }

        if (now >= s.WaitDeadline)
        {
            if (progressed)
                FinishBatch(s, now);
            else
                FailDelivery(s, owned, gil);
        }
    }

    private void FinishBatch(PurchaseSession s, DateTime now)
    {
        int pending = s.PendingQty;
        s.Phase = PurchasePhase.Idle;
        s.Status = PurchaseStatus.Running;
        s.PendingQty = 0;
        s.YesNoOpenedByUs = false;
        s.FlagClearedAt = DateTime.MinValue;
        s.NextActionAt = now.AddMilliseconds(Math.Max(50, configuration.PurchaseDelayMs));
        Plugin.Log.Information(
            "[RepeatBuy] batch landed delivered={Delivered} pending={Pending}",
            s.Delivered, pending);
    }

    private void FailDelivery(PurchaseSession s, int owned, long gil)
    {
        bool waiting = IsTransactionWaiting();
        Plugin.Log.Warning(
            "[RepeatBuy] purchase not received index={Index} qty={Qty} ownedBefore={OwnedBefore} ownedNow={OwnedNow} gilBefore={GilBefore} gilNow={GilNow} waiting={Waiting} inventoryEvent={InventoryEvent}",
            s.RealIndex, s.PendingQty, s.OwnedAtSend, owned, s.GilAtSend, gil, waiting, s.InventoryEventSinceSend);
        DismissOurYesNo();
        StopPurchase(Strings.Reasons_NotReceived);
    }

    private bool TryResolveCurrentRow(PurchaseSession s)
    {
        var wanted = new ShopItemInfo(
            s.RealIndex, s.VisibleIndex, s.ItemId, s.UnitPrice, s.ItemName, 0, s.IsHQ, s.IsUnique, s.ShopStack);
        if (!TryMatchListing(ReadShopItems(), wanted, out var row))
            return false;

        CopyListing(s, row);
        return true;
    }

    private bool IsShopSessionAlive()
    {
        if (IsShopUIOpen())
            return true;
        return HoldOpenDuringPurchase();
    }

    private static unsafe string? ShopBlockReason()
    {
        var handler = GetShopEventHandler();
        if (handler == null)
        {
            Plugin.Log.Debug("[RepeatBuy] ShopBlockReason: handler is null");
            return Strings.Reasons_ShopClosed;
        }
        if (handler->IsTradingWithRetainer)
        {
            Plugin.Log.Debug("[RepeatBuy] ShopBlockReason: retainer trade detected");
            return Strings.Reasons_Retainer;
        }
        // Debug logging for tab detection issues
        var isBuyback = handler->BuybackTabActive;
        var currentMode = handler->CurrentMode;
        Plugin.Log.Debug(
            "[RepeatBuy] ShopBlockReason: BuybackTabActive={Buyback}, CurrentMode={Mode}",
            isBuyback, currentMode);
        if (isBuyback)
            return Strings.Reasons_Buyback;
        // FFXIV NPC shops use CurrentMode 0 for Buy tab.
        // Some patch versions may report transient non-zero values during tab transitions.
        // We now tolerate short-lived non-zero CurrentMode by also checking the Shop addon's
        // visible state to confirm we actually see a valid buy list.
        if (currentMode != 0)
            return Strings.Reasons_NotBuyMode;
        // Additional guard: verify at least one shop item is readable to confirm the Buy tab is truly active.
        if (!IsBuyTabItemsReadable())
        {
            Plugin.Log.Debug("[RepeatBuy] ShopBlockReason: no readable items on buy tab");
            return Strings.Reasons_NotBuyMode;
        }
        return null;
    }

    /// <summary>
    /// Quick sanity check: can we actually read at least one item from the current shop list?
    /// Returns false if the handler exists but no items are visible (suggesting a non-buy tab).
    /// </summary>
    private static unsafe bool IsBuyTabItemsReadable()
    {
        var handler = GetShopEventHandler();
        if (handler == null)
            return false;
        try
        {
            int visibleCount = handler->VisibleItemsCount;
            int itemsCount = handler->ItemsCount;
            if (visibleCount <= 0 || itemsCount <= 0)
                return false;

            var items = handler->Items;
            var visible = handler->VisibleItems;

            for (int i = 0; i < visibleCount && i < visible.Length; i++)
            {
                int realIdx = visible[i];
                if (realIdx < 0 || realIdx >= itemsCount || realIdx >= items.Length)
                    continue;
                ref var entry = ref items[realIdx];
                if (entry.ItemId > 0)
                    return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static string ReasonForCompletion(PurchaseSession s)
    {
        return s.Mode switch
        {
            StopMode.FixedCount => Strings.Reasons_DoneCount,
            StopMode.UntilOwned => Strings.Reasons_TargetReached,
            StopMode.GilFloor => Strings.Reasons_GilLow,
            StopMode.InventoryFull => Strings.Reasons_InventoryFull,
            _ => Strings.Reasons_DoneCount
        };
    }

    private void StopPurchase(string reason)
    {
        int delivered = purchaseSession.Delivered;
        long spent = Math.Max(0, purchaseSession.StartingGil - GetCurrentGil());
        purchaseSession.Running = false;
        purchaseSession.Phase = PurchasePhase.Idle;
        purchaseSession.CancelRequested = false;
        purchaseSession.PendingQty = 0;
        purchaseSession.YesNoOpenedByUs = false;
        Plugin.Log.Information("[RepeatBuy] stop delivered={Delivered} gilSpent={GilSpent} reason={Reason}", delivered, spent, reason);
        Plugin.ChatGui.Print($"[RepeatBuy] {reason} ({string.Format(Strings.Bought, delivered)}, {string.Format(Strings.GilSpent, spent.ToString("N0"))})");
    }

    /// <summary>
    /// Sets BuyItemIndex and calls ExecuteBuy.
    /// Waiting means a transaction is already in flight, so the caller must not count a purchase.
    /// </summary>
    private static unsafe BuyAttempt TryExecuteBuy(int realIndex, int qty)
    {
        try
        {
            var handler = GetShopEventHandler();
            if (handler == null)
                return BuyAttempt.Failed;
            if (handler->WaitingForTransactionToFinish)
                return BuyAttempt.Waiting;

            handler->BuyItemIndex = realIndex;
            handler->ExecuteBuy(qty);
            Plugin.Log.Information(
                "[RepeatBuy] ExecuteBuy index={Index} qty={Qty} waiting={Waiting}",
                realIndex, qty, handler->WaitingForTransactionToFinish);
            return BuyAttempt.Sent;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[RepeatBuy] ExecuteBuy failed");
            return BuyAttempt.Failed;
        }
    }

    private static unsafe bool IsTransactionWaiting()
    {
        var handler = GetShopEventHandler();
        return handler != null && handler->WaitingForTransactionToFinish;
    }

    private static unsafe bool IsSelectYesnoOpen()
        => GetSelectYesno() != null;

    private static unsafe AtkUnitBase* GetSelectYesno()
    {
        for (int rootIndex = 0; rootIndex < 2; rootIndex++)
        {
            var addon = Plugin.GameGui.GetAddonByName("SelectYesno", rootIndex);
            if (addon == null || addon.Address == nint.Zero)
                continue;
            var unit = (AtkUnitBase*)addon.Address;
            if (unit != null && unit->IsVisible)
                return unit;
        }

        return null;
    }

    private static unsafe bool AcceptSelectYesno()
    {
        var unit = GetSelectYesno();
        if (unit == null)
            return false;
        return unit->FireCallbackInt(0);
    }

    private unsafe void DismissOurYesNo()
    {
        if (!purchaseSession.YesNoOpenedByUs)
            return;

        var unit = GetSelectYesno();
        if (unit != null)
            unit->FireCallbackInt(1);
        purchaseSession.YesNoOpenedByUs = false;
    }
}
