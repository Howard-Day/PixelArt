// Toony Colors Pro+Mobile 2
// (c) 2014-2016 Jean Moreno

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// General helper functions for TCP2

public static class TCP2_Utils
{
	//--------------------------------------------------------------------------------------------------------------------------------

	public enum TextureChannel
	{
		Alpha, Red, Green, Blue
	}

	static public string ToShader (this TextureChannel channel)
	{
		switch(channel)
		{
			case TextureChannel.Alpha: return ".a";
			case TextureChannel.Red: return ".r";
			case TextureChannel.Green: return ".g";
			case TextureChannel.Blue: return ".b";
			default: Debug.LogError("[TCP2_Utils] Unrecognized texture channel: " + channel.ToShader()); return null;
		}
	}

	static public TextureChannel FromShader(string str)
	{
		if(string.IsNullOrEmpty(str))
			return TextureChannel.Alpha;

		switch(str)
		{
			case ".a": return TextureChannel.Alpha;
			case ".r": return TextureChannel.Red;
			case ".g": return TextureChannel.Green;
			case ".b": return TextureChannel.Blue;
			default: Debug.LogError("[TCP2_Utils] Unrecognized texture channel from shader: " + str + "\nDefaulting to Alpha"); return TextureChannel.Alpha;
		}
	}

	//--------------------------------------------------------------------------------------------------------------------------------
	// CUSTOM INSPECTOR UTILS

	static public bool HasKeywords(List<string> list, params string[] keywords)
	{
		bool v = false;
		foreach(string kw in keywords)
			v |= list.Contains(kw);
		
		return v;
	}
	
	static public bool ShaderKeywordToggle(string keyword, string label, string tooltip, List<string> list, ref bool update, string helpTopic = null)
	{
		float w = EditorGUIUtility.labelWidth;
		if(!string.IsNullOrEmpty(helpTopic))
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUIUtility.labelWidth = w - 16;
			TCP2_GUI.HelpButton(helpTopic);
		}

		bool boolValue = list.Contains(keyword);
		EditorGUI.BeginChangeCheck();
		boolValue = EditorGUILayout.ToggleLeft(new GUIContent(label, tooltip), boolValue, boolValue ? EditorStyles.boldLabel : EditorStyles.label);
		if(EditorGUI.EndChangeCheck())
		{
			if(boolValue)
				list.Add(keyword);
			else
				list.Remove(keyword);
			
			update = true;
		}

		if(!string.IsNullOrEmpty(helpTopic))
		{
			EditorGUIUtility.labelWidth = w;
			EditorGUILayout.EndHorizontal();
		}
		
		return boolValue;
	}
	
	static public bool ShaderKeywordRadio(string header, string[] keywords, GUIContent[] labels, List<string> list, ref bool update)
	{
		int index = 0;
		for(int i = 1; i < keywords.Length; i++)
		{
			if(list.Contains(keywords[i]))
			{
				index = i;
				break;
			}
		}
		
		EditorGUI.BeginChangeCheck();
		
		//Header and rect calculations
		bool hasHeader = (!string.IsNullOrEmpty(header));
		Rect headerRect = GUILayoutUtility.GetRect(120f, 16f, GUILayout.ExpandWidth(false));
		Rect r = headerRect;
		if(hasHeader)
		{
			Rect helpRect = headerRect;
			helpRect.width = 16;
			headerRect.width -= 16;
			headerRect.x += 16;
			string helpTopic = header.ToLowerInvariant();
			helpTopic = char.ToUpperInvariant(helpTopic[0]) + helpTopic.Substring(1);
			TCP2_GUI.HelpButton(helpRect, helpTopic);
			GUI.Label(headerRect, header, index > 0 ? EditorStyles.boldLabel : EditorStyles.label);
			r.width = Screen.width - headerRect.width - 34f;
			r.x += headerRect.width;
		}
		else
		{
			r.width = Screen.width - 34f;
		}
		
		for(int i = 0; i < keywords.Length; i++)
		{
			Rect rI = r;
			rI.width /= keywords.Length;
			rI.x += i * rI.width;
			if(GUI.Toggle(rI, index == i,labels[i], (i == 0) ? EditorStyles.miniButtonLeft : (i == keywords.Length-1) ? EditorStyles.miniButtonRight : EditorStyles.miniButtonMid))
			{
				index = i;
			}
		}
		
		if(EditorGUI.EndChangeCheck())
		{
			//Remove all other keywords and add selected
			for(int i = 0; i < keywords.Length; i++)
			{
				if(list.Contains(keywords[i]))
					list.Remove(keywords[i]);
			}
			
			if(index > 0)
			{
				list.Add(keywords[index]);
			}
			
			update = true;
		}
		
		return (index > 0);
	}

	// Enable/Disable a feature on the shader and mark it for update
	static public void ShaderVariantUpdate(string feature, List<string> featuresList, List<bool> featuresEnabled, bool enable, ref bool update)
	{
		int featureIndex = featuresList.IndexOf(feature);
		if(featureIndex < 0)
		{
			EditorGUILayout.HelpBox("Couldn't find shader feature in list: " + feature, MessageType.Error);
			return;
		}
		
		if(featuresEnabled[featureIndex] != enable)
		{
			featuresEnabled[featureIndex] = enable;
			update = true;
		}
	}

	static public void AddIfMissing(List<string> list, string item)
	{
		if(!list.Contains(item))
			list.Add(item);
	}

	//--------------------------------------------------------------------------------------------------------------------------------
	
	static public string FindReadmePath(bool relativeToAssets = false)
	{
		string[] paths = System.IO.Directory.GetFiles(Application.dataPath, "!ToonyColorsPro Readme.txt", System.IO.SearchOption.AllDirectories);
		if(paths.Length > 0)
		{
			string firstPath = System.IO.Path.GetDirectoryName( paths[0] );
			firstPath = UnityToSystemPath(firstPath);
			if(relativeToAssets)
			{
#if UNITY_EDITOR_WIN
				firstPath = firstPath.Replace(UnityToSystemPath(Application.dataPath), "").Replace(@"\", "/");
#else
				firstPath = firstPath.Replace(UnityToSystemPath(Application.dataPath), "");
#endif
			}
			return firstPath;
		}
		
		return null;
	}

	//--------------------------------------------------------------------------------------------------------------------------------

	static public Mesh CreateSmoothedMesh(Mesh originalMesh, string format, bool vcolors, bool vtangents, bool uv2, bool overwriteMesh)
	{
		if(originalMesh == null)
		{
			Debug.LogWarning("[TCP2 : Smoothed Mesh] Supplied OriginalMesh is null!\nCan't create smooth normals version.");
			return null;
		}

		//Create new mesh
		Mesh newMesh = overwriteMesh ? originalMesh : new Mesh();
		if(!overwriteMesh)
		{
//			EditorUtility.CopySerialized(originalMesh, newMesh);
			newMesh.vertices = originalMesh.vertices;
			newMesh.normals = originalMesh.normals;
			newMesh.tangents = originalMesh.tangents;
			newMesh.uv = originalMesh.uv;
			newMesh.uv2 = originalMesh.uv2;
#if UNITY_5
			newMesh.uv3 = originalMesh.uv3;
			newMesh.uv4 = originalMesh.uv4;
#else
			newMesh.uv2 = originalMesh.uv2;
#endif
			newMesh.colors32 = originalMesh.colors32;
			newMesh.triangles = originalMesh.triangles;
			newMesh.bindposes = originalMesh.bindposes;
			newMesh.boneWeights = originalMesh.boneWeights;

#if !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2
			//Only available from Unity 5.3 onward
			if(originalMesh.blendShapeCount > 0)
				CopyBlendShapes(originalMesh, newMesh);
#endif

			newMesh.subMeshCount = originalMesh.subMeshCount;
			if(newMesh.subMeshCount > 1)
				for(int i = 0; i < newMesh.subMeshCount; i++)
					newMesh.SetTriangles(originalMesh.GetTriangles(i), i);
		}

		//--------------------------------
		// Format

		Vector3 chSign = Vector3.one;
		if(string.IsNullOrEmpty(format)) format = "xyz";
		format = format.ToLowerInvariant();
		int[] channels = new int[]{0,1,2};
		bool skipFormat = (format == "xyz");
		int charIndex = 0;
		int ch = 0;
		while(charIndex < format.Length)
		{
			switch(format[charIndex])
			{
				case '-': chSign[ch] = -1; break;
				case 'x': channels[ch] = 0; ch++; break;
				case 'y': channels[ch] = 1; ch++; break;
				case 'z': channels[ch] = 2; ch++; break;
				default: break;
			}
			if(ch > 2) break;
			charIndex++;
		}

		//--------------------------------
		//Calculate smoothed normals
		
		//Iterate, find same-position vertices and calculate averaged values as we go
		Dictionary<Vector3, Vector3> averageNormalsHash = new Dictionary<Vector3, Vector3>();
		for(int i = 0; i < newMesh.vertexCount; i++)
		{
			if(!averageNormalsHash.ContainsKey(newMesh.vertices[i]))
				averageNormalsHash.Add(newMesh.vertices[i], newMesh.normals[i]);
			else
				averageNormalsHash[newMesh.vertices[i]] = (averageNormalsHash[newMesh.vertices[i]] + newMesh.normals[i]).normalized;
		}
		
		//Convert to Array
		Vector3[] averageNormals = new Vector3[newMesh.vertexCount];
		for(int i = 0; i < newMesh.vertexCount; i++)
		{
			averageNormals[i] = averageNormalsHash[newMesh.vertices[i]];
			if(!skipFormat)
				averageNormals[i] = Vector3.Scale(new Vector3(averageNormals[i][channels[0]], averageNormals[i][channels[1]], averageNormals[i][channels[2]]), chSign);
		}
		
	#if DONT_ALTER_NORMALS
		//Debug: don't alter normals to see if converting into colors/tangents/uv2 works correctly
		for(int i = 0; i < newMesh.vertexCount; i++)
		{
			averageNormals[i] = newMesh.normals[i];
		}
	#endif

		//--------------------------------
		// Store in Vertex Colors

		if(vcolors)
		{
			//Assign averaged normals to colors
			Color32[] colors = new Color32[newMesh.vertexCount];
			for(int i = 0; i < newMesh.vertexCount; i++)
			{
				byte r = (byte)(((averageNormals[i].x * 0.5f) + 0.5f)*255);
				byte g = (byte)(((averageNormals[i].y * 0.5f) + 0.5f)*255);
				byte b = (byte)(((averageNormals[i].z * 0.5f) + 0.5f)*255);
				
				colors[i] = new Color32(r,g,b,255);
			}
			newMesh.colors32 = colors;
		}

		//--------------------------------
		// Store in Tangents

		if(vtangents)
		{
			//Assign averaged normals to tangent
			Vector4[] tangents = new Vector4[newMesh.vertexCount];
			for(int i = 0; i < newMesh.vertexCount; i++)
			{
				tangents[i] = new Vector4(averageNormals[i].x, averageNormals[i].y, averageNormals[i].z, 0f);
			}
			newMesh.tangents = tangents;
		}

		//--------------------------------
		// Store in UV2

		if(uv2)
		{
			//Assign averaged normals to UV2 (x,y to uv2.x and z to uv2.y)
			Vector2[] uvs2 = new Vector2[newMesh.vertexCount];
			for(int i = 0; i < newMesh.vertexCount; i++)
			{
				float x = averageNormals[i].x * 0.5f + 0.5f;
				float y = averageNormals[i].y * 0.5f + 0.5f;
				float z = averageNormals[i].z * 0.5f + 0.5f;
				
				//pack x,y to uv2.x
				x = Mathf.Round(x*15);
				y = Mathf.Round(y*15);
				float packed = Vector2.Dot(new Vector2(x,y), new Vector2((float)(1.0/(255.0/16.0)), (float)(1.0/255.0)));
				
				//store to UV2
				uvs2[i].x = packed;
				uvs2[i].y = z;
			}
			newMesh.uv2 = uvs2;
		}

		return newMesh;
	}

#if !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2
	//Only available from Unity 5.3 onward
	static private void CopyBlendShapes(Mesh originalMesh, Mesh newMesh)
	{
		for(int i = 0; i < originalMesh.blendShapeCount; i++)
		{
			string shapeName = originalMesh.GetBlendShapeName(i);
			int frameCount = originalMesh.GetBlendShapeFrameCount(i);
			for(int j = 0; j < frameCount; j++)
			{
				Vector3[] dv = new Vector3[originalMesh.vertexCount];
				Vector3[] dn = new Vector3[originalMesh.vertexCount];
				Vector3[] dt = new Vector3[originalMesh.vertexCount];

				float frameWeight = originalMesh.GetBlendShapeFrameWeight(i, j);
				originalMesh.GetBlendShapeFrameVertices(i, j, dv, dn, dt);
				newMesh.AddBlendShapeFrame(shapeName, frameWeight, dv, dn, dt);
			}
		}
	}
#endif

	//--------------------------------------------------------------------------------------------------------------------------------
	// SHADER PACKING/UNPACKING

	public class PackedFile
	{
		public PackedFile(string _path, string _content)
		{
			this.mPath = _path;
			this.content = _content;
		}
		
		private string mPath;
		public string path
		{
			get
			{
#if UNITY_EDITOR_WIN
				return this.mPath;
#else
				return this.mPath.Replace(@"\","/");
#endif
			}
		}
		public string content { get; private set; }
	}

	//Get a PackedFile from a system file path
	static public PackedFile PackFile(string windowsPath)
	{
		if(!File.Exists(windowsPath))
		{
			EditorApplication.Beep();
			Debug.LogError("[TCP2 PackFile] File doesn't exist:" + windowsPath);
			return null;
		}
		
		//Get properties
		// Content
		string content = File.ReadAllText(windowsPath, System.Text.Encoding.UTF8);
		// File relative path
		string tcpRoot = TCP2_Utils.FindReadmePath();
		if(tcpRoot == null)
		{
			EditorApplication.Beep();
			Debug.LogError("[TCP2 PackFile] Can't find TCP2 Readme file!\nCan't determine root folder to pack/unpack files.");
			return null;
		}
		tcpRoot = UnityToSystemPath(tcpRoot);
		string relativePath = windowsPath.Replace(tcpRoot, "");
		
		PackedFile pf = new PackedFile(relativePath, content);
		return pf;
	}
	
	//Create an archive of PackedFile
	static public void CreateArchive(PackedFile[] packedFiles, string outputFile)
	{
		if(packedFiles == null || packedFiles.Length == 0)
		{
			EditorApplication.Beep();
			Debug.LogError("[TCP2 PackFile] No file to pack!");
			return;
		}
		
		System.Text.StringBuilder sbIndex = new System.Text.StringBuilder();
		System.Text.StringBuilder sbContent = new System.Text.StringBuilder();
		
		sbIndex.AppendLine("# TCP2 PACKED SHADERS");
		int cursor = 0;
		foreach(PackedFile pf in packedFiles)
		{
			sbContent.Append(pf.content);
			sbIndex.AppendLine( pf.path + ";" + cursor.ToString() + ";" + pf.content.Length );	// PATH ; START ; LENGTH
			cursor += pf.content.Length;
		}
		
		string archiveContent = sbIndex.ToString() + "###\n" + sbContent.ToString();
		
		string fullPath = Application.dataPath + "/" + outputFile;
		string directory = Path.GetDirectoryName(fullPath);
		if(!Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}
		File.WriteAllText(fullPath, archiveContent);
		AssetDatabase.Refresh();
		Debug.Log("[TCP2 CreateArchive] Created archive:\n" + fullPath);
	}
	
	//Extract an archive into an array of PackedFile
	static public PackedFile[] ExtractArchive(string archivePath, string filter = null)
	{
		string archive = File.ReadAllText(archivePath);
		string[] archiveLines = File.ReadAllLines(archivePath);
		
		if(archiveLines[0] != "# TCP2 PACKED SHADERS")
		{
			EditorApplication.Beep();
			Debug.LogError("[TCP2 ExtractArchive] Invalid TCP2 archive:\n" + archivePath);
			return null;
		}
		
		//Find offset
		int offset = archive.IndexOf("###") + 4;
		if(offset < 20)
		{
			Debug.LogError("[TCP2 ExtractArchive] Invalid TCP2 archive:\n" + archivePath);
			return null;
		}
		
		string tcpRoot = TCP2_Utils.FindReadmePath();
		List<PackedFile> packedFilesList = new List<PackedFile>();
		for(int line = 1; line < archiveLines.Length; line++)
		{
			//Index end, start content parsing
			if(archiveLines[line].StartsWith("#"))
			{
				break;
			}
			
			string[] shaderIndex = archiveLines[line].Split(new string[]{";"}, System.StringSplitOptions.RemoveEmptyEntries);
			if(shaderIndex.Length != 3)
			{
				EditorApplication.Beep();
				Debug.LogError("[TCP2 ExtractArchive] Invalid format in TCP2 archive, at line " + line + ":\n" + archivePath);
				return null;
			}
			
			//Get data
			string relativePath = shaderIndex[0];
			int start = int.Parse(shaderIndex[1]);
			int length = int.Parse(shaderIndex[2]);
			//Get content
			string content = archive.Substring(offset + start, length);
			
			//Skip if file already extracted
			if(File.Exists( tcpRoot + relativePath ))
			{
				continue;
			}
			
			//Filter?
			if(!string.IsNullOrEmpty(filter))
			{
				string[] filters = filter.Split(new string[]{" "}, System.StringSplitOptions.RemoveEmptyEntries);
				bool skip = false;
				foreach(string f in filters)
				{
					if(!relativePath.ToLower().Contains(f.ToLower()))
					{
						skip = true;
						break;
					}
				}
				if(skip)
					continue;
			}
			
			//Add File
			packedFilesList.Add( new PackedFile(relativePath, content) );
		}
		
		return packedFilesList.ToArray();
	}
	
	static public string UnityRelativeToSystemPath(string path)
	{
		string sysPath = path;
#if UNITY_EDITOR_WIN
		sysPath = path.Replace("/", @"\");
#endif
		string appPath = UnityToSystemPath(Application.dataPath);
		appPath = appPath.Substring(0, appPath.Length - 6); // Remove 'Assets'
		sysPath = appPath + sysPath;
		return sysPath;
	}
	static public string UnityToSystemPath(string path)
	{
#if UNITY_EDITOR_WIN
		return path.Replace("/", @"\");
#else
		return path;
#endif
	}
}
