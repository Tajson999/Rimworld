using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;


namespace Verse.AI{

public class JobDriver_Wait : JobDriver
{
	//Constants
	private const int TargetSearchInterval = 4;


	public JobDriver_Wait(Pawn pawn) : base(pawn){}


	protected override IEnumerable<Toil> MakeNewToils()
	{
		Toil wait = new Toil();
		wait.initAction = ()=>
		{
			Find.PawnDestinationManager.ReserveDestinationFor(pawn, pawn.Position);

			pawn.pather.StopDead();
		};
		wait.tickAction = ()=>
		{
			if( (Find.TickManager.TicksGame + pawn.thingIDNumber) % TargetSearchInterval != 0 )
				return;

			if( pawn.story == null || !pawn.story.WorkTagIsDisabled(WorkTags.Violent) )
			{
				bool shouldFightFires = pawn.RaceProps.humanoid && pawn.Faction == Faction.OfColony;

				//Melee attack adjacent enemy pawns
				//Barring that, put out fires
				for( int i=0; i<9; i++ )
				{
					IntVec3 neigh = pawn.Position + GenAdj.AdjacentCellsAndInside[i];

					if( !neigh.InBounds() )
						continue;

					Fire foundFire = null;
					List<Thing> thingList = Find.ThingGrid.ThingsListAt(neigh);
					if( thingList != null )
					{
						for( int j=0; j<thingList.Count; j++ )
						{
							Pawn otherPawn = thingList[j] as Pawn;
							if( otherPawn != null && !otherPawn.Downed && pawn.HostileTo(otherPawn)  )
							{
								pawn.natives.TryMeleeAttack(otherPawn);
								return;
							}

							//Note: It checks our position first, so we keep our first found fire
							//This way, we prioritize a fire we're standing on
							if( shouldFightFires )
							{
								Fire fire = thingList[j] as Fire;
								if( fire != null
									&& foundFire == null
									&& (fire.parent == null || fire.parent != pawn) )
									foundFire = fire;
							}
						}
					}

					if( shouldFightFires && foundFire != null )
					{
						pawn.natives.TryBeatFire( foundFire );
						return;
					}
				}


				//Shoot at the closest enemy in range
                if( pawn.Faction != null 
					&& pawn.JailerFaction == null
					&& pawn.jobs.curJob.def == JobDefOf.WaitCombat  )
				{
				//	Log.Message("Scanshoot " + pawn);

                    // if the pawn is not a colonist, allow pawn to select a weapon that normally requires an order
                    bool allowManualCastWeapons = !pawn.IsColonist;
                    Verb attackVerb = pawn.BestAttackVerb(allowManualCastWeapons);

					if( !attackVerb.verbProps.MeleeRange )
					{
						//We increase the range because we can hit targets slightly outside range by shooting at their ShootableSquares
						//We could just put the range at int.MaxValue but this is slightly more optimized so whatever
						Thing curTarg = GenAI.BestShootTargetFromCurrentPosition
														(pawn.Position,
														pawn,
														validator:          null, 
														maxDistance:        attackVerb.verbProps.range,
														minDistance:        attackVerb.verbProps.minRange,
														needsLOS:           true,
														allowBurningTargets: !attackVerb.verbProps.ai_IsIncendiary );
                     
						if( curTarg != null )
						{
							pawn.equipment.TryStartAttack( curTarg );
							return;
						}
					}
				}
			}


		};
		wait.defaultCompleteMode = ToilCompleteMode.Never;

		yield return wait;
	}
}}



