using Verse;
// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI

namespace CaveworldFlora;

/// <summary>
///     ThingDef_ClusterPlant class.
/// </summary>
/// <author>Rikiki</author>
/// <permission>
///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release thread.
///     Remember learning is always better than just copy/paste...
/// </permission>
public class ThingDef_ClusterPlant : ThingDef
{
    public readonly float clusterExclusivityRadiusFactor = 0f;
    public readonly float clusterExclusivityRadiusOffset = 1f;
    public readonly float clusterSpawnRadius = 1f;
    public readonly bool growOnlyNearNaturalRock = false;

    public readonly bool growOnlyOnRoughRock = false;
    public readonly bool growOnlyUndeRoof = false;
    public readonly bool growsOnlyInCaveBiome = false;
    public readonly bool hasDynamicGlower = false; // Glower must be named "defName + Small/Medium/Big".

    public readonly bool hasStaticGlower = false; // Glower must be named "defName + Glower".

    // When a cluster is mature, it will spawn a new symbiosis cluster.
    // Symbiosis plants cannot spawn new cluster on their own.
    public readonly bool isSymbiosisPlant = false;
    public readonly float maxFertility = 999f;
    public readonly int maxGrowTemperature = 50;
    public readonly float maxLight = 1f;
    public readonly int maxOptimalGrowTemperature = 40;
    public readonly float minFertility = 0f;

    public readonly int minGrowTemperature = 0; // Plant will enter cryostatis under this temperature.

    public readonly float minLight = 0f;
    public readonly int minOptimalGrowTemperature = 10;
    public readonly ThingDef_ClusterPlant symbiosisPlantDefEvolution = null; // Plant can evolve into this plant.

    public readonly ThingDef_ClusterPlant
        symbiosisPlantDefSource = null; // Symbiosis plant will evolve from this plant.

    public IntRange clusterSizeRange;
}