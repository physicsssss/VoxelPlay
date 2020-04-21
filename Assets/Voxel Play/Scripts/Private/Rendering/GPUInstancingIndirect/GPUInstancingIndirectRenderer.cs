//#define DEBUG_BATCHES

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay.GPURendering.InstancingIndirect {

	public class GPUInstancingIndirectRenderer : IGPUInstancingRenderer {
		const float CELL_SIZE = 128;
		const string SKW_VOXELPLAY_USE_ROTATION = "VOXELPLAY_USE_ROTATION";
        const string SKW_VOXELPLAY_GPU_INSTANCING = "VOXELPLAY_GPU_INSTANCING";
		Material defaultInstancingMaterial;
		FastIndexedList<VoxelChunk, InstancedChunk> instancedChunks;
		FastIndexedList<Vector3, BatchedCell> cells;
		VoxelPlayEnvironment env;

		public GPUInstancingIndirectRenderer (VoxelPlayEnvironment env) {
			this.env = env;
			defaultInstancingMaterial = Resources.Load<Material> ("VoxelPlay/Materials/VP Indirect VertexLit");
			if (!SystemInfo.supportsComputeShaders) {
				Debug.LogError ("Current platform does not support compute buffers. Switch off 'Compute Buffers' option in Voxel Play Environment inspector.");
			}
			instancedChunks = new FastIndexedList<VoxelChunk, InstancedChunk> ();
			cells = new FastIndexedList<Vector3, BatchedCell> ();
		}

		public void Dispose () {
			if (cells == null)
				return;
			for (int k = 0; k <= cells.lastIndex; k++) {
				BatchedCell cell = cells.values [k];
				if (cell != null) {
					cell.DisposeBuffers ();
				}
			}
		}


		public void ClearChunk (VoxelChunk chunk) {
			InstancedChunk instancedChunk;
			if (instancedChunks.TryGetValue (chunk, out instancedChunk)) {
				if (!instancedChunk.batchedCell.rebuild) {
					instancedChunk.batchedCell.rebuild = true;
				}
				instancedChunk.Clear ();
			}
		}


		BatchedCell GetBatchedCell (VoxelChunk chunk) {
			Vector3 pos = chunk.position;
			int cellX, cellY, cellZ;
			FastMath.FloorToInt (pos.x / CELL_SIZE, pos.y / CELL_SIZE, pos.z / CELL_SIZE, out cellX, out cellY, out cellZ);
			pos.x = cellX;
			pos.y = cellY;
			pos.z = cellZ;
			BatchedCell cell;
			if (!cells.TryGetValue (pos, out cell)) {
				cell = new BatchedCell (pos, CELL_SIZE);
				cells.Add (pos, cell);
			}
			return cell;
		}

		public void AddVoxel (VoxelChunk chunk, int voxelIndex, Vector3 position, Quaternion rotation, Vector3 scale) {
			
			// Add chunk to cell rendering lists
			InstancedChunk instancedChunk;
			if (!instancedChunks.TryGetValue (chunk, out instancedChunk)) {
				BatchedCell batchedCell = GetBatchedCell (chunk);
				instancedChunk = new InstancedChunk (chunk, batchedCell);
				instancedChunks.Add (chunk, instancedChunk);
				batchedCell.instancedChunks.Add (instancedChunk);
			}

			// Ensure there're batches for this voxel definition in its cell
			InstancedVoxel instancedVoxel = new InstancedVoxel ();
			VoxelDefinition voxelDefinition = env.voxelDefinitions [chunk.voxels [voxelIndex].typeIndex];
			BatchedCell cell = instancedChunk.batchedCell;
			BatchedMesh batchedMesh;
			if (!cell.batchedMeshes.TryGetValue (voxelDefinition, out batchedMesh)) {
				batchedMesh = new BatchedMesh (voxelDefinition);
				Material material = voxelDefinition.material;
				if (material == null) {
					material = defaultInstancingMaterial;
				}
                material.EnableKeyword(SKW_VOXELPLAY_GPU_INSTANCING);
				batchedMesh.material = material;
				cell.batchedMeshes.Add (voxelDefinition, batchedMesh);
			}

			// Add voxel to the rendering lists
			instancedVoxel.batchedMesh = batchedMesh;
			instancedVoxel.voxelDefinition = voxelDefinition;
			instancedVoxel.meshSize = voxelDefinition.mesh.bounds.size;
			// only uniform scale is supported in indirect rendering (for optimization purposes)
			instancedVoxel.position.x = position.x; instancedVoxel.position.y = position.y; instancedVoxel.position.z = position.z; instancedVoxel.position.w = scale.x;
			instancedVoxel.rotation.x = rotation.x; instancedVoxel.rotation.y = rotation.y; instancedVoxel.rotation.z = rotation.z; instancedVoxel.rotation.w = rotation.w;
			instancedVoxel.color = chunk.voxels [voxelIndex].color;
			instancedVoxel.packedLight = chunk.voxels [voxelIndex].packedLight;
			instancedChunk.instancedVoxels.Add (instancedVoxel);

			// Mark cell to be rebuilt
			cell.rebuild = true;
		}

		void RebuildCellRenderingLists (BatchedCell cell, Vector3 observerPos, float visibleDistance) {
			// rebuild batch lists to be used in the rendering loop
			cell.ClearBatches ();

			float cullDistance = (visibleDistance * VoxelPlayEnvironment.CHUNK_SIZE) * (visibleDistance * VoxelPlayEnvironment.CHUNK_SIZE);

			for (int j = 0; j < cell.instancedChunks.count; j++) {
				InstancedChunk instancedChunk = cell.instancedChunks.values [j];
				if (instancedChunk == null)
					continue;
					
				// check if chunk is in area
				Vector3 chunkCenter = instancedChunk.chunk.position;
				if (FastVector.SqrDistance (ref chunkCenter, ref observerPos) > cullDistance)
					continue;
				
				// add instances to batch
				InstancedVoxel[] voxels = instancedChunk.instancedVoxels.values;
				for (int i = 0; i < instancedChunk.instancedVoxels.count; i++) {
					BatchedMesh batchedMesh = voxels [i].batchedMesh;

					Batch batch = batchedMesh.batches.last;
					if (batch == null || batch.instancesCount >= Batch.MAX_INSTANCES) {
						batch = batchedMesh.batches.FetchDirty ();
						if (batch == null) {
							batch = new Batch ();
							batch.instancedMaterial = GameObject.Instantiate<Material> (batchedMesh.material);
							if (batchedMesh.voxelDefinition.rotationRandomY || batchedMesh.voxelDefinition.rotation != Misc.vector3zero) {
								batch.instancedMaterial.EnableKeyword (SKW_VOXELPLAY_USE_ROTATION);
							}
							batchedMesh.batches.Add (batch);
						}
						batch.Init ();
					}
					int pos = batch.instancesCount++;
					batch.positions [pos] = voxels [i].position;
					batch.rotations [pos].x = voxels [i].rotation.x; batch.rotations [pos].y = voxels [i].rotation.y; batch.rotations [pos].z = voxels [i].rotation.z; batch.rotations [pos].w = voxels [i].rotation.w;
//						batch.scales[pos] = voxels[i].scale;
					batch.colorsAndLight [pos].x = voxels [i].color.r / 255f;
					batch.colorsAndLight [pos].y = voxels [i].color.g / 255f;
					batch.colorsAndLight [pos].z = voxels [i].color.b / 255f;
					batch.colorsAndLight [pos].w = voxels [i].packedLight;
					batch.UpdateBounds (voxels [i].position, voxels [i].meshSize);
				}
			}

			for (int i = 0; i <= cell.batchedMeshes.lastIndex; i++) {
				BatchedMesh batchedMesh = cell.batchedMeshes.values [i];
				if (batchedMesh == null)
					continue;
				for (int j = 0; j < batchedMesh.batches.count; j++) {
					Batch batch = batchedMesh.batches.values [j];
					batch.ComputeBounds ();
					// Set positions
					batch.positionsBuffer.SetData (batch.positions);
					batch.instancedMaterial.SetBuffer ("_Positions", batch.positionsBuffer);
					// Set colors and light
					batch.colorsAndLightBuffer.SetData (batch.colorsAndLight);
					batch.instancedMaterial.SetBuffer ("_ColorsAndLight", batch.colorsAndLightBuffer);
					// Set rotations
					batch.rotationsBuffer.SetData(batch.rotations);
					batch.instancedMaterial.SetBuffer ("_Rotations", batch.rotationsBuffer);
					// Set buffer args
					Mesh mesh = batchedMesh.voxelDefinition.mesh;
					batch.args [0] = mesh.GetIndexCount(0);
					batch.args [1] = (uint)batch.instancesCount;
					batch.args [2] = mesh.GetIndexStart(0);
					batch.args [3] = 0; // (uint)mesh.GetBaseVertex (0);
					batch.argsBuffer.SetData (batch.args);
				}
			}

		}

		public void Render (Vector3 observerPos, float visibleDistance, Vector3[] frustumPlanesNormals, float[] frustumPlanesDistances) {
			#if DEBUG_BATCHES
			int batches = 0;
			int instancesCount = 0;
			#endif

			for (int k = 0; k <= cells.lastIndex; k++) {
				BatchedCell cell = cells.values [k];
				if (cell == null)
					continue;
				if (!GeometryUtilityNonAlloc.TestPlanesAABB (frustumPlanesNormals, frustumPlanesDistances, ref cell.boundsMin, ref cell.boundsMax))
					continue;

				if (cell.rebuild) {
					if (!Application.isPlaying || Time.frameCount - cell.lastRebuildFrame > 10) {
						cell.lastRebuildFrame = Time.frameCount;
						RebuildCellRenderingLists (cell, observerPos, visibleDistance);
						cell.rebuild = false;
					}
				}

				for (int j = 0; j <= cell.batchedMeshes.lastIndex; j++) {
					BatchedMesh batchedMesh = cell.batchedMeshes.values [j];
					if (batchedMesh == null)
						continue;
					VoxelDefinition vd = batchedMesh.voxelDefinition;
					Mesh mesh = vd.mesh;
					ShadowCastingMode shadowCastingMode = (vd.castShadows && env.enableShadows) ? ShadowCastingMode.On : ShadowCastingMode.Off;
					bool receiveShadows = vd.receiveShadows && env.enableShadows;
					for (int i = 0; i < batchedMesh.batches.count; i++) {
						Batch batch = batchedMesh.batches.values [i];
						if (GeometryUtilityNonAlloc.TestPlanesAABB (frustumPlanesNormals, frustumPlanesDistances, ref batch.boundsMin, ref batch.boundsMax)) {
							Graphics.DrawMeshInstancedIndirect (mesh, 0, batch.instancedMaterial, batch.bounds, batch.argsBuffer, 0, null, shadowCastingMode, receiveShadows, env.layerVoxels);
							#if DEBUG_BATCHES
							batches++;
							instancesCount += batch.instancesCount;
							#endif
						}
					}
				}
			}
			#if DEBUG_BATCHES
			Debug.Log ("Batches: " + batches + " Instances: " + instancesCount);
			#endif
		}


	}
}
