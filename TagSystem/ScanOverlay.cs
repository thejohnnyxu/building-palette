using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace BuildingPalette
{
    public class ScanOverlay : ModSystem
    {
        private const int TileSize = 16;
        private const int MaxSize  = 100;
        private const int BorderPx = 2;

        private static readonly Color ColBox    = new Color(80,  200, 255, 180);
        private static readonly Color ColFill   = new Color(40,  120, 200, 35);
        private static readonly Color ColCorner = new Color(160, 240, 255, 230);
        private static readonly Color ColCursor = new Color(255, 220, 60,  200);
        private static readonly Color ColCapped = new Color(255, 100, 60,  220);

        public override void PostDrawTiles()
        {
            if (TileScan.State == TileScan.ScanState.Idle) return;
            if (Main.LocalPlayer == null) return;

            // Use the game's own view matrix — this is what the tile renderer uses
            // and correctly accounts for zoom level and screen scale.
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            if (TileScan.State == TileScan.ScanState.WaitingPoint1)
                DrawCursorHighlight();
            else if (TileScan.State == TileScan.ScanState.WaitingPoint2)
                DrawSelectionBox();

            Main.spriteBatch.End();
        }

        private static void DrawCursorHighlight()
        {
            int tx = Player.tileTargetX;
            int ty = Player.tileTargetY;
            DrawBorderRect(TileToWorld(tx, ty, 1, 1), ColCursor, Color.Transparent, BorderPx);
        }

        private static void DrawSelectionBox()
        {
            int tx = Player.tileTargetX;
            int ty = Player.tileTargetY;
            var p1 = TileScan.Point1;

            int x1 = System.Math.Min(p1.X, tx);
            int y1 = System.Math.Min(p1.Y, ty);
            int x2 = System.Math.Max(p1.X, tx);
            int y2 = System.Math.Max(p1.Y, ty);

            bool cappedX = (x2 - x1) > MaxSize;
            bool cappedY = (y2 - y1) > MaxSize;
            if (cappedX) x2 = x1 + MaxSize;
            if (cappedY) y2 = y1 + MaxSize;

            int w = x2 - x1 + 1;
            int h = y2 - y1 + 1;
            Color borderCol = (cappedX || cappedY) ? ColCapped : ColBox;

            var rect = TileToWorld(x1, y1, w, h);
            DrawRect(rect, ColFill);
            DrawBorderRect(rect, borderCol, Color.Transparent, BorderPx);

            int cs = 6;
            DrawRect(new Rectangle(rect.Left,        rect.Top,         cs, cs), ColCorner);
            DrawRect(new Rectangle(rect.Right - cs,  rect.Top,         cs, cs), ColCorner);
            DrawRect(new Rectangle(rect.Left,        rect.Bottom - cs, cs, cs), ColCorner);
            DrawRect(new Rectangle(rect.Right - cs,  rect.Bottom - cs, cs, cs), ColCorner);

            DrawBorderRect(TileToWorld(p1.X, p1.Y, 1, 1), ColCorner, Color.Transparent, BorderPx);
        }

        // When using TransformationMatrix, draw in world space directly.
        // The matrix handles the conversion from world coords to screen pixels.
        private static Rectangle TileToWorld(int tileX, int tileY, int tilesW, int tilesH)
        {
            int wx = (int)(tileX * TileSize - Main.screenPosition.X);
            int wy = (int)(tileY * TileSize - Main.screenPosition.Y);
            return new Rectangle(wx, wy, tilesW * TileSize, tilesH * TileSize);
        }

        private static void DrawRect(Rectangle r, Color color)
        {
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, r, color);
        }

        private static void DrawBorderRect(Rectangle r, Color border, Color fill, int t)
        {
            if (fill.A > 0)
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, r, fill);
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.X, r.Y, r.Width, t), border);
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.X, r.Bottom - t, r.Width, t), border);
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.X, r.Y, t, r.Height), border);
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(r.Right - t, r.Y, t, r.Height), border);
        }
    }
}
