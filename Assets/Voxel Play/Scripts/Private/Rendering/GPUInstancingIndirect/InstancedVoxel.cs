using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;


namespace VoxelPlay.GPURendering.InstancingIndirect {

	struct InstancedVoxel {
		public BatchedMesh batchedMesh;
		public VoxelDefinition voxelDefinition;
		public Vector3 meshSize;
		public Vector4 position; // w = uniform scale
		public Vector4 rotation;
		public Color32 color;
		public float packedLight;
	}

}
