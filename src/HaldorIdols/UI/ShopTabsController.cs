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

        internal static int ActiveTab => _activeTab;

        internal static string Token(int tab)
        {
            return tab == TabIdols ? TabsLocalization.IdolsToken : TabsLocalization.GoodsToken;
        }

        /// <summary>
        /// Drop-in replacement for Trader.GetAvailableItems() inside StoreGui.
        /// If the tab bar could not be built, the full list is returned so the
        /// idols stay reachable in a single list.
        /// </summary>
        internal static List<Trader.TradeItem> GetVisibleItems(Trader trader)
        {
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

            // StoreGui cannot handle an empty list: SelectItem(0) would index [0].
            // Falling back to the full list keeps the window usable.
            if (visible.Count == 0) return available;

            return visible;
        }

        internal static void Sync(StoreGui gui)
        {
            if (gui == null || gui.m_rootPanel == null || _uiFailed) return;

            Trader trader = TraderField != null ? TraderField.GetValue(gui) as Trader : null;
            if (!HaldorShop.IsHaldor(trader)) return;

            bool created = false;
            try
            {
                if (_view == null || !_view.IsAlive)
                {
                    _view = ShopTabsView.Create(gui);
                    _uiAvailable = true;
                    created = true;
                }

                _view.SetVisible(true);
                _view.Refresh();
            }
            catch (System.Exception e)
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
            if (_activeTab == TabGoods) return;
            _activeTab = TabGoods;
            _view?.Refresh();
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
            _activeTab = TabGoods;
        }

        private static void RefreshList(StoreGui gui)
        {
            if (FillListMethod == null) return;
            FillListMethod.Invoke(gui, null);
        }
    }
}
