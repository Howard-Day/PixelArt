using UnityEngine;
using System.Collections;

public class ReferenceRTT : MonoBehaviour {
	public Camera RenderTextureSource;
	RenderTexture RefTex;
	Camera CurrentCam;
	// Use this for initialization
	void Start () {
		RefTex = RenderTextureSource.targetTexture;
		CurrentCam = gameObject.GetComponent<Camera>();
	}
	
	// Update is called once per frame
	void Update () {
		RefTex = RenderTextureSource.targetTexture;
		CurrentCam.targetTexture = RefTex;
	}
}
