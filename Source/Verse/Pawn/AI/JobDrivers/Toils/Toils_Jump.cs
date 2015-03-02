using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Verse.AI{
public static class Toils_Jump
{
	public static Toil Jump( Toil jumpTarget )
	{
		Toil toil = new Toil();
		toil.initAction = ()=>
		{
			toil.actor.jobs.curDriver.JumpToToil(jumpTarget);
		};
		return toil;
	}

	public static Toil JumpIf( Toil jumpTarget, Func<bool> condition )
	{
		Toil toil = new Toil();
		toil.initAction = ()=>
		{
			if( condition() )
				toil.actor.jobs.curDriver.JumpToToil(jumpTarget);
		};
		return toil;
	}

	public static Toil JumpIfTargetDespawned( TargetIndex ind, Toil jumpToil )
	{
		Toil toil = new Toil();
		toil.initAction = ()=>
			{
				if( !toil.actor.jobs.curJob.GetTarget(ind).Thing.SpawnedInWorld )
					toil.actor.jobs.curDriver.JumpToToil(jumpToil);
			};
		return toil;
	}


	public static Toil JumpIfCannotHitTarget( TargetIndex ind, Toil jumpToil )
	{
		Toil toil = new Toil();
		toil.initAction = ()=>
			{
				if( !toil.actor.jobs.curJob.verbToUse.CanHitTarget( toil.actor.jobs.curJob.GetTarget(ind) ) )
					toil.actor.jobs.curDriver.JumpToToil(jumpToil);
			};
		return toil;
	}

	public static Toil JumpIfHaveTargetInQueue( TargetIndex ind, Toil jumpToil )
	{
		Toil toil = new Toil();
		toil.initAction = ()=>
		{
			Pawn actor = toil.actor;
			Job curJob = actor.jobs.curJob;

			List<TargetInfo> queue = curJob.GetTargetQueue(ind);
			if( !queue.NullOrEmpty() )
				actor.jobs.curDriver.JumpToToil(jumpToil);
		};

		return toil;
	}
}}