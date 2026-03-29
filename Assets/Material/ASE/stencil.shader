Shader "Custom/stencil"
{
    Properties
    {
        _index ("stencil index", Int) = 1
        _DepthBias ("Depth bias (toward camera)", Range(0, 0.02)) = 0.0008
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry-10"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "StencilWrite"
            Tags { "LightMode" = "UniversalForward" }

            ColorMask 0
            Blend One Zero
            ZWrite Off
            ZTest LEqual
            Cull Back
            Offset -1, -1

            Stencil
            {
                Ref [_index]
                Comp Always
                Pass Replace
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _DepthBias;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 towardCamera = normalize(_WorldSpaceCameraPos.xyz - positionWS);
                positionWS += towardCamera * _DepthBias;
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
