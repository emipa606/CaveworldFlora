using System.Collections.Generic;
using Verse;
//using VerseBase;   // Material/Graphics handling functions are found here
// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound

namespace CaveworldFlora;

/// <summary>
///     PlaceWorker_FungiponicsBasin custom PlaceWorker class.
/// </summary>
/// <author>Rikiki</author>
/// <permission>
///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release thread.
///     Remember learning is always better than just copy/paste...
/// </permission>
public class PlaceWorker_FungiponicsBasin : PlaceWorker
{
    public const float minDistanceBetweenFungiponicsBasins = 5.9f;

    /// <summary>
    ///     Check if a new fungiponics basin can be built at this location.
    ///     - the fungiponics basin must be roofed.
    ///     - must not be too near from another fungiponics basin.
    /// </summary>
    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map,
        Thing thingToIgnore = null, Thing thing = null)
    {
        foreach (var cell in GenAdj.CellsOccupiedBy(loc, rot, checkingDef.Size))
        {
            if (ClusterPlant.IsNaturalRoughRockAt(map, cell) == false)
            {
                return new AcceptanceReport("CaveworldFlora.MustOnRoughRock".Translate());
            }

            if (map.roofGrid.Roofed(loc) == false)
            {
                return new AcceptanceReport("CaveworldFlora.MustBeRoofed".Translate());
            }
        }

        var fungiponicsBasinsList = new List<Thing>();
        IEnumerable<Thing> list = map.listerThings.ThingsOfDef(ThingDef.Named("FungiponicsBasin"));
        foreach (var basin in list)
        {
            fungiponicsBasinsList.Add(basin);
        }

        list = map.listerThings.ThingsOfDef(ThingDef.Named("FungiponicsBasin").blueprintDef);
        foreach (var basin in list)
        {
            fungiponicsBasinsList.Add(basin);
        }

        list = map.listerThings.ThingsOfDef(ThingDef.Named("FungiponicsBasin").frameDef);
        foreach (var basin in list)
        {
            fungiponicsBasinsList.Add(basin);
        }

        foreach (var basin in fungiponicsBasinsList)
        {
            if (basin.Position.InHorDistOf(loc, minDistanceBetweenFungiponicsBasins))
            {
                return new AcceptanceReport("CaveworldFlora.TooClose".Translate());
            }
        }

        return true;
    }
}