using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
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
            frameworkHooked = true;
        }
    }

    public void Dispose()
    {
        if (frameworkHooked)
        {
            Plugin.Framework.Update -= OnFrameworkUpdate;
            frameworkHooked = false;
        }
    }

    public override void PreOpenCheck()
    {
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
        if (configuration.AnchorWindow)
            AnchorToShop();

        if (!IsShopUIOpen())
        {
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

        // re-resolve the chosen item by ItemId so that the displayed price and
        // owned count stay live as the underlying list updates
        foreach (var row in rows)
        {
            if (row.ItemId == current.ItemId && row.IsHQ == current.IsHQ)
            {
                chosen = row;
                return;
            }
        }
        // item disappeared from the list — drop selection
        chosen = null;
    }

    private void DrawHeader()
    {
        long gil = GetCurrentGil();

        if (chosen.HasValue)
        {
            int owned = CountOwned(chosen.Value.ItemId);
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
                            && chosen.Value.IsHQ == row.IsHQ;

            if (ImGui.Selectable($"{row.Name}##rb_row_{i}", selected, ImGuiSelectableFlags.SpanAllColumns))
            {
                chosen = row;
            }

            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted($"{row.UnitPrice:N0}");

            ImGui.TableSetColumnIndex(2);
            int held = CountOwned(row.ItemId);
            ImGui.TextUnformatted($"{held:N0}");
        }

        ImGui.EndTable();
    }
}
