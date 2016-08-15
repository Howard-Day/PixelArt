// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Pixel Art/Dither" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
		
    }
    SubShader {
             Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
             ZWrite Off ZTest Always Lighting Off Cull Off Fog { Mode Off } Blend DstColor SrcColor
             LOD 110
            
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
                sampler2D _DitherTex;

                struct a2v  {
                    float4 vertex : POSITION;
                    float4 texcoord : TEXCOORD0;
                    float4 color : COLOR;
                };
 
                struct v2f  {
                    float4 pos : SV_POSITION;
                    float3 worldPos : TEXCOORD0;       // Made this a float, fixed sometimes isn't precise enough for UVs.
                    fixed4 color : COLOR;
                };
 
                v2f vert(a2v  i){
                    v2f o; 
                    o.pos = mul(UNITY_MATRIX_MVP, i.vertex);
                    fixed4 ObjDepth = mul(UNITY_MATRIX_IT_MV, i.vertex);
                    o.color = ObjDepth;
                    o.worldPos = mul(unity_ObjectToWorld, i.vertex);
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
                
                                         
                     float4 final;                
                     final.rgb = i.color.rgb * c.rgb * 2;
                     final.a = i.color.a * c.a;
                     return lerp(float4(0.5f,0.5f,0.5f,0.5f), final, final.a);
                    
                }
            ENDCG
        }    
		
	}
    Fallback "VertexLit"
}