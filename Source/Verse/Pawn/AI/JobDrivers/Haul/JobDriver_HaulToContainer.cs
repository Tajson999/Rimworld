using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;


namespace Verse.AI{
public class JobDriver_HaulToContainer : JobDriver
{
	public JobDriver_HaulToContainer(Pawn pawn) : base(pawn) { }

    public override string GetReport()
	{
		Thing hauledThing = null;
		if( pawn.carryHands.CarriedThing != null )
			hauledThing = pawn.carryHands.CarriedThing;
		else
			hauledThing = TargetThingA;

		return "ReportHaulingTo".Translate( hauledThing.LabelCap, CurJob.targetB.Thing.LabelBaseShort );
	}


	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyed( TargetIndex.A );
		this.FailOnForbidden( TargetIndex.A );
		this.FailOnDestroyed( TargetIndex.B );


		//Reserve resources
		yield return Toils_Reserve.Reserve( TargetIndex.A, ReservationType.Total );
		yield return Toils_Reserve.ReserveQueue( TargetIndex.A, ReservationType.Total );

		//Reserve construct targets
		yield return Toils_Reserve.Reserve( TargetIndex.B, ReservationType.Total );
		yield return Toils_Reserve.ReserveQueue( TargetIndex.B, ReservationType.Total );

		Toil getToHaulTarget = Toils_Goto.GotoThing( TargetIndex.A, PathMode.ClosestTouch );
		yield return getToHaulTarget;

		yield return Toils_Haul.StartCarryThing(TargetIndex.A);

		yield return Toils_Haul.JumpIfAlsoCollectingNextTargetInQueue( getToHaulTarget, TargetIndex.A );

		Toil carryToContainer = Toils_Haul.CarryHauledThingToContainer();
		yield return carryToContainer;

		yield return Toils_Goto.GetOutOfBlueprint(true, TargetIndex.B);

		yield return Toils_Construct.MakeSolidThingFromBlueprintIfNecessary(TargetIndex.B);

		yield return Toils_Haul.DepositHauledThingInContainer(TargetIndex.B);

		yield return Toils_Haul.JumpToCarryToNextContainerIfPossible( carryToContainer );
	}

}}

