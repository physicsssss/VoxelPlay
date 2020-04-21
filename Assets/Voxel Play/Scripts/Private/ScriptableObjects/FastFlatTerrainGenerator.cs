using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	[CreateAssetMenu (menuName = "Voxel Play/Terrain Generators/Fast Flat Terrain Generator", fileName = "FastFlatTerrainGenerator", order = 103)]
	public class FastFlatTerrainGenerator : VoxelPlayTerrainGenerator {

		public int altitude = 50;
		public VoxelDefinition terrainVoxel;
		public Color32 voxelColor1 = new Color32 (0, 128, 0, 255);
		public Color32 voxelColor2 = new Color32 (128, 0, 0, 255);

		/// <summary>
		/// Used to initialize any data structure or reload
		/// </summary>
		protected override void Init () {
			if (terrainVoxel == null)
				terrainVoxel = VoxelPlayEnvironment.instance.defaultVoxel;
		}

		/// <summary>
		/// Gets the altitude and moisture
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="z">The z coordinate.</param>
		/// <param name="altitude">Altitude.</param>
		/// <param name="moisture">Moisture.</param>
		public override void GetHeightAndMoisture (float x, float z, out float altitude, out float moisture) {
			altitude = this.altitude / maxHeight;
			moisture = 0;
		}

		/// <summary>
		/// Paints the terrain inside the chunk defined by its central "position"
		/// </summary>
		/// <returns><c>true</c>, if terrain was painted, <c>false</c> otherwise.</returns>
		public override bool PaintChunk (VoxelChunk chunk) {
			int chunkBottomPos = FastMath.FloorToInt (chunk.position.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE);
			if (chunkBottomPos >= altitude) {
				return false; // does not have contents
			}

			// Voxel Color (checker board style)
			Color32 voxelColor;
			uint chance = (((uint)chunk.position.x + (uint)chunk.position.z) / VoxelPlayEnvironment.CHUNK_SIZE) % 2;
			if (chance == 1) {
				voxelColor = voxelColor1;
			} else {
				voxelColor = voxelColor2;
			}

			// a chunk is made of 16x16x16 voxels - calculate the last voxel position in the array to be filled
			// constant ONE_Y_ROW equals to the number of voxels in an horizontal slice (ie. 16*16 voxels per row)
			int maxY = altitude - chunkBottomPos;
			int lastVoxel = maxY * ONE_Y_ROW;
			if (lastVoxel >= chunk.voxels.Length)
				lastVoxel = chunk.voxels.Length;

			// Fill the chunk.voxels 3D array with voxels
			for (int k = 0; k < lastVoxel; k++) {
				chunk.voxels [k].Set (terrainVoxel, voxelColor);
			}

			// For correct light illumination, specify if this chunk on surface level
			int chunkTopPos = (int)chunk.position.y + VoxelPlayEnvironment.CHUNK_HALF_SIZE;
			chunk.isAboveSurface = chunkTopPos >= altitude;

			return true; // true = > this chunk has contents
		}

	}
}

