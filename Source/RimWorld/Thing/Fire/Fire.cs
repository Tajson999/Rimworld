using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;




namespace RimWorld{
public class Fire : AttachableThing, SizeReporter
{
	//Working vars - gameplay
	public float			fireSize = MinFireSize; //1 is a medium-sized campfire
	private int				ticksSinceSpark; //Unsaved, unimportant
	private float			flammabilityMax = 0.5f; //Updated only periodically


	//Working vars - audiovisual
	private int				ticksUntilSmoke = 0;
	private Sustainer		sustainer = null;

	//Working vars  -fast
	private static List<Thing> flammableList = new List<Thing>();



	//Constants and content
	private static readonly SoundDef BurningSoundDef = SoundDef.Named("FireBurning");

	public const float		MinFireSize = 0.1f;
	private const float		MinSizeForSpark = 1f;
	private const float		TicksBetweenSparksBase = 150; //Halves for every fire size
	private const float		TicksBetweenSparksReductionPerFireSize = 40;
	private const float		MinTicksBetweenSparks = 75;
	private const float		MinFireSizeToEmitSpark = 1f;
	private const float		MaxFireSize = 1.75f;

	private const float		CellIgniteChancePerTickPerSize = 0.01f;
	private const int		CellIgniteCheckInterval = 150;
	private const float		MinSizeForIgniteMovables = 0.4f;

	private const float		FireBaseGrowthPerTick = 0.00065f;
	private const int		FireGrowCheckInterval = 50;

	private static readonly IntRange SmokeIntervalRange = new IntRange(130,200);
	private const int		SmokeIntervalRandomAddon = 10;

	private const float		BaseSkyExtinguishChance = 0.04f;
	private const int		BaseSkyExtinguishDamage = 4;

    private const int       RoomHeatInterval = 150;

	private const float		SnowClearRadiusPerFireSize = 3f;
	private const float		SnowClearDepthFactor = 0.1f;

	//Properties
	public override string Label
	{
		get
		{
			if( parent != null )
				return "FireOn".Translate( parent.LabelCap);	
			else
				return "Fire".Translate();
		}
	}
	public override string InspectStringAddon
	{
		get
		{
			return "Burning".Translate() + " (" + "FireSizeLower".Translate( (fireSize*100).ToString("F0") ) + ")";	
		}
	}
	private float SparkInterval
	{
		get
		{
			if( fireSize < MinSizeForSpark )
				return 999999;

			float ticks = TicksBetweenSparksBase - (fireSize-1)*TicksBetweenSparksReductionPerFireSize;

			if( ticks < MinTicksBetweenSparks )
				ticks = MinTicksBetweenSparks;

			return ticks;
		}
	}



	public override void ExposeData()
	{
		base.ExposeData();

		Scribe_Values.LookValue(ref fireSize, "fireSize");
	}

	public override void SpawnSetup()
	{
		base.SpawnSetup();
		RecalcPathsOnAndAroundMe();
		ConceptDecider.TeachOpportunity(ConceptDefOf.HomeRegion, this, OpportunityType.Important );

		ticksSinceSpark = (int)(SparkInterval * Rand.Value);

		SoundInfo info = SoundInfo.InWorld(this, MaintenanceType.PerTick);
		sustainer = SustainerAggregatorUtility.AggregateOrSpawnSustainerFor(this, BurningSoundDef, info);
	}

	public float CurrentSize()
	{
		return fireSize;
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		if( sustainer.externalParams.sizeAggregator == null )
			sustainer.externalParams.sizeAggregator = new SoundSizeAggregator();
		sustainer.externalParams.sizeAggregator.RemoveReporter(this);

		base.Destroy(mode);
		RecalcPathsOnAndAroundMe();
	}

	private void RecalcPathsOnAndAroundMe()
	{
		for( int i=0; i<GenAdj.AdjacentCellsAndInside.Length; i++ )
		{
			IntVec3 c = Position + GenAdj.AdjacentCellsAndInside[i];

			if( !c.InBounds() )
				continue;

			Find.PathGrid.RecalculatePerceivedPathCostAt(c);
		}
	}

	public override void AttachTo(Thing parent)
	{
		base.AttachTo(parent);

		Pawn p = parent as Pawn;
		if( p != null )
			TaleRecorder.RecordTale( TaleDef.Named("WasOnFire"), p );
	}
	
	public override void Tick()
	{
		sustainer.Maintain();

		Profiler.BeginSample("Fire tick");

        Profiler.BeginSample("Spawn particles");

		//Do micro sparks
		if( fireSize > 0.7f )
		{
			if( Rand.Value < fireSize * 0.01f )
			{
				MoteThrower.ThrowMicroSparks(DrawPos);
			}
		}

		//Do smoke and glow
		ticksUntilSmoke--;
		if( ticksUntilSmoke <= 0 )
		{
			MoteThrower.ThrowSmoke( DrawPos, fireSize );

			if( parent == null )//No fire glow for moving things (it trails them)
			{
				Vector3 glowLoc = DrawPos + fireSize*( new Vector3(Rand.Value-0.5f,0,Rand.Value-0.5f)   );
				MoteThrower.ThrowFireGlow( glowLoc, fireSize );
			}

			float firePct = fireSize / 2;
			if( firePct > 1 ) firePct = 1;
			firePct = 1f-firePct;
			ticksUntilSmoke = SmokeIntervalRange.Lerped(firePct) + (int)(SmokeIntervalRandomAddon*Rand.Value);
		}

		//Emit sparks
		ticksSinceSpark++;
		if( ticksSinceSpark >= SparkInterval )
		{
			ThrowSpark();
			ticksSinceSpark = 0;
		}
		Profiler.EndSample();



		if( Gen.IsHashIntervalTick( this, CellIgniteCheckInterval ) )
		{
			Profiler.BeginSample("Get flammables list");
			//Determine list of flammables in my cell
			flammableList.Clear();
			flammabilityMax = 0;
			List<Thing> cellThings = null;
			if( parent == null )
			{
				//Burn anything in the cell
				cellThings = Find.ThingGrid.ThingsListAt(Position);
				for( int i=0; i<cellThings.Count; i++ )
				{
					var thingFlam = cellThings[i].GetStatValue( StatDefOf.Flammability );

					if( thingFlam < 0.01f )
						continue;

					flammableList.Add(cellThings[i]);

					if( thingFlam > flammabilityMax )
						flammabilityMax = thingFlam;
				}
			}
			else
			{
				//Burn only my parent
				flammableList.Add( parent );
				flammabilityMax = parent.GetStatValue( StatDefOf.Flammability );
			}


			Profiler.EndSample();


			Profiler.BeginSample("Do damage");
			//Choose what I'm going to damage something
			Thing damTarget = null;
			if( parent != null )
				damTarget = parent;	//Damage parent
			else if( flammableList.Count > 0 )
				damTarget = flammableList.RandomElement(); //Damage random flammable thing in cell
			
			//Damage whatever we're supposed to damage
			if( damTarget != null )
			{
				//We don't damage the target if it's not our parent, it would attach a fire, and we're too small
				//This is to avoid tiny fires igniting passing pawns
				if( !(fireSize < MinSizeForIgniteMovables
					  && damTarget != parent
					  && damTarget.CanEverAttachFire()) )
				{
					DoFireDamage( damTarget );
				}
			}

			//Static fires: Ignite movables in my cell
			if( parent == null && fireSize > MinSizeForIgniteMovables )
			{
				for( int i=0; i<cellThings.Count; i++ )
				{
					if( cellThings[i].CanEverAttachFire() )
						cellThings[i].TryAttachFire(fireSize*0.2f);
				}
			}
			
			Profiler.EndSample();
		}

		//Destroy if nothing to burn in cell
		if( flammabilityMax < 0.01f )
		{
			Destroy();
			return;
		}

        // Emit heat into room, if any
        Profiler.BeginSample("Room heat");
        if( Gen.IsHashIntervalTick(this, RoomHeatInterval) )
        {
			//Push some heat
            // enough energy to heat up 100 cells by 1 degree for a size 1.0 fire
            float fireEnergy = fireSize * 100.0f;
			GenTemperature.PushHeat(Position, fireEnergy);

			//Clear some snow around the fire
			{
				float snowClearRadius = fireSize * SnowClearRadiusPerFireSize;
				SnowUtility.AddSnowRadial( Position, snowClearRadius, -(fireSize * SnowClearDepthFactor) );
			}
        }
        Profiler.EndSample();

        Profiler.BeginSample("Grow/extinguish");
		if( Gen.IsHashIntervalTick(this, FireGrowCheckInterval) )
		{
			//Try to grow the fire
			fireSize += FireBaseGrowthPerTick
					  * flammabilityMax
					  * FireGrowCheckInterval;

			if( fireSize > MaxFireSize )
				fireSize = MaxFireSize;

			//Extinguish from sky (rain etc)
			if( Find.WeatherManager.RainRate > 0.01f )
			{
				Thing building = Position.GetEdifice();
				bool roofHolderIsHere = building != null && building.def.holdsRoof;
			
 				if( roofHolderIsHere || !Find.RoofGrid.Roofed(Position) )
				{
					if( Rand.Value < BaseSkyExtinguishChance * FireGrowCheckInterval )
					{
						TakeDamage( new DamageInfo(DamageDefOf.Extinguish, BaseSkyExtinguishDamage, null) );
					}
				}
			}
		}
        Profiler.EndSample();

		Profiler.EndSample();//Fire tick
	}

	private void DoFireDamage( Thing damTarget )
	{
		BodyPartDamageInfo part = new BodyPartDamageInfo(null, BodyPartDepth.Outside);

		float damPerTick = 0.0125f + (0.0032f * fireSize);
		damPerTick = Mathf.Clamp( damPerTick, 0.0125f, 0.05f );
		int damAmount = GenMath.RoundRandom(damPerTick * CellIgniteCheckInterval);
		if( damAmount < 1 )
			damAmount = 1;

		damTarget.TakeDamage( new DamageInfo( DamageDefOf.Flame, damAmount, this, part ) );

		//Damage a random apparel
		Pawn p = damTarget as Pawn;
		if( p != null && p.apparel != null )
		{
			Apparel ap;
			if( p.apparel.WornApparelListForReading.TryRandomElement(out ap) )
				ap.TakeDamage( new DamageInfo( DamageDefOf.Flame, damAmount, this ) );
		}
	}

	public override void PostApplyDamage(DamageInfo dinfo)
	{
		if( !Destroyed && dinfo.Def == DamageDefOf.Extinguish )
		{
			//One damage reduces fireSize by 0.01f
			fireSize -= dinfo.Amount / 100f;

			if( fireSize <= MinFireSize )
			{
				Destroy();
				return;
			}
		}
	}
	
	//Spread randomly to one Thing in this or an adjacent square
	protected void ThrowSpark()
	{
		IntVec3 targLoc = Position;
		if( Rand.Value < 0.8f )
			targLoc = Position + GenRadial.ManualRadialPattern[ Rand.RangeInclusive(1,8) ];	//Spark adjacent
		else
			targLoc = Position + GenRadial.ManualRadialPattern[ Rand.RangeInclusive(10,20) ];	//Spark distant
		
		Spark sp = (Spark)GenSpawn.Spawn( ThingDef.Named("Spark"), Position );
		sp.Launch( this, new TargetInfo(targLoc) );
	}

}}