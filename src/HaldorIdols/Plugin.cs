using System;
using System.IO;
using System.Text;
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

        // BepInEx does not watch the config file, so a manual edit only reaches
        // the running game after a restart. This watcher closes that gap.
        private const double ReloadDebounceSeconds = 0.4;
        private const int MaxReloadAttempts = 10;

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private readonly object _reloadLock = new object();

        private Harmony _harmony;
        private FileSystemWatcher _watcher;
        private bool _reloadPending;
        private DateTime _reloadDueUtc;
        private int _reloadAttempts;
        private bool _reloading;
        private bool _refreshQueued;
        private string _lastSnapshot;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            ModConfig.Init(Config);
            TabsLocalization.Init();

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(Plugin).Assembly);

            Config.SettingChanged += OnSettingChanged;
            Config.ConfigReloaded += OnConfigReloaded;
            SynchronizationManager.OnConfigurationSynchronized += OnConfigurationSynchronized;

            _lastSnapshot = BuildSnapshot();
            SetupWatcher();

            // ObjectDB is usually not loaded yet at plugin startup; the ObjectDB.Awake
            // postfix applies the chance as soon as the item database exists.
            UpgradeTuning.Apply();
            TrophyShop.ApplyPrices();

            Log.LogInfo($"{ModName} {ModVersion} loaded.");
        }

        private void Update()
        {
            ProcessReload();
            ProcessRefresh();
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            if (e == null) return;

            // A synced entry changes while connected: the host's value replaces
            // ours, so the upgrade chance has to be re-applied to the prefabs.
            if (ReferenceEquals(e.ChangedSetting, ModConfig.SuccessChance))
                UpgradeTuning.Apply();

            if (ReferenceEquals(e.ChangedSetting, ModConfig.Enabled) ||
                ReferenceEquals(e.ChangedSetting, ModConfig.TrophySelling) ||
                ModConfig.IsTrophyPriceEntry(e.ChangedSetting))
            {
                TrophyShop.ApplyPrices();
                TrophyShop.Invalidate();
            }

            QueueRefresh();
        }

        /// <summary>
        /// Fires after a manual config edit was reloaded from disk.
        /// </summary>
        private void OnConfigReloaded(object sender, EventArgs e)
        {
            string snapshot = BuildSnapshot();
            bool changed = snapshot != _lastSnapshot;
            _lastSnapshot = snapshot;

            UpgradeTuning.Apply();
            TrophyShop.ApplyPrices();
            TrophyShop.Invalidate();

            if (!changed) return;

            QueueRefresh();
            Log.LogInfo("Config reloaded: Enabled=" + ModConfig.Enabled.Value +
                        ", TrophySelling=" + ModConfig.TrophySelling.Value +
                        ", SuccessChance=" + ModConfig.SuccessChance.Value.ToString("P0") +
                        ", Wooden=" + ModConfig.Prices[0].Value +
                        ", Bloodgold=" + ModConfig.Prices[7].Value);
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
            TrophyShop.ApplyPrices();
            TrophyShop.Invalidate();
            _lastSnapshot = BuildSnapshot();
            QueueRefresh();

            Log.LogInfo("Config synchronized from server: Enabled=" + ModConfig.Enabled.Value +
                        ", TrophySelling=" + ModConfig.TrophySelling.Value +
                        ", SuccessChance=" + ModConfig.SuccessChance.Value.ToString("P0") +
                        ", Wooden=" + ModConfig.Prices[0].Value +
                        ", Bloodgold=" + ModConfig.Prices[7].Value);
        }

        private void SetupWatcher()
        {
            try
            {
                string path = Config.ConfigFilePath;
                if (string.IsNullOrEmpty(path)) return;

                string directory = Path.GetDirectoryName(path);
                string file = Path.GetFileName(path);
                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(file) || !Directory.Exists(directory))
                    return;

                _watcher = new FileSystemWatcher(directory, file)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size |
                                   NotifyFilters.FileName | NotifyFilters.CreationTime,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };

                _watcher.Changed += OnConfigFileTouched;
                _watcher.Created += OnConfigFileTouched;
                _watcher.Deleted += OnConfigFileTouched;
                _watcher.Renamed += OnConfigFileRenamed;
                _watcher.Error += OnWatcherError;
            }
            catch (Exception e)
            {
                Log.LogWarning("Config hot reload is unavailable: " + e.Message);
            }
        }

        private void OnConfigFileTouched(object sender, FileSystemEventArgs e)
        {
            QueueReload();
        }

        private void OnConfigFileRenamed(object sender, RenamedEventArgs e)
        {
            QueueReload();
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            Exception error = e != null ? e.GetException() : null;
            Log.LogWarning("Config watcher error: " + (error != null ? error.Message : "unknown"));
        }

        private void QueueReload()
        {
            lock (_reloadLock)
            {
                _reloadDueUtc = DateTime.UtcNow.AddSeconds(ReloadDebounceSeconds);
                _reloadPending = true;
            }
        }

        private void ProcessReload()
        {
            bool due;

            lock (_reloadLock)
            {
                if (!_reloadPending) return;

                due = DateTime.UtcNow >= _reloadDueUtc;
                if (!due) return;

                _reloadPending = false;
            }

            // The host owns the synced values; a client edit would only be
            // overwritten by the next synchronization.
            if (ZNet.instance != null && !ZNet.instance.IsServer())
            {
                Log.LogInfo("Config file changed, but only the host can apply it while connected.");
                return;
            }

            TryReloadConfig();
        }

        private void TryReloadConfig()
        {
            if (_reloading) return;
            _reloading = true;

            bool saveOnSet = Config.SaveOnConfigSet;

            try
            {
                // Reload writes the entries back, which would immediately
                // overwrite the file we just read.
                Config.SaveOnConfigSet = false;
                Config.Reload();
                _reloadAttempts = 0;
            }
            catch (IOException e)
            {
                // The editor may still hold the file open; retry shortly.
                _reloadAttempts++;

                if (_reloadAttempts <= MaxReloadAttempts)
                {
                    Log.LogDebug("Config reload postponed (" + _reloadAttempts + "): " + e.Message);
                    QueueReload();
                }
                else
                {
                    _reloadAttempts = 0;
                    Log.LogWarning("Config reload failed, keeping the previous values: " + e.Message);
                }
            }
            catch (Exception e)
            {
                Log.LogWarning("Config reload failed, keeping the previous values: " + e.Message);
            }
            finally
            {
                Config.SaveOnConfigSet = saveOnSet;
                _reloading = false;
            }
        }

        private void QueueRefresh()
        {
            _refreshQueued = true;
        }

        private void ProcessRefresh()
        {
            if (!_refreshQueued) return;
            _refreshQueued = false;
            ShopTabsController.RefreshOpenShop();
        }

        private string BuildSnapshot()
        {
            var builder = new StringBuilder(768);

            builder.Append(ModConfig.Enabled.Value).Append('|')
                   .Append(ModConfig.TrophySelling.Value).Append('|')
                   .Append(ModConfig.SuccessChance.Value.ToString("R"));

            for (int i = 0; i < ModConfig.Prices.Length; i++)
            {
                builder.Append('|').Append(ModConfig.Prices[i].Value)
                       .Append(':').Append(ModConfig.Keys[i].Value);
            }

            foreach (TrophyCatalog.Entry trophy in TrophyCatalog.Entries)
            {
                builder.Append('|').Append(ModConfig.TrophyPrice(trophy.Prefab));
            }

            return builder.ToString();
        }

        private void OnDestroy()
        {
            Config.SettingChanged -= OnSettingChanged;
            Config.ConfigReloaded -= OnConfigReloaded;
            SynchronizationManager.OnConfigurationSynchronized -= OnConfigurationSynchronized;

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }

            _harmony?.UnpatchSelf();
        }
    }
}
