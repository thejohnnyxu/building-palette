using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace BuildingPalette
{
    public class ScanCommand : ModCommand
    {
        public override string Command     => "scan";
        public override CommandType Type   => CommandType.Chat;
        public override string Description => "Enter scan mode: left-click two tiles to define a box and tag all blocks inside.";
        public override string Usage       => "/scan";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (TileScan.State != TileScan.ScanState.Idle)
            {
                TileScan.Cancel();
                return;
            }

            TileScan.Begin();
        }
    }
}
