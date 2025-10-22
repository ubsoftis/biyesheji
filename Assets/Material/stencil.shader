Shader "Custom/stencil"
{
    Properties
    {
        _index ("stencil index", Int) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Blend Zero One
            ZWrite Off

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
                Fail Keep
            }
        }
    }
}