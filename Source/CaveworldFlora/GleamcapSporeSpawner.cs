using RimWorld;
using Verse;
// RimWorld specific functions are found here
// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI

namespace CaveworldFlora
{
    /// <summary>
    ///     GleamcapSporeSpawner class.
    /// </summary>
    /// <author>Rikiki</author>
    /// <permission>
    ///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release thread.
    ///     Remember learning is always better than just copy/paste...
    /// </permission>
    public class GleamcapSporeSpawner : Building
    {
        public const int sporeEffectRadius = 5;
        public const int minSporeSpawningDurationInTicks = 20 * GenTicks.TicksPerRealSecond;
        public const int maxSporeSpawningDurationInTicks = 60 * GenTicks.TicksPerRealSecond;
        public int nextNearbyPawnCheckTick;
        public int nextSporeThrowTick;
        public ClusterPlant_Gleamcap parent;
        public int sporeSpawnEndTick;

        // ===================== Setup Work =====================
        /// <summary>
        ///     Initialize instance variables.
        /// </summary>
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            sporeSpawnEndTick = Find.TickManager.TicksGame +
                                Rand.RangeInclusive(minSporeSpawningDurationInTicks, maxSporeSpawningDurationInTicks);
        }

        // ===================== Saving =====================
        /// <summary>
        ///     Save and load internal state variables (stored in savegame data).
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref sporeSpawnEndTick, "sporeSpawnEndTick");
            Scribe_References.Look(ref parent, "parentGleamcap");
        }

        // ===================== Main Work Function =====================
        /// <summary>
        ///     Main function:
        ///     - throw some spore.
        ///     - try to apply a mood effect on nearby colonists.
        /// </summary>
        public override void Tick()
        {
            base.Tick();

            if (Find.TickManager.TicksGame > nextSporeThrowTick)
            {
                nextSporeThrowTick = Find.TickManager.TicksGame + 10;
                FleckMaker.ThrowDustPuff(this.TrueCenter(), Map, Rand.Value);
            }

            if (Find.TickManager.TicksGame > nextNearbyPawnCheckTick)
            {
                nextNearbyPawnCheckTick = Find.TickManager.TicksGame + GenTicks.TicksPerRealSecond;
                foreach (var pawn in Map.mapPawns.AllPawns)
                {
                    if (pawn.Position.InHorDistOf(Position, sporeEffectRadius))
                    {
                        pawn.health?.AddHediff(Util_CaveworldFlora.GleamcapSmokeDef);
                    }
                }
            }

            if (Find.TickManager.TicksGame <= sporeSpawnEndTick)
            {
                return;
            }

            // Inform the gleamcap that the spore spawner is destroyed.
            parent.sporeSpawner = null;
            Destroy();
        }
    }
}