using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;



namespace Verse.AI{
public static class Toils_Goto
{
	//Constants
	private const int TicksBetweenBreaks = 400;

	public static Toil GotoThing( TargetIndex ind, PathMode pathMode)
	{
		Toil toil = new Toil();
		toil.initAction = ()=>
		{
			Pawn actor = toil.actor;
			actor.pather.StartPath( actor.jobs.curJob.GetTarget(ind), pathMode );		
		};
		toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
		toil.FailOnDespawned(ind);
		return toil;
	}
    
    public static Toil GotoThing( TargetIndex ind, IntVec3 exactCell )
    {
        Toil toil = new Toil();
        toil.initAction = ()=>
        {
            Pawn actor = toil.actor;
            actor.pather.StartPath( exactCell, PathMode.OnCell );       
        };
        toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
        toil.FailOnDespawned(ind);
        return toil;
    }

	//GotoLoc does not have fail conditions
	public static Toil GotoCell( TargetIndex ind, PathMode pathMode)
	{
		Toil toil = new Toil();
		toil.initAction = ()=>
		{
			Pawn actor = toil.actor;
			actor.pather.StartPath( actor.jobs.curJob.GetTarget(ind), pathMode );		
		};
		toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
		return toil;
	}








	public static Toil GetOutOfBlueprint(bool onlyTarget, TargetIndex targetInd)
	{
		Toil toil = new Toil();
		toil.initAction = () =>
		{
			Pawn actor = toil.actor;

			Thing blue = Find.ThingGrid.ThingAt(actor.Position, EntityType.Blueprint);
			if( blue == null )
			{
				//Already not on blueprint
				actor.jobs.curDriver.BeginNextToil();
				return;
			}

			Job curJob = actor.jobs.curJob;

			if( onlyTarget )
			{
				//Find the blueprint I already had targeted
				Thing targetBlue = curJob.GetTarget(targetInd).Thing as Blueprint;
				if( blue != targetBlue )
				{
					//Blueprint I'm on isn't my target; it's okay
					actor.jobs.curDriver.BeginNextToil();
					return;
				}
			}

			//Escape the blueprint
			//Optimization opportunity
			{
				//First try to find a nice standable spot
				foreach( IntVec3 c in GenAdj.CellsAdjacent8Way(blue).InRandomOrder() )
				{
					if( c.Standable() )
					{
						actor.pather.StartPath( c, PathMode.OnCell );
						return;
					}
				}

				//Didn't find a standable spot. Try for anything walkable. Anything at all
				//This handles cases like if you're making a sandbag surrounded by other sandbags
				//Or a building surrounded by rocks and walls
				foreach( IntVec3 c in GenAdj.CellsAdjacent8Way(blue).InRandomOrder() )
				{
					if( c.Walkable() )
					{
						actor.pather.StartPath( c, PathMode.OnCell );
						return;
					}
				}
			}

			//Could not escape blueprint
			actor.jobs.EndCurrentJob(JobCondition.Incompletable);
		};
		toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
		return toil;
	}




		/*
	public static Toil GetToTargetA( Func<bool> extraFailCondition )
	{
		Toil toil = new Toil();
		toil.initAction = ()=>
		{
			toil.actor.pather.StartPathTowards( 
		};
		toil.completeMode = ToilCompleteMode.PatherArrival;
		return toil;
	}
	*/


}}

