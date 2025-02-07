using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace SquidTestingMod.UI
{
    // Instead of inheriting the vanilla behavior we override the drawing and click behavior.
    public class ItemSlot : UIItemSlot
    {
        // We store the item that should be shown in the browser.
        private int itemSlotContext;
        private Item displayItem;

        public ItemSlot(Item[] itemArray, int itemIndex, int itemSlotContext) : base(itemArray, itemIndex, itemSlotContext)
        {
            this.itemSlotContext = itemSlotContext;
            this.displayItem = itemArray[itemIndex].Clone();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (!displayItem.IsAir)
            {
                CalculatedStyle dimensions = GetInnerDimensions();
                Terraria.UI.ItemSlot.Draw(spriteBatch, ref displayItem, itemSlotContext, dimensions.Position(), Color.White);
            }

            // draw the hovering tooltip for each item slot
            if (IsMouseHovering)
            {
                Main.HoverItem = displayItem.Clone();

                CalculatedStyle dimensions = GetInnerDimensions();
                Texture2D overlay = TextureAssets.InventoryBack14.Value;
                spriteBatch.Draw(overlay, dimensions.Position(), null, Color.White * 0.5f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }
        }

        // When the user left-clicks, we want to give them a full-stack copy without removing the item from our browser.
        public override void LeftClick(UIMouseEvent evt)
        {
            if (Main.mouseItem.IsAir)
            {
                // Clone our display item and give the clone the max stack.
                Main.mouseItem = displayItem.Clone();
                Main.mouseItem.stack = displayItem.maxStack;
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        // (Optional) Override any additional drag or right-click handlers if you want to completely disable taking items.
    }
}
