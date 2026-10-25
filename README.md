# Haldor Idols

Adds all **16 vanilla Battle and Protection idols** to Haldor's shop, in a separate **Idols** tab,
with boss-progression gating, configurable prices and an 80% upgrade chance.

- No custom items, no custom assets — only the idols that already exist in Valheim 1.0.
- Server-synced config: the host decides prices, progression and the upgrade chance.
- Works in singleplayer and multiplayer. **Everyone must have the mod installed.**

---

## What it does

| Feature | Details |
|---|---|
| Idols tab | Haldor's window gets a second tab. Vanilla goods stay under **Goods**, the 16 idols are under **Idols**. |
| Progression | The next biome's idols unlock after you kill that biome's boss, exactly like Haldor Expansion. |
| Prices | Every tier has its own price in the config file. Defaults: 50 → 1000 coins. |
| Upgrade chance | Vanilla idols succeed 65% of the time. This mod raises it to **80%** for all 16 idols. |
| Localization | Tab captions follow the game language (English and Russian included). |

## Default prices and progression

| Tier | Boss required | Price |
|---|---|---:|
| Wooden | — (available from the start) | 50 |
| Bronze | Eikthyr | 100 |
| Iron | The Elder | 175 |
| Silver | Bonemass | 275 |
| Black Metal | Moder | 400 |
| Black Marble | Yagluth | 550 |
| Flametal | The Queen | 750 |
| Bloodgold | Fader | 1000 |

Battle and Protection idols of the same tier cost the same.

## Upgrade chance

All 16 idols use the same roll. With the default setting:

- **80%** — the item gains one quality level.
- **20%** — the attempt fails and the item is destroyed, returning part of the materials (vanilla behaviour).

The roll happens on the client that performs the upgrade, which is why every player needs the mod.

## Configuration

The config file is created on first launch:

```
BepInEx/config/Gezorg.HaldorIdols.cfg
```

Open it with **Edit config** in the Gale / Thunderstore Mod Manager, or edit it with any text editor.
On a dedicated server the host's values are sent to all clients.

```ini
[General]
Enabled = true
SuccessChance = 0.8

[Prices]
Wooden = 50
Bronze = 100
Iron = 175
Silver = 275
BlackMetal = 400
BlackMarble = 550
Flametal = 750
Bloodgold = 1000

[Progression]
Wooden =
Bronze = defeated_eikthyr
Iron = defeated_gdking
Silver = defeated_bonemass
BlackMetal = defeated_dragon
BlackMarble = defeated_goblinking
Flametal = defeated_queen
Bloodgold = defeated_fader
```

- `SuccessChance` accepts 0.05–0.99.
- Leave a progression key empty to make that tier always available.
- Prices are per single idol; the shop always sells one at a time.

## Installation

### With Gale / Thunderstore Mod Manager (recommended)

1. Install **BepInExPack Valheim** and **Jotunn**.
2. Install **Haldor Idols**.
3. Launch the game.

### Manual

1. Install BepInEx 5 (5.4.23xx or newer) and Jotunn 2.30.x.
2. Copy `BepInEx/plugins/HaldorIdols/HaldorIdols.dll` into your `BepInEx/plugins` folder.
3. Launch the game.

## Compatibility

- Requires **BepInEx 5** and **Jotunn**.
- Not compatible with mods that rebuild Haldor's shop UI or item list at the same time
  (for example **Haldor Expansion** or **Better Trader**). Use one of them at a time.
- Safe to add to an existing world. Removing the mod only removes the shop rows; already bought
  idols stay in your inventory.

## Notes

- Idols bought from Haldor are normal vanilla items: they can be stored, traded and used at the
  Forge of Potential.
- The 80% chance applies to **all** idols, including ones found in chests.
- Multiplayer: install the same mod version on the server and on every client.

## Links

- Source code: https://github.com/Gezorg/Haldor-Idols
- Issues: https://github.com/Gezorg/Haldor-Idols/issues
