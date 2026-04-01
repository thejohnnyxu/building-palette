using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace BuildingPalette
{
    /// <summary>
    /// Owns all input for the tag editor.
    /// PreUpdate runs before vanilla inventory, item use, and UI layers —
    /// the only reliable place to block mouse passthrough and capture text.
    /// </summary>
    public class TagEditorPlayer : ModPlayer
    {
        // Shared input buffer — UIState reads this to display it
        private static string _input = "";
        private static Item   _item  = null;

        public static void SetItem(Item item)  => _item = item;
        public static void ClearItem()         => _item = null;
        public static bool IsActive            => _item != null && TagEditorUISystem.IsOpen;

        public static void CommitTag()
        {
            if (_item == null) return;
            var normalized = TagSystem.Normalize(_input);
            if (string.IsNullOrWhiteSpace(normalized)) return;
            TagSystem.AddTag(_item.type, normalized);
            _input = "";
            TagEditorUISystem.RefreshChips(_item);
        }

        public override void PreUpdate()
        {
            if (!IsActive) return;

            // ── Mouse blocking ────────────────────────────────────────────────
            // PreUpdate is before ALL UI layers and inventory processing.
            // Setting mouseInterface here reliably blocks item pickup,
            // tile placement, and inventory interaction.
            var ui = TagEditorUISystem.GetState();
            if (ui != null && ui.IsMouseOver())
                Player.mouseInterface = true;

            // ── Text input ────────────────────────────────────────────────────
            // WritingText + HandleIME enables GetInputText to receive characters.
            PlayerInput.WritingText = true;
            Main.instance.HandleIME();

            string updated = Main.GetInputText(_input);
            if (updated != _input)
            {
                if (updated.Contains('\r') || updated.Contains('\n'))
                    CommitTag();
                else
                {
                    _input = updated;
                    TagEditorUISystem.GetState()?.SetInputText(_input);
                }
            }

            // ── Escape closes ─────────────────────────────────────────────────
            if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
                TagEditorUISystem.Close();
        }
    }
}
