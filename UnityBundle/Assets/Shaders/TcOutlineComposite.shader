// Death Perception: borde de la silueta (dilatacion de la mascara menos la mascara), mezclado sobre la imagen.
Shader "TarkovaCola/OutlineComposite"
{
    Properties { _MainTex ("Mask", 2D) = "black" {} }
    SubShader
    {
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _TcOutlineWidth;     // px
            float _TcOutlineAlpha;
            float4 frag(v2f_img i) : SV_Target
            {
                float4 c = tex2D(_MainTex, i.uv);
                if (c.a > 0.5) return 0;                                            // solo el borde (sin relleno)
                float4 best = 0;
                float w = _TcOutlineWidth;
                [unroll] for (int k = 0; k < 16; k++)
                {
                    float a = k * 0.3926991;
                    float2 o = float2(cos(a), sin(a)) * _MainTex_TexelSize.xy;
                    float4 s1 = tex2D(_MainTex, i.uv + o * w);
                    float4 s2 = tex2D(_MainTex, i.uv + o * w * 0.5);
                    float4 s = s1.a > s2.a ? s1 : s2;
                    if (s.a > best.a) best = s;
                }
                return float4(best.rgb, best.a * _TcOutlineAlpha);
            }
            ENDCG
        }
    }
}
