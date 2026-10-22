using System;
using Dalamud.Configuration;
using RepeatBuy.Localization;

namespace RepeatBuy;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 3;

    public bool AnchorWindow { get; set; } = true;
    public bool AutoOpenOnShop { get; set; } = true;

    /// <summary>
    /// 0=English, 1=Japanese, 2=German, 3=French, 4=Chinese, 5=Korean.
    /// -1 means "follow client language" (resolves to EN/JA/DE/FR only since
    /// the FFXIV global client never reports Chinese or Korean).
    /// </summary>
    public int Language { get; set; } = -1;

    public int PurchaseDelayMs { get; set; } = 350;

    public int LastFixedCount { get; set; } = 99;
    public int LastUntilOwned { get; set; } = 999;
    public long LastGilFloor { get; set; } = 0;

    public int SelectedMode { get; set; } = 0;

    /// <summary>
    /// When a shop confirmation appears after this plugin sends a buy, click Yes.
    /// Dialogs that were already open are left alone.
    /// </summary>
    public bool AutoAcceptShopConfirm { get; set; } = true;

    public Language ResolveLanguage(Dalamud.Game.ClientLanguage clientLanguage)
    {
        if (Language >= 0 && Language <= 5)
            return (Language)Language;

        return clientLanguage switch
        {
            Dalamud.Game.ClientLanguage.Japanese => Localization.Language.Japanese,
            Dalamud.Game.ClientLanguage.German => Localization.Language.German,
            Dalamud.Game.ClientLanguage.French => Localization.Language.French,
            _ => Localization.Language.English
        };
    }

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
