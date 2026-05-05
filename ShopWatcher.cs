using System;
using Dalamud.Plugin.Services;

namespace RepeatBuy;

public sealed class ShopWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly Func<bool> isEnabled;
    private readonly Func<bool> isShopOpen;
    private readonly Action<bool> setMainWindowOpen;

    private bool lastVisible;
    private DateTime nextPoll = DateTime.MinValue;
    private readonly TimeSpan pollInterval = TimeSpan.FromMilliseconds(150);

    public ShopWatcher(
        IFramework framework,
        Func<bool> isShopOpen,
        Action<bool> setMainWindowOpen,
        Func<bool> isEnabled)
    {
        this.framework = framework;
        this.isShopOpen = isShopOpen;
        this.setMainWindowOpen = setMainWindowOpen;
        this.isEnabled = isEnabled;

        framework.Update += OnUpdate;
    }

    private void OnUpdate(IFramework _)
    {
        var now = DateTime.UtcNow;
        if (now < nextPoll) return;
        nextPoll = now + pollInterval;

        if (!isEnabled()) return;

        bool visible = isShopOpen();
        if (visible != lastVisible)
        {
            setMainWindowOpen(visible);
            lastVisible = visible;
        }
    }

    public void Dispose() => framework.Update -= OnUpdate;
}
