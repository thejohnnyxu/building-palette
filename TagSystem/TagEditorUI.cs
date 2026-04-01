using System.Collections.Generic;
using System.Linq;
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
        private const int PanelW       = 300;
        private const int Pad          = 12;
        private const int ChipH        = 22;
        private const int ChipGap      = 4;
        private const int ChipsAreaH   = 100;
        private const int HeaderH      = 40;
        private const int BtnW         = 68;
        private const int BtnH         = 28;
        private const int InputTop     = HeaderH + Pad + 18 + ChipsAreaH + Pad;
        private const int PanelH       = InputTop + BtnH + Pad + BtnH + Pad;
        private const int SuggH        = 24; // height of each autocomplete row
        private const int MaxSugg      = 3;

        private UIPanel    _root;
        private UIText     _titleText;
        private UIPanel    _iconHeader;
        private UIItemIcon _itemIcon;
        private UIPanel    _chipsArea;
        private UIPanel    _inputBox;    // stored so we can update border on focus change
        private UIText     _inputDisplay;
        private UIPanel    _autocompletePanel; // dropdown

        // Track current item for chip refresh
        private Item _currentItem;
        private int  _currentChest = -1;

        // ── OnInitialize ──────────────────────────────────────────────────────

        public override void OnInitialize()
        {
            _root = new UIPanel();
            _root.Width.Set(PanelW, 0f);
            _root.Height.Set(PanelH, 0f);
            _root.BackgroundColor = new Color(42, 58, 92, 255);
            _root.BorderColor     = new Color(74, 106, 156, 255);
            _root.SetPadding(0);
            Append(_root);

            // Header
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

            // Tags label
            var tagsLabel = new UIText("TAGS", 0.65f);
            tagsLabel.Left.Set(Pad, 0f);
            tagsLabel.Top.Set(HeaderH + Pad, 0f);
            _root.Append(tagsLabel);

            // Chips area
            _chipsArea = new UIPanel();
            _chipsArea.Left.Set(Pad, 0f);
            _chipsArea.Top.Set(HeaderH + Pad + 18, 0f);
            _chipsArea.Width.Set(PanelW - Pad * 2, 0f);
            _chipsArea.Height.Set(ChipsAreaH, 0f);
            _chipsArea.BackgroundColor = new Color(20, 28, 48, 255);
            _chipsArea.BorderColor     = new Color(50, 80, 130, 255);
            _chipsArea.SetPadding(0);
            _root.Append(_chipsArea);

            // Input box
            _inputBox = new UIPanel();
            _inputBox.Left.Set(Pad, 0f);
            _inputBox.Top.Set(InputTop, 0f);
            _inputBox.Width.Set(PanelW - Pad * 2 - BtnW - Pad, 0f);
            _inputBox.Height.Set(BtnH, 0f);
            _inputBox.BackgroundColor = new Color(20, 28, 48, 255);
            _inputBox.BorderColor     = new Color(50, 80, 130, 255);
            _inputBox.SetPadding(0);
            _root.Append(_inputBox);

            _inputDisplay = new UIText("", 0.72f);
            _inputDisplay.Left.Set(8, 0f);
            _inputDisplay.Top.Set(7, 0f);
            _inputBox.Append(_inputDisplay);

            // Add button
            var addBtn = new UITextPanel<string>("Add", 0.72f);
            addBtn.Left.Set(PanelW - Pad - BtnW, 0f);
            addBtn.Top.Set(InputTop, 0f);
            addBtn.Width.Set(BtnW, 0f);
            addBtn.Height.Set(BtnH, 0f);
            addBtn.BackgroundColor = new Color(38, 68, 118, 255);
            addBtn.BorderColor     = new Color(74, 122, 204, 255);
            addBtn.OnLeftClick    += (_, _) => TagEditorPlayer.CommitTag();
            _root.Append(addBtn);

            // Hint + cancel
            var hint = new UIText("Esc to close", 0.6f);
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

            // Autocomplete dropdown — starts hidden, sits below the input box
            _autocompletePanel = new UIPanel();
            _autocompletePanel.Left.Set(Pad, 0f);
            _autocompletePanel.Top.Set(InputTop + BtnH, 0f);
            _autocompletePanel.Width.Set(PanelW - Pad * 2 - BtnW - Pad, 0f);
            _autocompletePanel.Height.Set(0, 0f);
            _autocompletePanel.BackgroundColor = Color.Transparent;
            _autocompletePanel.BorderColor     = Color.Transparent;
            _autocompletePanel.SetPadding(0);
            _root.Append(_autocompletePanel);
        }

        // ── Open (single item) ────────────────────────────────────────────────

        public void SetItem(Item item, Vector2 anchor)
        {
            _currentItem  = item;
            _currentChest = -1;

            _titleText.SetText(item.Name);
            SwapIcon(item);
            PositionPanel(anchor);
            RefreshChips(item);
            SetInputText("|");
            ClearAutocomplete();
            Recalculate();
        }

        // ── Open (bulk chest) ─────────────────────────────────────────────────

        public void SetBulk(int chestIdx, Vector2 anchor)
        {
            _currentItem  = null;
            _currentChest = chestIdx;

            var chest = Main.chest[chestIdx];
            int itemCount = chest.item.Count(i => i != null && !i.IsAir);

            // Show chest icon (use a default chest item)
            var chestItem = new Item();
            chestItem.SetDefaults(Terraria.ID.ItemID.Chest);
            SwapIcon(chestItem);

            _titleText.SetText($"Chest ({itemCount} items)");
            PositionPanel(anchor);
            RefreshBulkChips(chestIdx);
            SetInputText("|");
            ClearAutocomplete();
            Recalculate();
        }

        // ── Chips ─────────────────────────────────────────────────────────────

        public void RefreshChips(Item item)
        {
            _chipsArea.RemoveAllChildren();
            var tags = TagSystem.GetTags(item.type);
            BuildChips(tags, t =>
            {
                TagSystem.RemoveTag(item.type, t);
                RefreshChips(item);
                _chipsArea.Recalculate();
            });
        }

        public void RefreshBulkChips(int chestIdx)
        {
            _chipsArea.RemoveAllChildren();
            if (chestIdx < 0 || chestIdx >= Main.chest.Length) return;

            // Show the union of all tags across all items in the chest
            var allTags = new HashSet<string>();
            foreach (var item in Main.chest[chestIdx].item)
            {
                if (item == null || item.IsAir) continue;
                foreach (var t in TagSystem.GetTags(item.type))
                    allTags.Add(t);
            }

            BuildChips(allTags, t =>
            {
                // Remove this tag from every item in the chest
                foreach (var item in Main.chest[chestIdx].item)
                {
                    if (item == null || item.IsAir) continue;
                    TagSystem.RemoveTag(item.type, t);
                }
                RefreshBulkChips(chestIdx);
                _chipsArea.Recalculate();
            });
        }

        private void BuildChips(IEnumerable<string> tags, System.Action<string> onRemove)
        {
            int cx = 5, cy = 5;
            bool any = false;

            foreach (var tagName in tags)
            {
                any = true;
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
                chip.OnLeftClick    += (_, _) => onRemove(captured);
                _chipsArea.Append(chip);
                cx += (int)chipW + ChipGap;
            }

            if (!any)
            {
                var empty = new UIText("no tags yet", 0.72f);
                empty.Left.Set(5, 0f);
                empty.Top.Set(6, 0f);
                _chipsArea.Append(empty);
            }

            _chipsArea.Recalculate();
        }

        // ── Autocomplete ──────────────────────────────────────────────────────

        public void UpdateAutocomplete(string input)
        {
            _autocompletePanel.RemoveAllChildren();

            if (string.IsNullOrWhiteSpace(input))
            {
                _autocompletePanel.Height.Set(0, 0f);
                _autocompletePanel.Recalculate();
                return;
            }

            string norm = TagSystem.Normalize(input);
            var suggestions = TagSystem.GetAllTags()
                .SelectMany(kv => kv.Value)
                .Distinct()
                .Where(t => t.StartsWith(norm) && t != norm)
                .OrderBy(t => t)
                .Take(MaxSugg)
                .ToList();

            if (suggestions.Count == 0)
            {
                _autocompletePanel.Height.Set(0, 0f);
                _autocompletePanel.BackgroundColor = Color.Transparent;
                _autocompletePanel.BorderColor     = Color.Transparent;
                _autocompletePanel.Recalculate();
                return;
            }

            int totalH = suggestions.Count * SuggH;
            _autocompletePanel.Height.Set(totalH, 0f);
            _autocompletePanel.Top.Set(InputTop + BtnH, 0f); // below the input box
            _autocompletePanel.BackgroundColor = new Color(15, 22, 40, 255);
            _autocompletePanel.BorderColor     = new Color(74, 122, 204, 255);

            for (int i = 0; i < suggestions.Count; i++)
            {
                string s = suggestions[i];
                var row = new UITextPanel<string>(s, 0.68f);
                row.Left.Set(0, 0f);
                row.Top.Set(i * SuggH, 0f);
                row.Width.Set(0, 1f);
                row.Height.Set(SuggH, 0f);
                row.BackgroundColor = new Color(25, 38, 65, 255);
                row.BorderColor     = new Color(50, 80, 130, 0); // no border between rows
                row.SetPadding(0);
                row.OnLeftClick += (_, _) => TagEditorPlayer.FillAutocomplete(s);
                _autocompletePanel.Append(row);
            }

            _autocompletePanel.Recalculate();
        }

        private void ClearAutocomplete()
        {
            _autocompletePanel.RemoveAllChildren();
            _autocompletePanel.Height.Set(0, 0f);
            _autocompletePanel.BackgroundColor = Color.Transparent;
            _autocompletePanel.BorderColor     = Color.Transparent;
            _autocompletePanel.Recalculate();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        public void SetInputText(string display)
        {
            _inputDisplay?.SetText(display);

            if (_inputBox == null) return;
            if (TagEditorPlayer.IsFocused)
            {
                // Bright blue border + slightly lighter bg when focused
                _inputBox.BorderColor     = new Color(100, 160, 255, 255);
                _inputBox.BackgroundColor = new Color(28, 40, 70, 255);
            }
            else
            {
                // Dim border when unfocused
                _inputBox.BorderColor     = new Color(50, 80, 130, 255);
                _inputBox.BackgroundColor = new Color(20, 28, 48, 255);
            }
        }

        public bool IsMouseOver()
        {
            if (_root == null) return false;
            var r = _root.GetDimensions().ToRectangle();
            return r.Contains(Main.mouseX, Main.mouseY);
        }

        private void SwapIcon(Item item)
        {
            _iconHeader.RemoveChild(_itemIcon);
            _itemIcon = new UIItemIcon(item, false);
            _itemIcon.Left.Set(Pad, 0f);
            _itemIcon.Top.Set((HeaderH - 26) / 2, 0f);
            _itemIcon.Width.Set(26, 0f);
            _itemIcon.Height.Set(26, 0f);
            _iconHeader.Append(_itemIcon);
            _iconHeader.Recalculate();
        }

        private void PositionPanel(Vector2 anchor)
        {
            float x = anchor.X + 18;
            float y = anchor.Y - 10;
            if (x + PanelW > Main.screenWidth)  x = anchor.X - PanelW - 10;
            if (y + PanelH > Main.screenHeight) y = Main.screenHeight - PanelH - 10;
            if (x < 0) x = 8;
            if (y < 0) y = 8;
            _root.Left.Set(x, 0f);
            _root.Top.Set(y, 0f);
        }
    }
}
