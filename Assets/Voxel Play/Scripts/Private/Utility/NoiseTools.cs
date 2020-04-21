using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	public static partial class NoiseTools {

		/// <summary>
		/// Random seeded offsets to the terrain sampling. Used to provide different terrain outputs by translating the zero position.
		/// </summary>
		public static Vector3 seedOffset;

		/// <summary>
		/// Misc. useful functions for terrain generators
		/// </summary>
		/// <returns>The noise texture.</returns>
		/// <param name="tex">Tex.</param>
		/// <param name="textureSize">Texture size.</param>
		public static float[] LoadNoiseTexture(Texture tex, out int textureSize) {
			if (tex == null) {
				textureSize = 0;
				return null;
			}
			textureSize = tex.width;
			Color[] temp = null;
			if (tex is Texture2D) {
				Texture2D tex2D = (Texture2D)tex;
				temp = tex2D.GetPixels();
			} else if (tex is Texture3D) {
				Texture3D tex3D = (Texture3D)tex;
				temp = tex3D.GetPixels();
			} else return null;
				
			int count = temp.Length;
			float[] values = new float[count];
			for (int k = 0; k < temp.Length; k++) {
				values[k] = temp[k].r;
			}
			return values;
		}


		/// <summary>
		/// Samples a 2D noise texture at a given world position (returns raw value)
		/// </summary>
		/// <returns>The noise value one sample.</returns>
		/// <param name="noiseArray">Noise array.</param>
		/// <param name="textureSize">Texture size.</param>
		/// <param name="x">The x coordinate.</param>
		/// <param name="z">The z coordinate.</param>
		public static float GetNoiseValue(float[] noiseArray, int textureSize, float x, float z, bool ridgeNoise = false) {

			if (textureSize == 0)
				return 0;

			z = z + textureSize * 10000f + seedOffset.z;
			x = x + textureSize * 10000f + seedOffset.x;
			int posZInt = (int)z;
			int posXInt = (int)x;

			// Texture array position
			int ty0 = posZInt % textureSize;
			int tx0 = posXInt % textureSize;

			float value = noiseArray[ty0 * textureSize + tx0];
			if (ridgeNoise) {
				value = 0.5f - value;
				if (value < 0) {
					value = 2f * (0.5f + value);
				} else {
					value = 2f * (0.5f - value);
				}
			}
			return value;
		}

		/// <summary>
		/// Samples a 2D noise texture at given world position using bilinear filtering
		/// </summary>
		/// <returns>The noise value bilinear.</returns>
		/// <param name="noiseArray">Noise array.</param>
		/// <param name="textureSize">Texture size.</param>
		/// <param name="x">The x coordinate.</param>
		/// <param name="z">The z coordinate.</param>
		public static float GetNoiseValueBilinear(float[] noiseArray, int textureSize, float x, float z, bool ridgeNoise = false) {

			if (textureSize == 0)
				return 0;

			z = z + textureSize * 10000f + seedOffset.z;
			x = x + textureSize * 10000f + seedOffset.x;
			int posZInt = (int)z;
			int posXInt = (int)x;
			float fy = z - posZInt;
			float fx = x - posXInt;

			// Texture array position
			int ty0 = posZInt % textureSize;
			int tx0 = posXInt % textureSize;

			// Get noise for upper/left corner
			int ty, tx;
			ty = (ty0 == textureSize - 1) ? 0 : ty0 + 1;
			float noiseUL = noiseArray[ty * textureSize + tx0];
			// Get noise for upper/right corner
			tx = (tx0 == textureSize - 1) ? 0 : tx0 + 1;
			float noiseUR = noiseArray[ty * textureSize + tx];
			// Get noise for bottom/left corner
			float noiseBL = noiseArray[ty0 * textureSize + tx0];
			// Get noise for bottom/right corner
			float noiseBR = noiseArray[ty0 * textureSize + tx];

			// Bilinear interpolation
			float value =
				(1f - fx) * (fy * noiseUL + (1f - fy) * noiseBL) +
				fx * (fy * noiseUR + (1f - fy) * noiseBR);

			if (ridgeNoise) {
				value = 0.5f - value;
				if (value < 0) {
					value = 2f * (0.5f + value);
				} else {
					value = 2f * (0.5f - value);
				}
			}
			return value;
		}


		/// <summary>
		/// Samples a 3D noise texture at a given world position (returns raw value)
		/// </summary>
		/// <returns>The noise value one sample.</returns>
		/// <param name="noiseArray">Noise array.</param>
		/// <param name="textureSize">Texture size.</param>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <param name="z">The z coordinate.</param>
		public static float GetNoiseValue(float[] noiseArray, int textureSize, float x, float y, float z) {

			float f = GetNoiseValue (noiseArray, textureSize, x, z);
			float t = GetNoiseValue (noiseArray, textureSize, 1, y);

			f = f * t * 2.0f;
			if (f > 1f)
				f--;
			return f;

		}


	}

}