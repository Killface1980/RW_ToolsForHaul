using System;
using Combat_Realism;
using UnityEngine;
using Verse;

namespace ToolsForHaul
{
    public class Vehicle_TurretTop
    {
        private const float IdleTurnDegreesPerTick = 0.26f;

        private const int IdleTurnDuration = 140;

        private const int IdleTurnIntervalMin = 150;

        private const int IdleTurnIntervalMax = 350;

        private Vehicle_Turret parentTurret;

        private float curRotationInt;

        private int ticksUntilIdleTurn;

        private int idleTurnTicksLeft;

        private bool idleTurnClockwise;

        private float CurRotation
        {
            get
            {
                return curRotationInt;
            }
            set
            {
                curRotationInt = value;
                if (curRotationInt > 360f)
                {
                    curRotationInt -= 360f;
                }
                if (curRotationInt < 0f)
                {
                    curRotationInt += 360f;
                }
            }
        }

        public Vehicle_TurretTop(Vehicle_Turret ParentTurret)
        {
            parentTurret = ParentTurret;
        }



        public void TurretTopTick()
        {
            TargetInfo currentTarget = parentTurret.CurrentTarget;
            if (currentTarget.IsValid)
            {
                float curRotation = (currentTarget.Cell.ToVector3Shifted() - parentTurret.DrawPos).AngleFlat();
                CurRotation = curRotation;
                ticksUntilIdleTurn = Rand.RangeInclusive(IdleTurnIntervalMin, IdleTurnIntervalMax);
            }
            else if (ticksUntilIdleTurn > 0)
            {
                ticksUntilIdleTurn--;
                if (ticksUntilIdleTurn == 0)
                {
                    if (Rand.Value < 0.5f)
                    {
                        idleTurnClockwise = true;
                    }
                    else
                    {
                        idleTurnClockwise = false;
                    }
                    idleTurnTicksLeft = IdleTurnDuration;
                }
            }
            else
            {
                if (idleTurnClockwise)
                {
                    CurRotation += IdleTurnDegreesPerTick;
                }
                else
                {
                    CurRotation -= IdleTurnDegreesPerTick;
                }
                idleTurnTicksLeft--;
                if (idleTurnTicksLeft <= 0)
                {
                    ticksUntilIdleTurn = Rand.RangeInclusive(IdleTurnIntervalMin, IdleTurnIntervalMax);
                }
            }
        }

        public void DrawTurret()
        {
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(parentTurret.DrawPos + Altitudes.AltIncVect, CurRotation.ToQuat(), Vector3.one);
            Graphics.DrawMesh(MeshPool.plane20, matrix, parentTurret.def.building.turretTopMat, 0);
        }

        
    }
}
