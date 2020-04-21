//#define DEBUG_BATCHES

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;


namespace VoxelPlay.GPURendering.Instancing {

	public class GPUInstancingRenderer : IGPUInstancingRenderer {

        const string SKW_VOXELPLAY_GPU_INSTANCING = "VOXELPLAY_GPU_INSTANCING";

        Material defaultInstancingMaterial;
		FastIndexedList<VoxelChunk, InstancedChunk> instancedChunks;
		FastList<BatchedMesh> batchedMeshes;
		public bool rebuild;
		VoxelPlayEnvironment env;
		int lastRebuildFrame;


		public GPUInstancingRenderer (VoxelPlayEnvironment env) {
			this.env = env;
			defaultInstancingMaterial = Resources.Load<Material> ("VoxelPlay/Materials/VP Model VertexLit");
			instancedChunks = new FastIndexedList<VoxelChunk, InstancedChunk> ();
			batchedMeshes = new FastList<BatchedMesh> ();
		}

		public void Dispose() {
		}


		public void ClearChunk (VoxelChunk chunk) {
			InstancedChunk instancedChunk;
			if (instancedChunks.TryGetValue (chunk, out instancedChunk)) {
				instancedChunk.Clear ();
				rebuild = true;
			}
		}

		public void AddVoxel (VoxelChunk chunk, int voxelIndex, Vector3 position, Quaternion rotation, Vector3 scale) {

			VoxelDefinition voxelDefinition = env.voxelDefinitions [chunk.voxels [voxelIndex].typeIndex];

			// Ensure there're batches for this voxel definition
			if (voxelDefinition.batchedIndex < 0) {
				BatchedMesh batchedMesh = new BatchedMesh (voxelDefinition);
				Material material = voxelDefinition.material;
				if (material == null) {
					material = defaultInstancingMaterial;
				}
                material.EnableKeyword(SKW_VOXELPLAY_GPU_INSTANCING);
				batchedMesh.material = material;
				voxelDefinition.batchedIndex = batchedMeshes.Add (batchedMesh);
			}

			// Add chunk and voxel to the rendering lists
			InstancedChunk instancedChunk;
			if (!instancedChunks.TryGetValue (chunk, out instancedChunk)) {
				instancedChunk = new InstancedChunk (chunk);
				instancedChunks.Add (chunk, instancedChunk);
			}
			InstancedVoxel instancedVoxel = new InstancedVoxel ();
			instancedVoxel.voxelDefinition = voxelDefinition;
			instancedVoxel.meshSize = voxelDefinition.mesh.bounds.size;
			instancedVoxel.position = position;
			instancedVoxel.matrix.SetTRS (position, rotation, scale);
			instancedVoxel.color = chunk.voxels [voxelIndex].color;
			instancedVoxel.packedLight = chunk.voxels [voxelIndex].packedLight;
			instancedChunk.instancedVoxels.Add (instancedVoxel);
			rebuild = true;
		}

		void RebuildZoneRenderingLists (Vector3 observerPos, float visibleDistance) {
			// rebuild batch lists to be used in the rendering loop
			for (int k = 0; k < batchedMeshes.count; k++) {
				BatchedMesh batchedMesh = batchedMeshes.values [k];
				batchedMesh.batches.Clear ();
			}

			float cullDistance = (visibleDistance * VoxelPlayEnvironment.CHUNK_SIZE) * (visibleDistance * VoxelPlayEnvironment.CHUNK_SIZE);

			for (int j = 0; j <= instancedChunks.lastIndex; j++) {
				InstancedChunk instancedChunk = instancedChunks.values [j];
				if (instancedChunk == null)
					continue;
				// check if chunk is in area
				Vector3 chunkCenter = instancedChunk.chunk.position;
				if (FastVector.SqrDistance (ref chunkCenter, ref observerPos) > cullDistance)
					continue;
					
				// add instances to batch
				InstancedVoxel[] voxels = instancedChunk.instancedVoxels.values;
				for (int i = 0; i < instancedChunk.instancedVoxels.count; i++) {
					VoxelDefinition vd = voxels [i].voxelDefinition;
					BatchedMesh batchedMesh = batchedMeshes.values [vd.batchedIndex];
					Batch batch = batchedMesh.batches.last;
					if (batch == null || batch.instancesCount >= Batch.MAX_INSTANCES) {
						batch = batchedMesh.batches.FetchDirty ();
						if (batch == null) {
							batch = new Batch ();
							batchedMesh.batches.Add (batch);
						}
						batch.Init ();
					}
					int pos = batch.instancesCount++;
					// just copying the matrix triggers lot of expensive memcpy() calls so we directly copy the fields
//					batch.matrices[pos] = voxels[i].matrix;
					batch.matrices [pos].m00 = voxels [i].matrix.m00; batch.matrices [pos].m01 = voxels [i].matrix.m01; batch.matrices [pos].m02 = voxels [i].matrix.m02; batch.matrices [pos].m03 = voxels [i].matrix.m03;
					batch.matrices [pos].m10 = voxels [i].matrix.m10; batch.matrices [pos].m11 = voxels [i].matrix.m11; batch.matrices [pos].m12 = voxels [i].matrix.m12; batch.matrices [pos].m13 = voxels [i].matrix.m13;
					batch.matrices [pos].m20 = voxels [i].matrix.m20; batch.matrices [pos].m21 = voxels [i].matrix.m21; batch.matrices [pos].m22 = voxels [i].matrix.m22; batch.matrices [pos].m23 = voxels [i].matrix.m23;
					batch.matrices [pos].m30 = voxels [i].matrix.m30; batch.matrices [pos].m31 = voxels [i].matrix.m31; batch.matrices [pos].m32 = voxels [i].matrix.m32; batch.matrices [pos].m33 = voxels [i].matrix.m33;

					batch.colorsAndLight [pos].x = voxels [i].color.r / 255f;
					batch.colorsAndLight [pos].y = voxels [i].color.g / 255f;
					batch.colorsAndLight [pos].z = voxels [i].color.b / 255f;
					batch.colorsAndLight [pos].w = voxels [i].packedLight;
					batch.UpdateBounds (voxels[i].position, voxels[i].meshSize);
				}
			}

			for (int k = 0; k < batchedMeshes.count; k++) {
				BatchedMesh batchedMesh = batchedMeshes.values [k];
				for (int j = 0; j < batchedMesh.batches.count; j++) {
					Batch batch = batchedMesh.batches.values [j];
					batch.materialPropertyBlock.SetVectorArray ("_TintColor", batch.colorsAndLight);
				}
			}
		}

		public void Render (Vector3 observerPos, float visibleDistance, Vector3[] frustumPlanesNormals, float[] frustumPlanesDistances ) {
#if DEBUG_BATCHES
			int batches = 0;
			int instancesCount = 0;
#endif

			if (rebuild) {
				if (!Application.isPlaying || Time.frameCount - lastRebuildFrame > 10) {
					lastRebuildFrame = Time.frameCount;
					RebuildZoneRenderingLists (observerPos, visibleDistance);
					rebuild = false;
				}
			}
			for (int k = 0; k < batchedMeshes.count; k++) {
				BatchedMesh batchedMesh = batchedMeshes.values [k];
				VoxelDefinition vd = batchedMesh.voxelDefinition;
				Mesh mesh = vd.mesh;
				Material material = batchedMesh.material;
				ShadowCastingMode shadowCastingMode = (vd.castShadows && env.enableShadows) ? ShadowCastingMode.On : ShadowCastingMode.Off;
				bool receiveShadows = vd.receiveShadows && env.enableShadows;
				for (int j = 0; j < batchedMesh.batches.count; j++) {
					Batch batch = batchedMesh.batches.values [j];
					if (GeometryUtilityNonAlloc.TestPlanesAABB (frustumPlanesNormals, frustumPlanesDistances, ref batch.boundsMin, ref batch.boundsMax)) {
						Graphics.DrawMeshInstanced (mesh, 0, material, batch.matrices, batch.instancesCount, batch.materialPropertyBlock, shadowCastingMode, receiveShadows, env.layerVoxels);
						#if DEBUG_BATCHES
						batches++;
						instancesCount += batch.instancesCount;
						#endif
					}
				}
			}
			#if UNITY_EDITOR
			// required to fix a bug by which Draw Calls skyrocket in Stats windows when some voxel uses GPU instancing when "Render In SceneView" is enabled and the scene has just loaded in Editor
			UnityEditor.EditorUtility.SetDirty(env.gameObject);
			#endif

			#if DEBUG_BATCHES
			Debug.Log ("Batches: " + batches + " Instances: " + instancesCount);
			#endif
		}


	}
}
