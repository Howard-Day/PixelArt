// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Pixel Art/ScreenBlendZOverlay" {
Properties {
   _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
   _Color ("Color", Color) = (1,1,1,1)
   _ColorDither("Dither Amount", float) = .1
}
 
SubShader {
   Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
   LOD 100
 
	//Blend One Zero, Zero One
   Blend OneMinusDstColor One, One Zero
   //BlendOp Min//ReverseSubtract
   //Blend SrcAlpha One, One Zero // linear dodge
   ZWrite off
   //AlphaTest Greater .01
   ZTest Greater
   Offset 2000,2000
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
       sampler2D _DitherTex;
	   fixed4 _DitherScale;
	   fixed _ColorDither;
	   
       v2f vert (appdata_t v)
       {
         v2f o;
         o.vertex = UnityObjectToClipPos(v.vertex);
         o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
         o.screenPos = ComputeScreenPos(o.vertex);
         o.color = v.color*2;
         return o;
       }
     
       fixed4 frag (v2f i) : COLOR
       {
         fixed4 col = _Color*tex2D(_MainTex, i.texcoord)*i.color;

 		 fixed dither = 0;
		 #if DITHER_ON
			dither = tex2D(_DitherTex,i.screenPos*_DitherScale).r;// _Color.rgb; 
			dither -= .5;
			dither *= .05;
		 #endif 
         col.rgb *= i.color.a*2;
         col.rgb += dither*_ColorDither;
         return col;
       }
     ENDCG
   }
}
}