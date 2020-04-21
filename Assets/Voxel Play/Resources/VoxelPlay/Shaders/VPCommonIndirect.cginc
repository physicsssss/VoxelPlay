#ifndef VOXELPLAY_COMMON_INDIRECT
#define VOXELPLAY_COMMON_INDIRECT

#if SHADER_TARGET >= 45
    StructuredBuffer<float4> _Positions;
    StructuredBuffer<fixed4> _ColorsAndLight;
    StructuredBuffer<float4> _Rotations;

    float3x3 ConvertQuaternion(float4 q) {
            // Precalculate coordinate products
            float x = q.x * 2.0F;
            float y = q.y * 2.0F;
            float z = q.z * 2.0F;
            float xx = q.x * x;
            float yy = q.y * y;
            float zz = q.z * z;
            float xy = q.x * y;
            float xz = q.x * z;
            float yz = q.y * z;
            float wx = q.w * x;
            float wy = q.w * y;
            float wz = q.w * z;
 
            // Calculate 3x3 matrix from orthonormal basis
            float3x3 m;
 
            float3 row1;
            m[0][0] = 1.0f - (yy + zz);
            m[0][1] = xy + wz;
            m[0][2] = xz - wy;
 
            float3 row2;
            m[1][0] = xy - wz;
            m[1][1] = 1.0f - (xx + zz);
            m[1][2] = yz + wx;
 
            float3 row3;
            m[2][0] = xz + wy;
            m[2][1] = yz - wx;;
            m[2][2] = 1.0f - (xx + yy);
 
            return m;
 }

    #define VOXELPLAY_COMPUTE_WORLD_MATRIX(position, rotation) VPObjectToWorldMatrix(position, rotation);
    float3x3 VPObjectToWorld;
    float4x4 unity_ObjectToWorld_2;
    void VPObjectToWorldMatrix(float4 position, float4 rotationQuaternion) {
        VPObjectToWorld = ConvertQuaternion(rotationQuaternion);
        unity_ObjectToWorld_2 = unity_ObjectToWorld;
       	unity_ObjectToWorld_2._14_24_34_44 = float4(position[0], position[1], position[2], 1);
        unity_ObjectToWorld_2._11_21_31_41 = float4(VPObjectToWorld[0][0], VPObjectToWorld[0][1], VPObjectToWorld[0][2], 0);
        unity_ObjectToWorld_2._12_22_32_42 = float4(VPObjectToWorld[1][0], VPObjectToWorld[1][1], VPObjectToWorld[1][2], 0);
        unity_ObjectToWorld_2._13_23_33_43 = float4(VPObjectToWorld[2][0], VPObjectToWorld[2][1], VPObjectToWorld[2][2], 0);
    }

#else
	#define VOXELPLAY_COMPUTE_WORLD_MATRIX(position, rotation)
    #define unity_ObjectToWorld_2 unity_ObjectToWorld
#endif

#endif // VOXELPLAY_COMMON_INDIRECT

