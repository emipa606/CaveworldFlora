using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
// Always needed
// RimWorld specific functions are found here
// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI

namespace CaveworldFlora
{
    /// <summary>
    ///     ClusterPlant_Gleamcap class.
    /// </summary>
    /// <author>Rikiki</author>
    /// <permission>
    ///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release thread.
    ///     Remember learning is always better than just copy/paste...
    /// </permission>
    public class ClusterPlant_BlackLotus : ClusterPlant
    {
        public const float poisonRadius = 7f;
        public const float minGrowthToPoison = 0.3f;
        public static bool alertHasBeenSent;

        public int nextLongTick = GenTicks.TickLongInterval;

        // ===================== Saving =====================
        /// <summary>
        ///     Save and load internal state variables (stored in savegame data).
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextLongTick, "nextLongTick");
            Scribe_Values.Look(ref alertHasBeenSent, "alertHasBeenSent");
        }

        // ===================== Main Work Function =====================
        /// <summary>
        ///     Main function:
        ///     -
        /// </summary>
        public override void TickRare()
        {
            if (Growth >= minGrowthToPoison
                && Dying == false
                && IsInCryostasis == false)
            {
                // Spawn toxic gas.
                ThrowPoisonSmoke();

                // Poison nearby pawns.
                var allPawnsSpawned = Map.mapPawns.AllPawnsSpawned;
                foreach (var pawn in allPawnsSpawned)
                {
                    if (!pawn.Position.InHorDistOf(Position, poisonRadius))
                    {
                        continue;
                    }

                    var num = 0.01f;
                    num *= pawn.GetStatValue(StatDefOf.ToxicSensitivity);
                    if (num == 0f)
                    {
                        continue;
                    }

                    var num2 = Mathf.Lerp(0.85f, 1.15f, Rand.ValueSeeded(pawn.thingIDNumber ^ 74374237));
                    num *= num2;
                    HealthUtility.AdjustSeverity(pawn, HediffDefOf.ToxicBuildup, num);
                    if (alertHasBeenSent || !pawn.IsColonist)
                    {
                        continue;
                    }

                    Find.LetterStack.ReceiveLetter("CaveworldFlora.LetterLabelBlackLotus".Translate(),
                        "CaveworldFlora.BlackLotus".Translate(),
                        LetterDefOf.NegativeEvent, new GlobalTargetInfo(pawn));
                    alertHasBeenSent = true;
                }
            }

            if (Find.TickManager.TicksGame < nextLongTick)
            {
                return;
            }

            nextLongTick = Find.TickManager.TicksGame + GenTicks.TickLongInterval;
            base.TickLong();
        }

        public void ThrowPoisonSmoke()
        {
            var spawnPosition = Position.ToVector3Shifted() + Vector3Utility.RandomHorizontalOffset(3f);

            if (!spawnPosition.ShouldSpawnMotesAt(Map) || Map.moteCounter.SaturatedLowPriority)
            {
                return;
            }

            if (!(ThingMaker.MakeThing(Util_CaveworldFlora.MotePoisonSmokeDef) is MoteThrown moteThrown))
            {
                return;
            }

            moteThrown.Scale = 3f * Growth;
            moteThrown.rotationRate = Rand.Range(-5, 5);
            moteThrown.exactPosition = spawnPosition;
            moteThrown.SetVelocity(Rand.Range(-20, 20), 0);
            GenSpawn.Spawn(moteThrown, spawnPosition.ToIntVec3(), Map);
        }
    }
}