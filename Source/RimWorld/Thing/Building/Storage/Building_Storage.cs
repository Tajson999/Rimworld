using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld{
public class Building_Storage : Building, SlotGroupParent
{
		//Working vars
	public SlotGroup		slotGroup;
	public StorageSettings	settings;

	private List<IntVec3>	cachedOccupiedCells = null;

	//=======================================================================
	//========================== SlotGrouParent interface=======================
	//=======================================================================

	public SlotGroup GetSlotGroup(){return slotGroup;}
	public virtual void Notify_ReceivedThing(Thing newItem){/*Nothing by default*/}
	public virtual void Notify_LostThing(Thing newItem){/*Nothing by default*/}
	public virtual IEnumerable<IntVec3> AllSlotCells()
	{
		foreach( IntVec3 c in GenAdj.CellsOccupiedBy(this) )
		{
			yield return c;
		}
	}
	public List<IntVec3> AllSlotCellsList()
	{
		if( cachedOccupiedCells == null )
			cachedOccupiedCells = AllSlotCells().ToList();

		return cachedOccupiedCells;
	}
	public StorageSettings GetStoreSettings()
	{
		return settings;
	}
	public StorageSettings GetParentStoreSettings()
	{
		return def.building.fixedStorageSettings;
	}
	public string SlotYielderLabel(){return LabelCap;}
	


	//=======================================================================
	//============================== Other stuff ============================
	//=======================================================================

	public override void PostMake()
	{
		base.PostMake();
		settings = new StorageSettings(this);

		if( def.building.defaultStorageSettings != null )
			settings.CopyFrom( def.building.defaultStorageSettings );
	}

	public override void SpawnSetup()
	{
		base.SpawnSetup();
		slotGroup = new SlotGroup(this);

		cachedOccupiedCells = AllSlotCells().ToList();
	}
	
	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Deep.LookDeep(ref settings, "settings", this);
	}
	
	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		if( slotGroup != null )
			slotGroup.Notify_ParentDestroying();

		base.Destroy(mode);
	}
}}