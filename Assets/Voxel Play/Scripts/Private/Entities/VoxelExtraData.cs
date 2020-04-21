using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	/// <summary>
	/// This struct represents additional data present on sparse voxels. That's why these fields are not present in Voxel entity.
	/// </summary>
	public struct VoxelExtraData {
		public bool hidden;
		public ushort hiddenTypeIndex;
		public byte hiddenOpaque, hiddenLight;
		public HideStyle hiddenStyle;
	}
}