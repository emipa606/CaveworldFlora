using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;   // Always needed
using RimWorld;      // RimWorld specific functions are found here
using Verse;         // RimWorld universal objects are here
//using Verse.AI;    // Needed when you do something with the AI
using Verse.Sound;   // Needed when you do something with the Sound

namespace CaveworldFlora
{
    /// <summary>
    /// Cluster class.
    /// </summary>
    /// <author>Rikiki</author>
    /// <permission>Use this code as you want, just remember to add a link to the corresponding Ludeon forum mod release thread.
    /// Remember learning is always better than just copy/paste...</permission>
    public class Cluster : Plant
    {

        // Plant def.
        public ThingDef_ClusterPlant plantDef = null;

        // Size and next reproduction tick.
        public int actualSize = 0;
        public int desiredSize = 0;
        public int nextGrownTick = 0;
        public int nextReproductionTick = 0;

        // Symbiosis cluster.
        public Cluster symbiosisCluster = null;

        // New cluster initialization.
        public static ClusterPlant SpawnNewClusterAt(Map map, IntVec3 spawnCell, ThingDef_ClusterPlant plantDef, int desiredSize)
        {
            ClusterPlant newPlant = ThingMaker.MakeThing(plantDef) as ClusterPlant;
            GenSpawn.Spawn(newPlant, spawnCell, map);
            Cluster newCluster = ThingMaker.MakeThing(Util_CaveworldFlora.ClusterDef) as Cluster;
            newCluster.Initialize(plantDef, desiredSize);
            GenSpawn.Spawn(newCluster, spawnCell, map);
            newPlant.cluster = newCluster;
            return newPlant;
        }
        public void Initialize(ThingDef_ClusterPlant plantDef, int desiredSize)
        {
            Growth = 1f; // For texture dimension.
            this.plantDef = plantDef;
            actualSize = 1;
            this.desiredSize = desiredSize;
        }

        // Exclusivity radius.
        public float ExclusivityRadius
        {
            get
            {
                return (plantDef.clusterExclusivityRadiusOffset + ((float)desiredSize) * plantDef.clusterExclusivityRadiusFactor);
            }
        }
        public static float GetExclusivityRadius(ThingDef_ClusterPlant plantDef, int clusterSize)
        {
            return (plantDef.clusterExclusivityRadiusOffset + (float)clusterSize * plantDef.clusterExclusivityRadiusFactor);
        }
        public static float GetMaxExclusivityRadius(ThingDef_ClusterPlant plantDef)
        {
            return (plantDef.clusterExclusivityRadiusOffset + ((float)plantDef.clusterSizeRange.max) * plantDef.clusterExclusivityRadiusFactor);
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
            if ((Find.TickManager.TicksGame > nextGrownTick)
                && ClusterPlant.IsTemperatureConditionOkAt(plantDef, Map, Position)
                && ClusterPlant.IsLightConditionOkAt(plantDef, Map, Position))
            {
                nextGrownTick = Find.TickManager.TicksGame + (int)(plantDef.plant.lifespanDaysPerGrowDays * GenDate.TicksPerDay);

                // Grow cluster.
                GenClusterPlantReproduction.TryGrowCluster(this);

                // Spawn symbiosis cluster.
                if ((actualSize == desiredSize)
                    && (plantDef.symbiosisPlantDefEvolution != null))
                {
                    GenClusterPlantReproduction.TrySpawnNewSymbiosisCluster(this);
                }
            }
            
            // Spawn new cluster.
            if ((actualSize == desiredSize)
                && (Find.TickManager.TicksGame > nextReproductionTick)
                && ClusterPlant.IsTemperatureConditionOkAt(plantDef, Map, Position)
                && ClusterPlant.IsLightConditionOkAt(plantDef, Map, Position))
            {
                GenClusterPlantReproduction.TrySpawnNewClusterAwayFrom(this);
                nextReproductionTick = Find.TickManager.TicksGame + (int)(plantDef.plant.lifespanDaysPerGrowDays * 10f * GenDate.TicksPerDay);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            string plantDefAsString = "";
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                plantDefAsString = plantDef.defName;
                Scribe_Values.Look<string>(ref plantDefAsString, "plantDefAsString");
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Scribe_Values.Look<string>(ref plantDefAsString, "plantDefAsString");
                plantDef = ThingDef.Named(plantDefAsString) as ThingDef_ClusterPlant;
            }
            Scribe_Values.Look<int>(ref actualSize, "actualSize");
            Scribe_Values.Look<int>(ref desiredSize, "desiredSize");
            Scribe_Values.Look<int>(ref nextReproductionTick, "nextGrownTick");
            Scribe_Values.Look<int>(ref nextReproductionTick, "nextReproductionTick");

            Scribe_References.Look<Cluster>(ref symbiosisCluster, "symbiosisCluster");
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
                return;
            }
        }

        public void NotifySymbiosisClusterAdded(Cluster symbiosisCluster)
        {
            this.symbiosisCluster = symbiosisCluster;
            symbiosisCluster.symbiosisCluster = this;
        }

        public void NotifySymbiosisClusterRemoved(Cluster symbiosisCluster)
        {
            this.symbiosisCluster = null;
            symbiosisCluster.symbiosisCluster = null;
        }

        public override string LabelMouseover
        {
            get
            {
                return def.LabelCap;
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(plantDef.LabelCap);
            return stringBuilder.ToString();
        }
    }
}
