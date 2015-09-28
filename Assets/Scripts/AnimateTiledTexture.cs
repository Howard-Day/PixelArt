using UnityEngine;
using System.Collections;

class AnimateTiledTexture : MonoBehaviour
{
	public int columns = 2;
	public int rows = 2;
	public float framesPerSecond = 10f;
	public bool AffectAll = false; 
	//the current frame to display
	private int index = 0;
	bool hasBeenPaused;
	Material AnimMat;
	void Start()
	{
		if(!AffectAll)
			AnimMat = GetComponent<Renderer>().material;
		if(AffectAll)
			AnimMat = GetComponent<Renderer>().sharedMaterial;
		StartCoroutine(updateTiling());
		//set the tile size of the texture (in UV units), based on the rows and columns
		Vector2 size = new Vector2(1f / columns, 1f / rows);
		if(!AffectAll)
			GetComponent<Renderer>().material.SetTextureScale("_MainTex", size);
		if(AffectAll)
			GetComponent<Renderer>().sharedMaterial.SetTextureScale("_MainTex", size);
	}
	
	private IEnumerator updateTiling()
	{
		while (true && DemoSceneControl.Play)
		{
			//move to the next index
			index++;
			if (index >= rows * columns)
				index = 0;
			
			//split into x and y indexes

			Vector2 offset = new Vector2((float)index / columns - (index / columns), //x index
			                             (index / columns) / (float)rows);          //y index
			if(!AffectAll)
				AnimMat.SetTextureOffset("_MainTex", offset);
			if(AffectAll)
				AnimMat.SetTextureOffset("_MainTex", offset);

			yield return new WaitForSeconds(1f / framesPerSecond);
		}
		
	}
	void Update()
	{
		if (!DemoSceneControl.Play)
			hasBeenPaused = true;
		if(DemoSceneControl.Play && hasBeenPaused)
			StartCoroutine(updateTiling());
	}
}