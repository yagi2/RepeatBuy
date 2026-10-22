using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Interface.Windowing;
using RepeatBuy.Localization;

namespace RepeatBuy.Windows;

public partial class MainWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly Action toggleConfigUi;

    private const float AnchorGapX = 8f;
    private const float ListMinHeight = 220f;

    private const ImGuiWindowFlags WindowFlags =
        ImGuiWindowFlags.NoCollapse;

    private bool frameworkHooked;

    /// <summary>
    /// Currently chosen shop row, persisted across frames so it stays selected
    /// even if the underlying list briefly reorders or the user reopens the panel.
    /// Cleared when the shop addon closes.
    /// </summary>
    private ShopItemInfo? chosen;

    public MainWindow(Configuration configuration, Action toggleConfigUi)
        : base(Strings.WindowTitle + "###RepeatBuyMain", WindowFlags)
    {
        this.configuration = configuration;
        this.toggleConfigUi = toggleConfigUi;

        RespectCloseHotkey = false;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(360f, 380f),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        if (!frameworkHooked)
        {
            Plugin.Framework.Update += OnFrameworkUpdate;
            Plugin.GameInventory.InventoryChangedRaw += OnInventoryChangedRaw;
            frameworkHooked = true;
        }
    }

    public void Dispose()
    {
        if (frameworkHooked)
        {
            Plugin.Framework.Update -= OnFrameworkUpdate;
            Plugin.GameInventory.InventoryChangedRaw -= OnInventoryChangedRaw;
            frameworkHooked = false;
        }
    }

    public override void PreOpenCheck()
    {
        if (HoldOpenDuringPurchase())
            return;

        if (!IsShopUIOpen())
        {
            if (purchaseSession.Running)
                CancelPurchase();
            chosen = null;
            IsOpen = false;
        }
    }

    public override void Draw()
    {
        WindowName = Strings.WindowTitle + "###RepeatBuyMain";

        if (configuration.AnchorWindow && IsShopUIOpen())
            AnchorToShop();

        if (!IsShopUIOpen())
        {
            if (HoldOpenDuringPurchase())
            {
                DrawRunningState();
                return;
            }

            if (purchaseSession.Running)
                CancelPurchase();
            chosen = null;
            IsOpen = false;
            return;
        }

        var rows = ReadShopItems();
        RefreshChosenFrom(rows);

        DrawHeader();
        ImGui.Separator();

        DrawShopList(rows);
        ImGui.Separator();

        if (purchaseSession.Running)
            DrawRunningState();
        else
            DrawIdleState();

        DrawConfirmModal();
    }

    private void RefreshChosenFrom(System.Collections.Generic.List<ShopItemInfo> rows)
    {
        if (chosen == null) return;
        var current = chosen.Value;

        if (TryMatchListing(rows, current, out var match))
        {
            chosen = match;
            return;
        }
        // item disappeared from the list — drop selection
        chosen = null;
    }

    internal static bool TryMatchListing(
        System.Collections.Generic.List<ShopItemInfo> rows,
        ShopItemInfo wanted,
        out ShopItemInfo match)
    {
        ShopItemInfo? fallback = null;
        foreach (var row in rows)
        {
            if (row.ItemId != wanted.ItemId || row.IsHQ != wanted.IsHQ || row.UnitPrice != wanted.UnitPrice)
                continue;

            if (row.RealIndex == wanted.RealIndex)
            {
                match = row;
                return true;
            }

            fallback ??= row;
        }

        if (fallback.HasValue)
        {
            match = fallback.Value;
            return true;
        }

        match = default;
        return false;
    }

    private void DrawHeader()
    {
        long gil = GetCurrentGil();

        if (chosen.HasValue)
        {
            int owned = CountOwned(chosen.Value.ItemId, chosen.Value.IsHQ);
            ImGui.TextUnformatted($"{Strings.SelectedItem} {chosen.Value.Name}");
            ImGui.TextUnformatted($"{Strings.UnitPrice} {chosen.Value.UnitPrice:N0}   {Strings.Owned} {owned:N0}");
        }
        else
        {
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.3f, 1f), Strings.NoItemSelected);
        }

        ImGui.TextUnformatted($"{Strings.Gil} {gil:N0}");
    }

    private void DrawShopList(System.Collections.Generic.List<ShopItemInfo> rows)
    {
        ImGui.TextUnformatted(Strings.ShopList);

        float listHeight = ListMinHeight * ImGui.GetIO().FontGlobalScale;

        if (rows.Count == 0)
        {
            ImGui.BeginChild("##RepeatBuyShopList", new Vector2(0, listHeight), true);
            ImGui.TextDisabled(Strings.ShopListEmpty);
            ImGui.EndChild();
            return;
        }

        const ImGuiTableFlags tableFlags =
            ImGuiTableFlags.SizingFixedFit |
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.BordersInnerH;

        if (!ImGui.BeginTable("##RepeatBuyShopTable", 3, tableFlags, new Vector2(0, listHeight)))
            return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn(Strings.ColumnName, ImGuiTableColumnFlags.WidthStretch, 1f);
        ImGui.TableSetupColumn(Strings.ColumnPrice, ImGuiTableColumnFlags.WidthFixed, 90f);
        ImGui.TableSetupColumn(Strings.ColumnOwned, ImGuiTableColumnFlags.WidthFixed, 60f);
        ImGui.TableHeadersRow();

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);

            bool selected = chosen.HasValue
                            && chosen.Value.ItemId == row.ItemId
                            && chosen.Value.IsHQ == row.IsHQ
                            && chosen.Value.UnitPrice == row.UnitPrice
                            && chosen.Value.RealIndex == row.RealIndex;

            if (ImGui.Selectable($"{row.Name}##rb_row_{i}", selected, ImGuiSelectableFlags.SpanAllColumns))
            {
                chosen = row;
            }

            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted($"{row.UnitPrice:N0}");

            ImGui.TableSetColumnIndex(2);
            int held = CountOwned(row.ItemId, row.IsHQ);
            ImGui.TextUnformatted($"{held:N0}");
        }

        ImGui.EndTable();
    }
}
