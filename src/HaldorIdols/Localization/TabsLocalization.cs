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

        private static bool _initialized;

        internal static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            CustomLocalization loc = LocalizationManager.Instance.GetLocalization();

            loc.AddTranslation("English", GoodsToken, "Goods");
            loc.AddTranslation("English", IdolsToken, "Idols");
            loc.AddTranslation("English", SellToken, "Sell");

            loc.AddTranslation("Russian", GoodsToken, "Товары");
            loc.AddTranslation("Russian", IdolsToken, "Идолы");
            loc.AddTranslation("Russian", SellToken, "Скупка");
        }

        internal static string Translate(string token)
        {
            string translated = LocalizationManager.Instance.TryTranslate(token);
            if (!string.IsNullOrEmpty(translated) && translated != token)
                return translated;

            if (token == IdolsToken) return "Idols";
            if (token == SellToken) return "Sell";
            return "Goods";
        }
    }
}
