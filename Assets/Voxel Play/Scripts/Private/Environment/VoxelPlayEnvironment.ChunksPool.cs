using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		// Chunk buffer & cache
		VoxelChunk[] chunksPool;

		/// <summary>
		/// index of the chunk in the pool which is being created
		/// </summary>
		int chunksPoolCurrentIndex;

		/// <summary>
		/// used to annotate if last chunk produced a chunk with contents; use its voxel memory space otherwise
		/// </summary>
		bool chunksPoolFetchNew;

		/// <summary>
		/// used for incremental non-blocking preload of chunk slots
		/// </summary>
		int chunksPoolLoadIndex;

		/// <summary>
		/// used to optimize the look-up of reusable chunks due to lot of clouds chunks marked as non-reusable at the start of the buffer
		/// </summary>
		int chunksPoolFirstReusableIndex;

		#region Chunks pool functions

		void ReserveChunkMemory () {
			if (chunksPoolLoadIndex < maxChunks) {
				chunksPool [chunksPoolLoadIndex] = CreateChunkPoolEntry ();
                chunksPool [chunksPoolLoadIndex].poolIndex = chunksPoolLoadIndex;
                chunksPoolLoadIndex++;
			}
		}


		void FetchNewChunkIndex (Vector3 position) {
			if (chunksUsed >= chunksPool.Length) {
				ReuseChunkEntry (position);
			} else {
				if (chunksPoolCurrentIndex >= chunksPoolLoadIndex - 1) {
					for (int k = 0; k < 1000; k++) {
						ReserveChunkMemory ();
					}
				}
				chunksPoolCurrentIndex++;
				chunksUsed++;
			}
		}

		void ComputeFirstReusableChunk () {
			chunksPoolFirstReusableIndex = 0;
			if (chunksPool == null)
				return;
			for (int k = 0; k < chunksPool.Length; k++) {
				if (!chunksPool [k].cannotBeReused) {
					chunksPoolFirstReusableIndex = k;
					return;
				}
			}
		}

		/// <summary>
		/// Picks first chunk out of visible distance and not near to the current desired position
		/// </summary>
		void ReuseChunkEntry (Vector3 position) {
			bool valid = false;
			float visibleDistance = (_visibleChunksDistance + 1) * CHUNK_SIZE; // adds one to avoid chunks appearing and disappearing on the border of visible distance
            //TODO: Change chunk distance for reuse calculation
            float minDistance = 8 * CHUNK_SIZE;
			int lastGood = -1;
            bool notifyChunkReuse = OnChunkReuse != null;
			for (int i = 0; i < chunksPool.Length; i++) {
				if (++chunksPoolCurrentIndex >= chunksPool.Length) {
					chunksPoolCurrentIndex = chunksPoolFirstReusableIndex;
				}

				VoxelChunk chunk = chunksPool [chunksPoolCurrentIndex];

				// if chunk has been modified or chunk is marked as non reusable, skip it
				if (chunk.modified || chunk.cannotBeReused)
					continue;
				
				// if chunk is too near from desired position, skip it
				float dx = position.x - chunk.position.x;
				float dy = position.y - chunk.position.y;
				float dz = position.y - chunk.position.z;
				if (dx >= -minDistance && dx <= minDistance && dy >= -minDistance && dy <= minDistance && dz >= -minDistance && dz <= minDistance)
					continue;
				lastGood = chunksPoolCurrentIndex;

				// if chunk is within visible distance, skip it
				dx = currentAnchorPos.x - chunk.position.x;
				dz = currentAnchorPos.z - chunk.position.z;
				if (dx >= -visibleDistance && dx <= visibleDistance && dz >= -visibleDistance && dz <= visibleDistance)
                    continue;
				
				// check event confirmation
				if (notifyChunkReuse) {
                    bool canReuse;
                    OnChunkReuse(chunk, out canReuse);
					if (!canReuse)
						continue;
				}

				// chunk seems good, pick it up!
				valid = true;
				break;
			}
			if (lastGood >= 0)
				chunksPoolCurrentIndex = lastGood;
			if (!valid) {
				ShowMessage ("Reusing visible chunks (Chunks Pool Size value must be increased)");
			}
			VoxelChunk bestChunk = chunksPool [chunksPoolCurrentIndex];
			lock (lockLastChunkFetch) {
				if (bestChunk == lastChunkFetch)
					lastChunkFetch = null;
			}
			bestChunk.PrepareForReuse (effectiveGlobalIllumination ? (byte)0 : (byte)15);
			// Remove cached chunk at old position
			SetChunkOctreeIsDirty(bestChunk.position, true);
            // Force update chunk visible distance status
            TriggerFarChunksUnloadCheck();
        }

		/// <summary>
		/// Gets the octree from a position and clears the explored flag to ensure that area can be refreshed again.
		/// Optionally remove the chunk from cache.
		/// </summary>
		void SetChunkOctreeIsDirty(Vector3 position, bool removeFromCache) {
			int chunkX, chunkY, chunkZ;
			FastMath.FloorToInt (position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out chunkX, out chunkY, out chunkZ);
			int existingChunkHash = GetChunkHash (chunkX, chunkY, chunkZ);
			CachedChunk cachedChunk;
			if (cachedChunks.TryGetValue (existingChunkHash, out cachedChunk)) {
				Octree octree = cachedChunk.octree;
				while (octree != null) {
					octree.explored = false;
					if (octree.parent != null) {
						octree.parent.exploredChildren--;
					}
					octree = octree.parent;
				}
				if (removeFromCache) {
					cachedChunks.Remove (existingChunkHash);
				}
			}
		}


		VoxelChunk CreateChunkPoolEntry () {

			GameObject chunkGO = Instantiate<GameObject> (chunkPlaceholderPrefab, Misc.vector3far, Misc.quaternionZero, chunksRoot);
			chunkGO.layer = layerVoxels;
			chunkGO.hideFlags = HideFlags.DontSave;
			#if UNITY_EDITOR
			if (hideChunksInHierarchy) {
				chunkGO.hideFlags |= HideFlags.HideInHierarchy;
			}
			#endif
			chunkGO.name = "Chunk";
			VoxelChunk chunk = chunkGO.GetComponent<VoxelChunk> ();
			chunk.voxels = new Voxel[CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE];
			chunk.mf = chunkGO.GetComponent<MeshFilter> ();
			chunk.mf.sharedMesh = null;
			chunk.mr = chunkGO.GetComponent<MeshRenderer> ();
			chunk.mr.enabled = false;
			if (draftModeActive && !Application.isPlaying) {
				chunk.mr.receiveShadows = false;
				chunk.mr.shadowCastingMode = ShadowCastingMode.Off;
			} else {
				chunk.mr.receiveShadows = enableShadows;
				if (enableShadows) {
					chunk.mr.shadowCastingMode = ShadowCastingMode.On;
				} else {
					chunk.mr.shadowCastingMode = ShadowCastingMode.Off;
				}
			}
			chunk.mc = chunkGO.GetComponent<MeshCollider> ();
			return chunk;
		}

		#endregion

	}



}
