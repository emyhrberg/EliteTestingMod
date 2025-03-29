using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ModHelper.Common.Configs;
using MonoMod.RuntimeDetour;
using Terraria;
using Terraria.ID;

namespace ModHelper.Helpers
{
    //All functions, related to reload
    internal class ReloadUtilities
    {
        public static void PrepareClient(ClientMode clientMode)
        {
            ClientDataHandler.ClientMode = clientMode;
            ClientDataHandler.PlayerID = Utilities.FindPlayerId();
            ClientDataHandler.WorldID = Utilities.FindWorldId();
        }

        public static async Task Reload()
        {
            if (!Conf.C.Reload)
            {
                ChatHelper.NewText("Reload is disabled, toggle it in config.");
                Log.Warn("Reload is disabled");
                // WorldGen.SaveAndQuit();
                // Main.menuMode = 0;
                return;
            }

            if (ModsToReload.modsToReload.Count == 0)
            {
                ChatHelper.NewText("No mods to reload. Add a mod by checking the box in the Mod Sources list.");
                Log.Warn("No mods to reload");
                return;
            }

            // 1 Clear logs if needed
            if (Conf.C.ClearClientLogOnReload)
                Log.ClearClientLog();

            // 2 Prepare client data
            ReloadUtilities.PrepareClient(ClientMode.SinglePlayer);

            // 3 Exit server or world
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                await ReloadUtilities.ExitWorldOrServer();
            }

            // 4 Reload
            ReloadUtilities.BuildAndReloadMods();
        }

        public static Task ExitWorldOrServer()
        {
            // TODO check if Conf is null
            if (Conf.C == null) // Assuming 'Instance' is a static property or field in 'Conf'
            {
                Log.Warn("Conf is null");
                return Task.CompletedTask;
            }

            if (Conf.C.SaveWorldOnReload)
            {
                Log.Warn("Saving and quitting...");

                // Creating task that will delay reloading a mod until world finish saving
                var tcs = new TaskCompletionSource();
                WorldGen.SaveAndQuit(tcs.SetResult);
                return tcs.Task;
            }
            else
            {
                Log.Warn("Just quitting...");
                WorldGen.JustQuit();
                return Task.CompletedTask;
            }

        }

        public static void ReloadMod()
        {
            // Going to reload mod menu(that automaticly invokes reload)
            Main.menuMode = 10002;
        }

        public static void BuildAndReloadMods(Action actionAfterBuild = null)
        {
            // 0. Check if we should reload mods
            if (Conf.C.Reload == false)
            {
                Log.Warn("Config.Reload is false, skipping mod reload.");
                return;
            }

            // 1. Getting Assembly
            Assembly tModLoaderAssembly = typeof(Main).Assembly;

            // 2. Getting method for finding modSources paths
            Type modCompileType = tModLoaderAssembly.GetType("Terraria.ModLoader.Core.ModCompile");
            MethodInfo findModSourcesMethod = modCompileType.GetMethod("FindModSources", BindingFlags.NonPublic | BindingFlags.Static);
            string[] modSources = (string[])findModSourcesMethod.Invoke(null, null);

            // Check if modSources is null or empty.
            if (modSources == null || modSources.Length == 0)
            {
                Log.Warn("No mod sources were found via reflection.");
                return;
            }

            // 3. Get all modPaths for future
            Log.Info("Executing Mods to reload: " + string.Join(", ", ModsToReload.modsToReload));

            var modPaths = ModsToReload.modsToReload.Select((modName) =>
                modSources.FirstOrDefault(p =>
                    !string.IsNullOrEmpty(p) &&
                    Directory.Exists(p) &&
                    Path.GetFileName(p)?.Equals(modName, StringComparison.InvariantCultureIgnoreCase) == true));

            // 4. Getting method for reloading a mod
            // 4.1 Getting UIBuildMod Instance
            Type interfaceType = tModLoaderAssembly.GetType("Terraria.ModLoader.UI.Interface");
            FieldInfo buildModField = interfaceType.GetField("buildMod", BindingFlags.NonPublic | BindingFlags.Static);
            object buildModInstance = buildModField?.GetValue(null);

            // 4.2 Getting correct BuildMod method of UIBuildMod
            Type uiBuildModType = tModLoaderAssembly.GetType("Terraria.ModLoader.UI.UIBuildMod");
            MethodInfo buildModMethod = uiBuildModType.GetMethod("BuildMod", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(Action<>).MakeGenericType(modCompileType), typeof(bool)]);

            // Check if it exist
            if (buildModMethod == null)
            {
                Log.Warn("No buildMethod were found via reflection.");
                return;
            }

            // 4.3 Getting correct Build method from ModCompile
            MethodInfo mcBuildModFolder = modCompileType.GetMethod("Build", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(string)]);

            // 5. Setting a hook on BuildMod method of UIBuildMod
            Hook buildModMethodHook = null;
            buildModMethodHook = new Hook(buildModMethod, (Func<object, Action<object>, bool, Task> orig, object self, Action<object> buildAction, bool reload) =>
            {
                Task origTask = orig(self, buildAction, reload); // Call original method correctly

                return origTask.ContinueWith(t =>
                {
                    actionAfterBuild?.Invoke(); // Execute custom action after the method finishes
                    buildModMethodHook?.Dispose(); // Disable hook
                });
            });

            Log.Info("Starting to build mods..." + string.Join(", ", modPaths));

            // 6. Creating a task
            Main.menuMode = 10003;
            Task.Run(() =>
            {
                if (buildModMethod == null)
                {
                    Log.Error("buildModMethod is null. Cannot proceed with building mods.");
                    return Task.CompletedTask;
                }
                if (buildModInstance == null)
                {
                    Log.Error("buildModInstance is null. Cannot proceed with building mods.");
                    return Task.CompletedTask;
                }
                if (modPaths == null || !modPaths.Any())
                {
                    Log.Error("No modPaths found. Cannot proceed with building mods.");
                    return Task.CompletedTask;
                }

                try
                {
                    return (Task)buildModMethod.Invoke(buildModInstance,
                    [
                        (Action<object>) (mc =>
                        {
                            foreach (var modPath in modPaths)
                            {
                                if (string.IsNullOrWhiteSpace(modPath))
                                {
                                    Log.Error("Encountered empty or null modPath. Skipping.");
                                    continue;
                                }
                                try
                                {
                                    mcBuildModFolder.Invoke(mc, [modPath]);
                                }
                                catch (Exception buildEx)
                                {
                                    Log.Error($"Failed to build mod at '{modPath}': {buildEx.Message}");
                                }
                            }
                        }),
                        true
                    ]);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to invoke buildModMethod: {ex.Message}");
                    return Task.CompletedTask;
                }
            });
        }
    }
}