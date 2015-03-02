using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Verse.AI{
public class JobDriver_Kill : JobDriver
{
	//Constants
	private const TargetIndex VictimInd = TargetIndex.A;

	//Ctor
	public JobDriver_Kill(Pawn pawn) : base(pawn){}


	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.EndOnDespawned( VictimInd, JobCondition.Succeeded );

		yield return Toils_Reserve.Reserve(VictimInd, ReservationType.Total );

		yield return Toils_Combat.SetJobToUseToBestAttackVerb();

		Toil gotoCastPos = Toils_Combat.GotoCastPosition( VictimInd );
		yield return gotoCastPos;

		Toil jumpIfCannotHit =  Toils_Jump.JumpIfCannotHitTarget( VictimInd, gotoCastPos );
		yield return jumpIfCannotHit;

		yield return Toils_Combat.CastVerb( VictimInd );

		yield return Toils_Jump.Jump( jumpIfCannotHit );
	}
}}
