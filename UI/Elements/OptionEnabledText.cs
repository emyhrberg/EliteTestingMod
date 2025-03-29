using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModHelper.Helpers;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.UI;
using static ModHelper.UI.Elements.OptionElement;

namespace ModHelper.UI.Elements
{
    public class OptionEnabledText : UIText
    {
        private Color red = new(226, 57, 39);

        private State state;

        public OptionEnabledText(string text) : base(text)
        {
            // text and size and position
            TextColor = red;

            // Position: Centered vertically, 65 pixels from the right
            VAlign = 0.5f;
            float def = -65f;
            Left.Set(def, 1f);
        }

        public void SetTextState(State state)
        {
            this.state = state;
            TextColor = state == State.Enabled ? Color.Green : red;
            SetText(state == State.Enabled ? "Enabled" : "Disabled");
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (IsMouseHovering)
            {
                if (state == State.Enabled)
                {
                    UICommon.TooltipMouseText("Click to disable");
                }
                else
                {
                    UICommon.TooltipMouseText("Click to enable");
                }
            }
        }
    }
}