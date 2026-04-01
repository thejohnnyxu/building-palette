using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BuildingPalette
{
    /// <summary>
    /// Handles per-player persistence of tags via tModLoader's TagCompound system.
    /// Tags are saved to the player's .tplr file and travel with the character.
    /// </summary>
    public class TagPlayer : ModPlayer
    {
        // ── tModLoader Hooks ─────────────────────────────────────────────────

        public override void SaveData(TagCompound tag)
        {
            // Serialize: Dictionary<int, HashSet<string>> → TagCompound
            // We store each itemId as a string key (TagCompound keys must be strings)
            // and each tag set as a List<string>.

            var allTags = TagSystem.GetAllTags();
            var compound = new TagCompound();

            foreach (var (itemId, tagSet) in allTags)
            {
                if (tagSet.Count == 0) continue;
                compound[itemId.ToString()] = new List<string>(tagSet);
            }

            tag["itemTags"] = compound;
        }

        public override void LoadData(TagCompound tag)
        {
            var result = new Dictionary<int, HashSet<string>>();

            if (!tag.ContainsKey("itemTags")) return;

            var compound = tag.Get<TagCompound>("itemTags");

            // TagCompound implements IEnumerable<KeyValuePair<string, object>>
            // — iterate as pairs rather than using .Keys
            foreach (var pair in compound)
            {
                if (!int.TryParse(pair.Key, out int itemId)) continue;

                var tagList = compound.GetList<string>(pair.Key);
                if (tagList == null || tagList.Count == 0) continue;

                result[itemId] = new HashSet<string>(tagList);
            }

            TagSystem.LoadAllTags(result);
        }

        public override void OnEnterWorld()
        {
            // Nothing needed here — LoadData fires before this.
            // Good place for future "welcome back" logic if needed.
        }

        public override void PlayerDisconnect()
        {
            // Wipe runtime state so tags don't bleed to the next
            // character loaded in the same session.
            TagSystem.Clear();
        }

        public override void PreSavePlayer()
        {
            // SaveData fires automatically; this is just a hook if
            // we need pre-save cleanup later.
        }
    }
}
