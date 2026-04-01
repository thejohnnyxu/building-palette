using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace BuildingPalette
{
    public class TagKeybind : ModSystem
    {
        public static ModKeybind OpenTagEditor { get; private set; }

        public override void Load()
        {
            OpenTagEditor = KeybindLoader.RegisterKeybind(Mod, "Open Tag Editor", "T");
        }

        public override void Unload()
        {
            OpenTagEditor = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (OpenTagEditor == null || !OpenTagEditor.JustPressed) return;
            if (Main.drawingPlayerChat || Main.editSign || Main.editChest) return;

            var anchor = new Vector2(Main.mouseX, Main.mouseY);

            // ── Chest tile hover → bulk tag mode ──────────────────────────────
            // Check if the mouse is over a chest tile in the world.
            // Player.tileTargetX/Y is the tile the cursor is pointing at.
            int tx = Player.tileTargetX;
            int ty = Player.tileTargetY;
            if (!Main.playerInventory && IsChestTile(tx, ty))
            {
                int chestIdx = Chest.FindChest(tx, ty);
                // FindChest needs top-left corner — scan nearby if needed
                if (chestIdx < 0)
                {
                    // Try the four possible top-left origins for a 2x2 chest
                    for (int dx = 0; dx <= 1 && chestIdx < 0; dx++)
                        for (int dy = 0; dy <= 1 && chestIdx < 0; dy++)
                            chestIdx = Chest.FindChest(tx - dx, ty - dy);
                }

                if (chestIdx >= 0)
                {
                    TagEditorUISystem.OpenBulk(chestIdx, anchor);
                    return;
                }
            }

            // ── Item hover → single item mode ─────────────────────────────────
            var hoveredItem = GetHoveredItem();
            if (hoveredItem == null || hoveredItem.IsAir) return;
            TagEditorUISystem.Open(hoveredItem, anchor);
        }

        private static bool IsChestTile(int tx, int ty)
        {
            if (!WorldGen.InWorld(tx, ty)) return false;
            var tile = Main.tile[tx, ty];
            return tile.HasTile && tile.TileType == Terraria.ID.TileID.Containers;
        }

        private static Item GetHoveredItem()
        {
            if (Main.HoverItem != null && !Main.HoverItem.IsAir)
                return Main.HoverItem;
            return null;
        }
    }
}
