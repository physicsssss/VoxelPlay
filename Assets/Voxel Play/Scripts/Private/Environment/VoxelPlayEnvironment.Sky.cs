using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay {


	public partial class VoxelPlayEnvironment : MonoBehaviour {

		const int CLOUDS_SPECIAL_ALTITUDE = 1304;
		const string CLOUDS_ROOT_NAME = "Clouds Root";

		GameObject cloudsRoot;
		List<VoxelChunk> cloudsChunks;

		void InitSky () {
			if (skyboxMaterial != null) {
				RenderSettings.skybox = skyboxMaterial;
				if (cameraMain != null) {
					cameraMain.clearFlags = CameraClearFlags.Skybox;
				}
			}
			if (sun == null) {
				Light[] ll = FindObjectsOfType<Light> ();
				for (int k = 0; k < ll.Length; k++) {
					if (ll [k].type == LightType.Directional) {
						sun = ll [k];
						break;
					}
				}
			}
		}

		void InitClouds () {

            if (!enableClouds || draftModeActive || world.cloudVoxel == null)
                return;

            if (cloudsChunks == null) {
				cloudsChunks = new List<VoxelChunk> (1000);
			} else {
				cloudsChunks.Clear ();
			}

			Texture2D noise = Resources.Load<Texture2D> ("VoxelPlay/Textures/Noise");
			Color32[] noises = noise.GetPixels32 ();

			int tw = noise.width;
			int tz = noise.height;

			Vector3 pos;
			pos.y = CLOUDS_SPECIAL_ALTITUDE;

			VoxelChunk chunk, lastChunk = null;
			int voxelIndex;
			for (int z = 0; z < 256; z++) {
				pos.z = z;
				int trow = ((z / 4 + tz / 2) % tz) * tw;
				int trow2 = (z / 2 % tz) * tw;
				for (int x = 0; x < 256; x++) {
					pos.x = x;
					int tindex = trow + ((x / 4 + tw / 2) % tw);
					int tindex2 = trow2 + (x / 2 % tw);
					int r1 = noises [tindex].r;
					int r2 = noises [tindex2].r;
					int r = (r1 + r2) / 2;
					if (r < world.cloudCoverage) {
						Vector3 dpos = pos;
						dpos.x -= 128;
						dpos.z -= 128;
						VoxelSingleSet (dpos, world.cloudVoxel, out chunk, out voxelIndex, Misc.color32White);
						if (chunk != null && chunk != lastChunk) {
							chunk.cannotBeReused = true;
							chunk.ignoreFrustum = true;
                            chunk.isCloud = true;
							lastChunk = chunk;
							if (!cloudsChunks.Contains (chunk)) {
								cloudsChunks.Add (chunk);
							}
						}
					}
					if (r < world.cloudCoverage * 19 / 20) {
						Vector3 dpos = pos;
						dpos.x -= 128;
						dpos.z -= 128;
						dpos.y--;
						VoxelSingleSet (dpos, world.cloudVoxel, out chunk, out voxelIndex, Misc.color32White);
						if (chunk != null && chunk != lastChunk) {
							chunk.cannotBeReused = true;
							chunk.ignoreFrustum = true;
                            chunk.isCloud = true;
                            lastChunk = chunk;
							if (!cloudsChunks.Contains (chunk)) {
								cloudsChunks.Add (chunk);
							}
						}
					}
				}
			}

			int chunksCount = cloudsChunks.Count;
			if (chunksCount == 0)
				return;

            Vector3 initialPosition = cameraMain != null ? cameraMain.transform.position : Misc.vector3zero;
            if (cloudsRoot == null) {
				cloudsRoot = new GameObject (CLOUDS_ROOT_NAME);
				cloudsRoot.hideFlags = HideFlags.DontSave;
				cloudsRoot.transform.hierarchyCapacity = 1000;
				cloudsRoot.transform.SetParent (worldRoot, false);
				cloudsRoot.transform.position = initialPosition;
				cloudsRoot.AddComponent<VoxelCloudsAnimator> ().cloudChunks = cloudsChunks;
			}

			for (int k = 0; k < chunksCount; k++) {
				if (cloudsChunks [k] == null)
					continue;
                ChunkRequestRefresh (cloudsChunks [k], false, true);
                Transform tc = cloudsChunks [k].transform;
				pos = cloudsChunks [k].position;
                pos.x = (pos.x - initialPosition.x) * 4f + initialPosition.x + 0.5f;
                pos.z = (pos.z - initialPosition.z) * 4f + initialPosition.z + 0.5f;
                pos.y = world.cloudAltitude + 0.5f;
				cloudsChunks [k].position = pos;
				tc.position = pos;
				tc.SetParent (cloudsRoot.transform, true);
			}
		}

					
	}



}
