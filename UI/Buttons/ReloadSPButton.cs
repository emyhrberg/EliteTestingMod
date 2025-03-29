using Microsoft.Xna.Framework.Graphics;
using ModHelper.Common.Configs;
using ModHelper.Helpers;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace ModHelper.UI.Buttons
{
    public class ReloadSPButton(Asset<Texture2D> spritesheet, string buttonText, string hoverText, string hoverTextDescription) : BaseButton(spritesheet, buttonText, hoverText, hoverTextDescription)
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
            await ReloadUtilities.Reload();
        }
    }
}