using HarmonyLib;

namespace AutoFeedAnimals
{
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Damage))]
    internal static class FeedContainerProtectionPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(WearNTear __instance, HitData hit)
        {
            return !AutoFeedAnimalsPlugin.FeedService.ShouldProtectContainer(__instance, hit);
        }
    }
}
