using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace BuildingPalette
{
    /// <summary>
    /// Registers the "T" keybind and detects which item the player
    /// is hovering when it's pressed. Hands off to TagEditorUI.
    /// </summary>
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
            // Only act when the keybind is freshly pressed (not held)
            if (OpenTagEditor == null || !OpenTagEditor.JustPressed) return;

            // Don't fire during text input (e.g. chat, signs, etc.)
            if (Main.drawingPlayerChat || Main.editSign || Main.editChest) return;

            var hoveredItem = GetHoveredItem();
            if (hoveredItem == null || hoveredItem.IsAir) return;

            // Capture mouse position as the anchor point for the panel.
            // We use the mouse position rather than slot position because
            // tModLoader doesn't expose individual slot screen rects easily.
            // The panel will appear just to the right of the cursor.
            TagEditorUISystem.Open(hoveredItem, new Vector2(Main.mouseX, Main.mouseY));
        }

        // ── Hover Detection ──────────────────────────────────────────────────

        /// <summary>
        /// Finds the item currently under the cursor across all common
        /// inventory slots: hotbar, main inventory, armor, and chest/storage UI.
        /// Returns null if nothing is hovered.
        /// </summary>
        private static Item GetHoveredItem()
        {
            // tModLoader sets Main.HoverItem when the cursor is over an item slot.
            // It's always current-frame so this is the safest single source of truth.
            if (Main.HoverItem != null && !Main.HoverItem.IsAir)
                return Main.HoverItem;

            // Fallback: check if the mouse is over a slot in the player's inventory.
            // Main.mouseItem is the item being dragged; skip it.
            // ItemSlot.Context gives us more granular control if needed later.

            return null;
        }
    }
}
