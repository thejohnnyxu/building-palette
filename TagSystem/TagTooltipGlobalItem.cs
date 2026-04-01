using System.Collections.Generic;
using System.Linq;
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

            var tags = TagSystem.GetTags(item.type);
            var tagString = string.Join(", ", tags.OrderBy(t => t));

            var line = new TooltipLine(Mod, "ItemTags", $"Tags: {tagString}")
            {
                // Soft grey so it's visible but clearly secondary info
                OverrideColor = new Microsoft.Xna.Framework.Color(180, 180, 180)
            };

            // Append after all vanilla tooltip lines
            tooltips.Add(line);
        }
    }
}
