# CLAUDE.md

Notes for future agents working on RepeatBuy.

## Project at a glance

- **Type:** Dalamud plugin (FFXIV), API 15, .NET via `Dalamud.NET.Sdk/15.0.0`.
- **Goal:** mass-buy from standard NPC shops with configurable stop conditions
  and a cost-estimate popup before each run.
- **Scope locked to** `Shop` addon (gil-based vendors). SpecialShop, InclusionShop,
  currency-exchange shops are out of scope unless the user explicitly asks.

## Code map

```
RepeatBuy/
├── RepeatBuy.csproj            # Dalamud.NET.Sdk/15.0.0
├── RepeatBuy.json              # plugin manifest shown in Dalamud
├── Plugin.cs                   # IDalamudPlugin entry, services, /repeatbuy command
├── Configuration.cs            # IPluginConfiguration, persisted user prefs
├── ShopWatcher.cs              # 150 ms framework poll, opens/closes the panel
├── Localization/Strings.cs     # bilingual EN/JA strings, single toggle
└── Windows/
    ├── ConfigWindow.cs         # settings window
    ├── MainWindow.cs           # partial: lifecycle + Draw + shop list table
    ├── MainWindow.GameInfo.cs  # partial: addon/agent reads, anchor math
    └── MainWindow.Purchase.cs  # partial: stop modes, modal, framework loop
```

## Game-side wiring (the non-obvious bits)

These are the patterns that took inspection of `FFXIVClientStructs.dll` to land
on. Re-derive from `%AppData%\XIVLauncher\addon\Hooks\dev\FFXIVClientStructs.dll`
if Dalamud / FFXIVClientStructs ever change shape.

### Reaching ShopEventHandler

There is no static `Instance()` on `ShopEventHandler`. To get one:

```csharp
var agent = AgentShop.Instance();
var proxy = (ShopEventHandler.AgentProxy*)agent->EventReceiver;
var handler = proxy->Handler;
```

`AgentProxy` is a nested struct in `ShopEventHandler` whose `Handler` field is a
back-pointer to its parent `ShopEventHandler`. `AtkEventInterface` is at offset 0
of `AgentProxy`, so the cast from `agent->EventReceiver` (typed
`AtkEventInterface*`) to `AgentProxy*` is a valid reinterpret.

### Reading the shop's item list

```csharp
handler->Items           // Span<ShopItem>, capacity 60, count = ItemsCount
handler->VisibleItems    // Span<int>, indices into Items, count = VisibleItemsCount
```

`ShopItem.PriceBuy` is the unit price we display. `ShopItem.NumOwned` is what the
vendor reports as your held count (we still recompute via `InventoryManager` for
the live "Have" column to include saddlebags).

`VisibleItems[i]` is the *real* index into `Items[]` for the i-th visible row.
That real index — not the visible position — is what `BuyItemIndex` expects.

### Driving a purchase

```csharp
handler->BuyItemIndex = realIndex;
handler->ExecuteBuy(qty);   // qty up to 99 per call
```

`ExecuteBuy` is the method the game itself calls when you confirm a buy in the
native UI. It is synchronous-ish — fire it, then wait at least one tick. We poll
`handler->WaitingForTransactionToFinish` and skip a tick when it's set.

The earlier `Callback.Fire(addon, true, 0u, slot, qty)` approach also works on
`Shop`, but `ExecuteBuy` is cleaner and avoids the addon's quantity-spinner
side-effects.

### Detecting the shop window

```csharp
Plugin.GameGui.GetAddonByName("Shop", rootIndex)
```

returns `Dalamud.Game.NativeWrapper.AtkUnitBasePtr` in API 15 (not a raw
pointer like older Dalamud). To get the raw pointer when needed:
`(AtkUnitBase*)addon.Address`. Most properties (`X`, `Y`, `Width`, `Height`,
`IsVisible`, `Position`, `ScaledSize`) are exposed on the wrapper directly.

### `AtkValueType`, not `ValueType`

The enum that types `AtkValue.Type` is named
`FFXIVClientStructs.FFXIV.Component.GUI.AtkValueType` (members `Int`, `UInt`,
`Bool`, etc.). Plain `ValueType` resolves to `System.ValueType` and gives a
confusing CS0117 error.

## UX rules learned the hard way

- **Don't rely on a "currently selected" item from the game.** FFXIV's `Shop`
  addon has no select-without-buy action: clicking a row opens the buy dialog
  immediately, and `AgentShop.SelectedItemIndex` only reflects the last clicked
  row, not a persistent selection. The plugin must mirror the shop list itself
  and let the user pick from that.
- **Persist selection by `ItemId`, not by index.** `RealIndex` and `VisibleIndex`
  can shift as the shop re-sorts. `MainWindow.RefreshChosenFrom` re-resolves the
  chosen row by ItemId (+ HQ flag) every frame so the highlight stays put.
- **Anchor by the addon's screen position, not by ImGui state.** See
  `AnchorToShop` — read `unit->X / Y / Width / Height * Scale` and snap the
  ImGui window only when the position changes by more than 1 px squared.

## Localization

`Localization/Strings.cs` is a single static class with `T(en, ja)` helpers and
a `SetLanguage(bool)` toggle that flips a static field. There's no resource file
or culture switch — adding a string is a one-liner: declare the property,
return `T("english", "日本語")`. `SetLanguage` is called from `Plugin` ctor and
from the *Use Japanese UI* checkbox in `ConfigWindow`.

## Build

```
dotnet build -c Release
```

- SDK targets the local Dalamud install at
  `$(appdata)\XIVLauncher\addon\Hooks\dev\` automatically (handled by
  `Dalamud.NET.Sdk`).
- Output: `bin/Release/RepeatBuy.dll` plus `RepeatBuy.json`.
- The `.NET 11 preview` warning (`NETSDK1057`) is harmless — Dalamud SDK 15
  picks the right TFM for us.

## Reference plugins consulted

In `..\Mass-Withdraw` (same parent dir):
- `MainWindow.GameGui.cs` — addon position / anchor pattern.
- `MainWindow.Logic.cs` — framework-update loop with throttling and cancel.
- `RetainerWatcher.cs` — the 150 ms poll-and-toggle pattern that `ShopWatcher`
  copies almost verbatim.

If you need a feature that Mass-Withdraw also has (humanized delays, FPS
breathers, progress UI), copy the shape from there before inventing one.

## What not to do

- Do not call `ExecuteBuy` outside the framework thread. It must run on the
  main game thread; `IFramework.Update` already runs there.
- Do not stack purchases without checking `WaitingForTransactionToFinish` —
  it can fire YesNo confirms or stall the queue otherwise.
- Do not assume `BuyItemIndex` is a visible-list index — it is the real index
  into `Items[]`.
- Do not add SpecialShop / InclusionShop support without re-confirming scope
  with the user; the addon name and event handler are different and cheap
  generalization will break the gil-shop path.
