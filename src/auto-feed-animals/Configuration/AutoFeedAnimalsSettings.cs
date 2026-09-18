using BepInEx.Configuration;
using ServerSync;
using System;
using System.Reflection;

namespace AutoFeedAnimals
{
    internal sealed class AutoFeedAnimalsSettings
    {
        internal bool AutoFeedingEnabled { get; private set; }
        internal float FeedRangeMeters { get; private set; }
        internal bool IgnorePathing { get; private set; }
        internal bool ProtectFeedContainers { get; private set; }
        internal string DisallowFeed { get; private set; } = string.Empty;
        internal string DisallowAnimal { get; private set; } = string.Empty;
        internal bool ShowAnimalStats { get; private set; }
        internal FeedFilter Filter { get; }
        internal bool ConfigSyncEnabled { get; private set; }

        internal event Action? ChestSettingsChanged;

        private readonly ConfigSync? _configSync;

        internal AutoFeedAnimalsSettings(ConfigFile config)
        {
            ConfigEntry<bool> enableAutoFeeding = config.Bind(
                "Feeding",
                "Enable Auto Feeder",
                true,
                "Enables native feeding for tame, hungry animals from accessible chest food.");
            ConfigEntry<float> feedRange = config.Bind(
                "Feeding",
                "Feed Range (Meters)",
                5f,
                new ConfigDescription(
                    "Maximum 3D distance from an animal to a feed container.",
                    new AcceptableValueRange<float>(1f, 60f)));
            ConfigEntry<bool> ignorePathing = config.Bind(
                "Feeding",
                "Ignore Pathing",
                false,
                "Consumes chest food remotely within range without requiring a walkable path.");
            ConfigEntry<bool> protectFeedContainers = config.Bind(
                "Feeding",
                "Protect Feed Containers",
                true,
                "Prevents untamed animals from damaging registered feed containers.");
            ConfigEntry<string> disallowFeed = config.Bind(
                "Feeding",
                "Disallow Feed",
                string.Empty,
                "Comma-separated food names excluded from automatic chest feeding.");
            ConfigEntry<string> disallowAnimal = config.Bind(
                "Feeding",
                "Disallow Animal",
                string.Empty,
                "Comma-separated animal names excluded from automatic chest feeding.");
            ConfigEntry<bool> showAnimalStats = config.Bind(
                "Interface",
                "Show Animal Stats",
                true,
                "Shows native taming progress beside untamed animal names.");

            AutoFeedingEnabled = enableAutoFeeding.Value;
            IgnorePathing = ignorePathing.Value;
            FeedRangeMeters = feedRange.Value;
            ProtectFeedContainers = protectFeedContainers.Value;
            DisallowFeed = disallowFeed.Value;
            DisallowAnimal = disallowAnimal.Value;
            ShowAnimalStats = showAnimalStats.Value;
            Filter = new FeedFilter(DisallowFeed, DisallowAnimal);

            enableAutoFeeding.SettingChanged += (_, _) => AutoFeedingEnabled = enableAutoFeeding.Value;
            ignorePathing.SettingChanged += (_, _) =>
            {
                IgnorePathing = ignorePathing.Value;
                ChestSettingsChanged?.Invoke();
            };
            feedRange.SettingChanged += (_, _) =>
            {
                FeedRangeMeters = feedRange.Value;
                ChestSettingsChanged?.Invoke();
            };
            protectFeedContainers.SettingChanged += (_, _) => ProtectFeedContainers = protectFeedContainers.Value;
            disallowFeed.SettingChanged += (_, _) =>
            {
                DisallowFeed = disallowFeed.Value;
                Filter.Update(DisallowFeed, DisallowAnimal);
                ChestSettingsChanged?.Invoke();
            };
            disallowAnimal.SettingChanged += (_, _) =>
            {
                DisallowAnimal = disallowAnimal.Value;
                Filter.Update(DisallowFeed, DisallowAnimal);
                ChestSettingsChanged?.Invoke();
            };
            showAnimalStats.SettingChanged += (_, _) => ShowAnimalStats = showAnimalStats.Value;

            if (!IsConfigSyncCompatible())
            {
                return;
            }

            _configSync = new ConfigSync(AutoFeedAnimalsPlugin.PluginGuid)
            {
                DisplayName = AutoFeedAnimalsPlugin.PluginName,
                CurrentVersion = AutoFeedAnimalsPlugin.PluginVersion,
                MinimumRequiredVersion = AutoFeedAnimalsPlugin.PluginVersion
            };
            _configSync.AddConfigEntry(enableAutoFeeding);
            _configSync.AddConfigEntry(ignorePathing);
            _configSync.AddConfigEntry(feedRange);
            _configSync.AddConfigEntry(protectFeedContainers);
            _configSync.AddConfigEntry(disallowFeed);
            _configSync.AddConfigEntry(disallowAnimal);
            ConfigSyncEnabled = true;
        }

        private static bool IsConfigSyncCompatible()
        {
            FieldInfo? everybody = typeof(ZRoutedRpc).GetField(
                "Everybody",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return everybody != null && !everybody.IsLiteral;
        }
    }
}
