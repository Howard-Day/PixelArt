
Shader "Pixel Art/MultiTex_Flame" {
Properties {
   _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
   _Color ("Color", Color) = (1,1,1,1)
   _AlphaClipOffset("Alpha Clipping Offset", float) = 0
   _AlphaDither("Alpha Clipping Offset", float) = .3
   _ColorDither("Alpha Clipping Offset", float) = .3
}
 
SubShader {
   Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
   LOD 100
 
   //BlendOp Add
   Blend SrcAlpha OneMinusSrcAlpha//OneMinusDstColor One, One Zero // screen
   //Blend SrcAlpha One, One Zero // linear dodge
   ZWrite Off
   //AlphaTest Greater .01
   Offset -1420,-1420
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
	   fixed _AlphaClipOffset;
	   fixed _AlphaDither;
	   fixed _ColorDither;
       v2f vert (appdata_t v)
       {
         v2f o;
         o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
         o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
         o.screenPos = ComputeScreenPos(o.vertex);
         o.color = v.color;
         return o;
       }
     
       fixed4 frag (v2f i) : COLOR
       {
			fixed4 col = _Color*tex2D(_MainTex, i.texcoord);
			fixed dither = 0;
			#if DITHER_ON
				dither = tex2D(_DitherTex,i.screenPos*_DitherScale).r;// _Color.rgb; 
				dither -= .5;
			#endif         
			col.r = lerp(col.r,col.g,1-i.color.a);
			fixed blendMask = col.r;
			col.a = saturate((col.r-i.color.a/4)*2)
					*saturate(((1-(col.g+(.75-col.b*2))/2)-(1-i.color.a*.75))*2);
			fixed edgeMask = col.r;
			col.a = saturate((col.a+dither*_AlphaDither-(_AlphaClipOffset*.25))*3048*2048);//(128*saturate((1-i.color.a)*2)));
			col.rgb = ((1-saturate((col.r)-i.color.a))+(col.b)*(1+(dither*_ColorDither)*6)*3*(i.color.a)-1)*saturate(col.b*4+(1-i.color.a)+(.2+(1-col.b)-1));
			col.rgb = lerp(i.color,(col.rgb+.75)*1.25*i.color,saturate(edgeMask*4-.5));

			return col;
       }
     ENDCG
   }
}
}