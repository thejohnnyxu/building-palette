using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;

namespace BuildingPalette
{
    public class TagEditorUI : UIState
    {
        private const int PanelW     = 300;
        private const int Pad        = 12;
        private const int ChipH      = 22;
        private const int ChipGap    = 4;
        private const int ChipsAreaH = 100;
        private const int HeaderH    = 40;
        private const int BtnW       = 68;
        private const int BtnH       = 28;
        private const int InputTop   = HeaderH + Pad + 18 + ChipsAreaH + Pad; // chips bottom + gap
        private const int PanelH     = InputTop + BtnH + Pad + BtnH + Pad;    // input + cancel + margins

        private UIPanel    _root;
        private UIText     _titleText;
        private UIPanel    _iconHeader;  // need ref to swap the icon
        private UIItemIcon _itemIcon;
        private UIPanel    _chipsArea;
        private UIText     _inputDisplay;

        public override void OnInitialize()
        {
            _root = new UIPanel();
            _root.Width.Set(PanelW, 0f);
            _root.Height.Set(PanelH, 0f);
            _root.BackgroundColor = new Color(42, 58, 92, 255);
            _root.BorderColor     = new Color(74, 106, 156, 255);
            _root.SetPadding(0);
            Append(_root);

            // ── Header — full width, no offset ────────────────────────────────
            _iconHeader = new UIPanel();
            _iconHeader.Width.Set(PanelW, 0f);
            _iconHeader.Height.Set(HeaderH, 0f);
            _iconHeader.Left.Set(0, 0f);
            _iconHeader.Top.Set(0, 0f);
            _iconHeader.BackgroundColor = new Color(22, 34, 58, 255);
            _iconHeader.BorderColor     = new Color(74, 106, 156, 255);
            _iconHeader.SetPadding(0);
            _root.Append(_iconHeader);

            _itemIcon = new UIItemIcon(new Item(), false);
            _itemIcon.Left.Set(Pad, 0f);
            _itemIcon.Top.Set((HeaderH - 26) / 2, 0f);
            _itemIcon.Width.Set(26, 0f);
            _itemIcon.Height.Set(26, 0f);
            _iconHeader.Append(_itemIcon);

            _titleText = new UIText("", 0.85f);
            _titleText.Left.Set(Pad + 26 + 8, 0f);
            _titleText.Top.Set((HeaderH - 16) / 2, 0f);
            _iconHeader.Append(_titleText);

            // ── Tags label ────────────────────────────────────────────────────
            var tagsLabel = new UIText("TAGS", 0.65f);
            tagsLabel.Left.Set(Pad, 0f);
            tagsLabel.Top.Set(HeaderH + Pad, 0f);
            _root.Append(tagsLabel);

            // ── Chips area ────────────────────────────────────────────────────
            _chipsArea = new UIPanel();
            _chipsArea.Left.Set(Pad, 0f);
            _chipsArea.Top.Set(HeaderH + Pad + 18, 0f);
            _chipsArea.Width.Set(PanelW - Pad * 2, 0f);
            _chipsArea.Height.Set(ChipsAreaH, 0f);
            _chipsArea.BackgroundColor = new Color(20, 28, 48, 255);
            _chipsArea.BorderColor     = new Color(50, 80, 130, 255);
            _chipsArea.SetPadding(0);
            _root.Append(_chipsArea);

            // ── Input row ─────────────────────────────────────────────────────
            var inputBox = new UIPanel();
            inputBox.Left.Set(Pad, 0f);
            inputBox.Top.Set(InputTop, 0f);
            inputBox.Width.Set(PanelW - Pad * 2 - BtnW - Pad, 0f);
            inputBox.Height.Set(BtnH, 0f);
            inputBox.BackgroundColor = new Color(20, 28, 48, 255);
            inputBox.BorderColor     = new Color(50, 80, 130, 255);
            inputBox.SetPadding(0);
            _root.Append(inputBox);

            // UIText inside input box — vertically centred manually
            _inputDisplay = new UIText("", 0.72f);
            _inputDisplay.Left.Set(8, 0f);
            _inputDisplay.Top.Set(7, 0f);
            inputBox.Append(_inputDisplay);

            var addBtn = new UITextPanel<string>("Add", 0.72f);
            addBtn.Left.Set(PanelW - Pad - BtnW, 0f);
            addBtn.Top.Set(InputTop, 0f);
            addBtn.Width.Set(BtnW, 0f);
            addBtn.Height.Set(BtnH, 0f);
            addBtn.BackgroundColor = new Color(38, 68, 118, 255);
            addBtn.BorderColor     = new Color(74, 122, 204, 255);
            addBtn.OnLeftClick    += (_, _) => TagEditorPlayer.CommitTag();
            _root.Append(addBtn);

            // ── Bottom row: hint left, cancel right ───────────────────────────
            var hint = new UIText("T / Esc to close", 0.6f);
            hint.Left.Set(Pad, 0f);
            hint.Top.Set(InputTop + BtnH + Pad, 0f);
            _root.Append(hint);

            var cancelBtn = new UITextPanel<string>("Cancel", 0.72f);
            cancelBtn.Left.Set(PanelW - Pad - BtnW, 0f);
            cancelBtn.Top.Set(InputTop + BtnH + Pad - 2, 0f);
            cancelBtn.Width.Set(BtnW, 0f);
            cancelBtn.Height.Set(BtnH, 0f);
            cancelBtn.BackgroundColor = new Color(68, 28, 28, 255);
            cancelBtn.BorderColor     = new Color(130, 60, 60, 255);
            cancelBtn.OnLeftClick    += (_, _) => TagEditorUISystem.Close();
            _root.Append(cancelBtn);
        }

                public void SetItem(Item item, Vector2 anchor)
        {
            _titleText.SetText(item.Name);

            // UIItemIcon has no SetItem — swap it out entirely
            _iconHeader.RemoveChild(_itemIcon);
            _itemIcon = new UIItemIcon(item, false);
            _itemIcon.Left.Set(Pad, 0f);
            _itemIcon.Top.Set((HeaderH - 26) / 2, 0f);
            _itemIcon.Width.Set(26, 0f);
            _itemIcon.Height.Set(26, 0f);
            _iconHeader.Append(_itemIcon);
            _iconHeader.Recalculate();

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

        public void RefreshChips(Item item)
        {
            _chipsArea.RemoveAllChildren();
            var tags = TagSystem.GetTags(item.type);
            int cx = 5, cy = 5;

            foreach (var tagName in tags)
            {
                string captured = tagName;
                float chipW = tagName.Length * 6.5f + 26f;

                if (cx + chipW > PanelW - Pad * 2 - 10 && cx > 5)
                {
                    cx  = 5;
                    cy += ChipH + ChipGap;
                }

                var chip = new UITextPanel<string>(tagName + " ×", 0.68f);
                chip.Left.Set(cx, 0f);
                chip.Top.Set(cy, 0f);
                chip.Width.Set(chipW, 0f);
                chip.Height.Set(ChipH, 0f);
                chip.BackgroundColor = new Color(38, 68, 118, 255);
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
                var empty = new UIText("no tags yet", 0.72f);
                empty.Left.Set(5, 0f);
                empty.Top.Set(6, 0f);
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
