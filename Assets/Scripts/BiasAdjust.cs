using UnityEngine;
using System.Collections;

public class BiasAdjust : MonoBehaviour {
	public float PCBias = .05f;
	public float AndroidBias = .5f;
	// Use this for initialization
	void Start () {
		gameObject.GetComponent<Light> ().shadowBias = PCBias;
		if(Application.platform == RuntimePlatform.Android)
			gameObject.GetComponent<Light> ().shadowBias = AndroidBias;

	}

}
