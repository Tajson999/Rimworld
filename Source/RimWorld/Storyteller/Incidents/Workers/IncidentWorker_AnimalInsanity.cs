using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;



namespace RimWorld{

	
//Special case of AnimalInsanity
public class IncidentWorker_AnimalInsanitySingle : IncidentWorker
{
	private const int FixedPoints = 30; //one squirrel

	public override bool TryExecute( IncidentParms parms )
	{
		int maxPoints = 150;
		if( GenDate.DaysPassed < 14 )
			maxPoints = 40;

		//Choose an animal type
		List<Pawn> validAnimals = Find.ListerPawns.AllPawns
							.Where( p => !p.RaceProps.humanoid
								&& p.kindDef.pointsCost <= maxPoints
								&& p.SpawnedInWorld
								&& !p.Position.Fogged() )
							.ToList();

		if( validAnimals.Count == 0 )
			return false;

		Pawn animal = validAnimals.RandomElement();
		animal.mindState.broken.StartBrokenState(BrokenStateDefOf.Psychotic);

		string letter;
		letter = "AnimalInsanitySingle".Translate(  animal.Label );
        Find.History.AddGameEvent(letter, GameEventType.BadUrgent, true, animal);
		return true;
	}
}






public class IncidentWorker_AnimalInsanity : IncidentWorker
{
	public override bool TryExecute( IncidentParms parms )
	{
		if( parms.points <= 0 )
		{
			Log.Error("AnimalInsanity running without points.");
			parms.points = (int)(Find.StoryWatcher.watcherStrength.StrengthRating * 50);
		}

		float adjustedPoints = parms.points;
		if( adjustedPoints > 250 )
		{
			//Halve the amount of points over 250
			adjustedPoints -= 250;
			adjustedPoints *= 0.5f;
			adjustedPoints += 250;
		}


		//Choose an animal kind
		IEnumerable<PawnKindDef> animalKinds = DefDatabase<PawnKindDef>.AllDefs
												.Where( def => !def.race.race.humanoid
														&& def.pointsCost <= adjustedPoints
														&& Find.ListerPawns.AllPawns.Where(p=>p.kindDef == def 
																							&& p.SpawnedInWorld
																							&& !p.Position.Fogged() ).Count() >= 3 );

		PawnKindDef animalDef;
		if( !animalKinds.TryRandomElement(out animalDef) )
			return false;

		List<Pawn> allUsableAnimals = Find.ListerPawns.AllPawns
												.Where(p=>p.kindDef == animalDef && p.SpawnedInWorld && !p.Position.Fogged() )
												.ToList();

		float pointsPerAnimal = animalDef.pointsCost;
		float pointsSpent = 0;
		int animalsMaddened = 0;
        Pawn lastAnimal = null;
		allUsableAnimals.Shuffle();
		foreach( Pawn animal in allUsableAnimals )
		{
			if( pointsSpent+pointsPerAnimal > adjustedPoints )
				break;

			animal.mindState.broken.StartBrokenState(BrokenStateDefOf.Psychotic);

			pointsSpent += pointsPerAnimal;
			animalsMaddened++;
            lastAnimal = animal;
		}

		//Not enough points/animals for even one animal to be maddened
		if( pointsSpent == 0 )
			return false;

		string letter;
		if( animalsMaddened == 1 )
			letter = "AnimalInsanitySingle".Translate(  animalDef.label );
		else
			letter = "AnimalInsanityMultiple".Translate(  animalDef.label );

        Find.History.AddGameEvent(letter, GameEventType.BadUrgent, true, lastAnimal);

		SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera();

		Find.CameraMap.shaker.DoShake( 1.0f );

		return true;
	}
}

}