using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;

namespace BuildingPalette
{
    public class TagEditorPlayer : ModPlayer
    {
        // ── State ─────────────────────────────────────────────────────────────
        private static string _input    = "";
        private static Item   _item     = null;   // null in bulk mode
        private static int    _chestIdx = -1;     // -1 in single mode
        private static int    _tick     = 0;
        private const  int    BlinkRate = 30;

        public static bool IsActive   => TagEditorUISystem.IsOpen;
        public static bool IsBulkMode => _chestIdx >= 0;

        // ── Open/close ────────────────────────────────────────────────────────

        // ── Focus ─────────────────────────────────────────────────────────────
        // Focus is gained by clicking inside the panel, lost by clicking outside.
        // Keyboard input (Enter, Esc, typing) only fires when focused.
        private static bool _focused = false;

        public static void SetFocused(bool focused) => _focused = focused;
        public static bool IsFocused => _focused;

        public static void SetItem(Item item)
        {
            _item     = item;
            _chestIdx = -1;
            _input    = "";
            _tick     = 0;
            _focused  = true; // auto-focus on open
        }

        public static void SetBulk(int chestIdx)
        {
            _item     = null;
            _chestIdx = chestIdx;
            _input    = "";
            _tick     = 0;
            _focused  = true;
        }

        public static void ClearItem()
        {
            _item     = null;
            _chestIdx = -1;
            _input    = "";
            _tick     = 0;
            _focused  = false;
        }

        // ── Commit ────────────────────────────────────────────────────────────

        public static void CommitTag()
        {
            var normalized = TagSystem.Normalize(_input);
            if (string.IsNullOrWhiteSpace(normalized)) return;

            if (IsBulkMode)
                CommitBulkTag(normalized);
            else if (_item != null)
                CommitSingleTag(normalized);

            _input = "";
            _tick  = 0;
            UpdateDisplay();
        }

        private static void CommitSingleTag(string tag)
        {
            TagSystem.AddTag(_item.type, tag);
            TagEditorUISystem.RefreshChips(_item);
        }

        private static void CommitBulkTag(string tag)
        {
            if (_chestIdx < 0 || _chestIdx >= Main.chest.Length) return;
            var chest = Main.chest[_chestIdx];
            if (chest == null) return;

            int count = 0;
            foreach (var item in chest.item)
            {
                if (item == null || item.IsAir) continue;
                TagSystem.AddTag(item.type, tag);
                count++;
            }

            Main.NewText($"Tagged {count} items with: {tag}",
                Microsoft.Xna.Framework.Color.LightGreen);
            TagEditorUISystem.RefreshBulkChips(_chestIdx);
        }

        // ── Autocomplete input fill ───────────────────────────────────────────

        public static void FillAutocomplete(string tag)
        {
            _input = tag;
            _tick  = 0;
            UpdateDisplay();
            TagEditorUISystem.GetState()?.UpdateAutocomplete(_input);
        }

        // ── PreUpdate ────────────────────────────────────────────────────────

        // Track previous mouseLeft state for edge detection in scan mode
        private static bool _prevMouseLeft = false;

        public override void PreUpdate()
        {
            bool mouseJustPressed = Main.mouseLeft && !_prevMouseLeft;
            _prevMouseLeft = Main.mouseLeft;

            // ── Scan mode click detection ──────────────────────────────────────
            if (TileScan.State != TileScan.ScanState.Idle
            && !Main.playerInventory
            && mouseJustPressed
            && !Main.LocalPlayer.mouseInterface)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.mouseLeft = false;
                TileScan.HandleClick(Player.tileTargetX, Player.tileTargetY);
                return;
            }

            if (!IsActive) return;

            // Note: focus gain/loss is handled in TagEditorUISystem's blocking
            // layer, where Main.mouseLeft is still true before we zero it.

            if (_focused)
            {
                // Suppress chat from opening on Enter
                Main.chatRelease = false;

                // Capture keyboard exclusively while focused
                PlayerInput.WritingText = true;
                Main.instance.HandleIME();

                bool enterPressed = Main.keyState.IsKeyDown(Keys.Enter)
                                 && !Main.oldKeyState.IsKeyDown(Keys.Enter);
                if (enterPressed)
                {
                    CommitTag();
                    return;
                }

                string updated = Main.GetInputText(_input);
                if (updated != _input)
                {
                    if (updated.Contains('\r') || updated.Contains('\n'))
                    {
                        CommitTag();
                        return;
                    }
                    _input = updated;
                    _tick  = 0;
                    TagEditorUISystem.GetState()?.UpdateAutocomplete(_input);
                }

                // Escape only closes when focused
                if (Main.keyState.IsKeyDown(Keys.Escape)
                && !Main.oldKeyState.IsKeyDown(Keys.Escape))
                {
                    TagEditorUISystem.Close();
                    return;
                }
            }

            _tick++;
            UpdateDisplay();
        }

        // ── Display ──────────────────────────────────────────────────────────

        private static void UpdateDisplay()
        {
            string display;
            if (!_focused)
            {
                // Show input without blinking cursor when unfocused
                display = string.IsNullOrEmpty(_input) ? "" : _input;
            }
            else
            {
                bool cursorOn = (_tick / BlinkRate) % 2 == 0;
                string cursor = cursorOn ? "|" : " ";
                display = string.IsNullOrEmpty(_input) ? cursor : _input + cursor;
            }
            TagEditorUISystem.GetState()?.SetInputText(display);
        }
    }
}
