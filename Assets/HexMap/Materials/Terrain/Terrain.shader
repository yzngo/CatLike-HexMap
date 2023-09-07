Shader "Custom Shader/Terrain"
{
    Properties
    {
        _Color ("Color Tint", Color) = (1,1,1,1)
        [MainTexture] _MainTex ("Texture", 2DArray) = "white" {}
        _GridTex ("Grid Texture", 2D) = "white" {}

        _Diffuse ("Diffuse", Color) = (1,1,1,1)
        _Specular("Specular", Color) = (1,1,1,1)
        _Gloss ("Gloss", Range(1, 256)) = 82 // 光泽度
        
        // 是否启用Alpha Clipping
        [Toggle(_ALPHACLIPPING)]
        _AlphaClipping("Alpha Clipping", Float) = 0 
        _CutOff("Cut Off", Range(0, 1)) = 0 // Alpha Clipping的阈值

        // 控制是否启用多光源
        [Toggle(_ADDITIONALLIGHTS)]
        _AdditionalLights("Additional Lights", Float) = 1
    }
    
    SubShader
    {
        Tags
        {
            "Queue"="Geometry"
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }

        LOD 100

        HLSLINCLUDE
        #pragma target 3.5
        // 预编译指令，控制是否启用 Alpha Clipping
        #pragma shader_feature _ALPHACLIPPING
        // 预编译指令，控制是否启用多光源
        #pragma shader_feature _ADDITIONALLIGHTS
        
        // 接收阴影需要的预编译指令
        // 主光源
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS  // 控制是否接收阴影
        #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE  // 控制是否使用阴影级联
        // 额外光源
        #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
        #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
        #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS_CASCADE
        // 软阴影
        #pragma multi_compile _ _SHADOWS_SOFT  // 控制是否使用软阴影
        
        // 控制是否启用雾效
        #pragma multi_compile_fog

        #pragma multi_compile _ _GRID_ON

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _Color;
        float4 _MainTex_ST;
        float4 _GridTex_ST;
        float4 _Diffuse;
        float4 _Specular;
        float _Gloss;
        float _CutOff;
    CBUFFER_END
        TEXTURE2D_ARRAY(_MainTex);
        SAMPLER(sampler_MainTex);
        
        TEXTURE2D(_GridTex);
        SAMPLER(sampler_GridTex);
        
        ENDHLSL

        // 因为CBUFFER的原因，使用默认的阴影投射器无法支持SRP Batcher
        // 同一Shader的不同 Pass 的 UnityPerMaterial-CBUFFER 要有完全相同的大小和布局
		// UsePass "Universal Render Pipeline/Lit/ShadowCaster"

        // 自定义阴影投射器
        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            ColorMask 0

            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS, true);
                Light mainLight = GetMainLight();
                // 应用 shadow bias
                positionWS = ApplyShadowBias(positionWS, normalWS, mainLight.direction);
                o.vertex = TransformWorldToHClip(positionWS);
                
                #if UNITY_REVERSED_Z
                    o.vertex.z = min(o.vertex.z, o.vertex.w * UNITY_NEAR_CLIP_VALUE);
                #else
					o.vertex.z = max(o.vertex.z, o.vertex.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
            #ifdef _ALPHACLIPPING
                // 主材质采样
                float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv.xy);
                clip(mainTex.a - _CutOff);
            #endif
                return 0;
            }
            ENDHLSL
        }

        // 前向渲染Pass
        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            struct appdata
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                float3 uv2 : TEXCOORD2;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float4 color : COLOR;
                
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;

                float3 normalWS : TEXCOORD1;
                float3 tangentWS : TEXCOORD2; // w分量是切线的方向
                float3 binormalWS : TEXCOORD3;

                float4 shadowCoord : TEXCOORD4;
                float3 positionWS : TEXCOORD5;

                float fogCoord : TEXCOORD7;
                float3 terrain : TEXCOORD8;

            };

            v2f vert(appdata v)
            {
                v2f o;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.positionOS.xyz);
                o.vertex = vertexInput.positionCS;
                o.positionWS = vertexInput.positionWS;
                o.shadowCoord = GetShadowCoord(vertexInput);

                o.uv.xy = TRANSFORM_TEX(v.texcoord, _MainTex);

                o.normalWS = TransformObjectToWorldNormal(v.normalOS, true);
                o.tangentWS = normalize(mul(v.tangentOS, unity_WorldToObject)).xyz;
                o.binormalWS = normalize(cross(o.normalWS, o.tangentWS) * v.tangentOS.w);

                o.fogCoord = ComputeFogFactor(o.positionWS.z);
                o.color = v.color;
                o.terrain = v.uv2.xyz;
                return o;
            }

            half3 LambertShading(Light light, float3 albedo, float3 normalWS)
            {
                float3 lightColor = light.color;
                float3 lightDirWS = normalize(light.direction);
                float lightAtten = light.distanceAttenuation;
                float shadowAtten = light.shadowAttenuation;
                // Lambert
                return lightColor * albedo * saturate(dot(normalWS, lightDirWS)) * lightAtten * shadowAtten;
            }

            half3 BlinnPhongShading(Light light, float3 specularColor, float3 normalWS, float3 viewDirWS, float gloss)
            {
                float3 lightColor = light.color;
                float3 lightDirWS = normalize(light.direction);
                // 求半程向量
                float3 halfDirWS = normalize(lightDirWS + viewDirWS);
                // Blinn-Phong
                return lightColor * specularColor * pow(saturate(dot(normalWS, halfDirWS)), gloss);
            }

            float4 GetTerrainColor(v2f i, int index)
            {
                float4 c = SAMPLE_TEXTURE2D_ARRAY(_MainTex, sampler_MainTex, i.positionWS.xz*0.02, i.terrain[index]);
                return c * i.color[index];
            }
            
            half4 frag(v2f i) : SV_Target
            {
                // 根据 splat map 混合地形纹理
                float4 mainTex = GetTerrainColor(i, 0) + GetTerrainColor(i, 1) + GetTerrainColor(i, 2);
                
                float4 gridColor = 1;
            #if defined(_GRID_ON)
                float2 gridUV = i.positionWS.xz;
                gridUV.x *= 1 / (4 * 8.660250404);
                gridUV.y *= 1 / (2 * 15.0);
                gridColor = SAMPLE_TEXTURE2D(_GridTex, sampler_GridTex, gridUV);
            #endif
                
                // 世界空间视线方向，从顶点看向摄像机
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(i.positionWS));
                // 计算 albedo 颜色
                float3 albedo = mainTex.rgb * _Color.rgb;

                // 计算 环境光
                float3 ambient = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w) * albedo;
                float3 finalColor = ambient;

                // 获取世界空间的主光源信息
                Light mainLight = GetMainLight(i.shadowCoord);
                // 计算 Lambert 漫反射
                finalColor += LambertShading(mainLight, albedo, i.normalWS);
                // 计算 Blinn-Phong 高光
                finalColor += BlinnPhongShading(mainLight, _Specular.rgb, i.normalWS, viewDirWS, _Gloss);
                // 叠加雾效
                finalColor = MixFog(finalColor, i.fogCoord);
                finalColor = finalColor * gridColor;
                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Packages/com.unity.render-pipelines.universal/FallbackError"
}
