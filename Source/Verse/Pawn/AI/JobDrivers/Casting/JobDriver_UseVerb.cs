using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace Verse.AI{
public class JobDriver_CastVerbOnce : JobDriver
{
	public JobDriver_CastVerbOnce(Pawn pawn) : base(pawn){}

	public override string GetReport()
	{
		string targetLabel;
		if( TargetA.HasThing )
			targetLabel = TargetThingA.LabelCap;
		else
			targetLabel = "area";

		return "Using " + CurJob.verbToUse.verbProps.label + " on " + targetLabel + ".";
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		yield return Toils_Combat.GotoCastPosition( TargetIndex.A );

		yield return Toils_Combat.CastVerb( TargetIndex.A );
	}

}}

