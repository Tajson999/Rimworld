using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse.Sound;
using UnityEngine;
using RimWorld;



namespace Verse{
public class Building : ThingWithComponents
{
	//Working vars
	private Sustainer		sustainerAmbient = null;

	//Properties
	public IntVec3 InteractionCell	{get{ return InteractionCellWhenAt(def, Position, Rotation); }}
	public CompPower PowerComp		{get{ return GetComp<CompPower>(); }}
	public virtual bool TransmitsPowerNow
	{
		get
		{
			//Designed to be overridden
			//In base game this always just returns the value in the powercomp's def
			CompPower pc = PowerComp;
			return pc != null && pc.props.transmitsPower;
		}
	}
	
	public override void SpawnSetup()
	{
		base.SpawnSetup();
		
		Find.ListerBuildings.Add( this );	
		
		//Remake terrain meshes with new underwall under me
		if( def.coversFloor )
			Find.MapDrawer.MapChanged(Position, MapChangeType.Terrain, true, false);

		var occRect = this.OccupiedRect();
		for( int z=occRect.minZ; z<=occRect.maxZ; z++ )
		{
			for( int x=occRect.minX; x<=occRect.maxX; x++ )
			{
				var c = new IntVec3(x,0,z);
				Find.MapDrawer.MapChanged( c, MapChangeType.Buildings );
				Find.GlowGrid.MarkGlowGridDirty(c);
			}
		}

		if( def.IsEdifice() )
			Find.EdificeGrid.Register(this);

		if( Faction == Faction.OfColony )
		{
			if( def.building != null && def.building.spawnedConceptLearnOpportunity != null )
			{
				ConceptDecider.TeachOpportunity( def.building.spawnedConceptLearnOpportunity, OpportunityType.GoodToKnow );
			}
		}

		AutoHomeRegionMaker.Notify_BuildingSpawned( this );

		if( def.building != null && !def.building.soundAmbient.NullOrUndefined() )
		{
			SoundInfo info = SoundInfo.InWorld(this, MaintenanceType.None );
			sustainerAmbient = SoundStarter.TrySpawnSustainer( def.building.soundAmbient, info );
		}

		ListerBuildingsRepairable.Notify_BuildingSpawned(this);
	}

	public override void DeSpawn()
	{
		base.DeSpawn();

		if( sustainerAmbient != null )
			sustainerAmbient.End();

		if( def.IsEdifice() )
			Find.EdificeGrid.DeRegister(this);

		IntRect occRect = GenAdj.OccupiedRect(this);
		for( int z=occRect.minZ; z<=occRect.maxZ; z++ )
		{
			for( int x=occRect.minX; x<=occRect.maxX; x++ )
			{
				IntVec3 c = new IntVec3(x,0,z);

				MapChangeType changeType = MapChangeType.Buildings;

				if( def.coversFloor )
					changeType |= MapChangeType.Terrain;

				if( def.Fillage == FillCategory.Full )
					changeType |= MapChangeType.Roofs;

				Find.Map.mapDrawer.MapChanged( c, changeType );

				Find.GlowGrid.MarkGlowGridDirty(c);
			}
		}

		ListerBuildingsRepairable.Notify_BuildingDeSpawned(this);
	}
	
	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		base.Destroy(mode);
		
        if( def.MakeFog )
            Find.FogGrid.Notify_FogBlockerDestroyed(Position);

		if( def.holdsRoof )
			RoofCollapseChecker.Notify_RoofHolderDestroyed(this);

		if( def.leaveTerrain != null && Game.Mode == GameMode.MapPlaying )
		{
			IntRect occRect = GenAdj.OccupiedRect(this);
			for( int z=occRect.minZ; z<=occRect.maxZ; z++ )
			{
				for( int x=occRect.minX; x<=occRect.maxX; x++ )
				{
					Find.TerrainGrid.SetTerrain(new IntVec3(x,0,z), def.leaveTerrain);
				}
			}
		}

		Find.ListerBuildings.Remove(this);		
	}


	public override void Draw()
	{
		if( Health < MaxHealth && def.useStandardHealth )
			OverlayDrawer.DrawOverlay(this, OverlayTypes.Damaged);

		//If we've already added to the map mesh don't bother with drawing our base mesh
		if( def.drawerType == DrawerType.RealtimeOnly )
			base.Draw();
		
		Comps_PostDraw();
	}

	public override void SetFaction(Faction newFaction)
	{
		base.SetFaction(newFaction);

		ListerBuildingsRepairable.Notify_BuildingFactionChanged(this);
	}

	public override void PostApplyDamage(DamageInfo dinfo)
	{
		base.PostApplyDamage(dinfo);

		ListerBuildingsRepairable.Notify_BuildingTookDamage(this);
	}
	
	private static readonly Texture2D DeconstructCommandTex = ContentFinder<Texture2D>.Get("UI/Designators/Deconstruct");
	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach( var g in base.GetGizmos() )
		{
			yield return g;
		}

		if( Designator_Deconstruct.IsDeconstructible(this) )
		{
			Command_Action act = new Command_Action();
			act.action = ()=>Designator_Deconstruct.DesignateDeconstruct(this);
			act.icon = DeconstructCommandTex;
			act.defaultLabel = "DesignatorDeconstruct".Translate();
			act.defaultDesc = "DesignatorDeconstructDesc".Translate();
			act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
			act.groupKey = 689736;
			yield return act;
		}
	}



	public virtual bool ClaimableBy(Faction faction)
	{
		return !def.building.isNaturalRock;
	}

	public static IntVec3 InteractionCellWhenAt( EntityDef tDef, IntVec3 loc, IntRot rot )
	{	
		IntVec3 rotatedOffset = tDef.interactionSquareOffset.RotatedBy(rot);		
		return loc + rotatedOffset;
	}
}
}
