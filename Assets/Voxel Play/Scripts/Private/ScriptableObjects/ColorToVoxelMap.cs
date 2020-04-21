using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	[Serializable]
	public struct ColorToVoxelMapEntry {
		public Color32 color;
		public VoxelDefinition voxelDefinition;
	}

	[CreateAssetMenu (menuName = "Voxel Play/Color To Voxel Map Asset", fileName = "ColorToVoxelMap", order = 152)]
	public partial class ColorToVoxelMap : ScriptableObject {
		[Tooltip ("Default voxel definition used for any color that does not have an associated voxel definition.")]
		public VoxelDefinition defaultVoxelDefinition;

		[Tooltip ("Color to Voxel Definition map entries. Empty voxel definitions uses default voxel definition.")]
		public ColorToVoxelMapEntry[] colorMap;


		public VoxelDefinition GetVoxelDefinition (Color32 color, VoxelDefinition defaultVoxelDefinition) {
			VoxelDefinition vd = null;
			for (int k = 0; k < colorMap.Length; k++) {
				if (color.r == colorMap [k].color.r && color.g == colorMap [k].color.g && color.b == colorMap [k].color.b && color.a == colorMap [k].color.a) {
					vd = colorMap [k].voxelDefinition;
					break;
				}
			}
			if (vd == null) {
				vd = this.defaultVoxelDefinition;
				if (vd == null) {
					vd = defaultVoxelDefinition;
				}
			}
			return vd;
		}

	}

}