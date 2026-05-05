using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using RepeatBuy.Localization;

namespace RepeatBuy.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    private static readonly (int Value, Func<string> Label)[] LanguageOptions =
    {
        (-1, () => "Auto / 自動 / Auto / Auto"),
        ((int)Language.English,  () => Strings.LanguageNameEnglish),
        ((int)Language.Japanese, () => Strings.LanguageNameJapanese),
        ((int)Language.German,   () => Strings.LanguageNameGerman),
        ((int)Language.French,   () => Strings.LanguageNameFrench),
        ((int)Language.Chinese,  () => Strings.LanguageNameChinese),
        ((int)Language.Korean,   () => Strings.LanguageNameKorean)
    };

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

        DrawLanguageCombo();

        ImGui.Separator();

        int delay = configuration.PurchaseDelayMs;
        ImGui.SetNextItemWidth(220f);
        if (ImGui.SliderInt(Strings.SettingsDelay, ref delay, 100, 1500))
        {
            configuration.PurchaseDelayMs = delay;
            configuration.Save();
        }

        ImGui.Spacing();
        if (ImGui.Button(Strings.SettingsSave, new Vector2(120, 0)))
            configuration.Save();
    }

    private void DrawLanguageCombo()
    {
        int currentValue = configuration.Language;
        int currentIndex = 0;
        for (int i = 0; i < LanguageOptions.Length; i++)
        {
            if (LanguageOptions[i].Value == currentValue)
            {
                currentIndex = i;
                break;
            }
        }

        string preview = LanguageOptions[currentIndex].Label();
        ImGui.SetNextItemWidth(220f);
        if (ImGui.BeginCombo(Strings.SettingsLanguage, preview))
        {
            for (int i = 0; i < LanguageOptions.Length; i++)
            {
                bool selected = i == currentIndex;
                if (ImGui.Selectable(LanguageOptions[i].Label(), selected))
                {
                    configuration.Language = LanguageOptions[i].Value;
                    Strings.SetLanguage(configuration.ResolveLanguage(Plugin.ClientState.ClientLanguage));
                    configuration.Save();
                }
                if (selected) ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
    }
}
