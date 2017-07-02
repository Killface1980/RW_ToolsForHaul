using System;
namespace ToolsForHaul.Vehicles
{
    using UnityEngine;

    using Verse;

    public class Vehicle_CartGunTop
    {
        private const float IdleTurnDegreesPerTick = 0.26f;

        private const int IdleTurnDuration = 140;

        private const int IdleTurnIntervalMin = 150;

        private const int IdleTurnIntervalMax = 350;

        private Vehicle_Cart parentCart;

        private float curRotationInt;

        private int ticksUntilIdleTurn;

        private int idleTurnTicksLeft;

        private bool idleTurnClockwise;

        private float CurRotation
        {
            get
            {
                return this.curRotationInt;
            }
            set
            {
                this.curRotationInt = value;
                if (this.curRotationInt > 360f)
                {
                    this.curRotationInt -= 360f;
                }
                if (this.curRotationInt < 0f)
                {
                    this.curRotationInt += 360f;
                }
            }
        }

        public Vehicle_CartGunTop(Vehicle_Cart parentCart)
        {
            this.parentCart = parentCart;
        }

        public void CartTopTick()
        {
            LocalTargetInfo currentTarget = this.parentCart.CurrentTarget;
            if (currentTarget.IsValid)
            {
                float curRotation = (currentTarget.Cell.ToVector3Shifted() - this.parentCart.DrawPos).AngleFlat();
                this.CurRotation = curRotation;
                this.ticksUntilIdleTurn = Rand.RangeInclusive(IdleTurnIntervalMin, IdleTurnIntervalMax);
            }
            else if (this.ticksUntilIdleTurn > 0)
            {
                this.ticksUntilIdleTurn--;
                if (this.ticksUntilIdleTurn == 0)
                {
                    if (Rand.Value < 0.5f)
                    {
                        this.idleTurnClockwise = true;
                    }
                    else
                    {
                        this.idleTurnClockwise = false;
                    }
                    this.idleTurnTicksLeft = IdleTurnDuration;
                }
            }
            else
            {
                if (this.idleTurnClockwise)
                {
                    this.CurRotation += IdleTurnDegreesPerTick;
                }
                else
                {
                    this.CurRotation -= IdleTurnDegreesPerTick;
                }
                this.idleTurnTicksLeft--;
                if (this.idleTurnTicksLeft <= 0)
                {
                    this.ticksUntilIdleTurn = Rand.RangeInclusive(150, 350);
                }
            }
        }

        public void DrawLightTurret()
        {
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(this.parentCart.DrawPos + Altitudes.AltIncVect, this.CurRotation.ToQuat(), Vector3.one);
            Graphics.DrawMesh(MeshPool.plane20, matrix, this.parentCart.def.building.turretTopMat, 0);
        }
    }
}
