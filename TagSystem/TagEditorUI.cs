using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace BuildingPalette
{
    /// <summary>
    /// Pure visual UIState — no input handling whatsoever.
    /// All input is handled by TagEditorPlayer (ModPlayer hooks).
    /// </summary>
    public class TagEditorUI : UIState
    {
        private const int PanelW     = 322;
        private const int PanelH     = 350;
        private const int Pad        = 14;
        private const int ChipH      = 24;
        private const int ChipGap    = 5;
        private const int ChipsAreaH = 110;
        private const int InputTop   = 70 + 110 + 10;

        private UIPanel _root;
        private UIText  _titleText;
        private UIPanel _chipsArea;
        private UIText  _inputDisplay;

        // Called once at load
        public override void OnInitialize()
        {
            _root = new UIPanel();
            _root.Width.Set(PanelW, 0f);
            _root.Height.Set(PanelH, 0f);
            _root.BackgroundColor = new Color(42, 58, 92, 255);
            _root.BorderColor     = new Color(74, 106, 156, 255);
            Append(_root);

            var header = new UIPanel();
            header.Width.Set(PanelW - 8, 0f);
            header.Height.Set(40, 0f);
            header.Left.Set(0, 0f);
            header.Top.Set(0, 0f);
            header.BackgroundColor = new Color(30, 45, 74, 255);
            header.BorderColor     = new Color(74, 106, 156, 255);
            _root.Append(header);

            _titleText = new UIText("", 0.85f);
            _titleText.Left.Set(10, 0f);
            _titleText.Top.Set(10, 0f);
            header.Append(_titleText);

            var tagsLabel = new UIText("TAGS", 0.7f);
            tagsLabel.Left.Set(Pad, 0f);
            tagsLabel.Top.Set(52, 0f);
            _root.Append(tagsLabel);

            _chipsArea = new UIPanel();
            _chipsArea.Left.Set(Pad, 0f);
            _chipsArea.Top.Set(70, 0f);
            _chipsArea.Width.Set(PanelW - Pad * 2, 0f);
            _chipsArea.Height.Set(ChipsAreaH, 0f);
            _chipsArea.BackgroundColor = new Color(26, 36, 56, 255);
            _chipsArea.BorderColor     = new Color(58, 90, 140, 255);
            _root.Append(_chipsArea);

            // Input box — purely decorative, updated via SetInputText()
            var inputBox = new UIPanel();
            inputBox.Left.Set(Pad, 0f);
            inputBox.Top.Set(InputTop, 0f);
            inputBox.Width.Set(PanelW - Pad * 2 - 80, 0f);
            inputBox.Height.Set(30, 0f);
            inputBox.BackgroundColor = new Color(26, 36, 56, 255);
            inputBox.BorderColor     = new Color(58, 90, 140, 255);
            _root.Append(inputBox);

            _inputDisplay = new UIText("", 0.75f);
            _inputDisplay.Left.Set(6, 0f);
            _inputDisplay.Top.Set(6, 0f);
            inputBox.Append(_inputDisplay);

            // Add button — wired to TagEditorPlayer.CommitTag
            var addBtn = new UITextPanel<string>("Add", 0.75f);
            addBtn.Left.Set(PanelW - Pad - 72, 0f);
            addBtn.Top.Set(InputTop, 0f);
            addBtn.Width.Set(72, 0f);
            addBtn.Height.Set(30, 0f);
            addBtn.BackgroundColor = new Color(42, 74, 124, 255);
            addBtn.BorderColor     = new Color(74, 122, 204, 255);
            addBtn.OnLeftClick    += (_, _) => TagEditorPlayer.CommitTag();
            _root.Append(addBtn);

            // Cancel button
            var cancelBtn = new UITextPanel<string>("Cancel", 0.75f);
            cancelBtn.Left.Set(PanelW - Pad - 80, 0f);
            cancelBtn.Top.Set(InputTop + 40, 0f);
            cancelBtn.Width.Set(80, 0f);
            cancelBtn.Height.Set(28, 0f);
            cancelBtn.BackgroundColor = new Color(58, 42, 42, 255);
            cancelBtn.BorderColor     = new Color(122, 74, 74, 255);
            cancelBtn.OnLeftClick    += (_, _) => TagEditorUISystem.Close();
            _root.Append(cancelBtn);

            var hint = new UIText("T / Esc to close", 0.65f);
            hint.Left.Set(Pad, 0f);
            hint.Top.Set(InputTop + 46, 0f);
            _root.Append(hint);
        }

        // ── Called by TagEditorUISystem.Open ──────────────────────────────────

        public void SetItem(Item item, Vector2 anchor)
        {
            _titleText.SetText(item.Name);

            float x = anchor.X + 18;
            float y = anchor.Y - 10;
            if (x + PanelW > Main.screenWidth)  x = anchor.X - PanelW - 10;
            if (y + PanelH > Main.screenHeight) y = Main.screenHeight - PanelH - 10;
            if (x < 0) x = 8;
            if (y < 0) y = 8;
            _root.Left.Set(x, 0f);
            _root.Top.Set(y, 0f);

            RefreshChips(item);
            SetInputText("");
            Recalculate();
        }

        // ── Called by TagEditorPlayer each frame ──────────────────────────────

        public void RefreshChips(Item item)
        {
            _chipsArea.RemoveAllChildren();
            var tags = TagSystem.GetTags(item.type);
            int cx = 6, cy = 6;

            foreach (var tagName in tags)
            {
                string captured = tagName;
                float chipW = tagName.Length * 7f + 30f;

                if (cx + chipW > PanelW - Pad * 2 - 12 && cx > 6)
                {
                    cx  = 6;
                    cy += ChipH + ChipGap;
                }

                var chip = new UITextPanel<string>(tagName + " ×", 0.7f);
                chip.Left.Set(cx, 0f);
                chip.Top.Set(cy, 0f);
                chip.Width.Set(chipW, 0f);
                chip.Height.Set(ChipH, 0f);
                chip.BackgroundColor = new Color(42, 74, 124, 255);
                chip.BorderColor     = new Color(74, 122, 204, 255);
                chip.OnLeftClick    += (_, _) =>
                {
                    TagSystem.RemoveTag(item.type, captured);
                    RefreshChips(item);
                    _chipsArea.Recalculate();
                };

                _chipsArea.Append(chip);
                cx += (int)chipW + ChipGap;
            }

            if (!TagSystem.HasAnyTags(item.type))
            {
                var empty = new UIText("no tags yet", 0.75f);
                empty.Left.Set(6, 0f);
                empty.Top.Set(8, 0f);
                _chipsArea.Append(empty);
            }

            _chipsArea.Recalculate();
        }

        public void SetInputText(string text)
        {
            _inputDisplay?.SetText(string.IsNullOrEmpty(text) ? "new tag..." : text);
        }

        public bool IsMouseOver()
        {
            if (_root == null) return false;
            var r = _root.GetDimensions().ToRectangle();
            return r.Contains(Main.mouseX, Main.mouseY);
        }
    }
}
