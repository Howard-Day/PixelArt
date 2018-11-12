// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "RetroAA/UI" {
	Properties {
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}

	SubShader {
		Tags { 
			"Queue" = "Transparent" 
			"IgnoreProjector" = "True" 
			"RenderType" = "Transparent" 
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}
		
		Stencil {
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass {
			Name "Default"
			CGPROGRAM
				#pragma target 3.0
				#pragma vertex vert
				#pragma fragment frag

				#pragma multi_compile __ UNITY_UI_ALPHACLIP
				
				#include "RetroAA.cginc"
				#include "UnityUI.cginc"

				struct VertexInput {
					float4 vertex : POSITION;
					float4 color : COLOR;
					float2 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct FragmentInput {
					float4 vertex : SV_POSITION;
					fixed4 color : COLOR;
					float2 texcoord : TEXCOORD0;
					float4 worldPosition : TEXCOORD1;
					UNITY_VERTEX_OUTPUT_STEREO
				};
				
				fixed4 _Color;
				fixed4 _TextureSampleAdd;
				float4 _ClipRect;

				void vert(in VertexInput IN, out FragmentInput OUT){
					UNITY_SETUP_INSTANCE_ID(IN);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

					OUT.vertex = UnityObjectToClipPos(IN.vertex);
					OUT.color = _Color*IN.color;
					OUT.texcoord = IN.texcoord;
					OUT.worldPosition = IN.vertex;
				}

				sampler2D _MainTex;
				float4 _MainTex_ST;
				float4 _MainTex_TexelSize;

				fixed4 frag(in FragmentInput IN) : SV_Target {
					half4 color = (RetroAA(_MainTex, IN.texcoord, _MainTex_TexelSize) + _TextureSampleAdd)*IN.color;

					color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
					
					#ifdef UNITY_UI_ALPHACLIP
					clip (color.a - 0.001);
					#endif

					return color;
				}
			ENDCG
		}
	}
}
