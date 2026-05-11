Shader "UI/InkFlow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FlowSpeedX ("Flow Speed X", Float) = 0.02
        _FlowSpeedY ("Flow Speed Y", Float) = 0.01
        _Alpha ("Alpha", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _FlowSpeedX;
            float _FlowSpeedY;
            float _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // UV 滚动
                float2 uv = i.uv;
                uv.x += _Time.y * _FlowSpeedX;
                uv.y += _Time.y * _FlowSpeedY;

                fixed4 col = tex2D(_MainTex, uv);
                col.a *= _Alpha * i.color.a;
                col.rgb *= i.color.rgb;
                return col;
            }
            ENDCG
        }
    }
}