using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld{
public class Building_Campfire : Building
{
	private static Graphic FireGraphic = GraphicDatabase.Get<Graphic_Flicker>("Things/Special/Fire", ShaderDatabase.MotePostLight, IntVec2.one, Color.white );


	public override void DrawAt(Vector3 drawLoc)
	{
		base.DrawAt(drawLoc);

		FireGraphic.Draw( drawLoc, IntRot.north, this );
	}

}}
