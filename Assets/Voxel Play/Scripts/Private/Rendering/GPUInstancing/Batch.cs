using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;


namespace VoxelPlay.GPURendering.Instancing {

	class Batch {
		public static int MAX_INSTANCES = 1023;
		public Matrix4x4[] matrices;
		public Vector4[] colorsAndLight;
		public int instancesCount;
		public MaterialPropertyBlock materialPropertyBlock;
		public Vector3 boundsMin, boundsMax;

		public void Init () {
			instancesCount = 0;
			if (matrices == null) {
				matrices = new Matrix4x4[MAX_INSTANCES];
				colorsAndLight = new Vector4[MAX_INSTANCES];
				materialPropertyBlock = new MaterialPropertyBlock ();
			}
			materialPropertyBlock.Clear ();
			boundsMin = Misc.vector3max;
			boundsMax = Misc.vector3min;
		}

		public void UpdateBounds (Vector3 position, Vector3 size) {
			if (position.x - size.x < boundsMin.x) {
				boundsMin.x = position.x - size.x;
			}
			if (position.y - size.y < boundsMin.y) {
				boundsMin.y = position.y - size.y;
			}
			if (position.z - size.z < boundsMin.z) {
				boundsMin.z = position.z - size.z;
			}
			if (position.x + size.x > boundsMax.x) {
				boundsMax.x = position.x + size.x;
			}
			if (position.y + size.y > boundsMax.y) {
				boundsMax.y = position.y + size.y;
			}
			if (position.z + size.z > boundsMax.z) {
				boundsMax.z = position.z + size.z;
			}
		}
	}

}
