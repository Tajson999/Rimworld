using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;



namespace Verse.AI{
public static class Toils_General
{
	public static Toil Wait( int ticks )
	{
		Toil toil = new Toil();
		toil.initAction = ()=>
			{
				toil.actor.pather.StopDead();
			};
		toil.defaultCompleteMode = ToilCompleteMode.Delay;
		toil.defaultDuration = ticks;
		return toil;
	}

	public static Toil RemoveDesignationsOnThing( TargetIndex ind, DesignationDef def )
	{
		Toil toil = new Toil();
		toil.initAction = ()=>
			{
				Find.DesignationManager.RemoveAllDesignationsOn( toil.actor.jobs.curJob.GetTarget(ind).Thing );
			};
		return toil;

	}
}}

