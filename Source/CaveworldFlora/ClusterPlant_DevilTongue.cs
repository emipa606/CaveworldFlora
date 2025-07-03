using UnityEngine;
using Verse;
// Always needed
// RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI

namespace CaveworldFlora;

/// <summary>
///     ClusterPlant_DevilTongue class.
/// </summary>
/// <author>Rikiki</author>
/// <permission>
///     Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release thread.
///     Remember learning is always better than just copy/paste...
/// </permission>
[StaticConstructorOnStartup]
public class ClusterPlant_DevilTongue : ClusterPlant
{
    private const float pawnDetectionRadius = 5f;
    private const int pawnDetectionPeriodWhenOpenedInTicks = GenTicks.TicksPerRealSecond;
    private const int pawnDetectionPeriodWhenClosedInTicks = 5 * GenTicks.TicksPerRealSecond;
    private const int flowerClosingDurationInTicks = 1 * GenTicks.TicksPerRealSecond;
    private const int flowerOpeningDurationInTicks = 5 * GenTicks.TicksPerRealSecond;

    // Drawing.
    private static readonly Material flowerTexture = MaterialPool.MatFrom(
        "Things/Plant/DevilTongue/Flower/DevilTongueFlower",
        ShaderDatabase.Transparent);

    private int flowerClosingRemainingTicks;
    private Matrix4x4 flowerMatrix;
    private int flowerOpeningTicks;
    private Vector3 flowerScale = new(0f, 1f, 0f);

    // Flower state.
    private FlowerState flowerState = FlowerState.closed;

    private int nextLongTick = GenTicks.TickLongInterval;
    private int nextNearbyPawnCheckTick;

    // ===================== Saving =====================
    /// <summary>
    ///     Save and load internal state variables (stored in savegame data).
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref nextLongTick, "nextLongTick");
        Scribe_Values.Look(ref nextNearbyPawnCheckTick, "nextNearbyPawnCheckTick");
        Scribe_Values.Look(ref flowerState, "flowerState");
        Scribe_Values.Look(ref flowerClosingRemainingTicks, "flowerClosingRemainingTicks");
        Scribe_Values.Look(ref flowerOpeningTicks, "flowerOpeningTicks");
        Scribe_Values.Look(ref flowerScale, "flowerScale");
    }

    // ===================== Main Work Function =====================
    /// <summary>
    ///     Main function:
    ///     - look for nearby pawn: if any is found, close flower and glower.
    ///     - when pawn is away, re-open flower after a delay.
    /// </summary>
    protected override void Tick()
    {
        if (Find.TickManager.TicksGame >= nextLongTick)
        {
            nextLongTick = Find.TickManager.TicksGame + GenTicks.TickLongInterval;
            base.TickLong();
        }

        if (Destroyed)
        {
            return;
        }

        switch (flowerState)
        {
            case FlowerState.closed:
                if (IsInCryostasis)
                {
                    break;
                }

                if (growthInt < 0.3f)
                {
                    break;
                }

                lookForNearbyPawnWhenClosed();
                break;
            case FlowerState.opening:
                openFlower();
                break;
            case FlowerState.opened:
                if (IsInCryostasis)
                {
                    transitionToClosing();
                    break;
                }

                lookForNearbyPawnWhenOpened();
                break;
            case FlowerState.closing:
                closeFlower();
                break;
        }
    }

    private void lookForNearbyPawnWhenClosed()
    {
        if (Find.TickManager.TicksGame < nextNearbyPawnCheckTick)
        {
            return;
        }

        if (isPawnNearby())
        {
            nextNearbyPawnCheckTick = Find.TickManager.TicksGame + pawnDetectionPeriodWhenClosedInTicks;
        }
        else
        {
            transitionToOpening();
        }
    }

    private void transitionToOpening()
    {
        flowerScale = new Vector3(0f, 1f, 0f);
        flowerOpeningTicks = 0;
        flowerState = FlowerState.opening;
    }

    private void openFlower()
    {
        flowerOpeningTicks++;
        if (flowerOpeningTicks >= flowerOpeningDurationInTicks)
        {
            transitionToOpened();
        }

        var scale = flowerOpeningTicks / (float)flowerOpeningDurationInTicks;
        var growthFactor = def.plant.visualSizeRange.min +
                           (growthInt * (def.plant.visualSizeRange.max - def.plant.visualSizeRange.min));
        scale *= growthFactor;
        flowerScale.x = scale;
        flowerScale.z = scale;
    }

    private void transitionToOpened()
    {
        glower = GenSpawn.Spawn(Util_CaveworldFlora.GetGlowerStaticDef(def), Position, Map);
        flowerState = FlowerState.opened;
    }

    private void lookForNearbyPawnWhenOpened()
    {
        if (Find.TickManager.TicksGame < nextNearbyPawnCheckTick)
        {
            return;
        }

        if (isPawnNearby())
        {
            transitionToClosing();
        }
        else
        {
            nextNearbyPawnCheckTick = Find.TickManager.TicksGame + pawnDetectionPeriodWhenOpenedInTicks;
        }
    }

    private bool isPawnNearby()
    {
        foreach (var pawn in Map.mapPawns.AllPawns)
        {
            if (pawn.Position.InHorDistOf(Position, pawnDetectionRadius))
            {
                return true;
            }
        }

        return false;
    }

    private void transitionToClosing()
    {
        TryToDestroyGlower();
        flowerClosingRemainingTicks = flowerClosingDurationInTicks;
        flowerState = FlowerState.closing;
    }

    private void closeFlower()
    {
        flowerClosingRemainingTicks--;
        if (flowerClosingRemainingTicks <= 0)
        {
            transitionToClosed();
        }

        var scale = flowerClosingRemainingTicks / (float)flowerClosingDurationInTicks;
        flowerScale.x = scale;
        flowerScale.z = scale;
    }

    private void transitionToClosed()
    {
        nextNearbyPawnCheckTick = Find.TickManager.TicksGame + pawnDetectionPeriodWhenClosedInTicks;
        flowerState = FlowerState.closed;
    }

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        if (flowerState == FlowerState.closed)
        {
            return;
        }

        // Draw flower just under body texture.
        flowerMatrix.SetTRS(drawLoc + Altitudes.AltIncVect + new Vector3(0f, -0.1f, 0f), 0f.ToQuat(),
            flowerScale);
        Graphics.DrawMesh(MeshPool.plane10, flowerMatrix, flowerTexture, 0);
    }

    private enum FlowerState
    {
        closed,
        opening,
        opened,
        closing
    }
}