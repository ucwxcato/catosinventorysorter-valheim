using System;
using HarmonyLib;

namespace CatosInventorySorter
{
    [HarmonyPatch(typeof(InventoryGui), "Awake")]
    internal static class InventoryGuiPatches
    {
        private static void Postfix(InventoryGui __instance)
        {
            try { InventorySortButton.Attach(__instance); }
            catch (Exception ex) { Plugin.Log?.LogWarning($"Could not create inventory sort button: {ex.Message}"); }
        }
    }
}
