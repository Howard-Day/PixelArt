using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
[ExecuteInEditMode]
public class PixelArt : MonoBehaviour
{
	//public static readonly int MAX_NUM_COLORS = 8;
	public int pixelScale = 1;
	public bool isOrthographic = false;
	public float shaderOutlineWidth = 1;
	public static bool enableOutlines = true; 
	public int horizontalResolution = 160;
	public int verticalResolution = 200;
	public bool setManually = false;
	public static Texture2D IndexLUT;
	public Shader BufferShader; 
	public LayerMask BufferLayer;
	public static int BufferAA = 2;
	public float AAMulti = 2.5f;
	public static float vertPixelLocking;
	public Texture2D defaultLUT;
	public Texture2D ditherTex; 
	public TextMesh FPSText;
	RenderTexture AABuffer;
	RenderTexture Buffer;
	Camera RenderCam;
	float OutlinePixelScaling;
	Camera BufferCam; 
	GameObject BufferPlane;
	Material BufferMat;
	Material AABufferMat;
	public static float FPS = 60f; 
	
	float updateInterval = .5f;
	float accum   = 0; // FPS accumulated over the interval
	public static int frames  = 0; // Frames drawn over the interval
	public static int framecount  = 0; // total framecount!
	float timeleft; // Left time for current interval
	
	
	void Start(){
		Application.targetFrameRate = 60;
		FPS = 60;
		CleanBuffers ();
		RenderCam = gameObject.GetComponent<Camera> ();
		RegisterBuffer ();
		timeleft = updateInterval; 
		if(ditherTex != null)
			Shader.SetGlobalTexture ("_DitherTex",ditherTex);
	}
	void CleanBuffers(){
		if (transform.FindChild ("Buffer Camera"))
			DestroyImmediate(transform.FindChild ("Buffer Camera").gameObject);
		if (transform.FindChild ("Buffer Camera"))
			DestroyImmediate(transform.FindChild ("Buffer Camera").gameObject);
		if (transform.FindChild ("Buffer Camera"))
			DestroyImmediate(transform.FindChild ("Buffer Camera").gameObject);
		if (transform.FindChild ("Buffer Camera"))
			DestroyImmediate(transform.FindChild ("Buffer Camera").gameObject);
	}
	int OldAA;
	Vector2 OldSize;
	bool OldOutlines;

	void UpdateRTT(){
		RenderCam = gameObject.GetComponent<Camera> ();
		Buffer = new RenderTexture (horizontalResolution, verticalResolution, 16);
		Buffer.generateMips = false;
		Buffer.filterMode = FilterMode.Point;
		//Buffer.antiAliasing = BufferAA;
		Buffer.name = "Pixel Buffer!";
		RenderCam.targetTexture = Buffer; 
		if (BufferAA > 1) {
			AABuffer = new RenderTexture(Mathf.CeilToInt(horizontalResolution*AAMulti),Mathf.CeilToInt(verticalResolution*AAMulti),16);

			AABuffer.filterMode = FilterMode.Bilinear;
			AABuffer.generateMips = false;
			AABuffer.antiAliasing = 1;// BufferAA;

			Buffer.name = "AA Pixel Buffer!";
			RenderCam.targetTexture = AABuffer;
			Buffer.MarkRestoreExpected ();

		}
		if (pixelScale == 5)
			OutlinePixelScaling = 2f;
		if (pixelScale == 4)
			OutlinePixelScaling = 1.666f;
		if (pixelScale == 3)
			OutlinePixelScaling = 1.25f;
		if (pixelScale == 2)
			OutlinePixelScaling = .9f;
		if (pixelScale == 1)
			OutlinePixelScaling = .5f;
		if (Application.platform == RuntimePlatform.OSXWebPlayer || Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.WebGLPlayer) {
			OutlinePixelScaling *= -.05f;	
		} 
		if (!enableOutlines)
			OutlinePixelScaling = 0;
		if(BufferAA == 1)
			OutlinePixelScaling *= 1.1f;	
		Shader.SetGlobalFloat("_DitherScale", 24f/2*(768f/Screen.height)*pixelScale  );
		if (!isOrthographic) {
			Shader.SetGlobalFloat ("_OutlineWidth", (shaderOutlineWidth / 2f) * (768f / Screen.height)); 
		}
		if(isOrthographic)
		{	
			
			Shader.SetGlobalFloat ("_OutlineWidth", (shaderOutlineWidth * 1600 )* (768f/Screen.height) * (Camera.main.orthographicSize/12)*OutlinePixelScaling ); 
			Shader.SetGlobalVector("_DitherScale", new Vector4(Screen.width/256f/pixelScale, Screen.height/256f/pixelScale,0,0) );
			//Debug.Log (Screen.width/256f);
		}
		#if UNITY_EDITOR
		if(!Application.isPlaying && isOrthographic)
			Shader.SetGlobalFloat ("_OutlineWidth", (shaderOutlineWidth * 2f )* (768f/Screen.height));
		#endif

		if(BufferMat)
			BufferMat.mainTexture = Buffer;
		OldAA = BufferAA;
		OldSize = new Vector2 (horizontalResolution,verticalResolution);
		OldOutlines = enableOutlines;
	}
	
	void UpdateBufferPlane(){
		float pos = (BufferCam.nearClipPlane + 0.01f);
		BufferPlane.transform.localPosition = pos*Vector3.forward;
		float h = Mathf.Tan(BufferCam.fieldOfView*Mathf.Deg2Rad*0.5f)*pos*2f;
		BufferPlane.transform.localScale = new Vector3(h*BufferCam.aspect,h,0f);
	}
	
	void RegisterBuffer()
	{
		CleanBuffers ();
		UpdateRTT ();
		GameObject CamObj = new GameObject ("Buffer Camera");
		CamObj.transform.parent = gameObject.transform;
		CamObj.transform.localPosition = Vector3.zero;
		CamObj.transform.localRotation = Quaternion.identity;
		CamObj.transform.localScale = Vector3.one;
		CamObj.AddComponent<Camera> ();
		BufferCam = CamObj.GetComponent<Camera> ();
		BufferCam.cullingMask = BufferLayer;
		BufferCam.farClipPlane = 2f;
		BufferPlane = GameObject.CreatePrimitive (PrimitiveType.Quad);
		BufferPlane.name = "Buffer Display";
		BufferPlane.transform.parent = BufferCam.transform;
		BufferPlane.transform.localPosition = Vector3.zero;
		BufferPlane.transform.localRotation = Quaternion.identity;
		BufferPlane.transform.localScale = Vector3.one;
		BufferPlane.layer = 8;
		BufferMat = new Material (BufferShader);
		BufferMat.mainTexture = Buffer;
		if(IndexLUT != null)
			BufferMat.SetTexture ("_LUTTex", IndexLUT);
		else
			BufferMat.SetTexture ("_LUTTex", defaultLUT);
		if(AABufferMat != null)
			AABufferMat.SetTexture ("_LUTTex", IndexLUT);

		BufferMat.SetFloat ("_LUTSize", 32);
		BufferPlane.GetComponent<Renderer> ().material = BufferMat;
		
		UpdateBufferPlane ();
	}
	
	
	
	public void OnPostRender()
	{
		if (BufferMat) {
			if(IndexLUT != null)
				BufferMat.SetTexture ("_LUTTex", IndexLUT);
			else
				BufferMat.SetTexture ("_LUTTex", defaultLUT);
		}
		if (BufferAA > 1) {
			Shader.SetGlobalFloat ("_PixelSnap", (vertPixelLocking / AAMulti/3));
			Debug.Log ((vertPixelLocking / AAMulti / 4));
		} 
		else {
			Shader.SetGlobalFloat ("_PixelSnap", (vertPixelLocking/3));
			Debug.Log((vertPixelLocking / 4));
		}
		if (!AABufferMat) {
			AABufferMat = new Material (BufferShader);
			AABufferMat.SetInt ("_LUTSize", 32);
		} 
		else {
			if(IndexLUT != null)
				AABufferMat.SetTexture ("_LUTTex", IndexLUT);
			else
				AABufferMat.SetTexture ("_LUTTex", defaultLUT);
		}
		if (!setManually) {
			pixelScale = Mathf.CeilToInt (Screen.height / 320);
			horizontalResolution = Screen.width / pixelScale;
			verticalResolution = Screen.height / pixelScale;
		} else {
			horizontalResolution = Screen.width / pixelScale;
			verticalResolution = Screen.height / pixelScale;
		}
		
		horizontalResolution = Mathf.Clamp (horizontalResolution, 1, 2048);
		verticalResolution = Mathf.Clamp (verticalResolution, 1, 2048);
		
		if (!BufferCam) {
			RegisterBuffer ();
		}
		if (OldAA != BufferAA || horizontalResolution != OldSize.x || verticalResolution != OldSize.y || OldOutlines != enableOutlines) {
			UpdateRTT ();
			UpdateBufferPlane ();
		}
		

		//Debug.Log (FPS+" is the FPS");
		if (BufferAA > 1) {
			Graphics.Blit (AABuffer, Buffer,AABufferMat);
		}
		
	}
	void Update()
	{
		
		timeleft -= Time.deltaTime;
		accum += Time.timeScale/Time.deltaTime;
		++frames;
		++framecount;
		// Interval ended - update GUI text and start new interval
		if( timeleft <= 0.0 )
		{
			// display two fractional digits (f2 format)
			FPS = accum/frames;
			timeleft = updateInterval;
			accum = 0.0F;
			frames = 0;
		}
		FPSText.text = FPS.ToString();
		//FPS = (1f/Time.deltaTime);
		//	Debug.Log("FPS = " +1f/Time.deltaTime);

	}
	void OnApplicationQuit(){
		Shader.SetGlobalFloat ("_OutlineWidth", (shaderOutlineWidth * 2f )* (768f/Screen.height));
		CleanBuffers();
	}
	void OnDisable ()
	{
		CleanBuffers();
		RenderCam.targetTexture = null;
	}
}




