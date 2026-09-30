# Changelog

All notable changes to **GK2 Known Formula Helper** are documented here.

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
