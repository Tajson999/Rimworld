using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine; 
using Verse; 
using Verse.AI;


namespace RimWorld{
class IncidentWorker_ShipPartCrash : IncidentWorker
{
	//Constants
	private const float ShipPointsFactor = 0.9f;
	private const int IncidentMinimumPoints = 300; //One centipede


	public override bool StorytellerCanUseNow()
	{
		if( !Find.ListerThings.ThingsOfDef( ThingDefOf.CrashedShipPart ).NullOrEmpty() )
			return false;

		if( Find.MapConditionManager.ConditionIsActive( MapConditionDefOf.PsychicDrone ) )
			return false;

		return true;
	}

    public override bool TryExecute(IncidentParms parms)
    {
		Predicate<IntVec3> validator = c =>
			{
				if( c.Fogged() )
					return false;

				foreach( IntVec3 subCell in GenAdj.CellsOccupiedBy( c, IntRot.north, ThingDefOf.CrashedShipPart.size ) )
				{
					if( !subCell.Standable() )
						return false;

					if( Find.RoofGrid.Roofed( subCell ) )
						return false;
				}
			
				if( !c.CanReachColony() )
					return false;

				return true;
			};


        IntVec3 center;
		if( !GenCellFinder.TryFindRandomNotEdgeCellWith( 14, validator, out center ) )
			return false;

        // Make the crash explosion
        GenExplosion.DoExplosion(center, 3f, DamageDefOf.Flame, null);

		//Make letter
        string letter = "SpaceshipPartCrash".Translate();
        Find.History.AddGameEvent(letter, GameEventType.BadNonUrgent, true, center);

        // Spawn the ship part
        Building_PsychicEmanator shipPart = (Building_PsychicEmanator)GenSpawn.Spawn(ThingDefOf.CrashedShipPart, center);
        shipPart.SetFaction(Faction.OfMechanoids);
		shipPart.pointsLeft = parms.points * ShipPointsFactor;
		if( shipPart.pointsLeft < IncidentMinimumPoints )
			shipPart.pointsLeft = IncidentMinimumPoints;

		//Camera shake
		Find.CameraMap.shaker.DoShake( 1.0f );

        return true;
    }
}}
