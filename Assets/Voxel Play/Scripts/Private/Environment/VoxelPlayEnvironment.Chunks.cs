using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		const string CHUNKS_ROOT = "Chunks Root";
		const string CHUNKS_EXPORT_ROOT = "Exported Chunks";

		// Optimization support
		VoxelChunk lastChunkFetch;
		int lastChunkFetchX, lastChunkFetchY, lastChunkFetchZ;
		object lockLastChunkFetch = new object ();

		#region Chunk functions

		void GetChunkCoordinates (Vector3 position, out int chunkX, out int chunkY, out int chunkZ) {
			FastMath.FloorToInt(position.x / VoxelPlayEnvironment.CHUNK_SIZE, position.y / VoxelPlayEnvironment.CHUNK_SIZE, position.z / VoxelPlayEnvironment.CHUNK_SIZE, out chunkX, out chunkY, out chunkZ);
		}

        [MethodImpl (256)] // equals to MethodImplOptions.AggressiveInlining
        int GetChunkHash (int chunkX, int chunkY, int chunkZ) {
			int x00 = WORLD_SIZE_DEPTH * WORLD_SIZE_HEIGHT * (chunkX + WORLD_SIZE_WIDTH);
			int y00 = WORLD_SIZE_DEPTH * (chunkY + WORLD_SIZE_HEIGHT);
			return x00 + y00 + chunkZ;
		}

        /// <summary>
        /// Gets the chunk if exits or create it if forceCreation is set to true.
        /// </summary>
        /// <returns><c>true</c>, if chunk fast was gotten, <c>false</c> otherwise.</returns>
        /// <param name="chunkX">Chunk x.</param>
        /// <param name="chunkY">Chunk y.</param>
        /// <param name="chunkZ">Chunk z.</param>
        /// <param name="chunk">Chunk.</param>
        /// <param name="createIfNotAvailable">If set to <c>true</c> force creation if chunk doesn't exist.</param>
		bool GetChunkFast (int chunkX, int chunkY, int chunkZ, out VoxelChunk chunk, bool createIfNotAvailable = false) {
            lock (lockLastChunkFetch) {
                if (lastChunkFetchX == chunkX && lastChunkFetchY == chunkY && lastChunkFetchZ == chunkZ && (object)lastChunkFetch != null) {
                    chunk = lastChunkFetch;
                    return true;
                }
            }
            int hash = GetChunkHash (chunkX, chunkY, chunkZ);
            STAGE = 501;
            CachedChunk cachedChunk;
            bool exists = cachedChunks.TryGetValue (hash, out cachedChunk);
            chunk = exists ? cachedChunk.chunk : null;

            if (createIfNotAvailable) {
                if (!exists) {
                    STAGE = 502;
                    // not yet created, create it
                    chunk = CreateChunk (hash, chunkX, chunkY, chunkZ, false);
                    exists = true;
                }
                if ((object)chunk == null) { // chunk is really empty, create it with empty space
                    STAGE = 503;
                    chunk = CreateChunk (hash, chunkX, chunkY, chunkZ, true);
                }
            }
            STAGE = 0;
            if (exists) {
                lock (lockLastChunkFetch) {
                    lastChunkFetchX = chunkX;
                    lastChunkFetchY = chunkY;
                    lastChunkFetchZ = chunkZ;
                    lastChunkFetch = chunk;
                }
                return (object)chunk != null;
            }
            chunk = null;
            return false;
        }


		VoxelChunk GetChunkOrCreate (Vector3 position) {
			int x, y, z;
			FastMath.FloorToInt (position.x / VoxelPlayEnvironment.CHUNK_SIZE, position.y / VoxelPlayEnvironment.CHUNK_SIZE, position.z / VoxelPlayEnvironment.CHUNK_SIZE, out x, out y, out z);
			VoxelChunk chunk;
			GetChunkFast (x, y, z, out chunk, true);
			return chunk;
		}


		VoxelChunk GetChunkOrCreate (int chunkX, int chunkY, int chunkZ) {
			VoxelChunk chunk;
			GetChunkFast (chunkX, chunkY, chunkZ, out chunk, true);
			return chunk;
		}

		VoxelChunk GetChunkIfExists (int hash) {
			CachedChunk cachedChunk;
			if (cachedChunks.TryGetValue (hash, out cachedChunk)) {
				return cachedChunk.chunk;
			}
			return null;
		}

		bool ChunkExists (int chunkX, int chunkY, int chunkZ) {
			
			CachedChunk cachedChunk;
			int hash = GetChunkHash (chunkX, chunkY, chunkZ);
			if (cachedChunks.TryGetValue (hash, out cachedChunk)) {
				return cachedChunk.chunk != null;
			}
			return false;
		}


        /// <summary>
        /// Creates the chunk.
        /// </summary>
        /// <returns>The chunk.</returns>
        /// <param name="hash">Hash.</param>
        /// <param name="chunkX">Chunk x.</param>
        /// <param name="chunkY">Chunk y.</param>
        /// <param name="chunkZ">Chunk z.</param>
        /// <param name="createEmptyChunk">If set to <c>true</c> create empty chunk.</param>
        /// <param name="complete">If set to <c>true</c> detail generators will fire as well as OnChunkCreated event. Chunk will be marked as populated and a refresh will be triggered if within view distance.</param>
        VoxelChunk CreateChunk (int hash, int chunkX, int chunkY, int chunkZ, bool createEmptyChunk, bool complete = true) {

			STAGE = 101;
			Vector3 position;
			position.x = chunkX * CHUNK_SIZE + CHUNK_HALF_SIZE;
			position.y = chunkY * CHUNK_SIZE + CHUNK_HALF_SIZE;
			position.z = chunkZ * CHUNK_SIZE + CHUNK_HALF_SIZE;

            STAGE = 102;
			CachedChunk cachedChunk;
			// Create entry in the dictionary
			if (!cachedChunks.TryGetValue (hash, out cachedChunk)) {
				cachedChunk = new CachedChunk ();
				cachedChunks [hash] = cachedChunk;
			}

			STAGE = 103;
			VoxelChunk chunk;
			if ((object)cachedChunk.chunk == null) {
				// Fetch a new entry in the chunks pool
				if (chunksPoolFetchNew) {
					chunksPoolFetchNew = false;
					FetchNewChunkIndex (position);
				}
				chunk = chunksPool [chunksPoolCurrentIndex];
			} else {
				chunk = cachedChunk.chunk;
			}

			// Paint voxels
			bool chunkHasContents = false;
			chunk.position = position;

			STAGE = 104;
			if (createEmptyChunk) {
				chunk.isAboveSurface = CheckIfChunkAboveTerrain (position);
			} else {
				if (world.infinite || (position.x >= -world.extents.x && position.x <= world.extents.x && position.y >= -world.extents.y && position.y <= world.extents.y && position.z >= -world.extents.z && position.z <= world.extents.z)) {
					if (OnChunkBeforeCreate != null) {
                        // allows a external function to fill the contents of this new chunk
                        bool isAboveSurface;
                        OnChunkBeforeCreate (position, out chunkHasContents, chunk.voxels, out isAboveSurface);
						chunk.isAboveSurface = isAboveSurface;
					}
					if (!chunkHasContents) {
						if (!chunk.isCloud) {
							chunkHasContents = world.terrainGenerator.PaintChunk (chunk);
						}
						chunk.isAboveSurface |= !chunkHasContents;
					}
				}
			}

			STAGE = 105;
			VoxelChunk nchunk;

			if (chunkHasContents || createEmptyChunk) {
				// lit chunk if not global illumination
				if (!effectiveGlobalIllumination) {
					chunk.ClearLightmap (15);
				}
				chunksPoolFetchNew = true;
				chunksCreated++;

				cachedChunk.chunk = chunk;

				if (complete) {
					chunk.isPopulated = true;

					// Check for detail generators
					if (worldHasDetailGenerators) {
						for (int d = 0; d < world.detailGenerators.Length; d++) {
							if (world.detailGenerators [d].enabled) {
								world.detailGenerators [d].AddDetail (chunk);
							}
						}
					}

					if (chunkHasContents) {
						// if chunk is near camera, request a render refresh
						bool sendRefresh = (chunkX >= visible_xmin && chunkX <= visible_xmax && chunkZ >= visible_zmin && chunkZ <= visible_zmax && chunkY >= visible_ymin && chunkY <= visible_ymax);
						if (sendRefresh) {
							ChunkRequestRefresh (chunk, false, true);
						}
						// Check if neighbours are inconclusive because this chunk was not present 
						nchunk = chunk.bottom;
						if ((object)nchunk != null && (nchunk.inconclusiveNeighbours & CHUNK_TOP) != 0) {
							ChunkRequestRefresh (nchunk, true, true);
						}
						nchunk = chunk.top;
						if ((object)nchunk != null && (nchunk.inconclusiveNeighbours & CHUNK_BOTTOM) != 0) {
							ChunkRequestRefresh (nchunk, true, true);
						}
						nchunk = chunk.left;
						if ((object)nchunk != null && (nchunk.inconclusiveNeighbours & CHUNK_RIGHT) != 0) {
							ChunkRequestRefresh (nchunk, true, true);
						}
						nchunk = chunk.right;
						if ((object)nchunk != null && (nchunk.inconclusiveNeighbours & CHUNK_LEFT) != 0) {
							ChunkRequestRefresh (nchunk, true, true);
						}
						nchunk = chunk.back;
						if ((object)nchunk != null && (nchunk.inconclusiveNeighbours & CHUNK_FORWARD) != 0) {
							ChunkRequestRefresh (nchunk, true, true);
						}
						nchunk = chunk.forward;
						if ((object)nchunk != null && (nchunk.inconclusiveNeighbours & CHUNK_BACK) != 0) {
							ChunkRequestRefresh (nchunk, true, true);
						}

					} else {
						chunk.renderState = ChunkRenderState.RenderingComplete;
					}

					if ((object)OnChunkAfterCreate != null) {
						OnChunkAfterCreate (chunk);
					}
				}

				STAGE = 0;
				return chunk;
            }
				chunk.renderState = ChunkRenderState.RenderingComplete;
				STAGE = 0;
				return null;
		}

		bool CheckIfChunkAboveTerrain (Vector3 position) {

			position.y += (CHUNK_HALF_SIZE-1);
			if (position.y < waterLevel && waterLevel > 0) {
				return false;
			}

			position.x -= CHUNK_HALF_SIZE;
			position.z -= CHUNK_HALF_SIZE;
			Vector3 pos = position;

			for (int z = 0; z < CHUNK_SIZE; z++) {
				pos.z = position.z + z;
				for (int x = 0; x < CHUNK_SIZE; x++) {
					pos.x = position.x + x;
					float groundLevel = GetHeightMapInfoFast (pos.x, pos.z).groundLevel;
					float surfaceLevel = waterLevel > groundLevel ? waterLevel : groundLevel;
					if (position.y >= surfaceLevel) {
						// chunk is above terrain or water
						return true;
					}
				}
			}

			return false;
		}


		void RefreshNineChunks (VoxelChunk chunk, bool forceMeshRefresh = false) {
			if (chunk == null)
				return;

			int chunkX, chunkY, chunkZ;
			FastMath.FloorToInt (chunk.position.x / CHUNK_SIZE, chunk.position.y / CHUNK_SIZE, chunk.position.z / CHUNK_SIZE, out chunkX, out chunkY, out chunkZ);

			VoxelChunk neighbour;
			for (int y = -1; y <= 1; y++) {
				for (int z = -1; z <= 1; z++) {
					for (int x = -1; x <= 1; x++) {
						GetChunkFast (chunkX + x, chunkY + y, chunkZ + z, out neighbour);
						if (neighbour != null) {
							ChunkRequestRefresh (neighbour, true, forceMeshRefresh);
						}
					}
				}
			}
		}


		void RebuildNeighbours (VoxelChunk chunk, int voxelIndex) {
			int px, py, pz;
			GetVoxelChunkCoordinates (voxelIndex, out px, out py, out pz);
			if (px == 0 && (object)chunk.left != null)
				ChunkRequestRefresh (chunk.left, false, true);
			else if (px == (CHUNK_SIZE - 1) && (object)chunk.right != null)
				ChunkRequestRefresh (chunk.right, false, true);
			if (py == 0 && (object)chunk.bottom != null)
				ChunkRequestRefresh (chunk.bottom, false, true);
			else if (py == (CHUNK_SIZE - 1) && (object)chunk.top != null)
				ChunkRequestRefresh (chunk.top, false, true);
			if (pz == 0 && (object)chunk.back != null)
				ChunkRequestRefresh (chunk.back, false, true);
			else if (pz == (CHUNK_SIZE - 1) && (object)chunk.forward != null)
				ChunkRequestRefresh (chunk.forward, false, true);
		}


		/// <summary>
		/// Clears a chunk
		/// </summary>
		void ChunkClearFast (VoxelChunk chunk) {
			chunk.ClearVoxels (noLightValue);

		}

		public bool GetChunkNavMeshIsReady (VoxelChunk chunk) {
			return chunk.navMeshSourceIndex >= 0;
		}

		public void ChunksExportAll() {
			if (cachedChunks == null) {
				return;
			}
			GameObject exportRoot = GameObject.Find (CHUNKS_EXPORT_ROOT);
			if (exportRoot != null) {
				DestroyImmediate (exportRoot);
			}
			exportRoot = new GameObject (CHUNKS_EXPORT_ROOT);
			exportRoot.transform.position = Misc.vector3zero;

			ExportlGlobalSettings settings =  exportRoot.AddComponent<ExportlGlobalSettings> ();
			settings.lightPosBuffer = Shader.GetGlobalVectorArray ("_VPPointLightPosition");
			settings.lightColorBuffer = Shader.GetGlobalVectorArray ("_VPPointLightColor");
			settings.lightCount = Shader.GetGlobalInt ("_VPPointLightCount");
			settings.emissionIntensity = Shader.GetGlobalFloat ("_VPEmissionIntensity");
			settings.skyTint = Shader.GetGlobalColor ("_VPSkyTint");
			settings.fogData = Shader.GetGlobalVector ("_VPFogData");
			settings.fogAmount = Shader.GetGlobalFloat ("_VPFogAmount");
			settings.exposure = Shader.GetGlobalFloat ("_VPExposure");
			settings.ambientLight = Shader.GetGlobalFloat ("_VPAmbientLight");
			settings.daylightShadowAtten = Shader.GetGlobalFloat ("_VPDaylightShadowAtten");
			settings.enableFog = Shader.IsKeywordEnabled (SKW_VOXELPLAY_GLOBAL_USE_FOG);

			foreach (KeyValuePair<int, CachedChunk>kv in cachedChunks) {
				if (kv.Value == null)
					continue;
				VoxelChunk chunk = kv.Value.chunk;
				if (chunk == null)
					continue;
				if (chunk.mf.sharedMesh != null) {
					chunk.gameObject.hideFlags = 0;
					chunk.mf.sharedMesh.hideFlags = 0;
					if (chunk.mc != null && chunk.mc.sharedMesh != null) {
						chunk.mc.sharedMesh.hideFlags = 0;
					}
					chunk.transform.SetParent (exportRoot.transform, true);
				}
			}
            cachedChunks.Clear();

			#if UNITY_EDITOR
			// Mark scene as modified
			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
			UnityEditor.EditorUtility.DisplayDialog("Export Chunks", "Chunks now available under 'Exported Chunks' node in hierarchy as regular gameobjects. Materials, textures and meshes are now part of the scene.\n\nThe 'ExportGlobalSettings' behaviour has been attached to 'Exported Chunks' root gameobject to keep global shader values.\nYou can now remove Voxel Play Environment gameobject completely if you wish.", "Ok");
			#endif

		}

		#endregion

	}



}
