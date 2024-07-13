// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/UIFrostedGlass"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
		
		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp] 
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};
			struct v2f
			{
				float4 vertex   : SV_POSITION;
				half2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
			};

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.worldPosition = IN.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
				OUT.texcoord = half2(IN.texcoord.x, IN.texcoord.y);				
				return OUT;
			}

			sampler2D _MainTex;
			uniform half4 _MainTex_TexelSize;
			static const half4 curve4[7] = { 	half4(0.0205,0.0205,0.0205,0), 
												half4(0.0855,0.0855,0.0855,0), 
												half4(0.232,0.232,0.232,0),
												half4(0.324,0.324,0.324,1), 
												half4(0.232,0.232,0.232,0), 
												half4(0.0855,0.0855,0.0855,0), 
												half4(0.0205,0.0205,0.0205,0) };
			fixed4 frag(v2f IN) : SV_Target
			{
				half4 blurColor1 = 0;
				for(int i = 0; i < 7; i++){
					blurColor1 += tex2D(_MainTex, IN.texcoord + 0.005 * half2(i-3, 0)) * curve4[i];
				}

				half4 blurColor2 = 0;
				for(int i = 0; i < 7; i++){
					blurColor2 += tex2D(_MainTex, IN.texcoord + 0.005 * half2(0, i-3)) * curve4[i];
				}

				half4 blurColor = (blurColor1 * 0.5 + blurColor2 * 0.5) * 0.9;
				blurColor.a = 1.0;
				return blurColor;
			}
		ENDCG
		}
	}
}