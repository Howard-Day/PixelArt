using UnityEngine;
using System.Collections;

public class Rotater : MonoBehaviour {
	[HideInInspector]
	public bool Rotate = true;
	//public bool sourceParentRotationSnap = false;
	public bool Smooth = true;
	public float Speed = 1f;
	public int Skip = 120;
	public bool WorldAxis = false;
	public bool debug;
	int Framecounter;
	Vector3 RotateAxis;
	Vector3 ParentRotation;
	// Update is called once per frame
	void Update () {



		if (WorldAxis)
			RotateAxis = Vector3.up;
		else
			RotateAxis = transform.up;

		if (Rotate) {

			if (!Smooth) {
				if (PixelArt.framecount % Skip == 0) {
					transform.RotateAround (transform.position, RotateAxis, Speed);
				}
			} else {
				transform.RotateAround (transform.position, RotateAxis, Speed);
			}
		}
		if (DemoSceneControl.Play)
			Rotate = true;
		else
			Rotate = false;

		if (debug)
			Debug.Log (Rotate);
	}
}
