using UnityEditor;
using UnityEngine;

// Editor window for listing all float curves in an animation clip
public class ClipInfo : EditorWindow
{
	private AnimationClip clip;
	
	[MenuItem ("Window/Clip Info")]
	static void Init ()
	{
		GetWindow (typeof (ClipInfo));
	}
	
	public void OnGUI()
	{
		clip = EditorGUILayout.ObjectField ("Clip", clip, typeof (AnimationClip), false) as AnimationClip;
		
		EditorGUILayout.LabelField ("Curves:");
		if (clip != null)
		{
			foreach (var binding in AnimationUtility.GetCurveBindings (clip))
			{
				AnimationCurve curve = AnimationUtility.GetEditorCurve (clip, binding);

				EditorGUILayout.LabelField (binding.path + "/" + binding.propertyName + ", Keys: " + curve.keys.Length);

				for(int i=0;i<curve.keys.Length;i++)
				{
					EditorGUILayout.LabelField (curve.keys[i].inTangent +" " + curve.keys[i].outTangent);
					curve.keys[i].inTangent = Mathf.Infinity;
					curve.keys[i].outTangent = Mathf.Infinity;
					curve.keys[i].tangentMode = 3;
				}
				AnimationUtility.SetEditorCurve(clip,binding,curve);


			}
		}
	}
}