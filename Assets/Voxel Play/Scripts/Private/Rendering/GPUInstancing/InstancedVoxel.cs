using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;


namespace VoxelPlay.GPURendering.Instancing {

	struct InstancedVoxel {
		public VoxelDefinition voxelDefinition;
		public Vector3 meshSize;
		public Vector3 position;
		public Matrix4x4 matrix;
		public Color32 color;
		public float packedLight;
	}

}
