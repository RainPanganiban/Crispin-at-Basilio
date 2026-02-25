Shader "Custom/Outline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 2
        _RenderDistance ("Render Distance", Float) = 20
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
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _RenderDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  distanceToCamera : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Calculate world position and distance to camera
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.distanceToCamera = distance(positionWS, _WorldSpaceCameraPos);

                // Transform vertex to clip space
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                // Transform normal to clip space for screen-space extrusion
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 normalCS = TransformWorldToHClipDir(normalWS);

                // Normalize only in screen XY so extrusion is uniform on screen
                float2 screenNormal = normalize(normalCS.xy);

                // Scale by outline width and correct for aspect ratio
                // _ScreenParams.x / _ScreenParams.y gives the aspect ratio
                float2 offset = screenNormal * (_OutlineWidth / _ScreenParams.xy) * output.positionCS.w * 2.0;

                output.positionCS.xy += offset;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Discard outline fragments beyond the render distance
                clip(_RenderDistance - input.distanceToCamera);

                return half4(_OutlineColor.rgb, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
