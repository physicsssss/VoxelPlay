#include "VPCommon.cginc"
#include "VPCommonRealisticWater.cginc"

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
	float3 viewDir : TEXCOORD4;
	float4 bumpuv : TEXCOORD5;
	float4 foam   : TEXCOORD6;
	float4 foamCorners   : TEXCOORD7;
	float2 flow   : TEXCOORD8;
	float4 grabPos: TEXCOORD9;
	#if defined(USE_SHADOWS)
		SHADOW_COORDS(10)
	#endif
	VOXELPLAY_NORMAL_DATA
	UNITY_VERTEX_OUTPUT_STEREO
};

v2f vert (appdata v) {
	v2f o;

	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_OUTPUT(v2f, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
	VOXELPLAY_MODIFY_VERTEX(v.vertex, wpos)

	// wave effect
	v.vertex.y += _WaveAmplitude * (sin(wpos.x * 3.1415927 * 1.5 + _Time.w) * 0.025 + 0.028);

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
	o.uv.w = (w & 122880) / 122880.0; // light intensity encoded in bits 13-16 (8192+16384+32768+65536)

	VOXELPLAY_INITIALIZE_LIGHT_AND_FOG_NORMAL_NO_GI(wpos, v.normal);
	VOXELPLAY_SET_LIGHT(o, wpos, v.normal);

	o.grabPos = ComputeGrabScreenPos(o.pos);
    o.grabPos.z = lerp(o.grabPos.z, _ProjectionParams.z * (1.0 - o.grabPos.z), unity_OrthoParams.w);

	#if defined(USE_SHADOWS)
	TRANSFER_SHADOW(o);
	#endif

	float3 tang = float3( dot(float3(0,1,-1), v.normal), 0, dot(float3(1,0,0), v.normal) );
	VOXELPLAY_SET_TANGENT_SPACE(tang, v.normal)
	VOXELPLAY_OUTPUT_NORMAL_DATA(uv, o)

    // scroll waves normal
    float4 wavesOffset = _Time.xxxx * (o.flow.xyxy + float4(1,1,-0.4,-0.45) * _WaveSpeed);
    float4 temp = wpos.xzxz * _WaveScale * float4(2.0,2.0,3.0,3.0) + wavesOffset;
    o.bumpuv = temp.xyzw;

	o.viewDir = WorldSpaceViewDir(v.vertex);

	return o;
}


half4 frag (v2f i) : SV_Target {

	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    half3 worldViewDir = normalize(i.viewDir);

    // combine two scrolling bumpmaps into one
    half3 bump1 = GetWaterNormal(i.bumpuv.xy); 
    half3 bump2 = GetWaterNormal(i.bumpuv.zw);
//    half3 normal = (bump1 + bump2).xzy * 0.5;
    half3 normal = BlendNormals(bump1, bump2).xzy;

    // Fresnel factor
    float fresnelFac = dot( worldViewDir, normal * _NormalStrength );

    // Distored grabpos
    #if defined(USE_SHADOWS)
    float4 distortedUV = i.grabPos;
    i.grabPos.xy += normal.xz * _RefractionDistortion;
    #endif

    // Water color
    half4 color;
    fresnelFac = saturate(fresnelFac + _Fresnel);
    half4 water = tex2D( _ReflectiveColor, float2(fresnelFac,fresnelFac) );

	// Ocean foam
	half4 ofoam = tex2D(_FoamTex, normal.xz * 0.01);
	half3 oceanWater = _WaterColor.rgb + saturate(ofoam.rgb - _OceanWave.x) * _OceanWave.y;

    color.rgb = lerp(water.rgb, oceanWater, water.a);
    color.a   = _WaterColor.a;
    
    // Underwater fog
    half screenDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, i.grabPos));

    half depthInWater = saturate( (screenDepth -  i.grabPos.z) * _UnderWaterFogColor.a );
    color.rgb = _UnderWaterFogColor.rgb * (depthInWater * (1.0 - color.a)) + color.rgb;
    color.a = saturate(depthInWater + color.a);
    
    // Specular
    float3 h = normalize (_WorldSpaceLightPos0.xyz + worldViewDir);
    h *= (sign(_WorldSpaceLightPos0.y) + 1.0) * 0.5; // avoid specular under the horizon
    float nh = max (0, dot (normal, h));
    float spec = pow (nh, _SpecularPower);
    #if defined(USE_SHADOWS)
    spec *= SHADOW_ATTENUATION(i);
    #endif
    color.rgb += (_SpecularIntensity * spec) * _LightColor0.rgb;

    // Foam at sides (x = right, y = left, z =front, w = back)
    i.uv.xy = saturate(i.uv.xy);
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
    half4 foam2 = max(foamSides, foamCorners);

    // final foam intensity
    half foamIntensity = max( max(foam2.x, foam2.y), max(foam2.z, foam2.w) );
    foamIntensity *= 2.0;

    half3 foamGradient = 1 - tex2D(_FoamGradient, float2(foamIntensity - _Time.y*0.2, 0) + normal.xz * 0.2);
    float2 foamDistortUV = normal.xz;
    half3 foamColor = tex2D(_FoamTex, i.bumpuv.xy * 7.0 + foamDistortUV).rgb * _FoamColor;
    color.rgb += foamGradient * foamIntensity * foamColor;

    // Depth-based foam
    half depthFoam = 1.0 - foamIntensity;
    half depthDiff = screenDepth - i.grabPos.z;
    half foamAmount = depthFoam * (depthDiff>0) * saturate( (1.0 - depthDiff) * 2.0 );
    color.rgb += foamAmount * foamColor;

	VOXELPLAY_APPLY_LIGHTING_AND_GI(color, i);

	VOXELPLAY_APPLY_FOG(color, i);

    // Blend transparency
    #if defined(USE_SHADOWS)
    half4 bgColor = tex2Dproj(_WaterBackgroundTexture, i.grabPos);
    color = lerp(bgColor, color, color.a);
    #endif

	return color;

}

