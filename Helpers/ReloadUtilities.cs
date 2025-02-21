﻿using SquidTestingMod.PacketHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria;
using SquidTestingMod.Common.Configs;
using System.Reflection;
using Terraria.GameContent.UI.Elements;
using static SquidTestingMod.Common.Configs.Config;
using Terraria.ModLoader.UI;

namespace SquidTestingMod.Helpers
{
    //All functions, related to reload
    internal class ReloadUtilities
    {
        public async static void PrepareMainMPClient()
        {

            ClientDataHandler.Mode = ClientMode.MPMain;
            ClientDataHandler.PlayerId = Utilities.FindPlayerId();
            ClientDataHandler.WriteData();

            ExitAndKillServer();

            if (Utilities.ReloadConfig.WaitingTimeBeforeNavigatingToModSources > 0)
                await Task.Delay(Utilities.ReloadConfig.WaitingTimeBeforeNavigatingToModSources);

            object modSourcesInstance = NavigateToDevelopMods();

            if (Utilities.ReloadConfig.InvokeBuildAndReload)
            {
                if (Utilities.ReloadConfig.WaitingTimeBeforeBuildAndReload > 0)
                    await Task.Delay(Utilities.ReloadConfig.WaitingTimeBeforeBuildAndReload);
                BuildAndReloadMod(modSourcesInstance);
            }
        }

        public async static void PrepareMinorMPClient()
        {
            ClientDataHandler.Mode = ClientMode.MPMinor;
            ClientDataHandler.PlayerId = Utilities.FindPlayerId();
            ClientDataHandler.WriteData();

            ExitWorldOrServer();

            if (Utilities.ReloadConfig.WaitingTimeBeforeNavigatingToModSources > 0)
                await Task.Delay(Utilities.ReloadConfig.WaitingTimeBeforeNavigatingToModSources);

            ReloadMod();
        }

        public async static void PrepareSPClient()
        {
            PrepareClient(ClientMode.SinglePlayer);

            ExitWorldOrServer();

            await ReloadOrBuildAndReloadAsync(true);
        }

        public static void PrepareClient(ClientMode clientMode)
        {
            ClientDataHandler.Mode = clientMode;
            ClientDataHandler.PlayerId = Utilities.FindPlayerId();
            ClientDataHandler.WorldId = Utilities.FindWorldId();
        }

        public static async Task ReloadOrBuildAndReloadAsync(bool shoudBeBuilded)
        {
            object modSourcesInstance = null;


            if (Utilities.ReloadConfig.WaitingTimeBeforeNavigatingToModSources > 0)
                await Task.Delay(Utilities.ReloadConfig.WaitingTimeBeforeNavigatingToModSources);

            if (shoudBeBuilded)
            {
                modSourcesInstance = NavigateToDevelopMods();
            }

            if (Utilities.ReloadConfig.InvokeBuildAndReload)
            {
                if (Utilities.ReloadConfig.WaitingTimeBeforeBuildAndReload > 0 && shoudBeBuilded)
                    await Task.Delay(Utilities.ReloadConfig.WaitingTimeBeforeBuildAndReload);
                if (shoudBeBuilded)
                {
                    BuildAndReloadMod(modSourcesInstance);
                }
                else
                {
                    ReloadMod();
                }

            }
        }



        public static void ExitWorldOrServer()
        {

            if (Utilities.ReloadConfig.SaveAndQuitWorldWithoutSaving)
            {
                Log.Warn("Just quitting...");
                WorldGen.JustQuit();
            }
            else
            {
                Log.Warn("Saving and quitting...");
                WorldGen.SaveAndQuit();
            }
        }

        public static void ExitAndKillServer()
        {
            ModNetHandler.RefreshServer.SendKillingServer(255, Main.myPlayer, Utilities.ReloadConfig.SaveAndQuitWorldWithoutSaving);
            WorldGen.SaveAndQuit();
        }

        private static void ReloadMod()
        {
            Main.menuMode = 10002;
        }

        private static void BuildAndReloadMod(object modSourcesInstance)
        {
            if (modSourcesInstance == null)
            {
                Log.Warn("modSourcesInstance is null.");
                return;
            }

            var itemsField = modSourcesInstance.GetType().GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance);
            if (itemsField == null)
            {
                Log.Warn("_items field not found.");
                return;
            }

            var items = (System.Collections.IEnumerable)itemsField.GetValue(modSourcesInstance);
            if (items == null)
            {
                Log.Warn("_items is null.");
                return;
            }

            object modSourceItem = null;
            string modNameFound = "";

            foreach (var item in items)
            {
                if (item.GetType().Name == "UIModSourceItem")
                {
                    // Extract and log the mod name
                    var modNameField = item.GetType().GetField("_modName", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (modNameField != null)
                    {
                        var modNameValue = modNameField.GetValue(item);
                        if (modNameValue is UIText uiText)
                        {
                            string modName = uiText.Text;
                            Log.Info($"Mod Name: {modName}");
                            Config c = ModContent.GetInstance<Config>();
                            if (modName == Utilities.ReloadConfig.ModToReload)
                            {
                                modSourceItem = item;
                                modNameFound = modName;
                                break;
                            }
                        }
                        else
                        {
                            Log.Warn("Mod name is not a UIText.");
                        }
                    }
                }
            }

            if (modSourceItem == null)
            {
                Log.Warn("Second UIModSourceItem not found.");
                return;
            }

            var method = modSourceItem.GetType().GetMethod("BuildAndReload", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                Log.Warn("BuildAndReload method not found.");
                return;
            }

            Log.Info($"Invoking BuildAndReload method with {modNameFound} UIModSourceItem...");
            method.Invoke(modSourceItem, [null, null]);
        }

        private static object NavigateToDevelopMods()
        {
            try
            {
                Log.Info("Attempting to navigate to Develop Mods...");

                Assembly tModLoaderAssembly = typeof(Main).Assembly;
                Type interfaceType = tModLoaderAssembly.GetType("Terraria.ModLoader.UI.Interface");

                FieldInfo modSourcesField = interfaceType.GetField("modSources", BindingFlags.NonPublic | BindingFlags.Static);
                object modSourcesInstance = modSourcesField?.GetValue(null);

                FieldInfo modSourcesIDField = interfaceType.GetField("modSourcesID", BindingFlags.NonPublic | BindingFlags.Static);
                int modSourcesID = (int)(modSourcesIDField?.GetValue(null) ?? -1);
                Log.Info("modSourcesID: " + modSourcesID);

                Main.menuMode = modSourcesID;

                Log.Info($"Successfully navigated to Develop Mods (MenuMode: {modSourcesID}).");

                return modSourcesInstance;
            }
            catch (Exception ex)
            {
                Log.Error($"Error navigating to Develop Mods: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }
    }
}
