using Jotunn.Entities;
using Jotunn.Managers;

namespace HaldorIdols
{
    /// <summary>
    /// Tab captions. Jotunn injects these tokens into the game localization,
    /// so they follow the language selected in Valheim.
    /// </summary>
    internal static class TabsLocalization
    {
        internal const string GoodsToken = "$haldoridols_tab_goods";
        internal const string IdolsToken = "$haldoridols_tab_idols";
        internal const string SellToken = "$haldoridols_tab_sell";

        internal const string SwapToken = "$haldoridols_swap";
        internal const string SwapActionToken = "$haldoridols_swap_action";
        internal const string SwapDoneToken = "$haldoridols_swap_done";
        internal const string SwapFullToken = "$haldoridols_swap_full";

        private static bool _initialized;

        internal static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            CustomLocalization loc = LocalizationManager.Instance.GetLocalization();

            loc.AddTranslation("English", GoodsToken, "Goods");
            loc.AddTranslation("English", IdolsToken, "Idols");
            loc.AddTranslation("English", SellToken, "Sell");
            loc.AddTranslation("English", SwapToken, "Swap");
            loc.AddTranslation("English", SwapActionToken, "Swap");
            loc.AddTranslation("English", SwapDoneToken, "Swap complete");
            loc.AddTranslation("English", SwapFullToken, "Inventory is full");

            loc.AddTranslation("Russian", GoodsToken, "Товары");
            loc.AddTranslation("Russian", IdolsToken, "Идолы");
            loc.AddTranslation("Russian", SellToken, "Скупка");
            loc.AddTranslation("Russian", SwapToken, "Обмен");
            loc.AddTranslation("Russian", SwapActionToken, "Обменять");
            loc.AddTranslation("Russian", SwapDoneToken, "Обмен произведён");
            loc.AddTranslation("Russian", SwapFullToken, "Инвентарь полон");
        }

        internal static string Translate(string token)
        {
            string translated = LocalizationManager.Instance.TryTranslate(token);
            if (!string.IsNullOrEmpty(translated) && translated != token)
                return translated;

            if (token == IdolsToken) return "Idols";
            if (token == SellToken) return "Sell";
            if (token == SwapToken) return "Swap";
            if (token == SwapActionToken) return "Swap";
            if (token == SwapDoneToken) return "Swap complete";
            if (token == SwapFullToken) return "Inventory is full";
            return "Goods";
        }
    }
}
