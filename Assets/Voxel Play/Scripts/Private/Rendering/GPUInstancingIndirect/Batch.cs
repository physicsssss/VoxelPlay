using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;


namespace VoxelPlay.GPURendering.InstancingIndirect {

	class Batch {
		public const int MAX_INSTANCES = 65000;
		public Vector4[] positions;  // w = uniform scale
		public Vector4[] colorsAndLight;
		public Vector4[] rotations;
		public int instancesCount;
		public Bounds bounds;
		public Vector3 boundsMin, boundsMax;
		public Material instancedMaterial;

		public uint[] args;
		public ComputeBuffer argsBuffer;
		public ComputeBuffer positionsBuffer, colorsAndLightBuffer, rotationsBuffer;


		public void Init () {
			instancesCount = 0;
			if (argsBuffer == null) {
				args = new uint[] { 0, 0, 0, 0, 0 };
				argsBuffer = new ComputeBuffer (1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
				colorsAndLight = new Vector4[MAX_INSTANCES];
				colorsAndLightBuffer = new ComputeBuffer (MAX_INSTANCES, 16);
				positions = new Vector4[MAX_INSTANCES];
				positionsBuffer = new ComputeBuffer (MAX_INSTANCES, 4 * sizeof(float));
				rotations = new Vector4[MAX_INSTANCES];
				rotationsBuffer = new ComputeBuffer (MAX_INSTANCES, 4 * sizeof(float));
			}
			bounds = new Bounds ();
			boundsMin = Misc.vector3max;
			boundsMax = Misc.vector3min;
		}

		public void DisposeBuffers() {
			if (rotationsBuffer != null) {
				rotationsBuffer.Release ();
			}
			if (colorsAndLightBuffer != null) {
				colorsAndLightBuffer.Release ();
			}
			if (positionsBuffer != null) {
				positionsBuffer.Release ();
			}
			if (argsBuffer != null) {
				argsBuffer.Release ();
			}
		}

		public void UpdateBounds (Vector4 position, Vector3 size) {
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

		public void ComputeBounds() {
			bounds = new Bounds ((boundsMin + boundsMax) * 0.5f, boundsMax - boundsMin);
		}
	}

}
