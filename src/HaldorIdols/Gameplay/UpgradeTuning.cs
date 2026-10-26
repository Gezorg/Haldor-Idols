using HarmonyLib;
using UnityEngine;

namespace HaldorIdols
{
    /// <summary>
    /// Vanilla idols all use 65% success and destroy the item on any failure.
    /// The chance is stored on the idol's shared item data, and the client that
    /// performs the upgrade reads it from there (InventoryGui.DoCrafting).
    /// </summary>
    internal static class UpgradeTuning
    {
        internal const float MinChance = 0.05f;
        internal const float MaxChance = 0.99f;
        private const float DefaultChance = 0.8f;

        private static float _lastChance = float.NaN;
        private static int _lastApplied = -1;

        internal static void Apply(ObjectDB db = null)
        {
            db = db != null ? db : ObjectDB.instance;
            if (db == null) return;

            // The value can arrive from a server through Jotunn's config sync,
            // which bypasses BepInEx' acceptable-value clamping. Never trust it.
            float chance = ModConfig.SuccessChance != null ? ModConfig.SuccessChance.Value : DefaultChance;
            if (float.IsNaN(chance) || float.IsInfinity(chance)) chance = DefaultChance;
            float clamped = Mathf.Clamp(chance, MinChance, MaxChance);
            if (!Mathf.Approximately(chance, clamped))
            {
                Plugin.Log.LogWarning("SuccessChance " + chance + " is outside " + MinChance + ".." + MaxChance +
                                      "; using " + clamped + ".");
            }
            chance = clamped;

            int applied = 0;

            for (int tier = 0; tier < IdolCatalog.TierNames.Length; tier++)
            {
                applied += ApplyTo(db, IdolCatalog.WeaponPrefabs[tier], chance);
                applied += ApplyTo(db, IdolCatalog.ArmorPrefabs[tier], chance);
            }

            if (applied > 0 && (applied != _lastApplied || !Mathf.Approximately(chance, _lastChance)))
            {
                _lastApplied = applied;
                _lastChance = chance;
                Plugin.Log.LogInfo("Idol upgrade chance set to " + chance.ToString("P0") +
                                   " on " + applied + "/16 idols.");
            }
        }

        private static int ApplyTo(ObjectDB db, string prefabName, float chance)
        {
            GameObject prefab = db.GetItemPrefab(prefabName);
            if (prefab == null) return 0;

            ItemDrop drop = prefab.GetComponent<ItemDrop>();
            if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null) return 0;

            // m_breakChance is 1.0 on every vanilla idol: any failed roll destroys
            // the item, so only the success chance has to be changed.
            drop.m_itemData.m_shared.m_upgradeChance = chance;
            return 1;
        }
    }

    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    internal static class ObjectDbAwakePatch
    {
        private static void Postfix(ObjectDB __instance)
        {
            UpgradeTuning.Apply(__instance);
            TrophyShop.ApplyPrices(__instance);
        }
    }

    /// <summary>
    /// The main menu swaps the object database for the one loaded from disk
    /// (FejdStartup.SetupObjectDB calls ObjectDB.CopyOtherDB), which replaces
    /// m_items with fresh prefabs and resets the idol chance. Re-apply it there.
    /// </summary>
    [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
    internal static class ObjectDbCopyPatch
    {
        private static void Postfix(ObjectDB __instance)
        {
            UpgradeTuning.Apply(__instance);
            TrophyShop.ApplyPrices(__instance);
        }
    }
}
