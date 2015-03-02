using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
public enum ArtGenerationSource
{
	Outsider,
	Colony,
}

public class CompArt : ThingComp
{
	//Data
	private string			authorNameInt = null;
	private string			titleInt = null;
	private TaleReference	taleRef = null;

	//Properties
	public string AuthorName
	{
		get
		{
			if( authorNameInt.NullOrEmpty() )
				return "UnknownLower".Translate();
			
			return authorNameInt;
		}
	}
	public string Title
	{
		get
		{
			if( titleInt.NullOrEmpty() )
				return "Untitled".Translate(); //This shouldn't happen

			return titleInt;
		}
	}
	public bool Active
	{
		get
		{
			QualityCategory qc;
			if( !parent.TryGetQuality(out qc) )
				return true;
			return qc >= props.minQualityForArtistic;
		}
	}



	public void GenerateTaleRef(ArtGenerationSource source)
	{
		if( Active )
		{
			titleInt = props.nameMaker.GenerateDefault_Name();

			if( Game.Mode == GameMode.MapPlaying )
				taleRef = Find.TaleManager.GetRandomTaleReferenceForArt(source);
			else
				taleRef = TaleReference.Taleless;
		}
		else
		{
			titleInt = null;
			taleRef = null;
		}
	}

	public void JustCreatedBy( Pawn pawn )
	{
		if( Active )
			authorNameInt = pawn.Name.StringFull;
	}

	public override void PostExposeData()
	{
		base.PostExposeData();
		Scribe_Values.LookValue(ref authorNameInt, "authorName", null);
		Scribe_Values.LookValue(ref titleInt, "title", null);
		Scribe_Deep.LookDeep(ref taleRef, "taleRef");
	}

	public override string CompInspectStringExtra()
	{
		if( !Active )
			return null;

		string str = "Author".Translate() + ": " + AuthorName ;
		str += "\n" + "Title".Translate() + ": " + Title;
		return str;
	}

	public override void PostDestroy(DestroyMode mode = DestroyMode.Vanish)
	{
		base.PostDestroy(mode);

		if( taleRef != null )
			taleRef.ReferenceDestroyed();
	}

	public override string GetDescriptionPart()
	{
		if( !Active )
			return null;

		return ImageDescription();
	}

	public string ImageDescription()
	{
		return taleRef.GetDescription( props.descriptionMaker );
	}
}
}
