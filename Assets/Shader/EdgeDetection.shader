Shader "PostProcess/PixelNoir"
{
    Properties
    {
        _PaletteSize      ("Color Levels",      Range(2, 16))  = 8
        _DitherStrength   ("Dither Strength",   Range(0, 1))   = 0.45
        _PixelSize        ("Pixel Size",        Range(1, 4))   = 1
        _EdgeProtect      ("Edge Protection",   Range(0, 1))   = 0.7
        _SaturationBoost  ("Saturation Boost",  Range(0.5, 2)) = 1.3
        _VignetteAmount   ("Vignette",          Range(0, 1))   = 0.35
        _ScanlineStrength ("Scanline Strength", Range(0, 0.3)) = 0.08
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "PixelNoirPass"
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);
            float4 _BlitTexture_TexelSize;

            float _PaletteSize;
            float _DitherStrength;
            float _PixelSize;
            float _EdgeProtect;
            float _SaturationBoost;
            float _VignetteAmount;
            float _ScanlineStrength;

            #pragma vertex vert
            #pragma fragment frag

            // 8x8 Bayer matrix — finer, less blocky than 4x4
            static const float bayer8x8[64] =
            {
                 0.0/64.0, 32.0/64.0,  8.0/64.0, 40.0/64.0,  2.0/64.0, 34.0/64.0, 10.0/64.0, 42.0/64.0,
                48.0/64.0, 16.0/64.0, 56.0/64.0, 24.0/64.0, 50.0/64.0, 18.0/64.0, 58.0/64.0, 26.0/64.0,
                12.0/64.0, 44.0/64.0,  4.0/64.0, 36.0/64.0, 14.0/64.0, 46.0/64.0,  6.0/64.0, 38.0/64.0,
                60.0/64.0, 28.0/64.0, 52.0/64.0, 20.0/64.0, 62.0/64.0, 30.0/64.0, 54.0/64.0, 22.0/64.0,
                 3.0/64.0, 35.0/64.0, 11.0/64.0, 43.0/64.0,  1.0/64.0, 33.0/64.0,  9.0/64.0, 41.0/64.0,
                51.0/64.0, 19.0/64.0, 59.0/64.0, 27.0/64.0, 49.0/64.0, 17.0/64.0, 57.0/64.0, 25.0/64.0,
                15.0/64.0, 47.0/64.0,  7.0/64.0, 39.0/64.0, 13.0/64.0, 45.0/64.0,  5.0/64.0, 37.0/64.0,
                63.0/64.0, 31.0/64.0, 55.0/64.0, 23.0/64.0, 61.0/64.0, 29.0/64.0, 53.0/64.0, 21.0/64.0
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv         = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float luma(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }

            float3 boostSaturation(float3 c, float amount)
            {
                float l = luma(c);
                return lerp(float3(l, l, l), c, amount);
            }

            // Sobel edge magnitude at current pixel (on original color)
            float edgeMagnitude(float2 uv, float2 texel)
            {
                float3 s00 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv + float2(-1,-1)*texel).rgb;
                float3 s10 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv + float2( 0,-1)*texel).rgb;
                float3 s20 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv + float2( 1,-1)*texel).rgb;
                float3 s01 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv + float2(-1, 0)*texel).rgb;
                float3 s21 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv + float2( 1, 0)*texel).rgb;
                float3 s02 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv + float2(-1, 1)*texel).rgb;
                float3 s12 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv + float2( 0, 1)*texel).rgb;
                float3 s22 = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv + float2( 1, 1)*texel).rgb;

                float3 gx = -s00 + s20 - 2.0*s01 + 2.0*s21 - s02 + s22;
                float3 gy = -s00 - 2.0*s10 - s20 + s02 + 2.0*s12 + s22;
                float3 g  = sqrt(gx*gx + gy*gy);
                return (g.r + g.g + g.b) / 3.0;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv    = input.uv;
                float2 texel = _BlitTexture_TexelSize.xy;

                // --- 1. Pixelate ---
                float2 pixelUV = floor(uv * _BlitTexture_TexelSize.zw / _PixelSize)
                                 * _PixelSize * texel;
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, pixelUV);

                // --- 2. Saturation boost (makes colors pop before quantizing) ---
                color.rgb = boostSaturation(color.rgb, _SaturationBoost);

                // --- 3. Edge detection — strong edges get less dithering ---
                float edge    = edgeMagnitude(pixelUV, texel * _PixelSize);
                float edgeMask = saturate(edge * 8.0); // 0=flat area, 1=hard edge

                // --- 4. 8x8 Bayer dither ---
                uint2 bayerPos  = uint2(uv * _BlitTexture_TexelSize.zw) % 8;
                float threshold = bayer8x8[bayerPos.y * 8 + bayerPos.x];

                float levels   = _PaletteSize - 1.0;
                // Reduce dither strength near edges to preserve text/UI crispness
                float localDither = _DitherStrength * (1.0 - edgeMask * _EdgeProtect);
                float3 dithered   = color.rgb + (threshold - 0.5) * localDither / levels;
                float3 quantized  = round(dithered * levels) / levels;

                // --- 5. Very subtle scanlines (much gentler than before) ---
                float screenY   = uv.y * _BlitTexture_TexelSize.w;
                float scanline  = 1.0 - (fmod(screenY, 2.0) < 1.0 ? _ScanlineStrength : 0.0);
                quantized      *= scanline;

                // --- 6. Soft vignette ---
                float2 uvC    = uv - 0.5;
                float vignette = 1.0 - dot(uvC, uvC) * _VignetteAmount * 2.5;
                quantized     *= saturate(vignette);

                return float4(quantized, 1.0);
            }
            ENDHLSL
        }
    }
}