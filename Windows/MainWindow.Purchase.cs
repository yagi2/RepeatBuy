using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Component.GUI;
using RepeatBuy.Localization;

namespace RepeatBuy.Windows;

public partial class MainWindow
{
    private const int MaxPerCallback = 99;

    private enum StopMode
    {
        FixedCount = 0,
        UntilOwned = 1,
        GilFloor = 2,
        InventoryFull = 3
    }

    private sealed class PurchaseSession
    {
        public volatile bool Running;
        public StopMode Mode;
        public int FixedCount;
        public int UntilOwned;
        public long GilFloor;

        public uint ItemId;
        public int RealIndex;
        public int VisibleIndex;
        public long UnitPrice;
        public string ItemName = string.Empty;

        public int BoughtSoFar;
        public int InitialOwned;
        public long StartingGil;
        public DateTime NextActionAt = DateTime.MinValue;
        public bool CancelRequested;
    }

    private readonly PurchaseSession purchaseSession = new();

    // confirm modal state
    private bool openConfirmRequested;
    private string confirmReasonText = string.Empty;

    private int inputFixedCount;
    private int inputUntilOwned;
    private long inputGilFloor;

    private void DrawIdleState()
    {
        if (inputFixedCount <= 0) inputFixedCount = configuration.LastFixedCount;
        if (inputUntilOwned <= 0) inputUntilOwned = configuration.LastUntilOwned;
        if (inputGilFloor < 0) inputGilFloor = configuration.LastGilFloor;

        bool hasItem = chosen.HasValue;

        ImGui.TextUnformatted(Strings.ModeHeader);

        int mode = configuration.SelectedMode;
        if (ImGui.RadioButton(Strings.ModeFixed + "##mode0", mode == 0)) mode = 0;
        ImGui.SameLine();
        if (ImGui.RadioButton(Strings.ModeUntilOwned + "##mode1", mode == 1)) mode = 1;
        if (ImGui.RadioButton(Strings.ModeGilFloor + "##mode2", mode == 2)) mode = 2;
        ImGui.SameLine();
        if (ImGui.RadioButton(Strings.ModeInventoryFull + "##mode3", mode == 3)) mode = 3;
        if (mode != configuration.SelectedMode)
        {
            configuration.SelectedMode = mode;
            configuration.Save();
        }

        ImGui.Spacing();

        switch ((StopMode)mode)
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

        bool canBuy = hasItem && chosen!.Value.UnitPrice > 0;
        if (!canBuy) ImGui.BeginDisabled();
        if (ImGui.Button(Strings.EstimateBuy, new Vector2(120f, 0)))
            RequestConfirmModal(chosen!.Value, (StopMode)mode);
        if (!canBuy) ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.SmallButton("Settings##cfg"))
            toggleConfigUi();
    }

    private void DrawRunningState()
    {
        ImGui.TextUnformatted(Strings.Running);
        ImGui.TextUnformatted(string.Format(Strings.Bought, purchaseSession.BoughtSoFar));

        if (ImGui.Button(Strings.Stop, new Vector2(120f, 0)))
            CancelPurchase();
    }

    private void RequestConfirmModal(ShopItemInfo info, StopMode mode)
    {
        purchaseSession.Mode = mode;
        purchaseSession.FixedCount = inputFixedCount;
        purchaseSession.UntilOwned = inputUntilOwned;
        purchaseSession.GilFloor = inputGilFloor;
        purchaseSession.ItemId = info.ItemId;
        purchaseSession.RealIndex = info.RealIndex;
        purchaseSession.VisibleIndex = info.VisibleIndex;
        purchaseSession.UnitPrice = info.UnitPrice;
        purchaseSession.ItemName = info.Name;
        openConfirmRequested = true;
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
        int owned = CountOwned(s.ItemId);

        long estimatedQty = EstimateQuantity(s, owned, gil, out string note);
        long estimatedTotal = estimatedQty * s.UnitPrice;
        bool cannotAfford = estimatedTotal > gil;

        ImGui.TextUnformatted(Strings.ConfirmTitle);
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextUnformatted($"{Strings.ConfirmItem} {s.ItemName}");
        ImGui.TextUnformatted($"{Strings.ConfirmUnit} {s.UnitPrice:N0}");
        ImGui.TextUnformatted($"{Strings.ConfirmAmount} {estimatedQty:N0}");
        ImGui.TextUnformatted($"{Strings.ConfirmTotal} {estimatedTotal:N0}");
        ImGui.TextUnformatted($"{Strings.ConfirmHaveGil} {gil:N0}");

        if (!string.IsNullOrEmpty(note))
        {
            ImGui.Spacing();
            ImGui.TextDisabled(note);
        }

        if (cannotAfford)
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1f, 0.35f, 0.35f, 1f), Strings.ConfirmCannotAfford);
        }

        if (s.Mode != StopMode.FixedCount)
        {
            ImGui.TextDisabled(Strings.ConfirmEstimateOnly);
        }

        ImGui.Spacing();
        if (ImGui.Button(Strings.ConfirmStart, new Vector2(120f, 0)))
        {
            ImGui.CloseCurrentPopup();
            BeginPurchase();
        }
        ImGui.SameLine();
        if (ImGui.Button(Strings.Cancel, new Vector2(120f, 0)))
        {
            ImGui.CloseCurrentPopup();
        }
        ImGui.EndPopup();
    }

    private static long EstimateQuantity(PurchaseSession s, int owned, long gil, out string note)
    {
        note = string.Empty;
        long maxByGil = s.UnitPrice > 0 ? gil / s.UnitPrice : 0;
        switch (s.Mode)
        {
            case StopMode.FixedCount:
                return s.FixedCount;
            case StopMode.UntilOwned:
                return Math.Max(0, s.UntilOwned - owned);
            case StopMode.GilFloor:
            {
                long spendable = gil - s.GilFloor;
                if (spendable <= 0 || s.UnitPrice == 0) return 0;
                return spendable / s.UnitPrice;
            }
            case StopMode.InventoryFull:
            {
                int maxStack = GetItemMaxStack(s.ItemId);
                int free = CountInventoryFreeSlots();
                long capByInv = (long)free * Math.Max(1, maxStack);
                note = "≈ free slots × stack size";
                return Math.Min(capByInv, maxByGil);
            }
        }
        return 0;
    }

    private void BeginPurchase()
    {
        if (purchaseSession.Running) return;

        purchaseSession.Running = true;
        purchaseSession.CancelRequested = false;
        purchaseSession.BoughtSoFar = 0;
        purchaseSession.InitialOwned = CountOwned(purchaseSession.ItemId);
        purchaseSession.StartingGil = GetCurrentGil();
        purchaseSession.NextActionAt = DateTime.UtcNow;
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
                StopPurchase(Strings.Reasons_Cancelled);
                return;
            }

            if (!IsShopUIOpen())
            {
                StopPurchase(Strings.Reasons_ShopClosed);
                return;
            }

            var now = DateTime.UtcNow;
            if (now < s.NextActionAt) return;

            // gil check
            long gil = GetCurrentGil();
            if (gil < s.UnitPrice)
            {
                StopPurchase(Strings.Reasons_NotEnoughGil);
                return;
            }
            if (s.Mode == StopMode.GilFloor && gil <= s.GilFloor)
            {
                StopPurchase(Strings.Reasons_GilLow);
                return;
            }

            // inventory check
            int free = CountInventoryFreeSlots();
            int owned = CountOwned(s.ItemId);
            int maxStack = GetItemMaxStack(s.ItemId);

            if (free == 0)
            {
                bool roomInExistingStack = maxStack > 1 && owned > 0 && (owned % maxStack) != 0;
                if (!roomInExistingStack)
                {
                    StopPurchase(Strings.Reasons_InventoryFull);
                    return;
                }
            }

            // mode-specific termination
            int batchTarget = ComputeBatchSize(s, owned, gil, free, maxStack);
            if (batchTarget <= 0)
            {
                StopPurchase(ReasonForCompletion(s));
                return;
            }

            int batch = Math.Min(MaxPerCallback, batchTarget);

            // Buy `batch` items at the selected real index via ShopEventHandler.
            bool ok = TryExecuteBuy(s.RealIndex, batch);
            if (!ok)
            {
                StopPurchase(Strings.Reasons_Unexpected);
                return;
            }

            s.BoughtSoFar += batch;
            int delay = Math.Max(50, configuration.PurchaseDelayMs);
            s.NextActionAt = now.AddMilliseconds(delay);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[RepeatBuy] purchase loop crashed");
            StopPurchase(Strings.Reasons_Unexpected);
        }
    }

    private static int ComputeBatchSize(PurchaseSession s, int owned, long gil, int free, int maxStack)
    {
        long byGil = s.UnitPrice == 0 ? 0 : gil / s.UnitPrice;
        long bySpace = (long)Math.Max(0, free) * Math.Max(1, maxStack);

        long byMode;
        switch (s.Mode)
        {
            case StopMode.FixedCount:
                byMode = Math.Max(0, s.FixedCount - s.BoughtSoFar);
                break;
            case StopMode.UntilOwned:
                byMode = Math.Max(0, s.UntilOwned - owned);
                break;
            case StopMode.GilFloor:
            {
                long spendable = gil - s.GilFloor;
                byMode = s.UnitPrice == 0 ? 0 : Math.Max(0, spendable / s.UnitPrice);
                break;
            }
            case StopMode.InventoryFull:
                byMode = bySpace;
                break;
            default:
                byMode = 0;
                break;
        }

        long limit = Math.Min(byGil, Math.Min(byMode, Math.Max(bySpace, 1)));
        if (limit > int.MaxValue) limit = int.MaxValue;
        return (int)limit;
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
        purchaseSession.Running = false;
        purchaseSession.CancelRequested = false;
        Plugin.ChatGui.Print($"[RepeatBuy] {reason} ({string.Format(Strings.Bought, purchaseSession.BoughtSoFar)})");
    }

    /// <summary>
    /// Sets BuyItemIndex on the active ShopEventHandler then calls ExecuteBuy.
    /// Returns false if the handler is not reachable or a transaction is already
    /// in flight (we wait for the next tick instead of stacking buys).
    /// </summary>
    private static unsafe bool TryExecuteBuy(int realIndex, int qty)
    {
        try
        {
            var handler = GetShopEventHandler();
            if (handler == null) return false;
            if (handler->WaitingForTransactionToFinish) return true; // hold and retry

            handler->BuyItemIndex = realIndex;
            handler->ExecuteBuy(qty);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[RepeatBuy] ExecuteBuy failed");
            return false;
        }
    }
}
