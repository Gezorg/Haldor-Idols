using System;
using System.Collections.Generic;
using UnityEngine;

namespace HaldorIdols
{
    /// <summary>
    /// Adds the vanilla idols to Haldor's trade list and keeps prices and
    /// progression keys in sync with the current configuration.
    /// </summary>
    internal static class HaldorShop
    {
        private static readonly HashSet<Trader.TradeItem> AddedItems = new HashSet<Trader.TradeItem>();

        internal static bool IsHaldor(Trader trader)
        {
            return trader != null
                   && trader.gameObject != null
                   && trader.gameObject.name.StartsWith("Haldor", StringComparison.Ordinal);
        }

        internal static void Refresh(Trader trader)
        {
            if (trader == null) return;

            if (!ModConfig.Enabled.Value)
            {
                RemoveAddedItems(trader);
                return;
            }

            if (trader.m_items == null)
                trader.m_items = new List<Trader.TradeItem>();

            for (int tier = 0; tier < IdolCatalog.TierNames.Length; tier++)
            {
                AddOrUpdate(trader, IdolCatalog.WeaponPrefabs[tier], tier);
                AddOrUpdate(trader, IdolCatalog.ArmorPrefabs[tier], tier);
            }
        }

        private static void AddOrUpdate(Trader trader, string prefabName, int tier)
        {
            int price = Math.Max(1, ModConfig.Prices[tier].Value);
            string key = (ModConfig.Keys[tier].Value ?? string.Empty).Trim();

            Trader.TradeItem existing = FindItem(trader, prefabName);
            if (existing != null)
            {
                // Keep the row in sync with the current config without duplicating it.
                existing.m_price = price;
                existing.m_requiredGlobalKey = key;
                existing.m_stack = 1;
                return;
            }

            ItemDrop drop = ResolveDrop(prefabName);
            if (drop == null)
            {
                Plugin.Log.LogWarning("Idol prefab not found: " + prefabName);
                return;
            }

            var item = new Trader.TradeItem
            {
                m_prefab = drop,
                m_stack = 1,
                m_price = price,
                m_requiredGlobalKey = key,
                m_levelUpEffect = false,
                m_buyPlayerEffects = new EffectList(),
                m_icon = null,
                m_name = drop.m_itemData.m_shared.m_name,
                m_tooltip = string.Empty,
                m_buyKey = string.Empty,
                m_incrementKey = string.Empty,
                m_incrementAmount = 0
            };

            trader.m_items.Add(item);
            AddedItems.Add(item);
        }

        private static Trader.TradeItem FindItem(Trader trader, string prefabName)
        {
            if (trader.m_items == null) return null;

            foreach (Trader.TradeItem item in trader.m_items)
            {
                if (item != null && item.m_prefab != null && item.m_prefab.name == prefabName)
                    return item;
            }

            return null;
        }

        private static void RemoveAddedItems(Trader trader)
        {
            if (trader.m_items == null || AddedItems.Count == 0) return;

            for (int i = trader.m_items.Count - 1; i >= 0; i--)
            {
                Trader.TradeItem item = trader.m_items[i];
                if (item == null || !AddedItems.Contains(item)) continue;

                AddedItems.Remove(item);
                trader.m_items.RemoveAt(i);
            }
        }

        private static ItemDrop ResolveDrop(string prefabName)
        {
            ObjectDB db = ObjectDB.instance;
            if (db == null) return null;

            GameObject prefab = db.GetItemPrefab(prefabName);
            return prefab != null ? prefab.GetComponent<ItemDrop>() : null;
        }
    }
}
