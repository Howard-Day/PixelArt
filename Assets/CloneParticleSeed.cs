using UnityEngine;
using System.Collections;

public class CloneParticleSeed : MonoBehaviour {
	public ParticleSystem ParticleSystemToClone;
	ParticleSystem CloneSystem;
	// Use this for initialization
	void Start () {
		CloneSystem = GetComponent<ParticleSystem> ();
	}
	
	// Update is called once per frame
	void Update () {
		CloneSystem.randomSeed = ParticleSystemToClone.randomSeed;
	}
}
