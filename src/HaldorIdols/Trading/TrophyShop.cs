using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HaldorIdols
{
    /// <summary>
    /// Trophy selling. Vanilla StoreGui already has a Sell button, but it only
    /// sells the first item that has a coin value and trophies are worth 0.
    /// This class gives the trophies a configurable value and builds the rows of
    /// the Sell tab from the player's inventory.
    /// </summary>
    internal static class TrophyShop
    {
        private static readonly FieldInfo ItemListField = AccessTools.Field(typeof(StoreGui), "m_itemList");

        private static readonly List<Trader.TradeItem> SellItems = new List<Trader.TradeItem>();
        private static readonly Dictionary<string, Trader.TradeItem> ItemsByPrefab =
            new Dictionary<string, Trader.TradeItem>(StringComparer.Ordinal);
        private static readonly Dictionary<string, int> OriginalValues =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> TokenToPrefab =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, int> Counts =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private static readonly List<ItemDrop.ItemData> TempItems = new List<ItemDrop.ItemData>();
        private static readonly List<string> MissingPrefabs = new List<string>(TrophyCatalog.Entries.Length);
        private static readonly HashSet<string> ReportedMissingPrefabs = new HashSet<string>(StringComparer.Ordinal);

        private static bool _dirty = true;
        private static int _signature;
        private static int _lastApplied = -1;
        private static bool _lastEnabled;

        internal static bool Enabled
        {
            get
            {
                return ModConfig.Enabled != null && ModConfig.Enabled.Value
                       && ModConfig.TrophySelling != null && ModConfig.TrophySelling.Value;
            }
        }

        internal static List<Trader.TradeItem> Items
        {
            get
            {
                RebuildIfDirty();
                return SellItems;
            }
        }

        /// <summary>
        /// Writes the configured prices into the shared item data of every
        /// trophy prefab. m_value is what StoreGui.SellItem pays out and what
        /// Inventory.GetValuableItems looks at, so the vanilla Sell button also
        /// pays the right amount. When selling is disabled the original value is
        /// restored instead of leaving free money behind.
        /// </summary>
        internal static void ApplyPrices(ObjectDB db = null)
        {
            try
            {
                ApplyPricesCore(db);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not apply trophy prices: " + e);
            }
        }

        private static void ApplyPricesCore(ObjectDB db)
        {
            db = db != null ? db : ObjectDB.instance;
            if (db == null) return;

            bool enabled = Enabled;
            int applied = 0;

            TokenToPrefab.Clear();
            MissingPrefabs.Clear();

            foreach (TrophyCatalog.Entry entry in TrophyCatalog.Entries)
            {
                GameObject prefab = db.GetItemPrefab(entry.Prefab);
                if (prefab == null)
                {
                    MissingPrefabs.Add(entry.Prefab);
                    continue;
                }

                ItemDrop drop = prefab.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null) continue;

                ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
                if (!OriginalValues.ContainsKey(entry.Prefab))
                    OriginalValues[entry.Prefab] = shared.m_value;

                shared.m_value = enabled ? ModConfig.TrophyPrice(entry.Prefab) : OriginalValues[entry.Prefab];

                if (!string.IsNullOrEmpty(shared.m_name) && !TokenToPrefab.ContainsKey(shared.m_name))
                    TokenToPrefab[shared.m_name] = entry.Prefab;

                applied++;
            }

            // ObjectDB exists before its item list is filled, so the first call
            // during startup finds nothing. The ObjectDB.Awake postfix runs this
            // again once the prefabs are there; until then stay quiet.
            if (applied == 0) return;

            for (int i = 0; i < MissingPrefabs.Count; i++)
            {
                if (ReportedMissingPrefabs.Add(MissingPrefabs[i]))
                    Plugin.Log.LogWarning("Trophy prefab not found: " + MissingPrefabs[i]);
            }

            ApplyInventoryValues();

            _dirty = true;

            if (applied == _lastApplied && enabled == _lastEnabled) return;

            _lastApplied = applied;
            _lastEnabled = enabled;

            if (enabled)
                Plugin.Log.LogInfo("Trophy selling enabled for " + applied + " trophies.");
            else
                Plugin.Log.LogInfo("Trophy selling disabled; trophy values restored.");
        }

        /// <summary>
        /// Trophy instances that already exist (inventory, containers) may hold
        /// their own copy of the shared data, so the prefab value alone is not
        /// enough: a price edit has to reach the items the player is carrying.
        /// The payout in StoreGui.SellItem reads exactly this field.
        /// </summary>
        private static void ApplyInventoryValues()
        {
            Player player = Player.m_localPlayer;
            if (player == null) return;

            Inventory inventory = player.GetInventory();
            if (inventory == null) return;

            bool enabled = Enabled;

            TempItems.Clear();
            inventory.GetAllItems(ItemDrop.ItemData.ItemType.Trophy, TempItems);

            for (int i = 0; i < TempItems.Count; i++)
            {
                ItemDrop.ItemData item = TempItems[i];
                if (item == null || item.m_shared == null) continue;

                string prefab = ResolvePrefab(item);
                if (prefab == null) continue;

                int value;

                if (enabled)
                {
                    value = ModConfig.TrophyPrice(prefab);
                }
                else if (!OriginalValues.TryGetValue(prefab, out value))
                {
                    continue;
                }

                if (item.m_shared.m_value != value)
                    item.m_shared.m_value = value;
            }
        }

        internal static void Invalidate()
        {
            _dirty = true;
        }

        /// <summary>
        /// Rebuilds the Sell tab rows when the trophy set or the prices changed.
        /// Rows are cached so StoreGui can compare m_selectedItem by reference
        /// between GetSelectedItemIndex, SelectItem and FillList.
        /// </summary>
        internal static void RebuildIfDirty()
        {
            try
            {
                RebuildCore();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not build the trophy list: " + e);
                Clear();
            }
        }

        private static void RebuildCore()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                Clear();
                return;
            }

            Inventory inventory = player.GetInventory();
            if (inventory == null)
            {
                Clear();
                return;
            }

            Counts.Clear();

            TempItems.Clear();
            inventory.GetAllItems(ItemDrop.ItemData.ItemType.Trophy, TempItems);

            for (int i = 0; i < TempItems.Count; i++)
            {
                ItemDrop.ItemData item = TempItems[i];
                if (item == null || item.m_shared == null) continue;

                string prefab = ResolvePrefab(item);
                if (prefab == null || ModConfig.TrophyPrice(prefab) <= 0) continue;

                Counts[prefab] = Counts.TryGetValue(prefab, out int count) ? count + 1 : 1;
            }

            int signature = ComputeSignature();
            if (!_dirty && signature == _signature) return;

            _signature = signature;
            _dirty = false;

            var ordered = new List<string>(Counts.Keys);
            ordered.Sort(ComparePrefabs);

            var previous = new Dictionary<string, Trader.TradeItem>(ItemsByPrefab, StringComparer.Ordinal);
            SellItems.Clear();
            ItemsByPrefab.Clear();

            foreach (string prefab in ordered)
            {
                // StoreGui.SellItem removes the whole inventory stack and pays
                // m_value * m_stack, so the row shows that total and the stack
                // size. What the player sees is exactly what Haldor pays.
                int count = Counts[prefab];
                int price = TotalPrice(ModConfig.TrophyPrice(prefab), count);

                if (!previous.TryGetValue(prefab, out Trader.TradeItem item) || item == null || item.m_prefab == null)
                {
                    item = CreateItem(prefab, price, count);
                    if (item == null) continue;
                }
                else
                {
                    item.m_price = price;
                    item.m_stack = count;
                }

                SellItems.Add(item);
                ItemsByPrefab[prefab] = item;
            }
        }

        /// <summary>
        /// Inventory ItemData behind the selected Sell row. Used as a drop-in
        /// replacement for StoreGui.GetSellableItem() so the native Sell button
        /// sells the trophy the player actually selected.
        /// </summary>
        internal static ItemDrop.ItemData FindInInventory(string prefabName)
        {
            try
            {
                return FindInInventoryCore(prefabName);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not find the selected trophy: " + e);
                return null;
            }
        }

        private static ItemDrop.ItemData FindInInventoryCore(string prefabName)
        {
            Player player = Player.m_localPlayer;
            if (player == null || string.IsNullOrEmpty(prefabName)) return null;

            Inventory inventory = player.GetInventory();
            if (inventory == null) return null;

            TempItems.Clear();
            inventory.GetAllItems(ItemDrop.ItemData.ItemType.Trophy, TempItems);

            for (int i = 0; i < TempItems.Count; i++)
            {
                ItemDrop.ItemData item = TempItems[i];
                if (item == null || item.m_shared == null) continue;
                if (ResolvePrefab(item) == prefabName) return item;
            }

            return null;
        }

        /// <summary>
        /// StoreGui greys out rows the player cannot afford. In the Sell tab the
        /// price is income, so the rows must stay readable regardless of coins.
        /// </summary>
        internal static void WhitenRows(StoreGui gui)
        {
            if (gui == null || ItemListField == null) return;

            var rows = ItemListField.GetValue(gui) as List<GameObject>;
            if (rows == null) return;

            foreach (GameObject row in rows)
            {
                if (row == null) continue;

                Transform icon = row.transform.Find("icon");
                if (icon != null)
                {
                    Image image = icon.GetComponent<Image>();
                    if (image != null) image.color = Color.white;
                }

                Transform name = row.transform.Find("name");
                if (name != null)
                {
                    TMP_Text text = name.GetComponent<TMP_Text>();
                    if (text != null) text.color = Color.white;
                }

                foreach (TMP_Text text in row.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text != null && text.gameObject.name == "price")
                    {
                        text.color = Color.white;
                        break;
                    }
                }
            }
        }

        private static void Clear()
        {
            if (SellItems.Count == 0 && ItemsByPrefab.Count == 0)
            {
                _dirty = true;
                return;
            }

            SellItems.Clear();
            ItemsByPrefab.Clear();
            Counts.Clear();
            _signature = 0;
            _dirty = true;
        }

        private static int ComputeSignature()
        {
            var builder = new StringBuilder(Counts.Count * 24);
            foreach (KeyValuePair<string, int> pair in Counts)
            {
                builder.Append(pair.Key).Append('=').Append(pair.Value)
                       .Append('@').Append(ModConfig.TrophyPrice(pair.Key)).Append(';');
            }

            return builder.ToString().GetHashCode();
        }

        private static int TotalPrice(int unitPrice, int stack)
        {
            long total = (long)unitPrice * stack;
            if (total > int.MaxValue) total = int.MaxValue;
            if (total < 0) total = 0;

            return (int)total;
        }

        private static int ComparePrefabs(string left, string right)
        {
            int leftPrice = ModConfig.TrophyPrice(left);
            int rightPrice = ModConfig.TrophyPrice(right);
            if (leftPrice != rightPrice) return rightPrice.CompareTo(leftPrice);

            return string.Compare(DisplayName(left), DisplayName(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string DisplayName(string prefabName)
        {
            return TrophyCatalog.TryGet(prefabName, out TrophyCatalog.Entry entry) ? entry.DisplayName : prefabName;
        }

        private static Trader.TradeItem CreateItem(string prefabName, int price, int stack)
        {
            ItemDrop drop = ResolveDrop(prefabName);
            if (drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null) return null;

            return new Trader.TradeItem
            {
                m_prefab = drop,
                m_stack = stack,
                m_price = price,
                m_requiredGlobalKey = string.Empty,
                m_levelUpEffect = false,
                m_buyPlayerEffects = new EffectList(),
                m_icon = null,
                m_name = drop.m_itemData.m_shared.m_name,
                m_tooltip = string.Empty,
                m_buyKey = string.Empty,
                m_incrementKey = string.Empty,
                m_incrementAmount = 0
            };
        }

        private static string ResolvePrefab(ItemDrop.ItemData item)
        {
            GameObject drop = item.m_dropPrefab;
            if (drop != null && TrophyCatalog.TryGet(drop.name, out _)) return drop.name;

            string token = item.m_shared != null ? item.m_shared.m_name : null;
            if (!string.IsNullOrEmpty(token) && TokenToPrefab.TryGetValue(token, out string prefab))
                return prefab;

            return null;
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
