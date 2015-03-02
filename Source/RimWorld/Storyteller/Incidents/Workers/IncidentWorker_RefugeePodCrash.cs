using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;


namespace RimWorld{

public class IncidentWorker_RefugeePodCrash : IncidentWorker
{
	private const float FogClearRadius = 4.5f;



	public override bool TryExecute( IncidentParms parms )
	{
		IntVec3 dropSpot = RCellFinder.RandomDropSpot();

        Find.History.AddGameEvent("RefugeePodCrash".Translate(), GameEventType.BadNonUrgent, true, dropSpot);

		Faction fac = Find.FactionManager.FirstFactionOfDef( FactionDefOf.Spacer );

		Pawn refugee = PawnGenerator.GeneratePawn( PawnKindDef.Named("SpaceRefugee"), fac );
		HealthUtility.GiveInjuriesToForceDowned(refugee);

		DropPodInfo podInfo = new DropPodInfo();
		podInfo.SingleContainedThing = refugee;
		podInfo.openDelay = 180;
		podInfo.leaveSlag = true;
		DropPodUtility.MakeDropPodAt( dropSpot, podInfo );	

		Find.Storyteller.intenderPopulation.Notify_PopulationGainIncident();
		return true;
	}
}}
