using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BuildingPalette
{
    public class TagEditorUISystem : ModSystem
    {
        private UserInterface _ui;
        private TagEditorUI   _state;
        private GameTime      _lastGameTime;

        private static TagEditorUISystem Instance =>
            ModContent.GetInstance<TagEditorUISystem>();

        public override void Load()
        {
            if (Main.dedServ) return;
            _ui    = new UserInterface();
            _state = new TagEditorUI();
            _state.Activate();
        }

        public override void Unload()
        {
            _state = null;
            _ui    = null;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public static void Open(Item item, Vector2 anchor)
        {
            var inst = Instance;
            if (inst == null) return;

            if (inst._ui.CurrentState == inst._state)
            {
                Close();
                return;
            }

            TagEditorPlayer.SetItem(item);
            TagEditorState.SetItem(item);
            inst._state.SetItem(item, anchor);
            inst._ui.SetState(inst._state);
        }

        public static void Close()
        {
            var inst = Instance;
            if (inst == null) return;
            TagEditorPlayer.ClearItem();
            TagEditorState.Clear();
            inst._ui.SetState(null);
        }

        public static bool IsOpen =>
            Instance?._ui?.CurrentState != null;

        public static TagEditorUI GetState() =>
            Instance?._state;

        public static void RefreshChips(Item item) =>
            Instance?._state?.RefreshChips(item);

        // ── Mouse blocking ────────────────────────────────────────────────────
        // PreUpdateEntities runs AFTER UI updates (so GetDimensions() is valid)
        // but BEFORE player/inventory input processes clicks.
        // This is the correct place to set mouseInterface reliably.
        public override void PreUpdateEntities()
        {
            if (!IsOpen) return;
            if (_state == null) return;
            if (!Main.playerInventory) return; // inventory not open, nothing to block

            if (_state.IsMouseOver())
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.mouseLeft  = false;
                Main.mouseRight = false;
            }
        }



        public override void UpdateUI(GameTime gameTime)
        {
            _lastGameTime = gameTime;
            if (_ui?.CurrentState != null)
                _ui.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // After "Vanilla: Inventory" so it draws on top of it
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
            if (idx < 0) idx = layers.Count - 1;
            else idx++; // insert after, not before

            layers.Insert(idx, new LegacyGameInterfaceLayer(
                "BuildingPalette: Tag Editor",
                () =>
                {
                    if (_lastGameTime != null && _ui?.CurrentState != null)
                        _ui.Draw(Main.spriteBatch, _lastGameTime);
                    return true;
                },
                InterfaceScaleType.UI
            ));
        }
    }
}
