Shader "Voxel Play/FX/DamagedVoxel"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,0.5)
		_VoxelLight ("Voxel Ambient Light", Float) = 1
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Offset -1, -1

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ VERTEXLIGHT_ON
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			#include "VPCommonVertexModifier.cginc"

			sampler2D _MainTex;
			fixed3 _Color;
			fixed _VoxelLight;
			fixed _VPAmbientLight;

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv     : TEXCOORD0;
				fixed3 diff : COLOR0;
                #if defined(VERTEXLIGHT_ON)
	                fixed3 vertexLightColor: TEXCOORD1;
                #endif
				UNITY_VERTEX_OUTPUT_STEREO
			};


			v2f vert (appdata_base v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				VOXELPLAY_MODIFY_VERTEX(v.vertex, worldPos)

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord.xy;
 				// Daylight
				fixed  daylight    = max(0, _WorldSpaceLightPos0.y * 2.0);
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half nl = 0.25 + max(0.25, dot(worldNormal, _WorldSpaceLightPos0.xyz)) * daylight;
                // factor in the light color
                o.diff = max(saturate(nl), _VPAmbientLight) * _VoxelLight * _LightColor0.rgb * _Color;
				#if defined(VERTEXLIGHT_ON)
                o.vertexLightColor = Shade4PointLights(unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,unity_LightColor[0].rgb, unity_LightColor[1].rgb,unity_LightColor[2].rgb, unity_LightColor[3].rgb,unity_4LightAtten0, worldPos, worldNormal);
                #endif
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv) * 0.5;
				#if defined(VERTEXLIGHT_ON)
                col.rgb *= i.diff + i.vertexLightColor;
                #else
                col.rgb *= i.diff;
                #endif
				return col;
			}
			ENDCG
		}
	}
}
