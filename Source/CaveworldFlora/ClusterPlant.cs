using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;   // Always needed
using RimWorld;      // RimWorld specific functions are found here
using Verse;         // RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI
using Verse.Sound;   // Needed when you do something with the Sound

namespace CaveworldFlora
{
    /// <summary>
    /// ClusterPlant class.
    /// </summary>
    /// <author>Rikiki</author>
    /// <permission>Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release thread.
    /// Remember learning is always better than just copy/paste...</permission>
    public class ClusterPlant : Plant
    {
        // TODO: use compFlickable instead of spawning glower buildings.

        public const float minGrowthToReproduce = 0.6f;
        private string cachedLabelMouseover = null;

        public ThingDef_ClusterPlant ClusterPlantProps
        {
            get
            {
                return (def as ThingDef_ClusterPlant);
            }
        }

        // Fertility.
        public float FertilityGrowthRateFactor
        {
            get
            {
                if (ClusterPlantProps.growOnlyOnRoughRock)
                {
                    return 1f;
                }
                return Map.fertilityGrid.FertilityAt(Position);
            }
        }
        public bool IsFertilityConditionOk
        {
            get
            {
                return (FertilityGrowthRateFactor > 0f);
            }
        }

        // Temperature.
        public bool IsInCryostasis
        {
            get
            {
                return (Position.GetTemperature(Map) < ClusterPlantProps.minGrowTemperature);
            }
        }
        public float TemperatureGrowthRateFactor
        {
            get
            {
                float temperature = Position.GetTemperature(Map);
                if ((temperature < ClusterPlantProps.minGrowTemperature)
                    || (temperature > ClusterPlantProps.maxGrowTemperature))
                {
                    return 0f;
                }
                else if (temperature < ClusterPlantProps.minOptimalGrowTemperature)
                {
                    return Mathf.InverseLerp(ClusterPlantProps.minGrowTemperature, ClusterPlantProps.minOptimalGrowTemperature, temperature);
                }
                else if (temperature > ClusterPlantProps.maxOptimalGrowTemperature)
                {
                    return Mathf.InverseLerp(ClusterPlantProps.maxGrowTemperature, ClusterPlantProps.maxOptimalGrowTemperature, temperature);
                }
                return 1f;
            }
        }
        public bool IsTemperatureConditionOk
        {
            get
            {
                return (TemperatureGrowthRateFactor > 0f);
            }
        }

        // Light.
        public float LightGrowthRateFactor
        {
            get
            {
                float light = Map.glowGrid.GameGlowAt(Position);
                if ((light >= ClusterPlantProps.minLight)
                    && (light <= ClusterPlantProps.maxLight))
                {
                    return 1f;
                }
                return 0f;
            }
        }
        public bool IsLightConditionOk
        {
            get
            {
                return (LightGrowthRateFactor > 0f);
            }
        }

        // Symbiosis.
        public bool IsSymbiosisOk
        {
            get
            {
                if (ClusterPlantProps.isSymbiosisPlant == false)
                {
                    return true;
                }
                return (cluster.symbiosisCluster.DestroyedOrNull() == false);
            }
        }

        // Glower.
        public Thing glower = null;

        // Cluster.
        public Cluster cluster;

        // Growth rate
        public new float GrowthRate
        {
            get
            {
                return FertilityGrowthRateFactor * TemperatureGrowthRateFactor * LightGrowthRateFactor;
            }
        }
        public new float GrowthPerTick
        {
            get
            {
                if (LifeStage != PlantLifeStage.Growing
                    || IsInCryostasis)
                {
                    return 0f;
                }
                float growthPerTick = (1f / (GenDate.TicksPerDay * def.plant.growDays));
                return growthPerTick * GrowthRate;
            }
        }

        // Plant grower and terrain.
        public bool IsOnCavePlantGrower
        {
            get
            {
                Building edifice = Position.GetEdifice(Map);
                if ((edifice != null)
                    && ((edifice.def == Util_CaveworldFlora.fungiponicsBasinDef)
                    || (edifice.def == ThingDef.Named("PlantPot"))))
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsOnValidNaturalSpot
        {
            get
            {
                bool isValidSpot = true;
                if (ClusterPlantProps.growOnlyOnRoughRock)
                {
                    isValidSpot &= IsOnNaturalRoughRock;
                }
                else
                {
                    isValidSpot &= IsFertilityConditionOk;
                }
                if (ClusterPlantProps.growOnlyUndeRoof)
                {
                    isValidSpot &= Map.roofGrid.Roofed(Position);
                }
                if (ClusterPlantProps.growOnlyNearNaturalRock)
                {
                    isValidSpot &= this.IsCellNearNaturalRockBlock;
                }
                return isValidSpot;
            }
        }

        /// <summary>
        /// Check if the terrain is valid. Require a rough terrain not constructible by player.
        /// </summary>
        public bool IsOnNaturalRoughRock
        {
            get
            {
                TerrainDef terrain = Map.terrainGrid.TerrainAt(Position);
                if ((terrain.layerable == false)
                    && terrain.defName.Contains("Rough"))
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsCellNearNaturalRockBlock
        {
            get
            {
                for (int xOffset = -2; xOffset <= 2; xOffset++)
                {
                    for (int zOffset = -2; zOffset <= 2; zOffset++)
                    {
                        IntVec3 checkedPosition = Position + new IntVec3(xOffset, 0, zOffset);
                        if (checkedPosition.InBounds(Map))
                        {
                            Thing potentialRock = Map.thingGrid.ThingAt(checkedPosition, ThingCategory.Building);
                            if ((potentialRock != null)
                                && ((potentialRock as Building) != null))
                            {
                                if ((potentialRock as Building).def.building.isNaturalRock)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
        }

        // ===================== Setup Work =====================
        /// <summary>
        /// Initialize instance variables.
        /// </summary>
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            //this.UpdateGlowerAccordingToGrowth(); // TODO: disabled to avoid ton of warning messages when getting temperature.
        }

        /// <summary>
        /// Save and load internal state variables (stored in savegame data).
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Cluster>(ref cluster, "cluster");
            Scribe_References.Look<Thing>(ref glower, "glower");
        }

        // ===================== Destroy =====================
        /// <summary>
        /// Destroy the plant and the associated glower if existing.
        /// </summary>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
            TryToDestroyGlower();
            if (cluster.DestroyedOrNull() == false)
            {
                cluster.NotifyPlantRemoved();
            }
        }

        // ===================== Main Work Function =====================
        /// <summary>
        /// Main function:
        /// - update the glower if necessary.
        /// - verify the cave plant is in good conditions to growth.
        /// - when the cave plant is too old, damage it over time.
        /// </summary>
        public override void TickLong()
        {
            if (IsGrowingNow)
            {
                bool plantWasAlreadyMature = (LifeStage == PlantLifeStage.Mature);
                growthInt += GrowthPerTick * GenTicks.TickLongInterval;
                if (!plantWasAlreadyMature
                    && (LifeStage == PlantLifeStage.Mature))
                {
                    // Plant just became mature.
                    Map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things);
                }
            }

            // Verify the plant is not in cryostasis.
            if (IsInCryostasis == false)
            {
                if (LifeStage == PlantLifeStage.Mature)
                {
                    ageInt += GenTicks.TickLongInterval;
                }
                if (Dying)
                {
                    base.TakeDamage(new DamageInfo(DamageDefOf.Rotting, 5, 0, -1, null, null, null));
                }
            }

            // Update glower.
            if (!base.Destroyed)
            {
                UpdateGlowerAccordingToGrowth();
            }

            cachedLabelMouseover = null;
        }

        public bool IsGrowingNow
        {
            get
            {
                return LifeStage == PlantLifeStage.Growing
                    && IsTemperatureConditionOk
                    && IsLightConditionOk
                    && (IsOnCavePlantGrower
                       || (IsOnValidNaturalSpot
                          && (cluster.DestroyedOrNull() == false)));
            }
        }
        public new bool Dying
        {
            get
            {
                if (IsInCryostasis)
                {
                    return false;
                }
                bool plantIsTooOld = def.plant.LimitedLifespan && (ageInt > def.plant.LifespanTicks);
                if (plantIsTooOld)
                {
                    return true;
                }
                float temperature = Position.GetTemperature(Map);
                bool plantCanGrowHere = IsOnCavePlantGrower
                    || ((cluster.DestroyedOrNull() == false)
                       && IsOnValidNaturalSpot);
                bool plantIsInHostileConditions = (temperature > ClusterPlantProps.maxGrowTemperature)
                    || !IsLightConditionOk
                    || !plantCanGrowHere
                    || !IsSymbiosisOk;
                if (plantIsInHostileConditions)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Update the glower according to the plant growth, cryostatis state and snow depth.
        /// </summary>
        public void UpdateGlowerAccordingToGrowth()
        {
            if ((ClusterPlantProps.hasStaticGlower == false)
                && (ClusterPlantProps.hasDynamicGlower == false))
            {
                return;
            }
            
            if (IsInCryostasis
                || (Position.GetSnowDepth(Map) >= def.hideAtSnowDepth))
            {
                TryToDestroyGlower();
                return;
            }

            if (ClusterPlantProps.hasStaticGlower)
            {
                if (glower.DestroyedOrNull())
                {
                    glower = GenSpawn.Spawn(Util_CaveworldFlora.GetGlowerStaticDef(def), Position, Map);
                }
            }
            else if (ClusterPlantProps.hasDynamicGlower)
            {
                if (Growth < 0.33f)
                {
                    if ((glower.DestroyedOrNull())
                        || (glower.def != Util_CaveworldFlora.GetGlowerSmallDef(def)))
                    {
                        TryToDestroyGlower();
                        glower = GenSpawn.Spawn(Util_CaveworldFlora.GetGlowerSmallDef(def), Position, Map);
                    }
                }
                else if (Growth < 0.66f)
                {
                    if ((glower.DestroyedOrNull())
                        || (glower.def != Util_CaveworldFlora.GetGlowerMediumDef(def)))
                    {
                        TryToDestroyGlower();
                        glower = GenSpawn.Spawn(Util_CaveworldFlora.GetGlowerMediumDef(def), Position, Map);
                    }
                }
                else
                {
                    if ((glower.DestroyedOrNull())
                        || (glower.def != Util_CaveworldFlora.GetGlowerBigDef(def)))
                    {
                        TryToDestroyGlower();
                        glower = GenSpawn.Spawn(Util_CaveworldFlora.GetGlowerBigDef(def), Position, Map);
                    }
                }
            }
        }

        protected void TryToDestroyGlower()
        {
            if (glower.DestroyedOrNull() == false)
            {
                glower.Destroy();
            }
            glower = null;
        }

        /// <summary>
        /// Build the inspect string.
        /// </summary>
        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("PercentGrowth".Translate(new object[]
            {
                    GrowthPercentString
            }));
            if (LifeStage == PlantLifeStage.Mature)
            {
                if (def.plant.Harvestable)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append("ReadyToHarvest".Translate());
                }
                else
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append("Mature".Translate());
                }
            }
            else if (LifeStage == PlantLifeStage.Growing)
            {
                if (IsInCryostasis)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append("CaveworldFlora.InCryostasis".Translate());
                }
                else if (Dying)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append("CaveworldFlora.Dying".Translate());
                    if (Position.GetTemperature(Map) > ClusterPlantProps.maxGrowTemperature)
                    {
                        stringBuilder.Append(", " + "CaveworldFlora.Drying".Translate());
                    }
                    if (IsLightConditionOk == false)
                    {
                        float light = Map.glowGrid.GameGlowAt(Position);
                        if (light < ClusterPlantProps.minLight)
                        {
                            stringBuilder.Append(", " + "CaveworldFlora.TooDark".Translate());
                        }
                        else if (light > ClusterPlantProps.maxLight)
                        {
                            stringBuilder.Append(", " + "CaveworldFlora.Overlit".Translate());
                        }
                    }
                    if (ClusterPlantProps.growOnlyUndeRoof)
                    {
                        if (Map.roofGrid.Roofed(Position) == false)
                        {
                            stringBuilder.Append(", " + "CaveworldFlora.Unroofed".Translate());
                        }
                    }
                    if (IsOnCavePlantGrower == false)
                    {
                        if (ClusterPlantProps.growOnlyOnRoughRock)
                        {
                            if (IsOnNaturalRoughRock == false)
                            {
                                stringBuilder.Append(", " + "CaveworldFlora.UnadaptedSoil".Translate());
                            }
                        }
                        else if (IsFertilityConditionOk == false)
                        {
                            stringBuilder.Append(", " + "CaveworldFlora.UnadaptedSoil".Translate());
                        }
                        if (ClusterPlantProps.growOnlyNearNaturalRock)
                        {
                            if (this.IsCellNearNaturalRockBlock == false)
                            {
                                stringBuilder.Append(", " + "CaveworldFlora.TooFarFromRock".Translate());
                            }
                        }
                        if (cluster.DestroyedOrNull())
                        {
                            stringBuilder.Append(", " + "CaveworldFlora.ClusterRootRemoved".Translate());
                        }
                        if (ClusterPlantProps.isSymbiosisPlant)
                        {
                            if (IsSymbiosisOk == false)
                            {
                                stringBuilder.Append(", " + "CaveworldFlora.BrokenSymbiosis".Translate());
                            }
                        }
                    }
                }
            }
            return stringBuilder.ToString();
        }

        public override string LabelMouseover
        {
            get
            {
                if (cachedLabelMouseover == null)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append(def.LabelCap);
                    stringBuilder.Append(" (" + "PercentGrowth".Translate(new object[]
			        {
				        GrowthPercentString
			        }));
                    if (IsInCryostasis)
                    {
                        stringBuilder.Append(", " + "CaveworldFlora.Cryostasis".Translate());
                    }
                    if (Dying)
                    {
                        stringBuilder.Append(", " + "DyingLower".Translate());
                    }
                    stringBuilder.Append(")");
                    cachedLabelMouseover = stringBuilder.ToString();
                }
                return cachedLabelMouseover;
            }
        }

        // ===================== Static exported functions =====================
        public static bool IsFertilityConditionOkAt(ThingDef_ClusterPlant plantDef, Map map, IntVec3 position)
        {
            float fertility = map.fertilityGrid.FertilityAt(position);
            return ((fertility >= plantDef.minFertility)
                && (fertility <= plantDef.maxFertility));
        }

        public static bool IsLightConditionOkAt(ThingDef_ClusterPlant plantDef, Map map, IntVec3 position)
        {
            float light = map.glowGrid.GameGlowAt(position);
            if ((light >= plantDef.minLight)
                && (light <= plantDef.maxLight))
            {
                return true;
            }
            return false;
        }

        public static bool IsTemperatureConditionOkAt(ThingDef_ClusterPlant plantDef, Map map, IntVec3 position)
        {
            float temperature = position.GetTemperature(map);
            return ((temperature >= plantDef.minGrowTemperature)
                && (temperature <= plantDef.maxGrowTemperature));
        }

        public static bool CanTerrainSupportPlantAt(ThingDef_ClusterPlant plantDef, Map map, IntVec3 position)
        {
            bool isValidSpot = true;

            if (plantDef.growOnlyOnRoughRock)
            {
                isValidSpot &= IsNaturalRoughRockAt(map, position);
            }
            else
            {
                isValidSpot &= IsFertilityConditionOkAt(plantDef, map, position);
            }
            if (plantDef.growOnlyUndeRoof)
            {
                isValidSpot &= map.roofGrid.Roofed(position);
            }
            else
            {
                isValidSpot &= (map.roofGrid.Roofed(position) == false);
            }
            if (plantDef.growOnlyNearNaturalRock)
            {
                isValidSpot &= IsNearNaturalRockBlock(map, position);
            }
            return isValidSpot;
        }

        public static bool IsNaturalRoughRockAt(Map map, IntVec3 position)
        {
            TerrainDef terrain = map.terrainGrid.TerrainAt(position);
            if ((terrain.layerable == false)
                && terrain.defName.Contains("Rough"))
            {
                return true;
            }
            return false;
        }
        
        public static bool IsNearNaturalRockBlock(Map map, IntVec3 position)
        {
            for (int xOffset = -2; xOffset <= 2; xOffset++)
            {
                for (int zOffset = -2; zOffset <= 2; zOffset++)
                {
                    IntVec3 checkedPosition = position + new IntVec3(xOffset, 0, zOffset);
                    if (checkedPosition.InBounds(map))
                    {
                        Thing potentialRock = map.thingGrid.ThingAt(checkedPosition, ThingCategory.Building);
                        if ((potentialRock != null)
                            && ((potentialRock as Building) != null))
                        {
                            if ((potentialRock as Building).def.building.isNaturalRock)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
