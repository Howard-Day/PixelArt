using UnityEngine;
using System.Collections;

public class ParticlePlay : MonoBehaviour {
	ParticleSystem[] emitters;
	public int FrameRate = 15;
	public bool steppedSimulate = true;
	public bool debugStep = false;
	float nextStep;
	float steppedDelta; 

	// Use this for initialization
	void Start () {
		emitters = GetComponentsInChildren<ParticleSystem> ();

	}
	
	// Update is called once per frame
	void FixedUpdate () {
		float FrameDelta = Mathf.Round(PixelArt.FPS/FrameRate);
		if (!DemoSceneControl.Play) {
			foreach(ParticleSystem emitter in emitters){
				emitter.playbackSpeed = 0;
			}

		} 
		else {
			if(steppedSimulate){
				
				if(PixelArt.framecount >= nextStep)
				{
					foreach(ParticleSystem emitter in emitters){
						emitter.playbackSpeed = 1*FrameDelta;
					}
					nextStep = Mathf.Round(PixelArt.framecount+FrameDelta);
					if(debugStep)
						Debug.Log(gameObject.name + " Just Stepped at frame: "+  PixelArt.framecount +" will step again at: "  + nextStep );
				}
				else {
					foreach(ParticleSystem emitter in emitters){
						emitter.playbackSpeed = 0;
					}
					if(debugStep)
						Debug.Log("Not Stepping");
				}

			}
			else {
				foreach(ParticleSystem emitter in emitters){
					emitter.playbackSpeed = 1;
				}
			}
		}
	}
}
