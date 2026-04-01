using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BuildingPalette
{
    /// <summary>
    /// Resets scan state on world exit so a mid-scan state doesn't persist.
    /// </summary>
    public class TileScanSystem : ModSystem
    {
        public override void OnWorldUnload() => TileScan.Reset();
    }

    public static class TileScan
    {
        public enum ScanState { Idle, WaitingPoint1, WaitingPoint2 }

        public static ScanState State    { get; private set; } = ScanState.Idle;
        public static Point     Point1   { get; private set; }

        private const int MaxSize = 100;

        public static void Begin()
        {
            State = ScanState.WaitingPoint1;
            Main.NewText("[Scan] Left-click a tile to set point 1.", Color.Cyan);
        }

        public static void Cancel()
        {
            State = ScanState.Idle;
            Main.NewText("[Scan] Cancelled.", Color.Gray);
        }

        /// <summary>Called on world exit — silently clears stale scan state.</summary>
        public static void Reset() => State = ScanState.Idle;

        public static void HandleClick(int tileX, int tileY)
        {
            if (State == ScanState.WaitingPoint1)
            {
                Point1 = new Point(tileX, tileY);
                State   = ScanState.WaitingPoint2;
                Main.NewText(
                    $"[Scan] Point 1 set at ({tileX}, {tileY}). Left-click to set point 2.",
                    Color.Cyan);
            }
            else if (State == ScanState.WaitingPoint2)
            {
                State = ScanState.Idle;
                var results = Scan(Point1, new Point(tileX, tileY));
                if (results.Count == 0)
                {
                    Main.NewText("[Scan] No blocks found in that area.", Color.Orange);
                    return;
                }
                Main.NewText(
                    $"[Scan] Found {results.Count} unique block type(s). Opening Tag Manager...",
                    Color.LightGreen);
                TagManagerUISystem.OpenWithScanResults(results);
            }
        }

        public static List<(int itemId, string name)> Scan(Point a, Point b)
        {
            int x1 = System.Math.Min(a.X, b.X);
            int y1 = System.Math.Min(a.Y, b.Y);
            int x2 = System.Math.Max(a.X, b.X);
            int y2 = System.Math.Max(a.Y, b.Y);

            if (x2 - x1 > MaxSize) x2 = x1 + MaxSize;
            if (y2 - y1 > MaxSize) y2 = y1 + MaxSize;

            x1 = System.Math.Max(0, x1);
            y1 = System.Math.Max(0, y1);
            x2 = System.Math.Min(Main.maxTilesX - 1, x2);
            y2 = System.Math.Min(Main.maxTilesY - 1, y2);

            var seen    = new System.Collections.Generic.HashSet<int>();
            var results = new List<(int, string)>();

            for (int x = x1; x <= x2; x++)
            {
                for (int y = y1; y <= y2; y++)
                {
                    var tile = Main.tile[x, y];
                    if (tile == null) continue;

                    // Foreground tile
                    if (tile.HasTile)
                    {
                        int tileType = tile.TileType;
                        if (seen.Add(tileType))
                        {
                            int itemId = TileUtils.TileToItemAtCoords(x, y);
                            if (itemId > 0 && ContentSamples.ItemsByType.TryGetValue(itemId, out var item)
                            && !string.IsNullOrEmpty(item?.Name))
                                results.Add((itemId, item.Name));
                        }
                    }

                    // Wall tile — use a separate seen set offset by a large value
                    if (tile.WallType > 0)
                    {
                        int wallKey = tile.WallType + 100000; // namespace walls away from tiles
                        if (seen.Add(wallKey))
                        {
                            int itemId = TileUtils.WallToItem(tile.WallType);
                            if (itemId > 0 && ContentSamples.ItemsByType.TryGetValue(itemId, out var wItem)
                            && !string.IsNullOrEmpty(wItem?.Name))
                                results.Add((itemId, wItem.Name));
                        }
                    }
                }
            }

            results.Sort((a2, b2) => string.Compare(
                a2.Item2, b2.Item2, System.StringComparison.OrdinalIgnoreCase));

            return results;
        }
    }
}
