// Diagnostikorako shader-a, kapturetan bakarrik (materialak ez dira gordetzen).
// _Modua: 0 = normalak koloretan, 1 = UV0 xake-taula, 2 = UV2 xake-taula.
Shader "Hidden/Eclipse/Diag"
{
    Properties
    {
        _Modua ("Modua", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex Erpina
            #pragma fragment Pixela
            #include "UnityCG.cginc"

            float _Modua;

            struct Sarrera
            {
                float4 posizioa : POSITION;
                float3 normala : NORMAL;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
            };

            struct Tartekoa
            {
                float4 pos : SV_POSITION;
                float3 normala : TEXCOORD0;
                float2 uv0 : TEXCOORD1;
                float2 uv1 : TEXCOORD2;
            };

            Tartekoa Erpina(Sarrera s)
            {
                Tartekoa t;
                t.pos = UnityObjectToClipPos(s.posizioa);
                t.normala = UnityObjectToWorldNormal(s.normala);
                t.uv0 = s.uv0;
                t.uv1 = s.uv1;
                return t;
            }

            float Xakea(float2 uv, float zatiak)
            {
                float2 z = floor(uv * zatiak);
                return fmod(abs(z.x + z.y), 2.0);
            }

            fixed4 Pixela(Tartekoa t) : SV_Target
            {
                if (_Modua < 0.5)
                {
                    return fixed4(normalize(t.normala) * 0.5 + 0.5, 1.0);
                }

                float2 uv = _Modua < 1.5 ? t.uv0 : t.uv1;
                float x = Xakea(uv, 16.0);
                return fixed4(lerp(0.12, 0.9, x) * float3(frac(uv.x) * 0.6 + 0.4, frac(uv.y) * 0.6 + 0.4, 0.75), 1.0);
            }
            ENDCG
        }
    }
}
