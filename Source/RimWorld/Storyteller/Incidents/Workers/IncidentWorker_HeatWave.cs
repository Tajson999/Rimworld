using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;


namespace RimWorld{

public class IncidentWorker_HeatWave : IncidentWorker
{
	public override bool StorytellerCanUseNow()
	{
		if( GenTemperature.SeasonalTemp < 20 )
			return false;

		return !Find.MapConditionManager.ConditionIsActive( MapConditionDefOf.HeatWave );
	}

	public override bool TryExecute( IncidentParms parms )
	{	
		if( Find.MapConditionManager.ConditionIsActive(MapConditionDefOf.HeatWave) )
			return false;

		int duration = Rand.Range(2,5) * GenDate.TicksPerDay;
		Find.MapConditionManager.RegisterCondition( new MapCondition_HeatWave(duration));
	
        Find.History.AddGameEvent("LetterHeatWave".Translate(), GameEventType.BadNonUrgent, true);

		return true;
	}
}

public class IncidentWorker_ColdSnap : IncidentWorker
{
	public override bool StorytellerCanUseNow()
	{
		if( GenTemperature.SeasonalTemp < 0 || GenTemperature.SeasonalTemp > 10 )
			return false;

		return !Find.MapConditionManager.ConditionIsActive( MapConditionDefOf.ColdSnap );
	}

	public override bool TryExecute( IncidentParms parms )
	{	
		if( Find.MapConditionManager.ConditionIsActive(MapConditionDefOf.ColdSnap) )
			return false;

		int duration = Rand.Range(2,5) * GenDate.TicksPerDay;
		Find.MapConditionManager.RegisterCondition( new MapCondition_ColdSnap(duration));
	
        Find.History.AddGameEvent("LetterColdSnap".Translate(), GameEventType.BadNonUrgent, true);

		return true;
	}
}








}