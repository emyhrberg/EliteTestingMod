using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModHelper.Helpers;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace ModHelper.UI.Elements
{
    // Instead of inheriting the vanilla behavior we override the drawing and click behavior.
    public class CustomItemSlot : UIItemSlot
    {
        // We store the item that should be shown in the panel.
        private Item displayItem;
        private int _itemSlotContext;

        public CustomItemSlot(Item[] itemArray, int itemIndex, int itemSlotContext) : base(itemArray, itemIndex, itemSlotContext)
        {
            // set size
            Width.Set(44, 0f);
            Height.Set(44, 0f);

            displayItem = itemArray[itemIndex].Clone();
            _itemSlotContext = itemSlotContext;
        }

        public Item GetDisplayItem()
        {
            return displayItem;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Draw inventory background
            CalculatedStyle dimensions = GetInnerDimensions();
            float bgOpacity = IsMouseHovering ? 1.0f : 0.4f; // 0.9 when hovering, 0.4 when not
            Texture2D inventoryBack = TextureAssets.InventoryBack9.Value;
            spriteBatch.Draw(inventoryBack, dimensions.ToRectangle(), Color.White * bgOpacity);

            // Draw the item
            ItemSlot.DrawItemIcon(
                item: displayItem,
                context: _itemSlotContext,
                spriteBatch: spriteBatch,
                screenPositionForItemCenter: GetDimensions().Center(),
                scale: 1f,
                sizeLimit: 24f,
                environmentColor: Color.White
                );

            // draw the hovering tooltip for each item slot
            if (IsMouseHovering)
            {
                Main.HoverItem = displayItem.Clone();
                Main.hoverItemName = Main.HoverItem.Name;
            }
        }

        // When the user left-clicks, we want to give them a full-stack copy without removing the item from our panel.
        public override void LeftClick(UIMouseEvent evt)
        {
            // if dragging, do not perform any action
            MainSystem sys = ModContent.GetInstance<MainSystem>();
            if (sys.mainState.itemSpawnerPanel.IsDragging || sys.mainState.itemSpawnerPanel.GetActive() == false)
            {
                Log.Info("Dont spawn item, panel is hidden");
                return;
            }

            // force player inventory to open
            Main.playerInventory = true;

            // Clone our display item and give the clone the max stack.
            Main.mouseItem = displayItem.Clone();
            Main.mouseItem.stack = displayItem.maxStack;
        }

        public override void Update(GameTime gameTime)
        {
            // base.Update(gameTime);

            // Check if the mouse is over this UI element and the right mouse button is down.
            if (IsMouseHovering && Main.mouseRight)
            {
                // force open inventory (otherwise it wont work)
                Main.playerInventory = true;
                // Execute your "holding" logic here.
                // You might want to add a timer so it doesn't execute every frame.
                // Log.Info("Right mouse is being held down on " + displayItem.Name);
                // For example, increment the item stack gradually:
                if (Main.mouseItem.IsAir)
                {
                    Main.mouseItem = displayItem.Clone();
                    Main.mouseItem.stack = 1;
                }
                else if (Main.mouseItem.type == displayItem.type && Main.mouseItem.stack < displayItem.maxStack)
                {
                    Main.superFastStack = 1;
                    Main.mouseItem.stack++;
                }
            }
        }
    }
}
