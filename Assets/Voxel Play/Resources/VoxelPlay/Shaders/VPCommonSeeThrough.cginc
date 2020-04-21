#ifndef VOXELPLAY_SEE_THROUGH
#define VOXELPLAY_SEE_THROUGH

// So materials expose the property even if the feature is disabled
sampler2D _VPSeeThroughScreenMask;

#if defined(USES_SEE_THROUGH)
	uniform float4 _VPSeeThroughData;

	#define WORLD_VIEW_DIR _VPSeeThroughData.xyz
	#define CYLINDER_RADIUS_SQR _VPSeeThroughData.w
	#define DISTANCE_TO_TARGET _VPSeeThroughCharData.w

	#define VOXELPLAY_SEE_THROUGH_DATA(idx1,idx2) float3 worldPos : TEXCOORD##idx1; float4 scrPos : TEXCOORD##idx2;
	#define VOXELPLAY_OUTPUT_SEE_THROUGH_DATA(o, worldPos) o.worldPos = worldPos; o.scrPos = ComputeScreenPos(o.pos); o.scrPos.xy = o.scrPos.xy * 2.0 + _Time.xx;

	void ApplySeeThrough(inout fixed4 color, float3 worldPos, float4 scrPos) {
		float t = dot( WORLD_VIEW_DIR, worldPos - _WorldSpaceCameraPos );
		float3 p = _WorldSpaceCameraPos + WORLD_VIEW_DIR * t;
		float orthoDistSqr = dot(p - worldPos, p - worldPos);
		float screenMask = tex2Dproj(_VPSeeThroughScreenMask, scrPos).r + 0.5;
		float radiusSqr = CYLINDER_RADIUS_SQR * screenMask;
		color.a *= saturate(orthoDistSqr - radiusSqr);
	}

	#define VOXELPLAY_APPLY_SEE_THROUGH(color, i) ApplySeeThrough(color, i.worldPos, i.scrPos);


#else
	#define VOXELPLAY_SEE_THROUGH_DATA(idx1,idx2)
	#define VOXELPLAY_OUTPUT_SEE_THROUGH_DATA(o, worldPos)
	#define VOXELPLAY_APPLY_SEE_THROUGH(color, i)
#endif

#endif // VOXELPLAY_SEE_THROUGH

