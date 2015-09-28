
Shader "Pixel Art/Fake_Additive" {
Properties {
   _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
   _Color ("Color", Color) = (1,1,1,1)
   _PixelSnap ("Pixel Snap", float) = 1
   _AlphaClipOffset("Alpha Clipping Offset", float) = 0
   _AlphaDither("Alpha Clipping Offset", float) = .3
   _ColorDither("Alpha Clipping Offset", float) = .3
}
 
SubShader {
   Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
   LOD 100
 
   BlendOp Add
   Blend SrcAlpha OneMinusSrcAlpha, One Zero // screen
   //Blend SrcAlpha One, One Zero // linear dodge
   ZWrite Off
   //AlphaTest Greater .01
   Cull Off
   Pass {
     CGPROGRAM
       #pragma vertex vert
       #pragma fragment frag
       #pragma multi_compile DITHER_ON DITHER_OFF 
       #pragma glsl_no_auto_normalization 
       #pragma fragmentoption ARB_precision_hint_fastest       
       #include "UnityCG.cginc"
 
       struct appdata_t {
         float4 vertex : POSITION;
         float2 texcoord : TEXCOORD0;
         fixed4 color: COLOR;
       };
 
       struct v2f {
         float4 vertex : SV_POSITION;
         half2 texcoord : TEXCOORD0;
         fixed4 screenPos: TEXCOORD1;
         fixed4 color: COLOR;
       };
 
       sampler2D _MainTex;
       float4 _MainTex_ST;
       float4 _Color;
       float _PixelSnap;
 	   sampler2D _DitherTex;
	   fixed4 _DitherScale;
	   fixed _AlphaDither;
	   fixed _ColorDither;
	   fixed _AlphaClipOffset;
	          
       v2f vert (appdata_t v)
       {
         v2f o;
         o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
 
 	// Snapping params
		float hpcX = _ScreenParams.x * _PixelSnap;
		float hpcY = _ScreenParams.y * _PixelSnap;
	#ifdef UNITY_HALF_TEXEL_OFFSET
		float hpcOX = -0.5;
		float hpcOY = 0.5;
	#else
		float hpcOX = 0;
		float hpcOY = 0;
	#endif	
		// Snap
		float posi = floor((o.vertex.x / o.vertex.w) * hpcX + 0.5f) + hpcOX;
		o.vertex.x = posi / hpcX * o.vertex.w;

		posi = floor((o.vertex.y / o.vertex.w) * hpcY + 0.5f) + hpcOY;
		o.vertex.y = posi / hpcY * o.vertex.w;
         
         o.screenPos = ComputeScreenPos(o.vertex);
         o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
         o.color = v.color;
         return o;
       }
     
       fixed4 frag (v2f i) : COLOR
       {
       
   		fixed dither = 0;
	#if DITHER_ON
		dither = tex2D(_DitherTex,i.screenPos*_DitherScale).r;// _Color.rgb; 
		dither -= .5;
	#endif
		
         fixed4 col = _Color*tex2D(_MainTex, i.texcoord)*i.color;
         col.a = saturate((col.a+dither*_AlphaDither-(_AlphaClipOffset*.25))*3048*2048);
         col.rgb *= 1.5+(dither*_ColorDither)*4;
         return col;
       }
     ENDCG
   }
}
}