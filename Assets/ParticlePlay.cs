using UnityEngine;
using System.Collections;

public class ParticlePlay : MonoBehaviour {
	ParticleSystem[] emitters;
	public int FrameRate = 15;
	public bool steppedSimulate = true;

	float steppedDelta; 

	// Use this for initialization
	void Start () {
		emitters = GetComponentsInChildren<ParticleSystem> ();

	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if (!DemoSceneControl.Play) {
			foreach(ParticleSystem emitter in emitters){
				emitter.playbackSpeed = 0;
			}

		} 
		else {
			if(steppedSimulate){
				if(PixelArt.framecount % Mathf.Round((1/Time.deltaTime)/FrameRate) == 0)
				{
				foreach(ParticleSystem emitter in emitters){
						emitter.playbackSpeed = 1*(PixelArt.FPS/FrameRate);
				}
				}
				else {
					foreach(ParticleSystem emitter in emitters){
						emitter.playbackSpeed = 0;
					}
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
