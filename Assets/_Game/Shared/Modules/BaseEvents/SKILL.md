---
name: event-excel-so-config
description: Read event Excel configuration tables and fill Unity ScriptableObject reward fields. Use when the task says to read an Excel sheet and map reward names to canonical IDs for OutGame Events configs.
---

# Event Excel To SO Config

Follow this workflow whenever asked to read an Excel file and fill an Event ScriptableObject.

## Canonical Reward IDs

Always map any similar reward text to one of these IDs:

- `gold`
- `booster_magnet`
- `booster_pole`
- `booster_broom`
- `booster_magic_wand`
- `unlimited_live`

## Reward Name Mapping Rules

Normalize source text before mapping:

1. Trim spaces.
2. Convert to lowercase.
3. Replace spaces and hyphens with underscore.
4. Ignore punctuation.

Apply mapping immediately when a similar name appears:

- `gold`: `gold`, `coin`, `coins`
- `booster_magnet`: `magnet`, `booster_magnet`
- `booster_pole`: `pole`, `booster_pole`
- `booster_broom`: `broom`, `booster_broom`
- `booster_magic_wand`: `magicwand`, `magic_wand`, `magic wand`, `booster_magic_wand`
- `unlimited_live`: `lives_unlimited`, `unlimited_lives`, `unlimited_live`, `live_unlimited`, `lives unlimited`

If a reward name does not match any rule, stop and ask the user which canonical ID to use.

## Reward Class Mapping

Fill each reward as `Reward` with:

- `name`: canonical reward ID from this skill.
- `value`: numeric amount from Excel.

Use these rules for `value`:

- For normal rewards (`gold`, `booster_magnet`, `booster_pole`, `booster_broom`, `booster_magic_wand`):
  `value = quantity in Excel`.
- For `unlimited_live`:
  `value = duration in seconds`.

Convert unlimited duration text to seconds:

- `N minute`, `N minutes`, `N min` -> `N * 60`
- `N h`, `N hour`, `N hours` -> `N * 3600`

Examples:

- `unlimited_live x15 minute` -> `Reward { name = "unlimited_live", value = 900 }`
- `unlimited_live x30 minute` -> `Reward { name = "unlimited_live", value = 1800 }`
- `unlimited_live x1h` -> `Reward { name = "unlimited_live", value = 3600 }`

## Execution Steps

1. Read the requested Excel file and locate the target sheet.
2. Parse reward columns/rows and normalize each reward name.
3. Convert each reward name to the canonical ID list above.
4. Build `Reward{name, value}` entries with the value rules above.
5. Fill the target ScriptableObject fields using those reward entries.
6. Report which rows were mapped and any unmapped values.
