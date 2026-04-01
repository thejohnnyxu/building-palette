using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BuildingPalette
{
    public class TagManagerUISystem : ModSystem
    {
        private static readonly Item _emptyItem = new();

        private UserInterface  _ui;
        private TagManagerUI   _state;
        private GameTime       _lastGameTime;

        private static TagManagerUISystem Instance =>
            ModContent.GetInstance<TagManagerUISystem>();

        public static bool IsOpen => Instance?._ui?.CurrentState != null;

        public override void Load()
        {
            if (Main.dedServ) return;
            _ui    = new UserInterface();
            _state = new TagManagerUI();
            _state.Activate();
        }

        public override void Unload()
        {
            _state = null;
            _ui    = null;
        }

        public static void Open()
        {
            var inst = Instance;
            if (inst == null) return;
            inst._state.OnOpen();
            inst._ui.SetState(inst._state);
        }

        public static void OpenWithScanResults(System.Collections.Generic.List<(int itemId, string name)> results)
        {
            var inst = Instance;
            if (inst == null) return;
            inst._state.OpenScanResults(results);
            inst._ui.SetState(inst._state);
        }

        public static void Close()
        {
            var inst = Instance;
            if (inst == null) return;
            inst._ui.SetState(null);
        }

        public static void RefreshAll() => Instance?._state?.RefreshAll();

        public override void UpdateUI(GameTime gameTime)
        {
            _lastGameTime = gameTime;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
            if (idx < 0) idx = layers.Count - 1;

            // Blocking layer — before inventory
            layers.Insert(idx, new LegacyGameInterfaceLayer(
                "BuildingPalette: Tag Manager Block",
                () =>
                {
                    if (_ui?.CurrentState == null || _state == null) return true;

                    bool mouseOver = _state.IsMouseOver();

                    // Focus management
                    _state.HandleFocusClick(mouseOver, Main.mouseLeft);

                    if (!mouseOver) return true;

                    // Run UI update so buttons fire
                    if (_lastGameTime != null)
                        _ui.Update(_lastGameTime);

                    // Block vanilla input
                    Main.LocalPlayer.mouseInterface = true;
                    Main.mouseLeft                  = false;
                    Main.mouseRight                 = false;

                    // Suppress tooltips
                    Main.hoverItemName = null;
                    Main.HoverItem     = _emptyItem;
                    Main.mouseText     = false;

                    return true;
                },
                InterfaceScaleType.UI
            ));

            // Draw layer — after inventory
            int drawIdx = layers.FindIndex(l => l.Name == "Vanilla: Inventory");
            if (drawIdx < 0) drawIdx = layers.Count - 1;
            else drawIdx++;

            layers.Insert(drawIdx, new LegacyGameInterfaceLayer(
                "BuildingPalette: Tag Manager",
                () =>
                {
                    if (_lastGameTime == null || _ui?.CurrentState == null)
                        return true;

                    // Handle keyboard input here — after draw, before game reads it
                    _state.HandleInput();

                    _ui.Draw(Main.spriteBatch, _lastGameTime);
                    return true;
                },
                InterfaceScaleType.UI
            ));
        }
    }
}
