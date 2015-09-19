using UnityEngine;
using System.Collections;
[ExecuteInEditMode]
public class Switch : MonoBehaviour {
	[HideInInspector] public bool Active;
	public bool defaultEnabled = false;
	public LayerMask UILayer;
	public string ActiveText;
	public string DisableText;
	public GameObject OnLight;
	public GameObject OffLight;
	TextMesh ButtonText;
	// Use this for initialization
	void Start () {
		Active = defaultEnabled;
		ButtonText = gameObject.GetComponentInChildren<TextMesh> ();
	}

	//Method uses the mouse to select the joystick, and drag it around. Great for testing, not so great for most gameplay.
	void MouseClickMethod()
	{
		Vector3 agnosticMousePos = new Vector3(Input.mousePosition.x/Screen.width,Input.mousePosition.y/Screen.height,0);
		if(Input.GetMouseButtonUp(0))
		{
			Ray point = Camera.main.ViewportPointToRay(agnosticMousePos);
			RaycastHit hit;
			if(Physics.Raycast(point, out hit,400f,UILayer))
			{
				if(hit.transform.gameObject == this.gameObject)
				{
					Debug.Log ("Touch Detected!");
					Active = !Active;
				}
			}
		}		
	}
	bool beingTouched;
	void TouchMethod()
	{	
		int count = Input.touchCount;
		if (count == 0)
			beingTouched = false;
		if (count != 0) {
			
			for (int i = 0; i < count; i++) {
				
				Touch touch = Input.GetTouch (i);
				Vector3 agnosticTouchPos = new Vector3 (touch.position.x / Screen.width, touch.position.y / Screen.height, 0);
				Ray point = Camera.main.ViewportPointToRay (agnosticTouchPos);
				RaycastHit hit;

				if (Physics.Raycast (point, out hit, 400f, UILayer)) {
					if (hit.transform.gameObject == this.gameObject && !beingTouched) {
						Debug.Log ("Touch Detected!");
						Active = !Active;
						beingTouched = true;
					}
				}
			}
		}
	}





	// Update is called once per frame
	void Update () {
		if (Application.isPlaying) {
			if (!Application.isMobilePlatform) {
				MouseClickMethod ();
			} else {
				TouchMethod ();
			}
		}
		if (Active) {
			ButtonText.text = ActiveText;
			OffLight.SetActive (false);
			OnLight.SetActive (true);
		} 
		else {
			ButtonText.text = DisableText;
			OffLight.SetActive (true);
			OnLight.SetActive (false);
		}

	}
}
