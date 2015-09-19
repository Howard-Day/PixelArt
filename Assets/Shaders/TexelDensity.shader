Shader "Pixel Art/Lit MultiTile" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _TileSize("Tileset Size - Takes First 2, Last 2 Do Nothing", vector) = (8,8,0,0)
		_MinDensity ("Min Texel Density", float) = 1
		_MaxDensity ("Max Texel Density", float) = 12
    }
    SubShader { 
        Pass{
            Tags { "RenderType"="Opaque" "LightMode" = "Always" "LightMode" = "ForwardBase"}
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_fwdbase
				#pragma glsl_no_auto_normalization 
                #pragma fragmentoption ARB_precision_hint_fastest               
                #include "UnityCG.cginc"
                #include "Lighting.cginc"
                uniform sampler2D _MainTex;
                uniform fixed4 _MainTex_ST;
                uniform fixed4 _TileSize;          // Made this a fixed4, you won't get greater precision using fixed here.
               	uniform fixed _TileBufferSize;
               
                struct a2v  {
                    float4 vertex : POSITION;
                    float4 texcoord : TEXCOORD0;
                    fixed4 color: COLOR;
                };
 
                struct v2f  {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;          // Made this a fixed, fixed sometimes isn't precise enough for UVs.
                    float3 WorldPosition : TEXCOORD1;    // Added this to store light direction for fragment shader.
                };
 
                v2f vert(a2v  v){
                    v2f o;
                    o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
 					o.color = v.color;
                    // Here we set up the normal and light dir for use in the fragment shader, both in object space.
                    o.normal = v.normal;
                    o.lightDir = ObjSpaceLightDir(v.vertex);
                    return o;
                }
 
                // Made this a fixed4, you won't gain precision with a fixed.
                fixed4 frag(v2f i) : COLOR
                {
                    // Normalize the vectors to fix interpolation shortening.
                    i.normal = normalize(i.normal);
                    i.lightDir = normalize(i.lightDir);
 					_TileBufferSize *= .06;
 					fixed InvBufferSize = 1-_TileBufferSize;
                    // Get dot product to use as ramp UV coords.
                    // Transformed from -1 to 1 range to 0 - 1 range so it can use the full dimensions of the ramp texture.
                    fixed NdotL = dot(i.normal, i.lightDir) * 0.5 + 0.5;
					fixed2 InvTileSize = fixed2(1/_TileSize.x,1/_TileSize.y);
					
                    fixed tileU = (frac((i.uv.x)*_TileSize.x)+(_TileBufferSize)*.5+(_TileBufferSize*InvTileSize.x*.5))*(InvTileSize.x)+i.color.r;
                    tileU -=i.color.r;
                    tileU *= InvBufferSize;
                    tileU +=i.color.r;
                    
                    fixed tileV = (frac((i.uv.y)*_TileSize.y)+(_TileBufferSize)*.5+(_TileBufferSize*InvTileSize.y*.5))*(InvTileSize.y)+i.color.g;
                    tileV -=i.color.g;
                    tileV *= InvBufferSize;
                    tileV +=i.color.g;
                    
                    fixed2 uv_dx = clamp( ddx( i.uv ), -4, 4 );
					fixed2 uv_dy = clamp( ddy( i.uv ), -4, 4 );
					
                    fixed4 tileUVs = fixed4(tileU,tileV,0,0);
					//fixed4 c = tex2Dlod(_MainTex,tileUVs);
					fixed4 c = tex2D(_MainTex, tileUVs.xy , uv_dx , uv_dy);
                    //c.rgb =* _LightColor0.rgb;
                    //c.a = 1.0;
                    return c;
                }
            ENDCG
        }		
	}
    Fallback "VertexLit"
}











void MainVertexShader(
	FVertexFactoryInput Input,
	out FVertexFactoryInterpolants FactoryInterpolants,
	out float4 WorldPosition	: TEXCOORD6,
	out float4 Position			: POSITION
	)
{
	FVertexFactoryIntermediates VFIntermediates = GetVertexFactoryIntermediates(Input);
	WorldPosition = VertexFactoryGetWorldPosition(Input, VFIntermediates);
	float3x3 TangentBasis = VertexFactoryGetTangentBasis(Input, VFIntermediates);

	FMaterialVertexParameters VertexParameters = GetMaterialVertexParameters(Input, VFIntermediates, WorldPosition.xyz, TangentBasis);
	WorldPosition.xyz += GetMaterialWorldPositionOffset(VertexParameters);

	Position = MulMatrix(ViewProjectionMatrix, WorldPosition);
	FactoryInterpolants = VertexFactoryGetInterpolants(Input, VFIntermediates);
}


/*=============================================================================
	Pixel Shader
=============================================================================*/

#define MAX_LOOKUPS 16

float4 TextureDensityParameters;
float4 TextureLookupInfo[ MAX_LOOKUPS ];

#if PS3
float CalcDensity( float MinDensity, float MaxDensity, FMaterialPixelParameters MaterialParameters, float WorldSpaceArea )
{
	float Density = MinDensity;
	int NumLookups = TextureDensityParameters.x;
	for ( int LookupIndex = 0; LookupIndex < MAX_LOOKUPS; ++LookupIndex )
	{
		if ( LookupIndex < NumLookups )
		{
			int TexCoordIndex = TextureLookupInfo[LookupIndex].z;
			float2 TextureSize = TextureLookupInfo[LookupIndex].xy;
			float2 TexCoord = TextureSize;
			for ( int Index = 0; Index < NUM_MATERIAL_TEXCOORDS; ++Index )
			{
				if ( Index == TexCoordIndex )
				{
					TexCoord *= MaterialParameters.TexCoords[Index].xy;	// In texels
				}
			}
			float2 A = ddx(TexCoord);
			float2 B = ddy(TexCoord);
			float2 C = A.xy * B.yx;

			// Area of parallelogram, in texels.
			float TexelArea = abs( C.x - C.y );

			Density = max( Density, TexelArea / WorldSpaceArea );
		}
	}
	return min( Density, MaxDensity );
}
#else
float CalcDensity( float MinDensity, float MaxDensity, FMaterialPixelParameters MaterialParameters, float WorldSpaceArea )
{
	float Density = MinDensity;
	int NumLookups = TextureDensityParameters.x;
	for ( int LookupIndex = 0; LookupIndex < NumLookups && LookupIndex < MAX_LOOKUPS; ++LookupIndex )
	{
		int TexCoordIndex = TextureLookupInfo[LookupIndex].z;
		float2 TextureSize = TextureLookupInfo[LookupIndex].xy;
		float2 TexCoord = MaterialParameters.TexCoords[TexCoordIndex].xy * TextureSize;	// In texels
		float2 A = ddx(TexCoord);
		float2 B = ddy(TexCoord);
		float2 C = A.xy * B.yx;

		// Area of parallelogram, in texels.
		float TexelArea = abs( C.x - C.y );

		Density = max( Density, TexelArea / WorldSpaceArea );
	}
	return min( Density, MaxDensity );
}
#endif


void MainPixelShader(
	FVertexFactoryInterpolants FactoryInterpolants,
	float4 WorldPosition	: TEXCOORD6,
	OPTIONAL_FacingSign
	OPTIONAL_PixelShaderScreenPosition
	out float4 OutColor		: COLOR0
	)
{
	FMaterialPixelParameters MaterialParameters = GetMaterialPixelParameters( FactoryInterpolants );
	CalcMaterialParameters(MaterialParameters, FacingSign, float4(0,0,1,0), float4(0,0,.00001f,1));
	GetMaterialClipping(MaterialParameters, PixelShaderScreenPosition.xy);

	// Area of parallelogram, in world space units.
	float WorldSpaceArea = length( cross( ddx(WorldPosition.xyz), ddy(WorldPosition.xyz) ) );
	WorldSpaceArea = max( WorldSpaceArea, 0.00000001f );

	float MinDensity = TextureDensityParameters.y;
	float IdealDensity = TextureDensityParameters.z;
	float MaxDensity = TextureDensityParameters.w;
	float Density = CalcDensity( MinDensity, MaxDensity, MaterialParameters, WorldSpaceArea );

	if ( Density > IdealDensity )
	{
		float Range = MaxDensity - IdealDensity;
		Density -= IdealDensity;
		OutColor = RETURN_COLOR( float4( Density/Range, (Range-Density)/Range, 0.0f, 1.0f ) );
	}
	else
	{
		float Range = IdealDensity - MinDensity;
		Density -= MinDensity;
		OutColor = RETURN_COLOR( float4( 0.0f, Density/Range, (Range-Density)/Range, 1.0f ) )	;
	}
}





