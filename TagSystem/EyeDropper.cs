using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BuildingPalette
{
    /// <summary>
    /// Eye dropper — press F to pick the block/item under the cursor.
    ///
    /// Two modes:
    ///   Inventory open  → picks the hovered item slot directly (works in
    ///                      vanilla inventory, chests, and Magic Storage)
    ///   Inventory closed → identifies the tile under the cursor and selects
    ///                      the matching item from your inventory/hotbar
    /// </summary>
    public class EyeDropper : ModSystem
    {
        public static ModKeybind PickTile { get; private set; }

        public override void Load()
        {
            PickTile = KeybindLoader.RegisterKeybind(Mod, "Eye Dropper (Pick Tile)", "F");
        }

        public override void Unload()
        {
            PickTile = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (PickTile == null || !PickTile.JustPressed) return;
            if (Main.drawingPlayerChat || Main.editSign || Main.editChest) return;
            if (Main.LocalPlayer == null) return;
            if (TagEditorUISystem.IsOpen || TagManagerUISystem.IsOpen) return;

            // ── Inventory open: pick hovered item slot ────────────────────────
            // Works in vanilla inventory, open chests, and Magic Storage.
            // Main.HoverItem is set by ItemSlot for every rendered slot.
            if (Main.playerInventory)
            {
                var hovered = Main.HoverItem;
                if (hovered == null || hovered.IsAir)
                {
                    Main.NewText("Hover an item to pick it.", Color.Gray);
                    return;
                }

                // Find this item type in the hotbar and select it
                var player = Main.LocalPlayer;
                for (int i = 0; i < 10; i++)
                {
                    if (player.inventory[i].type == hovered.type)
                    {
                        player.selectedItem = i;
                        Main.NewText($"[i:{hovered.type}] {hovered.Name}", Color.LightYellow);
                        return;
                    }
                }

                // Not in hotbar — try full inventory and move to hotbar
                int inventorySlot = -1;
                for (int i = 10; i < 50; i++)
                {
                    if (player.inventory[i].type == hovered.type)
                    {
                        inventorySlot = i;
                        break;
                    }
                }

                if (inventorySlot < 0)
                {
                    Main.NewText($"[i:{hovered.type}] {hovered.Name} — not in inventory.", Color.Gray);
                    return;
                }

                MoveToHotbar(player, inventorySlot);
                Main.NewText($"[i:{hovered.type}] {player.inventory[player.selectedItem].Name}", Color.LightYellow);
                return;
            }

            // ── Inventory closed: identify tile under cursor ───────────────────
            int tx = Player.tileTargetX;
            int ty = Player.tileTargetY;

            if (!WorldGen.InWorld(tx, ty)) return;

            var tile = Main.tile[tx, ty];
            if (tile == null) return;

            // Try foreground tile first (style-aware), then wall
            int itemId = 0;

            if (tile.HasTile)
                itemId = TileUtils.TileToItemAtCoords(tx, ty);

            if (itemId <= 0 && tile.WallType > 0)
                itemId = TileUtils.WallToItem(tile.WallType);

            if (itemId <= 0)
            {
                Main.NewText("No placeable item for that tile.", Color.Gray);
                return;
            }

            var p = Main.LocalPlayer;

            // Check hotbar first
            for (int i = 0; i < 10; i++)
            {
                if (p.inventory[i].type == itemId)
                {
                    p.selectedItem = i;
                    Main.NewText($"[i:{itemId}] {p.inventory[i].Name}", Color.LightYellow);
                    return;
                }
            }

            // Search full inventory
            int slot = -1;
            for (int i = 10; i < 50; i++)
            {
                if (p.inventory[i].type == itemId)
                {
                    slot = i;
                    break;
                }
            }

            if (slot < 0)
            {
                string name = ContentSamples.ItemsByType.TryGetValue(itemId, out var s)
                    ? s.Name : $"Item #{itemId}";
                Main.NewText($"[i:{itemId}] {name} — not in inventory.", Color.Gray);
                return;
            }

            MoveToHotbar(p, slot);
            Main.NewText($"[i:{itemId}] {p.inventory[p.selectedItem].Name}", Color.LightYellow);
        }

        private static void MoveToHotbar(Player player, int inventorySlot)
        {
            // Find a free hotbar slot, or use the currently selected one
            int freeSlot = -1;
            for (int i = 0; i < 10; i++)
            {
                if (player.inventory[i].IsAir) { freeSlot = i; break; }
            }

            int targetSlot = freeSlot >= 0 ? freeSlot : player.selectedItem;
            var tmp = player.inventory[targetSlot];
            player.inventory[targetSlot]   = player.inventory[inventorySlot];
            player.inventory[inventorySlot] = tmp;
            player.selectedItem = targetSlot;
        }
    }
}
