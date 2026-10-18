Shader "Custom/GridShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BackgroundColor ("Background Color", Color) = (0.32,0.32,0.32,1)
        _LineColor ("Line Color", Color) = (0.36,0.36,0.36,1)
        _GridSize ("Grid Cell Size (xy world)", Vector) = (0.3,0.3,0,0)
        _Offset ("Grid Offset (xy world)", Vector) = (0,0,0,0)
        _LineWidth ("Line Width", Float) = 0.1
        _Padding ("Corner Padding", Float) = 0.018
        _Roundness ("Corner Roundness", Range(0,0.5)) = 0
        _NoiseScale ("Noise Scale", Float) = 0
        _NoiseStrength ("Noise Strength", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 worldXY     : TEXCOORD0;
                float4 color       : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _BackgroundColor;
                float4 _LineColor;
                float4 _GridSize;
                float4 _Offset;
                float _LineWidth;
                float _Padding;
                float _Roundness;
                float _NoiseScale;
                float _NoiseStrength;
            CBUFFER_END

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 wp = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(wp);
                OUT.worldXY = wp.xy;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 gs = max(_GridSize.xy, 1e-4);
                float2 p  = (IN.worldXY + _Offset.xy) / gs;   // cell-space coords
                float2 f  = frac(p) - 0.5;                    // -0.5..0.5 inside a cell
                float2 q  = abs(f);

                // rounded-rect signed distance for a single tile (normalized cell space)
                float r  = _Roundness + _Padding;
                float2 b = 0.5 - _LineWidth * 0.5 - r;
                float2 d = q - b;
                float sdf = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - r;

                // sdf < 0 -> inside tile, sdf > 0 -> gap (grid line)
                float aa = fwidth(sdf) + 1e-4;
                float lineMask = smoothstep(-aa, aa, sdf);

                half4 col = lerp(_BackgroundColor, _LineColor, lineMask);

                if (_NoiseStrength > 0.0)
                {
                    float n = hash21(floor(p * max(_NoiseScale, 1.0)));
                    col.rgb += (n - 0.5) * _NoiseStrength;
                }

                col *= _Color * IN.color;
                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
