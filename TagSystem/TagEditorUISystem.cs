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

        // ── Update + Draw ─────────────────────────────────────────────────────

        public override void UpdateUI(GameTime gameTime)
        {
            // Update is handled inside the blocking layer in ModifyInterfaceLayers
            // so it runs at exactly the right point in the input pipeline.
            _lastGameTime = gameTime;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
            if (idx < 0) idx = layers.Count - 1;

            // ── Early blocking layer (before inventory) ────────────────────────
            // We run _ui.Update() here first so our buttons see the click,
            // THEN we zero mouseLeft/mouseRight so vanilla inventory never sees it.
            layers.Insert(idx, new LegacyGameInterfaceLayer(
                "BuildingPalette: Tag Editor Block",
                () =>
                {
                    if (_ui?.CurrentState == null || _state == null) return true;
                    if (!_state.IsMouseOver()) return true;

                    // Let our UI handle the click first
                    if (_lastGameTime != null)
                        _ui.Update(_lastGameTime);

                    // Now block vanilla from seeing it
                    Main.LocalPlayer.mouseInterface = true;
                    Main.mouseLeft                  = false;
                    Main.mouseRight                 = false;
                    return true;
                },
                InterfaceScaleType.UI
            ));

            // ── Draw layer (after inventory) ───────────────────────────────────
            // Re-find inventory index since we just inserted before it
            int drawIdx = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
            if (drawIdx < 0) drawIdx = layers.Count - 1;
            else drawIdx++;

            layers.Insert(drawIdx, new LegacyGameInterfaceLayer(
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
