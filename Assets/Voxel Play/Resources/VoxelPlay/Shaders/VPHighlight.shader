Shader "Voxel Play/Misc/Highlight Voxel"
{
	Properties
	{
		_Color ("Color", Color) = (1,0,0,0.5)
	}
	SubShader
	{
		Tags { "Queue"="Transparent+2" "RenderType"="Transparent" }

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			ZTest Always
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			fixed4 _Color;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv     : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv     : TEXCOORD0;
				fixed4 color  : COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
			};


			v2f vert (appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color  = fixed4(_Color.rgb, (sin(_Time.w * 2.0) + 1.0) * 0.2 + 0.25 );
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float2 grd = abs(frac(i.uv + 0.5) - 0.5);
				grd /= fwidth(i.uv);
				float  lin = min(grd.x, grd.y);
				float4 col = float4(min(lin.xxx * 0.1 , 1.0), 1.0);
				return lerp(col, i.color, col.r);
			}
			ENDCG
		}
	}
}
