Shader "Voxel Play/Skybox/SkyboxEarthDayNightCubemap" {
Properties {
	[NoScaleOffset] _DayTex ("Day Cubemap   (HDR)", Cube) = "white" {}
	[NoScaleOffset] _NightTex ("Night Cubemap   (HDR)", Cube) = "black" {}
//	_VPSkyTint ("Sky Tint", Color) = (0.52, 0.5, 1.0)
//	_VPExposure("Exposure", Range(0, 8)) = 1.3
//	_VPFogAmount("Fog Amount", Range(0,1)) = 0.5
	_StarBlockSize ("Star Block Size", Range(100,300)) = 200
	_StarAmount("Star Amount", Range(0.9,1)) = 0.997
	_SunFlare("Sun Flare", Range(0, 1.0)) = 0.4
	_MoonFlare("Moon Flare", Range(0, 1.0)) = 0.1
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

		samplerCUBE _DayTex;
		half4 _DayTex_HDR;
		samplerCUBE _NightTex;
		half4 _NightTex_HDR;
		half3 _VPSkyTint;
		half _VPExposure;
		half _VPFogAmount;
		float _StarBlockSize,  _StarAmount;
		half _SunFlare;
		half _MoonFlare;

		struct appdata
		{
			float4 vertex   : POSITION;
			float2 texcoord : TEXCOORD0;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct v2f
		{
			float4 pos : SV_POSITION;
			float2 uv: TEXCOORD0;
			float3 ray : TEXCOORD1;
			float4 scrPos : TEXCOORD2;
			float4 lightPos : TEXCOORD3;
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
			o.uv = v.texcoord;
			o.scrPos = o.pos; // / o.pos.w;
			o.lightPos = UnityObjectToClipPos(_WorldSpaceLightPos0.xyz);
			return o;
		}

		half4 frag (v2f i) : SV_Target
		{
			float3 ray    = i.ray;
			float3 delta  = _WorldSpaceLightPos0.xyz - ray;
			float dist    = dot(delta, delta);

			// base color
			half4 texDay = texCUBE (_DayTex, ray);
			half3 skyColor = DecodeHDR (texDay, _DayTex_HDR) * _VPSkyTint;

			// fog
			float y = abs(ray.y);
			half fog = saturate(_VPFogAmount - y) / (1.0001 - _VPFogAmount);
			skyColor = lerp(skyColor, 1.0, fog);

			// sky tint
			float hy = abs(_WorldSpaceLightPos0.y) + y;
			half t = saturate( (0.4 - hy) * 2.2) / (1.0 + dist * 0.8);
			skyColor.r = lerp(skyColor.r, 1.0, t);
			skyColor.b = lerp(skyColor.b, 0.0, t);

			// daylight + obscure opposite side of sky
			half daylight = saturate(1.0 +_WorldSpaceLightPos0.y * 2.0 - dist * 0.03);
			skyColor *= daylight;

			// sun
			half sunFlare = _SunFlare / pow(1.0 + dist, 64.0);
			float2 scrDist = i.scrPos.xy/i.scrPos.w - i.lightPos.xy/i.lightPos.w;
			scrDist.x *= _ScreenParams.x/_ScreenParams.y;
			float2 scrDistVox = round(abs(scrDist) * 16.0) / 16.0;
			float distVox = max(scrDistVox.x, scrDistVox.y);
			half sunIntensity = (distVox + dist * 13.0 < 0.13);
			half3 sunColor = sunFlare + sunIntensity;

			// night skybox
			half4 tex = texCUBE (_NightTex, ray);
			half3 nightColor = DecodeHDR (tex, _NightTex_HDR);

			// final color
			skyColor = lerp(nightColor, skyColor, daylight);
			half3 col = skyColor + sunColor * saturate(ray.y + 0.2);

			// exposure
			col *= _VPExposure * _LightColor0.rgb;

			// gamma
			#if defined(UNITY_COLORSPACE_GAMMA)
			col = sqrt(col);
			#endif

			return half4(col,1.0);
		}
		ENDCG
	}
}

Fallback Off
}


