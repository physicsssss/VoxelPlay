Shader "Voxel Play/Voxels/Triangle/Transp Double Sided"
{
	Properties
	{
		[HideInInspector] _MainTex ("Main Texture Array", Any) = "white" {}
		_VPSeeThroughScreenMask ("Screen Mask", 2D) = "white" {}
	}
	SubShader {

		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		Pass {
			Offset -1, -1
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			Cull Off
			CGPROGRAM
			#pragma target 3.5
			#pragma vertex   vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile _ VOXELPLAY_GLOBAL_USE_FOG
			#pragma multi_compile _ VOXELPLAY_USE_AA
			#pragma multi_compile _ VOXELPLAY_USE_OUTLINE
			#pragma multi_compile _ VOXELPLAY_PIXEL_LIGHTS
			#pragma multi_compile _ VOXELPLAY_TRANSP_BLING
			#include "VPVoxelTriangleTransp.cginc"
			ENDCG
		}
	}
	Fallback Off
}