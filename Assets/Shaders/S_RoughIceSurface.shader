Shader "Custom/S_RoughIceSurface"
{
    Properties
    {
        _BaseColor ("Ice Color", Color) = (0.68, 0.9, 1, 0.68)
        _DeepColor ("Dense Ice Color", Color) = (0.2, 0.46, 0.68, 0.86)
        _FrostColor ("Frost Color", Color) = (0.92, 0.98, 1, 1)
        _CrackColor ("Crack Color", Color) = (0.08, 0.2, 0.32, 1)
        _Transparency ("Transparency", Range(0, 1)) = 0.42
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.28
        _Roughness ("Roughness", Range(0, 1)) = 0.72
        _FrostAmount ("Frost Amount", Range(0, 1)) = 0.48
        _CrackAmount ("Crack Amount", Range(0, 1)) = 0.36
        _NoiseScale ("Noise Scale", Range(1, 80)) = 22
        _FreezeCenter ("Freeze Center", Vector) = (0, 0, 0, 0)
        _FreezeRadius ("Freeze Radius", Float) = 9999
        _FreezeFeather ("Freeze Feather", Float) = 1.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        Cull Back

        Pass
        {
            Name "ForwardIce"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _DeepColor;
                half4 _FrostColor;
                half4 _CrackColor;
                half _Transparency;
                half _ReflectionStrength;
                half _Roughness;
                half _FrostAmount;
                half _CrackAmount;
                half _NoiseScale;
                float4 _FreezeCenter;
                half _FreezeRadius;
                half _FreezeFeather;
            CBUFFER_END

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash(i);
                float b = Hash(i + float2(1.0, 0.0));
                float c = Hash(i + float2(0.0, 1.0));
                float d = Hash(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    value += Noise(p) * amplitude;
                    p = p * 2.03 + 11.7;
                    amplitude *= 0.5;
                }
                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = normals.normalWS;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float freezeDistance = distance(input.positionWS.xz, _FreezeCenter.xz);
                half freezeMask = 1.0h - smoothstep(_FreezeRadius - _FreezeFeather, _FreezeRadius, freezeDistance);
                clip(freezeMask - 0.01h);

                float2 p = input.positionWS.xz * (_NoiseScale * 0.08);
                float frostNoise = Fbm(p);
                float scratchNoise = Fbm(p * 3.4 + 19.0);
                float crackLines = 1.0 - smoothstep(0.035, 0.12, abs(frac((p.x + scratchNoise * 0.65) * 1.7) - 0.5));
                crackLines *= smoothstep(0.52, 0.95, Fbm(p * 0.7 + 4.0)) * _CrackAmount;

                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirWS)), 3.5h);

                half roughMask = saturate(frostNoise * _FrostAmount + scratchNoise * _Roughness * 0.35h);
                half3 reflectVector = reflect(-viewDirWS, normalWS);
                half3 envReflection = GlossyEnvironmentReflection(reflectVector, lerp(0.05h, 0.72h, _Roughness), 1.0h);

                half3 color = lerp(_BaseColor.rgb, _DeepColor.rgb, saturate(frostNoise * 0.55h));
                color = lerp(color, _FrostColor.rgb, roughMask * 0.55h);
                color = lerp(color, _CrackColor.rgb, saturate(crackLines));
                color = lerp(color, envReflection, saturate((fresnel + 0.18h) * _ReflectionStrength));

                half alpha = saturate(1.0h - _Transparency + roughMask * 0.25h + crackLines * 0.28h + fresnel * 0.15h);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
