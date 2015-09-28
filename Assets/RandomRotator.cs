using UnityEngine;
using System.Collections;

public class RandomRotator : MonoBehaviour {
	public Vector3 RotationAxis;
	public float minRotation;
	public float maxRotation;
	public int minFrameSkip;
	public int maxFrameSkip;
	int checkFrame = 1;
	Vector3 NewAxis;
	Vector3 StartRotation;
	Vector3 NewRotation;
	// Use this for initialization
	void Start () {
		StartRotation = transform.localEulerAngles;
	}
	
	// Update is called once per frame
	void Update () {
			if (PixelArt.frames % checkFrame == 0 && DemoSceneControl.Play) {
				transform.localEulerAngles = StartRotation;
			NewAxis = (gameObject.transform.right*RotationAxis.x)+(gameObject.transform.up*RotationAxis.y)+(gameObject.transform.forward*RotationAxis.z);
				
			transform.RotateAround(gameObject.transform.position, NewAxis, Random.Range(minRotation,maxRotation) );
				checkFrame = Random.Range(minFrameSkip,maxFrameSkip);
		}
	}
}
