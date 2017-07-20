using UnityEngine;
using System.Collections;

public class CloneParticleSeed : MonoBehaviour {
	public ParticleSystem ParticleSystemToClone;
	ParticleSystem CloneSystem;
	// Use this for initialization
	void Start () {
		CloneSystem = GetComponent<ParticleSystem> ();
		var main = CloneSystem.main;
		main.playOnAwake = false;
		CloneSystem.Stop ();
		if (!CloneSystem.isPlaying) {
			CloneSystem.randomSeed = ParticleSystemToClone.randomSeed;	
		}
		CloneSystem.Play ();
	}
	void Update () {
		
	}

	

}
