Shader "TSirkus/ArtPass/Grime Overlay"
{
    Properties
    {
        _StainColor ("Stain tint", Color) = (0.24, 0.19, 0.12, 1)
        _Strength ("Weathering strength", Range(0, 0.85)) = 0.55
        _Seed ("Pattern seed", Float) = 1
        _Drips ("Vertical streaks", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent" }
        Pass
        {
            Name "SurfaceGrime"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend DstColor Zero
            ColorMask RGB
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _StainColor;
                half _Strength;
                float _Seed;
                half _Drips;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1,0)), f.x),
                            lerp(Hash(i + float2(0,1)), Hash(i + float2(1,1)), f.x), f.y);
            }
            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.uv;
                float2 p = uv * float2(9, 7) + _Seed;
                float grain = 0.65 * Noise(p) + 0.35 * Noise(p * 3.7);
                float streak = Noise(float2(uv.x * 36 + _Seed, uv.y * 2.3));
                float field = lerp(grain, grain * 0.4 + streak * 0.6, _Drips);
                float2 edge = smoothstep(float2(0,0), float2(0.13,0.13), uv)
                            * (1.0 - smoothstep(float2(0.80,0.80), float2(1,1), uv));
                half mask = edge.x * edge.y * smoothstep(0.26, 0.76, field) * _Strength;
                half3 rgb = lerp(half3(1,1,1), _StainColor.rgb, mask);
                // Multiplicative overlays fade to white, leaving the scene fog intact.
                rgb = MixFogColor(rgb, half3(1,1,1), input.fogFactor);
                return half4(rgb, 1);
            }
            ENDHLSL
        }
    }
}
