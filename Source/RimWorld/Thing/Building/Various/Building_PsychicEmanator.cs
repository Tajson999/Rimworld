using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld.SquadAI;
using Verse.Noise;

namespace RimWorld{
public enum PsychicDroneLevel
{
	None,
	Low,
	Medium,
	High,
	Extreme
}


class Building_PsychicEmanator : Building
{
	//Working vars
	public float				pointsLeft; //Configured externally
	private int					age = 0;
	private int					ticksToInsanityPulse;
	private int					ticksToIncreaseDroneLevel;
	private int					ticksToPlantHarm;
	public PsychicDroneLevel	droneLevel = PsychicDroneLevel.Low;
	private Brain				squadBrain;
	private float				snowRadius = 0f;
	private ModuleBase			snowNoise = null;

	//Constants
	private const int			PlantHarmInterval = 4;
	private const int			DroneLevelIncreaseInterval = GenDate.TicksPerDay * 5;
    private static readonly		IntRange InsanityPulseInterval = new IntRange( GenDate.TicksPerDay * 2, GenDate.TicksPerDay *5);
    private const int			AnimalInsaneRadius = 25;
	private const float			MechanoidsDefendRadius = 21;
	private const int			SnowExpandInterval = 500;
	private const float			SnowAddAmount = 0.12f;
	private const float			SnowMaxRadius = 55;

    public override void SpawnSetup()
    {
        base.SpawnSetup();

        ticksToInsanityPulse = InsanityPulseInterval.RandomInRange;
		ticksToIncreaseDroneLevel = DroneLevelIncreaseInterval;

		SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera();
    }


    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Values.LookValue(ref ticksToInsanityPulse, "ticksToInsanityPulse");
        Scribe_Values.LookValue(ref ticksToIncreaseDroneLevel, "ticksToIncreaseDroneLevel");
        Scribe_Values.LookValue(ref age, "age");
        Scribe_Values.LookValue(ref pointsLeft, "mechanoidPointsLeft");
        Scribe_Values.LookValue(ref droneLevel, "droneLevel");
		Scribe_Values.LookValue(ref snowRadius, "snowRadius");
		Scribe_References.LookReference( ref squadBrain, "defenseBrain" );
    }


    public override string GetInspectString()
    {
        StringBuilder str = new StringBuilder();
        str.AppendLine(base.GetInspectString());
        str.AppendLine("AwokeDaysAgo".Translate( age.TicksToDays().ToString("F1") )  );

		string droneLevelString = "Error";
		switch( droneLevel )
		{
			case PsychicDroneLevel.Low: droneLevelString = "PsychicDroneLevelLow".Translate();
				break;
			case PsychicDroneLevel.Medium: droneLevelString = "PsychicDroneLevelMedium".Translate();
				break;
			case PsychicDroneLevel.High: droneLevelString = "PsychicDroneLevelHigh".Translate();
				break;
			case PsychicDroneLevel.Extreme: droneLevelString = "PsychicDroneLevelExtreme".Translate();
				break;
		}

		str.AppendLine("PsychicDroneLevel".Translate( droneLevelString ) );

        return str.ToString();
    }

    public override void Tick()
    {
        base.Tick();

		age++;

        ticksToInsanityPulse--;
		if (ticksToInsanityPulse <= 0 )
		{
			DoAnimalInsanityPulse();
			ticksToInsanityPulse = InsanityPulseInterval.RandomInRange;
		}

		ticksToIncreaseDroneLevel--;
		if( ticksToIncreaseDroneLevel <= 0 )
		{
			IncreaseDroneLevel();
			ticksToIncreaseDroneLevel = DroneLevelIncreaseInterval;
		}

		ticksToPlantHarm--;
		if( ticksToPlantHarm <= 0 )
			HarmPlant();

		if( (Find.TickManager.TicksGame + this.HashOffset()) % SnowExpandInterval == 0 )
			ExpandSnow();


		//Maybe: do single colonist insanity
		/*
		if( Find.TickManager.tickCount % 100 == 0
			&& Rand.Value < ColonistInsanityChancePer100Ticks )
			DoSingleColonistInsanity();
		 * */
    }

	public override void PreApplyDamage(DamageInfo dinfo, out bool absorbed)
	{
		//Note we try to spawn mechanoids before ApplyDamage
		// in case the part dies in one big hit.
		if (dinfo.Def.harmsHealth)
		{
			float finalHealth = Health - dinfo.Amount;
			if ((finalHealth < MaxHealth * 0.98f && (dinfo.Instigator != null && dinfo.Instigator.Faction != null))
				|| finalHealth < MaxHealth * 0.7f)
				TrySpawnMechanoids();
		}

		absorbed = false;
	}

	private void TrySpawnMechanoids()
	{
		if( pointsLeft <= 0 )
			return;

		if( squadBrain == null )
		{
			var stateGraph = GraphMaker.MechanoidsDefendShipGraph( this, MechanoidsDefendRadius );
			squadBrain = BrainMaker.MakeNewBrain(Faction.OfMechanoids, stateGraph);
		}

		while( true )
		{
			PawnKindDef mechDef;
			if( !DefDatabase<PawnKindDef>.AllDefs.Where( def=>def.race.race.mechanoid 
														  && def.isFighter
														  && def.pointsCost <= pointsLeft )
														  .TryRandomElement( out mechDef ) )
				break;

			IntVec3 spawnSpot;
			if (!GenAdj.CellsAdjacent8Way(this).Where( cell=>CanSpawnMechanoidAt(cell) ).TryRandomElement(out spawnSpot) )
				break;

			Pawn mech = PawnGenerator.GeneratePawn( mechDef, Faction.OfMechanoids );

			if (!GenPlace.TryPlaceThing(mech, spawnSpot, ThingPlaceMode.Near))
				break;

			squadBrain.AddPawn(mech);
			pointsLeft -= mech.kindDef.pointsCost;
		}

		pointsLeft = 0;
		SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera();
	}

	private bool CanSpawnMechanoidAt( IntVec3 c )
	{
		if( !c.Walkable() )
			return false;

		return true;
	}

	private void TrySwitchMechanoidsToAssaultMode()
	{
		if( squadBrain.curState is State_DefendPoint )
		{
			squadBrain.ReceiveMemo( "AssaultColony" );
		}
	}


	/*
	private void DoSingleColonistInsanity()
	{
		Pawn colonist = Find.ListerPawns
						.FreeColonists
						.Where( pa=>!pa.Incapacitated && pa.GetStatValue( StatDefOf.PsychicSensitivity ) > 0.1f )
						.RandomElement();

		if( colonist == null )
			return;

        Find.History.AddGameEvent("LetterShipPartDroveColonistInsane".Translate(colonist.Name.nick), GameEventType.BadNonUrgent, true);

		int selector = Rand.Range(0,100);
		if( selector < 40 )
			PsychologyUtility.TryDoMentalBreak( colonist, SanityState.DazedWander );
		else
			PsychologyUtility.TryDoMentalBreak( colonist, SanityState.Psychotic );

		SoundDef.Named("PsychicPulseGlobal").PlayOneShotOnCamera();
	}*/

	private void ExpandSnow()
	{
		if( snowNoise == null )
		{
			snowNoise = new Perlin( 0.055f, 2, 0.5f, 5, Rand.Range(0, 651431), QualityMode.Medium );
		}

		if( snowRadius < 8 )
			snowRadius += 1.3f;
		else if( snowRadius < 17 )
			snowRadius += 0.7f;
		else if( snowRadius < 30 )
			snowRadius += 0.4f;
		else
			snowRadius += 0.1f;

		if( snowRadius > SnowMaxRadius )
			snowRadius = SnowMaxRadius;

		int numCells = GenRadial.NumCellsInRadius(snowRadius);
		for( int i=0; i<numCells; i++ )
		{
			IntVec3 c = Position + GenRadial.RadialPattern[i];

			float noiseVal = snowNoise.GetValue(c);
			noiseVal += 1;
			noiseVal *= 0.5f;
			if( noiseVal < 0.1f )
				noiseVal = 0.1f;

			if( Find.SnowGrid.GetDepth(c) > noiseVal )
				continue;

			float dist = (c-Position).LengthHorizontal;
			float falloff = 1f-(dist/snowRadius);

			Find.SnowGrid.AddDepth( c, falloff * SnowAddAmount * noiseVal );

		}
	}

	private void HarmPlant()
	{
		float dir = Rand.Range(0f, 360f);
		float dist = Rand.Range(0f, 20f);

		Quaternion rotation = Quaternion.AngleAxis(dir, Vector3.up);
		Vector3 forward = Vector3.forward * dist;
		Vector3 final = rotation * forward;

		IntVec3 offset = IntVec3.FromVector3(final);

		IntVec3 cell = Position + offset;

		if( cell.InBounds() )
		{
			Plant p = cell.GetPlant();
			if( p != null )
			{
				if( Rand.Value < 0.2f )
					p.Destroy(DestroyMode.Kill);
				else
				{
					p.MakeLeafless();
				}
			}
		}

		ticksToPlantHarm = PlantHarmInterval;
	}

	private void IncreaseDroneLevel()
	{
		if( droneLevel == PsychicDroneLevel.Extreme )
			return;

		droneLevel++;

		string letter = "LetterPsychicDroneLevelIncreased".Translate();
        Find.History.AddGameEvent(letter, GameEventType.BadNonUrgent, true);

		SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera();
	}

    private void DoAnimalInsanityPulse()
    {
        // Find the animals to work with
        IEnumerable<Pawn> nearbyAnimals = 
			Find.ListerPawns.AllPawns.Where(p => p.RaceProps.IsAnimal 
				&& p.Position.InHorDistOf(Position, AnimalInsaneRadius));

        // Make all animals psychotic (60% chance) 
        foreach (Pawn animal in nearbyAnimals)
        {
			animal.mindState.broken.StartBrokenState(BrokenStateDefOf.Psychotic);
        }

		Messages.Message( "MessageAnimalInsanityPulse".Translate() );

		Find.CameraMap.shaker.DoShake( 4.0f );
		SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera();
    }

	/*
    private void DoSelfDestruct()
    {
        Destroy(DestroyMode.Vanish);
        Explosion.DoExplosion(Position, 25.0f, DamageDefOf.Flame, null);

		Messages.Message( "MessageShipPartSelfDestruct".Translate() );

    }
	*/
}}
