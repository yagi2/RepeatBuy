using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using RepeatBuy.Windows;
using RepeatBuy.Localization;

namespace RepeatBuy;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameInventory GameInventory { get; private set; } = null!;

    private const string CommandName = "/repeatbuy";

    public Configuration Configuration { get; }
    public readonly WindowSystem WindowSystem = new("RepeatBuy");

    private MainWindow MainWindow { get; }
    private ConfigWindow ConfigWindow { get; }
    private ShopWatcher ShopWatcher { get; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Strings.SetLanguage(Configuration.UseJapanese);

        ConfigWindow = new ConfigWindow(Configuration);
        MainWindow = new MainWindow(Configuration, ToggleConfigUi);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the RepeatBuy panel (or '/repeatbuy config' for settings)."
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        ShopWatcher = new ShopWatcher(
            framework: Framework,
            isShopOpen: MainWindow.IsShopUIOpen,
            setMainWindowOpen: open => MainWindow.IsOpen = open,
            isEnabled: () => Configuration.AutoOpenOnShop
        );
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();

        ShopWatcher.Dispose();
        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        var a = (args ?? string.Empty).Trim();

        if (a.Length == 0)
        {
            if (!MainWindow.IsShopUIOpen())
            {
                ChatGui.PrintError("[RepeatBuy] Open an NPC shop window first.");
                return;
            }
            MainWindow.IsOpen = true;
            return;
        }

        if (a.StartsWith("config", StringComparison.OrdinalIgnoreCase))
        {
            ToggleConfigUi();
            return;
        }

        ChatGui.Print("[RepeatBuy] /repeatbuy           → open panel (shop must be open)");
        ChatGui.Print("[RepeatBuy] /repeatbuy config    → open settings window");
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
