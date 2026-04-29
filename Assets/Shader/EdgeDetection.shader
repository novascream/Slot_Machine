Shader "PostProcess/EdgeDetection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EdgeColor ("Edge Color", Color) = (0,0,0,1)
        _Threshold ("Threshold", Range(0, 1)) = 0.1
        _EdgeThickness ("Edge Thickness", Range(0, 5)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "EdgeDetectionPass"
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _EdgeColor;
            float _Threshold;
            float _EdgeThickness;

            #pragma vertex vert
            #pragma fragment frag

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 frag (Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float4 center = tex2D(_MainTex, uv);
                
                // Sample 4 neighboring pixels
                float2 offset = _MainTex_TexelSize.xy * _EdgeThickness;
                float4 top = tex2D(_MainTex, uv + float2(0, offset.y));
                float4 bottom = tex2D(_MainTex, uv - float2(0, offset.y));
                float4 left = tex2D(_MainTex, uv - float2(offset.x, 0));
                float4 right = tex2D(_MainTex, uv + float2(offset.x, 0));

                // Basic Sobel-style difference calculation
                float diff = distance(top, bottom) + distance(left, right);

                if (diff > _Threshold)
                {
                    return _EdgeColor; // Draw the outline
                }

                return center; // Draw the original game image
            }
            ENDHLSL
        }
    }
}