using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HaldorIdols
{
    /// <summary>
    /// Exchanges a Battle idol for a Protection idol of the same tier (and back).
    /// The two idols of a tier cost the same, so the exchange is a free 1:1 swap
    /// of items the player already owns; it never creates anything new.
    /// </summary>
    internal static class SwapService
    {
        private static readonly FieldInfo SelectedItemField =
            AccessTools.Field(typeof(StoreGui), "m_selectedItem");

        private static readonly List<ItemDrop.ItemData> TempItems = new List<ItemDrop.ItemData>();

        internal static bool ExchangeEnabled
        {
            get
            {
                return ModConfig.Enabled != null && ModConfig.Enabled.Value
                       && ModConfig.Exchange != null && ModConfig.Exchange.Value;
            }
        }

        internal static Trader.TradeItem GetSelected(StoreGui gui)
        {
            if (gui == null || SelectedItemField == null) return null;

            return SelectedItemField.GetValue(gui) as Trader.TradeItem;
        }

        internal static string SelectedIdolPrefab(StoreGui gui)
        {
            Trader.TradeItem selected = GetSelected(gui);
            if (selected == null || selected.m_prefab == null) return null;

            string prefab = selected.m_prefab.name;
            return IdolCatalog.IsIdolPrefab(prefab) ? prefab : null;
        }

        /// <summary>
        /// True when the selected row is an idol and the player carries the
        /// other idol of that tier. The selected row is always the result: the
        /// player gives the counterpart and receives what they selected.
        /// </summary>
        internal static bool CanSwap(StoreGui gui, out string sourcePrefab, out string targetPrefab)
        {
            sourcePrefab = null;
            targetPrefab = null;

            if (!ExchangeEnabled) return false;
            if (ShopTabsController.ActiveTab != ShopTabsController.TabIdols) return false;

            targetPrefab = SelectedIdolPrefab(gui);
            if (targetPrefab == null) return false;

            sourcePrefab = IdolCatalog.Counterpart(targetPrefab);
            if (sourcePrefab == null) return false;

            return FindInInventory(sourcePrefab) != null;
        }

        internal static void SwapSelected(StoreGui gui)
        {
            try
            {
                SwapCore(gui);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not exchange idols: " + e);
            }
        }

        private static void SwapCore(StoreGui gui)
        {
            if (!CanSwap(gui, out string sourcePrefab, out string targetPrefab)) return;

            Player player = Player.m_localPlayer;
            if (player == null) return;

            Inventory inventory = player.GetInventory();
            if (inventory == null) return;

            ItemDrop.ItemData sourceItem = FindInInventory(sourcePrefab);
            if (sourceItem == null) return;

            GameObject sourceDrop = ResolvePrefab(sourcePrefab);
            GameObject targetDrop = ResolvePrefab(targetPrefab);
            if (sourceDrop == null || targetDrop == null)
            {
                Plugin.Log.LogWarning("Cannot exchange idols: prefab not found (" +
                                      sourcePrefab + " / " + targetPrefab + ").");
                return;
            }

            // Take the source out first: a last idol frees its slot and every
            // idol frees its weight, so the counterpart fits even in a full or
            // overweight inventory. On failure the source goes straight back,
            // and since nothing was added the inventory has room for it.
            if (!inventory.RemoveItem(sourceItem, 1)) return;

            if (!inventory.AddItem(targetDrop, 1))
            {
                if (!inventory.AddItem(sourceDrop, 1))
                {
                    Plugin.Log.LogError("Exchange failed and the source idol could not be restored (" +
                                        sourcePrefab + ").");
                }

                ShowInventoryFull();
                return;
            }

            Plugin.Log.LogInfo("Exchanged " + sourcePrefab + " for " + targetPrefab + ".");
            ShowSwapped();
        }

        /// <summary>
        /// First matching item in the player's inventory. Idols are materials, so
        /// the type filter keeps the scan small; m_dropPrefab is the normal match,
        /// the shared name token is a fallback for items without a drop prefab.
        /// </summary>
        internal static ItemDrop.ItemData FindInInventory(string prefabName)
        {
            Player player = Player.m_localPlayer;
            if (player == null || string.IsNullOrEmpty(prefabName)) return null;

            Inventory inventory = player.GetInventory();
            if (inventory == null) return null;

            TempItems.Clear();
            inventory.GetAllItems(ItemDrop.ItemData.ItemType.Material, TempItems);

            for (int i = 0; i < TempItems.Count; i++)
            {
                ItemDrop.ItemData item = TempItems[i];
                if (item == null || item.m_shared == null) continue;
                if (item.m_dropPrefab != null && item.m_dropPrefab.name == prefabName) return item;
            }

            ItemDrop drop = ResolveDrop(prefabName);
            if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null) return null;

            string token = drop.m_itemData.m_shared.m_name;
            if (string.IsNullOrEmpty(token)) return null;

            for (int i = 0; i < TempItems.Count; i++)
            {
                ItemDrop.ItemData item = TempItems[i];
                if (item != null && item.m_shared != null && item.m_shared.m_name == token) return item;
            }

            return null;
        }

        private static ItemDrop ResolveDrop(string prefabName)
        {
            GameObject prefab = ResolvePrefab(prefabName);
            return prefab != null ? prefab.GetComponent<ItemDrop>() : null;
        }

        private static GameObject ResolvePrefab(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName) || ObjectDB.instance == null) return null;

            return ObjectDB.instance.GetItemPrefab(prefabName);
        }

        private static void ShowSwapped()
        {
            if (MessageHud.instance == null) return;

            MessageHud.instance.ShowMessage(
                MessageHud.MessageType.Center,
                TabsLocalization.Translate(TabsLocalization.SwapDoneToken),
                0,
                null,
                true,
                true);
        }

        private static void ShowInventoryFull()
        {
            if (MessageHud.instance == null) return;

            MessageHud.instance.ShowMessage(
                MessageHud.MessageType.Center,
                TabsLocalization.Translate(TabsLocalization.SwapFullToken),
                0,
                null,
                false,
                true);
        }
    }
}
