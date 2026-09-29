# GK2 Laboratory Folio Helper

A BepInEx mod for **Graveyard Keeper 2** that improves the alchemy folio and laboratory workflow.

## Features

- Pin up to **4 known alchemy formulas** from the folio.
- Shows pinned formulas directly on the HUD.
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

## Compatibility

Optional mod integrations are detected dynamically at runtime and are not hard compile-time dependencies.

### Pin My Recipe

The Laboratory HUD reacts to recipe pin changes and can reposition itself to avoid overlap.

### GK2 Shopping List

The Laboratory HUD can account for the Shopping List HUD when positioning pinned alchemy recipes.

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
