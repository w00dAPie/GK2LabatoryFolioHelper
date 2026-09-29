# GK2 Laboratory Folio Helper

A BepInEx mod for **Graveyard Keeper 2** that improves the alchemy folio and laboratory workflow.

## Features

- Pin up to **4 known alchemy formulas** from the folio.
- Shows pinned formulas directly on the HUD.
- Keeps the same HUD card size with or without other recipe pins.
- Automatically selects the best known ingredient mix for each formula.
- Displays:
  - recipe status: **Ready** / **Missing**
  - owned vs. required ingredient counts
  - buyable ingredients
  - vendor name and stock when available
- Ingredient ownership shown by the HUD is based on the **player inventory**.
- Laboratory crafting keeps the game's normal accessible craft inventories and storage.
- Laboratory II recipe helper:
  - open the pinned recipe picker from the result slot
  - choose a pinned recipe
  - automatically load the best available known mix
- Controller navigation for the pinned recipe picker.
- Optional compatibility with:
  - **Pin My Recipe**
  - **GK2 Shopping List**

These integrations are optional. The mod does not require either of them.

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
BepInEx/plugins/GK2LaboratoryFolioHelper
```

3. Copy `GK2LaboratoryFolioHelper.dll` into that folder.
4. Start the game.

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
bin/Release/net472/GK2LaboratoryFolioHelper.dll
```

### Build and install locally

The `publish.ps1` workflow is adapted from GK2 Rusty Tool Disposal. It resolves
`GameDir` from MSBuild, formats and builds the project, checks the version, and
verifies the installed DLL with SHA256. Existing DLLs are backed up under
`artifacts/install-backups/`.

```powershell
.\publish.ps1 -InstallLocal -SkipPackages
```

Use `-SkipPackages` without `-InstallLocal` to build only. These commands do not upload anything.
Restart the game after installing to load the new DLL.
If the running game keeps the old DLL locked, installation may leave a
`.dll.previous-*` file next to it. It is not loaded as a plugin and can be removed
after closing the game.

### Release packages and upload

With a valid `manifest.json` and `icon.png`, create Thunderstore and Nexus ZIPs
under `dist/thunderstore/` and `dist/nexus/`:

```powershell
.\publish.ps1 -NoPublish
```

An upload is only performed with `-Publish`. It additionally requires this mod's
`thunderstore.toml`, the `tcli` tool and a local `.thunderstore-token` file (ignored
by Git). The TOML must package `bin/Release/net472/GK2LaboratoryFolioHelper.dll`.
The script synchronizes its `versionNumber` with the project version; the manifest
and `Plugin.PluginVersion` must also match. Nexus uploads remain manual.

## Compatibility

Optional mod integrations are detected dynamically at runtime and are not hard compile-time dependencies.

## Credits and inspiration

A big thank you to the authors of **Pin My Recipe** and **GK2 Shopping List**.

Both mods provided great ideas for presenting pinned recipes, ingredient information, and compact HUD elements in a clear and useful way.

The Laboratory Folio Helper is an independent implementation, but its HUD design and usability were strongly inspired by ideas found in these mods.

Special thanks for the inspiration around:

- compact pinned recipe layouts
- readable ingredient presentation
- HUD positioning and usability
- keeping useful information visible without opening additional windows

Compatibility with **Pin My Recipe** and **GK2 Shopping List** is optional and handled dynamically at runtime.

### Pin My Recipe

The Laboratory HUD reacts to recipe pin changes and repositions itself to avoid overlapping the Pin My Recipe HUD.

When Pin My Recipe is detected, Laboratory Folio Helper can use a reduced alchemy pin limit to keep the combined HUD layout compact.

### GK2 Shopping List

The Laboratory HUD detects the visible Shopping List panel and positions its alchemy pins below compatible HUD elements when necessary.

## Development notes

- Narrow Harmony patches
- Preserve vanilla crafting behaviour
- No save-data changes
- No hard dependency on optional HUD mods
- No per-frame polling for normal pin updates

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## License

No license has been selected yet.
