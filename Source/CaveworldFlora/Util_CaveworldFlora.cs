//using VerseBase;   // Material/Graphics handling functions are found here

using Verse;
// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound

namespace CaveworldFlora;

/// <summary>
///     Util_CaveworldFlora utility class.
/// </summary>
/// <author>Rikiki</author>
/// <permission>
///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release thread.
///     Remember learning is always better than just copy/paste...
/// </permission>
public static class Util_CaveworldFlora
{
    public static bool BiotechInstalled;

    static Util_CaveworldFlora()
    {
        BiotechInstalled = ModLister.BiotechInstalled;
    }

    // Fungiponics buiding.
    public static ThingDef FungiponicsBasinDef => ThingDef.Named("FungiponicsBasin");

    // Spore spawner.
    public static ThingDef GleamcapSporeSpawnerDef => ThingDef.Named("GleamcapSporeSpawner");

    // HediffDef.
    public static HediffDef GleamcapSmokeDef => HediffDef.Named("HediffGleamcapSmoke");

    // Cluster.
    public static ThingDef ClusterDef => ThingDef.Named("Cluster");

    // Mote.
    public static ThingDef MotePoisonSmokeDef => ThingDef.Named("Mote_PoisonSmoke");

    // Glowers.
    public static ThingDef GetGlowerStaticDef(ThingDef plantDef)
    {
        return ThingDef.Named($"{plantDef.defName}Glower");
    }

    public static ThingDef GetGlowerSmallDef(ThingDef plantDef)
    {
        return ThingDef.Named($"{plantDef.defName}GlowerSmall");
    }

    public static ThingDef GetGlowerMediumDef(ThingDef plantDef)
    {
        return ThingDef.Named($"{plantDef.defName}GlowerMedium");
    }

    public static ThingDef GetGlowerBigDef(ThingDef plantDef)
    {
        return ThingDef.Named($"{plantDef.defName}GlowerBig");
    }
}