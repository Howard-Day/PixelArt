using UnityEngine;
#if UNITY_EDITOR_WIN
using UnityEditor;
#endif
using System.Collections;
[ExecuteInEditMode]



public class FlareControl : MonoBehaviour {

	public Material FlareMaterial;
	public Vector2 FlareXAngle = new Vector2(0,15);
	public Vector2 FlareYAngle = new Vector2(0,15);
	public Vector2 FlareDist = new Vector2(8,15);
	public Color FlareColor = Color.white;
	public Color FadeColor = Color.white;
	public float FlareSize = 1f;
	public float FadeSize = 0f;
	public bool CheckVisibility = true;
	public LayerMask VisibilityMask;
	public bool Optimize = true;
	public int CheckFrequency = 2;
	public float OccludeSpeed  = .2f;
	public bool DebugReport = false;

	Mesh refMesh;
	Transform FlareGeo;
	Mesh mesh;
	Color[] colors;
	float occludeSmooth = 0f;
	float refsmooth;
	float targetOcclude = 0f;
	Vector3 cameraDir;
	Vector3 invCameraDir;
	Renderer Visibility;
	float angleRight;
	float angleUp;
	float blendRight;
	float blendUp;
	float angleToCam;
	float angleFromCam;
	float distToCam;
	float forwardCheck;
	float forwardClamp;
	float blendForward;
	float finalBlend;
	float prevBlend;
	Vector3 CamPos;
	int FrameCheck = 2;
	bool hasInitialized = false;
	bool outOfView = false; 
	// Update is called once per frame
	public static void DestroyChildren(Transform transform) {
		for (int i = transform.childCount - 1; i >= 0; --i) {
			#if UNITY_EDITOR_WIN
			transform.GetChild(i).gameObject.hideFlags = HideFlags.None;
			#endif
			GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
		}
		transform.DetachChildren();
	}
	void Initialize()
	{
		if(!FlareMaterial)
		{	//Debug.Log ("Please Use a Material!");
			return;
		}
		if(FlareGeo)
			DestroyImmediate (FlareGeo.gameObject);
		FlareGeo = null;
		DestroyChildren(gameObject.transform);
		FlareGeo = null;
		GameObject flare = GameObject.CreatePrimitive (PrimitiveType.Quad);
		refMesh = flare.GetComponent<MeshFilter>().sharedMesh;
		DestroyImmediate(flare.GetComponent<MeshCollider>());
		flare.transform.parent = gameObject.transform;
		flare.transform.localPosition = Vector3.zero;
		Visibility = flare.GetComponent<Renderer>();
		Visibility.sharedMaterial = FlareMaterial;
		Visibility.receiveShadows = false;
		Visibility.useLightProbes = false;
		Visibility.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off; 
		FlareGeo = flare.transform;
		hasInitialized = true;
	}




	void OnEnable()	{
		hasInitialized = false;
		Initialize();
		if(hasInitialized)
		{
			#if UNITY_EDITOR
			//Only do this in the editor
			MeshFilter mf = FlareGeo.gameObject.GetComponent<MeshFilter>();   //a better way of getting the meshfilter using Generics
			Mesh meshCopy = Mesh.Instantiate(mf.sharedMesh) as Mesh;  //make a deep copy
			mesh = mf.mesh = meshCopy;                    //Assign the copy to the meshes
			#else
			//do this in play mode
			mesh = FlareGeo.gameObject.GetComponent<MeshFilter>().mesh;
			#endif


			Visibility = FlareGeo.gameObject.GetComponent<Renderer>();
			colors = new Color[mesh.vertices.Length];
			int i = 0;
			while (i < colors.Length) {
				colors[i] = FadeColor;
				i++;
			}
			mesh.colors = colors;
			#if UNITY_EDITOR_WIN
			FlareGeo.hideFlags |= HideFlags.NotEditable;
			FlareGeo.hideFlags |= HideFlags.HideInHierarchy;
			#else
			FlareGeo.hideFlags = HideFlags.None;
			#endif

		}

	}
	void OnDestroy() {
		DestroyChildren(gameObject.transform);

		hasInitialized = false;
	}

	void ReDrawFlare()
	{
		int i = 0;
		while (i < colors.Length) {
			colors[i] = Color.Lerp(FadeColor, FlareColor, finalBlend);
			i++;
		}
		mesh.colors = colors;			
		FlareGeo.localScale = Vector3.one*Mathf.Lerp (FadeSize,FlareSize,finalBlend);	
	}
	void Update(){
		if(!hasInitialized)
			Initialize();
	}
	void OnRenderObject() {
		if(hasInitialized)
		{
			if(mesh == null)
			{
				mesh = FlareGeo.gameObject.GetComponent<MeshFilter>().mesh;
				Visibility = FlareGeo.gameObject.GetComponent<Renderer>();
				colors = new Color[mesh.vertices.Length];
				int i = 0;
				while (i < colors.Length) {
					colors[i] = FadeColor;
					i++;
				}
				mesh.colors = colors;
				#if UNITY_EDITOR_WIN
				FlareGeo.hideFlags |= HideFlags.NotEditable;
				FlareGeo.hideFlags |= HideFlags.HideInHierarchy;
				#else
				FlareGeo.hideFlags = HideFlags.None;
				#endif
			}
			if(Visibility == null)
			{
				Visibility = FlareGeo.gameObject.GetComponent<Renderer>();

			}
			if(colors.Length <= 1)
			{	mesh = null;
				Visibility = null;
				FlareGeo.gameObject.GetComponent<MeshFilter>().mesh = refMesh;
			}
			if(CameraLocationTracker.cameraLoc.magnitude > 0)
				distToCam = (gameObject.transform.position-CameraLocationTracker.cameraLoc).magnitude;
			else
				distToCam = 0f;
			if(distToCam > FlareDist.y)
				distToCam = FlareDist.y;
			if(distToCam < FlareDist.x)
				distToCam = FlareDist.x;
			distToCam = 1-Mathf.Clamp01((distToCam-FlareDist.x)/(FlareDist.y-FlareDist.x));

			if (distToCam > 0 && CameraLocationTracker.frameCount % FrameCheck == 0 && CameraLocationTracker.cameraTransform != null)
			{

				if(CameraLocationTracker.cameraLoc.magnitude > 0)
				{
					CamPos =  CameraLocationTracker.cameraLoc;
					cameraDir = CamPos - transform.position;
					invCameraDir = transform.position - CamPos;
				}
				angleRight = Vector3.Angle(cameraDir,gameObject.transform.right);
				angleUp = Vector3.Angle(cameraDir,gameObject.transform.up);
				
				angleRight -= 90f;
				angleUp -= 90f;
				angleRight = Mathf.Abs (angleRight);
				angleUp = Mathf.Abs (angleUp);
				if(angleRight > FlareXAngle.y)
					angleRight = FlareXAngle.y;
				if(angleUp > FlareYAngle.y)
					angleUp = FlareYAngle.y;
				
				blendRight = 1-Mathf.Clamp01((angleRight-FlareXAngle.x)/(FlareXAngle.y-FlareXAngle.x));
				blendUp = 1-Mathf.Clamp01((angleUp-FlareYAngle.x)/(FlareYAngle.y-FlareYAngle.x));
				
				angleToCam = Vector3.Angle(cameraDir,gameObject.transform.forward);
				if(CameraLocationTracker.cameraTransform.forward.magnitude > 0)
					angleFromCam = Vector3.Angle(invCameraDir,CameraLocationTracker.cameraTransform.forward);
				if(angleFromCam < CameraLocationTracker.FOV)
				{
					forwardCheck = Mathf.Max(FlareXAngle.y,FlareYAngle.y);
					forwardClamp = forwardCheck-5;
					blendForward = 1-Mathf.Clamp01((angleToCam-forwardClamp)/(forwardCheck-forwardClamp));
				
					if (CheckVisibility && distToCam > 0)
					{	
						if(Physics.Linecast(CameraLocationTracker.cameraLoc,transform.position,VisibilityMask)) 
						{
							targetOcclude = 0f;
						}
						else
						{
							targetOcclude = 1f;
						}
					}
					if(!CheckVisibility)
						targetOcclude = 1f;
				}
				if(Optimize)
					FrameCheck = Random.Range(2,CheckFrequency);
				else
					FrameCheck = 1;
			}
			if(!Mathf.Approximately (occludeSmooth,targetOcclude))
				occludeSmooth = Mathf.SmoothDamp (occludeSmooth,targetOcclude, ref refsmooth, OccludeSpeed);

			finalBlend = Mathf.Min (blendRight,blendUp)*blendForward*occludeSmooth*distToCam;
						
			if (distToCam > 0 && !outOfView && CameraLocationTracker.frameCount % FrameCheck == 0)
			{
				if(!Mathf.Approximately (finalBlend,prevBlend) && finalBlend > 0)
				{
					ReDrawFlare();				
				}
			}
			if(angleFromCam < CameraLocationTracker.FOV+10 && outOfView)
			{
				ReDrawFlare();	
				outOfView = false;
			}

			if(!outOfView && angleFromCam > CameraLocationTracker.FOV+10)
			{
				outOfView = true;
			}


			prevBlend = finalBlend;
			if(finalBlend == 0)
				Visibility.enabled = false;
			else
				Visibility.enabled = true;
			if(DebugReport)
			{	Debug.Log ("Flare brightness is: "+finalBlend+" the side angle is " + angleRight + " and is blending at "+ blendRight + "deg, and the vertical angle is: " + angleUp +"deg blending at:"+ blendUp + ". The flare is: " + distToCam +" from the camera at an angle of: " + angleFromCam);
			}
		}
		else
		{
			Initialize();
		}
		if(CamPos.magnitude > 0)
			FlareGeo.LookAt(CamPos,Vector3.up);


	}

}
