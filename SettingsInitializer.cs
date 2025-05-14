// File: SettingsInitializer.cs
using System;
using System.IO;
using System.Threading;
using HarmonyLib;
using Newtonsoft.Json;
using RimWorld;
using UnityEngine;
using Verse;

namespace PhoenixRAP
{
    [StaticConstructorOnStartup]
    public static class SettingsInitializer
    {
        // Path to your mod’s config.json (in the PhoenixsRAP mod folder)
        private static readonly string ConfigPath =
            Path.Combine(GenFilePaths.CoreModsFolderPath, "PhoenixsRAP", "config.json");

        // Static constructor runs very early—queue our init as a LongEvent
        static SettingsInitializer()
        {
            LongEventHandler.QueueLongEvent(DoInit, "PhoenixRAP: Initializing…", true, null);
        }

        private static void DoInit()
        {
            // 1) Load or create default config.json
            ModSettingsData data = LoadOrCreateConfig();

            // 2) If first run, launch Python wizard to fill out config.json
            if (!data.hasPlayedMod)
            {
                Log.Message("[PhoenixRAP] First run detected — launching setup wizard.");
                PRAPmain.LaunchRepl();
                PRAPmain.CallDelegate("import SettingsWizard; SettingsWizard.run_wizard()");

                // Wait (up to 5 minutes) for the wizard to set hasPlayedMod = true
                DateTime start = DateTime.Now;
                while (!FileContainsHasPlayedTrue() && (DateTime.Now - start).TotalSeconds < 300)
                {
                    Thread.Sleep(500);
                }

                // Reload config after wizard finishes
                data = LoadOrCreateConfig();
            }

            // 3) Apply settings into your GPU/delegate system
            ApplySettings(data);
            Log.Message("[PhoenixRAP] Settings initialized.");
        }

        private static ModSettingsData LoadOrCreateConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                var def = new ModSettingsData();      // default values
                WriteConfig(def);
                return def;
            }

            try
            {
                string json = File.ReadAllText(ConfigPath);
                return JsonConvert.DeserializeObject<ModSettingsData>(json)
                       ?? new ModSettingsData();
            }
            catch (Exception e)
            {
                Log.Error($"[PhoenixRAP] Failed to read config.json: {e}");
                return new ModSettingsData();
            }
        }

        private static bool FileContainsHasPlayedTrue()
        {
            try
            {
                string json = File.ReadAllText(ConfigPath);
                return json.Contains("\"hasPlayedMod\": true");
            }
            catch { return false; }
        }

        private static void WriteConfig(ModSettingsData data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            catch (Exception e)
            {
                Log.Error($"[PhoenixRAP] Failed to write config.json: {e}");
            }
        }

        private static void ApplySettings(ModSettingsData d)
        {
            // Feed these values into your read_config defaults or global Config
            // For example, if you have a static Config class:
            Config.enable_python_delegate   = d.enablePythonDelegate;
            Config.max_threads              = d.maxThreads;
            Config.gpu_power_preference     = d.gpuPowerPreference;
            Config.gpu_force_fallback       = d.gpuFallback;
            Config.gpu_required             = d.useGpu;
            Config.error_destination        = d.errorDestination;
        }
    }

    /// <summary>
    /// Mirrors the JSON structure in config.json
    /// </summary>
    public class ModSettingsData
    {
        public bool hasPlayedMod           = false;
        public bool useGpu                 = true;
        public string gpuPowerPreference   = "HighPerformance";
        public bool gpuFallback            = true;
        public int  maxThreads             = Environment.ProcessorCount;
        public bool enablePythonDelegate   = true;
        public string errorDestination     = "file";
    }
}
