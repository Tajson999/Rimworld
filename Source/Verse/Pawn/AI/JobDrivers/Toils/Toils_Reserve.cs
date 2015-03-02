using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;



namespace Verse.AI{
public static class Toils_Reserve
{
	public static Toil Reserve( TargetIndex ind, ReservationType rType )
	{
		Toil toil = new Toil();
		toil.initAction = ()=>
		{
			if( !Find.Reservations.TryReserve( toil.actor, toil.actor.jobs.curJob.GetTarget(ind), rType ) )
				toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		return toil;
	}

	public static Toil ReserveQueue( TargetIndex ind, ReservationType rType )
	{
		Toil toil = new Toil();
		toil.initAction = ()=>
		{
			var queue = toil.actor.jobs.curJob.GetTargetQueue(ind);
			if( queue != null )
			{
				for( int i=0; i<queue.Count; i++ )
				{
					if( !Find.Reservations.TryReserve( toil.actor, queue[i], rType ) )
						toil.actor.jobs.EndCurrentJob(JobCondition.Incompletable);
				}
			}
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		return toil;
	}
	
	public static Toil Unreserve( TargetIndex ind ,  ReservationType rType )
	{
		Toil toil = new Toil();
		toil.initAction = ()=>
		{
			Find.Reservations.Release( toil.actor.jobs.curJob.GetTarget(ind), rType, toil.actor );
		};
		toil.defaultCompleteMode = ToilCompleteMode.Instant;
		return toil;
	}


}}

