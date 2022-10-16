using RimWorld;
using Verse;
// RimWorld specific functions are found here
// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI

namespace CaveworldFlora;

/// <summary>
///     Building_FungiponicsBasin class.
/// </summary>
/// <author>Rikiki</author>
/// <permission>
///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release thread.
///     Remember learning is always better than just copy/paste...
/// </permission>
public class Building_FungiponicsBasin : Building_PlantGrower
{
    public override string GetInspectString()
    {
        var temperature = GenTemperature.GetTemperatureForCell(Position, Map);
        var clusterPlantDef = GetPlantDefToGrow() as ThingDef_ClusterPlant;
        if (clusterPlantDef != null && temperature < clusterPlantDef.minGrowTemperature)
        {
            return "CaveworldFlora.CannotGrowTooCold".Translate();
        }

        if (clusterPlantDef != null && temperature > clusterPlantDef.maxGrowTemperature)
        {
            return "CaveworldFlora.CannotGrowTooHot".Translate();
        }

        return "CaveworldFlora.Growing".Translate();
    }
}