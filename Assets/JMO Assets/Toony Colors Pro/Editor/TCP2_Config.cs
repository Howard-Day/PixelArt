// Toony Colors Pro+Mobile 2
// (c) 2014-2016 Jean Moreno

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Represents a Toony Colors Pro 2 configuration to generate the corresponding shader

public class TCP2_Config
{
	//--------------------------------------------------------------------------------------------------

	public string Filename = "TCP2 Custom";
	public string ShaderName = "Toony Colors Pro 2/User/My TCP2 Shader";
	public List<string> Features = new List<string>();
	public List<string> Flags = new List<string>();
	public Dictionary<string, string> Keywords = new Dictionary<string, string>();

	//--------------------------------------------------------------------------------------------------

	private enum ParseBlock
	{
		None,
		Features,
		Flags
	}

	static public TCP2_Config CreateFromFile(TextAsset asset)
	{
		return CreateFromFile(asset.text);
	}
	static public TCP2_Config CreateFromFile(string text)
	{
		string[] lines = text.Split(new string[]{"\n","\r\n"}, System.StringSplitOptions.RemoveEmptyEntries);
		TCP2_Config config = new TCP2_Config();

		//Flags
		ParseBlock currentBlock = ParseBlock.None;
		for(int i = 0; i < lines.Length; i++)
		{
			string line = lines[i];
			
			if(line.StartsWith("//")) continue;
			
			string[] data = line.Split(new string[]{"\t"}, System.StringSplitOptions.RemoveEmptyEntries);
			if(line.StartsWith("#"))
			{
				currentBlock = ParseBlock.None;
				
				switch(data[0])
				{
					case "#filename":	config.Filename = data[1]; break;
					case "#shadername":	config.ShaderName = data[1]; break;
					case "#features":	currentBlock = ParseBlock.Features; break;
					case "#flags":		currentBlock = ParseBlock.Flags; break;
					
					default: Debug.LogWarning("[TCP2 Shader Config] Unrecognized tag: " + data[0] + "\nline " + (i+1)); break;
				}
			}
			else
			{
				if(data.Length > 1)
				{
					bool enabled = false;
					bool.TryParse(data[1], out enabled);
					
					if(enabled)
					{
						if(currentBlock == ParseBlock.Features)
							config.Features.Add(data[0]);
						else if(currentBlock == ParseBlock.Flags)
							config.Flags.Add(data[0]);
						else
							Debug.LogWarning("[TCP2 Shader Config] Unrecognized line while parsing : " + line + "\nline " + (i+1));
					}
				}
			}
		}
		
		return config;
	}

	public string ConvertToString()
	{
		System.Text.StringBuilder sb = new System.Text.StringBuilder();

		sb.AppendLine("#filename\t\t\t" + this.Filename);
		sb.AppendLine("#shadername\t\t\t" + this.ShaderName);

		sb.AppendLine();

		sb.AppendLine("#features");
		foreach(string f in this.Features)
		{
			sb.AppendLine(f + "\t\ttrue");
		}

		sb.AppendLine();

		sb.AppendLine("#flags");
		foreach(string f in this.Flags)
		{
			sb.AppendLine(f + "\t\ttrue");
		}

		return sb.ToString();
	}

	public int ToHash()
	{

		System.Text.StringBuilder sb = new System.Text.StringBuilder();
		sb.Append(this.Filename);
		sb.Append(this.ShaderName);
		List<string> orderedFeatures = new List<string>(this.Features);
		orderedFeatures.Sort();
		List<string> orderedFlags = new List<string>(this.Flags);
		orderedFlags.Sort();
		List<string> sortedKeywordsKeys = new List<string>(this.Keywords.Keys);
		sortedKeywordsKeys.Sort();
		List<string> sortedKeywordsValues = new List<string>(this.Keywords.Values);
		sortedKeywordsValues.Sort();

		foreach(string f in orderedFeatures)
			sb.Append(f);
		foreach(string f in orderedFlags)
			sb.Append(f);
		foreach(string f in sortedKeywordsKeys)
			sb.Append(f);
		foreach(string f in sortedKeywordsValues)
			sb.Append(f);

		return sb.ToString().GetHashCode();
	}
}
