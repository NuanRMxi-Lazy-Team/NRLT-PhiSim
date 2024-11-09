Shader "Custom/GaussianBlurWithBrightness"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Float) = 3.5
        _Brightness ("Brightness", Float) = 0.4
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
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
            float4 _MainTex_ST;
            float _BlurSize;
            float _Brightness;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float4 color = float4(0, 0, 0, 0);

                // Sample the texture at different offsets to create the blur effect
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        color += tex2D(_MainTex, uv + float2(x, y) * _BlurSize / _ScreenParams.xy);
                    }
                }

                color /= 25.0; // Normalize the color

                // Apply brightness adjustment
                color.rgb *= _Brightness;

                return color;
            }
            ENDCG
        }
    }
}
