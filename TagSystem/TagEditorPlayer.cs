using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace BuildingPalette
{
    public class TagEditorPlayer : ModPlayer
    {
        private static string _input   = "";
        private static Item   _item    = null;
        private static int    _tick    = 0;   // counts up each frame
        private const  int    BlinkRate = 30; // frames per cursor toggle

        public static void SetItem(Item item)  => _item = item;
        public static void ClearItem()         { _item = null; _input = ""; _tick = 0; }
        public static bool IsActive            => _item != null && TagEditorUISystem.IsOpen;

        public static void CommitTag()
        {
            if (_item == null) return;
            var normalized = TagSystem.Normalize(_input);
            if (string.IsNullOrWhiteSpace(normalized)) return;
            TagSystem.AddTag(_item.type, normalized);
            _input = "";
            _tick  = 0;
            TagEditorUISystem.RefreshChips(_item);
            UpdateDisplay();
        }

        public override void PreUpdate()
        {
            if (!IsActive) return;

            PlayerInput.WritingText = true;
            Main.instance.HandleIME();

            string updated = Main.GetInputText(_input);
            if (updated != _input)
            {
                if (updated.Contains('\r') || updated.Contains('\n'))
                {
                    CommitTag();
                    return;
                }
                _input = updated;
                _tick  = 0; // reset blink on keypress so cursor is always visible while typing
            }

            // Advance blink timer and refresh display every frame
            _tick++;
            UpdateDisplay();

            if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape))
                TagEditorUISystem.Close();
        }

        private static void UpdateDisplay()
        {
            bool cursorOn = (_tick / BlinkRate) % 2 == 0;
            string cursor = cursorOn ? "|" : " ";

            string display = string.IsNullOrEmpty(_input)
                ? cursor                    // just the cursor when empty
                : _input + cursor;

            TagEditorUISystem.GetState()?.SetInputText(display);
        }
    }
}
