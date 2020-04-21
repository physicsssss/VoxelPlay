using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;


namespace VoxelPlay.GPURendering.InstancingIndirect {

	class InstancedChunk {
		public BatchedCell batchedCell;
		public VoxelChunk chunk;
		public FastList<InstancedVoxel> instancedVoxels;

		public InstancedChunk(VoxelChunk chunk, BatchedCell cell) {
			this.chunk = chunk;
			this.batchedCell = cell;
			instancedVoxels = new FastList<InstancedVoxel>();
		}

		public void Clear() {
			instancedVoxels.Clear ();
		}
	}
}
