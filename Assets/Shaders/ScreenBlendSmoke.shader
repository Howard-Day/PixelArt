// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Pixel Art/MultiTex_Smoke" {
Properties {
   _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
   _Color ("Color", Color) = (1,1,1,1)
   _AlphaClipOffset("Alpha Clipping Offset", float) = 0
   _AlphaDither("Alpha Dither Amount", float) = .3
   _ColorDither("Color Dither Amount", float) = .3
}
 
SubShader {
   Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
   LOD 100
 
   //BlendOp Add
   Blend SrcAlpha OneMinusSrcAlpha//OneMinusDstColor One, One Zero // screen
   //Blend SrcAlpha One, One Zero // linear dodge
   ZWrite Off
   //AlphaTest Greater .01
   Offset -10,-10
   Cull Off
   Pass {
     CGPROGRAM
       #pragma vertex vert
       #pragma fragment frag
       #pragma multi_compile DITHER_ON DITHER_OFF 
       #pragma glsl_no_auto_normalization 
       #pragma fragmentoption ARB_precision_hint_fastest   
       #include "UnityCG.cginc"
       #include "AutoLight.cginc"
       #include "Lighting.cginc"
       struct appdata_t {
         float4 vertex : POSITION;
         float2 texcoord : TEXCOORD0;
         fixed4 color: COLOR;
         fixed4 normal: NORMAL;
       };
 
       struct v2f {
         float4 vertex : SV_POSITION;
         half2 texcoord : TEXCOORD0;
         fixed4 screenPos: TEXCOORD1;
         fixed3 lightDir : TEXCOORD2;
         fixed4 color: COLOR;
         fixed4 normal: NORMAL;
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
         o.vertex = UnityObjectToClipPos(v.vertex);
         o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
         o.screenPos = ComputeScreenPos(o.vertex);
         o.color = v.color;
         o.normal = v.normal;
         o.lightDir = ObjSpaceLightDir(v.vertex);
         return o;
       }
     
       fixed4 frag (v2f i) : COLOR
       {
			fixed4 tex = tex2D(_MainTex, i.texcoord);
			fixed dither = 0;
			fixed NdotL = dot(i.normal, i.lightDir)*.9+.1;
			#if DITHER_ON
				dither = tex2D(_DitherTex,i.screenPos*_DitherScale).r;// _Color.rgb; 
				dither -= .5;
				dither *= .05;
				//dither *= tex.b+.5;
			#endif         
			//col.r = lerp(col.r,col.g,1-i.color.a);
			//fixed blendMask = col.r;
			//col.a = saturate((col.r-i.color.a/4)*2)
			//		*saturate(((1-(col.g+(.75-col.b*2))/2)-(1-i.color.a*.75))*2);
			//fixed edgeMask = col.r;
			fixed4 col; //saturate((col.a+dither*_AlphaDither-(_AlphaClipOffset*.25))*3048*2048);//(128*saturate((1-i.color.a)*2)));
			//col.rgb = ((1-saturate((col.r)-i.color.a))+(col.b)*(1+(dither*_ColorDither)*6)*3*(i.color.a)-1)*saturate(col.b*4+(1-i.color.a)+(.2+(1-col.b)-1));
			//col.rgb = lerp(i.color,(col.rgb+.75)*1.25*i.color,saturate(edgeMask*4-.5));
			fixed colBlend = saturate((tex.r+dither*_ColorDither)*1000-500);
			fixed colShade = saturate((tex.g+dither*_ColorDither)*1000-750);
			fixed3 colTint = _Color.rgb*i.color.rgb; 
			//col.rgb = lerp(colTint-.05,colTint,colBlend);
			//col.rgb = lerp(colTint-.05,colTint.rgb,colShade);//*(tex.b*.5+.5);
			col.rgb = (colTint+saturate(_LightColor0.rgb*((1-tex.r)+.5)*NdotL))+saturate(UNITY_LIGHTMODEL_AMBIENT*(((1-tex.b)/2+.5)))*((1-i.color.a)/4)+dither*colTint*_ColorDither;
			col.a = saturate((_Color.a*i.color.a*lerp(tex.g,tex.b,i.color.a)+(dither*_AlphaDither))*50-5);

			return col;
       }
     ENDCG
   }
}
}