using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;


namespace VoxelPlay.GPURendering.InstancingIndirect {

	class BatchedMesh {
		public VoxelDefinition voxelDefinition;
		public Material material;
		public FastList<Batch> batches;

		public BatchedMesh(VoxelDefinition voxelDefinition) {
			this.voxelDefinition = voxelDefinition;
			batches = new FastList<Batch> ();
		}

		public void DisposeBuffers() {
			if (batches == null)
				return;
			for (int k = 0; k < batches.count; k++) {
				batches.values[k].DisposeBuffers ();
			}


		}
	}
}
