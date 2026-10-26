# Changelog

## 1.0.0

- Added all 16 vanilla Battle and Protection idols to Haldor's shop.
- Added a separate **Idols** tab; vanilla goods remain under **Goods**.
- Added boss-progression gating for the eight idol tiers.
- Added configurable prices (default 50–1000 coins) and progression keys.
- Configurable idol upgrade chance: vanilla's 65% by default, 5–99% in the config.
- Added a **Sell** tab where Haldor buys all 72 vanilla creature trophies at balanced,
  configurable prices. Trophy selling can be disabled completely.
- Added English and Russian tab captions.
- Config is synced from the server in multiplayer.
- Config edits are now hot-reloaded from disk (singleplayer and host) instead of requiring
  a game restart; the open shop window refreshes in place.
- Hardened config handling: synced values are clamped before use, prices are bounded,
  and the mod never overwrites rows added by other shop mods.
- Fixed an exception when a tab had no items; the buy button is simply disabled.
