using System.Collections.Generic;
using Verse;
//using VerseBase;   // Material/Graphics handling functions are found here
// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound

namespace CaveworldFlora;

/// <summary>
///     MapComponent_ClusterPlant class.
/// </summary>
/// <author>Rikiki</author>
/// <permission>
///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release thread.
///     Remember learning is always better than just copy/paste...
/// </permission>
public class MapComponent_ClusterPlant(Map map) : MapComponent(map)
{
    private List<ThingDef_ClusterPlant> cavePlantDefsInternal;
    private int nextRandomSpawnTick = 10;
    private int randomSpawnPeriodInTicks;

    private List<ThingDef_ClusterPlant> CavePlantDefs
    {
        get
        {
            if (!cavePlantDefsInternal.NullOrEmpty())
            {
                return cavePlantDefsInternal;
            }

            cavePlantDefsInternal = [];
            foreach (var plantDef in DefDatabase<ThingDef>.AllDefs)
            {
                if (plantDef.category != ThingCategory.Plant)
                {
                    continue;
                }

                if (plantDef is ThingDef_ClusterPlant { isSymbiosisPlant: false } clusterPlantDef &&
                    (!clusterPlantDef.growsOnlyInCaveBiome
                     || map.Biome.defName == "Cave"))
                {
                    cavePlantDefsInternal.Add(clusterPlantDef);
                }
            }

            return cavePlantDefsInternal;
        }
    }

    public override void MapComponentTick()
    {
        if (randomSpawnPeriodInTicks == 0)
        {
            // Occurs when loading a savegame.
            var mapSurfaceCoefficient = (map.Size.x * 2) + (map.Size.z * 2);
            randomSpawnPeriodInTicks = 200000 / (mapSurfaceCoefficient / 100);
        }

        if (Find.TickManager.TicksGame <= nextRandomSpawnTick)
        {
            return;
        }

        nextRandomSpawnTick = Find.TickManager.TicksGame + randomSpawnPeriodInTicks;
        trySpawnNewClusterAtRandomPosition();
    }

    /// <summary>
    ///     Tries to spawn a new cluster at a random position on the map. The exclusivity radius still applies.
    /// </summary>
    private void trySpawnNewClusterAtRandomPosition()
    {
        var cavePlantDef = CavePlantDefs.RandomElementByWeight(plantDef =>
            plantDef.plant.wildClusterWeight / plantDef.clusterSizeRange.Average);

        var newDesiredClusterSize = cavePlantDef.clusterSizeRange.RandomInRange;
        GenClusterPlantReproduction.TryGetRandomClusterSpawnCell(cavePlantDef, newDesiredClusterSize, true, map,
            out var spawnCell);
        if (spawnCell.IsValid)
        {
            Cluster.SpawnNewClusterAt(map, spawnCell, cavePlantDef, newDesiredClusterSize);
        }
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref nextRandomSpawnTick, "nextRandomSpawnTick");
    }
}