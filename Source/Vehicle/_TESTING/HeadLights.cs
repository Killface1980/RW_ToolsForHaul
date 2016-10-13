using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ppumkin.LEDTechnology;
using ppumkin.LEDTechnology.GlowFlooders;
using UnityEngine;
using Verse;
using RimWorld;

namespace ToolsForHaul
{
    public class HeadLights : IGlowFlooder
    {
        public IntVec3 Position { get; set; }

        public Rot4 Orientation { get; set; }
        public Color32 Color { get; private set; }


        public List<GlowGridCache> ColorCellIndexCache { get; set; }
        public List<GlowGridCache> ColorCellIndexCacheOriginal { get; set; }
        //public List<FloodBlocker> FloodBlockers { get; set; }

        Thing[] innerArray;

        int targetDistance;
        int angleModulus;


        public HeadLights(IntVec3 position, Rot4 orientation, Vehicle_Cart vehicleCart)
        {
            
            Position = position;
            Orientation = orientation;

            //TODO move the colors to xml along with the angleModulus + targetDistance
            ColorCellIndexCache = new List<GlowGridCache>();
            //FloodBlockers = new List<FloodBlocker>();

            //Color = new Color32(191, 63, 191, 1);
            //Color = new Color32(254, 255, 179, 0);
            Color = new Color32(210, 210, 140, 0);


            innerArray = Find.EdificeGrid.InnerArray;

            targetDistance = 20;
            angleModulus = 4;  //0 is 90 and the higher you go the more narrow the angle. //angle 45 is every two tiles? - actually its 90 because left side is 0->45 and then right is 45<-0
        }

        public void CalculateGlowFlood()
        {
            calculateGrid();
        }

        //public void CalculateGlowFlood(bool forceGridRefresh)
        //{
        //    CalculateGlowFlood();
        //    Find.MapDrawer.MapMeshDirty(Position, MapMeshFlag.GroundGlow);
        //}


        public void Clear()
        {
            Color32 noColor = new Color32(0, 0, 0, 0);
            foreach (GlowGridCache i in ColorCellIndexCache)
            {
                Find.GlowGrid.glowGrid[i.CellGridIndex] = noColor;
                //Find.MapDrawer.MapMeshDirty(thingPosition, MapMeshFlag.GroundGlow);
            }
            ColorCellIndexCache = new List<GlowGridCache>();
        }


        private void calculateGrid()
        {
            //Log.Message("Anlge: Calc init");



            //Log.Message("Anlge: Power ON");

            //if (!LEDTools.IsGridGlowing(thisPosition))
            //    return;

            //Log.Message("Anlge: checking cache");
            if (ColorCellIndexCache.Count > 0)
                updateGlowGrid();

            //Log.Message("Anlge: Not cached - doing calc");
            //start tile
            addCellIndex(this.Position, false);
            //next tiles


            ///implement collision detector
            ///check is in bounds
            ///find a way to fix very far lengths

            bool completeBlocker = false;

            int distance = 1;
            int andgleDistanceDelta = 0;
            do
            {

                if (andgleDistanceDelta == 0) //TODO CHECK BLOCKERS HERE, DUHHH!
                    addCellIndex(Position.ToOffsetPositionDirection(distance, 0, this.Orientation), false);

                for (int angleDelta = andgleDistanceDelta * (-1); angleDelta <= andgleDistanceDelta; angleDelta++)
                {
                    //Log.Message("distance: " + distance + " xD:" + x + " zD:" + z + " mod:" + distance % 3);
                    IntVec3 _pos = Position.ToOffsetPositionDirection(distance, angleDelta, this.Orientation);
                    if (_pos.InBounds())
                    {
                        ////Log.Message("Block?: X:" + _pos.x + " Z: " + _pos.z);
                        if (isBlocked(_pos, angleDelta))
                        {
                            completeBlocker = true;
                            //addCellIndex(_pos, true); //it gets added in isBlocked method
                            continue; //end of road buddy, neeeeext!
                        }
                        else
                        {
                            completeBlocker = false;
                            //Log.Message("Not blocking: X:" + _pos.x + " Z: " + _pos.z);
                            addCellIndex(_pos, false);
                        }

                    }
                    else
                        break; //Ge'me oughta here!
                }

                //if (completeBlocker)
                //{
                //    //Log.Message("Blocker: Complete blocker detected at: " )
                //    break; //dont bother calculating any more, thats all folks.
                //}

                if (distance % angleModulus == 0) //bigger mod casues tighter angles
                    andgleDistanceDelta++;

                distance++;

            } while (distance < targetDistance);


            updateGlowGrid();
        }

        private bool isBlocked(IntVec3 position, int angleDelta)
        {
            Thing thingBlockers = innerArray[CellIndices.CellToIndex(position)];
            if (thingBlockers != null)
            {
                if (thingBlockers.def.blockLight)
                {
                    //block this tile
                    addCellIndex(position, true);
                    //block next forward tile to prevent further light going this way
                    addCellIndex(position.TranslateDirection(this.Orientation), true);

                    //block next left and right tiles to prevent further light going this way and help with "angle" detection
                    addCellIndex(position.TranslateDirection(this.Orientation, 1), true);
                    addCellIndex(position.TranslateDirection(this.Orientation, -1), true);
                }
                thingBlockers = null;
                //Log.Message("Blocking by Def: X: " + position.x + " Z: " + position.z);
                return true;
            }

            /////----------If def.blocked not found scan for previous blocks-------------//////


            //I pre emtifly block the cell at the building collision level so now I can find it here and carry on blocking
            //This part feels a bit iffy to me because I have to carry on calculation blockers. Not sure how flag any further
            //blockers in that particular direction to be forgotten about. possibly a shortfall in my calculation logic. Oh well..it works I suppose
            GlowGridCache thisCellBlocked = ColorCellIndexCache.FirstOrDefault(deltaZ => position == deltaZ.Position & deltaZ.IsBlocked);

            if (thisCellBlocked != null)
            {
                //block the next cell ahead and repeat el'kapitan!
                addCellIndex(thisCellBlocked.Position.TranslateDirection(this.Orientation), true);
                if (angleDelta < 0)
                {
                    //pffff.. a bit ugly i know.. but i need to compensate for wide angles over distance
                    addCellIndex(thisCellBlocked.Position.TranslateDirection(this.Orientation, -1), true);
                    //addCellIndex(thisCellBlocked.Position.TranslateDirection(this.Orientation, -2), true);
                    //addCellIndex(thisCellBlocked.Position.TranslateDirection(this.Orientation, -3), true);
                }
                if (angleDelta > 0)
                {
                    addCellIndex(thisCellBlocked.Position.TranslateDirection(this.Orientation, 1), true);
                    //addCellIndex(thisCellBlocked.Position.TranslateDirection(this.Orientation, 2), true);
                    //addCellIndex(thisCellBlocked.Position.TranslateDirection(this.Orientation, 3), true);
                }
            }



            return false;
        }




        public class FloodBlocker
        {
            public int Z { get; set; }
            public int X { get; set; }
            public Rot4 Direction { get; set; }

            public FloodBlocker(IntVec3 position)
            {
                this.X = position.x;
                this.Z = position.z;
            }

        }

        private void addCellIndex(IntVec3 position, bool isBlocked)
        {
            int _idx = position.AsCellIndex();

            //protect from multipoe entires of the same index. I know we can use an array but in this case we want an IEnumarable list
            if (!ColorCellIndexCache.Any(x => x.CellGridIndex == _idx))
            {
                ColorCellIndexCache.Add(new GlowGridCache()
                {
                    Position = position,
                    CellGridIndex = _idx,
                    ColorAtCellIndex = isBlocked ? new Color32(0, 0, 0, 0) : this.Color,
                    IsBlocked = isBlocked
                });
            }


            //Log.Message("ANGLE POS: " + position.ToLog());
        }

        private void updateGlowGrid()
        {
            //Log.Message("Angle: start update glow grid using cache");

            //this is why I wanted a list to and not an array, saves some valuable CPU overhead
            foreach (GlowGridCache cell in ColorCellIndexCache.Where(x => !x.IsBlocked))
            {
                Find.GlowGrid.glowGrid[cell.CellGridIndex] = cell.ColorAtCellIndex;
            }


            //for (int i = 0; i < ColorCellIndexCache.Count; i++)
            //{
            //    var pos = ColorCellIndexCache[i];
            //    Find.GlowGrid.glowGrid[pos.CellGridIndex] = pos.ColorAtCellIndex;

            //    //dont know if this is inneficeint or what but it works for long range lighting in this case
            //    //Find.MapDrawer.MapMeshDirty(pos.Position, MapMeshFlag.GroundGlow);
            //}

            //Log.Message("Anlge: updated glowgrid from cache");
            //Dont really want this here but the timing of this particulr object is wierd so I need to mark it dirty

            //In this case we need to mark several positions as dirty as the internal updated works with regions only
            Find.MapDrawer.MapMeshDirty(Position, MapMeshFlag.GroundGlow);
        }

        public override string ToString()
        {
            return "AngledGlower - x:" + Position.x.ToString() + " z:" + Position.z.ToString();
        }

    }
}



////if (andgleDistanceDelta == 0)
//                //    addCellIndex(LEDTools.OffsetPosition(Position, 0, distance));

//                if (andgleDistanceDelta == 0)
//                    addCellIndex(LEDTools.OffsetPositionDirection(Position, distance, 0, this.Orientation));

//                for (int angleDelta = andgleDistanceDelta * (-1); angleDelta <= andgleDistanceDelta; angleDelta++)
//                {
//                    //Log.Message("distance: " + distance + " xD:" + x + " zD:" + z + " mod:" + distance % 3);
//                    var _pos = LEDTools.OffsetPosition(Position, angleDelta, distance); //TODO: rotation should be trnsformed here


//                    if (_pos.InBounds())
//                    {
//                        //Log.Message("Block?: X:" + _pos.x + " Z: " + _pos.z);
//                        if (isBlocked(_pos, angleDelta, andgleDistanceDelta))
//                        {
//                            completeBlocker = true;
//                            continue;
//                        }

//                        completeBlocker = false;
//                        //Log.Message("Not blocking: X:" + _pos.x + " Z: " + _pos.z);
//                        addCellIndex(_pos);
//                    }
//                }

//                if (completeBlocker)
//                {
//                    //Log.Message("Blocker: Complete blocker detected at: " )
//                    break; //dont bother calculating any more, thats all folks.
//                }

//                if (distance % angleModulus == 0) //bigger mod casues tighter angles
//                    andgleDistanceDelta++;

//                distance++;

//            } while (distance < targetDistance);