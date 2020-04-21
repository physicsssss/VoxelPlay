using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay.GPURendering {
	
	public interface IGPUInstancingRenderer {
		void ClearChunk (VoxelChunk chunk);

		void AddVoxel (VoxelChunk chunk, int voxelIndex, Vector3 position, Quaternion rotation, Vector3 scale);

		void Render (Vector3 observerPos, float visibleDistance, Vector3[] frustumPlanesNormals, float[] frustumPlanesDistances);

		void Dispose();
	}

}