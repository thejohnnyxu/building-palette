using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BuildingPalette
{
    /// <summary>
    /// Tracks which item is currently selected for chat commands.
    /// Separate from the UI — commands work even without the panel open.
    /// </summary>
    public static class TagEditorState
    {
        private static Item _pendingItem = null;

        public static bool IsOpen      => _pendingItem != null;
        public static Item PendingItem => _pendingItem;

        public static void SetItem(Item item) => _pendingItem = item;
        public static void Clear()            => _pendingItem = null;
    }

    // ── /tag <tagname> ────────────────────────────────────────────────────────

    public class TagCommand : ModCommand
    {
        public override string Command     => "tag";
        public override CommandType Type   => CommandType.Chat;
        public override string Description => "Add a tag to the selected item. Press T over an item first.";
        public override string Usage       => "/tag <tagname>";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (!TagEditorState.IsOpen)
            {
                caller.Reply("No item selected. Press T while hovering an item first.", Color.Orange);
                return;
            }
            if (args.Length == 0)
            {
                caller.Reply("Usage: /tag <tagname>", Color.Orange);
                return;
            }

            string tag = TagSystem.Normalize(string.Join("-", args));
            if (string.IsNullOrWhiteSpace(tag))
            {
                caller.Reply("Invalid tag name. Use letters, numbers, and hyphens.", Color.Orange);
                return;
            }

            var item = TagEditorState.PendingItem;
            TagSystem.AddTag(item.type, tag);
            TagEditorUISystem.RefreshChips(item);
            caller.Reply($"Tagged '{item.Name}' with: {tag}", Color.LightGreen);
            caller.Reply($"All tags: {string.Join(", ", TagSystem.GetTags(item.type))}", Color.Gray);
        }
    }

    // ── /untag <tagname> ──────────────────────────────────────────────────────

    public class UntagCommand : ModCommand
    {
        public override string Command     => "untag";
        public override CommandType Type   => CommandType.Chat;
        public override string Description => "Remove a tag from the selected item.";
        public override string Usage       => "/untag <tagname>";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (!TagEditorState.IsOpen)
            {
                caller.Reply("No item selected. Press T while hovering an item first.", Color.Orange);
                return;
            }
            if (args.Length == 0)
            {
                caller.Reply("Usage: /untag <tagname>", Color.Orange);
                return;
            }

            string tag = TagSystem.Normalize(string.Join("-", args));
            var item = TagEditorState.PendingItem;

            if (!TagSystem.HasTag(item.type, tag))
            {
                caller.Reply($"'{item.Name}' doesn't have tag: {tag}", Color.Orange);
                return;
            }

            TagSystem.RemoveTag(item.type, tag);
            TagEditorUISystem.RefreshChips(item);
            caller.Reply($"Removed tag '{tag}' from '{item.Name}'.", Color.LightGreen);
        }
    }

    // ── /tags ─────────────────────────────────────────────────────────────────

    public class TagsCommand : ModCommand
    {
        public override string Command     => "tags";
        public override CommandType Type   => CommandType.Chat;
        public override string Description => "List all tags on the selected item.";
        public override string Usage       => "/tags";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (!TagEditorState.IsOpen)
            {
                caller.Reply("No item selected. Press T while hovering an item first.", Color.Orange);
                return;
            }

            var item = TagEditorState.PendingItem;
            if (!TagSystem.HasAnyTags(item.type))
            {
                caller.Reply($"'{item.Name}' has no tags yet.", Color.Gray);
                return;
            }

            caller.Reply($"Tags on '{item.Name}': {string.Join(", ", TagSystem.GetTags(item.type))}", Color.Cyan);
        }
    }

    // ── /tagdone ──────────────────────────────────────────────────────────────

    public class TagDoneCommand : ModCommand
    {
        public override string Command     => "tagdone";
        public override CommandType Type   => CommandType.Chat;
        public override string Description => "Finish tagging and deselect the current item.";
        public override string Usage       => "/tagdone";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (!TagEditorState.IsOpen)
            {
                caller.Reply("No item is currently selected.", Color.Gray);
                return;
            }

            var item = TagEditorState.PendingItem;
            string tagList = TagSystem.HasAnyTags(item.type)
                ? string.Join(", ", TagSystem.GetTags(item.type))
                : "(none)";

            caller.Reply($"Done. '{item.Name}' tags: {tagList}", Color.Cyan);
            TagEditorState.Clear();
        }
    }

    // ── /alltags ──────────────────────────────────────────────────────────────

    public class AllTagsCommand : ModCommand
    {
        public override string Command     => "alltags";
        public override CommandType Type   => CommandType.Chat;
        public override string Description => "List every tag tracked across all items.";
        public override string Usage       => "/alltags";

        // Readable palette — bright enough to show on dark chat background
        private static readonly Color[] TagPalette = {
            new Color(255, 150, 150), // soft red
            new Color(255, 200, 100), // amber
            new Color(150, 255, 150), // mint
            new Color(100, 200, 255), // sky blue
            new Color(200, 150, 255), // lavender
            new Color(255, 160, 220), // pink
            new Color(100, 255, 220), // teal
            new Color(255, 220, 100), // yellow
        };

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            var allTags = TagSystem.GetAllTags();

            if (allTags.Count == 0)
            {
                caller.Reply("No tags tracked yet. Hover an item and press T to start.", Color.Gray);
                return;
            }

            // Invert: tag → list of (itemId, itemName)
            var byTag = new System.Collections.Generic.Dictionary<string,
                System.Collections.Generic.List<(int id, string name)>>();

            foreach (var (itemId, tagSet) in allTags)
            {
                string itemName = itemId < ItemID.Count
                    ? Lang.GetItemNameValue(itemId)
                    : $"Item #{itemId}";
                foreach (var tag in tagSet)
                {
                    if (!byTag.ContainsKey(tag))
                        byTag[tag] = new System.Collections.Generic.List<(int, string)>();
                    byTag[tag].Add((itemId, itemName));
                }
            }

            caller.Reply($"All tags ({byTag.Count}):", Color.Cyan);
            foreach (var (tag, itemEntries) in byTag)
            {
                // Hash tag name to a consistent color from a readable palette
                Color tagColor = TagPalette[Math.Abs(tag.GetHashCode()) % TagPalette.Length];
                string itemList = string.Join(", ", itemEntries.ConvertAll(e => $"[i:{e.id}]{e.name}"));
                caller.Reply($"  [c/{tagColor.R:X2}{tagColor.G:X2}{tagColor.B:X2}:#{tag}]: {itemList}", Color.LightGray);
            }
        }
    }

    // ── /renametag <old> <new> ────────────────────────────────────────────────

    public class RenameTagCommand : ModCommand
    {
        public override string Command     => "renametag";
        public override CommandType Type   => CommandType.Chat;
        public override string Description => "Rename a tag across all items.";
        public override string Usage       => "/renametag <oldname> <newname>";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length < 2)
            {
                caller.Reply("Usage: /renametag <oldname> <newname>", Color.Orange);
                return;
            }

            string oldTag = TagSystem.Normalize(args[0]);
            string newTag = TagSystem.Normalize(args[1]);

            if (string.IsNullOrWhiteSpace(oldTag) || string.IsNullOrWhiteSpace(newTag))
            {
                caller.Reply("Invalid tag name.", Color.Orange);
                return;
            }

            if (oldTag == newTag)
            {
                caller.Reply("Old and new tag names are the same.", Color.Orange);
                return;
            }

            int count = 0;
            foreach (var (itemId, tagSet) in TagSystem.GetAllTags())
            {
                if (!tagSet.Contains(oldTag)) continue;
                tagSet.Remove(oldTag);
                tagSet.Add(newTag);
                count++;
            }

            if (count == 0)
            {
                caller.Reply($"No items found with tag '{oldTag}'.", Color.Orange);
                return;
            }

            if (TagEditorState.IsOpen && TagEditorState.PendingItem != null)
                TagEditorUISystem.RefreshChips(TagEditorState.PendingItem);

            caller.Reply($"Renamed '{oldTag}' → '{newTag}' across {count} item(s).", Color.LightGreen);
        }
    }

    // ── /exporttag <tagname> ──────────────────────────────────────────────────

    public class ExportTagCommand : ModCommand
    {
        public override string Command     => "exporttag";
        public override CommandType Type   => CommandType.Chat;
        public override string Description => "Export a tag and its items as a shareable string.";
        public override string Usage       => "/exporttag <tagname>";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0)
            {
                caller.Reply("Usage: /exporttag <tagname>", Color.Orange);
                return;
            }

            string tag = TagSystem.Normalize(string.Join("-", args));

            // Check any items actually have this tag
            bool exists = TagSystem.GetAllTags().Any(kv => kv.Value.Contains(tag));
            if (!exists)
            {
                caller.Reply($"No items found with tag '{tag}'.", Color.Orange);
                return;
            }

            string exported = TagImportExport.Export(tag);
            caller.Reply("Copy the line below and share it:", Color.Cyan);
            caller.Reply(exported, Color.White);
        }
    }

    // ── /importtag <string> ───────────────────────────────────────────────────

    public class ImportTagCommand : ModCommand
    {
        public override string Command     => "importtag";
        public override CommandType Type   => CommandType.Chat;
        public override string Description => "Import a tag string exported by /exporttag.";
        public override string Usage       => "/importtag <tagname:Item One,Item Two>";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (args.Length == 0)
            {
                caller.Reply("Usage: /importtag warm-stone:Sandstone Block,Smooth Sandstone", Color.Orange);
                return;
            }

            // Rejoin args — the import string may contain spaces
            string importStr = string.Join(" ", args);
            var result = TagImportExport.Import(importStr);

            if (result.Warnings.Count > 0 && result.Added == 0)
            {
                foreach (var w in result.Warnings)
                    caller.Reply($"Error: {w}", Color.Orange);
                return;
            }

            caller.Reply($"Imported '{result.Tag}': added {result.Added} item(s).", Color.LightGreen);

            if (result.NotFound.Count > 0)
                caller.Reply($"Not found: {string.Join(", ", result.NotFound)}", Color.Orange);

            if (result.Warnings.Count > 0)
                foreach (var w in result.Warnings)
                    caller.Reply($"Warning: {w}", Color.Yellow);

            // Refresh Tag Manager if open
            if (TagManagerUISystem.IsOpen)
                TagManagerUISystem.RefreshAll();
        }
    }
}
