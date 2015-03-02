using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld{

public class IncidentWorker_Eclipse : IncidentWorker
{
	public override bool StorytellerCanUseNow()
	{
		return !Find.MapConditionManager.ConditionIsActive( MapConditionDefOf.Eclipse );
	}

	public override bool TryExecute( IncidentParms parms )
	{
		if( Find.MapConditionManager.ConditionIsActive(MapConditionDefOf.Eclipse) )
			return false;

		int eclipseDuration = Mathf.RoundToInt(Rand.Range(1.5f,2.5f) * GenDate.TicksPerDay);
		Find.MapConditionManager.RegisterCondition( new MapCondition_Eclipse( eclipseDuration)  );

		string letterString = "EclipseIncident".Translate();
        Find.History.AddGameEvent(letterString, GameEventType.BadNonUrgent, true);


		return true;
	}
}}