#include "VPCommonVertexModifier.cginc"
#include "VPCommonIndirect.cginc"


void vert (inout float4 v : POSITION, uint instanceID : SV_InstanceID) {

	#if SHADER_TARGET >= 45
       	float4 position = _Positions[instanceID];
       	v.xyz *= position.w;
       	#if VOXELPLAY_USE_ROTATION
	    	float4 rotationQuaternion = _Rotations[instanceID];
       		VOXELPLAY_COMPUTE_WORLD_MATRIX(position, rotationQuaternion)
        	float3 worldPos = mul(unity_ObjectToWorld, v).xyz;
        #else
        	float3 worldPos = position.xyz + v.xyz;
        #endif
    #else
	    float3 worldPos = v.xyz;
    #endif

	VOXELPLAY_MODIFY_WPOS(worldPos)
	v    = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0f));
}

fixed4 frag (float4 v : SV_POSITION) : SV_Target {
	return 0;
}

