using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {
	[ExecuteInEditMode]
	public class ExportlGlobalSettings : MonoBehaviour {

		public int lightCount;
		public Vector4[] lightPosBuffer;
		public Vector4[] lightColorBuffer;
		public float emissionIntensity;
		public Color skyTint;
		public Vector4 fogData;
		public float fogAmount;
		public float exposure;
		public float ambientLight;
		public float daylightShadowAtten;
		public bool enableFog;

		void OnEnable () {
			UpdateSettings ();
		}

		void OnValidate () {
			UpdateSettings ();
		}

		void UpdateSettings () {
			// Avoid interfering with Voxel Play environment.			
			if (VoxelPlayEnvironment.instance != null) {
				return;
			}
			if (lightPosBuffer != null && lightPosBuffer.Length > 0) {
				Shader.SetGlobalVectorArray ("_VPPointLightPosition", lightPosBuffer);
			}
			if (lightColorBuffer != null && lightColorBuffer.Length > 0) {
				Shader.SetGlobalVectorArray ("_VPPointLightColor", lightColorBuffer);
			}
			Shader.SetGlobalInt ("_VPPointLightCount", lightCount);
			Shader.SetGlobalFloat ("_VPEmissionIntensity", emissionIntensity);
			Shader.SetGlobalColor ("_VPSkyTint", skyTint);
			Shader.SetGlobalVector ("_VPFogData", fogData);
			Shader.SetGlobalFloat ("_VPFogAmount", fogAmount);
			Shader.SetGlobalFloat ("_VPExposure", exposure);
			Shader.SetGlobalFloat ("_VPAmbientLight", ambientLight);
			Shader.SetGlobalFloat ("_VPDaylightShadowAtten", daylightShadowAtten);
			if (enableFog) {
				Shader.EnableKeyword ("VOXELPLAY_GLOBAL_USE_FOG");
			} else {
				Shader.DisableKeyword ("VOXELPLAY_GLOBAL_USE_FOG");
			}
		}
	

	}

}