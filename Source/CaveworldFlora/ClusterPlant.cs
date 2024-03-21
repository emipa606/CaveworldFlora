using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
// Always needed
// RimWorld specific functions are found here
// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI

namespace CaveworldFlora;

/// <summary>
///     ClusterPlant class.
/// </summary>
/// <author>Rikiki</author>
/// <permission>
///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release thread.
///     Remember learning is always better than just copy/paste...
/// </permission>
public class ClusterPlant : Plant
{
    // TODO: use compFlickable instead of spawning glower buildings.

    public const float minGrowthToReproduce = 0.6f;
    private string cachedLabelMouseover;

    // Cluster.
    public Cluster cluster;

    // Glower.
    public Thing glower;

    public ThingDef_ClusterPlant ClusterPlantProps => def as ThingDef_ClusterPlant;

    // Fertility.
    public float FertilityGrowthRateFactor =>
        ClusterPlantProps.growOnlyOnRoughRock ? 1f : Map.fertilityGrid.FertilityAt(Position);

    public bool IsFertilityConditionOk => FertilityGrowthRateFactor > 0f;

    // Temperature.
    public bool IsInCryostasis => Position.GetTemperature(Map) < ClusterPlantProps.minGrowTemperature;

    public float TemperatureGrowthRateFactor
    {
        get
        {
            var temperature = Position.GetTemperature(Map);
            if (temperature < ClusterPlantProps.minGrowTemperature
                || temperature > ClusterPlantProps.maxGrowTemperature)
            {
                return 0f;
            }

            if (temperature < ClusterPlantProps.minOptimalGrowTemperature)
            {
                return Mathf.InverseLerp(ClusterPlantProps.minGrowTemperature,
                    ClusterPlantProps.minOptimalGrowTemperature, temperature);
            }

            if (temperature > ClusterPlantProps.maxOptimalGrowTemperature)
            {
                return Mathf.InverseLerp(ClusterPlantProps.maxGrowTemperature,
                    ClusterPlantProps.maxOptimalGrowTemperature, temperature);
            }

            return 1f;
        }
    }

    public bool IsTemperatureConditionOk => TemperatureGrowthRateFactor > 0f;

    // Light.
    public float LightGrowthRateFactor
    {
        get
        {
            var light = Map.glowGrid.GroundGlowAt(Position);
            if (light >= ClusterPlantProps.minLight
                && light <= ClusterPlantProps.maxLight)
            {
                return 1f;
            }

            return 0f;
        }
    }

    public bool IsLightConditionOk => LightGrowthRateFactor > 0f;

    // Symbiosis.
    public bool IsSymbiosisOk
    {
        get
        {
            if (ClusterPlantProps?.isSymbiosisPlant == false)
            {
                return true;
            }

            return cluster?.symbiosisCluster?.DestroyedOrNull() == false;
        }
    }

    // Growth rate
    public new float GrowthRate => FertilityGrowthRateFactor * TemperatureGrowthRateFactor * LightGrowthRateFactor;

    public new float GrowthPerTick
    {
        get
        {
            if (LifeStage != PlantLifeStage.Growing
                || IsInCryostasis)
            {
                return 0f;
            }

            var growthPerTick = 1f / (GenDate.TicksPerDay * def.plant.growDays);
            return growthPerTick * GrowthRate;
        }
    }

    // Plant grower and terrain.
    public bool IsOnCavePlantGrower
    {
        get
        {
            var edifice = Position.GetEdifice(Map);
            return edifice != null
                   && (edifice.def == Util_CaveworldFlora.FungiponicsBasinDef
                       || edifice.def == ThingDef.Named("PlantPot"));
        }
    }

    public bool IsOnValidNaturalSpot
    {
        get
        {
            var isValidSpot = true;
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
                isValidSpot &= IsCellNearNaturalRockBlock;
            }

            return isValidSpot;
        }
    }

    /// <summary>
    ///     Check if the terrain is valid. Require a rough terrain not constructible by player.
    /// </summary>
    public bool IsOnNaturalRoughRock
    {
        get
        {
            var terrain = Map.terrainGrid.TerrainAt(Position);
            return terrain.layerable == false
                   && terrain.defName.Contains("Rough");
        }
    }

    public bool IsCellNearNaturalRockBlock
    {
        get
        {
            for (var xOffset = -2; xOffset <= 2; xOffset++)
            {
                for (var zOffset = -2; zOffset <= 2; zOffset++)
                {
                    var checkedPosition = Position + new IntVec3(xOffset, 0, zOffset);
                    if (!checkedPosition.InBounds(Map))
                    {
                        continue;
                    }

                    var potentialRock = Map.thingGrid.ThingAt(checkedPosition, ThingCategory.Building);
                    if (potentialRock is not Building building)
                    {
                        continue;
                    }

                    if (building.def.building.isNaturalRock)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    public bool IsGrowingNow => LifeStage == PlantLifeStage.Growing
                                && IsTemperatureConditionOk
                                && IsLightConditionOk
                                && (IsOnCavePlantGrower
                                    || IsOnValidNaturalSpot
                                    && cluster.DestroyedOrNull() == false);

    public new bool Dying
    {
        get
        {
            if (IsInCryostasis)
            {
                return false;
            }

            var plantIsTooOld = def.plant.LimitedLifespan && ageInt > def.plant.LifespanTicks;
            if (plantIsTooOld)
            {
                return true;
            }

            var temperature = Position.GetTemperature(Map);
            var plantCanGrowHere = IsOnCavePlantGrower
                                   || cluster.DestroyedOrNull() == false
                                   && IsOnValidNaturalSpot;
            var plantIsInHostileConditions = temperature > ClusterPlantProps.maxGrowTemperature
                                             || !IsLightConditionOk
                                             || !plantCanGrowHere
                                             || !IsSymbiosisOk;
            return plantIsInHostileConditions;
        }
    }

    public override string LabelMouseover
    {
        get
        {
            if (cachedLabelMouseover != null)
            {
                return cachedLabelMouseover;
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(def.LabelCap);
            stringBuilder.Append(" (" + "PercentGrowth".Translate(GrowthPercentString));
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

            return cachedLabelMouseover;
        }
    }

    // ===================== Setup Work =====================

    /// <summary>
    ///     Save and load internal state variables (stored in savegame data).
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref cluster, "cluster");
        Scribe_References.Look(ref glower, "glower");
    }

    // ===================== Destroy =====================
    /// <summary>
    ///     Destroy the plant and the associated glower if existing.
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
    ///     Main function:
    ///     - update the glower if necessary.
    ///     - verify the cave plant is in good conditions to growth.
    ///     - when the cave plant is too old, damage it over time.
    /// </summary>
    public override void TickLong()
    {
        if (IsGrowingNow)
        {
            var plantWasAlreadyMature = LifeStage == PlantLifeStage.Mature;
            growthInt += GrowthPerTick * GenTicks.TickLongInterval;
            if (!plantWasAlreadyMature
                && LifeStage == PlantLifeStage.Mature)
            {
                // Plant just became mature.
                Map.mapDrawer.MapMeshDirty(Position, MapMeshFlagDefOf.Things);
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
                TakeDamage(new DamageInfo(DamageDefOf.Rotting, 5));
            }
        }

        // Update glower.
        if (!Destroyed)
        {
            UpdateGlowerAccordingToGrowth();
        }

        cachedLabelMouseover = null;
    }

    /// <summary>
    ///     Update the glower according to the plant growth, cryostatis state and snow depth.
    /// </summary>
    public void UpdateGlowerAccordingToGrowth()
    {
        if (ClusterPlantProps.hasStaticGlower == false
            && ClusterPlantProps.hasDynamicGlower == false)
        {
            return;
        }

        if (IsInCryostasis
            || Position.GetSnowDepth(Map) >= def.hideAtSnowDepth)
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
                if (!glower.DestroyedOrNull() && glower.def == Util_CaveworldFlora.GetGlowerSmallDef(def))
                {
                    return;
                }

                TryToDestroyGlower();
                glower = GenSpawn.Spawn(Util_CaveworldFlora.GetGlowerSmallDef(def), Position, Map);
            }
            else if (Growth < 0.66f)
            {
                if (!glower.DestroyedOrNull() && glower.def == Util_CaveworldFlora.GetGlowerMediumDef(def))
                {
                    return;
                }

                TryToDestroyGlower();
                glower = GenSpawn.Spawn(Util_CaveworldFlora.GetGlowerMediumDef(def), Position, Map);
            }
            else
            {
                if (!glower.DestroyedOrNull() && glower.def == Util_CaveworldFlora.GetGlowerBigDef(def))
                {
                    return;
                }

                TryToDestroyGlower();
                glower = GenSpawn.Spawn(Util_CaveworldFlora.GetGlowerBigDef(def), Position, Map);
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
    ///     Build the inspect string.
    /// </summary>
    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append("PercentGrowth".Translate(GrowthPercentString));
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
                    var light = Map.glowGrid.GroundGlowAt(Position);
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

                if (IsOnCavePlantGrower)
                {
                    return stringBuilder.ToString();
                }

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
                    if (IsCellNearNaturalRockBlock == false)
                    {
                        stringBuilder.Append(", " + "CaveworldFlora.TooFarFromRock".Translate());
                    }
                }

                if (cluster.DestroyedOrNull())
                {
                    stringBuilder.Append(", " + "CaveworldFlora.ClusterRootRemoved".Translate());
                }

                if (!ClusterPlantProps.isSymbiosisPlant)
                {
                    return stringBuilder.ToString();
                }

                if (!IsSymbiosisOk)
                {
                    stringBuilder.Append(", " + "CaveworldFlora.BrokenSymbiosis".Translate());
                }
            }
        }

        return stringBuilder.ToString();
    }

    // ===================== Static exported functions =====================
    public static bool IsFertilityConditionOkAt(ThingDef_ClusterPlant plantDef, Map map, IntVec3 position)
    {
        var fertility = map.fertilityGrid.FertilityAt(position);
        return fertility >= plantDef.minFertility
               && fertility <= plantDef.maxFertility;
    }

    public static bool IsLightConditionOkAt(ThingDef_ClusterPlant plantDef, Map map, IntVec3 position)
    {
        var light = map.glowGrid.GroundGlowAt(position);
        return light >= plantDef.minLight
               && light <= plantDef.maxLight;
    }

    public static bool IsTemperatureConditionOkAt(ThingDef_ClusterPlant plantDef, Map map, IntVec3 position)
    {
        if (map == null || plantDef == null)
        {
            return false;
        }

        var temperature = position.GetTemperature(map);
        return temperature >= plantDef.minGrowTemperature
               && temperature <= plantDef.maxGrowTemperature;
    }

    public static bool CanTerrainSupportPlantAt(ThingDef_ClusterPlant plantDef, Map map, IntVec3 position)
    {
        var isValidSpot = true;

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
            isValidSpot &= map.roofGrid.Roofed(position) == false;
        }

        if (plantDef.growOnlyNearNaturalRock)
        {
            isValidSpot &= IsNearNaturalRockBlock(map, position);
        }

        return isValidSpot;
    }

    public static bool IsNaturalRoughRockAt(Map map, IntVec3 position)
    {
        var terrain = map.terrainGrid.TerrainAt(position);
        return terrain.layerable == false
               && terrain.defName.Contains("Rough");
    }

    public static bool IsNearNaturalRockBlock(Map map, IntVec3 position)
    {
        for (var xOffset = -2; xOffset <= 2; xOffset++)
        {
            for (var zOffset = -2; zOffset <= 2; zOffset++)
            {
                var checkedPosition = position + new IntVec3(xOffset, 0, zOffset);
                if (!checkedPosition.InBounds(map))
                {
                    continue;
                }

                var potentialRock = map.thingGrid.ThingAt(checkedPosition, ThingCategory.Building);
                if (potentialRock is not Building building)
                {
                    continue;
                }

                if (building.def.building.isNaturalRock)
                {
                    return true;
                }
            }
        }

        return false;
    }
}