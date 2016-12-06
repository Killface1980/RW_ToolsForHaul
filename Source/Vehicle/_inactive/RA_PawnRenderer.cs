using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace ToolsForHaul
{
    public class RA_PawnRenderer : PawnRenderer
    {
        public static FieldInfo infoPawn;
        public static FieldInfo infoJitterer;

        public RA_PawnRenderer(Pawn Pawn) : base(Pawn)
        {
        }

        public bool CarryWeaponOpenly
        {
            get
            {
                return (pawn.carrier?.CarriedThing == null) &&
                       (pawn.Drafted || (pawn.CurJob?.def.alwaysShowWeapon ?? false) ||
                        (pawn.mindState.duty?.def.alwaysShowWeapon ?? false));
            }
        }

        public bool Aiming()
        {
            Stance_Busy stance_Busy = pawn.stances.curStance as Stance_Busy;
            return stance_Busy != null && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid;
        }

        public Pawn pawn => DetourInjector.GetHiddenValue(typeof(PawnRenderer), this, "pawn", infoPawn) as Pawn;

        public JitterHandler Jitterer
            => DetourInjector.GetHiddenValue(typeof(Pawn_DrawTracker), pawn.Drawer, "jitterer", infoJitterer) as
                JitterHandler;

        private void DrawEquipment(Vector3 rootLoc)
        {
            if (pawn.Dead || !pawn.Spawned)
            {
                return;
            }

            if (pawn.equipment?.Primary == null)
            {
                return;
            }

            if (pawn.CurJob?.def.neverShowWeapon ?? false)
            {
                return;
            }

            Stance_Busy stance_Busy = pawn.stances.curStance as Stance_Busy;
            if (stance_Busy != null && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid)
            {
                Vector3 aimVector = stance_Busy.focusTarg.HasThing
                                        ? stance_Busy.focusTarg.Thing.DrawPos
                                        : stance_Busy.focusTarg.Cell.ToVector3Shifted();
                float num = 0f;
                if ((aimVector - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
                {
                    num = (aimVector - pawn.DrawPos).AngleFlat();
                }

                Vector3 drawLoc = rootLoc + new Vector3(0f, 0f, 0.4f).RotatedBy(num);
                drawLoc.y += 0.04f;

                // default weapon angle axis is upward, but all weapons are facing right, so we turn base weapon angle by 90°
                num -= 90f;
                DrawEquipmentAiming(pawn.equipment.Primary, drawLoc, num);
            }
            else if (CarryWeaponOpenly)
            {
                if (pawn.Rotation == Rot4.South)
                {
                    Vector3 drawLoc2 = rootLoc + new Vector3(0f, 0f, -0.22f);
                    drawLoc2.y += 0.04f;
                    DrawEquipmentAiming(pawn.equipment.Primary, drawLoc2, 0f);
                }
                else if (pawn.Rotation == Rot4.North)
                {
                    Vector3 drawLoc3 = rootLoc + new Vector3(0f, 0f, -0.11f);
                    DrawEquipmentAiming(pawn.equipment.Primary, drawLoc3, 0f);
                }
                else if (pawn.Rotation == Rot4.East)
                {
                    Vector3 drawLoc4 = rootLoc + new Vector3(0f, 0f, -0.22f);
                    drawLoc4.y += 0.04f;
                    DrawEquipmentAiming(pawn.equipment.Primary, drawLoc4, 0f);
                }
                else if (pawn.Rotation == Rot4.West)
                {
                    Vector3 drawLoc5 = rootLoc + new Vector3(0f, 0f, -0.22f);
                    drawLoc5.y += 0.04f;
                    DrawEquipmentAiming(pawn.equipment.Primary, drawLoc5, 180f);
                }
            }
        }

        // draws hands on equipment and adjusts aiming angle position, if corresponding Comp is specified
        public new void DrawEquipmentAiming(Thing equipment, Vector3 weaponDrawLoc, float aimAngle)
        {
            CompWeaponExtensions compWeaponExtensions = pawn.equipment.Primary.TryGetComp<CompWeaponExtensions>();

            float weaponAngle;
            Vector3 weaponPositionOffset = Vector3.zero;

            Mesh weaponMesh;
            bool flipped;
            bool aiming = Aiming();
            if (aimAngle > 110 && aimAngle < 250)
            {
                flipped = true;

                // flip weapon texture
                weaponMesh = MeshPool.GridPlaneFlip(equipment.Graphic.drawSize);

                if (!aiming && compWeaponExtensions != null)
                {
                    weaponPositionOffset += compWeaponExtensions.WeaponPositionOffset;

                    // flip x position offset
                    weaponPositionOffset.x = -weaponPositionOffset.x;
                }

                weaponAngle = aimAngle - 180f;
                weaponAngle -= !aiming
                    ? equipment.def.equippedAngleOffset
                    : (compWeaponExtensions?.AttackAngleOffset ?? 0);
            }
            else
            {
                flipped = false;

                weaponMesh = MeshPool.GridPlane(equipment.Graphic.drawSize);

                if (!aiming && compWeaponExtensions != null)
                    weaponPositionOffset += compWeaponExtensions.WeaponPositionOffset;

                weaponAngle = aimAngle;
                weaponAngle += !aiming
                    ? equipment.def.equippedAngleOffset
                    : (compWeaponExtensions?.AttackAngleOffset ?? 0);
            }

            if (pawn.Rotation == Rot4.West || pawn.Rotation == Rot4.North)
            {
                // draw weapon beneath the pawn
                weaponPositionOffset += new Vector3(0, -0.5f, 0);
            }

            // weapon angle and position offsets based on current attack animation sequence
            DoAttackAnimationOffsets(ref weaponAngle, ref weaponPositionOffset, flipped);

            Graphic_StackCount graphic_StackCount = equipment.Graphic as Graphic_StackCount;
            Material weaponMat = graphic_StackCount != null
                ? graphic_StackCount.SubGraphicForStackCount(1, equipment.def).MatSingle
                : equipment.Graphic.MatSingle;

            // draw weapon
            Graphics.DrawMesh(weaponMesh, weaponDrawLoc + weaponPositionOffset,
                Quaternion.AngleAxis(weaponAngle, Vector3.up),
                weaponMat, 0);

            // draw hands on equipment, if CompWeaponExtensions defines them
            if (compWeaponExtensions != null)
            {
                DrawHands(weaponAngle, weaponDrawLoc + weaponPositionOffset, compWeaponExtensions, flipped);
            }
        }

        public void DoAttackAnimationOffsets(ref float weaponAngle, ref Vector3 weaponPosition, bool flipped)
        {
            DamageDef damageDef = pawn.equipment?.PrimaryEq?.PrimaryVerb?.verbProps?.meleeDamageDef;
            if (damageDef != null)
            {
                // total weapon angle change during animation sequence
                int totalSwingAngle = 0;
                float animationPhasePercent = Jitterer.CurrentJitterOffset.magnitude / Jitterer.JitterMax;
                if (damageDef == DamageDefOf.Stab)
                {
                    weaponPosition += Jitterer.CurrentJitterOffset;

                    // + new Vector3(0, 0, Mathf.Pow(Jitterer.CurrentJitterOffset.magnitude, 0.25f))/2;
                }
                else if (damageDef == DamageDefOf.Blunt || damageDef == DamageDefOf.Cut)
                {
                    totalSwingAngle = 120;
                    weaponPosition += Jitterer.CurrentJitterOffset +
                                      new Vector3(0, 0,
                                          Mathf.Sin(Jitterer.CurrentJitterOffset.magnitude * Mathf.PI / Jitterer.JitterMax) /
                                          10);
                }

                weaponAngle += flipped
                    ? -animationPhasePercent * totalSwingAngle
                    : animationPhasePercent * totalSwingAngle;
            }
        }

        public void DrawHands(float weaponAngle, Vector3 weaponPosition, CompWeaponExtensions compWeaponExtensions,
            bool flipped)
        {
            Material handMat =
                GraphicDatabase.Get<Graphic_Single>("Overlays/Hand", ShaderDatabase.CutoutSkin, Vector2.one,
                    pawn.story.SkinColor).MatSingle;

            Mesh handsMesh = MeshPool.GridPlane(Vector2.one);

            if (compWeaponExtensions.FirstHandPosition != Vector3.zero)
            {
                Vector3 handPosition = compWeaponExtensions.FirstHandPosition;
                if (flipped)
                {
                    handPosition = -handPosition;

                    // keep z the same
                    handPosition.z = -handPosition.z;
                }

                Graphics.DrawMesh(handsMesh,
                    weaponPosition + handPosition.RotatedBy(weaponAngle),
                    Quaternion.AngleAxis(weaponAngle, Vector3.up), handMat, 0);
            }

            if (compWeaponExtensions.SecondHandPosition != Vector3.zero)
            {
                Vector3 handPosition = compWeaponExtensions.SecondHandPosition;
                if (flipped)
                {
                    handPosition = -handPosition;

                    // keep z the same
                    handPosition.z = -handPosition.z;
                }

                Graphics.DrawMesh(handsMesh,
                    weaponPosition + handPosition.RotatedBy(weaponAngle),
                    Quaternion.AngleAxis(weaponAngle, Vector3.up), handMat, 0);
            }

            //// for debug
            // var centerMat =
            // GraphicDatabase.Get<Graphic_Single>("Overlays/Hand", ShaderDatabase.CutoutSkin, Vector2.one,
            // Color.red).MatSingle;

            // Graphics.DrawMesh(handsMesh, weaponPosition + new Vector3(0, 0.001f, 0),
            // Quaternion.AngleAxis(weaponAngle, Vector3.up), centerMat, 0);
        }
    }
}