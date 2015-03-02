using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Verse;
using RimWorld;


namespace Verse.AI{
public class JobDriver_HaulToCell : JobDriver
{
	//Constants
	private const TargetIndex HaulableInd = TargetIndex.A;
	private const TargetIndex StoreCellInd = TargetIndex.B;


	public JobDriver_HaulToCell(Pawn pawn) : base(pawn){}
	
	public override string GetReport()
	{
		IntVec3 destLoc = pawn.jobs.curJob.targetB.Cell;

		Thing hauledThing = null;
		if( pawn.carryHands.CarriedThing != null )
			hauledThing = pawn.carryHands.CarriedThing;
		else
			hauledThing = TargetThingA;

		string destName = null;
		SlotGroup destGroup = StoreUtility.GetSlotGroup(destLoc);
		if( destGroup != null )
			destName = destGroup.parent.SlotYielderLabel();

		string repString;
		if( destName != null )
			repString = "ReportHaulingTo".Translate( hauledThing.LabelCap, destName);
		else
			repString = "ReportHauling".Translate( hauledThing.LabelCap );

		return repString;
	}
	
	protected override IEnumerable<Toil> MakeNewToils()
	{
		//Set fail conditions
		this.FailOnDestroyed( HaulableInd );
		this.FailOnBurningImmobile( StoreCellInd );
		//Note we only fail on forbidden if the target doesn't start that way
		//This helps haul-aside jobs on forbidden items
		if( !TargetThingA.IsForbidden( pawn.Faction ) )
			this.FailOnForbidden( HaulableInd );


		//Reserve target storage cell
		yield return Toils_Reserve.Reserve( StoreCellInd, ReservationType.Store );

		//Reserve thing to be stored
		Toil reserveTargetA = Toils_Reserve.Reserve( HaulableInd, ReservationType.Total );
		yield return reserveTargetA;

		Toil toilGoto = null;
		toilGoto = Toils_Goto.GotoThing( HaulableInd, PathMode.ClosestTouch )
			.FailOn( ()=>
			{
				//Note we don't fail on losing hauling designation
				//Because that's a special case anyway

				//While hauling to cell storage, ensure storage dest is still valid
				Pawn actor = toilGoto.actor;
				Job curJob = actor.jobs.curJob;
				if( curJob.haulMode == HaulMode.ToCellStorage )
				{
					Thing haulThing = curJob.GetTarget( HaulableInd ).Thing;

					IntVec3 destLoc = actor.jobs.curJob.GetTarget(TargetIndex.B).Cell;
					if(!destLoc.IsValidStorageFor( haulThing)  )
						return true;
				}

				return false;
			});
		yield return toilGoto;


		yield return Toils_Haul.StartCarryThing( HaulableInd );

		if( CurJob.haulOpportunisticDuplicates )
			yield return Toils_Haul.CheckForGetOpportunityDuplicate( reserveTargetA, HaulableInd, StoreCellInd );

		Toil carryToCell = Toils_Haul.CarryHauledThingToCell( StoreCellInd );
		yield return carryToCell;

		yield return Toils_Haul.PlaceHauledThingInCell(StoreCellInd, carryToCell, true);
	}

}}
