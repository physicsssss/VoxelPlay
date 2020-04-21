using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;


namespace VoxelPlay.GPURendering.Instancing {

	class InstancedChunk {
		public VoxelChunk chunk;
		public FastList<InstancedVoxel> instancedVoxels;

		public InstancedChunk(VoxelChunk chunk) {
			this.chunk = chunk;
			instancedVoxels = new FastList<InstancedVoxel>();
		}

		public void Clear() {
			instancedVoxels.Clear ();
		}
	}
}
