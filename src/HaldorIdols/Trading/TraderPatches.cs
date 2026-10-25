using HarmonyLib;

namespace HaldorIdols
{
    /// <summary>
    /// Haldor's trade list is stored on the Trader component. The list is filled
    /// right before the store UI opens, which is the only reliable moment to add
    /// runtime rows to it.
    /// </summary>
    [HarmonyPatch(typeof(Trader), nameof(Trader.Interact))]
    internal static class TraderInteractPatch
    {
        private static void Prefix(Trader __instance)
        {
            if (!HaldorShop.IsHaldor(__instance)) return;
            HaldorShop.Refresh(__instance);
        }
    }
}
