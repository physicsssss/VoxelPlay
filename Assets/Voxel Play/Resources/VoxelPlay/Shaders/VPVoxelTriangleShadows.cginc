#include "VPCommon.cginc"

struct appdata {
	float4 vertex   : POSITION;
	float3 normal   : NORMAL;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f {
	V2F_SHADOW_CASTER;
	UNITY_VERTEX_OUTPUT_STEREO
};


v2f vert (appdata v) {
	v2f o;

	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_OUTPUT(v2f, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    #if defined(IS_CLOUD)
        v.vertex.xyz *= float3(4, 2, 4);
    #endif
	VOXELPLAY_MODIFY_VERTEX_NO_WPOS(v.vertex)
	TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);
	return o;
}

fixed4 frag (v2f i) : SV_Target {
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
	SHADOW_CASTER_FRAGMENT(i)
}

