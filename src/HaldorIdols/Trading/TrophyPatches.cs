using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace HaldorIdols
{
    /// <summary>
    /// Vanilla sells the first valuable item in the inventory. In the Sell tab
    /// the native Sell button has to sell the trophy the player selected instead.
    /// </summary>
    [HarmonyPatch(typeof(StoreGui), "GetSellableItem")]
    internal static class StoreGuiGetSellableItemPatch
    {
        private static readonly FieldInfo SelectedItemField =
            AccessTools.Field(typeof(StoreGui), "m_selectedItem");

        private static readonly List<ItemDrop.ItemData> TempValuables = new List<ItemDrop.ItemData>();

        private static bool Prefix(StoreGui __instance, ref ItemDrop.ItemData __result)
        {
            if (ShopTabsController.ActiveTab != ShopTabsController.TabSell) return true;
            if (!TrophyShop.Enabled) return true;

            Trader.TradeItem selected = SelectedItemField != null
                ? SelectedItemField.GetValue(__instance) as Trader.TradeItem
                : null;

            if (selected == null || selected.m_prefab == null ||
                ModConfig.TrophyPrice(selected.m_prefab.name) <= 0)
            {
                __result = null;
                return false;
            }

            __result = TrophyShop.FindInInventory(selected.m_prefab.name);

            return false;
        }

        /// <summary>
        /// Outside the Sell tab the native button keeps its vanilla behaviour,
        /// but it must not silently sell a trophy the player never picked.
        /// </summary>
        private static void Postfix(ref ItemDrop.ItemData __result)
        {
            if (ShopTabsController.ActiveTab == ShopTabsController.TabSell) return;
            if (__result == null || __result.m_shared == null) return;
            if (__result.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy) return;

            __result = FindNonTrophyValuable();
        }

        private static ItemDrop.ItemData FindNonTrophyValuable()
        {
            Player player = Player.m_localPlayer;
            if (player == null) return null;

            Inventory inventory = player.GetInventory();
            if (inventory == null) return null;

            TempValuables.Clear();
            inventory.GetValuableItems(TempValuables);

            for (int i = 0; i < TempValuables.Count; i++)
            {
                ItemDrop.ItemData item = TempValuables[i];
                if (item == null || item.m_shared == null) continue;
                if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy) return item;
            }

            return null;
        }
    }

    /// <summary>
    /// The Buy button is meaningless in the Sell tab; the native Sell button is
    /// used for the actual transaction.
    /// </summary>
    [HarmonyPatch(typeof(StoreGui), "BuySelectedItem")]
    internal static class StoreGuiBuyInSellTabPatch
    {
        private static bool Prefix()
        {
            return ShopTabsController.ActiveTab != ShopTabsController.TabSell;
        }
    }

    [HarmonyPatch(typeof(StoreGui), "UpdateBuyButton")]
    internal static class StoreGuiUpdateBuyButtonPatch
    {
        private static void Postfix(StoreGui __instance)
        {
            if (ShopTabsController.ActiveTab != ShopTabsController.TabSell) return;
            if (__instance.m_buyButton == null) return;

            __instance.m_buyButton.interactable = false;

            UITooltip tooltip = __instance.m_buyButton.GetComponent<UITooltip>();
            if (tooltip != null) tooltip.m_text = string.Empty;
        }
    }

    /// <summary>
    /// FillList greys out (and hides the icon of) rows the player cannot afford.
    /// In the Sell tab the price is income, so every row stays readable even
    /// when the player has no coins at all.
    /// </summary>
    [HarmonyPatch(typeof(StoreGui), "FillList")]
    internal static class StoreGuiSellTabFillListPatch
    {
        private static void Postfix(StoreGui __instance)
        {
            if (ShopTabsController.ActiveTab != ShopTabsController.TabSell) return;
            if (!TrophyShop.Enabled) return;

            try
            {
                TrophyShop.WhitenRows(__instance);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("Could not restyle the Sell tab rows: " + e);
            }
        }
    }
}
