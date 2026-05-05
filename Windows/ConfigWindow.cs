using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using RepeatBuy.Localization;

namespace RepeatBuy.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public ConfigWindow(Configuration configuration)
        : base(Strings.ConfigWindowTitle + "###RepeatBuyConfig",
            ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse)
    {
        this.configuration = configuration;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(320f, 0f),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        bool anchor = configuration.AnchorWindow;
        if (ImGui.Checkbox(Strings.SettingsAnchor, ref anchor))
        {
            configuration.AnchorWindow = anchor;
            configuration.Save();
        }

        bool autoOpen = configuration.AutoOpenOnShop;
        if (ImGui.Checkbox(Strings.SettingsAutoOpen, ref autoOpen))
        {
            configuration.AutoOpenOnShop = autoOpen;
            configuration.Save();
        }

        bool jp = configuration.UseJapanese;
        if (ImGui.Checkbox(Strings.SettingsLanguage, ref jp))
        {
            configuration.UseJapanese = jp;
            Strings.SetLanguage(jp);
            configuration.Save();
        }

        ImGui.Separator();

        int delay = configuration.PurchaseDelayMs;
        ImGui.SetNextItemWidth(180f);
        if (ImGui.SliderInt(Strings.SettingsDelay, ref delay, 100, 1500))
        {
            configuration.PurchaseDelayMs = delay;
            configuration.Save();
        }

        ImGui.Spacing();
        if (ImGui.Button(Strings.SettingsSave, new Vector2(120, 0)))
            configuration.Save();
    }
}
