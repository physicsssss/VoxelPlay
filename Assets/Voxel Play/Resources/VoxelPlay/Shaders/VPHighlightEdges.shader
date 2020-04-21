Shader "Voxel Play/Misc/Highlight Voxel Edges"
{
	Properties
	{
		_Color ("Color", Color) = (1,0,0,0.5)
		_Width ("Width", Float) = 0.05
	}
	SubShader
	{
		Tags { "Queue"="Transparent+2" "RenderType"="Transparent" }

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			Offset 0, -2
			ZWrite Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			half4 _Color;
			half _Width;

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
				half4  color  : COLOR;
				UNITY_VERTEX_OUTPUT_STEREO
			};


			v2f vert (appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				v.vertex.xyz *= 1.01;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color  = half4(_Color.rgb, (sin(_Time.w * 2.0) + 1.0) * 0.2 + 0.25 );
				return o;
			}
			
			half4 frag (v2f i) : SV_Target
			{
				half2 grd = abs(frac(i.uv + 0.5) - 0.5);
				grd /= fwidth(i.uv);
				half  lin = min(grd.x, grd.y);
				half  edge = 1.0 - min(lin.xxx * _Width , 1.0);
				i.color.a *= edge;
				return i.color;
			}
			ENDCG
		}
	}
}
