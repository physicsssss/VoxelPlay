using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;


namespace VoxelPlay {
				
	public static class GeometryUtilityNonAlloc {
								
		private static System.Action<Plane[], Matrix4x4> _calculateFrustumPlanes_Imp;

		public static void CalculateFrustumPlanes(Plane[] planes, Matrix4x4 worldToProjectMatrix) {
			if (planes == null)
				throw new System.ArgumentNullException("planes");
			if (planes.Length < 6)
				throw new System.ArgumentException("Output array must be at least 6 in length.", "planes");

			if (_calculateFrustumPlanes_Imp == null) {
				var meth = typeof(GeometryUtility).GetMethod("Internal_ExtractPlanes", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new System.Type[]
					{
						typeof(Plane[]),
						typeof(Matrix4x4)
					}, null);
				if (meth == null)
					throw new System.Exception("Failed to reflect internal method. Your Unity version may not contain the presumed named method in GeometryUtility.");

				_calculateFrustumPlanes_Imp = System.Delegate.CreateDelegate(typeof(System.Action<Plane[], Matrix4x4>), meth) as System.Action<Plane[], Matrix4x4>;
				if (_calculateFrustumPlanes_Imp == null)
					throw new System.Exception("Failed to reflect internal method. Your Unity version may not contain the presumed named method in GeometryUtility.");
			}

			_calculateFrustumPlanes_Imp(planes, worldToProjectMatrix);
		}

		public static bool TestPlanesAABB(Vector3[] planesNormals, float[] planesDistances, ref Vector3 boundsMin, ref Vector3 boundsMax) {
			Vector3 vmin;

			for (int planeIndex = 0; planeIndex < planesNormals.Length; planeIndex++) {
				var normal = planesNormals[planeIndex];

				// X axis
				if (normal.x < 0) {
					vmin.x = boundsMin.x;
				} else {
					vmin.x = boundsMax.x;
				}

				// Y axis
				if (normal.y < 0) {
					vmin.y = boundsMin.y;
				} else {
					vmin.y = boundsMax.y;
				}

				// Z axis
				if (normal.z < 0) {
					vmin.z = boundsMin.z;
				} else {              
					vmin.z = boundsMax.z;
				}

				var dot1 = normal.x * vmin.x + normal.y * vmin.y + normal.z * vmin.z;
				if (dot1 + planesDistances[planeIndex] < 0) {
					return false;
				}
			}

			return true;
		}



	}
}