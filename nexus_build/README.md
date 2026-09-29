# GK2 Known Formula Helper

A BepInEx mod for **Graveyard Keeper 2** that improves the alchemy folio and laboratory workflow.

## Features

- Pin known alchemy formulas from the folio.
- Shows pinned formulas directly on the HUD.
- Compact recipe cards with:
  - **Ready** / **Missing** status
  - owned vs. required ingredient counts
  - buyable ingredients
  - vendor name and stock when available
- Automatically selects the best known ingredient mix for each formula.
- Displays powder-free alternative mixes when a suitable known recipe exists.
- Open the normal alchemy folio directly from the laboratory result slot.
- Select a known formula directly from the folio.
- Automatically load the best matching mix into the laboratory.
- Craftable mixes are preferred when the required resources are already available.
- Laboratory mix selection considers available crafting resources and accessible storage.
- Recipes with missing ingredients are automatically pinned.
- Incomplete recipes are still loaded into the laboratory so available ingredients remain visible at the station.
- Normal folio pin / unpin behavior is preserved when the folio is opened normally.
- Controller support for folio recipe selection.
- Optional compatibility with:
  - **Pin My Recipe**
  - **GK2 Shopping List**

These integrations are optional. The mod does not require either of them.

## How it works

The helper evaluates your known alchemy formulas and chooses the most useful ingredient combination based on the resources currently available to the crafting system.

Pinned formulas are displayed directly on the HUD so you can quickly see:

- whether a recipe is ready
- which ingredients are missing
- how many ingredients you currently own
- whether missing ingredients can be purchased
- vendor name and stock when available
- a powder-free alternative when one is known

### Laboratory folio workflow

When the laboratory does not currently contain a valid mix, the empty result slot can be used to open the normal alchemy folio.

Selecting a known formula from the folio will:

1. Find the best known ingredient mix for the formula.
2. Prefer a mix that can already be crafted with the currently available resources.
3. Load that mix directly into the laboratory.
4. Automatically pin the formula if ingredients are still missing.

The recipe is still loaded when incomplete, allowing the laboratory to show the available and missing ingredients normally.

When the alchemy folio is opened outside this laboratory selection mode, clicking formulas continues to pin or unpin them normally.

## Powder-free alternatives

Some alchemy formulas have multiple valid ingredient combinations.

If the preferred mix contains alchemic powder and a known alternative without powder exists, the HUD can display the powder-free combination underneath the main recipe.

This makes it easier to preserve powders when another valid combination is available.

## Requirements

- Graveyard Keeper 2
- BepInEx 5
- .NET Framework 4.7.2 target

Tested with:

- Graveyard Keeper 2 **1.007.1**
- BepInEx **5.4.23.5**

## Installation

1. Install BepInEx 5.
2. Create:

```text
BepInEx/plugins/GK2KnownFormulaHelper
```

3. Copy `GK2KnownFormulaHelper.dll` into that folder.
4. Start or restart the game.

## Building

Clone the repository:

```powershell
git clone https://github.com/w00dAPie/GK2LabatoryFolioHelper.git
cd GK2LabatoryFolioHelper
```

Create a local `Directory.Build.props` based on [Directory.Build.props.example](Directory.Build.props.example).

Then:

```powershell
dotnet tool restore
dotnet tool run csharpier format .
dotnet build -c Release
```

The DLL is created at:

```text
bin/Release/net472/GK2KnownFormulaHelper.dll
```

### Build and install locally

The `publish.ps1` workflow resolves `GameDir` from MSBuild, formats and builds the project, checks the version, and verifies the installed DLL with SHA256.

Existing DLLs are backed up under:

```text
artifacts/install-backups/
```

Build and install locally:

```powershell
.\publish.ps1 -InstallLocal -SkipPackages
```

Use `-SkipPackages` without `-InstallLocal` to build only.

These commands do not upload anything.

Restart the game after installing to load the new DLL.

If the running game keeps the old DLL locked, installation may leave a `.dll.previous-*` file next to it. It is not loaded as a plugin and can be removed after closing the game.

### Release packages and upload

With a valid `manifest.json` and `icon.png`, create Thunderstore and Nexus packages under:

```text
dist/thunderstore/
dist/nexus/
```

Run:

```powershell
.\publish.ps1 -NoPublish
```

An upload is only performed with `-Publish`.

Thunderstore publishing additionally requires:

- `thunderstore.toml`
- the `tcli` tool
- a local `.thunderstore-token` file

The token file should remain ignored by Git.

The Thunderstore configuration must package:

```text
bin/Release/net472/GK2KnownFormulaHelper.dll
```

The script synchronizes the package version with the project version. The following versions should match before publishing:

- project version
- `Plugin.PluginVersion`
- `manifest.json`
- `thunderstore.toml`

Nexus uploads remain manual.

## Compatibility

Optional mod integrations are detected dynamically at runtime and are not hard compile-time dependencies.

### Pin My Recipe

The Known Formula Helper HUD reacts to compatible recipe pin elements and can reposition itself to avoid overlapping the **Pin My Recipe** HUD.

When Pin My Recipe is detected, the alchemy pin limit can be reduced to keep the combined HUD layout compact.

### GK2 Shopping List

The HUD detects the visible **GK2 Shopping List** panel and can position alchemy formula cards below compatible HUD elements when necessary.

## Controller support

The normal alchemy folio remains controller navigable when opened from the laboratory.

Formula selection uses the existing folio navigation instead of a separate custom recipe picker.

## Credits and inspiration

A big thank you to the authors of **Pin My Recipe** and **GK2 Shopping List**.

Both mods provided great ideas for presenting pinned recipes, ingredient information, and compact HUD elements in a clear and useful way.

GK2 Known Formula Helper is an independent implementation, but its HUD design and usability were strongly inspired by ideas found in these mods.

Special thanks for the inspiration around:

- compact pinned recipe layouts
- readable ingredient presentation
- HUD positioning and usability
- keeping useful information visible without opening additional windows

Compatibility with **Pin My Recipe** and **GK2 Shopping List** is optional and handled dynamically at runtime.

## Development notes

- Narrow Harmony patches
- Preserve vanilla crafting behaviour
- No save-data changes
- No hard dependency on optional HUD mods
- No per-frame polling for normal pin updates

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## License

This project is licensed under the [MIT License](LICENSE).
