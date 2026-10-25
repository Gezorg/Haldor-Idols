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
        internal static void Apply()
        {
            ObjectDB db = ObjectDB.instance;
            if (db == null) return;

            float chance = ModConfig.SuccessChance != null ? ModConfig.SuccessChance.Value : 0.8f;
            int applied = 0;

            for (int tier = 0; tier < IdolCatalog.TierNames.Length; tier++)
            {
                applied += ApplyTo(db, IdolCatalog.WeaponPrefabs[tier], chance);
                applied += ApplyTo(db, IdolCatalog.ArmorPrefabs[tier], chance);
            }

            Plugin.Log.LogInfo("Idol upgrade chance set to " + chance.ToString("P0") +
                               " on " + applied + "/16 idols.");
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
        private static void Postfix()
        {
            UpgradeTuning.Apply();
        }
    }
}
