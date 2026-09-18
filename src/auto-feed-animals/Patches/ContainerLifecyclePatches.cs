using HarmonyLib;

namespace AutoFeedAnimals
{
    internal static class ContainerLifecyclePatches
    {
        [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
        private static class AwakePatch
        {
            [HarmonyPostfix]
            private static void Postfix(Container __instance)
            {
                AutoFeedAnimalsPlugin.ContainerRegistry.Register(__instance);
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.OnDestroyed))]
        private static class DestroyedPatch
        {
            [HarmonyPostfix]
            private static void Postfix(Container __instance)
            {
                AutoFeedAnimalsPlugin.ContainerRegistry.Unregister(__instance);
            }
        }

        [HarmonyPatch(typeof(Container), nameof(Container.OnContainerChanged))]
        private static class ChangedPatch
        {
            [HarmonyPostfix]
            private static void Postfix(Container __instance)
            {
                AutoFeedAnimalsPlugin.ContainerRegistry.Register(__instance);
            }
        }
    }
}
