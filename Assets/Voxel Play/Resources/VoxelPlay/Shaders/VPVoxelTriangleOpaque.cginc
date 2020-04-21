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
	SHADOW_COORDS(4)
	VOXELPLAY_TINTCOLOR_DATA
	VOXELPLAY_BUMPMAP_DATA(5)
	VOXELPLAY_PARALLAX_DATA(6)
	VOXELPLAY_NORMAL_DATA
	UNITY_VERTEX_OUTPUT_STEREO
};

struct vertexInfo {
	float4 vertex;
};

v2f vert (appdata v) {
	v2f o;

	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_OUTPUT(v2f, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    #if defined(IS_CLOUD)
        v.vertex.xyz *= float3(4, 2, 4);
    #endif
	float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
	VOXELPLAY_MODIFY_VERTEX(v.vertex, wpos)

	float4 uv = v.uv;
	#if defined(VP_CUTOUT)
		int iuvz = (int)uv.z;
		float disp = (iuvz>>16) * sin(wpos.x + wpos.y + _Time.w) * _VPTreeWindSpeed;
		v.vertex.xy += disp;
		uv.z = iuvz & 65535; // remove wind animation flag
    #elif defined(USE_ANIMATION)
        int iuvz = (int)uv.z;
        int frameCount = (iuvz>>14) & 0xF;
        float speed = (iuvz>>18);
        speed = speed * speed / 8.0;
        uv.z = (iuvz & 16383) + ((uint)(_Time.y * speed) % frameCount);
	#endif

	o.pos    = UnityObjectToClipPos(v.vertex);

    #if defined(USE_WORLD_SPACE_NORMAL)
        v.normal = UnityObjectToWorldNormal(v.normal);
    #endif

	VOXELPLAY_OUTPUT_TINTCOLOR(o);
	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL(uv, wpos, v.normal);
	VOXELPLAY_SET_LIGHT(o, wpos, v.normal);
	TRANSFER_SHADOW(o);

	float3 tang = float3( dot(float3(0,1,-1), v.normal), 0, dot(float3(1,0,0), v.normal) );
	VOXELPLAY_SET_TANGENT_SPACE(tang, v.normal)

    #if defined(USE_WORLD_SPACE_UV)
        uv.xy = wpos.xz * v.normal.y + wpos.xy * v.normal.z + wpos.zy * v.normal.x;
        uv.xy = TRANSFORM_TEX(uv.xy, _MainTex);
    #endif

	VOXELPLAY_OUTPUT_PARALLAX_DATA(v, uv, o)
	VOXELPLAY_OUTPUT_NORMAL_DATA(uv, o)
	VOXELPLAY_OUTPUT_UV(uv, o)

	return o;
}


fixed4 frag (v2f i) : SV_Target {
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	VOXELPLAY_APPLY_PARALLAX(i);

	// Diffuse
	fixed4 color   = VOXELPLAY_GET_TEXEL_DD(i.uv.xyz);
	#if defined(VP_CUTOUT)
	    clip(color.a - 0.5);
	#endif

	VOXELPLAY_COMPUTE_EMISSION(color)

	VOXELPLAY_APPLY_NORMAL(i);

	VOXELPLAY_APPLY_TINTCOLOR(color, i);

	VOXELPLAY_APPLY_OUTLINE_SIMPLE(color, i);

    #if defined(IS_CLOUD)
        VOXELPLAY_APPLY_LIGHTING(color, i);
    #else
	    VOXELPLAY_APPLY_LIGHTING_AO_AND_GI(color, i);
    #endif

	VOXELPLAY_ADD_EMISSION(color)

	VOXELPLAY_APPLY_FOG(color, i);

	return color;
}

