using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace ToolsForHaul
{
    public class IncendiaryFuel : Filth
    {
        private const float maxFireSize = 1.75f;

        public override void SpawnSetup()
        {
            base.SpawnSetup();

            spawnTick = Find.TickManager.TicksGame;

            List<Thing> list = new List<Thing>(Position.GetThingList());
            foreach (Thing thing in list)
            {
                if (thing.HasAttachment(ThingDefOf.Fire))
                {
                    Fire fire = (Fire)thing.GetAttachment(ThingDefOf.Fire);
                    if (fire != null)
                        fire.fireSize = maxFireSize;
                }
              //else
              //{
              //    if (thing.HitPoints < 5)
              //    {
              //        thing.TryAttachFire(maxFireSize);
              //    }
              //}
            }
        }
        private int spawnTick;
        private int fireTick = -5000;

        public override void Tick()
        {
            if (Position.GetThingList().Any(x => x.def == ThingDefOf.FilthFireFoam))
            {
                if (!Destroyed)
                    Destroy();
            }
            else
            {

                if (this.HasAttachment(ThingDefOf.Fire) && Find.TickManager.TicksGame>fireTick)
                {
                    Fire fire = (Fire)this.GetAttachment(ThingDefOf.Fire);
                    if (fire != null)
                    {
                        fire.fireSize = maxFireSize;
                    }

                    fireTick = Find.TickManager.TicksGame + 200;
                }
            }
          //if (spawnTick + 15000 < Find.TickManager.TicksGame)
          //{
          //    Destroy(DestroyMode.Vanish);
          //}
        }
    }
}