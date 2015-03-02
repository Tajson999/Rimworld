using UnityEngine;
using System.Collections;
using Verse;
using Verse.Sound;


namespace RimWorld{
public class CompExplosive : ThingComp
{
	//Working vars
	public bool 			wickStarted = false;
	protected int			wickTicksLeft = 0;
	
	//Components
	protected Sustainer	wickSoundSustainer = null;
	
	//Constants
	private static readonly SoundDef WickStartSound = SoundDef.Named("MetalHitImportant");
	private static readonly SoundDef WickLoopSound = SoundDef.Named("HissSmall");
	
	//Properties
	protected int StartWickThreshold
	{
		get
		{
			return Mathf.RoundToInt(props.startWickHealthPercent * parent.MaxHealth);
		}
	}


	public override void PostExposeData()
	{
		base.PostExposeData();

		Scribe_Values.LookValue( ref wickStarted, "wickStarted", false );
		Scribe_Values.LookValue( ref wickTicksLeft, "wickTicksLeft", 0 );
	}


	public override void CompTick()
	{
		if( wickStarted )
		{
			if( wickSoundSustainer == null )
				StartWickSustainer(); //or sustainer is missing on load
			else
				wickSoundSustainer.Maintain();
			
			wickTicksLeft--;
			if( wickTicksLeft <= 0 )
				Detonate();
		}
	}
	
	private void StartWickSustainer()
	{
		WickStartSound.PlayOneShot(parent.Position);
		SoundInfo info = SoundInfo.InWorld(parent, MaintenanceType.PerTick);
		wickSoundSustainer = WickLoopSound.TrySpawnSustainer( info );
	}

	public override void PostDraw()
	{
		if( wickStarted )
		{
			OverlayDrawer.DrawOverlay(parent, OverlayTypes.BurningWick);
		}
	}
	
	public override void PostPostApplyDamage(DamageInfo dinfo)
	{
		if( parent.Health <= 0 )
		{
			if( dinfo.Def.externalViolence )
				Detonate();
		}
		else
		{
			
			if( wickStarted && dinfo.Def == DamageDefOf.Stun )
				StopWick();		
			else if( !wickStarted && parent.Health <= StartWickThreshold )
			{
				if( dinfo.Def.externalViolence )
					StartWick();
			}
		}
	}
	
	public override void PostDestroy( DestroyMode mode )
	{
		if( mode == DestroyMode.Kill )
			Detonate();
	}
	
	public void StartWick()
	{
		if( wickStarted )
		{
			Log.Warning("Started wick twice on " + parent );
			return;
		}
		
		wickStarted = true;
		wickTicksLeft = props.wickTicks.RandomInRange;
		StartWickSustainer();
	}
	
	public void StopWick()
	{
		wickStarted = false;	
	}
	

	bool detonated = false;
	protected void Detonate()
	{	
		if( detonated )
			return;

		detonated = true;

		if( !parent.Destroyed )
			parent.Destroy(	DestroyMode.Kill );

		//Expand radius for stackcount
		float radius = props.explosiveRadius;
		if( parent.stackCount > 1 && props.explosiveExpandPerStackcount > 0 )
			radius += Mathf.Sqrt((parent.stackCount-1) * props.explosiveExpandPerStackcount);

		GenExplosion.DoExplosion( parent.Position, radius, props.explosiveDamageType, parent );
	}	
}}