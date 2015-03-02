using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;


namespace RimWorld{
public class IncidentWorker_CropBlight : IncidentWorker
{
	const float MinGrowthPer20kTicks = 1f/20f;


	public override bool TryExecute( IncidentParms parms )
	{
        List<Thing> plants = Find.Map.listerThings.ThingsInGroup( ThingRequestGroup.CultivatedPlant );

		if( plants == null )
			return false;

		bool cropFound = false;
		for( int i=plants.Count-1; i>=0; i-- )
		{
			Plant plant = (Plant)plants[i];

			if( plant.def.plant.growthPer20kTicks < MinGrowthPer20kTicks )
				continue;

            if (plant.LifeStage == PlantLifeStage.Growing || plant.LifeStage == PlantLifeStage.Mature)
            {
				plant.CropBlighted();

                cropFound = true;
            }
		}

        if ( !cropFound )
            return false;
		else
		{
			Find.History.AddGameEvent("CropBlight".Translate(), GameEventType.BadNonUrgent, true);
			return true;
		}
	}
}

}