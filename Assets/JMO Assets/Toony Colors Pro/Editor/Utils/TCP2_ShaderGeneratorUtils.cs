// Toony Colors Pro+Mobile 2
// (c) 2014-2016 Jean Moreno

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

// Helper functions related to shader generation and file saving

public static class TCP2_ShaderGeneratorUtils
{
	private const string TCP2_PATH = "/JMO Assets/Toony Colors Pro/";
	public const string OUTPUT_PATH = TCP2_PATH + "Shaders 2.0 Generated/";
	private const string INCLUDE_REL_PATH = "../Shaders 2.0/Include/";

	//--------------------------------------------------------------------------------------------------

	static public bool SelectGeneratedShader;

	//--------------------------------------------------------------------------------------------------
	// GENERATION
	
	private struct GeneratedShader
	{
		public string filename;
		public string sourceCode;
		public string[] features;
		public bool isUserGenerated;
		public string error;
	}

	private struct ConditionBlock
	{
		public bool done;
	}
	
	static public Shader Compile(TCP2_Config config, string template, bool showProgressBar = true, bool overwritePrompt = true, bool modifiedPrompt = false)
	{
		return Compile(config, template, showProgressBar ? 0f : -1f, overwritePrompt, modifiedPrompt);
	}
	static public Shader Compile(TCP2_Config config, string template, float progress, bool overwritePrompt, bool modifiedPrompt)
	{
		//UI
		if(progress >= 0f)
			EditorUtility.DisplayProgressBar("Hold On", "Generating Shader: " + config.ShaderName, progress);
		
		//Generate source
		string source = config.GenerateShaderSource(template);
		if(string.IsNullOrEmpty(source))
		{
			Debug.LogError("[TCP2 Shader Generator] Can't save Shader: source is null or empty!");
			return null;
		}

		//Save to disk
		Shader shader = SaveShader(config, source, overwritePrompt, modifiedPrompt);
		
		//UI
		if(progress >= 0f)
			EditorUtility.ClearProgressBar();

		return shader;
	}
	
	//Generate the source code for the shader as a string
	static private string GenerateShaderSource(this TCP2_Config config, string template)
	{
		if(config == null)
		{
			string error = "[TCP2 Shader Generator] Config file is null";
			Debug.LogError(error);
			return error;
		}

		if(string.IsNullOrEmpty(template))
		{
			string error = "[TCP2 Shader Generator] Template string is null or empty";
			Debug.LogError(error);
			return error;
		}

		//------------------------------------------------
		// SHADER PARAMTERS

		//Custom Lighting
		bool customLighting = NeedCustomLighting(config) || HasFeatures(config, "CUSTOM_LIGHTING_FORCE");
		if(customLighting && !config.Features.Contains("CUSTOM_LIGHTING"))
			config.Features.Add("CUSTOM_LIGHTING");
		else if(!customLighting && config.Features.Contains("CUSTOM_LIGHTING"))
			config.Features.Remove("CUSTOM_LIGHTING");

		//Custom Ambient
		bool customAmbient = NeedCustomAmbient(config) || HasFeatures(config, "CUSTOM_AMBIENT_FORCE");
		if(customAmbient && !config.Features.Contains("CUSTOM_AMBIENT"))
			config.Features.Add("CUSTOM_AMBIENT");
		else if(!customAmbient && config.Features.Contains("CUSTOM_AMBIENT"))
			config.Features.Remove("CUSTOM_AMBIENT");

		//Specific dependencies
		if(HasFeatures(config, "MATCAP_ADD", "MATCAP_MULT"))
		{
			if(!config.Features.Contains("MATCAP"))
				config.Features.Add("MATCAP");
		}
		else
		{
			if(config.Features.Contains("MATCAP"))
				config.Features.Remove("MATCAP");
		}

		//Masks
		bool mask1 = false, mask2 = false, mask3 = false;
		string mask1features = "";
		string mask2features = "";
		string mask3features = "";
		foreach(KeyValuePair<string,string> kvp in config.Keywords)
		{
			if(kvp.Key == "UV_mask1")	ToggleFlag(config.Features, "UVMASK1", kvp.Value == "Independent UV");
			else if(kvp.Key == "UV_mask2")	ToggleFlag(config.Features, "UVMASK2", kvp.Value == "Independent UV");
			else if(kvp.Key == "UV_mask3")	ToggleFlag(config.Features, "UVMASK3", kvp.Value == "Independent UV");

			else if(kvp.Value == "mask1") { mask1 |= GetMaskDependency(config, kvp.Key); if(mask1) mask1features += GetDisplayNameForMask(kvp.Key) + ","; }
			else if(kvp.Value == "mask2") { mask2 |= GetMaskDependency(config, kvp.Key); if(mask2) mask2features += GetDisplayNameForMask(kvp.Key) + ","; }
			else if(kvp.Value == "mask3") { mask3 |= GetMaskDependency(config, kvp.Key); if(mask3) mask3features += GetDisplayNameForMask(kvp.Key) + ","; }
		}
		mask1features = mask1features.TrimEnd(',');
		mask2features = mask2features.TrimEnd(',');
		mask3features = mask3features.TrimEnd(',');

		ToggleFlag(config.Features, "MASK1", mask1);
		ToggleFlag(config.Features, "MASK2", mask2);
		ToggleFlag(config.Features, "MASK3", mask3);

		//---

		Dictionary<string, string> keywords = new Dictionary<string, string>(config.Keywords);
		List<string> flags = new List<string>(config.Flags);
		List<string> features = new List<string>(config.Features);

		//Masks
		keywords.Add("MASK1", mask1features);
		keywords.Add("MASK2", mask2features);
		keywords.Add("MASK3", mask3features);

		//Shader name
		keywords.Add("SHADER_NAME", config.ShaderName);
		
		//Include path
		string include = GetIncludePrefix(config) + INCLUDE_REL_PATH + GetIncludeFile(config);
		keywords.Add("INCLUDE_PATH", include);
		
		//Lighting Model
		string lightingModel = GetLightingFunction(config);
		keywords.Add("LIGHTING_MODEL", lightingModel);

		//SurfaceOutput struct
		string surfOut = GetSurfaceOutput(config);
		keywords.Add("SURFACE_OUTPUT", surfOut);

		//Shader Model target
		string target = GetShaderTarget(config);
		keywords.Add("SHADER_TARGET", target);

		//Vertex Function
		bool vertexFunction = NeedVertexFunction(config);
		if(vertexFunction)
		{
			TCP2_Utils.AddIfMissing(flags, "vertex:vert");
			features.Add("VERTEX_FUNC");
		}
		
		//Final Colors Function
		bool finalColorFunction = NeedFinalColorFunction(config);
		if(finalColorFunction)
		{
			TCP2_Utils.AddIfMissing(flags, "finalcolor:fcolor");
			features.Add("FINAL_COLOR");
		}
		
		//Alpha Testing (Cutout)
		if(HasFeatures(config, "CUTOUT"))
		{
			TCP2_Utils.AddIfMissing(flags, "alphatest:_Cutoff");
		}

#if UNITY_5
		//Alpha
		if(HasFeatures(config, "ALPHA"))
		{
			TCP2_Utils.AddIfMissing(flags, "keepalpha");
		}
#endif

		//Shadows
		if(HasFeatures(config, "CUTOUT"))
		{
			TCP2_Utils.AddIfMissing(flags, "addshadow");
		}

		//No/Custom Ambient
		if(HasFeatures(config, "CUSTOM_AMBIENT"))
		{
			TCP2_Utils.AddIfMissing(flags, "noambient");
		}
		
		//Generate Surface parameters
		string strFlags = ArrayToString(flags.ToArray(), " ");
		keywords.Add("SURF_PARAMS", strFlags);

		//------------------------------------------------
		// PARSING & GENERATION
		
		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		string[] templateLines = template.Split(new string[]{"\r\n","\n"}, System.StringSplitOptions.None);
		
		int depth = -1;
		List<bool> stack = new List<bool>();
		List<bool> done = new List<bool>();

		//Parse template file
		string line = null;
		for(int i = 0; i < templateLines.Length; i++)
		{
			line = templateLines[i];

			//Comment
			if(line.StartsWith("#"))
			{
				//Debugging
				if(line.StartsWith("#break"))
				{
					Debug.Log("[TCP2] Parse Break @ " + i);
				}

				continue;
			}

			//Line break
			if(string.IsNullOrEmpty(line) && ((depth >= 0 && stack[depth]) || depth < 0))
			{
				sb.AppendLine(line);
				continue;
			}

			//Conditions
			if(line.Contains("///"))
			{
				//Remove leading white spaces
				line = line.TrimStart();
				
				string[] parts = line.Split (new string[]{" "}, System.StringSplitOptions.RemoveEmptyEntries);
				if(parts.Length == 1)	//END TAG
				{
					if(depth < 0)
					{
						string error = "[TCP2 Shader Generator] Found end tag /// without any beginning! Aborting shader generation.\n@ line: " + i;
						Debug.LogError(error);
						return error;
					}

					stack.RemoveAt(depth);
					done.RemoveAt(depth);
					depth--;
				}
				else if(parts.Length >= 2)
				{
					if(parts[1] == "IF")
					{
						bool cond = EvaluateExpression(i, features, parts);

						depth++;
						stack.Add(cond && ((depth <= 0) ? true : stack[depth-1]));
						done.Add(cond);
					}
					else if(parts[1] == "ELIF")
					{
						if(done[depth])
						{
							stack[depth] = false;
							continue;
						}

						bool cond = EvaluateExpression(i, features, parts);

						stack[depth] = cond && ((depth <= 0) ? true : stack[depth-1]);
						done[depth] = cond;
					}
					else if(parts[1] == "ELSE")
					{
						if(done[depth])
						{
							stack[depth] = false;
							continue;
						}
						else
						{
							stack[depth] = ((depth <= 0) ? true : stack[depth-1]);
							done[depth] = true;
						}
					}
				}
			}
			//Regular line
			else
			{
				//Replace keywords
				line = ReplaceKeywords(line, keywords);

				if((depth >= 0 && stack[depth]) || depth < 0)
				{
					sb.AppendLine(line);
				}
			}
		}
		
		if(depth >= 0)
		{
			Debug.LogWarning("[TCP2 Shader Generator] Missing " + (depth+1) + " ending '///' tags");
		}
		
		string sourceCode = sb.ToString();
		return sourceCode;
	}

	static private bool EvaluateExpression(int lineNumber, List<string> features, params string[] conditions)
	{
		if(conditions.Length <= 2)
		{
			Debug.LogWarning("[TCP2 Shader Generator] Invalid condition block\n@ line " + lineNumber);
			return false;
		}

		string firstCondition = conditions[2];
		if(firstCondition.StartsWith("!"))
			firstCondition = firstCondition.Substring(1);

		bool condition = HasFeatures(features, firstCondition);
		if(conditions[2].StartsWith("!"))
			condition = !condition;
		
		int cond = 0;	//0: OR, 1: !OR, 2: AND, 3: !AND
		int i = 3;

		while(i < conditions.Length)
		{
			//And/Or
			if(conditions[i] == "&&")
				cond = 2;
			else if(conditions[i] == "||")
				cond = 0;
			else
				Debug.LogWarning("[TCP2 Shader Generator] Unrecognized condition: " + conditions[i] + "\n@ line " + lineNumber);

			i++;

			if(conditions[i].StartsWith("!"))
			{
				conditions[i] = conditions[i].Substring(1);
				cond++;
			}

			//Condition
			if(cond == 0)
				condition |= HasFeatures(features, conditions[i]);
			else if(cond == 1)
				condition |= !HasFeatures(features, conditions[i]);
			else if(cond == 2)
				condition &= HasFeatures(features, conditions[i]);
			else if(cond == 3)
				condition &= !HasFeatures(features, conditions[i]);

			i++;
		}

		return condition;
	}

	static private bool CheckFeature(string part, List<string> features)
	{
		if(part.Contains("&"))
		{
			string[] andParts = part.Split('&');
			
			bool b = true;
			foreach(string ap in andParts)
				b &= CheckFeature(ap, features);
			return b;
		}
		else if(part.StartsWith("!"))
		{
			return !features.Contains(part.Substring(1));
		}
		else
		{
			return features.Contains(part);
		}
	}
	
	static private string ArrayToString(string[] array, string separator)
	{
		string str = "";
		foreach(string s in array)
		{
			str += s + separator;
		}
		
		if(str.Length > 0)
		{
			str = str.Substring(0, str.Length - separator.Length);
		}
		
		return str;
	}
	
	static private string ReplaceKeywords(string line, Dictionary<string,string> searchAndReplace)
	{
		if(line.IndexOf("@%") < 0)
		{
			return line;
		}
		
		foreach(KeyValuePair<string,string> kv in searchAndReplace)
		{
			line = line.Replace("@%" + kv.Key + "%@", kv.Value);
		}
		
		return line;
	}
	
	//--------------------------------------------------------------------------------------------------
	// IO
	
	//Save .shader file
	static private Shader SaveShader(TCP2_Config config, string sourceCode, bool overwritePrompt, bool modifiedPrompt)
	{
		if(string.IsNullOrEmpty(config.Filename))
		{
			Debug.LogError("[TCP2 Shader Generator] Can't save Shader: filename is null or empty!");
			return null;
		}
		
		//Save file
		string path = Application.dataPath + OUTPUT_PATH;
		if(!System.IO.Directory.Exists(path))
		{
			System.IO.Directory.CreateDirectory(path);
		}
		
		string fullPath = path + config.Filename + ".shader";
		bool overwrite = true;
		if(overwritePrompt && System.IO.File.Exists(fullPath))
		{
			overwrite = EditorUtility.DisplayDialog("TCP2 : Shader Generation", "The following shader already exists:\n\n" + fullPath + "\n\nOverwrite?", "Yes", "No");
		}

		if(modifiedPrompt)
		{
			overwrite = EditorUtility.DisplayDialog("TCP2 : Shader Generation", "The following shader seems to have been modified externally or manually:\n\n" + fullPath + "\n\nOverwrite anyway?", "Yes", "No");
		}
		
		if(overwrite)
		{
			string directory = System.IO.Path.GetDirectoryName(path + config.Filename);
			if(!System.IO.Directory.Exists(directory))
			{
				System.IO.Directory.CreateDirectory(directory);
			}

			//Write file to disk
			System.IO.File.WriteAllText(path + config.Filename + ".shader", sourceCode, System.Text.Encoding.UTF8);
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			
			//Import (to compile shader)
			string assetPath = "Assets" + OUTPUT_PATH + config.Filename + ".shader";

			Shader shader = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Shader)) as Shader;
			if(SelectGeneratedShader)
			{
				Selection.objects = new Object[]{ shader };
			}
			
			//Set ShaderImporter userData
			ShaderImporter shaderImporter = ShaderImporter.GetAtPath(assetPath) as ShaderImporter;
			if(shaderImporter != null)
			{
				//Get file hash to verify if it has been manually altered afterwards
				string shaderHash = GetShaderContentHash(shaderImporter);

				//Use hash if available, else use timestamp
				string[] customData = new string[]{ !string.IsNullOrEmpty(shaderHash) ? shaderHash : shaderImporter.assetTimeStamp.ToString() };

				string userData = config.ToUserData( customData );
				shaderImporter.userData = userData;

				//Needed to save userData in .meta file
				AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.Default);
			}
			else
			{
				Debug.LogWarning("[TCP2 Shader Generator] Couldn't find ShaderImporter.\nMetadatas will be missing from the shader file.");
			}

			return shader;
		}

		return null;
	}

	//Returns hash of file content to check for manual modifications (with 'h' prefix)
	static public string GetShaderContentHash(ShaderImporter importer)
	{
		string shaderHash = null;
		string shaderFilePath = Application.dataPath.Replace("Assets", "") + importer.assetPath;
		if(System.IO.File.Exists( shaderFilePath ))
		{
			string shaderContent = System.IO.File.ReadAllText( shaderFilePath );
			shaderHash = (shaderContent != null) ? string.Format("h{0}", shaderContent.GetHashCode().ToString("X")) : "";
		}

		return shaderHash;
	}
	
	//--------------------------------------------------------------------------------------------------
	// UTILS
	
	static public bool HasFeatures(TCP2_Config config, params string[] features)
	{
		return HasFeatures(config, true, features);
	}
	static public bool HasFeatures(TCP2_Config config, bool anyFeature, params string[] features)
	{
		return HasFeatures(config.Features, anyFeature, features);
	}
	static public bool HasFeatures(List<string> configFeature, params string[] features)
	{
		return HasFeatures(configFeature, true, features);
	}
	static public bool HasFeatures(List<string> configFeature, bool anyFeature, params string[] features)
	{
		bool hasAllFeatures = true;
		foreach(string f in features)
		{
			if(configFeature.Contains(f))
			{
				if(anyFeature)
					return true;
				else
					hasAllFeatures &= configFeature.Contains(f);
			}
		}
		return anyFeature ? false : hasAllFeatures;
	}

	static public void ToggleSingleFeature(List<string> featuresList, string feature, int value)
	{
		ToggleSingleFeature(featuresList, feature, value > 0);
	}
	static public void ToggleSingleFeature(List<string> featuresList, string feature, bool enable)
	{
		if(enable && !featuresList.Contains(feature))
		{
			featuresList.Add(feature);
		}
		else if(!enable && featuresList.Contains(feature))
		{
			featuresList.Remove(feature);
		}
	}

	static public void ToggleFlag(List<string> flagsList, string flag, bool enable)
	{
		if(enable && !flagsList.Contains(flag))
		{
			flagsList.Add(flag);
		}
		else if(!enable && flagsList.Contains(flag))
		{
			flagsList.Remove(flag);
		}
	}

	static public void ToggleMultipleFeatures(List<string> featuresList, int value, params string[] features)
	{
		ToggleMultipleFeatures(featuresList, value, true, features);
	}
	static public void ToggleMultipleFeatures(List<string> featuresList, int value, bool firstIsVoid, params string[] features)
	{
		if(value < 0 || value >= features.Length)
		{
			Debug.LogWarning("[TCP2 Shader Generator] Invalid value for supplied params. Clamping.");
			value = Mathf.Clamp(value, 0, features.Length-1);
		}
		
		for(int i = 0; i < features.Length; i++)
		{
			if(firstIsVoid && i == 0)
				continue;
			
			bool enable = (i == value);
			
			if(enable && !featuresList.Contains(features[i]))
			{
				featuresList.Add(features[i]);
			}
			else if(!enable && featuresList.Contains(features[i]))
			{
				featuresList.Remove(features[i]);
			}
		}
	}

	static public string GetKeyword(TCP2_Config config, string key)
	{
		return GetKeyword(config.Keywords, key);
	}
	static public string GetKeyword(Dictionary<string,string> keywordsDict, string key)
	{
		if(key == null)
			return null;

		if(!keywordsDict.ContainsKey(key))
			return null;

		return keywordsDict[key];
	}

	static public void SetKeyword(Dictionary<string,string> keywordsDict, string key, string value)
	{
		if(string.IsNullOrEmpty(value))
		{
			if(keywordsDict.ContainsKey(key))
				keywordsDict.Remove(key);
		}
		else
		{
			if(keywordsDict.ContainsKey(key))
				keywordsDict[key] = value;
			else
				keywordsDict.Add(key, value);
		}
	}

	static private string GetDisplayNameForMask(string maskType)
	{
		switch(maskType)
		{
		case "SPEC_MASK": return "Specular";
		case "REFL_MASK": return "Reflection";
		case "MASK_MC": return "MatCap";
		case "SPEC_SHIN_MASK": return "Shininess";
		case "DETAIL_MASK": return "Detail";
		case "RIM_MASK": return "Rim";
		case "ILLUMIN_MASK": return "Self-Illumination";
		case "COLORMASK": return "Color";
		default : Debug.LogWarning("[TCP2 Shader Generator] Unknown mask: " + maskType); return "";
		}
	}

	static private bool GetMaskDependency(TCP2_Config config, string maskType)
	{
		switch(maskType)
		{
		case "SPEC_MASK": return HasFeatures(config, "SPECULAR", "SPECULAR_ANISOTROPIC");
		case "REFL_MASK": return HasFeatures(config, "REFLECTION");
		case "MASK_MC": return HasFeatures(config, "MATCAP");
		case "SPEC_SHIN_MASK": return HasFeatures(config, "SPECULAR", "SPECULAR_ANISOTROPIC");
		case "DETAIL_MASK": return HasFeatures(config, "DETAIL_TEX");
		case "RIM_MASK": return HasFeatures(config, "RIM","RIM_OUTLINE");
		case "ILLUMIN_MASK": return HasFeatures(config, "ILLUMINATION");
		case "COLORMASK": return true;
		}
		return false;
	}

	//-------------------------------------------------
	
	//Convert Config to ShaderImporter UserData
	static public string ToUserData(this TCP2_Config config, string[] customData)
	{
		string userData = "";
		if(!config.Features.Contains("USER"))
			userData = "USER,";

		foreach(string feature in config.Features)
			if(feature.Contains("USER"))
				userData += string.Format("{0},", feature);
			else
				userData += string.Format("F{0},", feature);
		foreach(string flag in config.Flags)
			userData += string.Format("f{0},", flag);
		foreach(KeyValuePair<string,string> kvp in config.Keywords)
			userData += string.Format("K{0}:{1},", kvp.Key, kvp.Value);
		foreach(string custom in customData)
			userData += string.Format("c{0},", custom);
		userData = userData.TrimEnd(',');

		return userData;
	}

	//Get Features array from ShaderImporter
	static public void ParseUserData(ShaderImporter importer, out List<string> Features)
	{
		string[] array;
		string[] dummy;
		Dictionary<string,string> dummyDict;
		ParseUserData(importer, out array, out dummy, out dummyDict, out dummy);
		Features = new List<string>(array);
	}
	static public void ParseUserData(ShaderImporter importer, out string[] Features, out string[] Flags, out Dictionary<string,string> Keywords, out string[] CustomData)
	{
		List<string> featuresList = new List<string>();
		List<string> flagsList = new List<string>();
		List<string> customDataList = new List<string>();
		Dictionary<string,string> keywordsDict = new Dictionary<string,string>();

		string[] data = importer.userData.Split(new string[]{","}, System.StringSplitOptions.RemoveEmptyEntries);
		foreach(string d in data)
		{
			if(string.IsNullOrEmpty(d)) continue;

			switch(d[0])
			{
			//Features
			case 'F': featuresList.Add(d.Substring(1)); break;
			//Flags
			case 'f': flagsList.Add(d.Substring(1)); break;
			//Keywords
			case 'K':
				string[] kw = d.Substring(1).Split(':');
				if(kw.Length != 2)
				{
					Debug.LogError("[TCP2 Shader Generator] Error while parsing userData: invalid Keywords format.");
					Features = null; Flags = null; Keywords = null; CustomData = null;
					return;
				}
				else
				{
					keywordsDict.Add(kw[0], kw[1]);
				}
				break;
			//Custom Data
			case 'c': customDataList.Add(d.Substring(1)); break;
			//old format
			default: featuresList.Add(d); break;
			}
		}

		Features = featuresList.ToArray();
		Flags = flagsList.ToArray();
		Keywords = keywordsDict;
		CustomData = customDataList.ToArray();
	}
	static public string[] GetUserDataFeatures(ShaderImporter importer)
	{
		//Contains Features & Flags
		if(importer.userData.Contains("|"))
		{
			string[] data = importer.userData.Split('|');
			if(data.Length < 2)
			{
				Debug.LogError("[TCP2 Shader Generator] Invalid userData in ShaderImporter.\n" + importer.userData);
				return null;
			}
			else
			{
				string[] features = data[0].Split(new string[]{","}, System.StringSplitOptions.RemoveEmptyEntries);
				return features;
			}
		}
		//No Flags data
		else
		{
			return importer.userData.Split(',');
		}
	}

	//Get Flags array from ShaderImporter
	static public string[] GetUserDataFlags(ShaderImporter importer)
	{
		//Contains Flags
		if(importer.userData.Contains("|"))
		{
			string[] data = importer.userData.Split('|');
			if(data.Length < 2)
			{
				Debug.LogError("[TCP2 Shader Generator] Invalid userData in ShaderImporter.\n" + importer.userData);
				return null;
			}
			else
			{
				string[] flags = data[1].Split(new string[]{","}, System.StringSplitOptions.RemoveEmptyEntries);
				return flags;
			}
		}
		//No Flags data
		else
		{
			return null;
		}
	}

	//--------------------------------------------------------------------------------------------------
	// PRIVATE - SHADER GENERATION

	static private string GetIncludePrefix(TCP2_Config config)
	{
		//Folder
		if(!config.Filename.Contains("/"))
			return "";

		string prefix = "";
		foreach(char c in config.Filename) if(c == '/') prefix += "../";
		return prefix;
	}

	static private string GetIncludeFile(TCP2_Config config)
	{
		return "TCP2_Include.cginc";
	}

	static private string GetLightingFunction(TCP2_Config config)
	{
		bool customLighting = HasFeatures(config, "CUSTOM_LIGHTING");
		if(customLighting)
			return "ToonyColorsCustom";

		bool specular = HasFeatures(config, "SPECULAR", "SPECULAR_ANISOTROPIC");
		
		if(specular)
			return "ToonyColorsSpec";
		else
			return "ToonyColors";
	}
	
	static private string GetSurfaceOutput(TCP2_Config config)
	{
		return "SurfaceOutput";
	}

	static private string GetShaderTarget(TCP2_Config config)
	{
		bool tessellate = HasFeatures(config, "DX11_TESSELLATION");
		bool forcesm2 = HasFeatures(config, "FORCE_SM2");

		if(forcesm2)
			return "2.0";
		else if(tessellate)
			return "5.0";
		else
			return "3.0";
	}
	
	static private bool NeedVertexFunction(TCP2_Config config)
	{
		bool vFunc = HasFeatures(config, "VERTEX_FUNC");
		bool anisotropic = HasFeatures(config, "SPECULAR_ANISOTROPIC");
		bool matcap = HasFeatures(config, "MATCAP");
		bool sketch = HasFeatures(config, "SKETCH", "SKETCH_GRADIENT");
		bool rimdir = HasFeatures(config, "RIMDIR");
		bool rim = HasFeatures(config, "RIM", "RIM_OUTLINE");
		bool rimvertex = HasFeatures(config, "RIM_VERTEX");
		bool bump = HasFeatures(config, "BUMP");
		bool cstamb = HasFeatures(config, "CUSTOM_AMBIENT");
		
		return vFunc || matcap || sketch || (rimvertex && rim) || (bump && rimdir && rim) || anisotropic || cstamb;
	}
	
	static private bool NeedFinalColorFunction(TCP2_Config config)
	{
		return false;
	}

	static private bool NeedCustomLighting(TCP2_Config config)
	{
		bool specmask = HasFeatures(config, "SPECULAR_MASK");
		bool anisotropic = HasFeatures(config, "SPECULAR_ANISOTROPIC");
		bool texthreshold = HasFeatures(config, "TEXTURED_THRESHOLD");
		bool occlusion = HasFeatures(config, "OCCLUSION");
		bool indshadows = HasFeatures(config, "INDEPENDENT_SHADOWS");
		bool sketch = HasFeatures(config, "SKETCH", "SKETCH_GRADIENT");
		bool lightmap = HasFeatures(config, "LIGHTMAP");

		return specmask || anisotropic || occlusion || texthreshold || indshadows || sketch || lightmap;
	}

	static private bool NeedCustomAmbient(TCP2_Config config)
	{
		bool occlusion = HasFeatures(config, "OCCLUSION");
		bool cubeambient = HasFeatures(config, "CUBE_AMBIENT");
		bool dirambient = HasFeatures(config, "DIRAMBIENT");
		
		return cubeambient || occlusion || dirambient;
	}
}