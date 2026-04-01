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
            inst._state.SetItem(item, anchor);
            inst._ui.SetState(inst._state);
        }

        public static void Close()
        {
            var inst = Instance;
            if (inst == null) return;
            TagEditorPlayer.ClearItem();
            inst._ui.SetState(null);
        }

        public static bool IsOpen =>
            Instance?._ui?.CurrentState != null;

        public static TagEditorUI GetState() =>
            Instance?._state;

        public static void RefreshChips(Item item) =>
            Instance?._state?.RefreshChips(item);

        // ── Update + Draw ─────────────────────────────────────────────────────

        public override void UpdateUI(GameTime gameTime)
        {
            _lastGameTime = gameTime;
            if (_ui?.CurrentState != null)
                _ui.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // Before "Vanilla: Inventory" per ExampleMod guidance
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
            if (idx < 0) idx = layers.Count - 1;

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
