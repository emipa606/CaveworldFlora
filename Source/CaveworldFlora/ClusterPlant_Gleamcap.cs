using RimWorld;
using Verse;
// RimWorld specific functions are found here
// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI

namespace CaveworldFlora;

/// <summary>
///     ClusterPlant_Gleamcap class.
/// </summary>
/// <author>Rikiki</author>
/// <permission>
///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release thread.
///     Remember learning is always better than just copy/paste...
/// </permission>
public class ClusterPlant_Gleamcap : ClusterPlant
{
    private const float chanceToSpawnSpore = 0.01f;
    private const int minDelayBetweenSporeSpawnInTicks = GenDate.TicksPerDay / 2;
    private int lastSporeSpawnTick;
    public GleamcapSporeSpawner sporeSpawner;

    // ===================== Saving =====================
    /// <summary>
    ///     Save and load internal state variables (stored in savegame data).
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref lastSporeSpawnTick, "lastSporeSpawnTick");
        Scribe_References.Look(ref sporeSpawner, "sporeSpawner");
    }

    // ===================== Destroy =====================
    /// <summary>
    ///     Destroy the plant and the associated glower if existing.
    /// </summary>
    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (!sporeSpawner.DestroyedOrNull())
        {
            sporeSpawner.Destroy();
        }

        base.Destroy(mode);
    }

    // ===================== Main Work Function =====================
    /// <summary>
    ///     Main function:
    ///     - perform the cluster plant normal treatment.
    ///     - when mature, has a small chance to spawn a spore spawner.
    /// </summary>
    public override void TickLong()
    {
        base.TickLong();

        trySpawnSporeSpawner();
    }

    /// <summary>
    ///     Try to spawn some spores if the plant is mature.
    /// </summary>
    private void trySpawnSporeSpawner()
    {
        var sporeSpawnOccuredLongAgo = lastSporeSpawnTick == 0
                                       || Find.TickManager.TicksGame - lastSporeSpawnTick >
                                       minDelayBetweenSporeSpawnInTicks;

        if (LifeStage != PlantLifeStage.Mature || Dying || IsInCryostasis ||
            !sporeSpawnOccuredLongAgo || !(Rand.Value < chanceToSpawnSpore) &&
            !Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.Eclipse))
        {
            return;
        }

        lastSporeSpawnTick = Find.TickManager.TicksGame;
        sporeSpawner =
            ThingMaker.MakeThing(Util_CaveworldFlora.GleamcapSporeSpawnerDef) as GleamcapSporeSpawner;
        GenSpawn.Spawn(sporeSpawner, Position, Map);
        if (sporeSpawner != null)
        {
            sporeSpawner.parent = this;
        }
    }
}