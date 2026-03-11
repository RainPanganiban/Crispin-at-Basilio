Shader "Custom/CelShader"
{
    Properties
    {
        [Header(Base)]
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)

        [Header(Cel Shading)]
        _CelSteps ("Cel Steps", Range(1, 10)) = 3
        _StepSmoothness ("Step Smoothness", Range(0.001, 0.5)) = 0.05
        _ShadowColor ("Shadow Color", Color) = (0.2, 0.2, 0.2, 1)

        [Header(Rim Lighting)]
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.1, 10)) = 4
        _RimAmount ("Rim Amount", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float  _CelSteps;
                float  _StepSmoothness;
                float4 _ShadowColor;
                float4 _RimColor;
                float  _RimPower;
                float  _RimAmount;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                
                output.shadowCoord = GetShadowCoord(vertexInput);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Base Color & Texture
                float4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float4 baseColor = texColor * _BaseColor;

                // 2. Lighting Calculations
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Get main light data
                Light mainLight = GetMainLight(input.shadowCoord);
                float3 lightDir = normalize(mainLight.direction);
                
                // Standard NdotL
                float d = dot(normalWS, lightDir);
                float halfLambert = d * 0.5 + 0.5; // Better for toon shading wrap
                
                // 3. Cel Shading (Quantization)
                // We create bands by scaling the NdotL, floor-ing it, and scaling it back
                float cel = floor(halfLambert * _CelSteps) / (_CelSteps - 1.0);
                
                // Smoothing the transitions
                float delta = fwidth(halfLambert);
                float smoothCel = smoothstep(cel - _StepSmoothness, cel + _StepSmoothness, halfLambert);
                float finalCel = lerp(cel, halfLambert, _StepSmoothness); // Simplified smoothing
                
                // Even better toon ramp:
                float ramp = smoothstep(0.5 - _StepSmoothness, 0.5 + _StepSmoothness, halfLambert);
                // Multi-step version:
                float toon = 0;
                for(int i=0; i < (int)_CelSteps; i++) {
                    float threshold = (float)i / _CelSteps;
                    toon += smoothstep(threshold - _StepSmoothness, threshold + _StepSmoothness, halfLambert);
                }
                toon /= _CelSteps;

                // 4. Combine Shadow & Light
                float3 lightColor = mainLight.color * mainLight.shadowAttenuation;
                float3 diffuse = lerp(_ShadowColor.rgb, lightColor, toon);
                
                // 5. Rim Lighting
                float rim = 1.0 - saturate(dot(viewDirWS, normalWS));
                float rimIntensity = pow(rim, _RimPower) * _RimAmount;
                float3 rimColor = rimIntensity * _RimColor.rgb;

                // 6. Final Composition
                float3 finalColor = baseColor.rgb * diffuse + rimColor;
                
                return half4(finalColor, baseColor.a);
            }
            ENDHLSL
        }
        
        // Shadow Caster Pass (Needed for the object to cast shadows)
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));
                return output;
            }

            half4 frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
