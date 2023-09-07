Shader "Custom Shader/VertexColors"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { 
            "Queue"="Geometry"
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
		}

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct appdata
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			struct v2f
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float fogCoord : TEXCOORD1;
				float4 color : COLOR;
			};
			
CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
CBUFFER_END
			
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			
			v2f vert (appdata v)
			{
				v2f o;
				o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.fogCoord = ComputeFogFactor(o.positionCS.z);
				o.color = v.color;
				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				// sample the texture
				half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
				col.rgb = MixFog(col, i.fogCoord);
				col.rgb *= i.color.rgb;
				return col;
			}
			ENDHLSL
		}
	}
}
