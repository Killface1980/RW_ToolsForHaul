using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ToolsForHaul
{
    public class CompMountable : ThingComp
    {
        private const string txtCommandDismountLabel = "CommandDismountLabel";
        private const string txtCommandDismountDesc = "CommandDismountDesc";
        private const string txtCommandMountLabel = "CommandMountLabel";
        private const string txtCommandMountDesc = "CommandMountDesc";
        private const string txtMountOn = "MountOn";
        private const string txtDismount = "Dismount";

        private Pawn driver;
        private Building_Door lastPassedDoor;
        private int tickLastDoorCheck = Find.TickManager.TicksGame;
        private const int TickCooldownDoorCheck = 96;
        private int tickCheck = Find.TickManager.TicksGame;
        private int tickCooldown = 60;

        public Sustainer sustainerAmbient;
        public CompDriver driverComp;

        private bool IsTank()
        {
            Vehicle_Turret vehicleCart = parent as Vehicle_Turret;
            if (vehicleCart != null)
                return true;
            return false;
        }

        public void MountOn(Pawn pawn)
        {
            if (driver != null)
                return;
            driver = pawn;

            Vehicle_Cart vehicleCart = parent as Vehicle_Cart;
            if (vehicleCart != null)
            {
                // Set faction of vehicle to whoever mounts it
                if (vehicleCart.Faction != driver.Faction && vehicleCart.ClaimableBy(driver.Faction))
                {
                    parent.SetFaction(driver.Faction);
                }

                if (!vehicleCart.compVehicles.AnimalsCanDrive())
                {
                    driver.RaceProps.makesFootprints = false;
                }
                if (vehicleCart.IsCurrentlyMotorized())
                {
                    SoundInfo info = SoundInfo.InWorld(parent);
                    sustainerAmbient = vehicleCart.compVehicles.compProps.soundAmbient.TrySpawnSustainer(info);
                }

                if (pawn.RaceProps.Humanlike)
                {
                    driverComp = new CompDriver { vehicleCart = parent as Vehicle_Cart };
                    driver.AllComps?.Add(driverComp);
                    driverComp.parent = driver;
                }
            }

            Vehicle_Turret vehicleTurret = parent as Vehicle_Turret;
            if (vehicleTurret != null)
            {
                // Set faction of vehicle to whoever mounts it
                if (vehicleTurret.Faction != driver.Faction && vehicleTurret.ClaimableBy(driver.Faction))
                {
                    parent.SetFaction(driver.Faction);
                }

                if (!vehicleTurret.compVehicles.AnimalsCanDrive())
                {
                    driver.RaceProps.makesFootprints = false;
                }
                if (vehicleTurret.IsCurrentlyMotorized())
                {
                    SoundInfo info = SoundInfo.InWorld(parent);
                    sustainerAmbient = vehicleTurret.compVehicles.compProps.soundAmbient.TrySpawnSustainer(info);
                }

                if (pawn.RaceProps.Humanlike)
                {
                    driverComp = new CompDriver { vehicleTurret = parent as Vehicle_Turret };
                    driver.AllComps?.Add(driverComp);
                    driverComp.parent = driver;
                }
            }


        }

        public bool IsMounted
        {
            get { return driver != null; }
        }

        public Pawn Driver
        {
            get { return driver; }
        }

        public void Dismount()
        {
            if (driver.RaceProps.Humanlike)
            {
                driver.AllComps?.Remove(driverComp);
                driverComp.vehicleCart = null;
                driverComp.vehicleTurret = null;
                driverComp.parent = null;
            }


            driver.RaceProps.makesFootprints = true;

            //if (Find.Reservations.IsReserved(parent, driver.Faction))
            Find.Reservations.ReleaseAllForTarget(parent);
            if (driver.Faction != Faction.OfPlayer)
            {
                parent.SetForbidden(true);
                parent.SetFaction(null);
            }

            driver = null;

            if (sustainerAmbient != null)
            {
                sustainerAmbient.End();
            }


            //  Find.ListerBuildings.Add(parent as Building);
        }

        public void DismountAt(IntVec3 dismountPos)
        {
            //if (driver.Position.IsAdjacentTo8WayOrInside(dismountPos, driver.Rotation, new IntVec2(1,1)))
            if (dismountPos != IntVec3.Invalid)
            {
                Dismount();
                parent.Position = dismountPos;
                return;
            }
            Log.Warning("Tried dismount at " + dismountPos);
        }

        public Vector3 InteractionOffset
        {
            get { return parent.def.interactionCellOffset.ToVector3().RotatedBy(driver.Rotation.AsAngle); }
        }

        public Vector3 Position
        {
            get
            {
                Vector3 position;
                if (IsTank())
                    position = driver.DrawPos + InteractionOffset * 1.3f;
                else
                    position = driver.DrawPos - InteractionOffset * 1.3f;

                //No driver
                if (driver == null)
                    return parent.DrawPos;
                //Out of bound or Preventing cart from stucking door
                if (!position.InBounds())
                    return driver.DrawPos;

                if (IsTank())
                    return driver.DrawPos + InteractionOffset * 1.3f;
                else
                    return driver.DrawPos - InteractionOffset * 1.3f;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.LookReference(ref driver, "driver");
            Scribe_References.LookReference(ref lastPassedDoor, "lastPassedDoor");
        }

        public override void CompTick()
        {
            base.CompTick();
            if (IsMounted)
            {

                if (!driver.Spawned)
                {
                    parent.DeSpawn();
                    return;
                }

                if (Find.TickManager.TicksGame - tickCheck >= tickCooldown)
                {
                    if (driver.Faction == Faction.OfPlayer && driver.CurJob != null)
                    {
                        if (driver.CurJob.def.playerInterruptible && (driver.CurJob.def == JobDefOf.DoBill ||
                                                                      driver.CurJob.def == JobDefOf.EnterCryptosleepCasket ||
                                                                      driver.CurJob.def == JobDefOf.LayDown ||
                                                                      driver.CurJob.def == JobDefOf.Lovin ||
                                                                      driver.CurJob.def == JobDefOf.MarryAdjacentPawn ||
                                                                      driver.CurJob.def == JobDefOf.Mate ||
                                                                      driver.CurJob.def == JobDefOf.PrisonerAttemptRecruit ||
                                                                      driver.CurJob.def == JobDefOf.Research ||
                                                                      driver.CurJob.def == JobDefOf.SocialRelax ||
                                                                      driver.CurJob.def == JobDefOf.SpectateCeremony ||
                                                                      driver.CurJob.def == JobDefOf.StandAndBeSociallyActive ||
                                                                      driver.CurJob.def == JobDefOf.TakeToBedToOperate ||
                                                                      driver.CurJob.def == JobDefOf.TendPatient ||
                                                                      driver.CurJob.def == JobDefOf.UseCommsConsole ||
                                                                      driver.CurJob.def == JobDefOf.UseNeurotrainer ||
                                                                      driver.CurJob.def == JobDefOf.VisitSickPawn ||
                                                                      driver.CurJob.def == JobDefOf.Shear ||

                                                                      driver.CurJob.def == JobDefOf.FeedPatient ||
                                                                      driver.CurJob.def == JobDefOf.PrisonerExecution ||
                                                                      driver.CurJob.def == JobDefOf.ManTurret ||
                                                                      driver.CurJob.def == JobDefOf.Train ||
                                                                      driver.health.NeedsMedicalRest ||
                                                                      driver.health.PrefersMedicalRest

                            ) && driver.Position.Roofed())
                        {
                            parent.Position = Position.ToIntVec3();
                            parent.Rotation = driver.Rotation;
                            if (!driver.Position.InBounds())
                            {
                                DismountAt(driver.Position);
                                return;
                            }
                            DismountAt(driver.Position - InteractionOffset.ToIntVec3());
                            driver.Position = driver.Position.RandomAdjacentCell8Way();
                            return;
                        }
                    }
                    tickCheck = Find.TickManager.TicksGame;
                    tickCooldown = Rand.RangeInclusive(60, 180);
                }
                if (Find.TickManager.TicksGame - tickLastDoorCheck >= 96 && (driver.Position.GetEdifice() is Building_Door || parent.Position.GetEdifice() is Building_Door))
                {
                    lastPassedDoor = (driver.Position.GetEdifice() is Building_Door ? driver.Position.GetEdifice() : parent.Position.GetEdifice()) as Building_Door;
                    lastPassedDoor.StartManualOpenBy(driver);
                    tickLastDoorCheck = Find.TickManager.TicksGame;
                }
                else if (Find.TickManager.TicksGame - tickLastDoorCheck >= 96 && lastPassedDoor != null)
                {
                    lastPassedDoor.StartManualCloseBy(driver);
                    lastPassedDoor = null;
                }
                parent.Position = Position.ToIntVec3();

                if (driver.pather.Moving)
                {
                    // Make sure the rotation isn't updated any more once the driver comes near the destination
                    if (!driver.Position.InHorDistOf(driver.pather.Destination.Cell, 0.4f))
                    {
                        parent.Rotation = driver.Rotation;
                    }
                    if (driver.Position.AdjacentTo8WayOrInside(driver.pather.Destination))
                    {
                        // Make the breaks sound once and throw some dust if driver comes to his destination
                        if (!soundPlayed)
                        {
                            SoundDef.Named("VehicleATV_Ambience_Break").PlayOneShot(driver.Position);
                            MoteMaker.ThrowDustPuff(driver.Position, 0.8f);
                            soundPlayed = true;
                        }
                    }
                }
                else
                {
                    soundPlayed = false;
                }

            }
        }

        private bool soundPlayed;

        private bool targetIsMoving
        {
            get
            {
                return driver != null && driver.pather != null && driver.pather.Moving;
            }
        }

        private bool targetIsNearDestination
        {
            get
            {
                Vehicle_Cart cart = parent as Vehicle_Cart;
                if (driver.Position.AdjacentTo8WayOrInside(driver.pather.Destination)) return true;
                return false;
            }
        }

        public override IEnumerable<Command> CompGetGizmosExtra()
        {
            foreach (Command compCom in base.CompGetGizmosExtra())
                yield return compCom;

            if (parent.Faction != Faction.OfPlayer)
                yield break;

            Command_Action com = new Command_Action();

            if (IsMounted)
            {
                com.defaultLabel = txtCommandDismountLabel.Translate();
                com.defaultDesc = txtCommandDismountDesc.Translate();
                com.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconUnmount");
                com.activateSound = SoundDef.Named("Click");
                com.action = Dismount;

                yield return com;
            }
            else
            {
                Designator_Mount designator = new Designator_Mount();

                designator.vehicle = parent;
                designator.defaultLabel = txtCommandMountLabel.Translate();
                designator.defaultDesc = txtCommandMountDesc.Translate();
                designator.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconMount");
                designator.activateSound = SoundDef.Named("Click");

                yield return designator;
            }

        }

        public IEnumerable<FloatMenuOption> CompGetFloatMenuOptionsForExtra(Pawn myPawn)
        {
            // order to drive
            Action action_Order;
            string verb;
            if (parent.Faction == Faction.OfPlayer)
            {
                if (!IsMounted)
                {
                    action_Order = () =>
                    {
                        Find.Reservations.ReleaseAllForTarget(parent);
                        Find.Reservations.Reserve(myPawn, parent);
                        Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("Mount"), parent);
                        myPawn.drafter.TakeOrderedJob(jobNew);
                    };
                    verb = txtMountOn;
                    yield return new FloatMenuOption(verb.Translate(parent.LabelShort), action_Order);
                }
                else if (IsMounted && myPawn == driver)// && !myPawn.health.hediffSet.HasHediff(HediffDef.Named("HediffWheelChair")))
                {
                    action_Order = () =>
                    {
                        Dismount();
                    };
                    verb = txtDismount;
                    yield return new FloatMenuOption(verb.Translate(parent.LabelShort), action_Order);
                }

            }
        }

    }
}