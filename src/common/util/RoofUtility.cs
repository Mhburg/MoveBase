using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MoveBase
{
    /// <summary>
    /// Utility for roof.
    /// </summary>
    [PerfProfile]
    public static class RoofUtility
    {
        private static Dictionary<IntVec3, Building> _supportedRoof = new Dictionary<IntVec3, Building>();

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
            if (_supportedRoof.TryGetValue(roof, out Building building1) && !exceptions.Contains(building1))
                return true;

            bool supported = false;

            map.floodFiller.FloodFill(
                roof
                , (cell) => cell.Roofed(map) && cell.InHorDistOf(roof, RoofCollapseUtility.RoofMaxSupportDistance)
                , (cell) =>
                {
                    if (cell.GetEdifice(map) is Building building && building.def.holdsRoof && !exceptions.Contains(building))
                    {
                        _supportedRoof[roof] = building;
                        return supported = true;
                    }

                    return false;
                });

            return supported;
        }
    }
}
