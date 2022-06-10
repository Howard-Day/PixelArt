Shader "EdgeDetectionShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Threshold("Threshold", float) = 0.01
        _EdgeColor("Edge color", Color) = (0,0,0,1)
    }
        SubShader
        {
            // No culling or depth
            Cull Off ZWrite Off ZTest Always

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                sampler2D _CameraDepthNormalsTexture;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                sampler2D _MainTex;
                float4 _MainTex_TexelSize;
                float _Threshold;
                fixed4 _EdgeColor;

                float4 GetPixelValue(in float2 uv) {
                    half3 normal;
                    float depth;
                    DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv), depth, normal);
                    return fixed4(normal, depth);
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    fixed4 col = tex2D(_MainTex, i.uv);
                    fixed4 orValue = GetPixelValue(i.uv);
                    float2 offsets[4] = {
                        float2(-1, 0),
                        float2(0, -1),
                        float2(0, 1),
                        float2(1, 0)
                    };
                    float2 offsetcorners[4] = {
                        float2(-1, -1),
                        float2(-1, 1),
                        float2(1, -1),
                        float2(1, 1)
                    };


                    fixed4 sampledValue = fixed4(0,0,0,0);
                    fixed4 sampledValueCorner = fixed4(0, 0, 0, 0);
                    for (int j = 0; j < 4; j++) {
                        sampledValue += GetPixelValue(i.uv + offsets[j]/2 * _MainTex_TexelSize.xy);
                    }
                    sampledValue /= 4;
                    for (int j = 0; j < 4; j++) {
                        sampledValueCorner += GetPixelValue(i.uv + offsetcorners[j] / 2 * _MainTex_TexelSize.xy);
                    }
                    sampledValueCorner /= 4;




                    float edge = step(_Threshold / 10000, length(orValue - sampledValue)) + step(_Threshold / 10000, length(orValue - sampledValueCorner))/2;
                    
                    return lerp(col, _EdgeColor * col-.25, edge * _EdgeColor.a);
                }
                ENDCG
            }
        }
}