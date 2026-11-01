using System.Collections.Generic;
using BepInEx.Configuration;
using Jotunn.Extensions;

namespace HaldorIdols
{
    internal static class ModConfig
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> SuccessChance;
        internal static ConfigEntry<bool> Exchange;
        internal static ConfigEntry<bool> TrophySelling;

        internal static readonly ConfigEntry<int>[] Prices = new ConfigEntry<int>[8];
        internal static readonly ConfigEntry<string>[] Keys = new ConfigEntry<string>[8];

        internal static readonly Dictionary<string, ConfigEntry<int>> TrophyPrices =
            new Dictionary<string, ConfigEntry<int>>(TrophyCatalog.Entries.Length);

        private static readonly int[] DefaultPrices = { 50, 100, 175, 275, 400, 550, 750, 1000 };

        private static readonly string[] DefaultKeys =
        {
            "",
            "defeated_eikthyr",
            "defeated_gdking",
            "defeated_bonemass",
            "defeated_dragon",
            "defeated_goblinking",
            "defeated_queen",
            "defeated_fader"
        };

        internal static void Init(ConfigFile config)
        {
            bool migrateTrophyToggle = MigrateTrophyToggle(config, out bool trophySellingDefault);

            Enabled = config.BindConfig(
                "General",
                "Enabled",
                true,
                "Master switch for the mod. When disabled, no idols are added to Haldor.",
                true);

            SuccessChance = config.BindConfig(
                "General",
                "SuccessChance",
                0.65f,
                "Chance that an idol upgrade succeeds. Vanilla is 0.65. On failure the item is destroyed. " +
                "The roll happens on the client that performs the upgrade.",
                true,
                acceptableValues: new AcceptableValueRange<float>(0.05f, 0.99f));

            Exchange = config.BindConfig(
                "General",
                "Exchange",
                true,
                "Let the Idols tab exchange a Battle idol for a Protection idol of the same tier " +
                "(and back), 1:1 and free of charge.",
                true);

            for (int i = 0; i < IdolCatalog.TierNames.Length; i++)
            {
                string tier = IdolCatalog.TierNames[i];

                Prices[i] = config.BindConfig(
                    "Prices",
                    tier,
                    DefaultPrices[i],
                    $"Price in coins for one {tier} idol (both Battle and Protection).",
                    true,
                    acceptableValues: new AcceptableValueRange<int>(1, 1000000));

                Keys[i] = config.BindConfig(
                    "Progression",
                    tier,
                    DefaultKeys[i],
                    $"World key required before {tier} idols appear at Haldor. Empty means always available.",
                    true);
            }

            TrophySelling = config.BindConfig(
                "TrophyPrices",
                "Enabled",
                trophySellingDefault,
                "Let Haldor buy creature trophies from you in a separate Sell tab. " +
                "When disabled, the tab is hidden and trophies keep their vanilla value.",
                true);

            foreach (TrophyCatalog.Entry trophy in TrophyCatalog.Entries)
            {
                TrophyPrices[trophy.Prefab] = config.BindConfig(
                    "TrophyPrices",
                    trophy.Prefab,
                    trophy.DefaultPrice,
                    trophy.DisplayName + " - price in coins Haldor pays for one trophy. " +
                    "0 means Haldor does not buy this trophy.",
                    true,
                    acceptableValues: new AcceptableValueRange<int>(0, 1000000));
            }

            if (!migrateTrophyToggle) return;

            try
            {
                // Removes the now-empty [Trophies] section from the file.
                config.Save();
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning("Could not remove the obsolete [Trophies] section: " + e.Message);
            }
        }

        /// <summary>
        /// 1.0.1 moved the trophy switch from [Trophies] into [TrophyPrices].
        /// BepInEx keeps keys that no plugin binds in OrphanedEntries, where
        /// TryGetEntry cannot see them, so the old key is bound once to recover
        /// the value chosen by existing installs. It is then removed and the
        /// file is rewritten without the dead section. Returns true when a
        /// legacy value was found.
        /// </summary>
        private static bool MigrateTrophyToggle(ConfigFile config, out bool trophySelling)
        {
            trophySelling = true;

            // A string sentinel distinguishes "the key was absent" from a real
            // true/false value, including an empty value in a hand-edited file.
            const string absent = "\0";
            ConfigEntry<string> legacy = config.Bind(
                "Trophies",
                "Enabled",
                absent,
                "Obsolete. Replaced by [TrophyPrices] Enabled.");

            bool found = legacy.Value != absent;
            if (found && bool.TryParse(legacy.Value, out bool parsed))
                trophySelling = parsed;

            config.Remove(legacy.Definition);
            return found;
        }

        internal static int TrophyPrice(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName)) return 0;
            if (!TrophyPrices.TryGetValue(prefabName, out ConfigEntry<int> entry) || entry == null) return 0;

            return System.Math.Max(0, System.Math.Min(1000000, entry.Value));
        }

        internal static bool IsTrophyPriceEntry(ConfigEntryBase entry)
        {
            if (entry == null) return false;

            foreach (ConfigEntry<int> price in TrophyPrices.Values)
            {
                if (ReferenceEquals(price, entry)) return true;
            }

            return false;
        }
    }
}
