using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MoveBase
{
    /// <summary>
    /// Utility for roof.
    /// </summary>
    public static class RoofUtility
    {
        /// <summary>
        /// Check if roof is supported by buildings other than those in <paramref name="exceptions"/>.
        /// </summary>
        /// <param name="roof"></param>
        /// <param name="map"></param>
        /// <param name="exceptions"></param>
        /// <returns></returns>
        /// <remarks>(141, 144) to (138, 138)</remarks>
        public static bool IsSupported(this IntVec3 roof, Map map, IEnumerable<Thing> exceptions)
        {
            bool supported = false;
            map.floodFiller.FloodFill(
                roof
                , (cell) => (cell.Roofed(map) || cell == roof) && cell.InHorDistOf(roof, RoofCollapseUtility.RoofMaxSupportDistance)
                , (cell) =>
                {
                    if (!supported && cell.GetEdifice(map) is Building building && building.def.holdsRoof && !exceptions.Contains(building))
                    {
                        supported = true;
                    }
                });

            return supported;
        }
    }
}
