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
        public static HashSet<IntVec3> RoofInRange(this Building building)
        {
            HashSet<IntVec3> supportedRoof = new HashSet<IntVec3>();
            Map map = building.MapHeld;
            map.floodFiller.FloodFill(
                building.Position
                , (cell) => cell.InHorDistOf(building.Position, RoofCollapseUtility.RoofMaxSupportDistance)
                , (cell) =>
                {
                    if (cell.Roofed(map))
                        supportedRoof.Add(cell);
                }
                , extraRoots: building.OccupiedRect());

            return supportedRoof;
        }
    }
}
