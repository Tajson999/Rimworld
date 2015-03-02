using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;



namespace Verse.AI{
public class JobDriver_DoBill : JobDriver
{
	public float workLeft;
	public int	startTick;
	
	public JobDriver_DoBill(Pawn pawn) : base(pawn){}
	

	public const TargetIndex BillGiverInd = TargetIndex.A;
	public const TargetIndex IngredientInd = TargetIndex.B;
	public const TargetIndex IngredientPlaceCellInd = TargetIndex.C;

	public override string GetReport()
	{
		return pawn.jobs.curJob.reportString;
	}
    
    private BillGiver BillGiver
    {
        get
        {
            BillGiver giver = pawn.jobs.curJob.GetTarget(BillGiverInd).Thing as BillGiver;

            if(giver == null)
				throw new InvalidOperationException("DoBill on non-Billgiver.");

            return giver;
        }
    }

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.LookValue(ref workLeft, "workLeft");
		Scribe_Values.LookValue(ref startTick, "startTick");
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrForbidden( BillGiverInd );	//Bill giver destroyed/forbidden
		this.FailOnBurningImmobile( BillGiverInd );		//Bill giver burning

		this.FailOn( ()=>
		{
			BillGiver billGiver = pawn.jobs.curJob.GetTarget(BillGiverInd).Thing as BillGiver;

			//conditions only apply during the billgiver-use phase
			if( billGiver != null )
			{
				if( pawn.jobs.curJob.bill.DeletedOrDereferenced )
					return true;

				if( !billGiver.CurrentlyUsable() )
					return true;
			}

			return false;
		});
		
        //This toil is yielded later
		Toil gotoBillGiver = Toils_Goto.GotoThing( BillGiverInd, BillGiver.BillInteractionCell );





		//Reserve the bill giver and all the ingredients
		yield return Toils_Reserve.Reserve( BillGiverInd, ReservationType.Use );
		yield return Toils_Reserve.ReserveQueue( IngredientInd, ReservationType.Total );

		//Jump over ingredient gathering if there are no ingredients needed 
		yield return Toils_Jump.JumpIf( gotoBillGiver, ()=> CurJob.GetTargetQueue(IngredientInd).NullOrEmpty() );

		//Gather ingredients
		{
    		//Extract an ingredient into TargetB
    		Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue( IngredientInd );
    		yield return extract;
    
    		//Get to ingredient and pick it up
    		//Note that these fail cases must be on these toils, otherwise the recipe work fails if you stacked
    		//   your targetB into another object on the bill giver square.
    		Toil getToHaulTarget = Toils_Goto.GotoThing( IngredientInd, PathMode.ClosestTouch)
    								.FailOnDespawned( IngredientInd )
									.FailOnForbidden( IngredientInd );
    		yield return getToHaulTarget;
    
    		yield return Toils_Haul.StartCarryThing( IngredientInd );
    
			//Jump to pick up more in this run if we're collecting from multiple stacks at once
			//Todo bring this back
    		yield return JumpToCollectNextIntoHandsForBill( getToHaulTarget, TargetIndex.B );
    
    		//Carry ingredient to the bill giver and put it on the square
    		yield return Toils_Goto.GotoThing( BillGiverInd, BillGiver.BillInteractionCell )
    								.FailOnDestroyed( IngredientInd );
    
			Toil findPlaceTarget = Toils_JobTransforms.SetTargetToIngredientPlaceCell( BillGiverInd, IngredientInd, IngredientPlaceCellInd );
    		yield return findPlaceTarget;
    		yield return Toils_Haul.PlaceHauledThingInCell( IngredientPlaceCellInd,
															nextToilOnPlaceFailOrIncomplete: findPlaceTarget,
															storageMode: false );
    
    		//Jump back if there is another ingredient needed
    		//Can happen if you can't carry all the ingredients in one run
    		yield return Toils_Jump.JumpIfHaveTargetInQueue( IngredientInd, extract );
   
		}

        //For it no ingredients needed, just go to the bill giver
		//This will do nothing if we took ingredients and are thus already at the bill giver
		yield return gotoBillGiver;

		//If the recipe calls for the use of an UnfinishedThing
		//Create that and convert our job to be a job about working on it
		yield return Toils_Recipe.MakeUnfinishedThingIfNeeded();

		//Do the recipe
		//This puts the first product (if any) in targetC
        yield return Toils_Recipe.DoRecipeWork()
								 .FailOnDespawnedOrForbiddenPlacedTargets();
		
		//Finish doing this recipe
		//Generate the products
		//Modify the job to store them
		yield return Toils_Recipe.FinishRecipeAndStartStoringProduct();
        
		//If recipe has any products, store the first one
        if( !CurJob.RecipeDef.products.NullOrEmpty() || !CurJob.RecipeDef.specialProducts.NullOrEmpty() ) 
        {
    		//Reserve the storage cell
    		yield return Toils_Reserve.Reserve( TargetIndex.B, ReservationType.Store );
    
    		Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
    		yield return carryToCell;
    
    		yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, carryToCell, storageMode: true);
    
    		//Bit of a hack here
    		//This makes the worker use a count including the one they just dropped
    		//When determining whether to make the next item if the bill has "make until you have" marked.
    		Toil recount = new Toil();
    		recount.initAction = ()=>
    			{
                    Bill_Production bill = recount.actor.jobs.curJob.bill as Bill_Production;
    				if( bill != null && bill.repeatMode == BillRepeatMode.TargetCount )
    					Find.ResourceCounter.UpdateResourceCounts();
    			};
    		yield return recount;
        }
	}

	private static Toil JumpToCollectNextIntoHandsForBill( Toil gotoGetTargetToil, TargetIndex ind )
	{
		Toil toil = new Toil();
		toil.initAction = ()=>
		{
			const float MaxDist = 8;
			Pawn actor = toil.actor;
			Job curJob = actor.jobs.curJob;
			List<TargetInfo> targetQueue = curJob.GetTargetQueue(ind);

			if( targetQueue.NullOrEmpty() )
				return;
			
			if( actor.carryHands.CarriedThing == null )
			{
				Log.Error("JumpToAlsoCollectTargetInQueue run on " + actor + " who is not carrying something.");
				return;
			}

			//Find an item in the queue matching what you're carrying
			for( int i=0; i<targetQueue.Count; i++ )
			{
				//Can't use item - skip
				if( !GenAI.CanUseItemForWork( actor, targetQueue[i].Thing ) )
					continue;

				//Cannot stack with thing in hands - skip
				if( !targetQueue[i].Thing.CanStackWith(actor.carryHands.CarriedThing) )
					continue;

				//Too far away - skip
				if( (actor.Position - targetQueue[i].Thing.Position).LengthHorizontalSquared > MaxDist*MaxDist )
					continue;

				//Determine num in hands
				int numInHands = (actor.carryHands.CarriedThing==null) ? 0 : actor.carryHands.CarriedThing.stackCount;

				//Determine num to take
				int numToTake = curJob.numToBringList[i];
				if(	numToTake + numInHands > targetQueue[i].Thing.def.stackLimit )
					numToTake = targetQueue[i].Thing.def.stackLimit - numInHands;

				//Won't take any - skip
				if( numToTake == 0 )
					continue;

				//Remove the amount to take from the num to bring list
				curJob.numToBringList[i] -= numToTake;

				//Set me to go get it
				curJob.maxNumToCarry = numInHands + numToTake;
				curJob.SetTarget( ind, targetQueue[i].Thing );

				//Remove from queue if I'm going to take all
				if( curJob.numToBringList[i] == 0 )
				{
					curJob.numToBringList.RemoveAt(i);
					targetQueue.RemoveAt(i);
				}

				actor.jobs.curDriver.JumpToToil( gotoGetTargetToil );
				return;
			}

		};

		return toil;
	}
}}
