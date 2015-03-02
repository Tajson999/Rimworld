using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld{

public enum RotStage
{
	Fresh,
	Rotting,
	Dessicated,
}


public class CompRottable : ThingComp
{
	//Working vars
	public float rotProgress = 0;

	//Properties
	private CompProperties_Rottable PropsRot { get { return (CompProperties_Rottable)props; } }
	public RotStage Stage
	{
		get
		{
			if (rotProgress < PropsRot.TicksToRotStart)
				return RotStage.Fresh;
			else if (rotProgress < PropsRot.TicksToDessicated)
				return RotStage.Rotting;
			else
				return RotStage.Dessicated;
		}
	}

	public override void PostExposeData()
	{
		base.PostExposeData();

		Scribe_Values.LookValue(ref rotProgress, "rotProg");
	}


	public override void CompTickRare()
	{
        // keep track of previous age
        float previousProgress = rotProgress;

        // Do rotting progress according to temperature
		float rotRate = 1f;
        float cellTemp = GenTemperature.GetTemperatureForCell(parent.Position);
        rotRate *= GenTemperature.RotRateAtTemperature(cellTemp);
		rotProgress += Mathf.FloorToInt(rotRate) * GenTicks.TickRareInterval;

		//Destroy if needed
		if( Stage == RotStage.Rotting && PropsRot.rotDestroys )
		{
			parent.Destroy();
			return;
		}

		//Do rot damage per day
		if (Stage == RotStage.Rotting && PropsRot.rotDamagePerDay > 0)
		{
			//Happens once per day
            bool isNewDay = Mathf.FloorToInt(previousProgress / GenDate.TicksPerDay) != Mathf.FloorToInt(rotProgress / GenDate.TicksPerDay);

            if (isNewDay)
			{
				DamageInfo dam = new DamageInfo(DamageDefOf.Rotting, PropsRot.rotDamagePerDay, null);
				parent.TakeDamage(dam);
			}
		}
	}


	public override void PreAbsorbStack( Thing otherStack, int count )
	{
		//New rot progress is the weighted average of our old rot progresses
		float proportionOther = (float)count/ (float)(parent.stackCount + count);

		float otherRotProg = ((ThingWithComponents)otherStack).GetComp<CompRottable>().rotProgress;

		rotProgress = Mathf.Lerp( rotProgress, otherRotProg, proportionOther );
	
	}

	public override void PostSplitOff(Thing piece)
	{
		//Piece inherits my rot progress
		((ThingWithComponents)piece).GetComp<CompRottable>().rotProgress = rotProgress;
	}

	public override string CompInspectStringExtra()
	{
		StringBuilder sb = new StringBuilder();

		switch( Stage)
		{
			case RotStage.Fresh:		sb.AppendLine("RotStateFresh".Translate()); break;
			case RotStage.Rotting:		sb.AppendLine("RotStateRotting".Translate()); break;
			case RotStage.Dessicated:	sb.AppendLine("RotStateDessicated".Translate()); break;
		}

		float progressUntilStartRot = PropsRot.TicksToRotStart - rotProgress;

        if(progressUntilStartRot > 0)
        {
            float cellTemp = GenTemperature.GetTemperatureForCell(parent.Position);

			//Rounding here reduces dithering
			cellTemp = Mathf.RoundToInt(cellTemp);

            float rotRate = GenTemperature.RotRateAtTemperature(cellTemp);

			int ticksUntilStartRot = Mathf.RoundToInt( progressUntilStartRot / rotRate );

            if( rotRate < 0.001f )
            {
                // frozen
                sb.AppendLine( "CurrentlyFrozen".Translate() );
            }
            else if( rotRate < 0.999f )
            {
				// refrigerated
				sb.AppendLine( "CurrentlyRefrigerated".Translate(ticksUntilStartRot.TicksToDaysExtendedString()) );
            }
            else
            {
                // not refrigerated
				sb.AppendLine("NotRefrigerated".Translate(ticksUntilStartRot.TicksToDaysExtendedString()));
            }
        }
		
		return sb.ToString();
	}
}}

