using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Verse;

using Verse.Sound;
using Verse.AI;


namespace RimWorld{
public class Building_Door : Building
{
	//Links
	public CompPowerTrader	powerComp;

	//Working vars
	public bool 			isOpen = false;
	public bool				lockedInt = false;
	protected int 			ticksUntilClose = 0;
	protected int 			visualTicksOpen = 0;

	//Constants
	private const float		BaseDoorOpenTime = 45;
	private const int		AutomaticCloseDelayTicks = 60;
	private const float		VisualDoorOffsetStart = 0.0f;
	private const float		VisualDoorOffsetEnd = 0.5f;
	private static readonly Texture2D LockCommandIcon = ContentFinder<Texture2D>.Get("UI/Commands/Lock");
	
	private const float		TempEqualizeIntervalOpen = 30;
	private const float		TempEqualizeIntervalClosed = 250;
	private const float		TempEqualizeRate = 0.55f;

	//Properties	
	public bool Locked
	{
		get{return lockedInt;}
		set
		{
			if( lockedInt == value )
				return;
			lockedInt = value;
			Reachability.ClearReachabilityCache();
		}
	}
	public bool CloseBlocked
	{
		get
		{
			var thingList = Position.GetThingList();
			for( int i=0; i<thingList.Count; i++ )
			{
				var t = thingList[i];
				//Don't close on items or pawns
				if(    t.def.category == EntityCategory.Pawn
					|| t.def.category == EntityCategory.Item )
					return true;
			}

			return false;
		}
	}
	public bool DoorPowerOn
	{
		get
		{
			return powerComp != null && powerComp.PowerOn;
		}
	}
	public bool ZeroPawnSlowdown
	{
		get
		{
			return DoorPowerOn && TicksToOpenNow <= 20;
		}
	}
	public int TicksToOpenNow
	{
		get
		{
			float ticks = BaseDoorOpenTime / this.GetStatValue( StatDefOf.DoorOpenSpeed );

			if( DoorPowerOn )
				ticks *= 0.25f;

			return Mathf.RoundToInt(ticks);
		}
	}
	private int VisualTicksToOpen
	{
		get
		{
			//return Mathf.RoundToInt(TicksToOpenNow * 0.85f);
			return TicksToOpenNow;
		}
	}


	public override void SpawnSetup()
	{
		base.SpawnSetup();

		powerComp = GetComp<CompPowerTrader>();

		//Doors default to having a metal tile underneath
		//GenSpawn.Spawn("Floor_MetalTile", Position);
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.LookValue(ref lockedInt, "locked", defaultValue: false);
		Scribe_Values.LookValue(ref isOpen, "isOpen", defaultValue: false);
	}

	public override void Tick()
	{
		base.Tick ();

		if( !isOpen )
		{
			//Visual - slide door closed
			if( visualTicksOpen > 0 )
				visualTicksOpen--;

			//Equalize temperatures
			if( (Find.TickManager.TicksGame + thingIDNumber.HashOffset()) % TempEqualizeIntervalClosed == 0 )
				EqualizeTemps();
		}
		else if( isOpen )
		{
			//Visual - slide door open
			if( visualTicksOpen < VisualTicksToOpen )
				visualTicksOpen++;

			//Count down to closing
			if( Find.ThingGrid.CellContains( Position, EntityType.Pawn ) )
				ticksUntilClose = AutomaticCloseDelayTicks;
			else
			{
				ticksUntilClose--;

				//If the power is on, close automatically
				if( DoorPowerOn && ticksUntilClose <= 0 )
					DoorTryClose();
			}

			//Equalize temperatures
			if( (Find.TickManager.TicksGame + thingIDNumber.HashOffset()) % TempEqualizeIntervalOpen == 0 )
				EqualizeTemps();
		}
	}

	private void EqualizeTemps()
	{
		Room[] neighRooms = new Room[4];
		int neighRoomCount = 0;
		float totalTemp = 0;
		for( int i=0; i<4; i++ )
		{
			IntVec3 neigh = Position + GenAdj.CardinalDirections[i];

			if( !neigh.InBounds() )
				continue;
			
			Room r = neigh.GetRoom();
			if( r != null ) 
			{
				totalTemp += r.Temperature;
				neighRooms[neighRoomCount] = r;
				neighRoomCount++;
			}
		}

		if( neighRoomCount == 0 )
		{
			//Door is entirely surrounded by walls
			return;
		}

		//My temp becomes the average of my neighbors' temperatures
		float avgTemp = totalTemp / neighRoomCount;

		Position.GetRoom().Temperature = avgTemp;

		//Push my temperature into my neighbors
		for( int i=0; i<neighRoomCount; i++ )
		{
			float neighTemp = neighRooms[i].Temperature;

			float diff = avgTemp - neighTemp;

			neighRooms[i].PushHeat(diff * TempEqualizeRate);
		}
	}

	public void Notify_PawnApproaching( Pawn p )
	{
		//Open automatically before pawn arrives
		//Only applies if door has ZeroPawnSlowdown active now
		if( WillOpenFor(p) && ZeroPawnSlowdown )
			DoorOpen();
	}

	public virtual bool WillOpenFor( Pawn p )
	{
		if( lockedInt )
			return false;

		if (p.inventory != null && p.inventory.container.Contains(ThingDefOf.DoorKey))
			return true;

		return GenAI.MachinesLike(Faction, p);
	}
	
	public override bool BlocksPawn( Pawn p )
	{
		if( isOpen )
			return false;
		else
			return !WillOpenFor(p);
	}

	protected void DoorOpen()
	{
		isOpen = true;
		ticksUntilClose = AutomaticCloseDelayTicks;

		if( DoorPowerOn )
			def.building.soundDoorOpenPowered.PlayOneShot(Position);
		else
			def.building.soundDoorOpenManual.PlayOneShot(Position);
	}


	protected void DoorTryClose()
	{
		if( CloseBlocked )
			return;

		isOpen = false;

		if( DoorPowerOn )
			def.building.soundDoorClosePowered.PlayOneShot(Position);
		else
			def.building.soundDoorCloseManual.PlayOneShot(Position);
	}

		
	public void StartManualOpenBy( Pawn opener )
	{
		DoorOpen();
	}

	public void StartManualCloseBy( Pawn closer )
	{
		DoorTryClose();
	}

	public override void Draw()
	{
		if( Locked )
			OverlayDrawer.DrawOverlay( this, OverlayTypes.Locked );

		//Note: It's a bit odd that I'm changing game variables in Draw
		//      but this is the easiest way to make this always look right even if
		//      conditions change while the game is paused.
		Rotation = DoorRotationAt(Position);

		//Draw the two moving doors
		float pctOpen = (float)visualTicksOpen / (float)VisualTicksToOpen;			
		float offsetDist = VisualDoorOffsetStart + (VisualDoorOffsetEnd-VisualDoorOffsetStart)*pctOpen;	

		for( int i=0; i<2; i++ )
		{
			//Left, then right
			Vector3 offsetNormal = new Vector3();
			Mesh mesh;
			if( i == 0 )
			{
				offsetNormal = new Vector3(0,0,-1);
				mesh = MeshPool.plane10;
			}
			else
			{
				offsetNormal = new Vector3(0,0,1);
				mesh = MeshPool.plane10Flip;
			}
			

			//Work out move direction
			IntRot openDir = Rotation;
			openDir.Rotate(RotationDirection.Clockwise);
			offsetNormal  = openDir.AsQuat * offsetNormal;

			//Position the door
			Vector3 doorPos =  DrawPos;
			doorPos.y = Altitudes.AltitudeFor(AltitudeLayer.DoorMoveable);
			doorPos += offsetNormal * offsetDist;
		
			//Draw!
			Graphics.DrawMesh(mesh, doorPos, Rotation.AsQuat, Graphic.MatAt(Rotation), 0 );
		}
			
		Comps_PostDraw();
	}


	private static int AlignQualityAgainst( IntVec3 c )
	{
		if( !c.InBounds() )
			return 0;

		//We align against anything unwalkthroughable and against blueprints for unwalkthroughable things
		if( !c.Walkable() )
			return 9;
			

		List<Thing> things = Find.ThingGrid.ThingsListAt(c);
		for(int i=0; i<things.Count; i++ )
		{
			Thing t = things[i];

			if( t.def.eType == EntityType.Door )
				return 1;

			Thing blue = t as Blueprint;
			if( blue != null )
			{
				if( blue.def.entityDefToBuild.passability == Traversability.Impassable )
					return 9;
				if( blue.def.eType == EntityType.Door )
					return 1;
			}
		}
			
		return 0;		
	}


	public static IntRot DoorRotationAt(IntVec3 loc)
	{
		int horVotes = 0;
		int verVotes = 0;

		horVotes += AlignQualityAgainst( loc + IntVec3.east );
		horVotes += AlignQualityAgainst( loc + IntVec3.west );
		verVotes += AlignQualityAgainst( loc + IntVec3.north );
		verVotes += AlignQualityAgainst( loc + IntVec3.south );

		if( horVotes >= verVotes )
			return IntRot.north;
		else
			return IntRot.east;
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach( var g in base.GetGizmos() )
		{
			yield return g;
		}

		if( Faction == Faction.OfColony )
		{
			Command_Toggle l = new Command_Toggle();
			l.defaultLabel = "CommandToggleDoorLock".Translate();
			l.defaultDesc = "CommandToggleDoorLockDesc".Translate();
			l.groupKey = 912515;
			l.hotKey = KeyBindingDefOf.CommandItemForbid;
			l.icon = LockCommandIcon;
			l.isActive = ()=>Locked;
			l.toggleAction = ()=>Locked = !Locked;
			yield return l;
		}
	}
}
}


