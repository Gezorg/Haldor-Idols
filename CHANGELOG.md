# Changelog

## 1.0.0

- Added all 16 vanilla Battle and Protection idols to Haldor's shop.
- Added a separate **Idols** tab; vanilla goods remain under **Goods**.
- Added boss-progression gating for the eight idol tiers.
- Added configurable prices (default 50–1000 coins) and progression keys.
- Raised the idol upgrade success chance from 65% to 80% (configurable).
- Added English and Russian tab captions.
- Config is synced from the server in multiplayer.
- Hardened config handling: synced values are clamped before use, prices are bounded,
  and the mod never overwrites rows added by other shop mods.
- Fixed an exception when a tab had no items; the buy button is simply disabled.
- Config edits apply to an open shop window immediately.

