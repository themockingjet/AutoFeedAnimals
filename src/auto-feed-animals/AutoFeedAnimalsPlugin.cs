using BepInEx;
using HarmonyLib;

namespace AutoFeedAnimals
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class AutoFeedAnimalsPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "str.autofeedanimals";
        public const string PluginName = "Auto Feed Animals";
        public const string PluginVersion = "0.1.0";

        internal static AutoFeedAnimalsSettings Settings { get; private set; } = null!;
        internal static FeedContainerRegistry ContainerRegistry { get; private set; } = null!;
        internal static AnimalFeedService FeedService { get; private set; } = null!;

        private Harmony? _harmony;

        private void Awake()
        {
            Settings = new AutoFeedAnimalsSettings(Config);
            ContainerRegistry = new FeedContainerRegistry(Settings);
            FeedService = new AnimalFeedService(Settings, ContainerRegistry);
            Settings.ChestSettingsChanged += FeedService.ClearChestTargets;

            if (!Settings.ConfigSyncEnabled)
            {
                Logger.LogWarning("ServerSync is incompatible with this Valheim build; synchronized config is disabled. Update ServerSync to restore multiplayer config synchronization.");
            }

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(AutoFeedAnimalsPlugin).Assembly);

            Logger.LogInfo($"{PluginName} {PluginVersion} loaded. Auto-feeding: {Settings.AutoFeedingEnabled}.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
