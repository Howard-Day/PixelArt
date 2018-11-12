// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/OutlineShader" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Outline("Outline Thickness", Range(0.0, 4)) = 0.002
		_OutlineColor("Outline Color", Color) = (0,0,0,1)
	}

		CGINCLUDE
#include "UnityCG.cginc"

		sampler2D _MainTex;
	half4 _MainTex_ST;
	float4 _MainTex_TexelSize;
	half _Outline;
	float _Outline2;
	half4 _OutlineColor;
	fixed _PixelSnap;

	struct appdata {
		half4 vertex : POSITION;
		half4 uv : TEXCOORD0;
		half3 normal : NORMAL;
		fixed4 color : COLOR;
	};

	struct v2f {
		half4 pos : POSITION;
		half2 uv : TEXCOORD0;
		fixed4 color : COLOR;
	};
	ENDCG

		SubShader
	{
		Tags{
		"RenderType" = "Opaque"
		"Queue" = "Transparent"
	}

		Pass{
		Name "OUTLINE"
		cull front
		offset 4,4
		CGPROGRAM

#pragma vertex vert
#pragma fragment frag
 	
		v2f vert(appdata v)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		// Snapping params
		float hpcX = _ScreenParams.x * .5;
		float hpcY = _ScreenParams.y * .5;
	#ifdef UNITY_HALF_TEXEL_OFFSET
		float hpcOX = -0.5;
		float hpcOY = 0.5;
	#else
		float hpcOX = 0;
		float hpcOY = 0;
	#endif	
		// Snap
		float posi = floor((o.pos.x / o.pos.w) * hpcX + 0.5f) + hpcOX;
		o.pos.x = posi / hpcX * o.pos.w;

		posi = floor((o.pos.y / o.pos.w) * hpcY + 0.5f) + hpcOY;
		o.pos.y = posi / hpcY * o.pos.w;
		//End Snap	
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);

		half3 norm = normalize(mul((half3x3)UNITY_MATRIX_IT_MV, v.normal));
		half2 offset = (TransformViewToProjection(norm.xy));
		o.pos.xy += offset * o.pos.z* _Outline/12;
		o.color = _OutlineColor;
		return o;
	}

	fixed4 frag(v2f i) : COLOR
	{
		fixed4 o;
		o = o = tex2D(_MainTex, i.uv.xy)*i.color;
		return o;
	}
		ENDCG
	}

		Pass
	{
		Name "TEXTURE"

		Cull Back
		ZWrite On
		ZTest LEqual

		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
fixed4 RetroAA(sampler2D tex, float2 uv, float4 texelSize){
		float2 texelCoord = uv*texelSize.zw;
		float2 hfw = fwidth(texelCoord)*.25;
		float2 fl = floor(texelCoord - 0.5) + 0.5;
		float2 uvaa = (fl + smoothstep(0.5 - hfw, 0.5 + hfw, texelCoord - fl))*texelSize.xy;

		return tex2D(tex, uvaa);
	}


		v2f vert(appdata v)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);	
		// Snapping params
		float hpcX = _ScreenParams.x * .5;
		float hpcY = _ScreenParams.y * .5;
	#ifdef UNITY_HALF_TEXEL_OFFSET
		float hpcOX = -0.5;
		float hpcOY = 0.5;
	#else
		float hpcOX = 0;
		float hpcOY = 0;
	#endif	
		// Snap
		float posi = floor((o.pos.x / o.pos.w) * hpcX + 0.5f) + hpcOX;
		o.pos.x = posi / hpcX * o.pos.w;

		posi = floor((o.pos.y / o.pos.w) * hpcY + 0.5f) + hpcOY;
		o.pos.y = posi / hpcY * o.pos.w;
		//End Snap
		o.uv = TRANSFORM_TEX(v.uv, _MainTex);
		o.color = v.color;
		return o;
	}

	fixed4 frag(v2f i) : COLOR
	{
		fixed4 o;
		o = //RetroAA(_MainTex, i.uv, _MainTex_TexelSize);
		tex2D(_MainTex, i.uv.xy);
	return o;
	}
		ENDCG
	}
	}
}