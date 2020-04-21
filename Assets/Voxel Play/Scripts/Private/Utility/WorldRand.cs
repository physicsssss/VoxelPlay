using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {
				
	public static class WorldRand {

		const int RANDOM_TABLE_SIZE = 8192;
		// 2^13

		static float[] rnd;
		static uint rndIndex = 0;

		static WorldRand() {
			WorldRand.Randomize(0);
		}

		/// <summary>
		/// Initializes random table with seed
		/// </summary>
		public static void Randomize(int seed) {
			Random.InitState(seed);
			if (rnd == null || rnd.Length == 0)
				rnd = new float[RANDOM_TABLE_SIZE];
			for (int k = 0; k < rnd.Length; k++) {
				do {
					rnd [k] = Random.value;
				} while(rnd [k] == 1f);
			}
		}

		/// <summary>
		/// Get one of the random values "linked" to a given position
		/// </summary>
		public static float GetValue(Vector3 position) {
			long hash = 17 * 23 + (long)position.x;
			hash = hash * 23 + (long)position.y;
			hash = hash * 23 + (long)position.z;
			hash += (hash >> 26) + (hash >> 13);
			rndIndex = (uint)(hash & 8191);
			return rnd[rndIndex];
		}

		/// <summary>
		/// Get one of the random values "linked" to a given position
		/// </summary>
		public static float GetValue(float x, float z) {
			long hash = 17 * 23 + (long)x;
			hash = hash * 23 + (long)z;
			hash += (hash >> 26) + (hash >> 13);
			rndIndex = (uint)(hash & 8191);
			return rnd[rndIndex];
		}

		/// <summary>
		/// Gets a random value "linked" to a given value
		/// </summary>
		public static float GetValue(int someValue) {
			long hash = (long)someValue;
			hash += (hash >> 26) + (hash >> 13);
			rndIndex = (uint)(hash & 8191);
			return rnd[rndIndex];
		}

		/// <summary>
		/// Returns a random value between min (inclusive) and max (exclusive) "linked" to a given position
		/// </summary>
		public static int Range(int min, int max, Vector3 position) {
			float v = GetValue(position);
			return (int)(min + (max - min) * 0.99999f * v);
		}

        /// <summary>
        /// Returns a random value between min (inclusive) and max (exclusive)
        /// </summary>
        public static int Range(int min, int max) {
            float v = GetValue();
            return (int)(min + (max - min) * 0.99999f * v);
        }


        /// <summary>
        /// Returns a random value between min (inclusive) and max (inclusive)
        /// </summary>
        public static float Range(float min, float max) {
            float v = GetValue();
            return min + (max - min) * v;
        }

        /// Returns a random value between min (inclusive) and max (inclusive) "linked" to a given seed
        /// </summary>
        public static float Range(float min, float max, int seed) {
            float v = GetValue(seed);
            return min + (max - min) * v;
        }

        /// <summary>
        /// Returns a random value in 0-1 range
        /// </summary>
		public static float GetValue() {
			rndIndex++;
			if (rndIndex >= rnd.Length) rndIndex = 0;
			return rnd[rndIndex];
		}

		/// <summary>
		/// Returns a random Vector3 value "linked" to a given position
		/// </summary>
		/// <returns>The vector3.</returns>
		/// <param name="position">Position.</param>
		/// <param name="scale">Scale.</param>
		/// <param name="shift">Random values are in 0..1 range. Shift is added to the random value before being multiplied by scale.</param>
		public static Vector3 GetVector3(Vector3 position, float scale, float shift = 0) {
			float x = (GetValue(position) + shift) * scale;
			float y = (GetValue() + shift) * scale;
			float z = (GetValue() + shift) * scale;
			return new Vector3(x, y, z);
		}

		/// <summary>
		/// Returns a random Vector3 value in range of scale linked to a given position
		/// </summary>
		/// <returns>The vector3.</returns>
		/// <param name="position">Position.</param>
		/// <param name="scale">Scale.</param>
		/// <param name="shift">Random values are in 0..1 range. Shift is added to the random value before being multiplied by scale.</param>
		public static Vector3 GetVector3(Vector3 position, Vector3 scale, float shift = 0) {
			float x = (GetValue(position) + shift) * scale.x;
			float y = (GetValue() + shift) * scale.y;
			float z = (GetValue() + shift) * scale.z;
			return new Vector3(x, y, z);
		}


	}

}