using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

using Verse.Sound;
using Verse.AI;


namespace RimWorld{

public enum PlantLifeStage
{
	Sowing,
	Growing,
	Mature,
}



public class Plant : Thing
{
	//Working vars
	public float 						growthPercent = 0.05f; //Start in growing phase by default, set to 0 if sowing
	public int							age = 0;
	private int							ticksSinceLit = 0;
	private int							madeLeaflessTick = -99999;

	//Fast vars
	private List<int>					posIndexList = new List<int>();
	private Color32[]					workingColors = new Color32[4];

	//Constants and content
	public const float					BaseGrowthPercent = 0.05f;
	private const float					RotDamagePerTick = 1f/200f;
	private const int					MinPlantYield = 2;
	private static readonly	Graphic		GraphicSowing =  GraphicDatabase.Get<Graphic_Single>("Things/Plant/Plant_Sowing", ShaderDatabase.Cutout, IntVec2.one, Color.white);
	private const float					GridPosRandomnessFactor = 0.30f;
	protected static readonly SoundDef	SoundHarvestReady = SoundDef.Named("HarvestReady");
	private const int					TicksWithoutLightBeforeRot = GenDate.TicksPerDay * 5; //5 days, for eclipse survivability
	private const int					LeaflessMinRecoveryTicks = 60000;	//Minimum time to not show leafless after being made leafless
    public const float					MinGrowthTemperature = 0;			//Min temperature at which plant can grow or reproduce
    public const float					MinOptimalGrowthTemperature = 10f;
    public const float					MaxOptimalGrowthTemperature = 42f;
    public const float					MaxGrowthTemperature = 58;			//Max temperature at which plant can grow or reproduce
	public const float					MaxLeaflessTemperature = -2;
	private const float					MinLeaflessTemperature = -10;
	private const float					MinAnimalEatPlantsTemperature = 0;

	//Properties
	public bool HarvestableNow
	{
		get
		{
			return def.plant.Harvestable && growthPercent > def.plant.harvestMinGrowth;
		}
	}
	public override bool IngestibleNow
	{
		get
		{
			//Trees are always edible
			// This allows alphabeavers completely destroy the tree ecosystem
			if( def.plant.IsTree )
				return true;

			if( growthPercent < def.plant.harvestMinGrowth )
				return false;

			if( LeaflessNow )
				return false;

			if( Position.GetSnowDepth() > def.hideAtSnowDepth )
				return false;

			return true;
		}
	}
	public bool Rotting
	{
		get
		{
			if( def.plant.LimitedLifespan && age > def.plant.LifespanTicks )
				return true;

			if( ticksSinceLit > TicksWithoutLightBeforeRot )
				return true;

			return false;
		}
	}
	public bool GrowingNow
	{
		get
		{
			return LifeStage == PlantLifeStage.Growing
			&& HasEnoughLightToGrow
			&& GenDate.CurrentDayPercent > 0.25f && GenDate.CurrentDayPercent < 0.8f;
		}
	}
	private float GrowthPerTick
	{
		get
		{
			float fertilityFactor = (LocalFertility*def.plant.fertilityFactorGrowthRate)
									+ (1f-def.plant.fertilityFactorGrowthRate);

			float plantFactor = (def.plant.growthPer20kTicks / 20000f);

			return fertilityFactor * plantFactor;
		}
	}
	private float GrowthPerTickRare
	{
		get
		{
			return GrowthPerTick * GenTicks.TickRareInterval;
		}
	}
	private int TicksUntilFullyGrown
	{
		get
		{
			if( growthPercent > 0.9999f )
				return 0;

			return (int)((1f-growthPercent) / GrowthPerTick);
		}
	}
	private string GrowthPercentString
	{
		get
		{
			float adjGrowthPercent = (growthPercent*100);
			if( adjGrowthPercent > 100f )
				adjGrowthPercent = 100.1f;
			return adjGrowthPercent.ToString("##0");
		}
	}
	public override string LabelMouseover
	{
		get
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(def.LabelCap);
			sb.Append(" (" + "PercentGrowth".Translate(GrowthPercentString));
			
			if( Rotting )
				sb.Append(", " + "DyingLower".Translate() );
			
			sb.Append(")");
			return sb.ToString();
		}
	}
	private bool HasEnoughLightToGrow
	{
		get
		{
			return Find.GlowGrid.PsychGlowAt(Position) >= def.plant.growMinGlow;
		}
	}
	private float LocalFertility
	{
		get
		{
			return Find.FertilityGrid.FertilityAt(Position);
		}
	}
	public PlantLifeStage LifeStage
	{
		get
		{
			if( growthPercent < 0.001f )
				return PlantLifeStage.Sowing;

			if( growthPercent > 0.999f )
				return PlantLifeStage.Mature;

			return PlantLifeStage.Growing;
		}
	}

	public override Graphic Graphic
	{
		get
		{
			if( LifeStage == PlantLifeStage.Sowing )
				return GraphicSowing;

			if( def.plant.leaflessGraphic != null && LeaflessNow )
				return def.plant.leaflessGraphic;

			return base.Graphic;
		}
	}
	public float TemperatureEfficiency
    {
        get
        {
            float cellTemp;
			if( !GenTemperature.TryGetTemperatureForCell(Position, out cellTemp) )
				return 1;

            if (cellTemp < MinOptimalGrowthTemperature)
				return Mathf.InverseLerp( MinGrowthTemperature, MinOptimalGrowthTemperature, cellTemp );
            else if (cellTemp > MaxOptimalGrowthTemperature)
				return Mathf.InverseLerp( MaxGrowthTemperature, MaxOptimalGrowthTemperature, cellTemp );
			else
				return 1;
        }
    }
	public bool LeaflessNow
	{
		get
		{
			if( Find.TickManager.TicksGame - madeLeaflessTick < LeaflessMinRecoveryTicks )
				return true;
			else
				return false;
		}
	}


	//temp for debug
	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		base.Destroy(mode);
	}


	public override void SpawnSetup()
	{
		base.SpawnSetup();

		//Store all the position indices
		for( int i=0; i<def.plant.maxMeshCount; i++ )
		{
			posIndexList.Add(i);
		}
		posIndexList.Shuffle();

		//Don't call during init because indoor warm plants will all go leafless if it's cold outside
		if( Game.Mode == GameMode.MapPlaying )
			CheckTemperatureMakeLeafless();
	}
	
	
	public override void ExposeData()
	{
		base.ExposeData();
		
		Scribe_Values.LookValue(ref growthPercent, 	"growthPercent");
		Scribe_Values.LookValue(ref age, 			"age", 0);
		Scribe_Values.LookValue(ref ticksSinceLit,	"ticksSinceLit", 0 );
	}

	public override void PostMapInit()
	{
		CheckTemperatureMakeLeafless();
	}

	public override void Ingested( Pawn eater, float nutritionWanted )
	{
        if (def.plant.harvestDestroys)
        {
            Destroy();
        }
        else
        {
            growthPercent -= 0.30f;

            if (growthPercent < 0.08f)
                growthPercent = 0.08f;

            Find.MapDrawer.MapChanged(Position, MapChangeType.Things);
        }

        //Note: does not take growth into account
        eater.needs.food.CurLevel += def.ingestible.nutrition; 
	}

	public void PlantCollected()
	{
		if( def.plant.harvestDestroys )
		{
			Destroy();
		}
		else
		{
			growthPercent = 0.08f;
			Find.MapDrawer.MapChanged(Position, MapChangeType.Things);
		}
	}
	
	private void CheckTemperatureMakeLeafless()
	{
		float diff = MaxLeaflessTemperature - MinLeaflessTemperature;
		float leaflessThresh = ((this.HashOffset() * 0.01f) % diff)-diff + MaxLeaflessTemperature;

		if( Position.GetTemperature() < leaflessThresh )
			MakeLeafless();
	}

	public void MakeLeafless()
	{
		bool changed = !LeaflessNow;

		madeLeaflessTick = Find.TickManager.TicksGame;

		if( def.plant.dieIfLeafless )
			TakeDamage( new DamageInfo( DamageDefOf.Rotting, 99999, null ) );	

		if( changed )
			Find.MapDrawer.MapChanged( Position, MapChangeType.Things );
	}



	public override void TickRare()
	{
		CheckTemperatureMakeLeafless();

		if( GenPlant.GrowthSeasonNow(Position) )
		{
			bool hasLight = HasEnoughLightToGrow;

			//Record light starvation
			if( !hasLight )
				ticksSinceLit += GenTicks.TickRareInterval;
			else
				ticksSinceLit = 0;
	
			//Grow
			if( GrowingNow )
			{
				growthPercent += GrowthPerTickRare * TemperatureEfficiency;

				if( LifeStage == PlantLifeStage.Mature )
				{
					//Newly matured

					Find.MapDrawer.MapChanged(Position, MapChangeType.Things);

					//Cultivated plant? Play the grown sound
					if( Find.Map.Biome.CommonalityOfPlant(def) == 0 )
						SoundHarvestReady.PlayOneShot(Position);
				}
			}		

			if( def.plant.LimitedLifespan )
			{
				//Age
				age += GenTicks.TickRareInterval;

				//Rot
				if( Rotting)
				{
					int rotDamAmount = Mathf.CeilToInt(RotDamagePerTick * GenTicks.TickRareInterval);
					TakeDamage( new DamageInfo(DamageDefOf.Rotting, rotDamAmount, null) );	
				}
			}
		
			//Reproduce
			if( !Destroyed )
				GenPlantReproduction.TickReproduceFrom(this);
		}
	}
	

	public int YieldNow()
	{
		if( !HarvestableNow )
			return 0;

		//If yield is 0, handle it
		if( def.plant.harvestYieldRange.max <= 1 )
			return Mathf.RoundToInt(def.plant.harvestYieldRange.max);

		float growthFactor = Mathf.InverseLerp( def.plant.harvestMinGrowth, 1 , growthPercent);

		//Start with max yield
		float yieldFloat = def.plant.harvestYieldRange.LerpThroughRange( growthFactor );

		//Factor down for health with a 50% lerp factor
		yieldFloat *=   Mathf.Lerp( 0.5f, 1f,  ((float)Health / (float)MaxHealth) );



		int yieldInt = Gen.RandomRoundToInt(yieldFloat);		
				
		//Food-yielding plants always give you a certain minimum food
		if( yieldInt < MinPlantYield )
			yieldInt = MinPlantYield;	
			
		return yieldInt;
	}


	public override void Print( SectionLayer layer )
	{
		Vector3 trueCenter = this.TrueCenter();

		Rand.Seed = Position.GetHashCode();//So our random generator makes the same numbers every time

		//Determine random local position variance
		float positionVariance;
		if( def.plant.maxMeshCount == 1 )
			positionVariance = 0.05f;
		else
			positionVariance = 0.50f;

		//Determine how many meshes to print
		int meshCount = Mathf.CeilToInt( growthPercent * def.plant.maxMeshCount );
		if( meshCount < 1 )
			meshCount = 1;

		//Grid width is the square root of max mesh count
		int gridWidth = 1;
		switch( def.plant.maxMeshCount )
		{
			case 1: gridWidth = 1; break;
			case 4: gridWidth = 2; break;
			case 9: gridWidth = 3; break;
			case 16: gridWidth = 4; break;
			case 25: gridWidth = 5; break;
			default: Log.Error(def + " must have plant.MaxMeshCount that is a perfect square."); break;
		}
		float gridSpacing = 1f/gridWidth; //This works out to give half-spacings around the edges

		//Shuffle up the position indices and place meshes at them
		//We do this to get even mesh placement by placing them roughly on a grid
		Vector3 adjustedCenter = Vector3.zero;
		Vector2 planeSize = Vector2.zero;
		int meshesYielded = 0;
		int posCount = posIndexList.Count;
		for(int i=0; i<posCount; i++ )
		{		
			int posIndex = posIndexList[i];

			//Determine plane size
			float size = def.plant.visualSizeRange.LerpThroughRange(growthPercent);

			//Determine center position
			if( def.plant.maxMeshCount == 1 )
			{
				adjustedCenter = trueCenter + new Vector3(Rand.Range(-positionVariance, positionVariance),
															 0,
															 Rand.Range(-positionVariance, positionVariance) );

				//Clamp bottom of plant to square bottom
				//So tall plants grow upward
				float squareBottom = Mathf.Floor(trueCenter.z);
				if( (adjustedCenter.z - (size/2f)) <  squareBottom )
					adjustedCenter.z = squareBottom + (size/2f);
			}
			else
			{
				adjustedCenter = Position.ToVector3(); //unshifted
				adjustedCenter.y = def.Altitude;//Set altitude

				//Place this mesh at its randomized position on the submesh grid
				adjustedCenter.x += 0.5f * gridSpacing;
				adjustedCenter.z += 0.5f * gridSpacing;
				int xInd = posIndex / gridWidth;
				int zInd = posIndex % gridWidth;
				adjustedCenter.x += xInd * gridSpacing;
				adjustedCenter.z += zInd * gridSpacing;
				
				//Add a random offset
				float gridPosRandomness = gridSpacing * GridPosRandomnessFactor;
				adjustedCenter += new Vector3(Rand.Range(-gridPosRandomness, gridPosRandomness),
											  0,
											  Rand.Range(-gridPosRandomness, gridPosRandomness) );
			}

			//Randomize horizontal flip
			bool flipped = Rand.Value < 0.5f;		

			//Randomize material
			Material mat = Graphic.MatSingle; //Pulls a random material

			//Set wind exposure value at each vertex by setting vertex color
			workingColors[1].a = workingColors[2].a = (byte)(255 * def.plant.topWindExposure);
			workingColors[0].a = workingColors[3].a = 0;

			if( def.graphicOverdraw )
				size += 2f;
			planeSize = new Vector2( size,size );
			Printer_Plane.PrintPlane( layer, 
									  adjustedCenter, 
									  planeSize,	
									  mat, 
									  flipUv: flipped, 
									  colors:  workingColors );


			meshesYielded++;
			if( meshesYielded >= meshCount )
				break;
		}

		if( def.sunShadowInfo != null)
		{
			//Brutal shadow positioning hack
			float shadowOffsetFactor = 0.85f;
			if( planeSize.y < 1 )
				shadowOffsetFactor = 0.6f; //for bushes
			else
				shadowOffsetFactor = 0.81f;	//for cacti

			Vector3 sunShadowLoc = adjustedCenter;
			sunShadowLoc.z -= (planeSize.y/2f) * shadowOffsetFactor;
			sunShadowLoc.y -= Altitudes.AltInc;

			Printer_Shadow.PrintShadow( layer, sunShadowLoc, def.sunShadowInfo );
		}
	}



	
	public override string GetInspectString()
	{
		StringBuilder sb = new StringBuilder();

		sb.Append(base.GetInspectString());
		sb.AppendLine();
		
		sb.AppendLine( "PercentGrowth".Translate(GrowthPercentString));

		if( LifeStage == PlantLifeStage.Sowing )
		{
		}
		else if( LifeStage == PlantLifeStage.Growing )
		{
			if( !HasEnoughLightToGrow )		
				sb.AppendLine("NotGrowingNow".Translate(def.plant.growMinGlow.HumanName().ToLower()) );
			else if( !GrowingNow )
				sb.AppendLine("NotGrowingNowResting".Translate());
			else
				sb.AppendLine("Growing".Translate() );
			
			sb.AppendLine("FullyGrownIn".Translate( TicksUntilFullyGrown.TickstoDaysString() ));

            float tempMultiplier = TemperatureEfficiency;
            if (!Mathf.Approximately(tempMultiplier, 1f))
            {
                if(Mathf.Approximately(tempMultiplier, 0f))
                    sb.AppendLine("OutOfIdealTemperatureRangeNotGrowing".Translate());
                else
                    sb.AppendLine("OutOfIdealTemperatureRange".Translate(Mathf.RoundToInt(tempMultiplier * 100f).ToString()));
            }
		}
		else if( LifeStage == PlantLifeStage.Mature )
		{
			if( def.plant.Harvestable )
				sb.AppendLine("ReadyToHarvest".Translate() );
			else
				sb.AppendLine("Mature".Translate() );
		}
		
		return sb.ToString();
	}
	
	public void CropBlighted()
	{
        if( Rand.Value < 0.85f )
			Destroy();
	}
}}