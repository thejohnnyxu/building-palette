using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BuildingPalette
{
    public class TagPlayer : ModPlayer
    {
        // Ensure this ModPlayer is always saved even if no tags exist yet
        protected override bool CloneNewInstances => false;

        public override void SaveData(TagCompound tag)
        {
            var allTags = TagSystem.GetAllTags();
            if (allTags.Count == 0) return;

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

            if (!tag.ContainsKey("itemTags"))
            {
                TagSystem.LoadAllTags(result);
                return;
            }

            var compound = tag.Get<TagCompound>("itemTags");

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
            // LoadData fires before this — good place to confirm tags loaded
            Main.NewText($"[Building Palette] Loaded {TagSystem.GetAllTags().Count} tagged items.", 
                Microsoft.Xna.Framework.Color.Gray);
        }

        public override void PlayerDisconnect()
        {
            // SaveData fires automatically before disconnect.
            // Clear after so tags don't bleed to next character in same session.
            TagSystem.Clear();
        }
    }
}
