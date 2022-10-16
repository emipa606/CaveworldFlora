﻿using RimWorld;
using Verse;
// RimWorld specific functions are found here
// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI

namespace CaveworldFlora;

/// <summary>
///     GenClusterPlantReproduction class.
/// </summary>
/// <author>Rikiki</author>
/// <permission>
///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release thread.
///     Remember learning is always better than just copy/paste...
/// </permission>
public static class GenClusterPlantReproduction
{
    /// <summary>
    ///     Try to get a valid cell to spawn a new cluster anywhere on the map.
    /// </summary>
    public static void TryGetRandomClusterSpawnCell(ThingDef_ClusterPlant plantDef, int newDesiredClusterSize,
        bool checkTemperature, Map map, out IntVec3 spawnCell)
    {
        spawnCell = IntVec3.Invalid;

        bool validator(IntVec3 cell)
        {
            // Check a plant can be spawned here.
            return IsValidPositionToGrowPlant(plantDef, map, cell, checkTemperature) &&
                   // Check there is no third cluster nearby.
                   IsClusterAreaClear(plantDef, newDesiredClusterSize, map, cell);
        }

        var validCellIsFound = CellFinderLoose.TryGetRandomCellWith(validator, map, 1000, out spawnCell);
        if (validCellIsFound == false)
        {
            // Just for robustness, TryGetRandomCellWith set result to IntVec3.Invalid if no valid cell is found.
            spawnCell = IntVec3.Invalid;
        }
    }

    /// <summary>
    ///     Try to spawn another plant in this cluster.
    /// </summary>
    public static ClusterPlant TryGrowCluster(Cluster cluster, bool checkTemperature = true)
    {
        if (cluster.actualSize >= cluster.desiredSize)
        {
            return null;
        }

        TryGetRandomSpawnCellNearCluster(cluster, checkTemperature, out var spawnCell);
        if (!spawnCell.IsValid)
        {
            return null;
        }

        var newPlant = ThingMaker.MakeThing(cluster.plantDef) as ClusterPlant;
        GenSpawn.Spawn(newPlant, spawnCell, cluster.Map);
        if (newPlant == null)
        {
            return null;
        }

        newPlant.cluster = cluster;
        cluster.NotifyPlantAdded();
        if (!cluster.plantDef.isSymbiosisPlant)
        {
            return newPlant;
        }

        // Destroy source symbiosis plant.
        var sourceSymbiosisPlant =
            spawnCell.GetFirstThing(cluster.Map, cluster.plantDef.symbiosisPlantDefSource);
        sourceSymbiosisPlant?.Destroy();

        return newPlant;
    }

    /// <summary>
    ///     Get a valid cell in this cluster to spawn another cave plant.
    /// </summary>
    public static void TryGetRandomSpawnCellNearCluster(Cluster cluster, bool checkTemperature,
        out IntVec3 spawnCell)
    {
        spawnCell = IntVec3.Invalid;

        var maxSpawnDistance =
            GenRadial.RadiusOfNumCells(cluster.actualSize + 1); // Min radius to hold cluster's plants + new plant.
        maxSpawnDistance += 2f; // Add a margin so the cluster does not have a perfect circle shape.

        bool validator(IntVec3 cell)
        {
            // Check cell is not too far away from current cluster.
            if (cell.InHorDistOf(cluster.Position, maxSpawnDistance) == false)
            {
                return false;
            }

            // Check cell is in the same room.
            var clusterRoom = cluster.GetRoom();
            var cellRoom = cell.GetRoom(cluster.Map);
            if (cellRoom == null
                || cellRoom != clusterRoom)
            {
                return false;
            }

            return IsValidPositionToGrowPlant(cluster.plantDef, cluster.Map, cell, checkTemperature);
        }

        var validCellIsFound = CellFinder.TryFindRandomCellNear(cluster.Position, cluster.Map,
            (int)maxSpawnDistance, validator, out spawnCell);
        if (validCellIsFound == false)
        {
            // Note that TryFindRandomCellNear set result to root if no valid cell is found!
            spawnCell = IntVec3.Invalid;
        }
    }

    /// <summary>
    ///     Try to spawn a new cluster away from plant.
    /// </summary>
    public static ClusterPlant TrySpawnNewClusterAwayFrom(Cluster cluster)
    {
        var newDesiredClusterSize = cluster.plantDef.clusterSizeRange.RandomInRange;
        TryGetRandomSpawnCellAwayFromCluster(cluster, newDesiredClusterSize, out var spawnCell);
        return spawnCell.IsValid
            ? Cluster.SpawnNewClusterAt(cluster.Map, spawnCell, cluster.plantDef, newDesiredClusterSize)
            : null;
    }

    /// <summary>
    ///     Try to get a valid cell to spawn a new cluster away from plant.
    /// </summary>
    public static void TryGetRandomSpawnCellAwayFromCluster(Cluster cluster, int newDesiredClusterSize,
        out IntVec3 spawnCell)
    {
        spawnCell = IntVec3.Invalid;
        var newClusterExclusivityRadius = Cluster.GetExclusivityRadius(cluster.plantDef, newDesiredClusterSize);

        // Current cluster and new cluster zones are exclusive and should not overlap.
        var newClusterMinDistance = cluster.ExclusivityRadius + newClusterExclusivityRadius;
        var newClusterMaxDistance = 2f * newClusterMinDistance;

        bool validator(IntVec3 cell)
        {
            // Check cell is not too close from current cluster.
            if (cell.InHorDistOf(cluster.Position, newClusterMinDistance))
            {
                return false;
            }

            // Check cell is not too distant from current cluster.
            if (cell.InHorDistOf(cluster.Position, newClusterMaxDistance) == false)
            {
                return false;
            }

            // Check cell is in the same room.
            if (cell.GetRoom(cluster.Map) != cluster.GetRoom())
            {
                return false;
            }

            // Check a plant can be spawned here.
            return IsValidPositionToGrowPlant(cluster.plantDef, cluster.Map, cell) &&
                   // Check there is no third cluster nearby.
                   IsClusterAreaClear(cluster.plantDef, newDesiredClusterSize, cluster.Map, cell);
        }

        var validCellIsFound = CellFinder.TryFindRandomCellNear(cluster.Position, cluster.Map,
            (int)newClusterMaxDistance, validator, out spawnCell);
        if (validCellIsFound == false)
        {
            // Note that TryFindRandomCellNear set result to root if no valid cell is found!
            spawnCell = IntVec3.Invalid;
        }
    }

    /// <summary>
    ///     Check if there is another cluster too close.
    /// </summary>
    public static bool IsClusterAreaClear(ThingDef_ClusterPlant plantDef, int newDesiredClusterSize, Map map,
        IntVec3 position)
    {
        var newClusterExclusivityRadius = Cluster.GetExclusivityRadius(plantDef, newDesiredClusterSize);
        foreach (var thing in map.listerThings.ThingsOfDef(Util_CaveworldFlora.ClusterDef))
        {
            var cluster = thing as Cluster;
            if (cluster?.plantDef != plantDef)
            {
                continue;
            }

            if (cluster != null &&
                cluster.Position.InHorDistOf(position, cluster.ExclusivityRadius + newClusterExclusivityRadius))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Check if position is valid to grow a plant. Does not check cluster exclusivity!
    /// </summary>
    public static bool IsValidPositionToGrowPlant(ThingDef_ClusterPlant plantDef, Map map, IntVec3 position,
        bool checkTemperature = true)
    {
        if (position.InBounds(map) == false)
        {
            return false;
        }

        if (plantDef.isSymbiosisPlant)
        {
            // For symbiosis plant, only check there is a source symbiosis plant.
            return position.GetFirstThing(map, plantDef.symbiosisPlantDefSource) != null;
        }

        // Check there is no building or cover.
        if (position.GetEdifice(map) != null
            || position.GetCover(map) != null)
        {
            return false;
        }

        // Check terrain condition.
        if (ClusterPlant.CanTerrainSupportPlantAt(plantDef, map, position) == false)
        {
            return false;
        }

        // Check temperature conditions.
        if (checkTemperature
            && ClusterPlant.IsTemperatureConditionOkAt(plantDef, map, position) == false)
        {
            return false;
        }

        // Check light conditions.
        if (ClusterPlant.IsLightConditionOkAt(plantDef, map, position) == false)
        {
            return false;
        }

        // Check there is no other plant (excepted cluster root).
        var plant = map.thingGrid.ThingAt(position, ThingCategory.Plant);
        if (plant != null
            && plant is not Cluster)
        {
            return false;
        }

        // Check the cell is not blocked by a plant, an item, a pawn, a rock...
        var thingList = map.thingGrid.ThingsListAt(position);
        foreach (var thing in thingList)
        {
            if (thing is Cluster)
            {
                // A plant can grow on the cluster root.
                continue;
            }

            if (thing.def.BlocksPlanting())
            {
                return false;
            }

            if (thing.def.category == ThingCategory.Plant
                || thing.def.category == ThingCategory.Item
                || thing.def.category == ThingCategory.Pawn)
            {
                return false;
            }
        }

        // Check snow level.
        return PlantUtility.SnowAllowsPlanting(position, map);
    }

    public static ClusterPlant TrySpawnNewSymbiosisCluster(Cluster cluster)
    {
        // Check there is not already a symbiosis cluster.
        if (cluster.symbiosisCluster != null)
        {
            return null;
        }

        foreach (var cell in GenRadial
                     .RadialCellsAround(cluster.Position, cluster.plantDef.clusterSpawnRadius, false).InRandomOrder())
        {
            if (cell.InBounds(cluster.Map) == false)
            {
                continue;
            }

            if (cell.GetFirstThing(cluster.Map, cluster.plantDef) is not ClusterPlant plant)
            {
                continue;
            }

            plant.Destroy();
            var symbiosisPlant = Cluster.SpawnNewClusterAt(cluster.Map, cell,
                cluster.plantDef.symbiosisPlantDefEvolution,
                cluster.plantDef.symbiosisPlantDefEvolution.clusterSizeRange.RandomInRange);
            cluster.NotifySymbiosisClusterAdded(symbiosisPlant.cluster);

            return symbiosisPlant;
        }

        return null;
    }
}