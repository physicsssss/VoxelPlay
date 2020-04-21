using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;


namespace VoxelPlay.GPULighting {

	public class VoxelPlayLightManager : MonoBehaviour {

		int lastX, lastY, lastZ;
		List<Light> lights;
		Vector3 camPos;
		Vector4[] lightPosBuffer;
		Vector4[] lightColorBuffer;
		bool rebuildBuffer;
		VoxelPlayEnvironment env;

        void OnEnable () {
            if (lights == null) {
                lights = new List<Light> ();
            }
            if (lightPosBuffer == null || lightPosBuffer.Length < 32) {
                lightPosBuffer = new Vector4 [32];
            }
            if (lightColorBuffer == null || lightColorBuffer.Length < 32) {
                lightColorBuffer = new Vector4 [32];
            }
        }

        private void Start () { 
            env = VoxelPlayEnvironment.instance;
            if (env != null) {
                if (!VoxelPlayEnvironment.supportsBrightPointLights) {
                    DestroyImmediate (this);
                    return;
                }
                env.OnTorchAttached += (chunk, lightSource) => rebuildBuffer = true;
                env.OnTorchDetached += (chunk, lightSource) => rebuildBuffer = true;
                env.OnChunkRender += (chunk) => rebuildBuffer = true;
                env.OnLightRefreshRequest += () => rebuildBuffer = true;
            }
		}

        void OnPreRender() {
            camPos = env.currentAnchorPos;
            int x, y, z;
            FastMath.FloorToInt(camPos.x, camPos.y, camPos.z, out x, out y, out z);
			x >>= 3;
			y >>= 3;
			z >>= 3;
			if (lastX == x && lastY == y && lastZ == z)
				return;
			lastX = x;
			lastY = y;
			lastZ = z;
			rebuildBuffer = true;
		}

		void LateUpdate() {
			if (rebuildBuffer) {
				FetchLights ();
			}
			UpdateLights ();
		}

		void FetchLights() {
			rebuildBuffer = false;
			lights.Clear ();
			Light[] sceneLights = FindObjectsOfType<Light> ();
			for (int k = 0; k < sceneLights.Length; k++) {
				if (sceneLights [k].isActiveAndEnabled && sceneLights [k].type == LightType.Point) {
					lights.Add (sceneLights [k]);
				}
			}
			lights.Sort (distanceComparer);
		}

		void UpdateLights() {
			int lightCount = lights.Count;
			float worldLightIntensity = Mathf.Max(env.world.lightIntensityMultiplier, 0);
			float worldLightScattering = Mathf.Max(env.world.lightScattering, 0);
			for (int k = 0; k < 32; k++) {
				if (k < lightCount) {
					Light light = lights [k];
					if (light != null) {
						Vector3 lightPos = light.transform.position;
						lightPosBuffer [k].x = lightPos.x;
						lightPosBuffer [k].y = lightPos.y;
						lightPosBuffer [k].z = lightPos.z;
						lightPosBuffer [k].w = 0.0001f + light.range * worldLightScattering;
						Color color = light.color;
						float intensity = light.intensity * worldLightIntensity;
						lightColorBuffer [k].x = color.r * intensity;
						lightColorBuffer [k].y = color.g * intensity;
						lightColorBuffer [k].z = color.b * intensity;
						lightColorBuffer [k].w = color.a;
						continue;
					}
				}
				lightPosBuffer [k].x = float.MaxValue;
				lightPosBuffer [k].y = float.MaxValue;
				lightPosBuffer [k].z = float.MaxValue;
				lightPosBuffer [k].w = 1.0f;
				lightColorBuffer [k].x = 0;
				lightColorBuffer [k].y = 0;
				lightColorBuffer [k].z = 0;
				lightColorBuffer [k].w = 0;
			}
			Shader.SetGlobalVectorArray ("_VPPointLightPosition", lightPosBuffer);
			Shader.SetGlobalVectorArray ("_VPPointLightColor", lightColorBuffer);
			Shader.SetGlobalInt ("_VPPointLightCount", lightCount);
		}


		int distanceComparer(Light a, Light b) {
			Vector3 posA = a.transform.position;
			Vector3 posB = b.transform.position;
			float distA = FastVector.SqrDistance (ref camPos, ref posA);
			float distB = FastVector.SqrDistance (ref camPos, ref posB);
			if (distA < distB)
				return -1;
			if (distA > distB)
				return 1;
			return 0;
		}

	}

}
