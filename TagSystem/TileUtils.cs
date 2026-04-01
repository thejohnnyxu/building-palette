using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BuildingPalette
{
    public class TileUtilsSystem : ModSystem
    {
        public override void Unload() => TileUtils.InvalidateCache();
    }

    public static class TileUtils
    {
        // Fallback cache for plain terrain tiles with no TileObjectData
        private static Dictionary<int, int> _tileToItemCache = null;
        private static Dictionary<int, int> _wallToItemCache = null;

        public static void InvalidateCache()
        {
            _tileToItemCache = null;
            _wallToItemCache = null;
        }

        private static void EnsureCache()
        {
            if (_tileToItemCache != null) return;

            _tileToItemCache = new Dictionary<int, int>();
            _wallToItemCache = new Dictionary<int, int>();

            foreach (var kv in ContentSamples.ItemsByType)
            {
                var item = kv.Value;
                if (item == null || item.IsAir) continue;

                if (item.createTile >= 0 && item.tileWand <= 0)
                    if (!_tileToItemCache.ContainsKey(item.createTile))
                        _tileToItemCache[item.createTile] = kv.Key;

                if (item.createWall > 0)
                    if (!_wallToItemCache.ContainsKey(item.createWall))
                        _wallToItemCache[item.createWall] = kv.Key;
            }
        }

        /// <summary>
        /// Returns the item for the tile at world coordinates (x, y).
        /// Uses TileObjectData.GetTileStyle to resolve the exact variant —
        /// glass door vs regular door, granite wall vs stone wall, etc.
        /// Returns 0 if nothing found.
        /// </summary>
        public static int TileToItemAtCoords(int x, int y)
        {
            var tile = Main.tile[x, y];
            if (tile == null || !tile.HasTile) return 0;

            int tileType = tile.TileType;

            // GetTileStyle reads TileFrameX/Y against TileObjectData to determine
            // which style variant this tile is — critical for doors, furniture, etc.
            int style = TileObjectData.GetTileStyle(tile);
            if (style >= 0)
            {
                int drop = TileLoader.GetItemDropFromTypeAndStyle(tileType, style);
                if (drop > 0) return drop;
            }

            // Fallback for plain terrain tiles with no TileObjectData (dirt, stone, ore)
            EnsureCache();
            return _tileToItemCache.TryGetValue(tileType, out int id) ? id : 0;
        }

        /// <summary>
        /// Returns the item for a tile type without coordinate context.
        /// Cannot distinguish variants — prefer TileToItemAtCoords when you have coords.
        /// </summary>
        public static int TileToItem(int tileType)
        {
            if (tileType >= TileID.Count)
            {
                int drop = TileLoader.GetItemDropFromTypeAndStyle(tileType, 0);
                if (drop > 0) return drop;
            }

            EnsureCache();
            return _tileToItemCache.TryGetValue(tileType, out int id) ? id : 0;
        }

        /// <summary>
        /// Returns the item ID that places a given wall type.
        /// </summary>
        public static int WallToItem(int wallType)
        {
            if (wallType <= 0) return 0;
            EnsureCache();
            return _wallToItemCache.TryGetValue(wallType, out int id) ? id : 0;
        }
    }
}
