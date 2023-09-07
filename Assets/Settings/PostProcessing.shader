Shader "Custom Shader/FogWithNoise"
{
    Properties
    {
        // 控制雾的浓度
        _FogDensity ("Fog Density", Range(0, 3.0)) = 0.42
        // 控制雾的颜色
        _FogColor ("Fog Color", Color) = (1, 1, 1, 1)
        // 控制雾的起始高度
        _FogStart ("Fog Start", Float) = 0.0
        // 控制雾的结束高度
        _FogEnd ("Fog End", Float) = 2.0
        // 噪声贴图,用于产生雾的噪声效果
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        // 控制噪声的程度
        // 当噪声程度为0时,雾的效果为线性的
        _NoiseAmount ("Noise Amount", Range(0, 3.0)) = 1.55
        // 控制噪声纹理在X和Y方向上的移动速度,以此模拟雾的飘动效果
        _FogXSpeed ("Fog X Speed", Range(-0.5, 0.5)) = 0.18
        _FogYSpeed ("Fog Y Speed", Range(-0.5, 0.5)) = 0.18

    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalRenderPipeline"
        }

        Cull Off
        Blend Off
        ZWrite Off


        Pass
        {
            ZTest Always
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                half _FogDensity;
                half4 _FogColor;
                half _FogStart;
                half _FogEnd;
                half _NoiseAmount;
                half _FogXSpeed;
                half _FogYSpeed;
            CBUFFER_END

            struct appdata
            {
                uint vertexID : SV_VertexID;
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                #if SHADER_API_GLES
				 float4 positionCS = i.positionOS;
				 float2 uv  = i.uv;
                #else
                float4 positionCS = GetFullScreenTriangleVertexPosition(v.vertexID);
                float2 uv = GetFullScreenTriangleTexCoord(v.vertexID);
                #endif
                o.positionCS = positionCS;
                o.uv = uv;
                return o;
            }


            half4 frag(v2f i) : SV_Target
            {
                // 从深度贴图重建世界空间坐标
                half2 screenUV = (i.positionCS.xy / _ScreenParams.xy);
                #if UNITY_REVERSED_Z
                float depth = SampleSceneDepth(screenUV);
                #else
			// Adjust Z to match NDC for OpenGL (-1,1)
			// Adjust depth from [0, 1] to [-1, 1]
			float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(screenUV));
                #endif
                float3 positionWS = ComputeWorldSpacePosition(screenUV, depth, UNITY_MATRIX_I_P);

                float2 speed = _Time.y * float2(_FogXSpeed, _FogYSpeed);
                float noise = (SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, screenUV + speed).r - 0.5) * _NoiseAmount;
                // 运用雾效公式
                float fogDensity = (_FogEnd - positionWS.y) / (_FogEnd - _FogStart);
                fogDensity = saturate(fogDensity * _FogDensity * (1 + noise));
                half3 finalColor = SampleSceneColor(screenUV);
                finalColor.rgb = lerp(finalColor.rgb, _FogColor.rgb, fogDensity);
                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}