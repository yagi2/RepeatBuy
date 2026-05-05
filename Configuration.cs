using System;
using Dalamud.Configuration;

namespace RepeatBuy;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool AnchorWindow { get; set; } = true;
    public bool AutoOpenOnShop { get; set; } = true;
    public bool UseJapanese { get; set; } = false;

    public int PurchaseDelayMs { get; set; } = 350;

    public int LastFixedCount { get; set; } = 99;
    public int LastUntilOwned { get; set; } = 999;
    public long LastGilFloor { get; set; } = 0;

    public int SelectedMode { get; set; } = 0;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
