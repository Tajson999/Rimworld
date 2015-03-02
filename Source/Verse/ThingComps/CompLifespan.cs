using UnityEngine;
using System.Collections;
using Verse;


namespace RimWorld{
public class CompLifespan : ThingComp
{
	public int startTick;

	public override void PostSpawnSetup()
	{
		base.PostSpawnSetup();

		startTick = Find.TickManager.TicksGame;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();

		Scribe_Values.LookValue(ref startTick, "startTick");
	}

	public override void CompTick()
	{
		if( Find.TickManager.TicksGame - props.lifespanTicks > startTick )
			parent.Destroy();
	}
}}