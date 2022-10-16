using System.Text;
using RimWorld;
using Verse;
// RimWorld specific functions are found here
// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI

namespace CaveworldFlora;

/// <summary>
///     Cluster class.
/// </summary>
/// <author>Rikiki</author>
/// <permission>
///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release thread.
///     Remember learning is always better than just copy/paste...
/// </permission>
public class Cluster : Plant
{
    // Size and next reproduction tick.
    public int actualSize;
    public int desiredSize;
    public int nextGrownTick;
    public int nextReproductionTick;

    // Plant def.
    public ThingDef_ClusterPlant plantDef;

    // Symbiosis cluster.
    public Cluster symbiosisCluster;

    // Exclusivity radius.
    public float ExclusivityRadius => plantDef.clusterExclusivityRadiusOffset +
                                      (desiredSize * plantDef.clusterExclusivityRadiusFactor);

    public override string LabelMouseover => def.LabelCap;

    // New cluster initialization.
    public static ClusterPlant SpawnNewClusterAt(Map map, IntVec3 spawnCell, ThingDef_ClusterPlant plantDef,
        int desiredSize)
    {
        var newPlant = ThingMaker.MakeThing(plantDef) as ClusterPlant;
        GenSpawn.Spawn(newPlant, spawnCell, map);
        if (ThingMaker.MakeThing(Util_CaveworldFlora.ClusterDef) is not Cluster newCluster)
        {
            return newPlant;
        }

        newCluster.Initialize(plantDef, desiredSize);
        GenSpawn.Spawn(newCluster, spawnCell, map);
        if (newPlant == null)
        {
            return null;
        }

        newPlant.cluster = newCluster;

        return newPlant;
    }

    public void Initialize(ThingDef_ClusterPlant plant, int size)
    {
        Growth = 1f; // For texture dimension.
        plantDef = plant;
        actualSize = 1;
        desiredSize = size;
    }

    public static float GetExclusivityRadius(ThingDef_ClusterPlant plantDef, int clusterSize)
    {
        return plantDef.clusterExclusivityRadiusOffset + (clusterSize * plantDef.clusterExclusivityRadiusFactor);
    }

    public static float GetMaxExclusivityRadius(ThingDef_ClusterPlant plantDef)
    {
        return plantDef.clusterExclusivityRadiusOffset +
               (plantDef.clusterSizeRange.max * plantDef.clusterExclusivityRadiusFactor);
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        base.Destroy(mode);
        if (symbiosisCluster.DestroyedOrNull() == false)
        {
            symbiosisCluster.NotifySymbiosisClusterRemoved(this);
        }
    }

    public override void TickLong()
    {
        // Grow cluster and spawn symbiosis cluster.
        if (Find.TickManager.TicksGame > nextGrownTick
            && ClusterPlant.IsTemperatureConditionOkAt(plantDef, Map, Position)
            && ClusterPlant.IsLightConditionOkAt(plantDef, Map, Position))
        {
            nextGrownTick = Find.TickManager.TicksGame +
                            (int)(plantDef.plant.lifespanDaysPerGrowDays * GenDate.TicksPerDay);

            // Grow cluster.
            GenClusterPlantReproduction.TryGrowCluster(this);

            // Spawn symbiosis cluster.
            if (actualSize == desiredSize
                && plantDef.symbiosisPlantDefEvolution != null)
            {
                GenClusterPlantReproduction.TrySpawnNewSymbiosisCluster(this);
            }
        }

        // Spawn new cluster.
        if (actualSize != desiredSize || Find.TickManager.TicksGame <= nextReproductionTick ||
            !ClusterPlant.IsTemperatureConditionOkAt(plantDef, Map, Position) ||
            !ClusterPlant.IsLightConditionOkAt(plantDef, Map, Position))
        {
            return;
        }

        GenClusterPlantReproduction.TrySpawnNewClusterAwayFrom(this);
        nextReproductionTick = Find.TickManager.TicksGame +
                               (int)(plantDef.plant.lifespanDaysPerGrowDays * 10f * GenDate.TicksPerDay);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        var plantDefAsString = "";
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            plantDefAsString = plantDef.defName;
            Scribe_Values.Look(ref plantDefAsString, "plantDefAsString");
        }
        else if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            Scribe_Values.Look(ref plantDefAsString, "plantDefAsString");
            plantDef = ThingDef.Named(plantDefAsString) as ThingDef_ClusterPlant;
        }

        Scribe_Values.Look(ref actualSize, "actualSize");
        Scribe_Values.Look(ref desiredSize, "desiredSize");
        Scribe_Values.Look(ref nextReproductionTick, "nextGrownTick");
        Scribe_Values.Look(ref nextReproductionTick, "nextReproductionTick");

        Scribe_References.Look(ref symbiosisCluster, "symbiosisCluster");
    }

    public void NotifyPlantAdded()
    {
        actualSize++;
    }

    public void NotifyPlantRemoved()
    {
        actualSize--;
        if (actualSize <= 0)
        {
            Destroy();
        }
    }

    public void NotifySymbiosisClusterAdded(Cluster cluster)
    {
        symbiosisCluster = cluster;
        cluster.symbiosisCluster = this;
    }

    public void NotifySymbiosisClusterRemoved(Cluster cluster)
    {
        symbiosisCluster = null;
        cluster.symbiosisCluster = null;
    }

    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder();

        stringBuilder.Append(plantDef.LabelCap);
        return stringBuilder.ToString();
    }
}