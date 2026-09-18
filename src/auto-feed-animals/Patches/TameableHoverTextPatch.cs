using HarmonyLib;
using System;

namespace AutoFeedAnimals
{
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.GetHoverText))]
    internal static class TameableHoverTextPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Tameable __instance, ref string __result)
        {
            if (!AutoFeedAnimalsPlugin.Settings.ShowAnimalStats || __instance.IsTamed() ||
                __instance.GetStatusString().IndexOf("Acclimatizing", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            __result = $"{__instance.GetName()} ({__instance.GetTameness()}%)";
        }
    }
}
