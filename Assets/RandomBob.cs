using UnityEngine;
using System.Collections;

public class RandomBob : MonoBehaviour {
	public float BobAmount = 5f;
	public int BobFreq = 60;
	public float BobSmoothing = 1f;
	Vector3 StartLoc;
	int BobFrame;
	float randomBob;
	float BobHeight;
	float refBob;

	// Use this for initialization
	void Start () {
		StartLoc = transform.localPosition;
		BobFrame = Random.Range (BobFreq / 2, BobFreq);
	}

	// Update is called once per frame
	void Update () {
		if (PixelArt.framecount % BobFrame == 0) {
			randomBob = Random.Range (-BobAmount, BobAmount);
			BobFrame = Random.Range (BobFreq / 2, BobFreq);
		}
		if (SceneControl.Play) {
			BobHeight = Mathf.SmoothDamp (BobHeight, randomBob, ref refBob, BobSmoothing);
			transform.localPosition = StartLoc + Vector3.up * BobHeight;
		}

	}
}
