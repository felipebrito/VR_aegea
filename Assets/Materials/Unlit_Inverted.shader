Shader "Custom/Unlit_Inverted"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Front  // Faz a esfera ser renderizada pelo lado de dentro
        ZWrite On
        Lighting Off
        Pass
        {
            SetTexture [_MainTex] { combine texture }
        }
    }
}
