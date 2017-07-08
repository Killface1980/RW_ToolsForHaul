using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToolsForHaul.Things
{
    using SpotlightTurret;

    using UnityEngine;

    using Verse;

    class SpotlightStuff
    {
        public enum LightMode
        {
            Conic,
            Automatic,
            Fixed
        }
        public const int updatePeriodInTicks = 30;
        public int updateOffsetInTicks = 0;
        public Thing light = null;

        // Light mode, spotlight range and rotation.
        public LightMode lightMode = LightMode.Conic;
        public const float spotlightMinRange = 5f;
        public const int spotLightRangeRateInTicksIdle = 16;          // Rate at which range is modified by 1 when idle.
        public const int spotLightRangeRateInTicksIdleTargetting = 8; // Rate at which range is modified by 1 when targetting.
        public float spotLightRangeBaseOffset = 15f;
        public float spotLightRange = 15f;
        public float spotLightRangeTarget = 15f;

        public const int spotLightRotationRateInTicksIdle = 4;       // Rate at which rotation is changed by 1° when idle.
        public const int spotLightRotationRateInTicksTargetting = 1; // Rate at which rotation is changed by 1° when targetting.
        public float spotLightRotationBaseOffset = 0f;
        public float spotLightRotation = 0f;
        public float spotLightRotationTarget = 0f;
        public bool spotLightRotationTurnRight = true;

        public const int idlePauseDurationInTicks = 3 * GenTicks.TicksPerRealSecond;
        public int idlePauseTicks = 1;
        
        // Textures.
        public static Material spotlightOnTexture = MaterialPool.MatFrom("Things/Building/SpotlightTurret_SpotlightOn");
        public Matrix4x4 spotlightMatrix = default(Matrix4x4);
        public Vector3 spotlightScale = new Vector3(5f, 1f, 5f);
        public static Material spotlightOffTexture = MaterialPool.MatFrom("Things/Building/SpotlightTurret_SpotlightOff");
        public Matrix4x4 spotlightLightEffectMatrix = default(Matrix4x4);
        public Vector3 spotlightLightEffectScale = new Vector3(5f, 1f, 5f);
        public static Material spotlightLightEffectTexture = MaterialPool.MatFrom("Things/Building/SpotlightTurret_LightEffect", ShaderDatabase.Transparent);
        public static Material targetLineTexture = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 1f, 1f));

        /// <summary>
        /// Power off the light.
        /// </summary>
        public void PowerOffLight()
        {
            if (this.light.DestroyedOrNull() == false)
            {
                this.light.Destroy();
            }

            this.light = null;
        }

        /// <summary>
        /// Light an area at given position.
        /// </summary>
        public void LightAreaAt(IntVec3 position)
        {
            // Remove old light if target has moved.
            if ((this.light.DestroyedOrNull() == false) && (position != this.light.Position))
            {
                PowerOffLight();
            }

            // Spawn a new light.
            if (this.light.DestroyedOrNull())
            {
                this.light = GenSpawn.Spawn(Util_SpotlightTurret.spotlightLightDef, position, this.Map);
            }
        }

        /// <summary>
        /// Start a new idle motion when turret is paused for a moment.
        /// </summary>
        public void IdleTurnTick()
        {
            if (this.idlePauseTicks > 0)
            {
                this.idlePauseTicks--;
                if (this.idlePauseTicks == 0)
                {
                    // Start a new idle motion.
                    StartNewIdleMotion();
                }
            }
        }

        /// <summary>
        /// Compute spotlight target rotation, target range and rotation direction.
        /// </summary>
        public void StartNewIdleMotion()
        {
            switch (this.lightMode)
            {
                case LightMode.Automatic:
                    this.spotLightRotationTarget = (float)Rand.Range(0, 360);
                    this.spotLightRangeTarget = Rand.Range(spotlightMinRange, this.def.specialDisplayRadius);
                    break;
                case LightMode.Conic:
                    if (this.spotLightRotation == Mathf.Repeat(
                            this.Rotation.AsAngle + this.spotLightRotationBaseOffset - 45f,
                            360f))
                    {
                        // Spotlight is targeting the left. Now, target the right.
                        this.spotLightRotationTarget = Mathf.Repeat(
                            this.Rotation.AsAngle + this.spotLightRotationBaseOffset + 45f,
                            360f);
                    }
                    else
                    {
                        // Spotlight is targeting the right. Now, target the left.
                        this.spotLightRotationTarget = Mathf.Repeat(
                            this.Rotation.AsAngle + this.spotLightRotationBaseOffset - 45f,
                            360f);
                    }

                    this.spotLightRangeTarget = this.spotLightRangeBaseOffset;
                    break;
                case LightMode.Fixed:
                    // Fixed range and rotation.
                    this.spotLightRotationTarget = Mathf.Repeat(
                        this.Rotation.AsAngle + this.spotLightRotationBaseOffset,
                        360f);
                    this.spotLightRangeTarget = this.spotLightRangeBaseOffset;
                    break;
            }

            // Compute rotation direction.
            ComputeRotationDirection();
        }

        /// <summary>
        /// Compute the optimal rotation direction.
        /// </summary>
        public void ComputeRotationDirection()
        {
            if (this.spotLightRotationTarget >= this.spotLightRotation)
            {
                float dif = this.spotLightRotationTarget - this.spotLightRotation;
                if (dif <= 180f)
                {
                    this.spotLightRotationTurnRight = true;
                }
                else
                {
                    this.spotLightRotationTurnRight = false;
                }
            }
            else
            {
                float dif = this.spotLightRotation - this.spotLightRotationTarget;
                if (dif <= 180f)
                {
                    this.spotLightRotationTurnRight = false;
                }
                else
                {
                    this.spotLightRotationTurnRight = true;
                }
            }
        }

        /// <summary>
        /// Update spotlight to face the target.
        /// </summary>
        public void SpotlightMotionTick()
        {
            // Update spotlight rotation.
            if (this.spotLightRotation != this.spotLightRotationTarget)
            {
                float rotationRate = spotLightRotationRateInTicksIdle;
                if (this.CurrentTarget != null)
                {
                    rotationRate = spotLightRotationRateInTicksTargetting;
                }
                else
                {
                    float deltaAngle = ComputeAbsoluteAngleDelta(this.spotLightRotation, this.spotLightRotationTarget);
                    if (deltaAngle < 20f)
                    {
                        rotationRate *= 2;
                    }
                }

                if ((Find.TickManager.TicksGame % rotationRate) == 0)
                {
                    if (this.spotLightRotationTurnRight)
                    {
                        this.spotLightRotation = Mathf.Repeat(this.spotLightRotation + 1f, 360f);
                    }
                    else
                    {
                        this.spotLightRotation = Mathf.Repeat(this.spotLightRotation - 1f, 360f);
                    }
                }
            }

            // Update spotlight range.
            if (this.spotLightRange != this.spotLightRangeTarget)
            {
                float rangeRate = spotLightRangeRateInTicksIdle;
                if (this.CurrentTarget != null)
                {
                    rangeRate = spotLightRangeRateInTicksIdleTargetting;
                }

                if ((Find.TickManager.TicksGame % rangeRate) == 0)
                {
                    if (Mathf.Abs(this.spotLightRangeTarget - this.spotLightRange) < 1f)
                    {
                        this.spotLightRange = this.spotLightRangeTarget;
                    }
                    else if (this.spotLightRange < this.spotLightRangeTarget)
                    {
                        this.spotLightRange++;
                    }
                    else
                    {
                        this.spotLightRange--;
                    }
                }
            }

            if ((this.CurrentTarget == null)
                && (this.idlePauseTicks == 0)
                && (this.spotLightRotation == this.spotLightRotationTarget)
                && (this.spotLightRange == this.spotLightRangeTarget))
            {
                // Motion is finished, start pause.
                this.idlePauseTicks = idlePauseDurationInTicks;
            }

            // Light the area in front of the spotlight.
            Vector3 lightVector3 = new Vector3(0, 0, this.spotLightRange).RotatedBy(this.spotLightRotation);
            IntVec3 lightIntVec3 = new IntVec3(Mathf.RoundToInt(lightVector3.x), 0, Mathf.RoundToInt(lightVector3.z));
            IntVec3 spotlightTarget = this.Position + lightIntVec3;
            IntVec3 farthestPosition = GetFarthestPositionInSight(spotlightTarget);
            LightAreaAt(farthestPosition);
        }

        /// <summary>
        /// Compute the absolute delta angle between two angles.
        /// </summary>
        public float ComputeAbsoluteAngleDelta(float angle1, float angle2)
        {
            float absoluteDeltaAngle = Mathf.Abs(angle2 - angle1) % 360f;
            if (absoluteDeltaAngle > 180f)
            {
                absoluteDeltaAngle -= 180f;
            }

            return absoluteDeltaAngle;
        }

        /// <summary>
        /// Get the farthest position from the turret in direction of spotlightTarget.
        /// </summary>
        public IntVec3 GetFarthestPositionInSight(IntVec3 spotlightTarget)
        {
            IntVec3 farthestPosition = this.Position;

            Mathf.Clamp(spotlightTarget.x, 0, this.Map.Size.x);
            Mathf.Clamp(spotlightTarget.z, 0, this.Map.Size.z);

            IEnumerable<IntVec3> lineOfSightPoints = GenSight.PointsOnLineOfSight(this.Position, spotlightTarget);
            foreach (IntVec3 point in lineOfSightPoints)
            {
                if (point.CanBeSeenOverFast(this.Map) == false)
                {
                    // Return last non-blocked position.
                    return farthestPosition;
                }

                farthestPosition = point; // Store last valid point in sight.
            }

            if (spotlightTarget.CanBeSeenOverFast(this.Map))
            {
                // Nothing is blocking.
                return spotlightTarget;
            }
            else
            {
                // Target position is blocked. Return last non-blocked position.
                return farthestPosition;
            }
        }


        /// <summary>
        /// Reset the light and immediately start an idle turn.
        /// </summary>
        public void ResetLight()
        {
            this.spotLightRotationTarget = this.spotLightRotation;
            this.spotLightRangeTarget = this.spotLightRange;
            this.idlePauseTicks = 1;
        }

        /// <summary>
        /// Switch light mode.
        /// </summary>
        public void SwitchLigthMode()
        {
            ResetLight();
            switch (this.lightMode)
            {
                case LightMode.Automatic:
                    this.lightMode = LightMode.Conic;
                    break;
                case LightMode.Conic:
                    this.lightMode = LightMode.Fixed;
                    break;
                case LightMode.Fixed:
                    this.lightMode = LightMode.Automatic;
                    break;
            }
        }

        /// <summary>
        /// Add an offset to the spotlight base rotation.
        /// </summary>
        public void AddSpotlightBaseRotationLeftOffset()
        {
            this.spotLightRotationBaseOffset = Mathf.Repeat(this.spotLightRotationBaseOffset - 10f, 360f);
            ResetLight();
        }

        /// <summary>
        /// Add an offset to the spotlight base rotation.
        /// </summary>
        public void AddSpotlightBaseRotationRightOffset()
        {
            this.spotLightRotationBaseOffset = Mathf.Repeat(this.spotLightRotationBaseOffset + 10f, 360f);
            ResetLight();
        }

        /// <summary>
        /// Decrease the spotlight range.
        /// </summary>
        public void DecreaseSpotlightRange()
        {
            if (this.spotLightRangeBaseOffset > spotlightMinRange)
            {
                this.spotLightRangeBaseOffset -= 1f;
            }

            ResetLight();
        }

        /// <summary>
        /// Increase the spotlight range.
        /// </summary>
        public void IncreaseSpotlightRange()
        {
            if (this.spotLightRangeBaseOffset < Mathf.Round(this.def.specialDisplayRadius))
            {
                this.spotLightRangeBaseOffset += 1f;
            }

            ResetLight();
        }
    }
}
