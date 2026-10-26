using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;

namespace HaldorIdols
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "Gezorg.HaldorIdols";
        public const string ModName = "Haldor Idols";
        public const string ModVersion = "1.0.0";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            ModConfig.Init(Config);
            TabsLocalization.Init();

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Config.SettingChanged += OnSettingChanged;
            SynchronizationManager.OnConfigurationSynchronized += OnConfigurationSynchronized;

            // ObjectDB is usually not loaded yet at plugin startup; the ObjectDB.Awake
            // postfix applies the chance as soon as the item database exists.
            UpgradeTuning.Apply();

            Log.LogInfo($"{ModName} {ModVersion} loaded.");
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            // A synced entry changes while connected: the host's value replaces
            // ours, so the upgrade chance has to be re-applied to the prefabs.
            if (e.ChangedSetting == ModConfig.SuccessChance)
                UpgradeTuning.Apply();

            ShopTabsController.RefreshOpenShop();
        }

        /// <summary>
        /// Fires after Jotunn applied server config values. Used to log the
        /// effective values (proof that the host's config won) and to re-apply
        /// the upgrade chance even if the value happened to be unchanged.
        /// </summary>
        private void OnConfigurationSynchronized(object sender, ConfigurationSynchronizationEventArgs e)
        {
            if (e == null || e.UpdatedPluginGUIDs == null || !e.UpdatedPluginGUIDs.Contains(ModGuid))
                return;

            UpgradeTuning.Apply();
            ShopTabsController.RefreshOpenShop();

            Log.LogInfo("Config synchronized from server: Enabled=" + ModConfig.Enabled.Value +
                        ", SuccessChance=" + ModConfig.SuccessChance.Value.ToString("P0") +
                        ", Wooden=" + ModConfig.Prices[0].Value +
                        ", Bloodgold=" + ModConfig.Prices[7].Value);
        }

        private void OnDestroy()
        {
            Config.SettingChanged -= OnSettingChanged;
            SynchronizationManager.OnConfigurationSynchronized -= OnConfigurationSynchronized;
            _harmony?.UnpatchSelf();
        }
    }
}
