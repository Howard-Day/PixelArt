using UnityEngine;
#if UNITY_EDITOR_WIN
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
[ExecuteInEditMode]
public class CameraLocationTracker : MonoBehaviour {
	public string RuntimeCamTag = "RenderCamera";
	public bool DebugReport = false;
	public KeyCode Fullscreen;
	public static Vector3 cameraLoc;
	public static Transform cameraTransform;
	public static int frameCount = 0;
	public static float Aspect;
	public static float FOV;
	GameObject CameraToTrack;
	// Update is called once per frame
	void Start () {
		if(Application.isPlaying)
		{
			CameraToTrack = GameObject.FindGameObjectWithTag(RuntimeCamTag);
			FOV = CameraToTrack.GetComponent<Camera>().fieldOfView;
			Aspect = CameraToTrack.GetComponent<Camera>().aspect;
		}
		if(CameraToTrack && Aspect == 0)
			Aspect = CameraToTrack.GetComponent<Camera>().aspect;

	}
	bool isFullscreen = false;

	void Update(){
		frameCount ++;
		if(isFullscreen && Input.GetKeyUp(KeyCode.Escape))
		{	Screen.SetResolution(Screen.width,Screen.height,false);
			isFullscreen = false;
			Debug.Log("returning from fullscreen");
		}
		if(!isFullscreen && Input.GetKeyUp(Fullscreen))
		{	Screen.SetResolution(Screen.currentResolution.width,Screen.currentResolution.width,true);
			isFullscreen = true;
			Debug.Log ("going fullscreen!");
		}

	}
	void OnRenderObject() {
		#if UNITY_EDITOR_WIN
		if(SceneView.currentDrawingSceneView && !Application.isPlaying)
		{
			//Transform SceneCam = SceneView.currentDrawingSceneView.camera.transform;
			cameraLoc =  SceneView.currentDrawingSceneView.camera.transform.position;
			FOV =  SceneView.currentDrawingSceneView.camera.fieldOfView;
			cameraTransform = SceneView.currentDrawingSceneView.camera.transform;
			Aspect = SceneView.currentDrawingSceneView.camera.aspect;
		}
		#endif	
		
		if(!CameraToTrack && Application.isPlaying)
		{
			if(CameraToTrack == null)
				CameraToTrack = GameObject.FindGameObjectWithTag(RuntimeCamTag);
			if(CameraToTrack)
			{
				FOV = CameraToTrack.GetComponent<Camera>().fieldOfView;
				Aspect = CameraToTrack.GetComponent<Camera>().aspect;
			}
		}
		if(CameraToTrack)
			cameraLoc = CameraToTrack.transform.position;
		if(Application.isPlaying)
		{
		if(!CameraToTrack)
			cameraLoc = Vector3.zero;
		if(!cameraTransform && CameraToTrack)
			cameraTransform = CameraToTrack.transform;
		if(CameraToTrack && Time.timeSinceLevelLoad < 2)
			Aspect = CameraToTrack.GetComponent<Camera>().aspect;
		}

		if(DebugReport)
			Debug.Log (Aspect);//"The currently tracked Camera is located at: " + cameraLoc);

	}
}
