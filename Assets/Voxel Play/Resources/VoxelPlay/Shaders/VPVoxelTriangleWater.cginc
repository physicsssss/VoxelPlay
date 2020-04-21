#include "VPCommon.cginc"

#define FOAM_SIZE 0.12

struct appdata {
	float4 vertex   : POSITION;
	float4 uv       : TEXCOORD0;
	float3 normal   : NORMAL;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};


struct v2f {
	float4 pos    : SV_POSITION;
	float4 uv     : TEXCOORD0;
	VOXELPLAY_LIGHT_DATA(1,2)
	VOXELPLAY_FOG_DATA(3)
	float4 foam   : TEXCOORD4;
	float4 foamCorners   : TEXCOORD5;
	float2 flow   : TEXCOORD6;
	#if defined(USE_SHADOWS)
	float4 grabPos: TEXCOORD7;
	SHADOW_COORDS(8)
	#endif
	VOXELPLAY_BUMPMAP_DATA(9)
	VOXELPLAY_PARALLAX_DATA(10)
	VOXELPLAY_NORMAL_DATA
	UNITY_VERTEX_OUTPUT_STEREO
};

struct vertexInfo {
	float4 vertex;
};

sampler2D _WaterBackgroundTexture;

v2f vert (appdata v) {
	v2f o;

	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_OUTPUT(v2f, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	VOXELPLAY_MODIFY_VERTEX(v.vertex, worldPos)

	// wave effect
	v.vertex.y += sin(worldPos.x * 3.1415927 * 1.5 + _Time.w) * 0.025 + 0.028;

	o.pos    = UnityObjectToClipPos(v.vertex);

	int w = (int)v.uv.w;
	o.foam.x = w & 1;	// back
	o.foam.y = (w >> 1) & 1; // front
	o.foam.z = (w >> 2) & 1; // left
	o.foam.w = (w >> 3) & 1; // right
	o.foam *= FOAM_SIZE;

	o.foamCorners.x = (w>>4) & 1; // BL
	o.foamCorners.y = (w>>5) & 1; // FL
	o.foamCorners.z = (w>>6) & 1; // FR
	o.foamCorners.w = (w>>7) & 1; // BR

	o.flow   = float2(((w>>8) & 3) - 1.0, ((w>>10) & 3) - 1.0);

	o.uv = v.uv;
    //o.uv.w = (w & 122880) / 122880.0; // light intensity encoded in bits 13-16 (8192+16384+32768+65536)
	o.uv.w = ((w>>13) & 15) / 15.0; // (w & 122880) / 122880.0; // light intensity encoded in bits 13-16 (8192+16384+32768+65536)
	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL_NO_GI(worldPos, v.normal);
    o.light.y = ((w>>17) & 15) / 15.0; // assign torch light contribution manually due to special water uv packing
	VOXELPLAY_SET_LIGHT(o, worldPos, v.normal);

	#if defined(USE_SHADOWS)
		TRANSFER_SHADOW(o);	
		o.grabPos = ComputeGrabScreenPos(o.pos);
	#endif

	float3 tang = float3( dot(float3(0,1,-1), v.normal), 0, dot(float3(1,0,0), v.normal) );
	VOXELPLAY_SET_TANGENT_SPACE(tang, v.normal)
	VOXELPLAY_OUTPUT_PARALLAX_DATA(v, uv, o)
	VOXELPLAY_OUTPUT_NORMAL_DATA(uv, o)

	return o;
}


fixed4 frag (v2f i) : SV_Target {

	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	// Foam
	i.uv.xy = saturate(i.uv.xy); // avoids foam artifacts

	// Foam at sides (x = right, y = left, z =front, w = back)
	fixed4 foamSides = fixed4(i.foam.w - (1.0 - i.uv.x), i.foam.z - (i.uv.x), i.foam.y - (1.0 - i.uv.y), i.foam.x - (i.uv.y));

	// Foam at corners
	fixed foamLeft = FOAM_SIZE - i.uv.x;
	fixed foamRight = FOAM_SIZE - (1.0 - i.uv.x);
	fixed foamBack = FOAM_SIZE - i.uv.y;
	fixed foamForward = FOAM_SIZE - (1.0 - i.uv.y);
	fixed4 foamCorners1 = fixed4(foamLeft, foamLeft, foamRight, foamRight);
	fixed4 foamCorners2 = fixed4(foamBack, foamForward, foamForward, foamBack);
	fixed4 foamCorners  = min(foamCorners1, foamCorners2) * i.foamCorners;

	// combine sides and corner foam
	fixed4 foam2 = max(foamSides, foamCorners);

	// final foam intensity
	fixed foam = max( max(foam2.x, foam2.y), max(foam2.z, foam2.w) );
	foam *= 4.0;

	// Animate
	i.uv.xy    = i.uv.xy - _Time.yy * i.flow + _Time.xx;

	// Diffuse
	VOXELPLAY_APPLY_PARALLAX(i);
	fixed4 color   = VOXELPLAY_GET_TEXEL_DD(i.uv.xyz);
	color.rgb += saturate(foam);
    
	VOXELPLAY_APPLY_NORMAL(i);

	VOXELPLAY_APPLY_LIGHTING_AND_GI(color, i);

	VOXELPLAY_APPLY_FOG(color, i);

	// Blend transparency
	#if defined(USE_SHADOWS)
	    fixed4 bgColor = tex2Dproj(_WaterBackgroundTexture, i.grabPos);
	    color = lerp(bgColor, color, color.a);
	#endif

	return color;
}

