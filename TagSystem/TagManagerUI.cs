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

        // Scroll state — pixel offset from the top of each list's content
        private int _itemListScroll   = 0;
        private int _searchScroll     = 0;
        private int _tagListScroll    = 0;
        private int _scanListScroll   = 0;
        private int _itemListContent  = 0;
        private int _searchContent    = 0;
        private int _tagListContent   = 0;
        private int _scanListContent  = 0;
        private const int ScrollStep  = RowH + 4;

        // Scrollbar drag state
        private enum DragTarget { None, TagList, ItemList, SearchResults, ScanList }
        private DragTarget _dragTarget      = DragTarget.None;
        private int        _dragStartY      = 0;
        private int        _dragStartScroll = 0;
        private bool       _prevMouseLeftDrag = false;

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
            exportBtn.PaddingTop  = 5;
            exportBtn.PaddingLeft = 8;
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
            importBtn.PaddingTop  = 5;
            importBtn.PaddingLeft = 8;
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
            _tagList.OverflowHidden  = true;
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
            _itemList.OverflowHidden  = true;
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
            // OnLeftClick fires inside _ui.Update() which runs inside the blocking layer,
            // after HandleFocusClick has already been evaluated for this frame.
            _searchBox.OnLeftClick += (_, _) =>
            {
                _focused = true;
                _scanTagFocused = false;
            };
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
            _searchResults.OverflowHidden  = true;
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
            _itemListScroll  = 0;
            _searchScroll    = 0;
            _tagListScroll   = 0;
            _dragTarget      = DragTarget.None;

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
            _scanListScroll  = 0;

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
            // scanPanel height = H - HeaderH, so calculate relative to that
            int scanPanelH = H - HeaderH;
            int listTop    = Pad + 26 + 6 + 22 + 6;
            int bottomBarH = Pad + 32 + Pad; // tag input row
            int listH      = scanPanelH - listTop - bottomBarH;
            _scanItemList = new UIPanel();
            _scanItemList.Left.Set(0, 0f);
            _scanItemList.Top.Set(listTop, 0f);
            _scanItemList.Width.Set(W, 0f);
            _scanItemList.Height.Set(listH, 0f);
            _scanItemList.BackgroundColor = new Color(18, 26, 44, 255);
            _scanItemList.BorderColor     = new Color(50, 80, 130, 0);
            _scanItemList.SetPadding(0);
            _scanItemList.OverflowHidden  = true;
            _scanPanel.Append(_scanItemList);

            int contentH = _scanResults.Count * (RowH + 3);
            _scanListContent = contentH;
            _scanListScroll  = System.Math.Clamp(_scanListScroll, 0, System.Math.Max(0, contentH - listH));

            int y = 4 - _scanListScroll;
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
            DrawScrollbar(_scanItemList, _scanListScroll, _scanListContent, listH, ref _scanListThumbRect);

            // Tag input + Tag button at bottom
            int bottomY = scanPanelH - Pad - 32;

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

            int contentH = allTags.Count * (RowH + 4);
            _tagListContent = contentH;
            int panelH = (int)_tagList.GetDimensions().Height;
            if (panelH <= 0) panelH = H - HeaderH - Pad - 18 - Pad;
            _tagListScroll = System.Math.Clamp(_tagListScroll, 0, System.Math.Max(0, contentH - panelH));

            int y = 4 - _tagListScroll;
            foreach (var tag in allTags)
            {
                string captured = tag;
                bool isSelected = tag == _selectedTag;

                var row = new UITextPanel<string>("#" + tag, 0.68f);
                row.Left.Set(4, 0f);
                row.Top.Set(y, 0f);
                row.Width.Set(TagColW - 8, 0f);
                row.Height.Set(RowH, 0f);
                row.PaddingTop  = (RowH - 14) / 2f;
                row.PaddingLeft = 6;
                row.BackgroundColor = isSelected ? new Color(50, 90, 160, 255) : new Color(30, 44, 72, 255);
                row.BorderColor     = isSelected ? new Color(100, 160, 255, 255) : new Color(50, 80, 130, 255);
                row.OnLeftClick += (_, _) =>
                {
                    _selectedTag    = captured;
                    _searchInput    = "";
                    _itemListScroll = 0;
                    _searchScroll   = 0;
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

            DrawScrollbar(_tagList, _tagListScroll, _tagListContent, H - HeaderH - Pad - 18 - Pad, ref _tagListThumbRect);
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
            int itemListH = (H - HeaderH - Pad - 18 - Pad) / 2 - Pad;
            int contentH  = items.Count * (RowH + 4);
            _itemListContent = contentH;
            _itemListScroll  = System.Math.Clamp(_itemListScroll, 0, System.Math.Max(0, contentH - itemListH));
            int y = 4 - _itemListScroll;

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
                icon.Top.Set((RowH - 20) / 2, 0f);
                icon.Width.Set(20, 0f);
                icon.Height.Set(20, 0f);
                row.Append(icon);

                var label = new UIText(capturedName, 0.68f);
                label.Left.Set(28, 0f);
                label.Top.Set((RowH - 14) / 2, 0f);
                row.Append(label);

                var removeBtn = new UITextPanel<string>("×", 1.2f);
                removeBtn.Width.Set(28, 0f);
                removeBtn.Height.Set(26, 0f);
                removeBtn.Left.Set(RightW - 44, 0f);
                removeBtn.Top.Set(1, 0f);
                removeBtn.BackgroundColor = new Color(68, 28, 28, 255);
                removeBtn.BorderColor     = new Color(130, 60, 60, 255);
                removeBtn.PaddingTop  = 1;
                removeBtn.PaddingLeft = 5;
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

            DrawScrollbar(_itemList, _itemListScroll, _itemListContent, itemListH, ref _itemListThumbRect);
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
            int searchBoxTop = (H - HeaderH - Pad - 18 - Pad) / 2 - Pad + Pad + 18 + SearchH + Pad;
            int searchPanelH = H - searchBoxTop - SearchH - Pad * 2;
            int contentH2    = results.Count * (RowH + 4);
            _searchContent   = contentH2;
            _searchScroll    = System.Math.Clamp(_searchScroll, 0, System.Math.Max(0, contentH2 - searchPanelH));
            int y = 4 - _searchScroll;

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
                icon.Top.Set((RowH - 20) / 2, 0f);
                icon.Width.Set(20, 0f);
                icon.Height.Set(20, 0f);
                row.Append(icon);

                var label = new UIText(name, 0.68f);
                label.Left.Set(28, 0f);
                label.Top.Set((RowH - 14) / 2, 0f);
                row.Append(label);

                var addBtn = new UITextPanel<string>("+", 1.2f);
                addBtn.Width.Set(28, 0f);
                addBtn.Height.Set(26, 0f);
                addBtn.Left.Set(RightW - 44, 0f);
                addBtn.Top.Set(1, 0f);
                addBtn.BackgroundColor = new Color(28, 68, 38, 255);
                addBtn.BorderColor     = new Color(60, 140, 80, 255);
                addBtn.PaddingTop  = 1;
                addBtn.PaddingLeft = 5;
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

            DrawScrollbar(_searchResults, _searchScroll, _searchContent, searchPanelH, ref _searchThumbRect);
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

            bool mouseJustPressed = Main.mouseLeft && !_prevMouseLeftDrag;
            _prevMouseLeftDrag    = Main.mouseLeft;
            var mousePos = new Point(Main.mouseX, Main.mouseY);

            // ── Scrollbar drag ────────────────────────────────────────────────
            if (Main.mouseLeft)
            {
                if (_dragTarget == DragTarget.None && mouseJustPressed)
                {
                    // Inflate thumb rect a little for easier grabbing
                    var tagThumb  = _tagListThumbRect;   tagThumb.Inflate(4, 0);
                    var itemThumb = _itemListThumbRect;  itemThumb.Inflate(4, 0);
                    var srchThumb = _searchThumbRect;    srchThumb.Inflate(4, 0);
                    var scanThumb = _scanListThumbRect;  scanThumb.Inflate(4, 0);

                    if (!tagThumb.IsEmpty  && tagThumb.Contains(mousePos))
                    {
                        _dragTarget     = DragTarget.TagList;
                        _dragStartY     = Main.mouseY;
                        _dragStartScroll = _tagListScroll;
                    }
                    else if (!itemThumb.IsEmpty && itemThumb.Contains(mousePos))
                    {
                        _dragTarget     = DragTarget.ItemList;
                        _dragStartY     = Main.mouseY;
                        _dragStartScroll = _itemListScroll;
                    }
                    else if (!srchThumb.IsEmpty && srchThumb.Contains(mousePos))
                    {
                        _dragTarget      = DragTarget.SearchResults;
                        _dragStartY      = Main.mouseY;
                        _dragStartScroll = _searchScroll;
                    }
                    else if (!scanThumb.IsEmpty && scanThumb.Contains(mousePos))
                    {
                        _dragTarget      = DragTarget.ScanList;
                        _dragStartY      = Main.mouseY;
                        _dragStartScroll = _scanListScroll;
                    }
                }

                if (_dragTarget != DragTarget.None)
                {
                    int mouseDelta = Main.mouseY - _dragStartY;

                    if (_dragTarget == DragTarget.TagList && _tagListContent > 0)
                    {
                        int panelH   = H - HeaderH - Pad - 18 - Pad;
                        float ratio  = (float)_tagListContent / panelH;
                        _tagListScroll = System.Math.Clamp(
                            _dragStartScroll + (int)(mouseDelta * ratio), 0,
                            System.Math.Max(0, _tagListContent - panelH));
                        RefreshTagList();
                    }
                    else if (_dragTarget == DragTarget.ItemList && _itemListContent > 0)
                    {
                        int panelH   = (H - HeaderH - Pad - 18 - Pad) / 2 - Pad;
                        float ratio  = (float)_itemListContent / panelH;
                        _itemListScroll = System.Math.Clamp(
                            _dragStartScroll + (int)(mouseDelta * ratio), 0,
                            System.Math.Max(0, _itemListContent - panelH));
                        RefreshItemList();
                    }
                    else if (_dragTarget == DragTarget.SearchResults && _searchContent > 0)
                    {
                        int searchBoxTop = (H - HeaderH - Pad - 18 - Pad) / 2 - Pad + Pad + 18 + SearchH + Pad;
                        int panelH       = H - searchBoxTop - SearchH - Pad * 2;
                        float ratio      = (float)_searchContent / panelH;
                        _searchScroll = System.Math.Clamp(
                            _dragStartScroll + (int)(mouseDelta * ratio), 0,
                            System.Math.Max(0, _searchContent - panelH));
                        RefreshSearchResults();
                    }
                    else if (_dragTarget == DragTarget.ScanList && _scanListContent > 0)
                    {
                        int scanPanelH2 = H - HeaderH;
                        int listTop2    = Pad + 26 + 6 + 22 + 6;
                        int bottomBarH2 = Pad + 32 + Pad;
                        int scanListH   = scanPanelH2 - listTop2 - bottomBarH2;
                        float ratio     = (float)_scanListContent / scanListH;
                        _scanListScroll = System.Math.Clamp(
                            _dragStartScroll + (int)(mouseDelta * ratio), 0,
                            System.Math.Max(0, _scanListContent - scanListH));
                        BuildScanPanel();
                        _root.Recalculate();
                    }

                    // Block vanilla from acting on the drag click
                    Main.LocalPlayer.mouseInterface = true;
                    return;
                }
            }
            else
            {
                _dragTarget = DragTarget.None;
            }

            // ── Mouse wheel scrolling ─────────────────────────────────────────
            if (IsMouseOver() && PlayerInput.ScrollWheelDeltaForUI != 0)
            {
                int delta = -(PlayerInput.ScrollWheelDeltaForUI / 120) * ScrollStep;
                var scrollPos = new Point(Main.mouseX, Main.mouseY);

                if (_tagList != null && _tagList.GetOuterDimensions().ToRectangle().Contains(scrollPos))
                {
                    _tagListScroll = System.Math.Clamp(_tagListScroll + delta, 0,
                        System.Math.Max(0, _tagListContent - (int)_tagList.GetDimensions().Height));
                    RefreshTagList();
                }
                else if (_itemList != null && _itemList.GetOuterDimensions().ToRectangle().Contains(scrollPos))
                {
                    int itemListH = (H - HeaderH - Pad - 18 - Pad) / 2 - Pad;
                    _itemListScroll = System.Math.Clamp(_itemListScroll + delta, 0,
                        System.Math.Max(0, _itemListContent - itemListH));
                    RefreshItemList();
                }
                else if (_searchResults != null && _searchResults.GetOuterDimensions().ToRectangle().Contains(scrollPos))
                {
                    int searchBoxTop = (H - HeaderH - Pad - 18 - Pad) / 2 - Pad + Pad + 18 + SearchH + Pad;
                    int searchPanelH = H - searchBoxTop - SearchH - Pad * 2;
                    _searchScroll = System.Math.Clamp(_searchScroll + delta, 0,
                        System.Math.Max(0, _searchContent - searchPanelH));
                    RefreshSearchResults();
                }
                else if (_scanItemList != null && _scanItemList.GetOuterDimensions().ToRectangle().Contains(scrollPos))
                {
                    int scanPanelH2 = H - HeaderH;
                    int listTop2    = Pad + 26 + 6 + 22 + 6;
                    int bottomBarH2 = Pad + 32 + Pad;
                    int scanListH   = scanPanelH2 - listTop2 - bottomBarH2;
                    _scanListScroll = System.Math.Clamp(_scanListScroll + delta, 0,
                        System.Math.Max(0, _scanListContent - scanListH));
                    BuildScanPanel();
                    _root.Recalculate();
                }

                // Consume scroll so vanilla doesn't also act on it
                PlayerInput.ScrollWheelDeltaForUI = 0;
            }

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

        /// <summary>
        /// Appends a simple scrollbar indicator on the right edge of a panel.
        /// Only visible when content exceeds panel height.
        /// </summary>
        // Screen-space thumb rectangles updated each Refresh — used for drag hit testing
        private Rectangle _tagListThumbRect;
        private Rectangle _itemListThumbRect;
        private Rectangle _searchThumbRect;
        private Rectangle _scanListThumbRect;

        private void DrawScrollbar(UIPanel panel, int scroll, int contentH, int panelH,
            ref Rectangle thumbRectOut)
        {
            thumbRectOut = Rectangle.Empty;
            if (contentH <= panelH) return;

            float ratio     = (float)panelH / contentH;
            float thumbH    = System.Math.Max(20, panelH * ratio);
            float maxScroll = contentH - panelH;
            float thumbY    = maxScroll > 0 ? (scroll / maxScroll) * (panelH - thumbH) : 0;

            var track = new UIPanel();
            track.Left.Set(panel.Width.Pixels - 6, 0f);
            track.Top.Set(0, 0f);
            track.Width.Set(5, 0f);
            track.Height.Set(panelH, 0f);
            track.BackgroundColor = new Color(15, 22, 40, 180);
            track.BorderColor     = new Color(30, 50, 80, 120);
            track.SetPadding(0);
            panel.Append(track);

            var thumb = new UIPanel();
            thumb.Left.Set(0, 0f);
            thumb.Top.Set((int)thumbY, 0f);
            thumb.Width.Set(5, 0f);
            thumb.Height.Set((int)thumbH, 0f);
            thumb.BackgroundColor = new Color(80, 130, 200, 200);
            thumb.BorderColor     = new Color(100, 160, 255, 0);
            thumb.SetPadding(0);
            track.Append(thumb);

            // Store screen-space rect for drag hit testing in HandleInput
            // Panel outer dims give us the panel's screen position
            var panelDim = panel.GetOuterDimensions();
            if (panelDim.Width > 0)
            {
                thumbRectOut = new Rectangle(
                    (int)(panelDim.X + panel.Width.Pixels - 6),
                    (int)(panelDim.Y + thumbY),
                    5,
                    (int)thumbH);
            }
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
            var dim = _root.GetOuterDimensions().ToRectangle();
            // Guard against uninitialized layout (all zeros before first Recalculate)
            if (dim.Width == 0 || dim.Height == 0) return false;
            return dim.Contains(Main.mouseX, Main.mouseY);
        }

        public void HandleFocusClick(bool mouseOverPanel, bool mouseJustPressed)
        {
            if (!mouseJustPressed) return;
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
