using EliteTestingMod.Common.Configs;
using EliteTestingMod.Helpers;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace EliteTestingMod.UI.Buttons
{
    public class ReloadSPButton(Asset<Texture2D> spritesheet, string buttonText, string hoverText, float textSize) : BaseButton(spritesheet, buttonText, hoverText, textSize)
    {
        // Set custom animation dimensions
        private float _scale = 0.8f;
        protected override int FrameCount => 5;
        protected override int FrameSpeed => 12;
        protected override int FrameWidth => 65;
        protected override int FrameHeight => 65;
        protected override float Scale => _scale;

        public async override void LeftClick(UIMouseEvent evt)
        {
            if (!Conf.Reload)
            {
                if (Conf.LogToChat) Main.NewText("Reload is disabled, toggle it in config.");
                Log.Warn("Reload is disabled");
                // WorldGen.SaveAndQuit();
                // Main.menuMode = 0;
                return;
            }

            if (ModsToReload.modsToReload.Count == 0)
            {
                if (Conf.LogToChat) Main.NewText("No mods to reload, add some in Mods.");
                Log.Warn("No mods to reload");
                return;
            }

            // 1 Clear logs if needed
            if (Conf.ClearClientLogOnReload)
                Log.ClearClientLog();

            // 2 Prepare client data
            ReloadUtilities.PrepareClient(ClientModes.SinglePlayer);

            // 3 Exit server or world
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                await ReloadUtilities.ExitWorldOrServer();
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                await ReloadUtilities.ExitAndKillServer();
            }

            // 4 Reload
            ReloadUtilities.BuildAndReloadMods();
        }

        public override void RightClick(UIMouseEvent evt)
        {
            // If we are in multiplayer, we cant right click
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // If right click, toggle the mode and return
            Active = false;
            ButtonText.Active = false;

            // set MP active
            MainSystem sys = ModContent.GetInstance<MainSystem>();
            foreach (var btn in sys?.mainState?.AllButtons)
            {
                if (btn is ReloadMPButton spBtn)
                {
                    spBtn.Active = true;
                    spBtn.ButtonText.Active = true;
                }
            }
            return;
        }
    }
}