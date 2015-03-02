using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using Verse;



namespace RimWorld{
public class Apparel : ThingWithComponents
{
	//Working vars
	public Pawn wearer;

	public virtual void DrawWornExtras()
	{
	}

	public virtual bool CheckPreAbsorbDamage(DamageInfo dinfo)
	{
		return false;
	}

	public virtual bool AllowVerbCast(IntVec3 root, TargetInfo targ)
	{
		return true;
	}

	public virtual IEnumerable<Gizmo> GetWornGizmos()
	{
		yield break;
	}

	public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
	{
		base.Destroy(mode);

		if( Destroyed && wearer != null )
			wearer.apparel.Notify_WornApparelDestroyed(this);
	}
}
}