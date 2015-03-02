using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;


namespace Verse{
public class Corpse : ThingWithComponents, ThoughtGiver, Strippable, BillGiver
{
	//Config
	public Pawn innerPawn = null;

	//Working vars
	private int timeOfDeath = -1000;
    private int vanishAfterTimestamp = VanishAfterTicks;
    private BillStack operationsBillStack = null;
    
    //Constants
    private const int VanishAfterTicks = 100 * GenDate.TicksPerDay;

	//Properties
    public int Age{
        get
        {
            return Find.TickManager.TicksGame - timeOfDeath;
        }
        set
        {
            timeOfDeath = Find.TickManager.TicksGame - value;
        }
    }
	public override string Label
	{
		get
		{
			return "DeadLabel".Translate(innerPawn.LabelCap);
		}
	}
    private bool ShouldVanish
    {
        get
        {
             return innerPawn.RaceProps.IsAnimal &&
                    vanishAfterTimestamp > 0 &&
                    Age >= vanishAfterTimestamp &&
                    this.GetRoom().TouchesMapEdge &&
                    !Find.RoofGrid.Roofed(Position);
        }
    }
    public BillStack BillStack { get { return operationsBillStack; } }
    public IntVec3 BillInteractionCell
    {
        get
        {
            IEnumerable <IntVec3> candidates = GenAdjFast.AdjacentCells8Way(Position).Where(x => GenGrid.Standable(x));
            
            if(candidates.Any())
                return candidates.First();
            else
                return IntVec3.Invalid;
        }
    }
    public IEnumerable <IntVec3> IngredientStackCells { get { yield return BillInteractionCell; } }
    
    public Corpse()
    {
        operationsBillStack = new BillStack(this);
    }
    
    public bool CurrentlyUsable()
    {
        return BillInteractionCell.IsValid;
    }
    
    public bool AnythingToStrip()
    {
        Thing th = this as Thing;
        if(th.IsBuried()) return false;
        return innerPawn.AnythingToStrip();
    }
    



	public override void SpawnSetup()
	{
		base.SpawnSetup();

		if( timeOfDeath < 0 )
			timeOfDeath = Find.TickManager.TicksGame;

		innerPawn.Rotation = IntRot.south; //Fixes drawing errors

		//Clearing out some data saves us from saveload errors, since dead pawns
		// are still kept around and saved by corpses
		//
		// note this doesn't happen in pawn.Destroy because that can cause little accessed nones in the
		// frame when a pawn is destroyed (e.g. when it exits the map).
		innerPawn.guest = null;
		innerPawn.jobs = null;
		innerPawn.thinker = null;
		innerPawn.pather = null;
	}
    
    public override void TickRare()
    {
		base.TickRare();

        // reset vanishAfterTimestamp to 100 days from now if not previously set, or if carcass still fresh
        CompRottable rottable = GetComp<CompRottable>();
        if (vanishAfterTimestamp < 0 || (rottable != null && rottable.Stage != RotStage.Dessicated))
            vanishAfterTimestamp = Age + VanishAfterTicks;

        if(ShouldVanish)
            Destroy();
    }
    
	public override IEnumerable<Thing> ButcherProducts( Pawn butcher, float efficiency )
	{
		//Just in case pawn is defined with special butcher products
		foreach( var t in innerPawn.ButcherProducts(butcher, efficiency) )
		{
			yield return t;
		}

		//Spread blood
		if( innerPawn.RaceProps.isFlesh )
		{
            FilthMaker.MakeFilth(butcher.Position, ThingDefOf.FilthBlood, innerPawn.LabelCap);
		}

		//Thought for butchering humanoid
		if( innerPawn.RaceProps.humanoid )
			butcher.needs.mood.thoughts.TryGainThought( ThoughtDefOf.ButcheredHumanoidCorpse );

		//Return product
		{
			Thing meat = ThingMaker.MakeThing( innerPawn.def.race.meatDef );
			meat.stackCount = Mathf.RoundToInt(innerPawn.def.race.MeatAmount * efficiency);
			yield return meat;
		}

        if(innerPawn.def.race.hasLeather && innerPawn.def.race.leatherDef != null)
		{
			Thing leather = ThingMaker.MakeThing( innerPawn.def.race.leatherDef );
			leather.stackCount = Mathf.RoundToInt(innerPawn.def.race.LeatherAmount * efficiency);
			yield return leather;
		}
	}


	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.LookValue( ref timeOfDeath, "timeOfDeath" );
        Scribe_Values.LookValue(ref vanishAfterTimestamp, "vanishAfterTimestamp");
        Scribe_Deep.LookDeep( ref operationsBillStack, "operationsBillStack", this );
		Scribe_Deep.LookDeep( ref innerPawn, "innerPawn" );
	}

	public void Strip()
    {
        if(innerPawn.equipment != null)
        {
            innerPawn.equipment.DropAllEquipment(Position, false);
        }
        if(innerPawn.apparel != null)
        {
            innerPawn.apparel.DropAll(Position, false);
        }
        if(innerPawn.inventory != null)
        {
            innerPawn.inventory.DropAll(Position);
        }
    }

	public override void DrawAt(Vector3 drawLoc)
	{
		//Don't draw in graves
		Building storeBuilding = this.StoringBuilding();
		if( storeBuilding != null && storeBuilding.def == ThingDefOf.Grave )
			return;

        BodyDrawType bodyDrawType = BodyDrawType.Normal;
        CompRottable rottable = GetComp <CompRottable> ();
        if(rottable != null)
        {
            if(rottable.Stage == RotStage.Rotting)
                bodyDrawType = BodyDrawType.Rotting;
            else if(rottable.Stage == RotStage.Dessicated)
                bodyDrawType = BodyDrawType.Dessicated;
        }
		innerPawn.drawer.renderer.RenderPawnAt( drawLoc, bodyDrawType );
	}

	public Thought GiveObservedThought()
	{
		//Non-humanoid corpses never give thoughts
		if( !innerPawn.RaceProps.humanoid )
			return null;

        Thing storingBuilding = this.StoringBuilding();
		if( storingBuilding == null )
		{
			//Laying on the ground
            
            bool rotting = false;
            CompRottable rottable = GetComp <CompRottable> ();
            if(rottable != null && rottable.Stage != RotStage.Fresh)
                rotting = true;
            
			if(rotting) return new Thought_Observation(ThoughtDefOf.ObservedLayingRottingCorpse, this);
            else return new Thought_Observation(ThoughtDefOf.ObservedLayingCorpse, this);
		}
        
		return null;
	}

	public override string GetInspectString()
	{
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		if( innerPawn.Faction != null )
			sb.AppendLine("Faction".Translate() + ": " + innerPawn.Faction);
		sb.AppendLine("DeadTime".Translate( Age.TickstoDaysString() ) );
		sb.AppendLine(base.GetInspectString());
		return sb.ToString();
	}
}}
