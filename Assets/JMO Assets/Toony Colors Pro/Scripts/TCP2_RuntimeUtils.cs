// Toony Colors Pro+Mobile 2
// (c) 2014-2016 Jean Moreno

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Play-mode utilities for Toony Colors Pro 2

public static class TCP2_RuntimeUtils
{
	private const string BASE_SHADER_PATH = "Toony Colors Pro 2/";
	private const string VARIANT_SHADER_PATH = "Hidden/Toony Colors Pro 2/Variants/";
	private const string BASE_SHADER_NAME = "Desktop";
	private const string BASE_SHADER_NAME_MOB = "Mobile";

	private static List<string[]> ShaderVariants = new List<string[]>()
	{
		new string[]{ "Specular", "TCP2_SPEC" },
		new string[]{ "Reflection", "TCP2_REFLECTION", "TCP2_REFLECTION_MASKED" },
		new string[]{ "Matcap", "TCP2_MC" },
		new string[]{ "Rim", "TCP2_RIM" },
		new string[]{ "RimOutline", "TCP2_RIMO" },
		new string[]{ "Outline", "OUTLINES" },
		new string[]{ "OutlineBlending", "OUTLINE_BLENDING" },
	};

	// Returns the appropriate shader according to the supplied Material's keywords
	//
	// Note that if the shader wasn't assigned on any material it will not be included in the build
	// You can force shaders to be included in the build in "Edit > Project Settings > Graphics"
	static public Shader GetShaderWithKeywords(Material material)
	{
		bool isMobileShader = material.shader != null && material.shader.name.ToLower().Contains("mobile");
		string baseName = isMobileShader ? BASE_SHADER_NAME_MOB : BASE_SHADER_NAME;
		
		string newShader = baseName;
		foreach(string[] variantKeywords in ShaderVariants)
		{
			foreach(string keyword in material.shaderKeywords)
			{
				for(int i = 1; i < variantKeywords.Length; i++)
				{
					if(keyword == variantKeywords[i])
					{
						newShader += " " + variantKeywords[0];
						continue;
					}
				}
			}
		}
		newShader = newShader.TrimEnd();
		
		//If variant shader
		string basePath = BASE_SHADER_PATH;
		if(newShader != baseName)
		{
			basePath = VARIANT_SHADER_PATH;
		}
		
		Shader shader = Shader.Find(basePath + newShader);
		return shader;
	}
}
