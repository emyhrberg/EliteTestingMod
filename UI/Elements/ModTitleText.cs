using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModHelper.Common.Configs;
using ModHelper.Helpers;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.UI;
using Terraria.UI;

namespace ModHelper.UI.Elements
{
    public class ModTitleText : UIText
    {
        public string hover = "";
        private readonly Action leftClick;
        private readonly Action rightClick;
        private string internalModName = "";
        private bool isConfigOpen = false;
        public readonly bool clickToOpenConfig;

        public ModTitleText(string text, string hover = "", Action leftClick = null, Action rightClick = null, float textSize = 1f, string internalModName = "", bool clickToOpenConfig = false) : base(text, textSize)
        {
            this.hover = hover;
            this.internalModName = internalModName;
            this.clickToOpenConfig = clickToOpenConfig;
            Left.Set(0, 0);
            VAlign = 0.5f;
            this.leftClick = leftClick;
            this.rightClick = rightClick;
        }

        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);

            rightClick?.Invoke();
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            // Log.Info("LeftClick on text: " + internalModName);
            leftClick?.Invoke();
            base.LeftClick(evt);

            if (clickToOpenConfig)
            {
                if (isConfigOpen)
                {
                    hover = $"Open {internalModName} config";
                    ChatHelper.NewText($"Closing {internalModName} config");
                    Main.menuMode = 0;
                    Main.InGameUI.SetState(null);
                    isConfigOpen = false;

                    // Expand the hotbar
                    MainSystem sys = ModContent.GetInstance<MainSystem>();
                    if (sys != null)
                    {
                        sys.mainState.collapse.SetCollapsed(false);
                    }

                    return;
                }

                // Temp try to open config
                try
                {
                    // Use reflection to get the private ConfigManager.Configs property.
                    FieldInfo configsProp = typeof(ConfigManager).GetField("Configs", BindingFlags.Static | BindingFlags.NonPublic);
                    var configs = configsProp.GetValue(null) as IDictionary<Mod, List<ModConfig>>;

                    // Get the mod name from the modPath.
                    // string modName = Path.GetFileName(modPath);
                    string modName = internalModName;
                    Mod modInstance = ModLoader.GetMod(modName);
                    if (modInstance == null)
                    {
                        ChatHelper.NewText($"Mod '{modName}' not found.", Color.Red);
                        return;
                    }

                    // Check if there are any configs for this mod.
                    if (!configs.TryGetValue(modInstance, out List<ModConfig> modConfigs) || modConfigs.Count == 0)
                    {
                        ChatHelper.NewText("No config available for mod: " + modName, Color.Yellow);
                        return;
                    }

                    // Use the first available config.
                    ModConfig config = modConfigs[0];

                    // Open the config UI.
                    // Use reflection to set the mod and config for the modConfig UI.
                    Assembly assembly = typeof(Main).Assembly;
                    Type interfaceType = assembly.GetType("Terraria.ModLoader.UI.Interface");
                    var modConfigField = interfaceType.GetField("modConfig", BindingFlags.Static | BindingFlags.NonPublic);
                    var modConfigInstance = modConfigField.GetValue(null);
                    var setModMethod = modConfigInstance.GetType().GetMethod("SetMod", BindingFlags.Instance | BindingFlags.NonPublic);

                    // Invoke the SetMod method to set the mod and config for the modConfig UI.
                    setModMethod.Invoke(modConfigInstance, [modInstance, config, false, null, null, true]);

                    // Open the mod config UI.
                    Main.InGameUI.SetState(modConfigInstance as UIState);
                    Main.menuMode = 10024;
                    ChatHelper.NewText($"Opening {internalModName} config");

                    // Hover text update
                    hover = $"Close {internalModName} config";

                    // Collapse the hotbar
                    isConfigOpen = true;

                    Main.playerInventory = false;
                    MainSystem sys = ModContent.GetInstance<MainSystem>();
                    sys?.mainState?.collapse?.SetCollapsed(true);
                    sys.mainState.AreButtonsShowing = false;
                    sys.mainState.collapse.UpdateCollapseImage();
                }
                catch (Exception ex)
                {
                    ChatHelper.NewText($"No config found for mod '{internalModName}'. : {ex.Message}", Color.Red);
                    return;
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Only check for manual closure if we think config is still open
            if (isConfigOpen)
            {
                bool configClosed = false;

                // Log.SlowInfo("MenuMode: " + Main.menuMode, seconds: 1);

                // Check if Main.menuMode has changed from the config mode
                if (Main.menuMode != 10024)
                {
                    configClosed = true;
                }
                // Double-check with the actual UI state
                else if (Main.InGameUI != null)
                {
                    try
                    {
                        var currentStateProp = Main.InGameUI.GetType().GetProperty("CurrentState", BindingFlags.Public | BindingFlags.Instance);
                        if (currentStateProp != null)
                        {
                            var currentState = currentStateProp.GetValue(Main.InGameUI);

                            // If UI state is null or not a config UI
                            if (currentState == null)
                            {
                                configClosed = true;
                            }
                            else
                            {
                                // Get the type of the mod config UI for comparison
                                Assembly assembly = typeof(Main).Assembly;
                                Type interfaceType = assembly.GetType("Terraria.ModLoader.UI.Interface");
                                var modConfigField = interfaceType?.GetField("modConfig", BindingFlags.Static | BindingFlags.NonPublic);

                                if (modConfigField != null)
                                {
                                    var modConfigInstance = modConfigField.GetValue(null);

                                    // If current state is not the mod config UI
                                    if (modConfigInstance != null && currentState.GetType() != modConfigInstance.GetType())
                                    {
                                        configClosed = true;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error checking UI state in Update: " + ex.Message);
                    }
                }

                // If we detected the config was closed manually
                if (configClosed)
                {
                    hover = $"Open {internalModName} config";
                    isConfigOpen = false;

                    // Restore hotbar state since user manually closed config
                    MainSystem sys = ModContent.GetInstance<MainSystem>();
                    if (sys?.mainState != null)
                    {
                        sys.mainState.AreButtonsShowing = true;
                        sys.mainState.collapse?.SetCollapsed(false);
                        sys.mainState.collapse?.UpdateCollapseImage();
                    }

                    Log.SlowInfo($"Detected manual config closure for {internalModName}", seconds: 1);
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (!string.IsNullOrEmpty(hover) && IsMouseHovering)
            {
                UICommon.TooltipMouseText(hover);
            }
        }
    }
}