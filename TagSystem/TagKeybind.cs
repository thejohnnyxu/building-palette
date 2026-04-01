using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace BuildingPalette
{
    public class TagKeybind : ModSystem
    {
        public static ModKeybind OpenTagEditor { get; private set; }
        public static ModKeybind OpenScanMode  { get; private set; }

        public override void Load()
        {
            OpenTagEditor = KeybindLoader.RegisterKeybind(Mod, "Open Tag Editor", "T");
            OpenScanMode  = KeybindLoader.RegisterKeybind(Mod, "Start Area Scan", "OemOpenBrackets");
        }

        public override void Unload()
        {
            OpenTagEditor = null;
            OpenScanMode  = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.drawingPlayerChat || Main.editSign || Main.editChest) return;

            // ── Scan mode ─────────────────────────────────────────────────────
            if (OpenScanMode?.JustPressed == true)
            {
                if (TileScan.State != TileScan.ScanState.Idle)
                    TileScan.Cancel();
                else
                    TileScan.Begin();
                return;
            }

            if (OpenTagEditor == null || !OpenTagEditor.JustPressed) return;

            var anchor = new Vector2(Main.mouseX, Main.mouseY);

            // ── Item hovered (inventory open — works in Magic Storage too) ─────
            // Main.HoverItem is set by tModLoader's ItemSlot for every rendered
            // slot, including Magic Storage's UI grid. No special API needed.
            var hoveredItem = Main.HoverItem;
            if (hoveredItem != null && !hoveredItem.IsAir)
            {
                TagEditorUISystem.Open(hoveredItem, anchor);
                return;
            }

            // ── World interactions (inventory closed only) ─────────────────────
            if (Main.playerInventory) return;

            int tx = Player.tileTargetX;
            int ty = Player.tileTargetY;

            // Chest tile → bulk tag mode
            if (IsChestTile(tx, ty))
            {
                int chestIdx = Chest.FindChest(tx, ty);
                if (chestIdx < 0)
                    for (int dx = 0; dx <= 1 && chestIdx < 0; dx++)
                        for (int dy = 0; dy <= 1 && chestIdx < 0; dy++)
                            chestIdx = Chest.FindChest(tx - dx, ty - dy);

                if (chestIdx >= 0)
                {
                    TagEditorUISystem.OpenBulk(chestIdx, anchor);
                    return;
                }
            }
        }

        private static bool IsChestTile(int tx, int ty)
        {
            if (!WorldGen.InWorld(tx, ty)) return false;
            var tile = Main.tile[tx, ty];
            return tile.HasTile && tile.TileType == Terraria.ID.TileID.Containers;
        }
    }
}
