#include "VPCommon.cginc"

struct appdata {
	float4 vertex   : POSITION;
	float4 uv       : TEXCOORD0;
	float3 normal   : NORMAL;
	VOXELPLAY_TINTCOLOR_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


struct v2f {
	float4 pos     : SV_POSITION;
	float4 uv      : TEXCOORD0;
	VOXELPLAY_LIGHT_DATA(1,2)
	VOXELPLAY_FOG_DATA(3)
	VOXELPLAY_SEE_THROUGH_DATA(4,5)
	fixed4 color   : COLOR; 	// always passed to support custom alpha
	VOXELPLAY_NORMAL_DATA
	UNITY_VERTEX_OUTPUT_STEREO
};


v2f vert (appdata v) {
	v2f o;

	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_OUTPUT(v2f, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	VOXELPLAY_MODIFY_VERTEX(v.vertex, worldPos)
	o.pos    = UnityObjectToClipPos(v.vertex);

	VOXELPLAY_OUTPUT_SEE_THROUGH_DATA(o, worldPos)

	float4 uv = v.uv;

	VOXELPLAY_OUTPUT_TINTCOLOR(o);
	#if defined(USES_TINTING)
	o.color.a *= uv.y;
	#else 
	o.color = fixed4(1.0.xxx, uv.y);
	#endif

	int iuvx = (int)uv.x;
	uv.x = iuvx >> 1;
	uv.y = iuvx & 1;

	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL(uv, worldPos, v.normal);
	VOXELPLAY_SET_LIGHT(o, worldPos, v.normal);
    VOXELPLAY_OUTPUT_UV(uv, o);
	return o;
}


fixed4 frag (v2f i) : SV_Target {

	// Diffuse
//	i.uv.xy = frac(i.uv.xy);
//	fixed4 color   = UNITY_SAMPLE_TEX2DARRAY(_MainTex, i.uv.xyz);

	fixed4 color   = VOXELPLAY_GET_TEXEL(i.uv.xyz);

	#if VOXELPLAY_TRANSP_BLING
	color.ba += (1.0 - color.a) * 0.1 * (frac(_Time.x)>0.99) * (frac(_Time.y + (i.uv.x + i.uv.y) * 0.1) > 0.9);
	#endif

	color *= i.color;

	VOXELPLAY_APPLY_SEE_THROUGH(color, i)

	VOXELPLAY_APPLY_LIGHTING_AND_GI(color, i);

	VOXELPLAY_APPLY_FOG(color, i);

	return color;
}

