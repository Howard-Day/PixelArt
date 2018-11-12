using UnityEngine;
using System.Collections;
[ExecuteInEditMode]
public class SceneControl : MonoBehaviour {
	[Header("Camera Control")]
	public Camera SceneCamera;
	public GameObject SceneUI;

	[Range(0,1)]
	public float Zoom;
	public Vector2 NearFarZoom = new Vector2(15f,60f);

	public PixelArt PixelControl;
	public UnityStandardAssets.ImageEffects.Antialiasing AAControl;
	public UnityStandardAssets.ImageEffects.EdgeDetection OutlineControl;
	public Vector2 NearFarOutline = new Vector2(0.8f,0.3f);
	public Vector2 NearFarDetect = new Vector2(0.4f,0.6f);
	[Header("Scene Lighting")]
	public bool AutoAnimate = false;
	public Light SceneLight;
	public Rotater LightSpinner;
	[Range(0,1)]
	public float TOD;
	public AnimationCurve SunOrbit;
	public AnimationCurve MoonBlend;
	public Vector3 MoonRotation;
	public float SunOrbitAmount = 60;
	public Vector2 SunRotationStartEnd = new Vector2(-90,-60);
	public Gradient SunTODColor;// = Color.white;
	public Gradient AmbTODColor;// = Color.black;
	public Gradient SkyTODColor;

	[Header("Main Buttons")]
	public Switch Dither;
	public Switch AA;
	public Switch SpinLight;
	public Switch PixelScale;
	public Switch IndexColors;
	public Switch OutlineToggle;
	public Switch PixelToggle;
	public Switch ShadowToggle;
	public Switch PlayToggle; 
	public Switch BackToggle;
	public Switch PixelLockToggle;

	[Header("Palette Buttons")]
	public Switch Default;
	public Switch DOOM;
	public Switch DForces;
	public Switch Wolfie;
	public Switch Raptor;
	public Switch GDORG32;
	public Switch Nukem;
	public Switch OTTD;
	public Switch Arne32;
	public Switch WingCom;
	public Switch Quake;
	public Switch NES;
	public Switch DPaint;
	public Switch MIsland;

	[Header("Indexes")]
	public Texture2D IndexedDefault;
	public Texture2D IndexedDOOM;
	public Texture2D IndexedDForces;
	public Texture2D IndexedWolfie;
	public Texture2D IndexedRaptor;
	public Texture2D IndexedGDORG32;
	public Texture2D IndexedNukem;
	public Texture2D IndexedOTTD;
	public Texture2D IndexedArne32;
	public Texture2D IndexedWC;
	public Texture2D IndexedQuake;
	public Texture2D IndexedNES;
	public Texture2D IndexedDPaint;
	public Texture2D IndexedMIsland;
	public Texture2D Straight;



	public static bool Play = true;
	float InitialOrthoSize = 0;
	GameObject Sun;
	float SmoothTOD;
	// Use this for initialization
	void OnEnable () {
		InitialOrthoSize = SceneCamera.orthographicSize;
		Shader.DisableKeyword ("DITHER_ON");
		Sun = SceneLight.transform.gameObject;
	}

	void TODControl () {
		SmoothTOD = Mathf.SmoothStep (SmoothTOD, TOD, .125f);
		Quaternion SunRot = Sun.transform.localRotation;
		Vector3 SunOrbitAnim = new Vector3 (SunOrbit.Evaluate(SmoothTOD)*SunOrbitAmount, Mathf.Lerp(SunRotationStartEnd.x,SunRotationStartEnd.y,SmoothTOD), 0);
		SunRot.eulerAngles = SunOrbitAnim;
		Quaternion MoonRot = new Quaternion();// Quaternion.eulerAngles(MoonRotation);
		MoonRot.eulerAngles = MoonRotation;

		Sun.transform.localRotation = Quaternion.LerpUnclamped(SunRot,MoonRot, MoonBlend.Evaluate(SmoothTOD)); 

		SceneLight.color = SunTODColor.Evaluate (SmoothTOD);
		RenderSettings.ambientLight = SkyTODColor.Evaluate (SmoothTOD);//*.75f;
		SceneCamera.backgroundColor = SkyTODColor.Evaluate(SmoothTOD);
		if (AutoAnimate && Play) {
			TOD += Time.smoothDeltaTime / 120;
			if (SmoothTOD > 1) {
				SmoothTOD = 0;
				TOD = 0;
			}
		}
	}
	float UIScale;

	void CameraControl() {
		if (InitialOrthoSize == 0) {
			InitialOrthoSize = SceneCamera.orthographicSize;
		}
		SceneCamera.orthographicSize = Mathf.SmoothStep(SceneCamera.orthographicSize,Mathf.Lerp (NearFarZoom.x, NearFarZoom.y,Zoom),.2f);
		UIScale = SceneCamera.orthographicSize / InitialOrthoSize;	
		OutlineControl.edgesOnly = Mathf.Lerp (NearFarOutline.x, NearFarOutline.y, Zoom);
		OutlineControl.edgeExp = Mathf.Lerp (NearFarDetect.x, NearFarDetect.y, Zoom);
		OutlineControl.edgesColor = AmbTODColor.Evaluate (TOD) * 3f;
		SceneUI.transform.localScale = UIScale*Vector3.one;
	}

	// Update is called once per frame
	void Update () {
		TODControl ();
		CameraControl ();
		if (Dither.Active)
			Shader.DisableKeyword ("DITHER_OFF");
		else
			Shader.EnableKeyword ("DITHER_OFF");

		if (SpinLight.Active)
			LightSpinner.Rotate = true;
		else
			LightSpinner.Rotate = false;

		if (AA.Active)
			PixelArt.BufferAA = 4;
		else
			PixelArt.BufferAA = 1;

		if (PixelScale.Active) {
			if (Screen.height > 1070)
				PixelControl.pixelScale = 3;
			else
				PixelControl.pixelScale = 3;
			if (Screen.height < 600)
				PixelControl.pixelScale = 2;
		} 
		else {
			if (Screen.height > 1070)
				PixelControl.pixelScale = 2;
			else
				PixelControl.pixelScale = 2;
			if (Screen.height < 600)
				PixelControl.pixelScale = 1;
		}
		if (PixelLockToggle.Active) {
			PixelArt.vertPixelLocking = 2f;
		} else {
			PixelArt.vertPixelLocking = 10000f;
		}

		if (!OutlineToggle.Active)
			PixelArt.enableOutlines = false;
		else
			PixelArt.enableOutlines = true;

		if (!PixelToggle.Active)
			PixelControl.pixelScale = 1;


		if (ShadowToggle.Active)
			SceneLight.shadows = LightShadows.Hard;
		else
			SceneLight.shadows = LightShadows.None;

		if (PlayToggle.Active)
			Play = true;
		else
			Play = false;

		
//		if (BackToggle.Active)
//			gameObject.GetComponent<Camera>().backgroundColor = Dark;
//		else
//			gameObject.GetComponent<Camera>().backgroundColor = Light;

		if (Default.Active) {
			PixelArt.IndexLUT = IndexedDefault;
			DOOM.Active = false;
			DForces.Active = false;
			Wolfie.Active = false;
			Raptor.Active = false;
			GDORG32.Active = false;
			Nukem.Active = false;
			OTTD.Active = false;
			Arne32.Active = false;
			WingCom.Active = false;			
			Quake.Active = false;
			MIsland.Active = false;
			NES.Active = false;
			DPaint.Active = false;
		}
		else if (DOOM.Active){
			PixelArt.IndexLUT = IndexedDOOM;
			Default.Active = false;
			DForces.Active = false;
			Wolfie.Active = false;
			Raptor.Active = false;
			GDORG32.Active = false;
			Nukem.Active = false;
			OTTD.Active = false;
			Arne32.Active = false;
			WingCom.Active = false;	
			Quake.Active = false;
			MIsland.Active = false;
			NES.Active = false;
			DPaint.Active = false;
		}
		else if (DForces.Active){
			PixelArt.IndexLUT = IndexedDForces;
			Default.Active = false;
			DOOM.Active = false;
			Wolfie.Active = false;
			Raptor.Active = false;
			GDORG32.Active = false;
			Nukem.Active = false;
			OTTD.Active = false;
			Arne32.Active = false;
			WingCom.Active = false;	
			Quake.Active = false;
			MIsland.Active = false;
			NES.Active = false;
			DPaint.Active = false;
		}
		else if (Wolfie.Active){
			PixelArt.IndexLUT = IndexedWolfie;
			Default.Active = false;
			DOOM.Active = false;
			DForces.Active = false;
			Raptor.Active = false;
			GDORG32.Active = false;
			Nukem.Active = false;
			OTTD.Active = false;
			Arne32.Active = false;
			WingCom.Active = false;			
			Quake.Active = false;
			MIsland.Active = false;
			NES.Active = false;
			DPaint.Active = false;
		}
		else if (Raptor.Active){
			PixelArt.IndexLUT = IndexedRaptor;
			Default.Active = false;
			DOOM.Active = false;
			DForces.Active = false;
			Wolfie.Active = false;
			GDORG32.Active = false;
			Nukem.Active = false;
			OTTD.Active = false;
			Arne32.Active = false;
			WingCom.Active = false;			
			Quake.Active = false;
			MIsland.Active = false;
			NES.Active = false;
			DPaint.Active = false;
		}
		else if (GDORG32.Active){
			PixelArt.IndexLUT = IndexedGDORG32;
			Default.Active = false;
			DOOM.Active = false;
			DForces.Active = false;
			Wolfie.Active = false;
			Raptor.Active = false;
			Nukem.Active = false;
			OTTD.Active = false;
			Arne32.Active = false;
			WingCom.Active = false;	
			Quake.Active = false;
			MIsland.Active = false;
			NES.Active = false;
			DPaint.Active = false;
		}
		else if (Nukem.Active){
			PixelArt.IndexLUT = IndexedNukem;
			Default.Active = false;
			DOOM.Active = false;
			DForces.Active = false;
			Wolfie.Active = false;
			Raptor.Active = false;
			GDORG32.Active = false;
			OTTD.Active = false;
			Arne32.Active = false;
			WingCom.Active = false;	
			Quake.Active = false;
			MIsland.Active = false;
			NES.Active = false;
			DPaint.Active = false;
		}
		else if (OTTD.Active){
			PixelArt.IndexLUT = IndexedOTTD;
			Default.Active = false;
			DOOM.Active = false;
			DForces.Active = false;
			Wolfie.Active = false;
			Raptor.Active = false;
			GDORG32.Active = false;
			Nukem.Active = false;
			Arne32.Active = false;
			WingCom.Active = false;	
			Quake.Active = false;
			MIsland.Active = false;
			NES.Active = false;
			DPaint.Active = false;
		}
		else if (Arne32.Active){
			PixelArt.IndexLUT = IndexedArne32;
			Default.Active = false;
			DOOM.Active = false;
			DForces.Active = false;
			Wolfie.Active = false;
			Raptor.Active = false;
			GDORG32.Active = false;
			Nukem.Active = false;
			OTTD.Active = false;
			WingCom.Active = false;	
			Quake.Active = false;
			MIsland.Active = false;
			NES.Active = false;
			DPaint.Active = false;
		}
		else if (WingCom.Active){
			PixelArt.IndexLUT = IndexedWC;
			Default.Active = false;
			DOOM.Active = false;
			DForces.Active = false;
			Wolfie.Active = false;
			Raptor.Active = false;
			GDORG32.Active = false;
			Nukem.Active = false;
			OTTD.Active = false;
			Arne32.Active = false;
			Quake.Active = false;
			MIsland.Active = false;
			NES.Active = false;
			DPaint.Active = false;
		}
		else if (Quake.Active){
			PixelArt.IndexLUT = IndexedQuake;
			Default.Active = false;
			DOOM.Active = false;
			DForces.Active = false;
			Wolfie.Active = false;
			Raptor.Active = false;
			GDORG32.Active = false;
			Nukem.Active = false;
			OTTD.Active = false;
			Arne32.Active = false;
			WingCom.Active = false;
			MIsland.Active = false;
			NES.Active = false;
			DPaint.Active = false;
		}
		else if (MIsland.Active){
			PixelArt.IndexLUT = IndexedMIsland;
			Default.Active = false;
			DOOM.Active = false;
			DForces.Active = false;
			Wolfie.Active = false;
			Raptor.Active = false;
			GDORG32.Active = false;
			Nukem.Active = false;
			OTTD.Active = false;
			Arne32.Active = false;
			WingCom.Active = false;
			Quake.Active = false;
			NES.Active = false;
			DPaint.Active = false;
		}
		else if (NES.Active){
			PixelArt.IndexLUT = IndexedNES;
			Default.Active = false;
			DOOM.Active = false;
			DForces.Active = false;
			Wolfie.Active = false;
			Raptor.Active = false;
			GDORG32.Active = false;
			Nukem.Active = false;
			OTTD.Active = false;
			Arne32.Active = false;
			WingCom.Active = false;
			Quake.Active = false;
			MIsland.Active = false;
			DPaint.Active = false;
		}
		else if (DPaint.Active){
			PixelArt.IndexLUT = IndexedDPaint;
			Default.Active = false;
			DOOM.Active = false;
			DForces.Active = false;
			Wolfie.Active = false;
			Raptor.Active = false;
			GDORG32.Active = false;
			Nukem.Active = false;
			OTTD.Active = false;
			Arne32.Active = false;
			WingCom.Active = false;
			Quake.Active = false;
			MIsland.Active = false;
			NES.Active = false;
		}
		
		if (!IndexColors.Active){
			PixelArt.IndexLUT = Straight;
			Shader.DisableKeyword ("DITHER");
		}
	}
}
