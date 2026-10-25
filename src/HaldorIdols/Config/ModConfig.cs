using BepInEx.Configuration;
using Jotunn.Extensions;

namespace HaldorIdols
{
    internal static class ModConfig
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> SuccessChance;

        internal static readonly ConfigEntry<int>[] Prices = new ConfigEntry<int>[8];
        internal static readonly ConfigEntry<string>[] Keys = new ConfigEntry<string>[8];

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
            Enabled = config.BindConfig(
                "General",
                "Enabled",
                true,
                "Master switch for the mod. When disabled, no idols are added to Haldor.",
                true);

            SuccessChance = config.BindConfig(
                "General",
                "SuccessChance",
                0.8f,
                "Chance that an idol upgrade succeeds. Vanilla is 0.65. On failure the item is destroyed. " +
                "The roll happens on the client that performs the upgrade.",
                true,
                acceptableValues: new AcceptableValueRange<float>(0.05f, 0.99f));

            for (int i = 0; i < IdolCatalog.TierNames.Length; i++)
            {
                string tier = IdolCatalog.TierNames[i];

                Prices[i] = config.BindConfig(
                    "Prices",
                    tier,
                    DefaultPrices[i],
                    $"Price in coins for one {tier} idol (both Battle and Protection).",
                    true);

                Keys[i] = config.BindConfig(
                    "Progression",
                    tier,
                    DefaultKeys[i],
                    $"World key required before {tier} idols appear at Haldor. Empty means always available.",
                    true);
            }
        }
    }
}
