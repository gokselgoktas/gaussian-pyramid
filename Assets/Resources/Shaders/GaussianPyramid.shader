Shader "Hidden/Gaussian Pyramid"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    Texture2D _MainTex;
    SamplerState sampler_MainTex;

    float4 _MainTex_TexelSize;

    float2 _Direction;
    float2 _Spread;

    float2 _TexelSize;

    float _LOD;

    struct Input
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct Varyings
    {
        float4 vertex : SV_POSITION;
        float2 uv : TEXCOORD0;
    };

    Varyings vertex(in Input input)
    {
        Varyings output;
        output.vertex = UnityObjectToClipPos(input.vertex.xyz);
        output.uv = input.uv;

    #if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0)
            output.uv.y = 1. - input.uv.y;
    #endif

        return output;
    }

    float4 fragment(in Varyings input) : SV_Target
    {
        return
            _MainTex.SampleLevel(sampler_MainTex, input.uv - 3.2307692308 * _Direction * _Spread * _TexelSize, _LOD) * .0702702703 +
            _MainTex.SampleLevel(sampler_MainTex, input.uv - 1.3846153846 * _Direction * _Spread * _TexelSize, _LOD) * .3162162162 +
            _MainTex.SampleLevel(sampler_MainTex, input.uv, _LOD) * .2270270270 +
            _MainTex.SampleLevel(sampler_MainTex, input.uv + 1.3846153846 * _Direction * _Spread * _TexelSize, _LOD) * .3162162162 +
            _MainTex.SampleLevel(sampler_MainTex, input.uv + 3.2307692308 * _Direction * _Spread * _TexelSize, _LOD) * .0702702703;
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vertex
            #pragma fragment fragment
            ENDCG
        }
    }
}
