// Death Perception: silueta del enemigo en una textura (color por enemigo en _TcOutlineColor). Sin profundidad: se ve entera.
Shader "TarkovaCola/OutlineMask"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _TcOutlineColor;
            float4 vert(float4 v : POSITION) : SV_POSITION { return UnityObjectToClipPos(v); }
            float4 frag() : SV_Target { return float4(_TcOutlineColor.rgb, 1); }
            ENDCG
        }
    }
}
