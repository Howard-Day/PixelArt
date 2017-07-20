using UnityEngine;
using System.Collections;

public class RandomScaler : MonoBehaviour {
	public float MinScale = .9f;
	public float MaxScale = 1.1f;
	public int MaxFrameSkip = 4;
	public int MinFrameSkip = 2; 
	public Vector3 NonlinearScale = Vector3.one;
	Vector3 NewScale;
	int checkFrame = 1;
	Vector3 StartScale;
	// Use this for initialization
	void Start () {
		StartScale = transform.localScale;
	}
	
	// Update is called once per frame
	void Update () {
		if (PixelArt.frames % checkFrame == 0 && SceneControl.Play) {
			NewScale = new Vector3(StartScale.x*NonlinearScale.x,StartScale.y*NonlinearScale.y,StartScale.z*NonlinearScale.z)*Random.Range(MinScale,MaxScale);
			transform.localScale = NewScale;
			checkFrame = Random.Range(MinFrameSkip,MaxFrameSkip);
		}
	}
}
