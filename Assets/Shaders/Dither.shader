Shader "Pixel Art/Dither" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
		
    }
    SubShader {
            Tags { "RenderType"="Transparent"  "Queue" = "Transparent" }
            Blend DstColor OneMinusSrcAlpha //DstColor Zero
            Zwrite Off
            ZTest Always
            
            Pass{
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag 
                #pragma fragmentoption ARB_precision_hint_fastest            
                #include "UnityCG.cginc"

                uniform sampler2D _MainTex;
                uniform float4 _MainTex_ST;
                uniform fixed4 _Color;          // Made this a fixed4, you won't get greater precision using float here.
            	float _DitherScale;
               
                struct a2v  {
                    float4 vertex : POSITION;
                    float4 texcoord : TEXCOORD0;
                };
 
                struct v2f  {
                    float4 pos : SV_POSITION;
                    float3 worldPos : TEXCOORD0;       // Made this a float, fixed sometimes isn't precise enough for UVs.
                };
 
                v2f vert(a2v  i){
                    v2f o; 
                    o.pos = mul(UNITY_MATRIX_MVP, i.vertex);
                    o.worldPos = mul(_Object2World, i.vertex);
                    return o;
                }
 
                // Made this a fixed4, you won't gain precision with a float.
                fixed4 frag(v2f i) : COLOR
                {
                    fixed4 c;
                    float2 newCoords = i.worldPos.xy;
                    fixed3 dither = tex2D(_MainTex, newCoords/_DitherScale).rgb;
                    c.rgb = dither*_Color.rgb;
                    c.a = _Color.a;
                    return c;
                }
            ENDCG
        }    
		
	}
    Fallback "VertexLit"
}