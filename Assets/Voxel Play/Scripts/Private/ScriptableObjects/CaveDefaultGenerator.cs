using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	[CreateAssetMenu (menuName = "Voxel Play/Detail Generators/Cave Generator", fileName = "CaveGenerator", order = 101)]
	public class CaveDefaultGenerator : VoxelPlayDetailGenerator {

		struct HoleWorm {
			public Vector3 head;
			public int life;
			public int lastX, lastY, lastZ;
			public float ax, ay, az;
		}

		List<HoleWorm> worms;

		VoxelPlayEnvironment env;
		Dictionary<Vector3, bool> wormBorn;
		float[] noiseValues;
		uint texSize;
		int caveBorder;

		/// <summary>
		/// Initialization method. Called by Voxel Play at startup.
		/// </summary>
		public override void Init () {
			env = VoxelPlayEnvironment.instance;
			wormBorn = new Dictionary<Vector3, bool> (100);
			texSize = 1024;
			noiseValues = new float[texSize];
			const int octaves = 4;
			for (int o = 1; o <= octaves; o++) {
				float v = 0;
				for (int k = 0; k < texSize; k++) {
					v += (Random.value - 0.5f) * o / 10f;
					noiseValues [k] = (noiseValues [k] + v) * 0.5f;
				}
			}
			// Clamp
			for (int k = 0; k < texSize; k++) {
				if (noiseValues [k] < -0.5f)
					noiseValues [k] = -0.5f;
				else if (noiseValues [k] > 0.5f)
					noiseValues [k] = 0.5f;
			}
			worms = new List<HoleWorm> (100);
		}


        /// <summary>
        /// Called by Voxel Play to inform that player has moved onto another chunk so new detail can start generating
        /// </summary>
        /// <param name="position">Current player position.</param>
        /// <param name="checkOnlyBorders">True means the player has moved to next chunk. False means player position is completely new and all chunks in
        /// range should be checked for detail in this call.</param>
        /// <param name="endTime">Provides a maximum time frame for execution this frame. Compare this with env.stopwatch milliseconds.</param>
        public override void ExploreArea (Vector3 position, bool checkOnlyBorders, long endTime) {
			int explorationRange = env.visibleChunksDistance + 10;
			int minz = -explorationRange;
			int maxz = +explorationRange;
			int minx = -explorationRange;
			int maxx = +explorationRange;
			HoleWorm worm;
			Vector3 pos = position;
			for (int z = minz; z <= maxz; z++) {
				for (int x = minx; x < maxx; x++) {
					if (checkOnlyBorders && z > minz && z < maxz && x > minx && x < maxx)
						continue;
					pos.x = position.x + x * VoxelPlayEnvironment.CHUNK_SIZE;
					pos.z = position.z + z * VoxelPlayEnvironment.CHUNK_SIZE;
					pos = env.GetChunkPosition (pos);
					if (WorldRand.GetValue (pos) > 0.98f) {
						bool born;
						pos.y = env.GetTerrainHeight (pos);
						if (pos.y > env.waterLevel && !wormBorn.TryGetValue (pos, out born)) {
							if (!born) {
								worm.head = pos;
								worm.life = 2000;
								worm.lastX = worm.lastY = worm.lastZ = int.MinValue;
								worm.ax = worm.ay = worm.az = 0;
								worms.Add (worm);
							}
							wormBorn [pos] = true;
						}
					}
				}
			}

			if (!checkOnlyBorders) {
				for (int k = 0; k < 1000; k++) {
					if (!DoWork (endTime))
						break;
				}
			}
		}

		/// <summary>
		/// Move worms
		/// </summary>
		public override bool DoWork (long endTime) {

			int count = worms.Count;
			if (count == 0)
				return false;
            const int MAX_ITERATIONS = 10000;

			for (int p = 0; p < MAX_ITERATIONS; p++) {
				for (int k = 0; k < count; k++) {
					env.STAGE = 3000;
					HoleWorm worm = worms [k];
					uint xx = (uint)worm.head.x % texSize;
					worm.ax += noiseValues [xx];
					if (worm.ax > 1f)
						worm.ax = 1f;
					else if (worm.ax < -1f)
						worm.ax = -1f;
					worm.head.x += worm.ax;
					uint yy = (uint)worm.head.y % texSize;
					worm.ay += noiseValues [yy];
					if (worm.ay > 1f)
						worm.ay = 1f;
					else if (worm.ay < -1f)
						worm.ay = -1f;
					worm.head.y += worm.ay;
					uint zz = (uint)worm.head.z % texSize;
					worm.az += noiseValues [zz];
					if (worm.az > 1f)
						worm.az = 1f;
					else if (worm.az < -1f)
						worm.az = -1f;
					worm.head.z += worm.az;
					int ix = (int)(worm.head.x);
					int iy = (int)(worm.head.y);
					int iz = (int)(worm.head.z);
					env.STAGE = 3001;
					if (ix != worm.lastX || iy != worm.lastY || iz != worm.lastZ) {
						worm.lastX = ix;
						worm.lastY = iy;
						worm.lastZ = iz;

						// keep this order of assignment to improve randomization
						int minx = ix - (caveBorder++ & 7);
						int miny = iy - (caveBorder++ & 7);
						int maxx = ix + (caveBorder++ & 3);
						int minz = iz - (caveBorder++ & 7);
						int maxy = iy + (caveBorder++ & 3);
						int maxz = iz + (caveBorder++ & 3);
						int mx = (maxx + minx) / 2;
						int my = (maxy + miny) / 2;
						int mz = (maxz + minz) / 2;

						VoxelChunk chunk = null;
						int lastChunkX = int.MinValue, lastChunkY = int.MinValue, lastChunkZ = int.MinValue;
						for (int y = miny; y < maxy; y++) {
							int chunkY = FastMath.FloorToInt (y / (float)VoxelPlayEnvironment.CHUNK_SIZE);
							int py = y - chunkY * VoxelPlayEnvironment.CHUNK_SIZE;
							int voxelIndexY = py * ONE_Y_ROW;
                            int dy = y - my;
                            dy *= dy;
                            for (int z = minz; z < maxz; z++) {
								int chunkZ = FastMath.FloorToInt (z / (float)VoxelPlayEnvironment.CHUNK_SIZE);
								int pz = z - chunkZ * VoxelPlayEnvironment.CHUNK_SIZE;
								int voxelIndexZ = voxelIndexY + pz * ONE_Z_ROW;
                                int dz = z - mz;
                                int dyz = dy + dz * dz;
                                for (int x = minx; x < maxx; x++) {
									int dx = x - mx;
									if (dx * dx + dyz > 23) {
										continue;
									}
									int chunkX = FastMath.FloorToInt (x / (float)VoxelPlayEnvironment.CHUNK_SIZE);
									if (chunkX != lastChunkX || chunkZ != lastChunkZ || chunkY != lastChunkY) {

										lastChunkX = chunkX;
										lastChunkY = chunkY;
										lastChunkZ = chunkZ;
										env.STAGE = 3004;
										chunk = env.GetChunkUnpopulated (chunkX, chunkY, chunkZ);
										env.STAGE = 3005;
										if (chunk.isPopulated) {
											worm.life = 0;
											y = maxy;
											z = maxz;
											break;
										}
										// mark the chunk as modified by this detail generator
										SetChunkIsDirty (chunk);
									}

									int px = x - chunkX * VoxelPlayEnvironment.CHUNK_SIZE;
									int voxelIndex = voxelIndexZ + px;

                                    // set this voxel as a "hole" (hasContent = 2) so it doesn't get filled by terrain generator when it creates terrain
									chunk.voxels [voxelIndex].hasContent = 2;
								}
							}
						}
						worm.life--;
						if (worm.life <= 0) {
							env.STAGE = 3007;
							worms.RemoveAt (k);
							env.STAGE = 0;
							return true;
						}
					}
					worms [k] = worm;

					long elapsed = env.stopWatch.ElapsedMilliseconds;
                    if (elapsed >= endTime) {
                        p = MAX_ITERATIONS; // exit outer block
                        break;
                    }
				}
				env.STAGE = 0;
			}
			return true;
		}

	}

}