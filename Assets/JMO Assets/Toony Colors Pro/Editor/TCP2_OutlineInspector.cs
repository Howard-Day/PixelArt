// Toony Colors Pro+Mobile 2
// (c) 2014-2016 Jean Moreno

//#define SHOW_DEFAULT_INSPECTOR

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

// Custom Inspector when using the Outline Only shaders

public class TCP2_OutlineInspector : MaterialEditor
{
	//Properties
	private Material targetMaterial { get { return this.target as Material; } }
	private Shader mCurrentShader;
	private bool mIsOutlineBlending;
	private bool mShaderModel2;

	//--------------------------------------------------------------------------------------------------

	public override void OnEnable()
	{
		mCurrentShader = targetMaterial.shader;
		UpdateFeaturesFromShader();
		base.OnEnable();
	}

	public override void OnDisable()
	{
		base.OnDisable();
	}

	private void UpdateFeaturesFromShader()
	{
		if(targetMaterial != null && targetMaterial.shader != null)
		{
			string name = targetMaterial.shader.name;
			mIsOutlineBlending = name.ToLowerInvariant().Contains("blended");
			mShaderModel2 = name.ToLowerInvariant().Contains("sm2");
		}
	}

	public override void OnInspectorGUI()
	{
		if(!this.isVisible)
		{
			return;
		}

#if SHOW_DEFAULT_INSPECTOR
		base.OnInspectorGUI();
		return;
#else

		//Detect if Shader has changed
		if(targetMaterial.shader != mCurrentShader)
		{
			mCurrentShader = targetMaterial.shader;
		}

		UpdateFeaturesFromShader();

		//Get material keywords
		List<string> keywordsList = new List<string>(targetMaterial.shaderKeywords);
		bool updateKeywords = false;

		//Header
		TCP2_GUI.HeaderBig("TOONY COLORS PRO 2 - Outlines Only");
		TCP2_GUI.Separator();

		//Iterate Shader properties
		serializedObject.Update();
		SerializedProperty mShader = serializedObject.FindProperty("m_Shader");
		if(isVisible && !mShader.hasMultipleDifferentValues && mShader.objectReferenceValue != null)
		{
			EditorGUIUtility.labelWidth = Screen.width - 120f;
			EditorGUIUtility.fieldWidth = 64f;

			EditorGUI.BeginChangeCheck();

			MaterialProperty[] props = GetMaterialProperties(this.targets);

			//UNFILTERED PARAMETERS ==============================================================
			
			if(ShowFilteredProperties(null, props, false))
			{
				TCP2_GUI.Separator();
			}

			//FILTERED PARAMETERS ================================================================

			//Outline Type ---------------------------------------------------------------------------
			ShowFilteredProperties("#OUTLINE#", props, false);
			if(!mShaderModel2)
			{
				bool texturedOutline = TCP2_Utils.ShaderKeywordToggle("TCP2_OUTLINE_TEXTURED", "Outline Color from Texture", "If enabled, outline will take an averaged color from the main texture multiplied by Outline Color", keywordsList, ref updateKeywords);
				if(texturedOutline)
				{
					ShowFilteredProperties("#OUTLINETEX#", props);
				}
			}

			TCP2_Utils.ShaderKeywordToggle("TCP2_OUTLINE_CONST_SIZE", "Constant Size Outline", "If enabled, outline will have a constant size independently from camera distance", keywordsList, ref updateKeywords);
			if( TCP2_Utils.ShaderKeywordToggle("TCP2_ZSMOOTH_ON", "Correct Z Artefacts", "Enable the outline z-correction to try to hide artefacts from complex models", keywordsList, ref updateKeywords) )
			{
				ShowFilteredProperties("#OUTLINEZ#", props);
			}
			
			//Smoothed Normals -----------------------------------------------------------------------
			TCP2_GUI.Header("OUTLINE NORMALS", "Defines where to take the vertex normals from to draw the outline.\nChange this when using a smoothed mesh to fill the gaps shown in hard-edged meshes.");
			TCP2_Utils.ShaderKeywordRadio(null, new string[]{"TCP2_NONE", "TCP2_COLORS_AS_NORMALS", "TCP2_TANGENT_AS_NORMALS", "TCP2_UV2_AS_NORMALS"}, new GUIContent[]
			{
				new GUIContent("Regular", "Use regular vertex normals"),
				new GUIContent("Vertex Colors", "Use vertex colors as normals (with smoothed mesh)"),
				new GUIContent("Tangents", "Use tangents as normals (with smoothed mesh)"),
				new GUIContent("UV2", "Use second texture coordinates as normals (with smoothed mesh)"),
			},
			keywordsList, ref updateKeywords);

			//Outline Blending -----------------------------------------------------------------------

			if(mIsOutlineBlending)
			{
				MaterialProperty[] blendProps = GetFilteredProperties("#BLEND#", props);

				if(blendProps.Length != 2)
				{
					EditorGUILayout.HelpBox("Couldn't find Blending properties!", MessageType.Error);
				}
				else
				{
					TCP2_GUI.Header("OUTLINE BLENDING", "BLENDING EXAMPLES:\nAlpha Transparency: SrcAlpha / OneMinusSrcAlpha\nMultiply: DstColor / Zero\nAdd: One / One\nSoft Add: OneMinusDstColor / One");

					UnityEngine.Rendering.BlendMode blendSrc = (UnityEngine.Rendering.BlendMode)blendProps[0].floatValue;
					UnityEngine.Rendering.BlendMode blendDst = (UnityEngine.Rendering.BlendMode)blendProps[1].floatValue;

					EditorGUI.BeginChangeCheck();
					float f = EditorGUIUtility.fieldWidth;
					float l = EditorGUIUtility.labelWidth;
					EditorGUIUtility.fieldWidth = 110f;
					EditorGUIUtility.labelWidth -= Mathf.Abs(f - EditorGUIUtility.fieldWidth);
					blendSrc = (UnityEngine.Rendering.BlendMode)EditorGUILayout.EnumPopup("Source Factor", blendSrc);
					blendDst = (UnityEngine.Rendering.BlendMode)EditorGUILayout.EnumPopup("Destination Factor", blendDst);
					EditorGUIUtility.fieldWidth = f;
					EditorGUIUtility.labelWidth = l;
					if(EditorGUI.EndChangeCheck())
					{
						blendProps[0].floatValue = (float)blendSrc;
						blendProps[1].floatValue = (float)blendDst;
					}
				}
			}

			TCP2_GUI.Separator();

			//--------------------------------------------------------------------------------------

			if(EditorGUI.EndChangeCheck())
			{
				PropertiesChanged();
			}
		}

		//Update Keywords
		if(updateKeywords)
		{
			if(targets != null && targets.Length > 0)
			{
				foreach(Object t in targets)
				{
					(t as Material).shaderKeywords = keywordsList.ToArray();
					EditorUtility.SetDirty(t);
				}
			}
			else
			{
				targetMaterial.shaderKeywords = keywordsList.ToArray();
				EditorUtility.SetDirty(targetMaterial);
			}
		}
#endif
	}

	//--------------------------------------------------------------------------------------------------
	// Properties GUI

	private bool ShowFilteredProperties(string filter, MaterialProperty[] properties, bool indent = true)
	{
		if(indent)
			EditorGUI.indentLevel++;

		bool propertiesShown = false;
		foreach(MaterialProperty p in properties)
			propertiesShown |= ShaderMaterialPropertyImpl(p, filter);

		if(indent)
			EditorGUI.indentLevel--;

		return propertiesShown;
	}

	private MaterialProperty[] GetFilteredProperties(string filter, MaterialProperty[] properties, bool indent = true)
	{
		List<MaterialProperty> propList = new List<MaterialProperty>();

		foreach(MaterialProperty p in properties)
		{
			if(p.displayName.Contains(filter))
				propList.Add(p);
		}

		return propList.ToArray();
	}

	private bool ShaderMaterialPropertyImpl(MaterialProperty property, string filter = null)
	{
		//Filter
		string displayName = property.displayName;
		if(filter != null)
		{
			if(!displayName.Contains(filter))
				return false;

			displayName = displayName.Remove(displayName.IndexOf(filter), filter.Length+1);
		}
		else if(displayName.Contains("#"))
		{
			return false;
		}

		//GUI
		switch(property.type)
		{
		case MaterialProperty.PropType.Color:
			ColorProperty(property, displayName);
			break;

		case MaterialProperty.PropType.Float:
			FloatProperty(property, displayName);
			break;

		case MaterialProperty.PropType.Range:
			EditorGUILayout.BeginHorizontal();
			
			//Add float field to Range parameters
#if UNITY_4 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6
			float value = RangeProperty(property, displayName);
			Rect r = GUILayoutUtility.GetLastRect();
			r.x = r.width - 160f;
			r.width = 65f;
			value = EditorGUI.FloatField(r, value);
			if(property.floatValue != value)
			{
				property.floatValue = value;
			}
#else
			RangeProperty(property, displayName);
#endif
			EditorGUILayout.EndHorizontal();
			break;

		case MaterialProperty.PropType.Texture:
			TextureProperty(property, displayName);
			break;

		case MaterialProperty.PropType.Vector:
			VectorProperty(property, displayName);
			break;

		default:
			EditorGUILayout.LabelField("Unknown Material Property Type: " + property.type.ToString());
			break;
		}

		return true;
	}
}
