using System;
using UnityEngine;
using System.Collections;

namespace VoxelPlay {
	public static class FastVector {

		/// <summary>
		/// Averages two vectors and writes results to another vector
		/// </summary>
		public static void Average(ref Vector2 v1, ref Vector2 v2, out Vector2 result) {
			result.x = (v1.x + v2.x) * 0.5f;
			result.y = (v1.y + v2.y) * 0.5f;
		}


		/// <summary>
		/// Averages two vectors and writes results to another vector
		/// </summary>
		public static void Average(ref Vector3 v1, ref Vector3 v2, out Vector3 result) {
			result.x = (v1.x + v2.x) * 0.5f;
			result.y = (v1.y + v2.y) * 0.5f;
			result.z = (v1.z + v2.z) * 0.5f;
		}

		/// <summary>
		/// Substracts one vector to another
		/// </summary>
		public static void Substract(ref Vector3 v1, ref Vector3 v2) {
			v1.x -= v2.x;
			v1.y -= v2.y;
			v1.z -= v2.z;
		}

		/// <summary>
		/// Adds v2 to v1
		/// </summary>
		public static void Add(ref Vector3 v1, ref Vector3 v2) {
			v1.x += v2.x;
			v1.y += v2.y;
			v1.z += v2.z;
		}

        /// <summary>
        /// Adds v2 multiplied by a float value to v1
        /// </summary>
        public static void Add(ref Vector3 v1, ref Vector3 v2, float v) {
			v1.x += v2.x * v;
			v1.y += v2.y * v;
			v1.z += v2.z * v;
		}

		/// <summary>
		/// Adds v2 to v1
		/// </summary>
		public static void Add(ref Vector2 v1, ref Vector2 v2) {
			v1.x += v2.x;
			v1.y += v2.y;
		}

		/// <summary>
		/// Adds v2 multiplied by a float value to v1
		/// </summary>
		public static void Add(ref Vector2 v1, ref Vector2 v2, float v) {
			v1.x += v2.x * v;
			v1.y += v2.y * v;
		}

		/// <summary>
		/// Writes to result the normalized direction from one position to another position
		/// </summary>
		/// <param name="from">From.</param>
		/// <param name="to">To.</param>
		/// <param name="result">Result.</param>
		public static void NormalizedDirection(ref Vector2 from, ref Vector2 to, out Vector2 result) {
			float dx = to.x - from.x;
			float dy = to.y - from.y;
			float length = (float)Math.Sqrt(dx * dx + dy * dy);
			result.x = dx / length;
			result.y = dy / length;
		}


		/// <summary>
		/// Writes to result the normalized direction from one position to another position
		/// </summary>
		/// <param name="from">From.</param>
		/// <param name="to">To.</param>
		/// <param name="result">Result.</param>
		public static void NormalizedDirection(ref Vector3 from, ref Vector3 to, out Vector3 result) {
			float dx = to.x - from.x;
			float dy = to.y - from.y;
			float dz = to.z - from.z;
			float length = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
			result.x = dx / length;
			result.y = dy / length;
			result.z = dz / length;
		}

		/// <summary>
		/// Writes to result the normalized direction from one position to another position
		/// </summary>
		public static Vector3 NormalizedDirectionByValue(ref Vector3 from, ref Vector3 to) {
			Vector3 result = Misc.vector3zero;
			float dx = to.x - from.x;
			float dy = to.y - from.y;
			float dz = to.z - from.z;
			float length = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
			result.x = dx / length;
			result.y = dy / length;
			result.z = dz / length;
			return result;
		}


        public static float SqrMinDistanceXZ(Vector3 v1, Vector3 v2) {
            float dx = v2.x - v1.x;
            dx *= dx;
            float dz = v2.z - v1.z;
            dz *= dz;
            return dx > dz ? dz : dx;
        }
		/// <summary>
		/// Returns the sqr distance from one position to another
		/// </summary>
		public static float SqrDistance(ref Vector3 v1, ref Vector3 v2) {
			float dx = v2.x - v1.x;
			float dy = v2.y - v1.y;
			float dz = v2.z - v1.z;
			return dx * dx + dy * dy + dz * dz;
		}

        /// <summary>
        /// Returns the sqr distance from one position to another
        /// </summary>
        public static float SqrMaxDistanceXorZ(ref Vector3 v1, ref Vector3 v2) {
            float dx = v2.x - v1.x;
            dx *= dx;
            float dz = v2.z - v1.z;
            dz *= dz;
            return dx > dz ? dx : dz;
        }

        /// <summary>
        /// Returns the sqr distance from one position to another. Alternate version that passes vectors by value.
        /// </summary>
        public static float SqrDistanceByValue(Vector3 v1, Vector3 v2) {
			float dx = v2.x - v1.x;
			float dy = v2.y - v1.y;
			float dz = v2.z - v1.z;
			return dx * dx + dy * dy + dz * dz;
		}

		/// <summary>
		/// Returns the sqr distance from one position to another
		/// </summary>
		public static float SqrDistance(ref Vector2 v1, ref Vector2 v2) {
			float dx = v2.x - v1.x;
			float dy = v2.y - v1.y;
			return dx * dx + dy * dy;
		}

        /// <summary>
        /// Returns the sqr distance from one position to another in XZ plane
        /// </summary>
        public static float SqrDistanceXZ(ref Vector3 v1, ref Vector3 v2) {
            float dx = v2.x - v1.x + 1;
            float dz = v2.z - v1.z + 1;
            return dx * dx + dz * dz;
        }

        /// <summary>
        /// Returns the sqr distance from one position to another in XZ plane
        /// </summary>
        public static int SqrDistanceXZ(ref Vector3Int v1, ref Vector3Int v2) {
            int dx = v2.x - v1.x + 1;
            int dz = v2.z - v1.z + 1;
            return dx * dx + dz * dz;
        }

        /// <summary>
        /// Returns the sqr distance from one position to another
        /// </summary>
        public static int SqrMaxDistanceXorZ(ref Vector3Int v1, ref Vector3Int v2) {
            int dx = v2.x - v1.x;
            dx = dx < 0 ? -dx : dx;
            int dz = v2.z - v1.z;
            dz = dz < 0 ? -dz : dz;
            return dx > dz ? dx : dz;
        }
		/// Ensures "to" position is within given range to "from" position. Returns true if value has changed.
		/// </summary>
		/// <param name="from">From.</param>
		/// <param name="to">To.</param>
		/// <param name="minDistance">Minimum distance.</param>
		/// <param name="maxDistance">Max distance.</param>
		public static bool ClampDistance(ref Vector3 from, ref Vector3 to, float minDistance, float maxDistance) {
			float dx = to.x - from.x;
			float dy = to.y - from.y;
			float dz = to.z - from.z;
			float dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);									
			if (dist < 0.0001f)
				return false;
			if (dist < minDistance) {
				float m = minDistance / dist;
				to.x = from.x + (to.x - from.x) * m;
				to.y = from.y + (to.y - from.y) * m;
				to.z = from.z + (to.z - from.z) * m;
				return true;
            }
            if (dist > maxDistance) {
				float m = maxDistance / dist;
				to.x = from.x + (to.x - from.x) * m;
				to.y = from.y + (to.y - from.y) * m;
				to.z = from.z + (to.z - from.z) * m;
				return true;
			}
			return false;
		}


		/// <summary>
		/// Multiplies value by scale and puts the result back into value
		/// </summary>
		/// <param name="value">Value.</param>
		/// <param name="scale">Scale.</param>
		public static void Multiply(ref Vector3 value, ref Vector3 scale, float additionalScale = 1f) {
			value.x *= scale.x * additionalScale;
			value.y *= scale.y * additionalScale;
			value.z *= scale.z * additionalScale;
		}

		/// <summary>
		/// Converts vector values to integer rounding down
		/// </summary>
		public static void Floor(ref Vector3 v) {
			v.x = (float)Math.Floor (v.x);
			v.y = (float)Math.Floor (v.y);
			v.z = (float)Math.Floor (v.z);
		}

		/// <summary>
		/// Converts vector values to integer rounding up
		/// </summary>
		public static void Ceiling(ref Vector3 v) {
			v.x = (float)Math.Ceiling (v.x);
			v.y = (float)Math.Ceiling (v.y);
			v.z = (float)Math.Ceiling (v.z);
		}

        /// <summary>
        /// Converts vector values to the center of the voxel
        /// </summary>
        public static void Middling(ref Vector3 v) {
            v.x = (float)Math.Floor(v.x) + 0.5f;
            v.y = (float)Math.Floor(v.y) + 0.5f;
            v.z = (float)Math.Floor(v.z) + 0.5f;
        }
		/// Returns a vector with signs of each component of another vector
		/// </summary>
		/// <param name="v">V.</param>
		public static Vector3 Sign(ref Vector3 v) {
			Vector3 r = Misc.vector3zero;
			r.x = v.x >= 0 ? 1 : -1;
			r.y = v.y >= 0 ? 1 : -1;
			r.z = v.z >= 0 ? 1 : -1;
			return r;
		}


	}
}