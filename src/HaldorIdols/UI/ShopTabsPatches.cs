using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace HaldorIdols
{
    /// <summary>
    /// StoreGui asks Trader.GetAvailableItems() for the rows it draws. Replacing
    /// that call in the three methods that use it makes the whole window (list,
    /// selection and buy button) work against the currently selected tab.
    /// </summary>
    [HarmonyPatch]
    internal static class StoreGuiItemListPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(StoreGui), "FillList");
            yield return AccessTools.Method(typeof(StoreGui), "SelectItem");
            yield return AccessTools.Method(typeof(StoreGui), "GetSelectedItemIndex");
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo original = AccessTools.Method(typeof(Trader), nameof(Trader.GetAvailableItems));
            MethodInfo replacement = AccessTools.Method(typeof(ShopTabsController),
                nameof(ShopTabsController.GetVisibleItems));

            int replaced = 0;

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(original))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = replacement;
                    replaced++;
                }

                yield return instruction;
            }

            if (replaced != 1)
            {
                Plugin.Log.LogError("Unexpected StoreGui item-list layout: replaced " + replaced +
                                    " GetAvailableItems call(s). Tab filtering may be disabled.");
            }
        }
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Show))]
    internal static class StoreGuiShowPatch
    {
        private static void Postfix(StoreGui __instance)
        {
            ShopTabsController.Sync(__instance);
        }
    }

    [HarmonyPatch(typeof(StoreGui), "Hide")]
    internal static class StoreGuiHidePatch
    {
        private static void Postfix()
        {
            ShopTabsController.OnHide();
        }
    }

    [HarmonyPatch(typeof(StoreGui), "Update")]
    internal static class StoreGuiUpdatePatch
    {
        private static void Prefix()
        {
            ShopTabsController.Tick();
        }
    }

    [HarmonyPatch(typeof(StoreGui), "OnDestroy")]
    internal static class StoreGuiDestroyPatch
    {
        private static void Prefix()
        {
            ShopTabsController.OnDestroy();
        }
    }
}
