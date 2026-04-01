using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BuildingPalette
{
    public class TagItemIndexSystem : ModSystem
    {
        public override void Unload() => TagItemIndex.Invalidate();
    }

    public static class TagItemIndex
    {
        // Store both display name and pre-lowercased name to avoid
        // repeated ToLowerInvariant() allocations on every search keystroke
        private static List<(int id, string name, string nameLower)> _index = null;

        public static void EnsureBuilt()
        {
            if (_index != null) return;

            _index = new List<(int id, string name, string nameLower)>();

            foreach (var kv in ContentSamples.ItemsByType)
            {
                int id   = kv.Key;
                var item = kv.Value;

                if (item == null || item.IsAir) continue;
                if (string.IsNullOrWhiteSpace(item.Name)) continue;

                _index.Add((id, item.Name, item.Name.ToLowerInvariant()));
            }

            _index.Sort((a, b) => string.Compare(a.name, b.name,
                System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Substring search. Pre-lowercased names avoid per-result allocations.
        /// </summary>
        public static List<(int id, string name)> Search(
            string query, string excludeTag, int max = 10)
        {
            EnsureBuilt();

            if (string.IsNullOrWhiteSpace(query))
                return new List<(int, string)>();

            string q = query.Trim().ToLowerInvariant();

            return _index
                .Where(e => e.nameLower.Contains(q))
                .Where(e => !TagSystem.HasTag(e.id, excludeTag))
                .Take(max)
                .Select(e => (e.id, e.name))
                .ToList();
        }

        public static List<(int id, string name)> GetTaggedItems(string tag)
        {
            EnsureBuilt();

            // O(n) pre-built lookup instead of O(n²) FirstOrDefault per item
            var idToName = new Dictionary<int, string>(_index.Count);
            foreach (var (id, name, _) in _index)
                idToName[id] = name;

            var result = new List<(int id, string name)>();
            foreach (var (itemId, tagSet) in TagSystem.GetAllTags())
            {
                if (!tagSet.Contains(tag)) continue;
                string n = idToName.TryGetValue(itemId, out var found)
                    ? found : $"Item #{itemId}";
                result.Add((itemId, n));
            }

            result.Sort((a, b) => string.Compare(a.name, b.name,
                System.StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public static void Invalidate() => _index = null;
    }
}
