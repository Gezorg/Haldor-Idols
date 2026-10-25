using System.Collections.Generic;

namespace HaldorIdols
{
    /// <summary>
    /// The 16 vanilla Forge of Potential idols, one pair per tier.
    /// The prefab names are the ones shipped by the game in 1.0.x.
    /// </summary>
    internal static class IdolCatalog
    {
        internal static readonly string[] TierNames =
        {
            "Wooden",
            "Bronze",
            "Iron",
            "Silver",
            "BlackMetal",
            "BlackMarble",
            "Flametal",
            "Bloodgold"
        };

        internal static readonly string[] WeaponPrefabs =
        {
            "Upgrader0Weapon",
            "Upgrader1Weapon",
            "Upgrader2Weapon",
            "Upgrader3Weapon",
            "Upgrader4Weapon",
            "Upgrader5Weapon",
            "Upgrader6Weapon",
            "Upgrader7Weapon"
        };

        internal static readonly string[] ArmorPrefabs =
        {
            "Upgrader0Armor",
            "Upgrader1Armor",
            "Upgrader2Armor",
            "Upgrader3Armor",
            "Upgrader4Armor",
            "Upgrader5Armor",
            "Upgrader6Armor",
            "Upgrader7Armor"
        };

        private static readonly HashSet<string> AllPrefabs = BuildSet();

        private static HashSet<string> BuildSet()
        {
            var set = new HashSet<string>();
            foreach (string prefab in WeaponPrefabs) set.Add(prefab);
            foreach (string prefab in ArmorPrefabs) set.Add(prefab);
            return set;
        }

        internal static bool IsIdolPrefab(string prefabName)
        {
            return !string.IsNullOrEmpty(prefabName) && AllPrefabs.Contains(prefabName);
        }
    }
}
