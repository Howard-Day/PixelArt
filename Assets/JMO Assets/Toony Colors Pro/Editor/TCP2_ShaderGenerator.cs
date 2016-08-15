// Toony Colors Pro+Mobile 2
// (c) 2014-2016 Jean Moreno

//#define DEBUG_MODE

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// Utility to generate custom Toony Colors Pro 2 shaders with specific features

public class TCP2_ShaderGenerator : EditorWindow
{
	//--------------------------------------------------------------------------------------------------

	[MenuItem(TCP2_Menu.MENU_PATH + "Shader Generator")]
	static void OpenTool()
	{
		GetWindowTCP2();
	}

	static public void OpenWithShader(Shader shader)
	{
		TCP2_ShaderGenerator shaderGenerator = GetWindowTCP2();
		shaderGenerator.LoadCurrentConfigFromShader(shader);
	}

	static private TCP2_ShaderGenerator GetWindowTCP2()
	{
		TCP2_ShaderGenerator window = EditorWindow.GetWindow<TCP2_ShaderGenerator>(true, "TCP2 : Shader Generator", true);
		window.minSize = new Vector2(350f, 400f);
		window.maxSize = new Vector2(500f, 900f);
		return window;
	}

	//--------------------------------------------------------------------------------------------------
	// UI
	
	static private TextAsset _Template;
	static private TextAsset Template
	{
		get
		{
			if(_Template == null)
			{
#if UNITY_5
				_Template = AssetDatabase.LoadAssetAtPath("Assets/JMO Assets/Toony Colors Pro/Editor/Shader Templates/TCP2_User_Unity5.txt", typeof(TextAsset)) as TextAsset;
#else
				_Template = AssetDatabase.LoadAssetAtPath("Assets/JMO Assets/Toony Colors Pro/Editor/Shader Templates/TCP2_User.txt", typeof(TextAsset)) as TextAsset;
#endif
			}
			return _Template;
		}
		set { _Template = value; }
	}

	//--------------------------------------------------------------------------------------------------
	// INTERFACE

	private Shader mCurrentShader;
	private TCP2_Config mCurrentConfig;
	private int mCurrentHash;
	private TextAsset mConfigSource;
	private Shader[] mUserShaders;
	private List<string> mUserShadersLabels;
	private string mDebugText;
	private Vector2 mScrollPosition;
	private int mConfigChoice;
	private bool mIsModified;
	private bool mAutoNames;
	private bool mOverwriteConfigs;
	private bool mHideDisabled;
	private bool mLoadAllShaders;
	private bool mSelectGeneratedShader;
	private bool mDirtyConfig;
	private bool mGUIEnabled;

	private bool HasFeat(string feature) { return mCurrentConfig != null && TCP2_ShaderGeneratorUtils.HasFeatures(mCurrentConfig, feature); }
	private bool HasFeatOr(params string[] features)
	{
		bool ret = false;
		if(mCurrentConfig != null)
		{
			foreach(string f in features)
				ret |= TCP2_ShaderGeneratorUtils.HasFeatures(mCurrentConfig, f);
		}
		return ret;
	}
	private bool HasFeatAnd(params string[] features)
	{
		bool ret = true;
		if(mCurrentConfig != null)
		{
			foreach(string f in features)
				ret &= TCP2_ShaderGeneratorUtils.HasFeatures(mCurrentConfig, f);
		}
		else
			ret = false;

		return ret;
	}

	void OnEnable()
	{
		LoadUserPrefs();
		ReloadUserShaders();
		if(mUserShaders != null && mUserShaders.Length > 0)
		{
			if((mConfigChoice-1) > 0 && (mConfigChoice-1) < mUserShaders.Length)
			{
				mCurrentShader = mUserShaders[mConfigChoice-1];
				LoadCurrentConfigFromShader(mCurrentShader);
			}
			else
				NewShader();
		}
	}

	void OnDisable()
	{
		SaveUserPrefs();
	}

	void OnGUI()
	{
		EditorGUILayout.BeginHorizontal();
		TCP2_GUI.HeaderBig("TOONY COLORS PRO 2 - SHADER GENERATOR");
		TCP2_GUI.HelpButton("Shader Generator");
		EditorGUILayout.EndHorizontal();
		TCP2_GUI.Separator();

		float lW = EditorGUIUtility.labelWidth;
		EditorGUIUtility.labelWidth = 105f;

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.BeginHorizontal();
		mCurrentShader = EditorGUILayout.ObjectField("Current Shader:", mCurrentShader, typeof(Shader), false) as Shader;
		if(EditorGUI.EndChangeCheck())
		{
			if(mCurrentShader != null)
			{
				LoadCurrentConfigFromShader(mCurrentShader);
			}
		}
		if(GUILayout.Button("Copy Shader", EditorStyles.miniButton, GUILayout.Width(78f)))
		{
			CopyShader();
		}
		if(GUILayout.Button("New Shader", EditorStyles.miniButton, GUILayout.Width(76f)))
		{
			NewShader();
		}
		EditorGUILayout.EndHorizontal();

		if(mIsModified)
		{
			EditorGUILayout.HelpBox("It looks like this shader has been modified externally/manually. Updating it will overwrite the changes.", MessageType.Warning);
		}

		if(mUserShaders != null && mUserShaders.Length > 0)
		{
			EditorGUI.BeginChangeCheck();
			int prevChoice = mConfigChoice;
			Color gColor = GUI.color;
			GUI.color = mDirtyConfig ? gColor * Color.yellow : GUI.color;
			GUILayout.BeginHorizontal();
			mConfigChoice = EditorGUILayout.Popup("Load Shader:", mConfigChoice, mUserShadersLabels.ToArray());
			if(GUILayout.Button("◄", EditorStyles.miniButtonLeft, GUILayout.Width(22)))
			{
				mConfigChoice--;
				if(mConfigChoice < 1) mConfigChoice = mUserShaders.Length;
			}
			if(GUILayout.Button("►", EditorStyles.miniButtonRight,GUILayout.Width(22)))
			{
				mConfigChoice++;
				if(mConfigChoice > mUserShaders.Length) mConfigChoice = 1;
			}
			GUILayout.EndHorizontal();
			GUI.color = gColor;
			if(EditorGUI.EndChangeCheck() && prevChoice != mConfigChoice)
			{
				bool load = true;
				if(mDirtyConfig)
				{
					if(mCurrentShader != null)
						load = EditorUtility.DisplayDialog("TCP2 : Shader Generation", "You have unsaved changes for the following shader:\n\n" + mCurrentShader.name + "\n\nDiscard the changes and load a new shader?", "Yes", "No");
					else
						load = EditorUtility.DisplayDialog("TCP2 : Shader Generation", "You have unsaved changes.\n\nDiscard the changes and load a new shader?", "Yes", "No");
				}
				
				if(load)
				{
					//New Shader
					if(mConfigChoice == 0)
					{
						NewShader();
					}
					else
					{
						//Load selected Shader
						Shader selectedShader = mUserShaders[mConfigChoice-1];
						mCurrentShader = selectedShader;
						LoadCurrentConfigFromShader(mCurrentShader);
					}
				}
				else
				{
					//Revert choice
					mConfigChoice = prevChoice;
				}
			}
		}

		Template = EditorGUILayout.ObjectField("Template:", Template, typeof(TextAsset), false) as TextAsset;
		EditorGUIUtility.labelWidth = lW;
		
		if(mCurrentConfig == null)
		{
			NewShader();
		}
		mGUIEnabled = GUI.enabled;

		//Name & Filename
		TCP2_GUI.Header("NAME");
		GUI.enabled = (mCurrentShader == null);
		EditorGUI.BeginChangeCheck();
		mCurrentConfig.ShaderName = EditorGUILayout.TextField(new GUIContent("Shader Name", "Path will indicate how to find the Shader in Unity's drop-down list"), mCurrentConfig.ShaderName);
		mCurrentConfig.ShaderName = Regex.Replace(mCurrentConfig.ShaderName, @"[^a-zA-Z0-9 _!/]", "");
		if(EditorGUI.EndChangeCheck() && mAutoNames)
		{
			AutoNames();
		}
		GUI.enabled &= !mAutoNames;
		EditorGUILayout.BeginHorizontal();
		mCurrentConfig.Filename = EditorGUILayout.TextField("File Name", mCurrentConfig.Filename);
		mCurrentConfig.Filename = Regex.Replace(mCurrentConfig.Filename, @"[^a-zA-Z0-9 _!/]", "");
		GUILayout.Label(".shader", GUILayout.Width(50f));
		EditorGUILayout.EndHorizontal();
		GUI.enabled = mGUIEnabled;

		Space();

		//########################################################################################################
		// FEATURES
		TCP2_GUI.Header("FEATURES");

		//Scroll view
		mScrollPosition = EditorGUILayout.BeginScrollView(mScrollPosition);
		EditorGUI.BeginChangeCheck();

#if DEBUG_MODE
		//Custom Lighting
		GUISingleFeature("CUSTOM_LIGHTING_FORCE", "Custom Lighting", "Use an inline custom lighting model, allowing more flexibility per shader over lighting");
		GUISingleFeature("VERTEX_FUNC", "Vertex Function", "Force custom vertex function in surface shader");
		Space();
		//----------------------------------------------------------------
#endif
		//Ramp
		GUIMultipleFeatures("Ramp Style", "Defines the transitioning between dark and lit areas of the model", "Slider Ramp|", "Texture Ramp|TEXTURE_RAMP");
		//Textured Threshold
		GUISingleFeature("TEXTURED_THRESHOLD", "Textured Threshold", "Adds a textured variation to the highlight/shadow threshold, allowing handpainting like effects for example");
		Space();
		//----------------------------------------------------------------
		//Detail
		GUISingleFeature("DETAIL_TEX", "Detail Texture");
		//Detail UV2
		GUISingleFeature("DETAIL_UV2", "Use UV2 coordinates", "Use second texture coordinates for the detail texture", HasFeat("DETAIL_TEX"), true);
		GUIMask("Detail Mask", null, "DETAIL_MASK", "DETAIL_MASK_CHANNEL", "DETAIL_MASK", HasFeatOr("DETAIL_TEX"), true);
		Space();
		//----------------------------------------------------------------
		//Color Mask
		GUIMask("Color Mask", "Adds a mask to the main Color", "COLORMASK", "COLORMASK_CHANNEL", "COLORMASK", true, helpTopic: "Color Mask");
		Space();
		//----------------------------------------------------------------
		//Vertex Colors
		GUISingleFeature("VCOLORS", "Vertex Colors", "Multiplies the color with vertex colors");
		//Texture Blend
		GUISingleFeature("VCOLORS_BLENDING", "Vertex Texture Blending", "Enables 2-way texture blending based on the mesh's vertex color alpha");
		Space();
		//----------------------------------------------------------------
		//Self-Illumination
		GUIMask("Self-Illumination Map", null, "ILLUMIN_MASK", "ILLUMIN_MASK_CHANNEL", "ILLUMINATION", helpTopic:"Self-Illumination Map");
		//Self-Illumination Color
		GUISingleFeature("ILLUM_COLOR", "Self-Illumination Color", null, HasFeat("ILLUMINATION"), true);
		Space();
		//----------------------------------------------------------------
		//Bump
		GUISingleFeature("BUMP", "Normal/Bump map", helpTopic: "normal_bump_map_sg");
		//Parallax
		GUISingleFeature("PARALLAX", "Parallax/Height map", null, HasFeat("BUMP"), true);
		Space();
		//----------------------------------------------------------------
		//Occlusion
//		GUISingleFeature("OCCLUSION", "Occlusion Map", "Use an Occlusion Map that will be multiplied with the Ambient lighting");
		//Occlusion RGB
//		GUISingleFeature("OCCL_RGB", "Use RGB map", "Use the RGB channels for Occlusion Map if enabled, use Alpha channel is disabled", HasFeat("OCCLUSION"), true);
//		Space();
		//----------------------------------------------------------------
		//Specular
		GUIMultipleFeaturesHelp("Specular", null, "specular_sg", "Off|", "Regular|SPECULAR", "Anisotropic|SPECULAR_ANISOTROPIC");
		if(HasFeatAnd("FORCE_SM2","SPECULAR_ANISOTROPIC"))
		{
			EditorGUILayout.HelpBox("Anisotropic Specular will not compile with Shader Model 2!\n(too many instructions used)", MessageType.Warning);
		}
		//Specular Mask
		GUIMask("Specular Mask", "Enables specular mask (gloss map)", "SPEC_MASK", "SPEC_MASK_CHANNEL", "SPECULAR_MASK", HasFeatOr("SPECULAR", "SPECULAR_ANISOTROPIC"), true);
		//Specular Shininess Mask
		GUIMask("Shininess Mask", null, "SPEC_SHIN_MASK", "SPEC_SHIN_MASK_CHANNEL", "SPEC_SHIN_MASK", HasFeatOr("SPECULAR", "SPECULAR_ANISOTROPIC"), true);
		//Cartoon Specular
		GUISingleFeature("SPECULAR_TOON", "Cartoon Specular", "Enables clear delimitation of specular color", HasFeatOr("SPECULAR", "SPECULAR_ANISOTROPIC"), true);
		Space();
		//----------------------------------------------------------------
		//Reflection
		GUISingleFeature("REFLECTION", "Reflection", "Enables cubemap reflection", helpTopic: "reflection_sg");
		//Reflection Mask
		GUIMask("Reflection Mask", null, "REFL_MASK", "REFL_MASK_CHANNEL", "REFL_MASK", HasFeatOr("REFLECTION"), true);
#if UNITY_5
		//Unity5 Reflection Probes
		GUISingleFeature("U5_REFLPROBE", "Reflection Probes (Unity5)", "Pick reflection from Unity 5 Reflection Probes", HasFeat("REFLECTION"), true, helpTopic:"Reflection Probes");
#endif
		//Reflection Color
		GUISingleFeature("REFL_COLOR", "Reflection Color", "Enables reflection color control", HasFeat("REFLECTION"), true);
		//Reflection Roughness
		GUISingleFeature("REFL_ROUGH", "Reflection Roughness", "Simulates reflection roughness using the Cubemap's LOD levels\n\nREQUIRES MipMaps ENABLED IN THE CUBEMAP TEXTURE!", HasFeat("REFLECTION") && !HasFeat("U5_REFLPROBE"), true);
		//Rim Reflection
		GUISingleFeature("RIM_REFL", "Rim Reflection/Fresnel", "Reflection will be multiplied by rim lighting, resulting in a fresnel-like effect", HasFeat("REFLECTION"), true);
		Space();
		//----------------------------------------------------------------
		//Cubemap Ambient
		GUIMultipleFeaturesInternal("Custom Ambient", "Custom ambient lighting", new string[]{"Off|", "Cubemap Ambient|CUBE_AMBIENT", "Directional Ambient|DIRAMBIENT"});
		Space();
		//----------------------------------------------------------------
		//Independent Shadows
		GUISingleFeature("INDEPENDENT_SHADOWS", "Independent Shadows", "Disable shadow color influence for cast shadows");
		Space();
		//----------------------------------------------------------------
		//Rim
		GUIMultipleFeaturesInternal("Rim", "Rim effects (fake light coming from behind the model)", new string[]{"Off|", "Rim Lighting|RIM", "Rim Outline|RIM_OUTLINE"}, !(HasFeatAnd("REFLECTION", "RIM_REFL")), false, "rim_sg");
		if(HasFeat("REFLECTION") && HasFeat("RIM_REFL"))
			TCP2_ShaderGeneratorUtils.ToggleSingleFeature(mCurrentConfig.Features, "RIM", true);
		//Vertex Rim
		GUISingleFeature("RIM_VERTEX", "Vertex Rim", "Compute rim lighting per-vertex (faster but innacurate)", HasFeatOr("RIM","RIM_OUTLINE"), true);
		//Directional Rim
		GUISingleFeature("RIMDIR", "Directional Rim", null, HasFeatOr("RIM","RIM_OUTLINE"), true);
		//Rim Mask
		GUIMask("Rim Mask", null, "RIM_MASK", "RIM_MASK_CHANNEL", "RIM_MASK", HasFeatOr("RIM","RIM_OUTLINE"), true);
		Space();
		//----------------------------------------------------------------
		//MatCap
		GUIMultipleFeaturesHelp("MatCap", "MatCap effects (fast fake reflection using a spherical texture)", "matcap_sg", "Off|", "MatCap Add|MATCAP_ADD", "MatCap Multiply|MATCAP_MULT");
		//MatCap Mask
		GUIMask("MatCap Mask", null, "MASK_MC", "MASK_MC_CHANNEL", "MASK_MC", HasFeatOr("MATCAP_ADD","MATCAP_MULT"), true);
		//MatCap Pixel
		GUISingleFeature("MATCAP_PIXEL", "Pixel MatCap", "If enabled, will calculate MatCap per-pixel\nRequires normal map", HasFeat("BUMP") && HasFeatOr("MATCAP_ADD","MATCAP_MULT"), true);
		//MatCap Color
		GUISingleFeature("MC_COLOR", "MatCap Color", null, HasFeatOr("MATCAP_ADD","MATCAP_MULT"), true);
		Space();
		//----------------------------------------------------------------
		//Sketch
		GUIMultipleFeatures("Sketch", "Sketch texture overlay on the shadowed areas\nOverlay: regular texture overlay\nGradient: used for halftone-like effects", "Off|", "Sketch Overlay|SKETCH", "Sketch Gradient|SKETCH_GRADIENT");
		//Sketch Blending
		GUIMultipleFeaturesInternal("Sketch Blending", "Defines how to blend the Sketch texture with the model",
		                            new string[]{"Regular|", "Color Burn|SKETCH_COLORBURN"},
		                            HasFeat("SKETCH") && !HasFeat("SKETCH_GRADIENT"), true, null, false, 166);
		//Sketch Anim
		GUISingleFeature("SKETCH_ANIM", "Animated Sketch", "Animates the sketch overlay texture, simulating a hand-drawn animation style",
		                 HasFeatOr("SKETCH","SKETCH_GRADIENT"), true);
		//Sketch Vertex
		GUISingleFeature("SKETCH_VERTEX", "Vertex Coords", "Compute screen coordinates in vertex shader (faster but can cause distortions)\nIf disabled will compute in pixel shader (slower)",
		                 HasFeatOr("SKETCH","SKETCH_GRADIENT"), true);
		//Sketch Scale
		GUISingleFeature("SKETCH_SCALE", "Scale with model", "If enabled, overlay texture scale will depend on model's distance from view",
		                 HasFeatOr("SKETCH","SKETCH_GRADIENT"), true);
		Space();
		//----------------------------------------------------------------
		//Outline
		GUIMultipleFeatures("Outline", "Outline around the model", "Off|", "Opaque Outline|OUTLINE", "Blended Outline|OUTLINE_BLENDING");
		GUISingleFeature("OUTLINE_BEHIND", "Outline behind model", "If enabled, outline will only show behind model",
		                 HasFeatOr("OUTLINE","OUTLINE_BLENDING"), true);
		Space();
		//----------------------------------------------------------------
		//Lightmaps
#if UNITY_4_5
		GUISingleFeature("LIGHTMAP", "TCP2 Lightmap", "Will use TCP2's lightmap decoding, affecting it with ramp and color settings", helpTopic:"Lightmap");
		Space();
#endif
		//----------------------------------------------------------------
		//Alpha Blending
		GUISingleFeature("ALPHA", "Alpha Blending");
		//Alpha Testing
		GUISingleFeature("CUTOUT", "Alpha Testing (Cutout)");
		Space();
		//----------------------------------------------------------------
		//Culling
		int cull = TCP2_ShaderGeneratorUtils.HasFeatures(mCurrentConfig, "CULL_FRONT") ? 1 : TCP2_ShaderGeneratorUtils.HasFeatures(mCurrentConfig, "CULL_OFF") ? 2 : 0;
		EditorGUILayout.BeginHorizontal();
		TCP2_GUI.SubHeader("Culling", "Defines how to cull faces", cull > 0, 166);
		cull = EditorGUILayout.Popup(cull, new string[]{"Default", "Front", "Off (double-sided)"});
		TCP2_ShaderGeneratorUtils.ToggleSingleFeature(mCurrentConfig.Features, "CULL_FRONT", cull == 1);
		TCP2_ShaderGeneratorUtils.ToggleSingleFeature(mCurrentConfig.Features, "CULL_OFF", cull == 2);
		EditorGUILayout.EndHorizontal();
		Space();
		//----------------------------------------------------------------

		//########################################################################################################
		// FLAGS
		TCP2_GUI.Header("FLAGS");
		GUISingleFlag("addshadow", "Add Shadow Passes", "Force the shader to have the Shadow Caster and Collector passes.\nCan help if shadows don't work properly with the shader");
		GUISingleFlag("fullforwardshadows", "Full Forward Shadows", "Enable support for all shadow types in Forward rendering path");
#if UNITY_5
		GUISingleFlag("noshadow", "Disable Shadows", "Disables all shadow receiving support in this shader");
		GUISingleFlag("nofog", "Disable Fog", "Disables Unity Fog support.\nCan help if you run out of vertex interpolators and don't need fog.");
#endif
		GUISingleFlag("nolightmap", "Disable Lightmaps", "Disables all lightmapping support in this shader.\nCan help if you run out of vertex interpolators and don't need lightmaps.");
		GUISingleFlag("noambient", "Disable Ambient Lighting", "Disable ambient lighting", !HasFeatOr("DIRAMBIENT","CUBE_AMBIENT","OCCLUSION"));
		GUISingleFlag("novertexlights", "Disable Vertex Lighting", "Disable vertex lights and spherical harmonics (light probes)");
		GUISingleFeature("FORCE_SM2", "Force Shader Model 2", "Compile with Shader Model 2 target. Useful for (very) old GPU compatibility, but some features may not work with it.", showHelp:false);
		
		TCP2_GUI.Header("FLAGS (Mobile-friendly)", null, true);
		GUISingleFlag("noforwardadd", "One Directional Light", "Use additive lights as vertex lights.\nRecommended for Mobile");
#if UNITY_5
		GUISingleFlag("interpolateview", "Vertex View Dir", "Calculate view direction per-vertex instead of per-pixel.\nRecommended for Mobile");
#else
		GUISingleFlag("approxview", "Vertex View Dir", "Calculate view direction per-vertex instead of per-pixel.\nRecommended for Mobile");
#endif
		GUISingleFlag("halfasview", "Half as View", "Pass half-direction vector into the lighting function instead of view-direction.\nFaster but inaccurate.\nRecommended for Specular, but use Vertex Rim to optimize Rim Effects instead");

#if DEBUG_MODE
		TCP2_GUI.SeparatorBig();
		GUILayout.BeginHorizontal();
		mDebugText = EditorGUILayout.TextField("Debug", mDebugText);
		if(GUILayout.Button("Add Feature", EditorStyles.miniButtonLeft, GUILayout.Width(80f)))
			mCurrentConfig.Features.Add(mDebugText);
		if(GUILayout.Button("Add Flag", EditorStyles.miniButtonRight, GUILayout.Width(80f)))
			mCurrentConfig.Flags.Add(mDebugText);

		GUILayout.EndHorizontal();
		GUILayout.Label("Features:");
		GUILayout.BeginHorizontal();
		int count = 0;
		for(int i = 0; i < mCurrentConfig.Features.Count; i++)
		{
			if(count >= 3)
			{
				count = 0;
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
			}
			count++;
			if(GUILayout.Button(mCurrentConfig.Features[i], EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
			{
				mCurrentConfig.Features.RemoveAt(i);
				break;
			}
		}
		GUILayout.EndHorizontal();
		GUILayout.Label("Flags:");
		GUILayout.BeginHorizontal();
		count = 0;
		for(int i = 0; i < mCurrentConfig.Flags.Count; i++)
		{
			if(count >= 3)
			{
				count = 0;
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
			}
			count++;
			if(GUILayout.Button(mCurrentConfig.Flags[i], EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
			{
				mCurrentConfig.Flags.RemoveAt(i);
				break;
			}
		}
		GUILayout.EndHorizontal();
		GUILayout.Label("Keywords:");
		GUILayout.BeginHorizontal();
		count = 0;
		foreach(KeyValuePair<string,string> kvp in mCurrentConfig.Keywords)
		{
			if(count >= 3)
			{
				count = 0;
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
			}
			count++;
			if(GUILayout.Button(kvp.Key + ":" + kvp.Value, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
			{
				mCurrentConfig.Keywords.Remove(kvp.Key);
				break;
			}
		}
		GUILayout.EndHorizontal();

		//----------------------------------------------------------------
#endif
		
		//Update config
		if(EditorGUI.EndChangeCheck())
		{
			int newHash = mCurrentConfig.ToHash();
			if(newHash != mCurrentHash)
			{
				mDirtyConfig = true;
			}
			else
			{
				mDirtyConfig = false;
			}
		}

		//Scroll view
		EditorGUILayout.EndScrollView();

		Space();

		//GENERATE

		EditorGUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
#if DEBUG_MODE
		if(GUILayout.Button("Re-Generate All", GUILayout.Width(120f), GUILayout.Height(30f)))
		{
			float progress = 0;
			float total = mUserShaders.Length;
			foreach(Shader s in mUserShaders)
			{
				progress++;
				EditorUtility.DisplayProgressBar("Hold On", "Generating Shader: " + s.name, progress/total);

				mCurrentShader = null;
				LoadCurrentConfigFromShader(s);
				if(mCurrentShader != null && mCurrentConfig != null)
				{
					TCP2_ShaderGeneratorUtils.Compile(mCurrentConfig, Template.text, false, !mOverwriteConfigs, mIsModified);
				}
			}
			EditorUtility.ClearProgressBar();
		}
#endif
		if(GUILayout.Button(mCurrentShader == null ? "Generate Shader" : "Update Shader", GUILayout.Width(120f), GUILayout.Height(30f)))
		{
			if(Template == null)
			{
				EditorUtility.DisplayDialog("TCP2 : Shader Generation", "Can't generate shader: no Template file defined!\n\nYou most likely want to link the TCP2_User.txt file to the Template field in the Shader Generator.", "Ok");
				return;
			}

			Shader generatedShader = TCP2_ShaderGeneratorUtils.Compile(mCurrentConfig, Template.text, true, !mOverwriteConfigs, mIsModified);
			ReloadUserShaders();
			if(generatedShader != null)
			{
				mDirtyConfig = false;
				LoadCurrentConfigFromShader(generatedShader);
				mIsModified = false;
			}
		}
		EditorGUILayout.EndHorizontal();
		TCP2_GUI.Separator();

		// OPTIONS
		TCP2_GUI.Header("OPTIONS");

		GUILayout.BeginHorizontal();
		mSelectGeneratedShader = GUILayout.Toggle(mSelectGeneratedShader, new GUIContent("Select Generated Shader", "Will select the generated file in the Project view"), GUILayout.Width(180f));
		mAutoNames = GUILayout.Toggle(mAutoNames, new GUIContent("Automatic Name", "Will automatically generate the shader filename based on its UI name"), GUILayout.ExpandWidth(false));
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		mOverwriteConfigs = GUILayout.Toggle(mOverwriteConfigs, new GUIContent("Always overwrite shaders", "Overwrite shaders when generating/updating (no prompt)"), GUILayout.Width(180f));
		mHideDisabled = GUILayout.Toggle(mHideDisabled, new GUIContent("Hide disabled fields", "Hide properties settings when they cannot be accessed"), GUILayout.ExpandWidth(false));
		GUILayout.EndHorizontal();
		EditorGUI.BeginChangeCheck();
		mLoadAllShaders = GUILayout.Toggle(mLoadAllShaders, new GUIContent("Reload Shaders from all Project", "Load shaders from all your Project folders instead of just Toony Colors Pro 2.\nEnable it if you move your generated shader files outside of the default TCP2 Generated folder."), GUILayout.ExpandWidth(false));
		if(EditorGUI.EndChangeCheck())
		{
			ReloadUserShaders();
		}

		TCP2_ShaderGeneratorUtils.SelectGeneratedShader = mSelectGeneratedShader;
	}

	void OnProjectChange()
	{
		ReloadUserShaders();
		if(mCurrentShader == null && mConfigChoice != 0)
		{
			NewShader();
		}
	}

	//--------------------------------------------------------------------------------------------------
	// FEATURES

	private void GUISingleFeature(string featureName, string label, string tooltip = null,
	                              bool enabled = true, bool increaseIndentLevel = false, bool visible = true,
	                              string helpTopic = null, bool showHelp = true)
	{
		if(!enabled)
			GUI.enabled = false;
		if(increaseIndentLevel)
			label = "▪  " + label;

		bool feature = TCP2_ShaderGeneratorUtils.HasFeatures(mCurrentConfig, featureName);

		if(mHideDisabled)
			visible = enabled;

		if(visible)
		{
			EditorGUILayout.BeginHorizontal();
			if(increaseIndentLevel)
			{
				TCP2_GUI.SubHeader(label, tooltip, feature && enabled, 165);
				feature = EditorGUILayout.Toggle(feature);
			}
			else
			{
				GUI.enabled = mGUIEnabled;
				if(showHelp)
					TCP2_GUI.HelpButton(label.TrimStart('▪', ' '), string.IsNullOrEmpty(helpTopic) ? label.TrimStart('▪', ' ') : helpTopic);
				GUI.enabled = enabled;
				TCP2_GUI.SubHeader(label, tooltip, feature && enabled, 145);
				feature = EditorGUILayout.Toggle(feature);
			}
			EditorGUILayout.EndHorizontal();
		}

		TCP2_ShaderGeneratorUtils.ToggleSingleFeature(mCurrentConfig.Features, featureName, feature);

		if(!enabled)
			GUI.enabled = mGUIEnabled;
	}

	private bool GUIMultipleFeaturesHelp(string label, string tooltip, string helpTopic, params string[] labelsAndFeatures)
	{
		return GUIMultipleFeaturesInternal(label, tooltip, labelsAndFeatures, helpTopic: helpTopic);
	}
	private bool GUIMultipleFeatures(string label, string tooltip, params string[] labelsAndFeatures)
	{
		return GUIMultipleFeaturesInternal(label, tooltip, labelsAndFeatures);
	}
	private bool GUIMultipleFeaturesInternal(string label, string tooltip,  string[] labelsAndFeatures, bool enabled = true, bool increaseIndentLevel = false, string helpTopic = null, bool showHelp = true, float width = 146, bool visible = true)
	{
		if(!enabled)
			GUI.enabled = false;
		if(increaseIndentLevel)
			label = "▪  " + label;

		string[] labels = new string[labelsAndFeatures.Length];
		string[] features = new string[labelsAndFeatures.Length];

		int feature = 0;
		for(int i = 0; i < labelsAndFeatures.Length; i++)
		{
			string[] data = labelsAndFeatures[i].Split('|');
			labels[i] = data[0];
			features[i] = data[1];

			if(data.Length > 1 && !string.IsNullOrEmpty(features[i]))
			{
				if(TCP2_ShaderGeneratorUtils.HasFeatures(mCurrentConfig, features[i]))
				{
					feature = i;
				}
			}
		}

		visible = mHideDisabled ? enabled : visible;

		if(visible)
		{
			string help = string.IsNullOrEmpty(helpTopic) ? label.TrimStart('▪', ' ') : helpTopic;
			EditorGUILayout.BeginHorizontal();
			if(showHelp)
				TCP2_GUI.HelpButton(label.TrimStart('▪', ' '), help);
			TCP2_GUI.SubHeader(label, tooltip, (feature > 0) && enabled, width);
			feature = EditorGUILayout.Popup(feature, labels);
			EditorGUILayout.EndHorizontal();
		}

		TCP2_ShaderGeneratorUtils.ToggleMultipleFeatures(mCurrentConfig.Features, feature, features);

		if(!enabled)
			GUI.enabled = mGUIEnabled;

		return feature > 0;
	}
	
	private bool GUIMask(string label, string tooltip, string maskKeyword, string channelKeyword, string feature = null, bool enabled = true, bool increaseIndentLevel = false, bool visible = true, string helpTopic = null)
	{
		string[] labelsAndKeywords = new string[]{
			"Off|",
			"Main Texture|mainTex",
			"Mask 1|mask1","Mask 2|mask2","Mask 3|mask3"};

		if(!enabled)
			GUI.enabled = false;
		if(increaseIndentLevel)
			label = "▪  " + label;
		
		string[] labels = new string[labelsAndKeywords.Length];
		string[] masks = new string[labelsAndKeywords.Length];
		string[] uvs = new string[]{"Main Tex UV","Independent UV"};

		for(int i = 0; i < labelsAndKeywords.Length; i++)
		{
			string[] data = labelsAndKeywords[i].Split('|');
			labels[i] = data[0];
			masks[i] = data[1];
		}

		int curMask = System.Array.IndexOf(masks, TCP2_ShaderGeneratorUtils.GetKeyword(mCurrentConfig, maskKeyword));
		if(curMask < 0) curMask = 0;
		TCP2_Utils.TextureChannel curChannel = TCP2_Utils.FromShader( TCP2_ShaderGeneratorUtils.GetKeyword(mCurrentConfig, channelKeyword) );
		if(curMask <= 1)
			curChannel = TCP2_Utils.TextureChannel.Alpha;
		string uvKey = (curMask > 1) ? "UV_" + masks[curMask] : null;
		int curUv = System.Array.IndexOf(uvs, TCP2_ShaderGeneratorUtils.GetKeyword(mCurrentConfig, uvKey));
		if(curUv < 0) curUv = 0;

		if(mHideDisabled)
			visible = enabled;

		if(visible)
		{
			EditorGUILayout.BeginHorizontal();
			float w = 166;
			if(!string.IsNullOrEmpty(helpTopic))
			{
				w -= 20;
				TCP2_GUI.HelpButton(label.TrimStart('▪', ' '), helpTopic);
			}
			TCP2_GUI.SubHeader(label, tooltip, (curMask > 0) && enabled, w);
			curMask = EditorGUILayout.Popup(curMask, labels);
			GUI.enabled = curMask > 1;
			curChannel = (TCP2_Utils.TextureChannel)EditorGUILayout.EnumPopup(curChannel);
			curUv = EditorGUILayout.Popup(curUv, uvs);
			GUI.enabled = mGUIEnabled;
			TCP2_GUI.HelpButton("Masks");
			EditorGUILayout.EndHorizontal();
		}

		TCP2_ShaderGeneratorUtils.SetKeyword(mCurrentConfig.Keywords, maskKeyword, masks[curMask]);
		if(curMask > 0)
		{
			TCP2_ShaderGeneratorUtils.SetKeyword(mCurrentConfig.Keywords, channelKeyword, curChannel.ToShader());
		}
		if(curMask > 1 && !string.IsNullOrEmpty(uvKey))
		{
			TCP2_ShaderGeneratorUtils.SetKeyword(mCurrentConfig.Keywords, uvKey, uvs[curUv]);
		}
		TCP2_ShaderGeneratorUtils.ToggleSingleFeature(mCurrentConfig.Features, feature, (curMask > 0));

		if(!enabled)
			GUI.enabled = mGUIEnabled;
		
		return curMask > 0;
	}

	private void GUISingleFlag(string flagName, string label, string tooltip = null, bool enabled = true, bool increaseIndentLevel = false, bool visible = true, string helpTopic = null)
	{
		if(!enabled)
			GUI.enabled = false;
		if(increaseIndentLevel)
			label = "▪  " + label;
		
		bool flag = TCP2_ShaderGeneratorUtils.HasFeatures(mCurrentConfig.Flags, flagName);
		
		if(visible)
		{
			EditorGUILayout.BeginHorizontal();
			if(increaseIndentLevel)
			{
				TCP2_GUI.SubHeader(label, tooltip, flag, 165);
				flag = EditorGUILayout.Toggle(flag);
			}
			else
			{
				GUI.enabled = mGUIEnabled;
				if(!string.IsNullOrEmpty(helpTopic))
					TCP2_GUI.HelpButton(label.TrimStart('▪', ' '), string.IsNullOrEmpty(helpTopic) ? label.TrimStart('▪', ' ') : helpTopic);
				GUI.enabled = enabled;
				TCP2_GUI.SubHeader(label, tooltip, flag, 145);
				flag = EditorGUILayout.Toggle(flag);
			}
			EditorGUILayout.EndHorizontal();
		}
		
		TCP2_ShaderGeneratorUtils.ToggleFlag(mCurrentConfig.Flags, flagName, flag);
		
		if(!enabled)
			GUI.enabled = mGUIEnabled;
	}

	//--------------------------------------------------------------------------------------------------
	// MISC

	private void LoadUserPrefs()
	{
		mAutoNames = EditorPrefs.GetBool("TCP2_mAutoNames", true);
		mOverwriteConfigs = EditorPrefs.GetBool("TCP2_mOverwriteConfigs", false);
		mHideDisabled = EditorPrefs.GetBool("TCP2_mHideDisabled", false);
		mSelectGeneratedShader = EditorPrefs.GetBool("TCP2_mSelectGeneratedShader", true);
		mLoadAllShaders = EditorPrefs.GetBool("TCP2_mLoadAllShaders", false);
		mConfigChoice = EditorPrefs.GetInt("TCP2_mConfigChoice", 0);
	}

	private void SaveUserPrefs()
	{
		EditorPrefs.SetBool("TCP2_mAutoNames", mAutoNames);
		EditorPrefs.SetBool("TCP2_mOverwriteConfigs", mOverwriteConfigs);
		EditorPrefs.SetBool("TCP2_mHideDisabled", mHideDisabled);
		EditorPrefs.SetBool("TCP2_mSelectGeneratedShader", mSelectGeneratedShader);
		EditorPrefs.SetBool("TCP2_mLoadAllShaders", mLoadAllShaders);
		EditorPrefs.SetInt("TCP2_mConfigChoice", mConfigChoice);
	}

	private void LoadCurrentConfig(TCP2_Config config)
	{
		mCurrentConfig = config;
		mDirtyConfig = false;
		if(mAutoNames)
		{
			AutoNames();
		}
		mCurrentHash = mCurrentConfig.ToHash();
	}

	private void NewShader()
	{
		mCurrentShader = null;
		mConfigChoice = 0;
		mIsModified = false;
		LoadCurrentConfig(new TCP2_Config());
	}

	private void CopyShader()
	{
		mCurrentShader = null;
		mConfigChoice = 0;
		mIsModified = false;
		TCP2_Config newConfig = new TCP2_Config();
		newConfig.Features = mCurrentConfig.Features;
		newConfig.Flags = mCurrentConfig.Flags;
		newConfig.Keywords = mCurrentConfig.Keywords;
		newConfig.ShaderName = mCurrentConfig.ShaderName + " Copy";
		newConfig.Filename = mCurrentConfig.Filename + " Copy";
		LoadCurrentConfig(newConfig);
	}

	private void LoadCurrentConfigFromShader(Shader shader)
	{
		ShaderImporter shaderImporter = ShaderImporter.GetAtPath(AssetDatabase.GetAssetPath(shader)) as ShaderImporter;
		string[] features;
		string[] flags;
		string[] customData;
		Dictionary<string,string> keywords;
		TCP2_ShaderGeneratorUtils.ParseUserData(shaderImporter, out features, out flags, out keywords, out customData);
		if(features != null && features.Length > 0 && features[0] == "USER")
		{
			mCurrentConfig = new TCP2_Config();
			mCurrentConfig.ShaderName = shader.name;
			mCurrentConfig.Filename = System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(shader));
			mCurrentConfig.Features = new List<string>(features);
			mCurrentConfig.Flags = (flags != null) ? new List<string>(flags) : new List<string>();
			mCurrentConfig.Keywords = (keywords != null) ? new Dictionary<string,string>(keywords) : new Dictionary<string,string>();
			mCurrentShader = shader;
			mConfigChoice = mUserShadersLabels.IndexOf(shader.name);
			mDirtyConfig = false;
			AutoNames();
			mCurrentHash = mCurrentConfig.ToHash();

			mIsModified = false;
			if(customData != null && customData.Length > 0)
			{
				foreach(string data in customData)
				{
					//Hash
					if(data.Length > 0 && data[0] == 'h')
					{
						string dataHash = data;
						string fileHash = TCP2_ShaderGeneratorUtils.GetShaderContentHash(shaderImporter);

						if(!string.IsNullOrEmpty(fileHash) && dataHash != fileHash)
						{
							mIsModified = true;
						}
					}
					//Timestamp
					else
					{
						ulong timestamp;
						if(ulong.TryParse(data, out timestamp))
						{
							if(shaderImporter.assetTimeStamp != timestamp)
							{
								mIsModified = true;
							}
						}
					}
				}
			}
		}
		else
		{
			EditorApplication.Beep();
			this.ShowNotification(new GUIContent("Invalid shader loaded: it doesn't seem to have been generated by the TCP2 Shader Generator!"));
			mCurrentShader = null;
			NewShader();
		}
	}

	private void ReloadUserShaders()
	{
		mUserShaders = GetUserShaders();
		mUserShadersLabels = new List<string>(GetShaderLabels(mUserShaders));

		if(mCurrentShader != null)
		{
			mConfigChoice = mUserShadersLabels.IndexOf(mCurrentShader.name);
		}
	}

	private Shader[] GetUserShaders()
	{
		string rootPath = Application.dataPath + (mLoadAllShaders ? "" : TCP2_ShaderGeneratorUtils.OUTPUT_PATH);

		if(System.IO.Directory.Exists(rootPath))
		{
			string[] paths = System.IO.Directory.GetFiles(rootPath, "*.shader", System.IO.SearchOption.AllDirectories);
			List<Shader> shaderList = new List<Shader>();

			foreach(string path in paths)
			{
#if UNITY_EDITOR_WIN
				string assetPath = "Assets" + path.Replace(@"\", @"/").Replace(Application.dataPath, "");
#else
				string assetPath = "Assets" + path.Replace(Application.dataPath, "");
#endif
				Shader shader = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Shader)) as Shader;
				ShaderImporter shaderImporter = ShaderImporter.GetAtPath(assetPath) as ShaderImporter;
				if(shaderImporter != null && shader != null && !shaderList.Contains(shader))
				{
					if(shaderImporter.userData.Contains("USER"))
					{
						shaderList.Add(shader);
					}
				}
			}

			return shaderList.ToArray();
		}

		return null;
	}

	private string[] GetShaderLabels(Shader[] array, string firstOption = "New Shader")
	{
		if(array == null)
		{
			return new string[0];
		}

		List<string> labelsList = new List<string>();
		if(!string.IsNullOrEmpty(firstOption))
			labelsList.Add(firstOption);
		foreach(Shader shader in array)
		{
			labelsList.Add(shader.name);
		}
		return labelsList.ToArray();
	}

	private void AutoNames()
	{
		string rawName = mCurrentConfig.ShaderName.Replace("Toony Colors Pro 2/", "");
		mCurrentConfig.Filename = rawName;
	}

	private void Space()
	{
		TCP2_GUI.GUILine(new Color(0.65f,0.65f,0.65f), 1);
		GUILayout.Space(1);
	}
}