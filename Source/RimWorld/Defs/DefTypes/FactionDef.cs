using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

using UnityEngine;


namespace RimWorld{
public class SpawnGroup
{
	//Config
	public int 					selectionWeight;
	public List<PawnKindDef>	kinds = new List<PawnKindDef>();

	//Cache
	[Unsaved] private float		cachedCost = -1;

	//Properties
	public float Cost
	{
		get
		{
			if( cachedCost < 0 )
				cachedCost = kinds.Sum( k=>k.pointsCost );

			return cachedCost;
		}
	}
}

public class FactionDef : Def
{
	//General config
	public RulePackDef			factionNameMaker;
	public string				fixedName = null;
	public bool					humanoidFaction = true;
	public bool					hidden = false;
	public List<PawnGroupMaker>	pawnGroupMakers = null;
	public string				pawnsPlural = "characters";
	public float				raidCommonality = 0;
	public bool					canFlee = true;
	public bool					canSiege = false;
	public bool					canStageAttacks = false;
	public float				earliestRaidDays = 0;
	public string				leaderTitle = "Leader";

	//Faction generation
	public int					requiredCountAtGameStart = 0;
	public bool					canMakeRandomly = false;

	//Humanoid faction config
	public RulePackDef			pawnNameMaker;
	public TechLevel			techLevel = TechLevel.Undefined;
	public string				backstoryCategory = null;
	public List<string>			hairTags = new List<string>();
	public ThingFilter			apparelStuffFilter = null;

	//Relations (can apply to non-humanoid factions)
	public FloatRange			startingGoodwill = FloatRange.zero;
	public FloatRange			naturalColonyGoodwill = FloatRange.zero;
	public float				goodwillDailyGain = 1f;
	public float				goodwillDailyFall = 1f;
	public bool					appreciative = true;

	//Map drawing
	public string				homeIconPath;
	public Color				homeIconColor = Color.white;

	//Unsaved
	[Unsaved] public Material	baseRenderMaterial = BaseContent.BadMat;





	public override void PostLoad()
	{
		base.PostLoad();

		if( !homeIconPath.NullOrEmpty() )
			baseRenderMaterial = MaterialPool.MatFrom(homeIconPath, ShaderDatabase.Transparent, homeIconColor);
	}

	public override void ResolveReferences()
	{
		base.ResolveReferences();

		if( apparelStuffFilter != null )
			apparelStuffFilter.ResolveReferences();
	}

	public override IEnumerable<string> ConfigErrors()
	{
		foreach( var error in base.ConfigErrors() )
			yield return error;

		if( factionNameMaker == null && fixedName == null )
			yield return "FactionTypeDef " + defName + " lacks a factionNameMaker and a fixedName.";

		if( techLevel == TechLevel.Undefined )
			yield return defName + " has no tech level.";

		if( humanoidFaction )
		{
			if( backstoryCategory == null )
				yield return defName + " is humanoidFaction but has no backstory category.";

			if( hairTags.Count == 0 )
				yield return defName + " is humanoidFaction but has no hairTags.";
		}
	}

	public float MinPointsToGeneratePawnGroup()
	{
		if( pawnGroupMakers.NullOrEmpty() )
			return int.MaxValue;

		return pawnGroupMakers.Min( pgm=>pgm.MinPointsToGenerate );
	}

	public bool CanUseStuffForApparel( ThingDef stuffDef )
	{
		if( apparelStuffFilter == null )
			return true;

		return apparelStuffFilter.Allows( stuffDef );
	}



	public static FactionDef Named( string defName )
	{
		return DefDatabase<FactionDef>.GetNamed(defName);
	}
}}

