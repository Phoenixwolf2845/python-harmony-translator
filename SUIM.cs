// File: SUIM.cs
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
using SimpleJSON;  // from Unity's MiniJSON or similar

namespace PythonHarmonyTranslator
{
    [StaticConstructorOnStartup]
    public static class SUIM
    {
        // In-memory representation of one setting
        private abstract class Setting
        {
            public string Key;
            public abstract void Draw(ref float y, string sourcePath);
            public abstract void Commit(string sourcePath);
        }
        private class BoolSetting : Setting
        {
            public bool Value;
            public override void Draw(ref float y, string sourcePath)
            {
                Rect r = new Rect(10, y, 300, 24);
                bool newVal = Widgets.ButtonText(r, $"{Key}: {(Value ? "On" : "Off")}");
                if (newVal != Value)
                {
                    Value = newVal;
                    Commit(sourcePath);
                }
                y += 28;
            }
            public override void Commit(string sourcePath)
            {
                var json = JSON.Parse(File.ReadAllText(sourcePath));
                json[Key] = Value;
                File.WriteAllText(sourcePath, json.ToString(2));
            }
        }
        private class NumSetting : Setting
        {
            public float Min, Max, Current;
            public override void Draw(ref float y, string sourcePath)
            {
                Rect labelR = new Rect(10, y, 200, 24);
                Widgets.Label(labelR, $"{Key}: {Current}");
                Rect sliderR = new Rect(220, y, 300, 24);
                float newVal = Widgets.HorizontalSlider(sliderR, Current, Min, Max);
                if (Math.Abs(newVal - Current) > 0.001f)
                {
                    Current = newVal;
                    Commit(sourcePath);
                }
                y += 28;
            }
            public override void Commit(string sourcePath)
            {
                var json = JSON.Parse(File.ReadAllText(sourcePath));
                json[Key] = new JSONClass {
                    ["0"] = Min,
                    ["1"] = Max,
                    ["2"] = Current
                };
                File.WriteAllText(sourcePath, json.ToString(2));
            }
        }
        private class TextSetting : Setting
        {
            public List<string> Options;
            public int Index;
            public override void Draw(ref float y, string sourcePath)
            {
                Rect r = new Rect(10, y, 300, 24);
                string label = $"{Key}: {Options[Index]}";
                if (Widgets.ButtonText(r, label))
                {
                    Index = (Index + 1) % Options.Count;
                    Commit(sourcePath);
                }
                y += 28;
            }
            public override void Commit(string sourcePath)
            {
                var json = JSON.Parse(File.ReadAllText(sourcePath));
                json[Key] = Options[Index];
                File.WriteAllText(sourcePath, json.ToString(2));
            }
        }

        // Holds one mod’s UI data
        private class ModEntry
        {
            public string Id;
            public string Version;
            public List<string> Requirements;
            public bool UseExternal;
            public string SourcePath;
            public List<Setting> Settings = new List<Setting>();
        }

        private static List<ModEntry> modEntries;
        private static bool windowOpen = false;
        private static Rect windowRect = new Rect(100, 100, 550, 600);

        static SUIM()
        {
            LongEventHandler.QueueLongEvent(Init, "Initializing PHT UI…", false, null);
        }

        private static void Init()
        {
            var harmony = new Harmony("PythonHarmonyTranslator.SUIM");
            harmony.Patch(
                AccessTools.Method(typeof(MainButtonsRoot), nameof(MainButtonsRoot.DoButtons)),
                postfix: new HarmonyMethod(typeof(SUIM), nameof(DoButtons_Postfix))
            );
        }

        public static void DoButtons_Postfix()
        {
            var buttons = MainButtonsRoot.Instance.Buttons;
            if (buttons == null) return;

            // find gear/settings button
            var settingsBtn = buttons.FirstOrDefault(b => b.defName == "Menu_Settings");
            if (settingsBtn != null)
            {
                Rect sr = MainButtonsRoot.Instance.GetButtonRect(settingsBtn);
                Rect br = new Rect(sr.x - sr.width - 4, sr.y, sr.width, sr.height);

                if (Widgets.ButtonText(br, "PHT Settings"))
                {
                    BuildModEntries();
                    windowOpen = true;
                }
            }

            if (windowOpen)
                DoWindowContents();
        }

        private static void BuildModEntries()
        {
            // load imports.json
            string phtFolder = Path.Combine(GenFilePaths.SaveDataFolderPath, "PythonHarmonyTranslator");
            var impJson = JSON.Parse(File.ReadAllText(Path.Combine(phtFolder, "imports.json")))["KnownMods"];

            // scan running mods
            modEntries = new List<ModEntry>();
            foreach (var mod in LoadedModManager.RunningModsListForReading)
            {
                string pid = mod.PackageId;
                bool hasImport = impJson.HasKey(pid);
                string modConfig = Path.Combine(mod.RootDir, "modconfig.json");
                bool useExternal = false;
                if (File.Exists(modConfig))
                {
                    var mc = JSON.Parse(File.ReadAllText(modConfig));
                    useExternal = mc["isUsingExternalSettings"].AsBool;
                }

                // only include if known mod
                if (!hasImport && !useExternal) 
                    continue;

                var entry = new ModEntry {
                    Id = pid,
                    UseExternal = useExternal,
                    SourcePath = useExternal ? modConfig : Path.Combine(phtFolder, "imports.json")
                };

                // version
                if (useExternal)
                    entry.Version = JSON.Parse(File.ReadAllText(modConfig))["Version"];
                else
                    entry.Version = impJson[pid]["Version"];

                // requirements
                entry.Requirements = new List<string>();
                foreach (var r in impJson[pid]["requirements"].AsArray)
                    entry.Requirements.Add(r);

                // now load settings keys
                var src = JSON.Parse(File.ReadAllText(entry.SourcePath))[pid];
                foreach (var kv in src.AsObject)
                {
                    if (kv.Key == "Version" || kv.Key == "requirements" || kv.Key == "isUsingExternalSettings")
                        continue;

                    var node = kv.Value;
                    Setting s;
                    if (node.IsBoolean)
                    {
                        s = new BoolSetting { Key = kv.Key, Value = node.AsBool };
                    }
                    else if (node.IsNumber && node.AsArray != null && node.AsArray.Count == 3)
                    {
                        var arr = node.AsArray;
                        s = new NumSetting {
                            Key = kv.Key,
                            Min = arr[0].AsFloat,
                            Max = arr[1].AsFloat,
                            Current = arr[2].AsFloat
                        };
                    }
                    else if (node.IsString && node.AsArray != null)
                    {
                        var opts = node.AsArray.Select(x => x.Value).ToList();
                        int idx = opts.IndexOf(node.Value);
                        s = new TextSetting { Key = kv.Key, Options = opts, Index = idx >= 0 ? idx : 0 };
                    }
                    else
                    {
                        // unsupported type—skip
                        continue;
                    }
                    entry.Settings.Add(s);
                }

                modEntries.Add(entry);
            }
        }

        private static void DoWindowContents()
        {
            GUI.Window(123456, windowRect, DrawWindow, "PHT Settings");
        }

        private static void DrawWindow(int id)
        {
            Widgets.DrawMenuSection(windowRect);

            // header
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(windowRect.x + 10, windowRect.y + 10, 300, 32), "Python Harmony Translator");
            Text.Font = GameFont.Small;

            float y = windowRect.y + 50;
            foreach (var m in modEntries)
            {
                Widgets.Label(new Rect(windowRect.x + 10, y, 200, 24), $"{m.Id} v{m.Version}");
                y += 24;
                // show requirements
                Widgets.Label(new Rect(windowRect.x + 20, y, 300, 20),
                              "Requires: " + string.Join(", ", m.Requirements));
                y += 24;
                // draw each setting
                foreach (var s in m.Settings)
                    s.Draw(ref y, m.SourcePath);
                y += 10;
            }

            if (Widgets.ButtonText(new Rect(windowRect.x + windowRect.width - 80,
                                           windowRect.y + windowRect.height - 35, 70, 30),
                                   "Close"))
            {
                windowOpen = false;
            }
        }
    }
}
