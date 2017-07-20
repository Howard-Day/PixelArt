using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System;
public class Screenshot : MonoBehaviour {
	public string ScreenshotPrefix = "pixart_";
	public int CurrentNumber = 15;
	void Update() {
		if(Input.GetKeyDown(KeyCode.F10))
		   {
			//Application.CaptureScreenshot(ScreenshotPrefix + CurrentNumber.ToString + ".png");
			ScreenCapture.CaptureScreenshot(ScreenshotPrefix + CurrentNumber + ".png");
			Debug.Log (ScreenshotPrefix + CurrentNumber + ".png");
			CurrentNumber++;
		}
	}

}