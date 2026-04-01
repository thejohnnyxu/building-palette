using System.Collections.Generic;
using System.Linq;

namespace BuildingPalette
{
    /// <summary>
    /// Core tag data store. Static so anything in the mod can read/write tags.
    /// Actual persistence is handled by TagPlayer (ModPlayer save/load).
    /// </summary>
    public static class TagSystem
    {
        // itemId (net ID) → set of tags
        private static Dictionary<int, HashSet<string>> _tags = new();

        // ── Write ────────────────────────────────────────────────────────────

        public static void AddTag(int itemId, string tag)
        {
            tag = Normalize(tag);
            if (string.IsNullOrWhiteSpace(tag)) return;

            if (!_tags.TryGetValue(itemId, out var set))
            {
                set = new HashSet<string>();
                _tags[itemId] = set;
            }
            set.Add(tag);
        }

        public static void RemoveTag(int itemId, string tag)
        {
            tag = Normalize(tag);
            if (!_tags.TryGetValue(itemId, out var set)) return;

            set.Remove(tag);

            // Clean up empty entries so we don't accumulate dead keys
            if (set.Count == 0)
                _tags.Remove(itemId);
        }

        public static void ClearTags(int itemId)
        {
            _tags.Remove(itemId);
        }

        // ── Read ─────────────────────────────────────────────────────────────

        public static IReadOnlyCollection<string> GetTags(int itemId)
        {
            return _tags.TryGetValue(itemId, out var set)
                ? set
                : System.Array.Empty<string>();
        }

        public static bool HasTag(int itemId, string tag)
        {
            tag = Normalize(tag);
            return _tags.TryGetValue(itemId, out var set) && set.Contains(tag);
        }

        public static bool HasAnyTags(int itemId)
        {
            return _tags.TryGetValue(itemId, out var set) && set.Count > 0;
        }

        // ── Serialization (called by TagPlayer) ───────────────────────────────

        /// <summary>
        /// Returns a flat copy of the tag dictionary for saving.
        /// </summary>
        public static Dictionary<int, HashSet<string>> GetAllTags() => _tags;

        /// <summary>
        /// Replaces the entire tag dictionary on load.
        /// </summary>
        public static void LoadAllTags(Dictionary<int, HashSet<string>> saved)
        {
            _tags = saved ?? new Dictionary<int, HashSet<string>>();
        }

        /// <summary>
        /// Called on player unload to wipe runtime state so tags don't
        /// bleed between characters in the same session.
        /// </summary>
        public static void Clear()
        {
            _tags.Clear();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Lowercase, trimmed. Tags are case-insensitive.
        /// </summary>
        public static string Normalize(string tag) => tag?.Trim().ToLowerInvariant() ?? "";
    }
}
