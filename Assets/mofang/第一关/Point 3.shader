Shader "Unlit/Point1"
{
    Properties
    {
        _MainTex ("原图", 2D) = "white" {}
        _Color ("环颜色", Color) = (0.35, 0.9, 1.0, 1)
        _Speed ("扩散速度", Float) = 1.0
        _Width ("环宽度", Float) = 0.08
        _Count ("环数量", Float) = 6
        _Strength ("环强度", Float) = 1.2
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Stencil
            {
                Ref 1
                Comp Equal
                Pass Keep
                Fail Keep
                ZFail Keep
            }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _Color;
                half _Speed;
                half _Width;
                half _Count;
                half _Strength;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float aspect = _MainTex_TexelSize.z / max(_MainTex_TexelSize.w, 1e-5);
                float2 p = float2((i.uv.x - 0.5) * aspect, i.uv.y - 0.5);
                half maxDist = (half)0.5 * length(float2(aspect, 1.0));
                half dist = length(p) / max(maxDist, (half)1e-4);

                half t = _Time.y * _Speed;
                int ringCount = (int)clamp(round(_Count), 1.0, 16.0);
                half ringMask = (half)0.0;

                const half maxRadius = (half)1.0;
                const half timeScale = (half)0.55;
                const half phaseStep = (half)0.11;

                for (int k = 0; k < 16; k++)
                {
                    half on = (half)(k < ringCount ? 1 : 0);
                    half phase = frac(t * timeScale + (half)k * phaseStep);
                    half ringRadius = phase * maxRadius;
                    half ring = abs(dist - ringRadius);
                    half ringPx = max(_Width * (half)1.38, (half)1e-4);
                    ringMask += on * smoothstep(ringPx, (half)0.0, ring);
                }

                ringMask = saturate(ringMask * _Strength);

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half3 rgb = tex.rgb * _Color.rgb;
                half a = ringMask * tex.a * _Color.a;

                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
}
