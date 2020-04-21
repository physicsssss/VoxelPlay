// Voxel Play 
// Created by Ramiro Oliva (Kronnect)

// Voxel Play Behaviour - attach this script to any moving object that should receive voxel global illumination

using System;
using UnityEngine;
using System.Collections.Generic;

namespace VoxelPlay {
				
	[HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000001858-voxel-play-behaviour")]
	public class VoxelPlayBehaviour : MonoBehaviour {

		public bool enableVoxelLight = true;
		public bool forceUnstuck = true;
		public bool checkNearChunks = true;
		public Vector3 chunkExtents;
		public bool renderChunks = true;

		VoxelPlayEnvironment env;
		int lastX, lastY, lastZ;
		int lastChunkX, lastChunkY, lastChunkZ;
		Vector3 lastPosition;
        bool requireUpdateLighting;
        static List<Renderer> rr = new List<Renderer>();
        struct RendererData {
            public Material mat;
            public Color normalMatColor;
            public bool useMaterialColor;
        }

        RendererData[] rd;

		void Start() {
			env = VoxelPlayEnvironment.instance;
			if (env == null) {
				DestroyImmediate(this);
				return;
			}
            env.OnChunkRender += ChunkRender;
			lastPosition = transform.position;
            lastX = int.MaxValue;

            if (enableVoxelLight) {
                FetchMaterials();
            }

            CheckNearChunks(transform.position);

        }

        void FetchMaterials() {
            GetComponentsInChildren<Renderer>(true, rr);
            int count = rr.Count;
            rd = new RendererData[count];
            for (int k = 0; k < count; k++) {
                Renderer mr = rr[k];
                    Material mat = mr.sharedMaterial;
                if (mat == null) continue;
                    rd[k].useMaterialColor = !mat.name.Contains("VP Model");
                    if (rd[k].useMaterialColor) {
						mat = Instantiate (mat) as Material;
						mat.hideFlags = HideFlags.DontSave;
						mr.sharedMaterial = mat;
                        rd[k].normalMatColor = mat.color;
					}
                    rd[k].mat = mat;
				}
                requireUpdateLighting = true;
			}

        private void OnDestroy() {
            if (env == null) return;

            env.OnChunkRender -= ChunkRender;

        }
        void ChunkRender(VoxelChunk chunk) {
            if (FastVector.SqrMinDistanceXZ(chunk.position, transform.position) < 32 * 32) {
                requireUpdateLighting = true;
            }
        }

		public void Refresh() {
			lastX = int.MaxValue;
			lastChunkX = int.MaxValue;
		}

		void LateUpdate() {

			if (!env.initialized)
				return;

			// Check if position has changed since previous
			Vector3 position = transform.position;
			int x, y, z;
			FastMath.FloorToInt (position.x, position.y, position.z, out x, out y, out z);

            if (lastX != x || lastY != y || lastZ != z) {
                requireUpdateLighting = true;

			lastPosition = position;
			lastX = x;
			lastY = y;
			lastZ = z;
	
			if (forceUnstuck) {
				Vector3 pos = transform.position;
				pos.y += 0.1f;
				if (env.CheckCollision (pos)) {
					float deltaY = FastMath.FloorToInt (pos.y) + 1f - pos.y;
					pos.y += deltaY + 0.01f;
					transform.position = pos;
					lastX--;
				}
			}

			CheckNearChunks (position);
		}
            if (requireUpdateLighting) {
                requireUpdateLighting = false;
                UpdateLightingNow();
            }
        }

		void CheckNearChunks(Vector3 position) {
			if (!checkNearChunks)
				return;
			int chunkX, chunkY, chunkZ;
			FastMath.FloorToInt (position.x / VoxelPlayEnvironment.CHUNK_SIZE, position.y / VoxelPlayEnvironment.CHUNK_SIZE, position.z / VoxelPlayEnvironment.CHUNK_SIZE, out chunkX, out chunkY, out chunkZ);
			if (lastChunkX != chunkX || lastChunkY != chunkY || lastChunkZ != chunkZ) {
				lastChunkX = chunkX;
				lastChunkY = chunkY;
				lastChunkZ = chunkZ;
                // Ensure area is rendered
				env.ChunkCheckArea (position, chunkExtents, renderChunks);
			}
		}


        public void UpdateLighting() {
            requireUpdateLighting = true;
        }

        void UpdateLightingNow() {
            if (!enableVoxelLight) return;
            if (rd == null || rd.Length == 0) {
                FetchMaterials();
            }
            Vector3 pos = lastPosition;
            // center of voxel
            pos.x += 0.5f;
            pos.y += 0.5f;
            pos.z += 0.5f;
            float light = env.GetVoxelLight(pos);
            for (int k = 0; k < rd.Length; k++) {
                if (rd[k].mat == null) continue;

                if (rd[k].useMaterialColor) {
                    Color newColor = new Color(rd[k].normalMatColor.r * light, rd[k].normalMatColor.g * light, rd[k].normalMatColor.b * light, rd[k].normalMatColor.a);
                    rd[k].mat.color = newColor;
                } else {
                    rd[k].mat.SetFloat("_VoxelLight", light);
                }
            }
        }

	}
}