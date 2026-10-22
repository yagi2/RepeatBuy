using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ItemRow = Lumina.Excel.Sheets.Item;

namespace RepeatBuy.Windows;

public partial class MainWindow
{
    private const string ShopAddonName = "Shop";
    private const uint GilItemId = 1u;

    private Vector2 lastAnchor = new(float.NaN, float.NaN);

    public const uint HqItemIdOffset = 1_000_000;

    public readonly record struct ShopItemInfo(
        int RealIndex,
        int VisibleIndex,
        uint ItemId,
        long UnitPrice,
        string Name,
        int OwnedAtVendor,
        bool IsHQ,
        bool IsUnique,
        int ShopStack);

    public readonly record struct BagSpace(int FreeSlots, int PartialRoom, int MaxStack)
    {
        public long Capacity => PurchaseMath.StackCapacity(FreeSlots, MaxStack, PartialRoom);
    }

    internal unsafe bool IsShopUIOpen()
    {
        return TryGetShopUI(out _, out _);
    }

    private static unsafe bool TryGetShopUI(out Vector2 topLeft, out Vector2 size)
    {
        topLeft = size = Vector2.Zero;

        for (int rootIndex = 0; rootIndex < 2; rootIndex++)
        {
            var addon = Plugin.GameGui.GetAddonByName(ShopAddonName, rootIndex);
            if (addon == null || addon.Address == nint.Zero)
                continue;

            var unit = (AtkUnitBase*)addon.Address;
            if (unit == null || !unit->IsVisible)
                continue;

            var rootNode = unit->RootNode;
            if (rootNode == null)
                continue;

            int width = rootNode->Width;
            int height = rootNode->Height;
            if (width <= 0 || height <= 0)
                continue;

            float sx = (rootNode->ScaleX > 0f && float.IsFinite(rootNode->ScaleX)) ? rootNode->ScaleX : 1f;
            float sy = (rootNode->ScaleY > 0f && float.IsFinite(rootNode->ScaleY)) ? rootNode->ScaleY : 1f;

            topLeft = new Vector2(unit->X, unit->Y);
            size = new Vector2(width * sx, height * sy);
            return true;
        }

        return false;
    }

    internal static unsafe AtkUnitBase* GetShopAddon()
    {
        for (int rootIndex = 0; rootIndex < 2; rootIndex++)
        {
            var addon = Plugin.GameGui.GetAddonByName(ShopAddonName, rootIndex);
            if (addon == null || addon.Address == nint.Zero)
                continue;
            var unit = (AtkUnitBase*)addon.Address;
            if (unit != null && unit->IsVisible)
                return unit;
        }
        return null;
    }

    private void AnchorToShop()
    {
        if (!TryGetShopUI(out var uiPos, out var uiSize))
            return;

        float scaledGap = AnchorGapX * ImGui.GetIO().FontGlobalScale;
        var targetPos = new Vector2(uiPos.X + uiSize.X + scaledGap, uiPos.Y);
        targetPos = ClampToViewport(targetPos);

        const float SnapDistanceSquared = 1f;
        if (!float.IsNaN(lastAnchor.X) && Vector2.DistanceSquared(targetPos, lastAnchor) < SnapDistanceSquared)
            return;

        Position = lastAnchor = targetPos;
    }

    private static Vector2 ClampToViewport(Vector2 target)
    {
        var viewport = ImGui.GetMainViewport();
        var windowSize = ImGui.GetWindowSize();
        float minX = viewport.WorkPos.X;
        float minY = viewport.WorkPos.Y;
        float maxX = Math.Max(minX, viewport.WorkPos.X + viewport.WorkSize.X - windowSize.X);
        float maxY = Math.Max(minY, viewport.WorkPos.Y + viewport.WorkSize.Y - windowSize.Y);
        return new Vector2(
            Math.Clamp(target.X, minX, maxX),
            Math.Clamp(target.Y, minY, maxY));
    }

    public void ClearAnchor()
    {
        Position = null;
        lastAnchor = new Vector2(float.NaN, float.NaN);
    }

    /// <summary>
    /// Resolves the active ShopEventHandler by walking AgentShop.EventReceiver →
    /// AgentProxy.Handler. Returns null when the shop is not active.
    /// </summary>
    internal static unsafe bool IsShopAgentActive()
    {
        try
        {
            var agent = AgentShop.Instance();
            return agent != null && agent->IsAgentActive();
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "[RepeatBuy] IsShopAgentActive failed");
            return false;
        }
    }

    internal static unsafe ShopEventHandler* GetShopEventHandler()
    {
        try
        {
            var agent = AgentShop.Instance();
            if (agent == null) return null;
            var proxy = (ShopEventHandler.AgentProxy*)agent->EventReceiver;
            if (proxy == null) return null;
            return proxy->Handler;
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "[RepeatBuy] GetShopEventHandler failed");
            return null;
        }
    }

    /// <summary>
    /// Reads the visible items currently being offered by the open NPC shop.
    /// Each row carries both a visible-list index (used by the shop callback)
    /// and a real-list index (used by ShopEventHandler.BuyItemIndex).
    /// </summary>
    internal static unsafe List<ShopItemInfo> ReadShopItems()
    {
        var rows = new List<ShopItemInfo>();
        var handler = GetShopEventHandler();
        if (handler == null) return rows;

        try
        {
            int visibleCount = handler->VisibleItemsCount;
            int itemsCount = handler->ItemsCount;
            if (visibleCount <= 0 || itemsCount <= 0) return rows;

            var items = handler->Items;
            var visible = handler->VisibleItems;

            for (int i = 0; i < visibleCount && i < visible.Length; i++)
            {
                int realIdx = visible[i];
                if (realIdx < 0 || realIdx >= itemsCount || realIdx >= items.Length) continue;

                ref var entry = ref items[realIdx];
                if (entry.ItemId == 0) continue;

                string name = ResolveItemName(entry.ItemId);
                if (entry.IsHQ) name += " (HQ)";

                rows.Add(new ShopItemInfo(
                    RealIndex: realIdx,
                    VisibleIndex: i,
                    ItemId: entry.ItemId,
                    UnitPrice: entry.PriceBuy,
                    Name: name,
                    OwnedAtVendor: entry.NumOwned,
                    IsHQ: entry.IsHQ,
                    IsUnique: entry.IsUnique,
                    ShopStack: Math.Max(0, entry.StackSize)
                ));
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Warning(ex, "[RepeatBuy] ReadShopItems failed");
        }

        return rows;
    }

    private static string ResolveItemName(uint itemId)
    {
        try
        {
            var sheet = Plugin.DataManager.GetExcelSheet<ItemRow>();
            var row = sheet?.GetRow(itemId);
            if (row.HasValue)
                return row.Value.Name.ExtractText();
        }
        catch
        {
            // ignore
        }
        return $"#{itemId}";
    }

    internal static unsafe long GetCurrentGil()
    {
        var inv = InventoryManager.Instance();
        if (inv == null) return 0;
        return inv->GetItemCountInContainer(GilItemId, InventoryType.Currency);
    }

    internal static uint ToInventoryItemId(uint itemId, bool hq)
        => itemId == 0 ? 0 : hq ? itemId + HqItemIdOffset : itemId;

    internal static unsafe int CountOwned(uint itemId, bool hq = false)
    {
        uint inventoryId = ToInventoryItemId(itemId, hq);
        if (inventoryId == 0) return 0;
        var inv = InventoryManager.Instance();
        if (inv == null) return 0;

        int total = 0;
        foreach (var p in OwnedSearchPages)
        {
            int c = inv->GetItemCountInContainer(inventoryId, p);
            if (c > 0) total += c;
        }
        return total;
    }

    internal static unsafe BagSpace MeasureBagSpace(uint itemId, bool hq)
    {
        int maxStack = GetItemMaxStack(itemId);
        uint inventoryId = ToInventoryItemId(itemId, hq);
        var inv = InventoryManager.Instance();
        if (inv == null || inventoryId == 0)
            return new BagSpace(0, 0, maxStack);

        int free = 0;
        int partial = 0;
        foreach (var page in MainBagPages)
        {
            var container = inv->GetInventoryContainer(page);
            if (container == null) continue;
            for (int s = 0; s < container->Size; s++)
            {
                var slot = container->GetInventorySlot(s);
                if (slot == null || slot->ItemId == 0)
                {
                    free++;
                    continue;
                }

                if (slot->ItemId == inventoryId && slot->Quantity < maxStack)
                    partial += maxStack - slot->Quantity;
            }
        }

        return new BagSpace(free, partial, maxStack);
    }

    internal static int GetItemMaxStack(uint itemId)
    {
        try
        {
            var sheet = Plugin.DataManager.GetExcelSheet<ItemRow>();
            var row = sheet?.GetRow(itemId);
            if (row.HasValue)
                return Math.Max(1, (int)row.Value.StackSize);
        }
        catch { }
        return 1;
    }

    private static readonly InventoryType[] OwnedSearchPages =
    {
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4,
        InventoryType.SaddleBag1,
        InventoryType.SaddleBag2,
        InventoryType.PremiumSaddleBag1,
        InventoryType.PremiumSaddleBag2
    };

    private static readonly InventoryType[] MainBagPages =
    {
        InventoryType.Inventory1,
        InventoryType.Inventory2,
        InventoryType.Inventory3,
        InventoryType.Inventory4
    };
}
