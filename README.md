# RepeatBuy

<p align="center"><img src="images/icon.png" alt="RepeatBuy icon" width="160"></p>

Dalamud plugin for FFXIV that lets you mass-buy items from standard NPC shops.
A small overlay docks beside the shop window with a mirror of the shop's item list,
four stop conditions, and a confirmation popup that shows the estimated total cost
before any gil leaves your wallet.

## Why

FFXIV's NPC shop UI caps a single transaction at 99 items, and there is no "select
this item" affordance — clicking a row immediately opens the buy dialog. RepeatBuy
sidesteps both limits: pick the item from the plugin's own list, choose a stop
condition (including counts well over 99), and the plugin drives the purchases for
you while watching for the conditions that should make it stop.

## Features

- **Anchored side panel.** Appears automatically beside the standard NPC `Shop`
  addon. Closes itself when the shop closes. Position can be detached in settings.
- **Mirrored shop list.** Reads the active vendor's items and prices directly,
  so you don't have to interact with the game's shop list to "select" anything.
  Sorts and updates as the shop's own list does.
- **Four stop conditions:**
  - *Fixed count* — buy an exact quantity, including values larger than 99
    (RepeatBuy issues batches of 99 internally).
  - *Until owned* — buy until your inventory holds N of the item.
  - *Gil floor* — buy until your gil drops to a chosen threshold.
  - *Inventory full* — buy until no more space (existing stack capacity counted).
- **Confirmation popup with estimated total.** Before the loop starts, a modal
  shows the unit price, estimated quantity, total cost, and current gil. If the
  estimate exceeds your gil, the warning line turns red.
- **Safe by default.** Errors never crash the game: out of gil, inventory full,
  shop closed mid-loop, transaction stuck — each terminates the loop cleanly with
  a message in chat. Throttled at a configurable 100–1500 ms per call.
- **Six-language UI.** English, 日本語, Deutsch, Français, 简体中文, 한국어.
  Defaults to the FFXIV client language (EN/JA/DE/FR auto-detected); Chinese
  and Korean must be picked manually in settings since the global FFXIV
  client never reports those languages.

## Slash commands

- `/repeatbuy` — open the panel (the NPC shop must already be open).
- `/repeatbuy config` — open the settings window.

## Settings

- *Anchor window next to the shop* — keep the panel docked to the right edge.
- *Auto-open with shop* — open the panel automatically when a shop opens.
- *Language* — `Auto` (follow client) / English / 日本語 / Deutsch / Français /
  简体中文 / 한국어.
- *Delay between purchases (ms)* — slider, 100–1500 ms.

## Scope

- Standard NPC `Shop` (gil-based) only. SpecialShop / InclusionShop / currency
  exchange windows are intentionally out of scope for this version.

## Install

In-game, open Dalamud settings (`/xlsettings`) → **Experimental** → **Custom Plugin Repositories**, then add:

```
https://raw.githubusercontent.com/yagi2/dalamud-plugins/main/repo.json
```

Save, then open the plugin installer (`/xlplugins`) and search for **RepeatBuy**.

> Note: that URL is yagi2's shared plugin index — every plugin published by yagi2
> appears under it, so adding it once is enough for any future plugins as well.

## Build

```
dotnet build -c Release
```

The Dalamud SDK uses your local Dalamud install at
`%AppData%\XIVLauncher\addon\Hooks\dev\` for reference assemblies; no separate
setup is required as long as you have Dalamud installed.

Output:

- `bin/Release/RepeatBuy/RepeatBuy.dll` — the plugin assembly.
- `bin/Release/RepeatBuy/latest.zip` — packaged archive ready for distribution.

## Install (dev)

Point Dalamud's *Dev Plugin Locations* at the `bin/Release/RepeatBuy` directory,
or drop `RepeatBuy.dll` and `RepeatBuy.json` into a Dalamud dev plugin folder.

## Release process (maintainer)

1. Bump the version in `RepeatBuy.csproj` (`<Version>`) and `RepeatBuy.json`
   (`Changelog`), then commit and push to `main`.
2. Create a GitHub Release tagged `vX.Y.Z`. **No need to attach a binary** — the
   `Release` GitHub Actions workflow builds the plugin on `windows-latest` and
   uploads `latest.zip` automatically when the release is published.
3. In the [`yagi2/dalamud-plugins`](https://github.com/yagi2/dalamud-plugins)
   repo, update this plugin's entry in `repo.json` (`AssemblyVersion`,
   `Changelog`) and push to `main`.

Once the workflow finishes (~1 min) and the index push lands, Dalamud picks up
the new version on its next plugin-list refresh.

## License

MIT.
