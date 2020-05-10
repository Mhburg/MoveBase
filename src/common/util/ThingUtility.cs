using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MoveBase
{
    public static class ThingUtility
    {
        public static HashSet<IntVec3> RoofInRange(this Thing thing)
        {
            HashSet<IntVec3> supportedRoof = new HashSet<IntVec3>();
            Map map = thing.MapHeld;
            map.floodFiller.FloodFill(
                thing.Position
                , (cell) => cell.InHorDistOf(thing.Position, RoofCollapseUtility.RoofMaxSupportDistance)
                , (cell) =>
                {
                    if (cell.Roofed(map))
                        supportedRoof.Add(cell);
                });

            return supportedRoof;
        }
    }
}
