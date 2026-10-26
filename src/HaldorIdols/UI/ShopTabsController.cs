using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace HaldorIdols
{
    /// <summary>
    /// Owns the active shop tab and supplies the filtered item list to StoreGui.
    /// </summary>
    internal static class ShopTabsController
    {
        internal const int TabGoods = 0;
        internal const int TabIdols = 1;
        internal const int TabCount = 2;

        private static readonly MethodInfo FillListMethod = AccessTools.Method(typeof(StoreGui), "FillList");
        private static readonly FieldInfo TraderField = AccessTools.Field(typeof(StoreGui), "m_trader");

        private static int _activeTab = TabGoods;
        private static ShopTabsView _view;
        private static bool _uiAvailable;
        private static bool _uiFailed;
        private static int _visibleCount;

        internal static int ActiveTab => _activeTab;

        /// <summary>
        /// Number of rows StoreGui currently sees. Used to keep SelectItem safe
        /// when a tab has no items at all.
        /// </summary>
        internal static int VisibleCount => _uiAvailable ? _visibleCount : int.MaxValue;

        internal static string Token(int tab)
        {
            return tab == TabIdols ? TabsLocalization.IdolsToken : TabsLocalization.GoodsToken;
        }

        /// <summary>
        /// Drop-in replacement for Trader.GetAvailableItems() inside StoreGui.
        /// When the tab bar is unavailable the full list is returned so the idols
        /// stay reachable in a single list.
        /// </summary>
        internal static List<Trader.TradeItem> GetVisibleItems(Trader trader)
        {
            if (trader == null) return new List<Trader.TradeItem>(0);

            List<Trader.TradeItem> available = trader.GetAvailableItems();
            if (!_uiAvailable) return available;

            bool wantIdols = _activeTab == TabIdols;
            var visible = new List<Trader.TradeItem>(available.Count);

            for (int i = 0; i < available.Count; i++)
            {
                Trader.TradeItem item = available[i];
                bool isIdol = item != null
                              && item.m_prefab != null
                              && IdolCatalog.IsIdolPrefab(item.m_prefab.name);

                if (isIdol == wantIdols)
                    visible.Add(item);
            }

            _visibleCount = visible.Count;
            return visible;
        }

        /// <summary>
        /// Called right before StoreGui rebuilds its row list, so the idols are
        /// always present in Trader.m_items no matter how the shop was opened.
        /// </summary>
        internal static void PrepareList(StoreGui gui)
        {
            if (gui == null) return;

            Trader trader = TraderField != null ? TraderField.GetValue(gui) as Trader : null;
            if (!HaldorShop.IsHaldor(trader)) return;

            HaldorShop.Refresh(trader);
        }

        internal static void Sync(StoreGui gui)
        {
            if (gui == null || gui.m_rootPanel == null) return;

            Trader trader = TraderField != null ? TraderField.GetValue(gui) as Trader : null;
            bool wasAvailable = _uiAvailable;

            // The tab bar only belongs to Haldor; other traders keep vanilla UI.
            if (!HaldorShop.IsHaldor(trader) || !ModConfig.Enabled.Value || _uiFailed)
            {
                _view?.SetVisible(false);
                _uiAvailable = false;
                if (_activeTab != TabGoods)
                {
                    _activeTab = TabGoods;
                    _view?.Refresh();
                }

                // The list was drawn with the old filter; rebuild it unfiltered.
                if (wasAvailable) RefreshList(gui);
                return;
            }

            bool created = false;
            try
            {
                if (_view == null || !_view.IsAlive)
                {
                    _view = ShopTabsView.Create(gui);
                    created = true;
                }

                _uiAvailable = true;
                _view.SetVisible(true);
                _view.Refresh();
            }
            catch (Exception e)
            {
                _uiFailed = true;
                _uiAvailable = false;
                Plugin.Log.LogError("Could not build the Idols tab, showing a single list instead: " + e);
                return;
            }

            // The first FillList of a session ran before the tab bar existed.
            if (created) RefreshList(gui);
        }

        internal static void Select(int tab)
        {
            if (tab < 0 || tab >= TabCount) return;
            if (tab == _activeTab)
            {
                _view?.Refresh();
                return;
            }

            _activeTab = tab;
            _view?.Refresh();

            StoreGui gui = StoreGui.instance;
            if (gui != null && StoreGui.IsVisible()) RefreshList(gui);
        }

        internal static void OnHide()
        {
            _view?.SetVisible(false);

            // The next Show calls Sync(), which refreshes the bar for the Goods tab.
            _activeTab = TabGoods;
        }

        internal static void Tick()
        {
            _view?.Tick();
        }

        internal static void OnDestroy()
        {
            _view?.Dispose();
            _view = null;
            _uiAvailable = false;
            _uiFailed = false;
            _visibleCount = 0;
            _activeTab = TabGoods;
        }

        /// <summary>
        /// Re-applies prices, progression and the master switch while the shop is
        /// open, so config edits (including a server sync) take effect immediately.
        /// </summary>
        internal static void RefreshOpenShop()
        {
            StoreGui gui = StoreGui.instance;
            if (gui == null || !StoreGui.IsVisible()) return;

            Trader trader = TraderField != null ? TraderField.GetValue(gui) as Trader : null;
            if (!HaldorShop.IsHaldor(trader)) return;

            HaldorShop.Refresh(trader);
            Sync(gui);
            RefreshList(gui);
        }

        private static void RefreshList(StoreGui gui)
        {
            if (FillListMethod == null || gui == null) return;

            try
            {
                FillListMethod.Invoke(gui, null);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Could not rebuild the shop list: " + e);
            }
        }
    }
}
