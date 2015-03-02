using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Verse{
public class Projectile_Explosive : Projectile
{
	private int ticksToDetonation = 0;

	public override void ExposeData()
	{
		base.ExposeData();

		Scribe_Values.LookValue(ref ticksToDetonation, "ticksToDetonation");
	}


	public override void Tick()
	{
		base.Tick();
		
		if( ticksToDetonation > 0 )
		{
			ticksToDetonation--;
			
			if( ticksToDetonation <= 0 )
				Explode();
		}
	}
	
	protected override void Impact(Thing hitThing)
	{
		if( def.projectile.explosionDelay == 0 )
		{
			Explode();
			return;
		}
		else
		{
			landed = true;
			ticksToDetonation = def.projectile.explosionDelay;
		}
	}	
	
	protected virtual void Explode()
	{
		Destroy();

        BodyPartDamageInfo part = new BodyPartDamageInfo(null, BodyPartDepth.Outside);
		ExplosionInfo e = new ExplosionInfo();
		e.center = Position;
		e.radius = def.projectile.explosionRadius;
		e.dinfo = new DamageInfo( def.projectile.damageDef, 999, launcher, part );
		e.postExplosionSpawnThingDef = def.projectile.postExplosionSpawnThingDef;
		e.explosionSpawnChance = def.projectile.explosionSpawnChance;
		e.explosionSound = def.projectile.soundExplode;
        e.projectile = def;
		e.DoExplosion();
	}
}
}