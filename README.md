# Haldor Idols

Haldor sells all **16 vanilla Battle and Protection idols** in a separate **Idols** tab, buys
trophies back in a **Sell** tab, and lets you tune the idol upgrade chance. Progression, prices
and trophy prices are configurable, and config edits apply without restarting the game.

The mod does three things:

1. **Buying idols** — the 16 vanilla idols, gated by boss progression and priced per tier.
2. **Selling trophies** — Haldor buys every vanilla creature trophy at configurable prices.
3. **Configurable upgrade chance** — the idol refinement roll, 65% by default, adjustable from
   5% to 99%.

- No custom items, no custom assets — only the idols and trophies that already exist in Valheim 1.0.
- Server-synced config: the host decides prices, progression, trophy prices and the upgrade chance.
- Works in singleplayer and multiplayer. **Everyone must have the mod installed.**

---

## What it does

| Feature | Details |
|---|---|
| Idols tab | Haldor's window gets three tabs: vanilla goods under **Goods**, the 16 idols under **Idols**, trophy selling under **Sell**. |
| Progression | The next biome's idols unlock after you kill that biome's boss, exactly like Haldor Expansion. |
| Prices | Every tier has its own price in the config file. Defaults: 50 → 1000 coins. |
| Upgrade chance | All 16 idols use one roll. The default is vanilla's **65%**; set anything from 5% to 99% in the config. |
| Trophy selling | Haldor buys 72 vanilla creature trophies at configurable prices. The whole feature can be switched off. |
| Live config | Saving the `.cfg` while the game is running re-applies prices, progression, the upgrade chance and trophy prices. |
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

All 16 idols use the same roll, and the chance is a single config value. With the default
(vanilla) setting:

- **65%** — the item gains one quality level.
- **35%** — the attempt fails and the item is destroyed, returning part of the materials
  (vanilla behaviour).

Raise `SuccessChance` if you want a friendlier refinement, lower it for a harsher economy.
The roll happens on the client that performs the upgrade, which is why every player needs the mod.

## Trophy selling

Haldor buys every vanilla creature trophy in the **Sell** tab. Select a trophy in the list and
press the vanilla **Sell** button; the whole stack is sold at once, exactly like vanilla
valuables, and the row already shows the total payout. Prices follow the biome progression, so
boss trophies are the main source of coins:

| Biome | Examples | Typical payout |
|---|---|---:|
| Meadows | Boar, Deer, Neck, Eikthyr | 10–250 |
| Black Forest | Greydwarf, Troll, The Elder | 15–300 |
| Swamp | Draugr, Abomination, Bonemass | 15–350 |
| Mountain | Wolf, Stone Golem, Moder | 30–400 |
| Ocean | Serpent, Bonemaw | 90 |
| Plains | Fuling, Lox, Yagluth | 25–450 |
| Mistlands | Seeker, Gjall, The Queen | 15–500 |
| Ashlands | Charred, Morgen, Fader | 40–600 |
| Deep North | Bear, Jotun, Barka | 40–120 |

A full set of one idol type costs 3300 coins (6600 for both Battle and Protection), so selling a
few boss trophies funds the next tier without making idols trivial. Every price is configurable,
and `0` means Haldor does not buy that trophy.

Trophy selling adds no custom items or assets: the mod only writes the sell value into the
existing trophy prefabs and builds the Sell tab from your inventory. Turning
`[Trophies] Enabled` off hides the tab and restores the vanilla value (trophies are not sellable).

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
SuccessChance = 0.65

[Trophies]
Enabled = true

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

[TrophyPrices]
TrophyBoar = 12
TrophyDeer = 15
TrophyNeck = 10
TrophyEikthyr = 250
; ... one line for each of the 72 trophies
```

- `SuccessChance` accepts 0.05–0.99; the default is 0.65, the vanilla value.
- Leave a progression key empty to make that tier always available.
- Idol prices are per single idol; the shop always sells one at a time.
- Trophy prices are 0–1,000,000 coins per trophy; `0` disables buying that trophy.

### Editing the config while the game is running

BepInEx itself does not watch the config file, so the mod runs its own watcher. Save the file
(singleplayer or host) and about half a second later the new values are applied:

- prices, progression keys, `Enabled` and `SuccessChance` are re-read from disk;
- the open Haldor window is rebuilt in place — no need to close and reopen the shop;
- `Config reloaded: ...` appears in `LogOutput.log` when the effective values changed.

A client connected to a server cannot hot-reload: the host's synced values always win and would
overwrite the edit on the next synchronization.

## Multiplayer and config authority

**The host's config always wins.** Every setting is bound as server-synced
(Jotunn `BindConfig(..., synced: true)`), so:

- when a client connects, the server sends its values and they are applied in memory
  before the world loads;
- the client's own `.cfg` file is **not** overwritten — it keeps the local values and is
  used again in singleplayer;
- editing the config on the host while the server is running pushes the changed values to
  connected clients right away (Jotunn re-sends on config reload);
- if the server does not run the mod, each client falls back to its own file, so install
  the same version on the server and on every client.

The effective values are written to the log after a sync:

```
[Haldor Idols] Config synchronized from server: Enabled=True, TrophySelling=True, SuccessChance=65%, Wooden=50, Bloodgold=1000
```

## Trust model

Prices and progression are enforced by the game client, exactly like vanilla Haldor prices:
Valheim's trade window adds the item to the local inventory and removes the coins on the
client. A modified client can therefore buy for less, unlock tiers early or sell trophies for
more. The mod adds no network endpoints of its own and does not widen that surface; it also
clamps every value it receives (idol prices 1–1,000,000, trophy prices 0–1,000,000, success
chance 5–99%) so a malformed config cannot break the game.


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
- Trophy selling writes the sell value into the vanilla trophy prefabs. Another mod that also
  changes trophy values may override them; turn `[Trophies] Enabled` off if you want the other
  mod to own those values.
- Safe to add to an existing world. Removing the mod only removes the shop rows; already bought
  idols stay in your inventory.

## Notes

- Idols bought from Haldor are normal vanilla items: they can be stored, traded and used at the
  Forge of Potential.
- The configured chance applies to **all** idols, including ones found in chests.
- Because trophies get a sell value, their tooltip shows that value while trophy selling is on.
  Turning the feature off restores the vanilla value (0).
- Multiplayer: install the same mod version on the server and on every client.

## Links

- Source code: https://github.com/Gezorg/Haldor-Idols
- Issues: https://github.com/Gezorg/Haldor-Idols/issues
