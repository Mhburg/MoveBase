using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace MoveBase
{
    /// <summary>
    /// Patch the original so pawn won't start a reinstall job if the subject building is the only support for nearby roof.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class WorkGiver_ConstructDeliverResourcesToBlueprints_Patch
    {
        static WorkGiver_ConstructDeliverResourcesToBlueprints_Patch()
        {
            MethodInfo original = typeof(WorkGiver_ConstructDeliverResourcesToBlueprints).GetMethod("JobOnThing", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo postfix = typeof(WorkGiver_ConstructDeliverResourcesToBlueprints_Patch).GetMethod("Postfix", BindingFlags.Public | BindingFlags.Static);
            HarmonyUtility.Instance.Patch(original, postfix: new HarmonyMethod(postfix));
        }

        /// <summary>
        /// Postfix method.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="forced"></param>
        /// <param name="__result"></param>
        public static void Postfix(Thing t, bool forced, ref Job __result)
        {
            if (t is Blueprint_Install install && install.MiniToInstallOrBuildingToReinstall is Building building && building.def.holdsRoof)
            {
                if (forced)
                {
                    DesignatorMoveBase.AddBeingReinstalledBuilding(building);
                    return;
                }

                if (building.MapHeld.designationManager.DesignationOn(building, MoveBaseDefOf.MoveBase) != null)
                {
                    bool canRemove = true;
                    HashSet<IntVec3> roofInRange = building.RoofInRange();
                    IEnumerable<Building> buildingsBeingRemoved = DesignatorMoveBase.GetBuildingsBeingReinstalled(building).Concat(building);
                    foreach (IntVec3 roof in roofInRange)
                    {
                        if (!roof.IsSupported(building.MapHeld, buildingsBeingRemoved))
                        {
                            building.MapHeld.areaManager.NoRoof[roof] = true;
                            building.MapHeld.areaManager.BuildRoof[roof] = false;
                            DesignatorMoveBase.AddToRoofToRemove(roof, building);
                            canRemove = false;
                        }
                    }

                    if (canRemove)
                        DesignatorMoveBase.AddBeingReinstalledBuilding(building);
                    else
                        __result = null;
                }
            }
        }
    }
}
