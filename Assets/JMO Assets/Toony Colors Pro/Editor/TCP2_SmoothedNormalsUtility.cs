// Toony Colors Pro+Mobile 2
// (c) 2014-2016 Jean Moreno

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// Utility to generate meshes with encoded smoothed normals, to fix hard-edged broken outline

public class TCP2_SmoothedNormalsUtility : EditorWindow
{
	[MenuItem(TCP2_Menu.MENU_PATH + "Smoothed Normals Utility")]
	static void OpenTool()
	{
		GetWindowTCP2();
	}

	static private TCP2_SmoothedNormalsUtility GetWindowTCP2()
	{
		TCP2_SmoothedNormalsUtility window = EditorWindow.GetWindow<TCP2_SmoothedNormalsUtility>(true, "TCP2 : Smoothed Normals Utility", true);
		window.minSize = new Vector2(352f, 300f);
		window.maxSize = new Vector2(352f, 600f);
		return window;
	}

	//--------------------------------------------------------------------------------------------------
	// INTERFACE

	private const string MESH_SUFFIX = "[TCP2 Smoothed]";
#if UNITY_EDITOR_WIN
	private const string OUTPUT_FOLDER = "\\Smoothed Meshes\\";
#else
	private const string OUTPUT_FOLDER = "/Smoothed Meshes/";
#endif

	private class SelectedMesh
	{
		public SelectedMesh(Mesh _mesh, string _name, bool _isAsset, Object _assoObj = null, bool _skinned = false)
		{
			this.mesh = _mesh;
			this.name = _name;
			this.isAsset = _isAsset;
			this.AddAssociatedObject(_assoObj);

			this.isSkinned = _skinned;
			if(_assoObj != null && _assoObj is SkinnedMeshRenderer)
				this.isSkinned = true;
			else if(this.mesh != null && this.mesh.boneWeights != null && this.mesh.boneWeights.Length > 0)
				this.isSkinned = true;
		}

		public void AddAssociatedObject(Object _assoObj)
		{
			if(_assoObj != null)
			{
				this._associatedObjects.Add(_assoObj);
			}
		}

		public Mesh mesh;
		public string name;
		public bool isAsset;
		public Object[] associatedObjects { get { if(_associatedObjects.Count == 0) return null; else return _associatedObjects.ToArray(); } } 	//can be SkinnedMeshRenderer or MeshFilter
		public bool isSkinned;

		private List<Object> _associatedObjects = new List<Object>();
	}

	private Dictionary<Mesh, SelectedMesh> mMeshes;
	private string mFormat = "XYZ";
	private bool mVColors, mTangents, mUV2;
	private Vector2 mScroll;

	private bool mAlwaysOverwrite;
	
	//--------------------------------------------------------------------------------------------------
	
	private void LoadUserPrefs()
	{
		mAlwaysOverwrite = EditorPrefs.GetBool("TCP2SMU_mAlwaysOverwrite", false);
	}
	
	private void SaveUserPrefs()
	{
		EditorPrefs.SetBool("TCP2SMU_mAlwaysOverwrite", mAlwaysOverwrite);
	}

	void OnEnable() { LoadUserPrefs(); }
	void OnDisable() { SaveUserPrefs(); }

	void OnFocus()
	{
		mMeshes = GetSelectedMeshes();
	}

	void OnGUI()
	{
		EditorGUILayout.BeginHorizontal();
		TCP2_GUI.HeaderBig("TCP 2 - SMOOTHED NORMALS UTILITY");
		TCP2_GUI.HelpButton("Smoothed Normals Utility");
		EditorGUILayout.EndHorizontal();
		TCP2_GUI.Separator();

		/*
		mFormat = EditorGUILayout.TextField(new GUIContent("Axis format", "Normals axis may need to be swapped before being packed into vertex colors/tangent/uv2 data. See documentation for more information."), mFormat);
		mFormat = Regex.Replace(mFormat, @"[^xyzXYZ-]", "");
		EditorGUILayout.BeginHorizontal();
		GUILayout.Label("Known formats:");
		if(GUILayout.Button("XYZ", EditorStyles.miniButtonLeft)) { mFormat = "XYZ"; GUI.FocusControl(null); }
		if(GUILayout.Button("-YZ-X", EditorStyles.miniButtonMid)) { mFormat = "-YZ-X"; GUI.FocusControl(null); }
		if(GUILayout.Button("-Z-Y-X", EditorStyles.miniButtonRight)) { mFormat = "-Z-Y-X"; GUI.FocusControl(null); }
		EditorGUILayout.EndHorizontal();
		*/

		if(mMeshes != null && mMeshes.Count > 0)
		{
			GUILayout.Space(4);
			TCP2_GUI.Header("Meshes ready to be processed:", null, true);
			mScroll = EditorGUILayout.BeginScrollView(mScroll);
			TCP2_GUI.GUILine(Color.gray, 1);
//			for(int i = 0; i < mMeshes.Count; i++)
			foreach(SelectedMesh sm in mMeshes.Values)
			{
				GUILayout.Space(2);
				GUILayout.BeginHorizontal();
				string label = sm.name;
				if(label.Contains(MESH_SUFFIX))
					label = label.Replace(MESH_SUFFIX, "\n" + MESH_SUFFIX);
				GUILayout.Label(label, EditorStyles.wordWrappedMiniLabel, GUILayout.Width(270));
				sm.isSkinned = GUILayout.Toggle(sm.isSkinned, new GUIContent("Skinned", "Should be checked if the mesh will be used on a SkinnedMeshRenderer"), EditorStyles.toolbarButton);
				GUILayout.Space(6);
				GUILayout.EndHorizontal();
				GUILayout.Space(2);
				TCP2_GUI.GUILine(Color.gray, 1);
			}
			EditorGUILayout.EndScrollView();

			GUILayout.FlexibleSpace();
			if(GUILayout.Button(mMeshes.Count == 1 ? "Generate Smoothed Mesh" : "Generate Smoothed Meshes", GUILayout.Height(30)))
			{
				List<Object> selection = new List<Object>();
				float progress = 1;
				float total = mMeshes.Count;
				foreach(SelectedMesh sm in mMeshes.Values)
				{
					if(sm == null)
						continue;

					EditorUtility.DisplayProgressBar("Hold On", (mMeshes.Count > 1 ? "Generating Smoothed Meshes:\n" : "Generating Smoothed Mesh:\n") + sm.name, progress/total);
					progress++;
					Object o = CreateSmoothedMeshAsset(sm);
					if(o != null)
						selection.Add(o);
				}
				EditorUtility.ClearProgressBar();
				Selection.objects = selection.ToArray();
			}
		}
		else
		{
			EditorGUILayout.HelpBox("Select one or multiple meshes to create a smoothed normals version.\n\nYou can also select models directly in the Scene, the new mesh will automatically be assigned.", MessageType.Info);
			GUILayout.FlexibleSpace();
		}

		TCP2_GUI.Header("Store smoothed normals in:", "You will have to select the correct option in the Material Inspector when using outlines", true);
		
		int choice =	0;
		if(mTangents)	choice = 1;
		if(mUV2)		choice = 2;
		choice = TCP2_GUI.RadioChoice(choice, true, "Vertex Colors", "Tangents", "UV2");
		EditorGUILayout.HelpBox("Smoothed Normals for Skinned meshes will be stored in Tangents only. See Help to know why.", MessageType.Warning);
		
		mVColors	= (choice == 0);
		mTangents	= (choice == 1);
		mUV2		= (choice == 2);

		TCP2_GUI.Header("Options", null, true);
		GUILayout.BeginHorizontal();
		mAlwaysOverwrite = EditorGUILayout.Toggle(new GUIContent("Always Overwrite", "Will always overwrite existing [TCP2 Smoothed] meshes"), mAlwaysOverwrite);
		if(GUILayout.Button(new GUIContent("Clear Progress Bar", "Clears the progress bar if it's hanging on screen after an error."), EditorStyles.miniButton, GUILayout.Width(164)))
		{
			EditorUtility.ClearProgressBar();
		}
		GUILayout.EndHorizontal();
	}

	//--------------------------------------------------------------------------------------------------

	private Mesh CreateSmoothedMeshAsset(SelectedMesh originalMesh)
	{
		//Check if we are ok to overwrite
		bool overwrite = true;
		string rootPath = TCP2_Utils.FindReadmePath() + OUTPUT_FOLDER;
		if(!System.IO.Directory.Exists(rootPath))
			System.IO.Directory.CreateDirectory(rootPath);

#if UNITY_EDITOR_WIN
		rootPath = rootPath.Replace(TCP2_Utils.UnityToSystemPath( Application.dataPath ), "").Replace(@"\", "/");
#else
		rootPath = rootPath.Replace(Application.dataPath, "");
#endif

		string assetPath = "Assets" + rootPath;
		string newAssetName = originalMesh.name + " " + MESH_SUFFIX + ".asset";
		if(originalMesh.name.Contains(MESH_SUFFIX))
		{
			newAssetName = originalMesh.name + ".asset";
		}
		assetPath += newAssetName;
		Mesh existingAsset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Mesh)) as Mesh;
		bool assetExists = (existingAsset != null) && originalMesh.isAsset;
		if(assetExists)
		{
			if(!mAlwaysOverwrite)
				overwrite = EditorUtility.DisplayDialog("TCP2 : Smoothed Mesh", "The following smoothed mesh already exists:\n\n" + newAssetName + "\n\nOverwrite?", "Yes", "No");

			if(!overwrite)
			{
				return null;
			}
			else
			{
				originalMesh.mesh = existingAsset;
				originalMesh.name = existingAsset.name;
			}
		}

		Mesh newMesh = null;
		if(originalMesh.isSkinned)
		{
			newMesh = TCP2_Utils.CreateSmoothedMesh(originalMesh.mesh, mFormat, false, true, false, !originalMesh.isAsset || (originalMesh.isAsset && assetExists));
		}
		else
		{
			newMesh = TCP2_Utils.CreateSmoothedMesh(originalMesh.mesh, mFormat, mVColors, mTangents, mUV2, !originalMesh.isAsset || (originalMesh.isAsset && assetExists));
		}

		if(newMesh == null)
		{
			ShowNotification(new GUIContent("Couldn't generate the mesh for:\n" + originalMesh.name));
		}
		else
		{
			if(originalMesh.associatedObjects != null)
			{
				Undo.RecordObjects(originalMesh.associatedObjects, "Assign TCP2 Smoothed Mesh to Selection");

				foreach(Object o in originalMesh.associatedObjects)
				{
					if(o is SkinnedMeshRenderer)
					{
						(o as SkinnedMeshRenderer).sharedMesh = newMesh;
					}
					else if(o is MeshFilter)
					{
						(o as MeshFilter).sharedMesh = newMesh;
					}
					else
					{
						Debug.LogWarning("[TCP2 Smoothed Normals Utility] Unrecognized AssociatedObject: " + o + "\nType: " + o.GetType());
					}
					EditorUtility.SetDirty(o);
				}
			}

			if(originalMesh.isAsset)
			{
				if(overwrite && !assetExists)
				{
					AssetDatabase.CreateAsset(newMesh, assetPath);
				}
			}
			else
				return null;
		}

		return newMesh;
	}

	private Dictionary<Mesh, SelectedMesh> GetSelectedMeshes()
	{
		Dictionary<Mesh, SelectedMesh> meshDict = new Dictionary<Mesh, SelectedMesh>();
		foreach(Object o in Selection.objects)
		{
			bool isProjectAsset = !string.IsNullOrEmpty( AssetDatabase.GetAssetPath(o) );

			//Assets from Project
			if(o is Mesh && !meshDict.ContainsKey(o as Mesh))
			{
				if((o as Mesh) != null)
				{
					SelectedMesh sm = GetMeshToAdd(o as Mesh, isProjectAsset);
					if(sm != null)
						meshDict.Add(o as Mesh, sm);
				}
			}
			else if(o is GameObject && isProjectAsset)
			{
				string path = AssetDatabase.GetAssetPath(o);
				Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
				foreach(Object asset in allAssets)
				{
					if(asset is Mesh && !meshDict.ContainsKey(asset as Mesh))
					{
						if((asset as Mesh) != null)
						{
							SelectedMesh sm = GetMeshToAdd(asset as Mesh, isProjectAsset);
							if(sm.mesh != null)
								meshDict.Add(asset as Mesh, sm);
						}
					}
				}
			}
			//Assets from Hierarchy
			else if(o is GameObject && !isProjectAsset)
			{
				SkinnedMeshRenderer[] renderers = (o as GameObject).GetComponentsInChildren<SkinnedMeshRenderer>();
				foreach(SkinnedMeshRenderer r in renderers)
				{
					if(r.sharedMesh != null)
					{
						if(meshDict.ContainsKey(r.sharedMesh))
						{
							SelectedMesh sm = meshDict[r.sharedMesh];
							sm.AddAssociatedObject(r);
						}
						else
						{
							if(r.sharedMesh.name.Contains(MESH_SUFFIX))
							{
								meshDict.Add(r.sharedMesh, new SelectedMesh(r.sharedMesh, r.sharedMesh.name, false));
							}
							else
							{
								if(r.sharedMesh != null)
								{
									SelectedMesh sm = GetMeshToAdd(r.sharedMesh, true, r);
									if(sm.mesh != null)
										meshDict.Add(r.sharedMesh, sm);
								}
							}
						}
					}
				}

				MeshFilter[] mfilters = (o as GameObject).GetComponentsInChildren<MeshFilter>();
				foreach(MeshFilter mf in mfilters)
				{
					if(mf.sharedMesh != null)
					{
						if(meshDict.ContainsKey(mf.sharedMesh))
						{
							SelectedMesh sm = meshDict[mf.sharedMesh];
							sm.AddAssociatedObject(mf);
						}
						else
						{
							if(mf.sharedMesh.name.Contains(MESH_SUFFIX))
							{
								meshDict.Add(mf.sharedMesh, new SelectedMesh(mf.sharedMesh, mf.sharedMesh.name, false));
							}
							else
							{
								if(mf.sharedMesh != null)
								{
									SelectedMesh sm = GetMeshToAdd(mf.sharedMesh, true, mf);
									if(sm.mesh != null)
										meshDict.Add(mf.sharedMesh, sm);
								}
							}
						}
					}
				}
			}
		}

		return meshDict;
	}

	private SelectedMesh GetMeshToAdd(Mesh mesh, bool isProjectAsset, Object _assoObj = null)
	{
		string meshPath = AssetDatabase.GetAssetPath(mesh);
		Mesh meshAsset = AssetDatabase.LoadAssetAtPath(meshPath, typeof(Mesh)) as Mesh;
		//If null, it can be a built-in Unity mesh
		if(meshAsset == null)
		{
			return new SelectedMesh(mesh, mesh.name, isProjectAsset, _assoObj);
		}
		string meshName = mesh.name;
		if(!AssetDatabase.IsMainAsset(meshAsset))
		{
			Object main = AssetDatabase.LoadMainAssetAtPath(meshPath);
			meshName = main.name + " - " + meshName + "_" + mesh.GetInstanceID().ToString();
		}

		SelectedMesh sm = new SelectedMesh(mesh, meshName, isProjectAsset, _assoObj);
		return sm;
	}

	private bool SelectedMeshListContains(List<SelectedMesh> list, Mesh m)
	{
		foreach(SelectedMesh sm in list)
			if(sm.mesh == m)
				return true;

		return false;
	}
}