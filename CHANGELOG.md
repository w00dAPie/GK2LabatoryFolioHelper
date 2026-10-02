# Changelog

All notable changes to **GK2 Known Formula Helper** are documented here.

## [0.3.0] - 2026-10-02

- Show up to ten craftable laboratory mix variants when available.
- Position variant arrows beside the ingredient slots for Laboratory I and II.
- Improve controller navigation through the laboratory controls.
- Add optional global R3 removal of the most recently added helper pin.
- Add optional Game/Sharp typography modes for readable helper text.
- Keep variant arrows and symbolic controls on their vanilla game font.

## [0.2.8] - 2026-10-01

- Move the mix controls into the visible laboratory ingredient area.
- Place the mix arrows beside the laboratory ingredient slots for both laboratory sizes.
- Put the Ready/Missing status directly above the ingredient row.
- Link controller focus through result, variant arrows, all ingredient slots, Craft Max and back to result.
- Bind global R3 input to remove the most recently added formula pin.
- Apply the same controller focus transitions used by the vanilla Craft Max button.

## [0.2.7] - 2026-10-01

- Rename the alternative pin recipe label to `Alt. recipe`.

## [0.2.6] - 2026-10-01

- Place compact mix navigation controls below the alchemy frame, using the game's queue-arrow style when available.
- Keep the controls clickable and register them with the laboratory's gamepad navigation.
- Keep D-pad left/right switching available while the laboratory window is open.

## [0.2.5] - 2026-10-01

- Replaced the separate overlay arrows with compact controls inside the laboratory UI.
- Kept the controls clickable without introducing a full-screen overlay.

## [0.2.4] - 2026-10-01

- Fixed variant controls being hidden by the laboratory canvas despite successful mix switching.
- Rendered the `<` / `>` controls on a dedicated screen overlay canvas with a high sorting order.
- Added a log entry confirming that the variant controls were created.

## [0.2.3] - 2026-10-01

- Fixed variant controls appearing outside or clipped by the laboratory window.
- Re-anchored the controls to the root UI canvas and kept them clickable beside the result slot.
- D-pad left/right now switches active variants directly.
- Fixed `publish.ps1 -InstallLocal` to install into the documented `BepInEx/plugins/GK2KnownFormulaHelper` folder.

## [0.2.2] - 2026-10-01

- Added up to ten ranked known laboratory mixes for the selected formula.
- Added `<` and `>` controls next to the laboratory result slot.
- Added Left Arrow/Right Arrow and Comma/Period keyboard shortcuts to switch variants.
- Loading a variant redraws the laboratory ingredients and availability immediately.
- Moved the variant controls beside the result slot so they do not cover the game's `+` / `−` controls.
- Re-anchored the variant controls to the laboratory root canvas so both buttons remain visible and clickable.
- Added direct D-pad left/right switching while variants are active; the on-screen `+` / `−` controls remain available for craft count.

## [0.2.1] - 2026-09-30

- Added an opt-in HUD position preview with simulated external panels and a temporary sample card, so positions can be tested without installing other pin mods.
- Added optional HUD compatibility with GK2RecipePin and Kebo Recipe Pins 2.5.1.
- Detect these mods through their loaded assemblies, including late loading and replacement assemblies.
- Position alchemy cards below visible overlapping pin panels and react to panel layout and visibility changes.
- Ignore the new panels when hidden or positioned outside the alchemy HUD's horizontal screen area.

## [0.2.0] - 2026-09-29

- Added direct alchemy folio access from the laboratory result slot
- Added direct recipe selection from the folio
- Selected formulas now load the best matching mix into the laboratory
- Best laboratory mix selection now considers current crafting resources
- Connected storage is considered when choosing a craftable mix
- Missing recipes are automatically pinned for ingredient tracking
- Recipes with missing ingredients are still loaded into the laboratory
- Added powder-free recipe alternatives
- Improved ingredient availability handling
- Preserved normal folio pin/unpin behavior
- Removed the old custom pinned-recipe dialog
- Improved gamepad handling for the folio-based workflow

## [0.1.0] - 2026-09-29

### Added

- Initial public release.
- Pin known alchemy formulas from the folio.
- Up to four formula pins.
- HUD display for pinned formulas.
- Automatic best-mix selection.
- Ready / Missing recipe status.
- Player inventory owned counts.
- Buyable ingredient detection.
- Vendor name and vendor stock display.
- Laboratory II pinned recipe picker.
- Automatic loading of selected pinned recipes.
- Controller navigation for the pinned recipe picker.
- Controller A-button recipe selection.
- Inventory-driven HUD refreshes.
- Refresh after completed player inventory transfers.
- Optional Pin My Recipe compatibility.
- Optional GK2 Shopping List compatibility.

### Technical

- BepInEx 5 integration.
- HarmonyX runtime patching.
- Target framework: .NET Framework 4.7.2.
- Optional mod integrations use runtime detection and reflection.
- No save-data changes.
