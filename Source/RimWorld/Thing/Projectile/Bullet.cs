using UnityEngine;
using System.Collections;
using Verse.Sound;
using Verse;

namespace RimWorld{
public class Bullet : Projectile
{
	//Constants
	private const float StunChance = 0.1f;


	protected override void Impact(Thing hitThing)
	{
		base.Impact(hitThing);
		
		if( hitThing != null )
		{
			int dmgAmount = def.projectile.damageAmountBase;
            BodyPartDamageInfo part = new BodyPartDamageInfo(null, null); // any height, any depth
			DamageInfo dinfo = new DamageInfo( def.projectile.damageDef,
											   dmgAmount,
											   launcher,
											   ExactRotation.eulerAngles.y,
											   part,
											   equipment);
			hitThing.TakeDamage(dinfo);
		}
		else
		{
			SoundDefOf.BulletImpactGround.PlayOneShot(Position);
			MoteThrower.ThrowStatic( ExactPosition, ThingDefOf.Mote_ShotHit_Dirt );
		}
	}
}

	/*
public class BulletIncendiary : Bullet
{
	protected override void Impact(Thing hitThing)
	{
		base.Impact(hitThing);

		if( hitThing != null )
			hitThing.TryAttachFire(0.2f);
		else
		{
			GenSpawn.Spawn( ThingDef.Named("Puddle_Fuel"), Position);

			FireUtility.TryStartFireIn(Position, 0.2f);
		}

		MoteThrower.ThrowStatic(Position, DefDatabase<ThingDef>.GetNamed("Mote_ShotFlash"), 6f );
		MoteThrower.ThrowMicroSparks(Position.ToVector3Shifted());
	}
}*/
}