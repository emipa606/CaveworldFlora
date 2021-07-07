//using VerseBase;   // Material/Graphics handling functions are found here

using Verse;
// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI
//using Verse.Sound; // Needed when you do something with the Sound

namespace CaveworldFlora
{
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
            var glowerDefName = plantDef.defName + "Glower";
            return ThingDef.Named(glowerDefName);
        }

        public static ThingDef GetGlowerSmallDef(ThingDef plantDef)
        {
            var glowerDefName = plantDef.defName + "GlowerSmall";
            return ThingDef.Named(glowerDefName);
        }

        public static ThingDef GetGlowerMediumDef(ThingDef plantDef)
        {
            var glowerDefName = plantDef.defName + "GlowerMedium";
            return ThingDef.Named(glowerDefName);
        }

        public static ThingDef GetGlowerBigDef(ThingDef plantDef)
        {
            var glowerDefName = plantDef.defName + "GlowerBig";
            return ThingDef.Named(glowerDefName);
        }
    }
}