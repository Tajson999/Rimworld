using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;



namespace RimWorld{
public class Building_PowerPlantSolar : Building_PowerPlant
{
	//Constants
	private const float FullSunPower = 1700;
	private const float NightPower = 0;
	private static readonly Vector2 BarSize = new Vector2(2.3f, 0.14f);
	private static readonly Material BarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial( new Color(0.5f, 0.475f, 0.1f) );
	private static readonly Material BarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial( new Color( 0.15f, 0.15f, 0.15f ) );
	//private const float BarWobbleAmp = 0.015f;
	//private const float BarWobbleFreq = 0.3f;
	


	public override void Tick()
	{
		base.Tick();

		if( Find.RoofGrid.Roofed(Position) )
			powerComp.powerOutput = 0;
		else
			powerComp.powerOutput = Mathf.Lerp( NightPower, FullSunPower, SkyManager.CurSkyGlow );
	}

	public override void Draw()
	{
		base.Draw( );
				
		GenDraw.FillableBarRequest req = new GenDraw.FillableBarRequest();
		req.center = DrawPos + Vector3.up*0.1f;
		req.size = BarSize;

		req.fillPercent = powerComp.powerOutput / FullSunPower;
		//if(req.FillPercent > 0.01f )
		//	req.FillPercent += Mathf.Sin((Find.TickManager.tickCount+thingIDNumber) * BarWobbleFreq) * BarWobbleAmp;

		req.filledMat = BarFilledMat;
		req.unfilledMat = BarUnfilledMat;
		req.margin = 0.15f;

		IntRot rot = Rotation;
		rot.Rotate(RotationDirection.Clockwise);
		req.rotation = rot;
		
		GenDraw.DrawFillableBar(req);	



	}
}}