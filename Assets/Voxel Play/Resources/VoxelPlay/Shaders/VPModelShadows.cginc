#include "UnityCG.cginc"
#include "VPCommon.cginc"

struct appdata {
	float4 vertex   : POSITION;
    #if defined(VP_CUTOUT)
        float2 uv       : TEXCOORD1;
    #endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f {
	V2F_SHADOW_CASTER;
    #if defined(VP_CUTOUT)
        float2 uv     : TEXCOORD1;
    #endif
	UNITY_VERTEX_OUTPUT_STEREO
};



v2f vert (appdata v) {
	v2f o;

	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_OUTPUT(v2f, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	VOXELPLAY_MODIFY_VERTEX_NO_WPOS(v.vertex)

	TRANSFER_SHADOW_CASTER(o);
    #if defined(VP_CUTOUT)
        o.uv = v.uv;
    #endif

	return o;
}

fixed4 frag (v2f i) : SV_Target {
    #if defined(VP_CUTOUT)
        fixed4 color = VOXELPLAY_GET_TEXEL_2D(i.uv);
        clip(color.a - 0.5);
    #endif
	SHADOW_CASTER_FRAGMENT(i)
}

