using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Verse;



namespace RimWorld{
public class CompForbiddable : ThingComp
{
	//Working vars
	private bool forbiddenInt = false;
	
	//Constants
	private static readonly Texture2D ButtonIconForbidden = ContentFinder<Texture2D>.Get("UI/Commands/Forbidden");
	

	public bool Forbidden
	{
		get
		{
			return forbiddenInt;
		}
		set
		{
			if( value != forbiddenInt )
			{
				forbiddenInt = value;

				if( forbiddenInt )
					ListerHaulables.Notify_Forbidden(parent);
				else
					ListerHaulables.Notify_Unforbidden(parent);
			}
		}
	}



	public override void PostExposeData()
	{
		Scribe_Values.LookValue(ref forbiddenInt, "forbidden", false);	
	}
	
	
	public override void PostDraw()
	{	
		if( forbiddenInt )
			OverlayDrawer.DrawOverlay(parent, OverlayTypes.Forbidden);
	}
	
	public override void PostSplitOff( Thing piece )
	{
		piece.SetForbidden( forbiddenInt );
	}

	public override IEnumerable<Command> CompGetGizmosExtra()
	{
		Command_Toggle com = new Command_Toggle();
		com.hotKey = KeyBindingDefOf.CommandItemForbid;
		com.icon = ButtonIconForbidden;
		com.isActive = ()=>!forbiddenInt;
		com.toggleAction = ()=>
			{
				Forbidden = !Forbidden;
				ConceptDatabase.KnowledgeDemonstrated(ConceptDefOf.Forbidding, KnowledgeAmount.SpecificInteraction);
			};

		if( forbiddenInt )
			com.defaultDesc = "CommandForbiddenDesc".Translate();
		else
			com.defaultDesc = "CommandNotForbiddenDesc".Translate();
		com.groupKey = 125691;
		com.tutorHighlightTag = "ToggleForbidden";

		yield return com;
	}
}


public static class GenForbid
{
	public static void SetForbidden(this Thing t, bool value, bool warnOnFail = true)
	{
		if( t == null )
		{
			if( warnOnFail )
				Log.Error("Tried to SetForbidden on null Thing." );
			return;
		}

		ThingWithComponents twc = t as ThingWithComponents;
		if( twc == null )
		{
			if( warnOnFail )
				Log.Error("Tried to SetForbidden on non-ThingWithComponents Thing " + t );
			return;
		}
		
		CompForbiddable f = twc.GetComp<CompForbiddable>();
		if( f == null )
		{
			if( warnOnFail )
				Log.Error("Tried to SetForbidden on non-Forbiddable Thing " + t );
			
			return;
		}
		
		f.Forbidden = value;
	}

	public static bool IsForbidden(this Thing t, Faction fac)
	{
		if( fac == null )
			return false;

		if( fac != Faction.OfColony )
			return false;

		ThingWithComponents twc = t as ThingWithComponents;
		if( twc == null )
			return false;
		
		CompForbiddable f = twc.GetComp<CompForbiddable>();
		if( f == null )
			return false;
		
		if( f.Forbidden )
			return true;
		
		return false;
	}

	public static void SetForbiddenIfOutsideHomeRegion(this Thing t)
	{
		if( !Find.HomeRegionGrid.Get( t.Position) )
			SetForbidden(t, true, false);
	}
}}







