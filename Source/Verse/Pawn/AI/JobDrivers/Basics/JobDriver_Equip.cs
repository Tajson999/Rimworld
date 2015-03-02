using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Verse;


namespace Verse.AI{
public class JobDriver_Equip : JobDriver
{
	public JobDriver_Equip(Pawn pawn) : base(pawn){}


	protected override IEnumerable<Toil> MakeNewToils()
	{
		yield return Toils_Reserve.Reserve( TargetIndex.A, ReservationType.Total );

		//Goto equipment
		{
			Toil gotoEquipment = new Toil();
			gotoEquipment.initAction = ()=>
			{
				pawn.pather.StartPath(TargetThingA, PathMode.ClosestTouch);
			};
			gotoEquipment.defaultCompleteMode = ToilCompleteMode.PatherArrival;
			gotoEquipment.FailOnDestroyedOrForbidden(TargetIndex.A);
			yield return gotoEquipment;
		}
		
		
		//Take equipment
		{
			Toil takeEquipment = new Toil();
			takeEquipment.initAction = ()=>
			{
				((Equipment)CurJob.targetA.Thing).TakenAndEquippedBy( pawn );
			};
			takeEquipment.defaultCompleteMode = ToilCompleteMode.Instant;
			yield return takeEquipment;
		}
	}
}}











