using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using Verse;

using Verse.AI;

namespace RimWorld{
public interface Usable
{
	void UsedBy( Pawn pawn );
}


public class Neurotrainer : ThingWithComponents, Usable
{
	//Config
	private SkillDef skill;

	//Constants
	private const float XPGainAmount = 50000;

	//Properties
	public override string LabelBase
	{
		get
		{
			return skill.label + " " + def.label;
		}
	}



	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Defs.LookDef( ref skill, "skill" );
	}

	public override void PostMake()
	{
		base.PostMake();

		skill = DefDatabase<SkillDef>.GetRandom();
	}


	public override IEnumerable<FloatMenuOption> GetFloatMenuOptionsFor(Pawn myPawn)
	{
		foreach( var ch in base.GetFloatMenuOptionsFor(myPawn) )
		{
			yield return ch;
		}

		FloatMenuOption useopt = new FloatMenuOption("UseNeurotrainer".Translate(skill.label), ()=>
			{
				Job job = new Job( JobDefOf.UseNeurotrainer, this );
					myPawn.playerController.TakeOrderedJob( job );
			});

		yield return useopt;
	}

	public void UsedBy( Pawn user )
	{
		int oldLevel = user.skills.GetSkill(skill).level;

		user.skills.Learn( skill, XPGainAmount );

		int newLevel = user.skills.GetSkill(skill).level;

		if( user.Faction == Faction.OfColony )
			Messages.Message("NeurotrainerUsed".Translate(user.LabelBaseShort, skill.label, oldLevel, newLevel), MessageSound.Benefit );

		Destroy();
	}

	public override bool CanStackWith(Thing other)
	{
		if( !base.CanStackWith(other) )
			return false;

		Neurotrainer on = other as Neurotrainer;
		if( on == null || on.skill != skill )
			return false;

		return true;
	}

	public override Thing SplitOff(int count)
	{
		Neurotrainer nt = (Neurotrainer)base.SplitOff(count);

		if( nt != null )
			nt.skill = skill;

		return nt;
	}
}}