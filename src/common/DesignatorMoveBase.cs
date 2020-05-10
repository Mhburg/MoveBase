using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MoveBase
{
    [StaticConstructorOnStartup]
    public class DesignatorMoveBase : Designator
    {
        private static Texture2D _icon = ContentFinder<Texture2D>.Get("UI/Designations/MoveBase");

        private static Rot4 _rotation = Rot4.North;
        private static bool _originFound = false;
        private static IntVec3 _origin = IntVec3.Zero;
        private static Dictionary<Thing, IntVec3> _ghostPos = new Dictionary<Thing, IntVec3>();
        private static List<RemoveRoofModel> _removeRoofModels = new List<RemoveRoofModel>();

        private static Mode _mode = Mode.Select;
        private static int _draggableDimension = 2;

        public bool KeepDesignation { get; set; } = false;
        public List<Thing> DesignatedThings { get; set; } = new List<Thing>();

        public override int DraggableDimensions => _draggableDimension;

        protected override DesignationDef Designation => MoveBaseDefOf.MoveBase;

        enum Mode
        {
            Select,
            Place,
        }

        /// <summary>
        /// Create instance of DesignatorMovebase.
        /// </summary>
        public DesignatorMoveBase()
        {
            this.icon = _icon;
            this.useMouseIcon = true;
            this.defaultDesc = "";
        }

        public static void ClearCache()
        {
            _removeRoofModels.Clear();
        }

        public static void ExposeData()
        {
            Scribe_Collections.Look(ref _removeRoofModels, nameof(_removeRoofModels), LookMode.Deep);
        }

        public static void Notify_Removing_Callback(Thing thing)
        {
            if (!(thing is Building building) || !building.def.holdsRoof || !building.Spawned)
                return;

            foreach (RemoveRoofModel model in _removeRoofModels)
            {
                if (model.BuildingsToReinstall.Contains(building))
                {
                    IntVec3 pos = thing.Position;
                    List<IntVec3> workingSet = new List<IntVec3>(model.RoofToRemove);
                    foreach (IntVec3 roof in workingSet)
                    {
                        if (pos.InHorDistOf(roof, RoofCollapseUtility.RoofMaxSupportDistance))
                        {
                            model.RoofToRemove.Remove(roof);
                            model.Map.areaManager.NoRoof[roof] = false;
                        }
                    }

                    model.BuildingsToReinstall.Remove(building);

                    break;
                }
            }
        }

        public static void AddBeingReinstalledBuilding(Building building)
        {
            if (building == null)
                return;

            foreach (RemoveRoofModel model in _removeRoofModels)
            {
                if (model.BuildingsToReinstall.Contains(building))
                {
                    model.BuildingsBeingReinstalled.Add(building);
                    break;
                }
            }
        }

        public static HashSet<Building> GetBuildingsBeingReinstalled(Building building)
        {
            if (building == null)
                return new HashSet<Building>();

            foreach (RemoveRoofModel model in _removeRoofModels)
            {
                if (model.BuildingsToReinstall.Contains(building))
                {
                    return model.BuildingsBeingReinstalled;
                }
            }

            return new HashSet<Building>();
        }

        public static void RemoveBuildingFromCache(Building building)
        {
            if (building == null)
                return;

            foreach (RemoveRoofModel model in _removeRoofModels)
            {
                if (model.BuildingsToReinstall.Contains(building))
                {
                    model.BuildingsBeingReinstalled.Remove(building);
                    model.BuildingsToReinstall.Remove(building);
                    if (!model.BuildingsToReinstall.Any() && !model.RoofToRemove.Any())
                    {
                        _removeRoofModels.Remove(model);
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// This method is invoked if this selector is selected.
        /// </summary>
        public override void Selected()
        {
            _mode = Mode.Select;
            _originFound = false;
            _origin = IntVec3.Zero;
            _rotation = Rot4.North;
            _draggableDimension = 2;
            DesignatedThings.Clear();
            _ghostPos.Clear();
            this.KeepDesignation = false;
        }

        /// <summary>
        /// This method is invoked when either a click or a drag from mouse is performed.
        /// </summary>
        /// <param name="loc"> cell on map. </param>
        /// <returns> Data model for acceptance. </returns>
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (_mode == Mode.Select)
                return this.CanDesignateThing(this.TopReinstallableInCell(loc));
            else
                return this.CanReinstallAllThings(loc);
        }

        public override void DoExtraGuiControls(float leftX, float bottomY)
        {
            Rect winRect = new Rect(leftX, bottomY - 90f, 200f, 90f);
            Find.WindowStack.ImmediateWindow(Rand.Int, winRect, WindowLayer.GameUI, delegate
            {
                RotationDirection rotationDirection = RotationDirection.None;
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                Rect rect = new Rect(winRect.width / 2f - 64f - 5f, 15f, 64f, 64f);
                if (Widgets.ButtonImage(rect, TexUI.RotLeftTex))
                {
                    SoundDefOf.DragSlider.PlayOneShotOnCamera();
                    rotationDirection = RotationDirection.Counterclockwise;
                    Event.current.Use();
                }
                Widgets.Label(rect, KeyBindingDefOf.Designator_RotateLeft.MainKeyLabel);
                Rect rect2 = new Rect(winRect.width / 2f + 5f, 15f, 64f, 64f);
                if (Widgets.ButtonImage(rect2, TexUI.RotRightTex))
                {
                    SoundDefOf.DragSlider.PlayOneShotOnCamera();
                    rotationDirection = RotationDirection.Clockwise;
                    Event.current.Use();
                }
                Widgets.Label(rect2, KeyBindingDefOf.Designator_RotateRight.MainKeyLabel);
                if (rotationDirection != 0)
                {
                    foreach (Thing thing in DesignatedThings)
                    {
                        _ghostPos[thing] = this.VectorRotation(_ghostPos[thing], rotationDirection);
                    }
                    _rotation.Rotate(rotationDirection);
                }
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            });
        }


        public override void SelectedUpdate()
        {
            GenDraw.DrawNoBuildEdgeLines();
            if (ArchitectCategoryTab.InfoRect.Contains(UI.MousePositionOnUIInverted))
                return;

            this.DrawGhostMatrix();
        }

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            if (_mode == Mode.Select)
            {
                Building building = t as Building;
                if (building == null)
                {
                    return false;
                }
                if (building.def.category != ThingCategory.Building)
                {
                    return false;
                }
                if (!building.def.Minifiable)
                {
                    return false;
                }
                if (!DebugSettings.godMode && building.Faction != Faction.OfPlayer)
                {
                    if (building.Faction != null)
                    {
                        return false;
                    }
                    if (!building.ClaimableBy(Faction.OfPlayer))
                    {
                        return false;
                    }
                }
                if (base.Map.designationManager.DesignationOn(t, Designation) != null)
                {
                    return false;
                }
                if (base.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
                {
                    return false;
                }
                return true;
            }

            return false;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            if (_mode == Mode.Select)
            {
                Thing thing = TopReinstallableInCell(c);
                if (thing != null)
                {
                    if (_originFound == false)
                    {
                        _originFound = true;
                        _origin = c;
                    }

                    DesignatedThings.Add(thing);
                    DesignateThing(thing);
                }
            }
            else if (_mode == Mode.Place)
            {
                IntVec3 mousePos = UI.MouseCell();
                foreach (Thing thing in DesignatedThings)
                {
                    GenConstruct.PlaceBlueprintForReinstall((Building)thing, this.GetDeltaCell(thing, mousePos), thing.MapHeld, this.GetRotation(thing.Rotation), Faction.OfPlayer);
                }

                RemoveRoofIfCollapse(DesignatedThings.OfType<Building>(), DesignatedThings.First().MapHeld);

                this.KeepDesignation = true;
                Find.DesignatorManager.Deselect();
            }
        }

        public override void DesignateThing(Thing t)
        {
            if (t == null)
                return;

            if (t.Faction != Faction.OfPlayer)
                t.SetFaction(Faction.OfPlayer);

            Designation designation = new Designation(t, this.Designation);
            base.Map.designationManager.AddDesignation(new Designation(t, this.Designation));
            _ghostPos[t] = t.Position - _origin;
        }

        public override void DrawMouseAttachments()
        {
            if (_mode == Mode.Select)
                base.DrawMouseAttachments();
            else
                this.DrawGhostMatrix();
        }

        public override void SelectedProcessInput(Event ev)
        {
            base.SelectedProcessInput(ev);
            if (DesignatedThings.Any() && _mode == Mode.Place)
            {
                HandleRotationShortcuts();
            }
        }

        /// <summary>
        /// Set no roof to false after roof is removed on <paramref name="cell"/>.
        /// </summary>
        /// <param name="cell"></param>
        /// <remarks> When no-roof is true, pawn will try to remove roof on that cell. </remarks>
        public static void SetNoRoofFalse(IntVec3 cell)
        {
            foreach (RemoveRoofModel model in _removeRoofModels)
            {
                if (model.RoofToRemove.Contains(cell))
                {
                    model.Map.areaManager.NoRoof[cell] = false;
                    model.RoofToRemove.Remove(cell);
                    if (!model.BuildingsToReinstall.Any() && !model.RoofToRemove.Any())
                    {
                        _removeRoofModels.Remove(model);
                    }

                    break;
                }
            }
        }

        public static void AddToRoofToRemove(IntVec3 roof, Thing thing)
        {
            _removeRoofModels
                .FirstOrDefault(model => model.BuildingsToReinstall.Contains(thing))
                ?.RoofToRemove
                .Add(roof);
        }

        /// <summary>
        /// This method is invoked when at least one thing is selected by this selector.
        /// </summary>
        protected override void FinalizeDesignationSucceeded()
        {
            _mode = Mode.Place;
            _draggableDimension = 0;
        }

        private Thing TopReinstallableInCell(IntVec3 loc)
        {
            foreach (Thing item in from t in base.Map.thingGrid.ThingsAt(loc)
                                   orderby t.def.altitudeLayer descending
                                   select t)
            {
                if (CanDesignateThing(item).Accepted)
                {
                    return item;
                }
            }

            return null;
        }

        private AcceptanceReport CanReinstallAllThings(IntVec3 mousePos)
        {
            AcceptanceReport result = AcceptanceReport.WasAccepted;
            this.TraverseDesignateThings(
                mousePos
                , (drawCell, thing) =>
                {
                    AcceptanceReport report = GenConstruct.CanPlaceBlueprintAt(thing.def, drawCell, this.GetRotation(thing.Rotation), thing.MapHeld, false, null, thing);
                    if (!report.Accepted)
                    {
                        result = report;
                        return true;
                    }

                    return false;
                });

            return result;
        }

        private AcceptanceReport CanReinstall(Thing thing, IntVec3 drawCell)
        {
            return GenConstruct.CanPlaceBlueprintAt(thing.def, drawCell, this.GetRotation(thing.Rotation), thing.MapHeld, false, null, thing);
        }

        private Rot4 GetRotation(Rot4 rot4)
        {
            return new Rot4(_rotation.AsInt + rot4.AsInt);
        }

        private IntVec3 GetDeltaCell(Thing thing, IntVec3 mousePos)
        {
            return new IntVec3(mousePos.x + _ghostPos[thing].x, mousePos.y, mousePos.z + _ghostPos[thing].z);
        }

        private void DrawGhostMatrix()
        {
            IntVec3 mousePos = UI.MouseCell();
            this.TraverseDesignateThings(
                mousePos
                , (drawCell, thing) =>
                {
                    this.DrawGhostThing(drawCell, thing);
                    return false;
                });
        }

        private void TraverseDesignateThings(IntVec3 mousePos, Func<IntVec3, Thing, bool> func)
        {
            foreach (Thing thing in DesignatedThings)
            {
                if (func(this.GetDeltaCell(thing, mousePos), thing))
                    break;
            }
        }

        private void DrawGhostThing(IntVec3 cell, Thing thing)
        {
            Graphic baseGraphic = thing.Graphic.ExtractInnerGraphicFor(thing);
            Color color = this.CanReinstall(thing, cell).Accepted ? Designator_Place.CanPlaceColor : Designator_Place.CannotPlaceColor;
            GhostDrawer.DrawGhostThing(cell, this.GetRotation(thing.Rotation), thing.def, baseGraphic, color, AltitudeLayer.Blueprint, thing);
        }

        private IntVec3 VectorRotation(IntVec3 cell, RotationDirection rotationDirection)
        {
            switch (rotationDirection)
            {
                case RotationDirection.Clockwise:
                    return new IntVec3(cell.z, cell.y, -cell.x);
                case RotationDirection.Counterclockwise:
                    return new IntVec3(-cell.z, cell.y, cell.x);
                default:
                    return cell;
            }
        }

        private void HandleRotationShortcuts()
        {
            RotationDirection rotationDirection = RotationDirection.None;
            if (KeyBindingDefOf.Designator_RotateRight.KeyDownEvent)
            {
                rotationDirection = RotationDirection.Clockwise;
            }
            else if (KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent)
            {
                rotationDirection = RotationDirection.Counterclockwise;
            }

            if (rotationDirection != RotationDirection.None)
            {
                foreach (Thing thing in DesignatedThings)
                {
                    _ghostPos[thing] = this.VectorRotation(_ghostPos[thing], rotationDirection);
                }
                _rotation.Rotate(rotationDirection);
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
            }
        }


        private static void RemoveRoofIfCollapse(IEnumerable<Building> buildings, Map map)
        {
            //HashSet<IntVec3> roofInRange = new HashSet<IntVec3>();
            //foreach (Building building in buildings)
            //{
            //    roofInRange.AddRange(building.RoofInRange());
            //}

            List<IntVec3> roofToRemove = new List<IntVec3>();
            _removeRoofModels.Add(new RemoveRoofModel(buildings.Where(b => b.def.holdsRoof).ToHashSet(), roofToRemove.ToHashSet(), map));
            //foreach (IntVec3 roof in roofInRange)
            //{
            //    bool supported = false;
            //    map.floodFiller.FloodFill(
            //        roof
            //        , (cell) => !supported && cell.InBounds(map) && cell.InHorDistOf(roof, RoofCollapseUtility.RoofMaxSupportDistance)
            //        , (cell) =>
            //        {
            //            if (cell.GetEdifice(map) is Building building && !things.Contains(building) && building.def.holdsRoof)
            //                supported = true;
            //        });

            //    if (!supported)
            //        roofToRemove.Add(roof);
            //}

            //if (roofToRemove.Any())
            //{
            //    _removeRoofModels.Add(new RemoveRoofModel(things.ToList(), roofToRemove.ToList(), map));
            //    foreach (IntVec3 cell in roofToRemove)
            //    {
            //        map.areaManager.NoRoof[cell] = true;
            //        map.areaManager.BuildRoof[cell] = false;
            //    }
            //}
        }

        private class RemoveRoofModel : IExposable
        {
            public HashSet<Building> BuildingsToReinstall;

            public HashSet<Building> BuildingsBeingReinstalled;

            public HashSet<IntVec3> RoofToRemove;

            public Map Map;

            public RemoveRoofModel()
            {
            }

            public RemoveRoofModel(HashSet<Building> buildingsToReinstall, HashSet<IntVec3> roofToRemove, Map map)
            {
                this.BuildingsToReinstall = buildingsToReinstall;
                this.RoofToRemove = roofToRemove;
                this.Map = map;
                this.BuildingsBeingReinstalled = new HashSet<Building>();
            }

            public void ExposeData()
            {
                Scribe_Collections.Look(ref this.BuildingsToReinstall, nameof(this.BuildingsToReinstall), LookMode.Reference);
                Scribe_Collections.Look(ref this.BuildingsBeingReinstalled, nameof(this.BuildingsBeingReinstalled), LookMode.Reference);
                Scribe_Collections.Look(ref this.RoofToRemove, nameof(RoofToRemove), LookMode.Value);
                Scribe_References.Look(ref this.Map, nameof(this.Map));
            }
        }
    }
}
