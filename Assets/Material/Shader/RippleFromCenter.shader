// 从屏幕中心向外扩散的涟漪（Unlit + 透明）。
// 用法：赋给 Quad / Sprite / UI RawImage 的全屏材质；调 Speed、RingDensity、颜色与强度。
// 注：Built-in 与多数项目兼容；若纯 URP 且材质报错，请在 URP 里用 Shader Graph 复刻相同公式。

Shader "Custom/RippleFromCenter"
{
    Properties
    {
        [Header(Base)]
        [HDR] _Tint ("涟漪颜色 (HDR)", Color) = (0.6, 0.85, 1.0, 0.45)
        _MainTex ("底层贴图 (可选)", 2D) = "white" {}
        [Toggle] _UseMainTex ("采样底层贴图", Float) = 0

        [Header(Ripple)]
        _Speed ("扩散速度", Float) = 1.2
        _RingDensity ("圈密度 (越大圈越密)", Range(4, 40)) = 18
        _WaveSharp ("波峰锐利度", Range(0.05, 1)) = 0.35
        _FalloffStart ("从中心开始衰减起点", Range(0, 1)) = 0.0
        _FalloffEnd ("向边缘衰减终点", Range(0.1, 2)) = 1.25
        _AspectFix ("宽高比修正 (0=自动用屏幕比)", Float) = 0

        [Header(UV Distort)]
        _Distort ("涟漪对 UV 的扰动强度", Range(0, 0.08)) = 0.015
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            half4 _Tint;
            float _Speed;
            float _RingDensity;
            float _WaveSharp;
            float _FalloffStart;
            float _FalloffEnd;
            float _AspectFix;
            float _Distort;
            float _UseMainTex;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            static const float PI = 3.14159265359;

            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float aspect = _AspectFix > 0.001
                    ? _AspectFix
                    : (_ScreenParams.x / max(_ScreenParams.y, 1.0));

                float2 p = uv - 0.5;
                p.x *= aspect;
                float dist = length(p) * 2.0;

                // 向外传播的相位：距离越大相位越大，时间让整体相位减小 → 环向外走
                float phase = dist * _RingDensity - _Time.y * _Speed;
                float wave = sin(phase * PI * 2.0);

                // 锐利波脊（像水纹亮线）
                float ring = 1.0 - smoothstep(0.0, _WaveSharp, abs(wave));

                // 中心到边缘渐隐
                float mask = 1.0 - smoothstep(_FalloffStart, _FalloffEnd, dist);
                float intensity = ring * mask;

                // 用波给 UV 一点径向+切向扰动（可选采样贴图）
                float2 dir = p / max(length(p), 1e-4);
                float2 tang = float2(-dir.y, dir.x);
                float2 distort = (dir * wave + tang * wave * 0.35) * _Distort;
                float2 uvS = uv + distort;

                half3 col = _Tint.rgb * intensity;
                half a = _Tint.a * intensity;

                if (_UseMainTex > 0.5)
                {
                    half4 tex = tex2D(_MainTex, uvS);
                    col = lerp(col, tex.rgb * _Tint.rgb, tex.a * 0.5 + 0.15 * intensity);
                    a = saturate(a + tex.a * 0.25 * intensity);
                }

                return half4(col, saturate(a));
            }
            ENDCG
        }
    }

    FallBack Off
}
