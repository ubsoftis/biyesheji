Shader "Ymck/Test"
{
    Properties//通过材质面板传入的属性
    {
        _MainColor("Main Color",Color)=(1,0,0,1)
        _ColorIntensity("Color Intensity",Range(0,1))=1
        _MainTex("Main Texture",2d)="white"{}
    }
    //SubShader通过lod选择
    SubShader//子着色器（高配）
    {
        LOD 600
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            #pragma vertex vert 
            //顶点从三维空间转换到投影空间  
            #pragma fragment frag 
            //顶点构成的三角形内部的每个显示在屏幕上的像素的着色
            
            float _ColorIntensity;
            float4 _MainColor;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            struct appdata//顶点数据
            {
                //字段类型 字段名称：字段语义
                float3 pos:POSITION;//顶点位置数据
                float2 uv:TEXCOORD0;
            };
            
            struct v2f//顶点构成三角形内部的每个显示显示在屏幕上的像素数据
            {
                float4 pos:SV_POSITION;//裁剪空间下位置
                float2 uv:TEXCOORD0;
            };
            
            v2f vert(appdata IN)//将appdata转换为v2f类型
            {
                v2f OUT=(v2f)0;//申明v2f变量 并赋值为0
                OUT.pos=mul(UNITY_MATRIX_MVP,float4(IN.pos,1));
                OUT.uv=IN.uv;
                TRANSFORM_TEX();
                return OUT;
            }
            
            float4 frag(v2f IN):SV_TARGET//申明渲染的颜色
            {
                return float4(_MainColor)*_ColorIntensity;
            }
            ENDHLSL
        }
    }
  
    FallBack "Hidden/Universal Render Pipeline/FallbackError"//故障情况下的最保守shader的路径和名称
    //urp “Hidden/Universal Render Pipeline/FallbackError”
}
