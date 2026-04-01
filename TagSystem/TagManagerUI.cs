using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;

namespace BuildingPalette
{
    public class TagManagerUI : UIState
    {
        // ── Layout constants ──────────────────────────────────────────────────
        private const int W           = 620;
        private const int H           = 480;
        private const int Pad         = 12;
        private const int HeaderH     = 40;
        private const int TagColW     = 160;
        private const int DivX        = TagColW + Pad * 2;
        private const int RightW      = W - DivX - Pad;
        private const int RowH        = 28;
        private const int SearchH     = 30;
        private const int MaxResults  = 10;

        // ── Elements ──────────────────────────────────────────────────────────
        private UIPanel _root;
        private UIPanel _tagList;
        private UIPanel _itemList;
        private UIPanel _searchBox;
        private UIText  _searchDisplay;
        private UIText  _selectedTagLabel;
        private UIPanel _searchResults;

        // ── State ─────────────────────────────────────────────────────────────
        private string _selectedTag  = null;
        private string _searchInput  = "";
        private bool   _focused      = false;
        private int    _tick         = 0;
        private const int BlinkRate  = 30;

        // Scan results mode
        private List<(int itemId, string name)> _scanResults = new();
        private HashSet<int> _scanSelected = new();   // itemIds checked for tagging
        private string _scanTagInput = "";
        private bool   _scanTagFocused = false;
        private UIPanel _scanPanel;
        private UIPanel _scanItemList;
        private UIPanel _scanTagBox;
        private UIText  _scanTagDisplay;

        public static bool IsFocused => _instance?._focused == true
                                     || _instance?._scanTagFocused == true;

        private static TagManagerUI _instance;

        // ── Build once ────────────────────────────────────────────────────────

        public override void OnInitialize()
        {
            _instance = this;

            _root = new UIPanel();
            _root.Width.Set(W, 0f);
            _root.Height.Set(H, 0f);
            _root.HAlign = 0.5f;
            _root.VAlign = 0.5f;
            _root.BackgroundColor = new Color(30, 42, 68, 255);
            _root.BorderColor     = new Color(74, 106, 156, 255);
            _root.SetPadding(0);
            Append(_root);

            // ── Header ────────────────────────────────────────────────────────
            var header = new UIPanel();
            header.Width.Set(W, 0f);
            header.Height.Set(HeaderH, 0f);
            header.BackgroundColor = new Color(20, 30, 52, 255);
            header.BorderColor     = new Color(74, 106, 156, 255);
            header.SetPadding(0);
            _root.Append(header);

            var title = new UIText("Tag Manager", 0.9f);
            title.Left.Set(Pad, 0f);
            title.Top.Set(11, 0f);
            header.Append(title);

            var closeBtn = new UITextPanel<string>("✕", 0.8f);
            closeBtn.Width.Set(32, 0f);
            closeBtn.Height.Set(26, 0f);
            closeBtn.Left.Set(W - 44, 0f);
            closeBtn.Top.Set(7, 0f);
            closeBtn.BackgroundColor = new Color(68, 28, 28, 255);
            closeBtn.BorderColor     = new Color(130, 60, 60, 255);
            closeBtn.SetPadding(0);
            closeBtn.OnLeftClick += (_, _) => TagManagerUISystem.Close();
            header.Append(closeBtn);

            // Export button — exports selected tag to chat
            var exportBtn = new UITextPanel<string>("Export", 0.65f);
            exportBtn.Width.Set(60, 0f);
            exportBtn.Height.Set(26, 0f);
            exportBtn.Left.Set(W - 44 - 8 - 60, 0f);
            exportBtn.Top.Set(7, 0f);
            exportBtn.BackgroundColor = new Color(28, 55, 38, 255);
            exportBtn.BorderColor     = new Color(60, 120, 80, 255);
            exportBtn.SetPadding(0);
            exportBtn.OnLeftClick += (_, _) =>
            {
                if (_selectedTag == null)
                {
                    Main.NewText("Select a tag first.", Microsoft.Xna.Framework.Color.Orange);
                    return;
                }
                string exported = TagImportExport.Export(_selectedTag);
                Main.NewText("Exported — copy from chat:", Microsoft.Xna.Framework.Color.Cyan);
                Main.NewText(exported, Microsoft.Xna.Framework.Color.White);
            };
            header.Append(exportBtn);

            // Import button — opens an import prompt in chat
            var importBtn = new UITextPanel<string>("Import", 0.65f);
            importBtn.Width.Set(60, 0f);
            importBtn.Height.Set(26, 0f);
            importBtn.Left.Set(W - 44 - 8 - 60 - 8 - 60, 0f);
            importBtn.Top.Set(7, 0f);
            importBtn.BackgroundColor = new Color(28, 40, 70, 255);
            importBtn.BorderColor     = new Color(74, 106, 156, 255);
            importBtn.SetPadding(0);
            importBtn.OnLeftClick += (_, _) =>
            {
                TagManagerUISystem.Close();
                Main.NewText("Type: /importtag tagname:Item One,Item Two", Microsoft.Xna.Framework.Color.Cyan);
                Main.OpenPlayerChat();
            };
            header.Append(importBtn);

            // ── Left: Tags column label ───────────────────────────────────────
            var tagsLabel = new UIText("TAGS", 0.65f);
            tagsLabel.Left.Set(Pad, 0f);
            tagsLabel.Top.Set(HeaderH + Pad, 0f);
            _root.Append(tagsLabel);

            // ── Left: Tag list panel ──────────────────────────────────────────
            _tagList = new UIPanel();
            _tagList.Left.Set(Pad, 0f);
            _tagList.Top.Set(HeaderH + Pad + 18, 0f);
            _tagList.Width.Set(TagColW, 0f);
            _tagList.Height.Set(H - HeaderH - Pad - 18 - Pad, 0f);
            _tagList.BackgroundColor = new Color(20, 28, 48, 255);
            _tagList.BorderColor     = new Color(50, 80, 130, 255);
            _tagList.SetPadding(0);
            _root.Append(_tagList);

            // ── Right: selected tag label ─────────────────────────────────────
            _selectedTagLabel = new UIText("Select a tag", 0.75f);
            _selectedTagLabel.Left.Set(DivX + Pad, 0f);
            _selectedTagLabel.Top.Set(HeaderH + Pad, 0f);
            _root.Append(_selectedTagLabel);

            // ── Right: item list panel ────────────────────────────────────────
            int itemListH = (H - HeaderH - Pad - 18 - Pad) / 2 - Pad;
            _itemList = new UIPanel();
            _itemList.Left.Set(DivX, 0f);
            _itemList.Top.Set(HeaderH + Pad + 18, 0f);
            _itemList.Width.Set(RightW, 0f);
            _itemList.Height.Set(itemListH, 0f);
            _itemList.BackgroundColor = new Color(20, 28, 48, 255);
            _itemList.BorderColor     = new Color(50, 80, 130, 255);
            _itemList.SetPadding(0);
            _root.Append(_itemList);

            // ── Right: search label ───────────────────────────────────────────
            int searchTop = HeaderH + Pad + 18 + itemListH + Pad;
            var searchLabel = new UIText("ADD ITEMS", 0.65f);
            searchLabel.Left.Set(DivX + Pad, 0f);
            searchLabel.Top.Set(searchTop, 0f);
            _root.Append(searchLabel);

            // ── Right: search box ─────────────────────────────────────────────
            int searchBoxTop = searchTop + 18;
            _searchBox = new UIPanel();
            _searchBox.Left.Set(DivX, 0f);
            _searchBox.Top.Set(searchBoxTop, 0f);
            _searchBox.Width.Set(RightW, 0f);
            _searchBox.Height.Set(SearchH, 0f);
            _searchBox.BackgroundColor = new Color(20, 28, 48, 255);
            _searchBox.BorderColor     = new Color(50, 80, 130, 255);
            _searchBox.SetPadding(0);
            _searchBox.OnLeftClick += (_, _) => _focused = true;
            _root.Append(_searchBox);

            _searchDisplay = new UIText("", 0.72f);
            _searchDisplay.Left.Set(8, 0f);
            _searchDisplay.Top.Set(7, 0f);
            _searchBox.Append(_searchDisplay);

            // ── Right: search results panel ───────────────────────────────────
            _searchResults = new UIPanel();
            _searchResults.Left.Set(DivX, 0f);
            _searchResults.Top.Set(searchBoxTop + SearchH + Pad, 0f);
            _searchResults.Width.Set(RightW, 0f);
            _searchResults.Height.Set(H - searchBoxTop - SearchH - Pad * 2, 0f);
            _searchResults.BackgroundColor = new Color(20, 28, 48, 255);
            _searchResults.BorderColor     = new Color(50, 80, 130, 255);
            _searchResults.SetPadding(0);
            _root.Append(_searchResults);

            // ── Scan results overlay ──────────────────────────────────────────
            // Dimensions are set explicitly in OpenScanResults, not here,
            // because percentage sizing resolves incorrectly before first append.
            _scanPanel = new UIPanel();
            _scanPanel.BackgroundColor = new Color(22, 32, 54, 255);
            _scanPanel.BorderColor     = new Color(74, 106, 156, 0);
            _scanPanel.SetPadding(0);
            // Not appended yet — added in OpenScanResults
        }

        // ── Open (normal) ─────────────────────────────────────────────────────

        public void OnOpen()
        {
            _instance        = this;
            _selectedTag     = null;
            _searchInput     = "";
            _focused         = false;
            _tick            = 0;

            // Hide scan panel if it was showing
            _root.RemoveChild(_scanPanel);

            TagItemIndex.EnsureBuilt();
            RefreshTagList();
            RefreshItemList();
            RefreshSearchResults();
            UpdateSearchDisplay();
            Recalculate();
        }

        // ── Open (scan results) ───────────────────────────────────────────────

        public void OpenScanResults(List<(int itemId, string name)> results)
        {
            _instance        = this;
            _scanResults     = results;
            _scanSelected    = new HashSet<int>(results.Select(r => r.itemId)); // all selected by default
            _scanTagInput    = "";
            _scanTagFocused  = false;
            _focused         = false;
            _tick            = 0;

            // Set explicit pixel dimensions now that we know the parent size
            _scanPanel.Left.Set(0, 0f);
            _scanPanel.Top.Set(HeaderH, 0f);
            _scanPanel.Width.Set(W, 0f);
            _scanPanel.Height.Set(H - HeaderH, 0f);

            BuildScanPanel();
            _root.Append(_scanPanel);
            Recalculate();
        }

        private void BuildScanPanel()
        {
            _scanPanel.RemoveAllChildren();

            // Title
            var title = new UIText($"Scan Results — {_scanResults.Count} unique block(s)", 0.8f);
            title.Left.Set(Pad, 0f);
            title.Top.Set(Pad, 0f);
            _scanPanel.Append(title);

            // Back button
            var backBtn = new UITextPanel<string>("← Back", 0.68f);
            backBtn.Width.Set(80, 0f);
            backBtn.Height.Set(26, 0f);
            backBtn.Left.Set(W - Pad - 80, 0f);
            backBtn.Top.Set(Pad, 0f);
            backBtn.BackgroundColor = new Color(40, 55, 85, 255);
            backBtn.BorderColor     = new Color(74, 106, 156, 255);
            backBtn.PaddingTop = 4;
            backBtn.PaddingLeft = 6;
            backBtn.OnLeftClick += (_, _) =>
            {
                _root.RemoveChild(_scanPanel);
                RefreshTagList();
                Recalculate();
            };
            _scanPanel.Append(backBtn);

            // "Select all / none" toggles
            var selAllBtn = new UITextPanel<string>("All", 0.65f);
            selAllBtn.Width.Set(40, 0f);
            selAllBtn.Height.Set(22, 0f);
            selAllBtn.Left.Set(Pad, 0f);
            selAllBtn.Top.Set(Pad + 26 + 6, 0f);
            selAllBtn.BackgroundColor = new Color(38, 68, 118, 255);
            selAllBtn.BorderColor     = new Color(74, 122, 204, 255);
            selAllBtn.PaddingTop = 3;
            selAllBtn.PaddingLeft = 6;
            selAllBtn.OnLeftClick += (_, _) =>
            {
                _scanSelected = new HashSet<int>(_scanResults.Select(r => r.itemId));
                BuildScanPanel();
                _root.Recalculate();
            };
            _scanPanel.Append(selAllBtn);

            var selNoneBtn = new UITextPanel<string>("None", 0.65f);
            selNoneBtn.Width.Set(50, 0f);
            selNoneBtn.Height.Set(22, 0f);
            selNoneBtn.Left.Set(Pad + 46, 0f);
            selNoneBtn.Top.Set(Pad + 26 + 6, 0f);
            selNoneBtn.BackgroundColor = new Color(50, 50, 70, 255);
            selNoneBtn.BorderColor     = new Color(80, 80, 110, 255);
            selNoneBtn.PaddingTop = 3;
            selNoneBtn.PaddingLeft = 6;
            selNoneBtn.OnLeftClick += (_, _) =>
            {
                _scanSelected.Clear();
                BuildScanPanel();
                _root.Recalculate();
            };
            _scanPanel.Append(selNoneBtn);

            // Scrollable item list
            int listTop  = Pad + 26 + 6 + 22 + 6;
            int listH    = H - HeaderH - listTop - Pad - 40 - Pad;
            _scanItemList = new UIPanel();
            _scanItemList.Left.Set(0, 0f);
            _scanItemList.Top.Set(listTop, 0f);
            _scanItemList.Width.Set(W, 0f);
            _scanItemList.Height.Set(listH, 0f);
            _scanItemList.BackgroundColor = new Color(18, 26, 44, 255);
            _scanItemList.BorderColor     = new Color(50, 80, 130, 0);
            _scanItemList.SetPadding(0);
            _scanPanel.Append(_scanItemList);

            int y = 4;
            foreach (var (id, name) in _scanResults)
            {
                int    capturedId   = id;
                bool   isSelected   = _scanSelected.Contains(id);

                var row = new UIPanel();
                row.Left.Set(4, 0f);
                row.Top.Set(y, 0f);
                row.Width.Set(W - 8, 0f);
                row.Height.Set(RowH, 0f);
                row.BackgroundColor = isSelected
                    ? new Color(38, 68, 118, 255)
                    : new Color(28, 40, 64, 255);
                row.BorderColor = isSelected
                    ? new Color(74, 122, 204, 255)
                    : new Color(40, 60, 90, 255);
                row.SetPadding(0);
                row.OnLeftClick += (_, _) =>
                {
                    if (_scanSelected.Contains(capturedId))
                        _scanSelected.Remove(capturedId);
                    else
                        _scanSelected.Add(capturedId);
                    BuildScanPanel();
                    _root.Recalculate();
                };

                var icon = new UIItemIcon(GetItem(capturedId), false);
                icon.Left.Set(4, 0f);
                icon.Top.Set(1, 0f);
                icon.Width.Set(24, 0f);
                icon.Height.Set(24, 0f);
                row.Append(icon);

                var label = new UIText(name, 0.68f);
                label.Left.Set(32, 0f);
                label.Top.Set(6, 0f);
                row.Append(label);

                // Checkmark indicator
                var check = new UIText(isSelected ? "✓" : "○", 0.7f);
                check.Left.Set(W - 28, 0f);
                check.Top.Set(6, 0f);
                row.Append(check);

                _scanItemList.Append(row);
                y += RowH + 3;
            }
            _scanItemList.Recalculate();

            // Tag input + Tag button at bottom
            int bottomY = H - HeaderH - Pad - 34;

            _scanTagBox = new UIPanel();
            _scanTagBox.Left.Set(0, 0f);
            _scanTagBox.Top.Set(bottomY, 0f);
            _scanTagBox.Width.Set(W - Pad - 120, 0f);
            _scanTagBox.Height.Set(32, 0f);
            _scanTagBox.BackgroundColor = _scanTagFocused
                ? new Color(28, 40, 70, 255)
                : new Color(20, 28, 48, 255);
            _scanTagBox.BorderColor = _scanTagFocused
                ? new Color(100, 160, 255, 255)
                : new Color(50, 80, 130, 255);
            _scanTagBox.SetPadding(0);
            _scanTagBox.OnLeftClick += (_, _) => { _scanTagFocused = true; _focused = false; };
            _scanPanel.Append(_scanTagBox);

            _scanTagDisplay = new UIText("", 0.72f);
            _scanTagDisplay.Left.Set(8, 0f);
            _scanTagDisplay.Top.Set(7, 0f);
            _scanTagBox.Append(_scanTagDisplay);
            UpdateScanTagDisplay();

            var tagAllBtn = new UITextPanel<string>("Tag Selected", 0.68f);
            tagAllBtn.Left.Set(W - 116, 0f);
            tagAllBtn.Top.Set(bottomY, 0f);
            tagAllBtn.Width.Set(116, 0f);
            tagAllBtn.Height.Set(32, 0f);
            tagAllBtn.BackgroundColor = new Color(28, 68, 38, 255);
            tagAllBtn.BorderColor     = new Color(60, 140, 80, 255);
            tagAllBtn.PaddingTop = 7;
            tagAllBtn.PaddingLeft = 6;
            tagAllBtn.OnLeftClick += (_, _) => CommitScanTags();
            _scanPanel.Append(tagAllBtn);

            _scanPanel.Recalculate();
        }

        private void CommitScanTags()
        {
            string tag = TagSystem.Normalize(_scanTagInput);
            if (string.IsNullOrWhiteSpace(tag))
            {
                Main.NewText("Enter a tag name first.", Color.Orange);
                return;
            }
            if (_scanSelected.Count == 0)
            {
                Main.NewText("No items selected.", Color.Orange);
                return;
            }

            foreach (int id in _scanSelected)
                TagSystem.AddTag(id, tag);

            Main.NewText(
                $"Tagged {_scanSelected.Count} item(s) with: {tag}",
                Color.LightGreen);

            // Switch back to normal view with the new tag selected
            _selectedTag = tag;
            _root.RemoveChild(_scanPanel);
            TagItemIndex.EnsureBuilt();
            RefreshTagList();
            RefreshItemList();
            RefreshSearchResults();
            _selectedTagLabel.SetText("#" + tag);
            Recalculate();
        }

        private void UpdateScanTagDisplay()
        {
            if (_scanTagDisplay == null) return;
            bool cursorOn = _scanTagFocused && (_tick / BlinkRate) % 2 == 0;
            string cursor = cursorOn ? "|" : (_scanTagFocused ? " " : "");
            string display = string.IsNullOrEmpty(_scanTagInput)
                ? (_scanTagFocused ? cursor : "tag name...")
                : _scanTagInput + cursor;
            _scanTagDisplay.SetText(display);
        }

        // ── Tag list ──────────────────────────────────────────────────────────

        private void RefreshTagList()
        {
            _tagList.RemoveAllChildren();

            var allTags = TagSystem.GetAllTags()
                .SelectMany(kv => kv.Value)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            int y = 4;
            foreach (var tag in allTags)
            {
                string captured = tag;
                bool isSelected = tag == _selectedTag;

                var row = new UITextPanel<string>("#" + tag, 0.68f);
                row.Left.Set(4, 0f);
                row.Top.Set(y, 0f);
                row.Width.Set(TagColW - 8, 0f);
                row.Height.Set(RowH, 0f);
                row.PaddingTop  = (RowH - 14) / 2f; // vertically centre the text
                row.PaddingLeft = 6;
                row.BackgroundColor = isSelected
                    ? new Color(50, 90, 160, 255)
                    : new Color(30, 44, 72, 255);
                row.BorderColor = isSelected
                    ? new Color(100, 160, 255, 255)
                    : new Color(50, 80, 130, 255);
                row.OnLeftClick += (_, _) =>
                {
                    _selectedTag = captured;
                    _searchInput = "";
                    RefreshTagList();
                    RefreshItemList();
                    RefreshSearchResults();
                    UpdateSearchDisplay();
                    _selectedTagLabel.SetText("#" + captured);
                };

                _tagList.Append(row);
                y += RowH + 4;
            }

            if (allTags.Count == 0)
            {
                var empty = new UIText("No tags yet", 0.7f);
                empty.Left.Set(6, 0f);
                empty.Top.Set(8, 0f);
                _tagList.Append(empty);
            }

            _tagList.Recalculate();
        }

        // ── Item list (tagged items) ───────────────────────────────────────────

        private void RefreshItemList()
        {
            _itemList.RemoveAllChildren();

            if (_selectedTag == null)
            {
                var hint = new UIText("Select a tag →", 0.7f);
                hint.Left.Set(6, 0f);
                hint.Top.Set(8, 0f);
                _itemList.Append(hint);
                _itemList.Recalculate();
                return;
            }

            var items = TagItemIndex.GetTaggedItems(_selectedTag);
            int y = 4;

            foreach (var (id, name) in items)
            {
                int capturedId   = id;
                string capturedName = name;

                var row = new UIPanel();
                row.Left.Set(4, 0f);
                row.Top.Set(y, 0f);
                row.Width.Set(RightW - 8, 0f);
                row.Height.Set(RowH, 0f);
                row.BackgroundColor = new Color(30, 44, 72, 255);
                row.BorderColor     = new Color(50, 80, 130, 255);
                row.SetPadding(0);

                var icon = new UIItemIcon(GetItem(capturedId), false);
                icon.Left.Set(4, 0f);
                icon.Top.Set(1, 0f);
                icon.Width.Set(24, 0f);
                icon.Height.Set(24, 0f);
                row.Append(icon);

                var label = new UIText(capturedName, 0.68f);
                label.Left.Set(32, 0f);
                label.Top.Set(6, 0f);
                row.Append(label);

                var removeBtn = new UITextPanel<string>("×", 0.7f);
                removeBtn.Width.Set(24, 0f);
                removeBtn.Height.Set(24, 0f);
                removeBtn.Left.Set(RightW - 36, 0f);
                removeBtn.Top.Set(2, 0f);
                removeBtn.BackgroundColor = new Color(68, 28, 28, 255);
                removeBtn.BorderColor     = new Color(130, 60, 60, 255);
                removeBtn.SetPadding(0);
                removeBtn.OnLeftClick += (_, _) =>
                {
                    TagSystem.RemoveTag(capturedId, _selectedTag);
                    RefreshTagList();
                    RefreshItemList();
                    RefreshSearchResults();
                };
                row.Append(removeBtn);

                _itemList.Append(row);
                y += RowH + 4;
            }

            if (items.Count == 0)
            {
                var empty = new UIText("No items tagged yet", 0.7f);
                empty.Left.Set(6, 0f);
                empty.Top.Set(8, 0f);
                _itemList.Append(empty);
            }

            _itemList.Recalculate();
        }

        // ── Search results ────────────────────────────────────────────────────

        private void RefreshSearchResults()
        {
            _searchResults.RemoveAllChildren();

            if (_selectedTag == null || string.IsNullOrWhiteSpace(_searchInput))
            {
                var hint = new UIText(
                    _selectedTag == null ? "Select a tag first" : "Type to search items",
                    0.7f);
                hint.Left.Set(6, 0f);
                hint.Top.Set(8, 0f);
                _searchResults.Append(hint);
                _searchResults.Recalculate();
                return;
            }

            var results = TagItemIndex.Search(_searchInput, _selectedTag, MaxResults);
            int y = 4;

            foreach (var (id, name) in results)
            {
                int capturedId = id;

                var row = new UIPanel();
                row.Left.Set(4, 0f);
                row.Top.Set(y, 0f);
                row.Width.Set(RightW - 8, 0f);
                row.Height.Set(RowH, 0f);
                row.BackgroundColor = new Color(30, 44, 72, 255);
                row.BorderColor     = new Color(50, 80, 130, 255);
                row.SetPadding(0);

                var icon = new UIItemIcon(GetItem(capturedId), false);
                icon.Left.Set(4, 0f);
                icon.Top.Set(1, 0f);
                icon.Width.Set(24, 0f);
                icon.Height.Set(24, 0f);
                row.Append(icon);

                var label = new UIText(name, 0.68f);
                label.Left.Set(32, 0f);
                label.Top.Set(6, 0f);
                row.Append(label);

                var addBtn = new UITextPanel<string>("+", 0.7f);
                addBtn.Width.Set(24, 0f);
                addBtn.Height.Set(24, 0f);
                addBtn.Left.Set(RightW - 36, 0f);
                addBtn.Top.Set(2, 0f);
                addBtn.BackgroundColor = new Color(28, 68, 38, 255);
                addBtn.BorderColor     = new Color(60, 140, 80, 255);
                addBtn.SetPadding(0);
                addBtn.OnLeftClick += (_, _) =>
                {
                    TagSystem.AddTag(capturedId, _selectedTag);
                    RefreshItemList();
                    RefreshSearchResults();
                };
                row.Append(addBtn);

                _searchResults.Append(row);
                y += RowH + 4;
            }

            if (results.Count == 0)
            {
                var empty = new UIText("No results", 0.7f);
                empty.Left.Set(6, 0f);
                empty.Top.Set(8, 0f);
                _searchResults.Append(empty);
            }

            _searchResults.Recalculate();
        }

        public void RefreshAll()
        {
            RefreshTagList();
            RefreshItemList();
            RefreshSearchResults();
        }

        public void HandleInput()
        {
            _tick++;

            // ── Scan tag input ────────────────────────────────────────────────
            if (_scanTagFocused)
            {
                Main.chatRelease = false;
                PlayerInput.WritingText = true;
                Main.instance.HandleIME();

                string updated = Main.GetInputText(_scanTagInput);
                if (updated != _scanTagInput)
                {
                    _scanTagInput = updated;
                }
                UpdateScanTagDisplay();

                if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape)
                && !Main.oldKeyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                {
                    TagManagerUISystem.Close();
                    return;
                }
                return;
            }

            // ── Normal search input ───────────────────────────────────────────
            if (!_focused) return;

            Main.chatRelease = false;
            PlayerInput.WritingText = true;
            Main.instance.HandleIME();

            string upd = Main.GetInputText(_searchInput);
            if (upd != _searchInput)
            {
                _searchInput = upd;
                RefreshSearchResults();
            }

            UpdateSearchDisplay();

            if (Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape)
            && !Main.oldKeyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
                TagManagerUISystem.Close();
        }

        private void UpdateSearchDisplay()
        {
            if (_searchDisplay == null) return;
            bool cursorOn = _focused && (_tick / BlinkRate) % 2 == 0;
            string cursor = cursorOn ? "|" : (_focused ? " " : "");
            string display = string.IsNullOrEmpty(_searchInput)
                ? (_focused ? cursor : "search items...")
                : _searchInput + cursor;
            _searchDisplay.SetText(display);

            if (_searchBox != null)
            {
                _searchBox.BorderColor = _focused
                    ? new Color(100, 160, 255, 255)
                    : new Color(50, 80, 130, 255);
                _searchBox.BackgroundColor = _focused
                    ? new Color(28, 40, 70, 255)
                    : new Color(20, 28, 48, 255);
            }
        }

        // ── Mouse focus ───────────────────────────────────────────────────────

        public bool IsMouseOver()
        {
            if (_root == null) return false;
            return _root.GetDimensions().ToRectangle().Contains(Main.mouseX, Main.mouseY);
        }

        public void HandleFocusClick(bool mouseOverPanel, bool mouseLeft)
        {
            if (!mouseLeft) return;
            if (!mouseOverPanel)
            {
                _focused        = false;
                _scanTagFocused = false;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Item GetItem(int id)
        {
            if (ContentSamples.ItemsByType.TryGetValue(id, out var sample))
                return sample;
            var item = new Item();
            item.SetDefaults(id);
            return item;
        }
    }
}
