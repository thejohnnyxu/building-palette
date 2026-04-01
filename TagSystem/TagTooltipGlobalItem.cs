using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace BuildingPalette
{
    /// <summary>
    /// GlobalItem that appends tag data to item tooltips.
    /// This is the entire Magic Storage integration — MS search indexes
    /// tooltip text, so tags become searchable for free.
    ///
    /// Format appended:  Tags: warm-stone, exterior, sandstone-palette
    /// </summary>
    public class TagTooltipGlobalItem : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!TagSystem.HasAnyTags(item.type)) return;

            // Tags are stored in a HashSet — join directly, no sort needed here.
            // Sorting at display time via OrderBy allocates on every hover; instead
            // we accept HashSet ordering (insertion order is undefined but stable
            // within a session, which is fine for tooltip display).
            var tags = TagSystem.GetTags(item.type);
            var tagString = string.Join(", ", tags);

            var line = new TooltipLine(Mod, "ItemTags", $"Tags: {tagString}")
            {
                OverrideColor = new Microsoft.Xna.Framework.Color(180, 180, 180)
            };

            tooltips.Add(line);
        }
    }
}
