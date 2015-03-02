using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;


namespace RimWorld{

public enum SeedTargFindMode
{
	ReproduceSeed,
	Cluster,
	MapEdge
}

public static class GenPlantReproduction
{
	public const float SeedShootMinGrowthPercent = 0.6f;


	public static void TickReproduceFrom( Plant plant )
	{
		if( !plant.def.plant.shootsSeeds )
			return;

		if( plant.growthPercent < SeedShootMinGrowthPercent )
			return;

		//Split the rand check to eliminate the possibility of float precision errors
		var test = (float)GenTicks.TickRareInterval / (float)GenDate.TicksPerDay;
		if( Rand.Value < test )
		{
			if( Rand.Value < plant.def.plant.seedEmitChancePerDay )
			{
				if( !GenPlant.SnowAllowsPlanting(plant.Position) )
					return;

				if( !GenPlant.GrowthSeasonNow(plant.Position) )
					return;

				//Otherwise roofed plants will be not counted from other plants, yet still shoot seeds
				//creating higher densities around themselves
				if( plant.Position.Roofed() )
					return;

				TrySpawnSeed( plant.Position, plant.def, SeedTargFindMode.ReproduceSeed, plant );		
			}
		}
	}

	public static bool TrySpawnSeed( IntVec3 cell, ThingDef plantDef, SeedTargFindMode mode, Thing plant = null )
	{
		IntVec3 seedTarg;
		if( !TryFindSeedTargFor( plantDef, cell, mode, out seedTarg ) )
		{
			//Nowhere to send the seed
			return false; 
		}

		Seed seed = (Seed)ThingMaker.MakeThing( plantDef.plant.seedDef);
		GenSpawn.Spawn ( seed, cell, IntRot.random);
		seed.Launch( plant, seedTarg );

		if( DebugSettings.fastEcology )
		{
			seed.ForceInstantImpact();
		}

		return true;
	}



	public static bool TryFindSeedTargFor( ThingDef plantDef, IntVec3 root, SeedTargFindMode mode, out IntVec3 foundCell )
	{
		float radius = -1;
		if( mode == SeedTargFindMode.ReproduceSeed )
			radius = plantDef.plant.seedShootRadius;
		else if( mode == SeedTargFindMode.Cluster )
			radius = plantDef.plant.WildClusterRadiusActual;
		else if( mode == SeedTargFindMode.MapEdge )
			radius = 35;

		//Gather some data about the area around root
		int numFoundPlants = 0;
		int numFoundPlantsMyDef = 0;
		float totalUnroofedFertility = 0;
		IntRect rect = IntRect.CenteredOn( root, Mathf.RoundToInt(radius) );
		rect.ClipInsideMap();
		for (int z = rect.minZ; z <= rect.maxZ; z++)
		{
			for (int x = rect.minX; x <= rect.maxX; x++)
			{
				var c = new IntVec3(x,0,z);
				if( !c.Roofed() )
				{
					Plant p = c.GetPlant();
					if( p != null )
					{
						numFoundPlants++;

						if( p.def == plantDef )
							numFoundPlantsMyDef++;
					}

					totalUnroofedFertility += c.GetTerrain().fertility;
				}
			}
		}

		//Determine theoretical number of desired plants of any type
		float numDesiredPlants = totalUnroofedFertility * Find.Map.Biome.plantDensity;
		bool full = numFoundPlants > numDesiredPlants;
		bool overloaded = numFoundPlants > numDesiredPlants * 1.25f;

		if( overloaded )
		{
			foundCell = IntVec3.Invalid;
			return false;
		}

		//Todo: This should have some measure of the _global_ proportion of plants and be able to make decisions about that


		//Determine num desired plants of my def
		BiomeDef curBiome = Find.Map.Biome;
		float totalCommonality = curBiome.AllWildPlants.Sum( pd=>curBiome.CommonalityOfPlant(pd) );
		float minProportion = curBiome.CommonalityOfPlant(plantDef) / totalCommonality;
		float maxProportion = (curBiome.CommonalityOfPlant(plantDef) *plantDef.plant.wildCommonalityMaxFraction) / totalCommonality;

		//Too many plants of my type nearby - don't reproduce
		float maxDesiredPlantsMyDef = numDesiredPlants * maxProportion;
		if( numFoundPlantsMyDef > maxDesiredPlantsMyDef )
		{
			foundCell = IntVec3.Invalid;
			return false;
		}

		//Too many plants nearby for the biome/total fertility - don't reproduce
		// UNLESS there are way too few of my kind of plant
		float minDesiredPlantsMyDef = numDesiredPlants * minProportion;
		bool desperateForPlantsMyDef = numFoundPlantsMyDef < minDesiredPlantsMyDef * 0.5f;
		if( full && !desperateForPlantsMyDef )
		{
			foundCell = IntVec3.Invalid;
			return false;
		}


		//We need to plant something!
		System.Predicate<IntVec3> seedTargValidator = c=>
		{
			if( !plantDef.CanEverPlantAt(c) )
				return false;

			if( !GenPlant.SnowAllowsPlanting(c) )
				return false;

			if( !root.InHorDistOf( c, radius) )
				return false;

			if( !GenSight.LineOfSight( root, c, skipFirstSquare: true ) )
				return false;

			return true;
		};

		return GenCellFinder.TryFindRandomMapCellNearWith( root,
											Mathf.CeilToInt(radius),
											seedTargValidator,
											out foundCell );
	}
}}

