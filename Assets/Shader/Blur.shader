Shader "Custom/HighQualityGaussianBlurWithBrightness"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 1.0
        _Brightness ("Brightness", Range(0, 1)) = 1.0 // 控制亮度，1为默认亮度
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // Unity will set this automatically
            float _BlurSize;
            float _Brightness;  // 新增亮度参数

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float Gaussian(float x, float sigma)
            {
                return exp(- (x * x) / (2.0 * sigma * sigma)) / (sqrt(6.28318530718) * sigma);
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 color = float4(0, 0, 0, 0);
                float totalWeight = 0.0;

                float2 dir = float2(_BlurSize * _MainTex_TexelSize.x, _BlurSize * _MainTex_TexelSize.y);
                float sigma = 2.0; // Standard deviation for Gaussian distribution
                int radius = 10; // Blur radius, can increase for stronger blur

                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        float weight = Gaussian(sqrt(x * x + y * y), sigma);
                        float2 uvOffset = i.uv + float2(x, y) * dir;
                        color += tex2D(_MainTex, uvOffset) * weight;
                        totalWeight += weight;
                    }
                }

                // Normalize the result to ensure the total weight adds up to 1
                color /= totalWeight;

                // 调整亮度，控制图片的整体亮度
                color.rgb *= _Brightness;

                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
