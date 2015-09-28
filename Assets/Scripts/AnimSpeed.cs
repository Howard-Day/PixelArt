using UnityEngine;
using System.Collections;


public class AnimSpeed : MonoBehaviour {
	[Header ("Animation Speed and Step Control")]
	[Header ("Global Speed Multiplier")]
	public float Speed;
	[Space(1)]
	[Header ("Force Step Animation, and if so, at what FPS?")]
	public bool StepAnimation = false;
	public int FrameRate = 15;

	Animation anim;
	string currentAnim;
	float currentAnimLength;
	float animationStepNormalized;
	float stepTime = 0;

	// Use this for initialization
	void Start () {
		anim = gameObject.GetComponent<Animation>();
		if (currentAnim == null) {
			currentAnim = anim.clip.name;
			currentAnimLength = anim.clip.length;
		}	
	}
	//Costly loop check for active, playing animations. 
	void FindActiveClip(){
		foreach (AnimationState clip in anim) {
			if(clip.weight >= .99f && anim.IsPlaying(clip.name) )
			{
				clip.name = currentAnim;
				currentAnimLength = clip.length;
				
			}
		}
	}

	void StepAnim(int FPS, float Length){
		animationStepNormalized = Length / FPS ;
	}


	// Update is called once per frame
	void FixedUpdate () {
		if (anim [anim.clip.name].weight < .1f)
			FindActiveClip ();

		if (StepAnimation) {
			if(DemoSceneControl.Play){
				if(PixelArt.framecount % Mathf.Round((1/Time.deltaTime)/FrameRate) == 0)
				{
					StepAnim (FrameRate,currentAnimLength);
					stepTime += animationStepNormalized*Speed;
					if(stepTime > 1)
						stepTime -= 1;
					//Debug.Log (currentAnim + " "+  stepTime +" "+ 1/PixelArt.FPS);
					anim[currentAnim].normalizedTime = stepTime;
					anim[currentAnim].normalizedSpeed = 0f;
				}
			}
			else{
				anim[currentAnim].normalizedTime = stepTime;
				anim[currentAnim].normalizedSpeed = 0f;
			}

		}

		if (!StepAnimation) {
			if (DemoSceneControl.Play)
				anim [currentAnim].normalizedSpeed = Speed;
			else
				anim [currentAnim].normalizedSpeed = 0;
		}

	
	}

}
