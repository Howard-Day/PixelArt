// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Pixel Art/ScreenGlare" {
Properties {
   _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
   _Color ("Color", Color) = (1,1,1,1)
   _Dither("Dithering Amount", float) = .1
}
 
SubShader {
   Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
   LOD 100
 
	//Blend One Zero, Zero One
   Blend OneMinusDstColor One, One Zero
   //BlendOp Add
   //Blend SrcAlpha One, One Zero // linear dodge
   ZWrite off
   //AlphaTest Greater .01
   ZTest Always
   Cull Off
   Pass {
     CGPROGRAM
       #pragma vertex vert
       #pragma fragment frag
     
       #include "UnityCG.cginc"
       #include "AutoLight.cginc"
       #include "Lighting.cginc"
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
	   fixed _Dither;
	   
       v2f vert (appdata_t v)
       {
         v2f o;
         fixed4 ObjDepth = mul(UNITY_MATRIX_IT_MV, v.vertex);
         o.vertex = UnityObjectToClipPos(v.vertex);
         o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
         o.color = v.color*2;
         o.screenPos = ComputeScreenPos(o.vertex);
         o.screenPos.w = ObjDepth.z/4;
         return o;
       }
     
       fixed4 frag (v2f i) : COLOR
       {

         fixed4 col = _Color*tex2D(_MainTex, i.texcoord)*i.color;
          fixed dither = 0;

			    dither = tex2D(_DitherTex,i.screenPos*_DitherScale).r;// _Color.rgb; 
			    dither -= .5;
			    dither *= .15*_Dither;
			    //dither *= .5;


		 col.rgb = col.rgb-dither*(1-col.r)*saturate(col.r*12);
         col.rgb *= i.color.a*2	;
         return col;
       }
     ENDCG
   }
}
}