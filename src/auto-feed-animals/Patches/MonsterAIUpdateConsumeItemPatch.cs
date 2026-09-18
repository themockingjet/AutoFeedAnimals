using HarmonyLib;

namespace AutoFeedAnimals
{
    [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.UpdateConsumeItem))]
    internal static class MonsterAIUpdateConsumeItemPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(MonsterAI __instance, Humanoid humanoid, float dt, ref bool __result)
        {
            if (__instance.m_consumeTarget != null)
            {
                return true;
            }

            if (AutoFeedAnimalsPlugin.FeedService.UpdateChestFeeding(__instance, humanoid, dt, ref __result))
            {
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        private static void Postfix(MonsterAI __instance, float dt)
        {
            if (__instance.m_consumeTarget == null)
            {
                AutoFeedAnimalsPlugin.FeedService.TryAcquireChestTarget(__instance, dt);
            }
        }
    }
}
