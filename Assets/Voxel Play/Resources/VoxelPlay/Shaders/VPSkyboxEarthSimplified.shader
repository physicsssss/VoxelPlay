Shader "Voxel Play/Skybox/SkyboxEarthSimplified" {
Properties {
}

SubShader {
	Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
	Cull Off ZWrite Off Fog { Mode Off }

	Pass {

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma target 3.0
		#include "UnityCG.cginc"
		#include "Lighting.cginc"

		half3 _VPSkyTint;
		half _VPExposure;
		half _VPFogAmount;

		struct appdata
		{
			float4 vertex   : POSITION;
			float2 texcoord : TEXCOORD0;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct v2f
		{
			float4 pos : SV_POSITION;
			half3 ray : TEXCOORD1;
			UNITY_VERTEX_OUTPUT_STEREO
		};

		v2f vert (appdata v)
		{
			v2f o;
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_OUTPUT(v2f, o);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			o.pos = UnityObjectToClipPos(v.vertex);
			o.ray = v.vertex.xyz;
			return o;
		}


		fixed4 frag (v2f i) : SV_Target
		{
			half3 ray    = i.ray;
			half3 delta  = _WorldSpaceLightPos0.xyz - ray;
			half dist    = dot(delta, delta);

			// base color
			fixed3 skyColor = _VPSkyTint;

			// fog
			float y = abs(ray.y);
			half fog = saturate(_VPFogAmount - y) / (1.0001 - _VPFogAmount);
			skyColor = lerp(skyColor, 1.0, fog);

			// sky tint
			float hy = abs(_WorldSpaceLightPos0.y) + abs(ray.y);
			fixed t = saturate( (0.4 - hy) * 2.2) / (1.0 + dist * 0.8);
			skyColor.r = lerp(skyColor.r, 1.0, t);
			skyColor.b = lerp(skyColor.b, 0.0, t);

			// daylight + obscure opposite side of sky
			fixed daylight = saturate(1.0 +_WorldSpaceLightPos0.y * 2.0 - dist * 0.03);
			skyColor *= daylight;

			// exposure
			skyColor *= _VPExposure * _LightColor0.rgb;

			return fixed4(skyColor, 1.0);
		}
		ENDCG
	}
}

Fallback Off
}


