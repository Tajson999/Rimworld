using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;



namespace RimWorld{
public class CompGatherSpot : ThingComp
{
	//Working vars
	private bool active = true;
	
	//Constants
	private static readonly Texture2D ButtonIconActive = ContentFinder<Texture2D>.Get("UI/Commands/GatherSpotActive");
	
	public bool Active
	{
		get
		{
			return active;
		}
		set
		{
			if( value == active )
				return;

			active = value;

			if( active )
				GatherSpotLister.RegisterActivated(this);
			else
				GatherSpotLister.RegisterDeactivated(this);
		}
	}


	public override void PostExposeData()
	{
		Scribe_Values.LookValue(ref active, "active", false);	
	}

	public override void PostSpawnSetup()
	{
		base.PostSpawnSetup();

		if( active )
			GatherSpotLister.RegisterActivated(this);
	}

	public override void PostDeSpawn()
	{
		base.PostDeSpawn();

		if( Active )
			Active = false;
	}

	public override IEnumerable<Command> CompGetGizmosExtra()
	{
		Command_Toggle com = new Command_Toggle();
		com.hotKey = KeyBindingDefOf.CommandTogglePower;
		com.defaultLabel = "CommandGatherSpotToggleLabel".Translate();
		com.icon = ButtonIconActive;
		com.isActive = ()=>Active;
		com.toggleAction = ()=>Active = !Active;
		com.groupKey = 61733;

		if( Active )
			com.defaultDesc = "CommandGatherSpotToggleDescActive".Translate();
		else
			com.defaultDesc = "CommandGatherSpotToggleDescInactive".Translate();
		
		yield return com;
	}
}



public static class GatherSpotLister
{
	public static List<CompGatherSpot> activeSpots = new List<CompGatherSpot>();

	public static void Reinit()
	{
		activeSpots.Clear();
	}

	public static void RegisterActivated( CompGatherSpot spot )
	{
		if( !activeSpots.Contains(spot) )
			activeSpots.Add(spot);
	}

	public static void RegisterDeactivated( CompGatherSpot spot )
	{
		if( activeSpots.Contains(spot) )
			activeSpots.Remove(spot);
	}
}}