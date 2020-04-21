using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		struct VegetationRequest {
			public VoxelChunk chunk;
			public int voxelIndex;
			public VoxelDefinition vd;
			public Vector3 chunkOriginalPosition;
		}

		const int VEGETATION_CREATION_BUFFER_SIZE = 20000;

		VegetationRequest[] vegetationRequests;
		int vegetationRequestLast, vegetationRequestFirst;

		void InitVegetation () {
			if (vegetationRequests == null || vegetationRequests.Length != VEGETATION_CREATION_BUFFER_SIZE) {
				vegetationRequests = new VegetationRequest[VEGETATION_CREATION_BUFFER_SIZE];
			}
			vegetationRequestLast = -1;
			vegetationRequestFirst = -1;
		}

		/// <summary>
		/// Requests the vegetation creation.
		/// </summary>
		public void RequestVegetationCreation (VoxelChunk chunk, int voxelIndex, VoxelDefinition vd) { 
			if (chunk == null || !enableVegetation) {
				return;
			}
			vegetationRequestLast++;
			if (vegetationRequestLast >= vegetationRequests.Length) {
				vegetationRequestLast = 0;
			}
			if (vegetationRequestLast != vegetationRequestFirst) {
				vegetationRequests [vegetationRequestLast].chunk = chunk;
				vegetationRequests [vegetationRequestLast].chunkOriginalPosition = chunk.position;
				vegetationRequests [vegetationRequestLast].voxelIndex = voxelIndex;
				vegetationRequests [vegetationRequestLast].vd = vd;
				vegetationInCreationQueueCount++;
			}
		}

		/// <summary>
		/// Monitors queue of new vegetations requests. This function calls Createvegetation to create the vegetation data and pushes a chunk refresh.
		/// </summary>
		void CheckVegetationRequests (long endTime) {
			int max = maxBushesPerFrame > 0 ? maxBushesPerFrame : 10000;
			for (int k = 0; k < max; k++) {
				if (vegetationRequestFirst == vegetationRequestLast)
					return;
				vegetationRequestFirst++;
				if (vegetationRequestFirst >= vegetationRequests.Length) {
					vegetationRequestFirst = 0;
				}
				vegetationInCreationQueueCount--;
				VoxelChunk chunk = vegetationRequests [vegetationRequestFirst].chunk; 
				if (chunk != null && !chunk.modified && chunk.position == vegetationRequests [vegetationRequestFirst].chunkOriginalPosition) {
					CreateVegetation (chunk, vegetationRequests [vegetationRequestFirst].voxelIndex, vegetationRequests [vegetationRequestFirst].vd); 
				}
				long elapsed = stopWatch.ElapsedMilliseconds;
				if (elapsed >= endTime)
					break;
			}
		}

		/// <summary>
		/// Gets the vegetation voxel based on position, biome and a random value
		/// </summary>
		/// <returns>The vegetation.</returns>
		/// <param name="biome">Biome.</param>
		/// <param name="random">Random.</param>
		public VoxelDefinition GetVegetation (BiomeDefinition biome, float random) {
			float acumProb = 0;
			int index = 0;
			for (int t = 0; t < biome.vegetation.Length; t++) {
				acumProb += biome.vegetation [t].probability;
				if (random < acumProb) {
					index = t;
					break;
				}
			}
			return biome.vegetation [index].vegetation;
		}

		/// <summary>
		/// Creates the vegetation.
		/// </summary>
		void CreateVegetation (VoxelChunk chunk, int voxelIndex, VoxelDefinition vd) {

			if (chunk != null) {
				// Updates current chunk
				if (chunk.allowTrees && chunk.voxels [voxelIndex].hasContent != 1) {
					chunk.voxels [voxelIndex].Set (vd);
					vegetationCreated++;
				}
			}
		}


	}



}
