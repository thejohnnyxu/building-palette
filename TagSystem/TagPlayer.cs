using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BuildingPalette
{
    public class TagPlayer : ModPlayer
    {
        // Instance field — not static. Each player gets their own.
        // TagSystem delegates to Main.LocalPlayer's instance at runtime.
        public Dictionary<int, HashSet<string>> Tags = new();

        public override void Initialize()
        {
            Tags = new Dictionary<int, HashSet<string>>();
        }

        public override void SaveData(TagCompound tag)
        {
            if (Tags.Count == 0) return;

            var compound = new TagCompound();
            foreach (var (itemId, tagSet) in Tags)
            {
                if (tagSet.Count == 0) continue;
                compound[itemId.ToString()] = new List<string>(tagSet);
            }

            tag["itemTags"] = compound;
        }

        public override void LoadData(TagCompound tag)
        {
            Tags = new Dictionary<int, HashSet<string>>();

            if (!tag.ContainsKey("itemTags")) return;

            var compound = tag.Get<TagCompound>("itemTags");
            foreach (var pair in compound)
            {
                if (!int.TryParse(pair.Key, out int itemId)) continue;
                var tagList = compound.GetList<string>(pair.Key);
                if (tagList == null || tagList.Count == 0) continue;
                Tags[itemId] = new HashSet<string>(tagList);
            }
        }

        public override void OnEnterWorld()
        {
            // Sync local player's instance tags into TagSystem
            TagSystem.LoadAllTags(Tags);

            int count = Tags.Count;
            if (count > 0)
                Main.NewText($"[Building Palette] Loaded {count} tagged item(s).",
                    Microsoft.Xna.Framework.Color.Gray);
        }
    }
}
