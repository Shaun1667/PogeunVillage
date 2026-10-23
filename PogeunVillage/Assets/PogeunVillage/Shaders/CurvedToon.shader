Shader "PogeunVillage/CurvedToon"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _BaseMap ("Base Map", 2D) = "white" {}
        [Toggle] _UseVertexColor ("Use Vertex Color", Float) = 0

        [Header(Toon Lighting)]
        _ShadowTint ("Shadow Tint", Color) = (0.62, 0.66, 0.86, 1)
        _RampThreshold ("Ramp Threshold", Range(-1, 1)) = 0.05
        _RampSmooth ("Ramp Smoothness", Range(0.001, 1)) = 0.12
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.35
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.12
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 0)

        [Header(Water Waves)]
        _WaveAmp ("Wave Amplitude", Float) = 0
        _WaveSpeed ("Wave Speed", Float) = 1
        _WaveScale ("Wave Scale", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _UseVertexColor;
            half4 _ShadowTint;
            half _RampThreshold;
            half _RampSmooth;
            half _AmbientStrength;
            half4 _RimColor;
            half _RimStrength;
            half4 _EmissionColor;
            float _WaveAmp;
            float _WaveSpeed;
            float _WaveScale;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        #include "CurvedWorld.hlsl"

        float3 PV_ApplyWave(float3 positionWS)
        {
            if (_WaveAmp > 0.0)
            {
                float t = _Time.y * _WaveSpeed;
                positionWS.y += (sin(positionWS.x * _WaveScale + t)
                               + sin(positionWS.z * _WaveScale * 1.3 + t * 0.8)) * 0.5 * _WaveAmp;
            }
            return positionWS;
        }

        void PV_GetBentWorld(float3 positionOS, float3 normalOS, out float3 positionWS, out float3 normalWS)
        {
            positionWS = PV_ApplyWave(TransformObjectToWorld(positionOS));
            normalWS = TransformObjectToWorldNormal(normalOS);
            PV_BendWorld(positionWS, normalWS);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                half4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                PV_GetBentWorld(input.positionOS.xyz, input.normalOS, output.positionWS, output.normalWS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                albedo.rgb *= lerp(half3(1, 1, 1), input.color.rgb, _UseVertexColor);

                float3 normalWS = normalize(input.normalWS);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                half ndotl = dot(normalWS, light.direction);
                half lit = smoothstep(_RampThreshold - _RampSmooth, _RampThreshold + _RampSmooth, ndotl);
                lit *= light.shadowAttenuation;

                half3 color = lerp(albedo.rgb * _ShadowTint.rgb, albedo.rgb * light.color, lit);
                color += albedo.rgb * SampleSH(normalWS) * _AmbientStrength;

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half rim = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 4.0) * _RimStrength;
                color += rim * _RimColor.rgb * lit;
                color += _EmissionColor.rgb;

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS, normalWS;
                PV_GetBentWorld(input.positionOS.xyz, input.normalOS, positionWS, normalWS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            half4 frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS, normalWS;
                PV_GetBentWorld(input.positionOS.xyz, input.normalOS, positionWS, normalWS);
                return TransformWorldToHClip(positionWS);
            }

            half frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                float3 positionWS;
                PV_GetBentWorld(input.positionOS.xyz, input.normalOS, positionWS, output.normalWS);
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 octNormal = PackNormalOctQuadEncode(normalWS);
                return half4(PackFloat2To888(saturate(octNormal * 0.5 + 0.5)), 0.0);
            #else
                return half4(normalWS, 0.0);
            #endif
            }
            ENDHLSL
        }
    }

    FallBack Off
}
