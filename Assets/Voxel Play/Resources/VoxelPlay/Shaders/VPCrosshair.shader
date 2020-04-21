Shader "Voxel Play/UI/Crosshair"
{
	Properties
	{
		_MainTex ("Texture (RGBA)", 2D) = "black" {}
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "Queue"="Overlay" "RenderType"="Transparent" }

		Grabpass { "_BackgroundTexture" } 

		Pass
		{
			ZWrite Off
			Cull Off
			ZTest Always

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#include "UnityCG.cginc"

			fixed4 _Color;
			sampler2D _MainTex;
			sampler2D _BackgroundTexture;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 texcoord  : TEXCOORD0;
				float4 grabPos : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};


			v2f vert (appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.pos = UnityObjectToClipPos(v.vertex);
				float4 cpos = UnityObjectToClipPos(float4(0,0,0,1));
				o.pos.xy = cpos.xy + (o.pos.xy - cpos.xy) * o.pos.w;	// make it uniform size regardless of world position
				o.grabPos = ComputeGrabScreenPos(o.pos);
				o.texcoord = v.texcoord;
				return o;
			}

			fixed getLuma(float3 rgb) {
				const fixed3 lum = float3(0.299, 0.587, 0.114);
				return dot(rgb, lum);
			}

			fixed4 frag (v2f i) : SV_Target {
				fixed4 color = tex2D(_MainTex, i.texcoord) * _Color;
				fixed4 bgColor = tex2Dproj(_BackgroundTexture, i.grabPos);
				fixed luma = getLuma(bgColor.rgb);
				if (luma>0.8) color.rgb *= 0.35;
				return lerp(bgColor, color, color.a);
			}
			ENDCG
		}
	}
}
