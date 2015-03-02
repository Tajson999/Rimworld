using UnityEngine;
using System.Collections.Generic;
using Verse;


namespace Verse.AI{
public class JobDriver_Goto : JobDriver
{
	public JobDriver_Goto(Pawn pawn) : base(pawn){}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		yield return Toils_Goto.GotoCell( TargetIndex.A, PathMode.OnCell );


		Toil arrive = new Toil();
		arrive.initAction = ()=>
		{
			if( CurJob.exitMapOnArrival && pawn.Position.OnEdge() )
			{
				pawn.ExitMap();
			}
		};
		arrive.defaultCompleteMode = ToilCompleteMode.Instant;
		yield return arrive;
		
	}
}}

