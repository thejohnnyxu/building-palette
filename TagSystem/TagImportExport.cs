using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BuildingPalette
{
    public class TagImportExportSystem : ModSystem
    {
        // Invalidate cached name index on mod unload so it rebuilds next session
        public override void Unload() => TagImportExport.InvalidateNameIndex();
    }

    public static class TagImportExport
    {
        // Cached name→ids lookup, built once on first import
        private static Dictionary<string, List<int>> _nameIndex = null;

        public static void InvalidateNameIndex() => _nameIndex = null;

        private static Dictionary<string, List<int>> GetNameIndex()
        {
            if (_nameIndex != null) return _nameIndex;

            _nameIndex = new Dictionary<string, List<int>>();
            foreach (var kv in ContentSamples.ItemsByType)
            {
                var item = kv.Value;
                if (item == null || item.IsAir || string.IsNullOrWhiteSpace(item.Name))
                    continue;

                string key = item.Name.ToLowerInvariant();
                if (!_nameIndex.TryGetValue(key, out var list))
                {
                    list = new List<int>();
                    _nameIndex[key] = list;
                }
                list.Add(kv.Key);
            }
            return _nameIndex;
        }

        // ── Export ────────────────────────────────────────────────────────────

        public static string Export(string tag)
        {
            tag = TagSystem.Normalize(tag);

            var itemNames = new List<string>();
            foreach (var (itemId, tagSet) in TagSystem.GetAllTags())
            {
                if (!tagSet.Contains(tag)) continue;
                if (ContentSamples.ItemsByType.TryGetValue(itemId, out var item)
                && !string.IsNullOrEmpty(item.Name))
                    itemNames.Add(item.Name);
            }

            itemNames.Sort();
            return $"{tag}:{string.Join(",", itemNames)}";
        }

        // ── Import ────────────────────────────────────────────────────────────

        public class ImportResult
        {
            public string Tag;
            public int    Added;
            public List<string> Warnings = new();
            public List<string> NotFound = new();
        }

        public static ImportResult Import(string input)
        {
            var result = new ImportResult();

            input = input.Trim();
            int colonIdx = input.IndexOf(':');
            if (colonIdx < 0)
            {
                result.Warnings.Add("Invalid format. Expected: tagname:Item One,Item Two");
                return result;
            }

            result.Tag = TagSystem.Normalize(input[..colonIdx]);
            if (string.IsNullOrWhiteSpace(result.Tag))
            {
                result.Warnings.Add("Tag name is empty.");
                return result;
            }

            var itemNames = input[(colonIdx + 1)..]
                .Split(',')
                .Select(n => n.Trim())
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            var nameIndex = GetNameIndex();

            foreach (var name in itemNames)
            {
                string key = name.ToLowerInvariant();

                if (!nameIndex.TryGetValue(key, out var matches))
                {
                    result.NotFound.Add(name);
                    continue;
                }

                if (matches.Count > 1)
                {
                    result.Warnings.Add(
                        $"'{name}' matched {matches.Count} items — skipped. " +
                        $"Be more specific or use /tag after hovering the exact item.");
                    continue;
                }

                TagSystem.AddTag(matches[0], result.Tag);
                result.Added++;
            }

            return result;
        }
    }
}
