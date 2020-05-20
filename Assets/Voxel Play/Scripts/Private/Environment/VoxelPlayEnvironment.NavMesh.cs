using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.AI;
using NavMeshBuilder = UnityEngine.AI.NavMeshBuilder;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {
		
		NavMeshData navMeshData;
		NavMeshDataInstance navMeshInstance;
		NavMeshBuildSettings navMeshBuildSettings;
		List<NavMeshBuildSource> navMeshSources;
		AsyncOperation navMeshUpdateOperation;
		Bounds worldBounds;
		bool navMeshIsUpdating, navMeshHasNewData;

		void InitNavMesh () {
            if (!enableNavMesh) return;
			navMeshBuildSettings = NavMesh.GetSettingsByIndex (0);
			navMeshBuildSettings.agentClimb = 18f;
			navMeshBuildSettings.agentSlope = 80;
			navMeshBuildSettings.agentHeight = 18;
			navMeshBuildSettings.agentRadius = 8;
			navMeshSources = Misc.GetList<NavMeshBuildSource> (lowMemoryMode, 2048);
			navMeshData = new NavMeshData ();
			navMeshInstance = NavMesh.AddNavMeshData (navMeshData);
			worldBounds = new Bounds ();
		}

		void DestroyNavMesh () {
			if (navMeshInstance.valid) {
				NavMesh.RemoveNavMeshData (navMeshInstance);
			}
		}

		void AddChunkNavMesh (VoxelChunk chunk) {
			if (!applicationIsPlaying || (object)chunk.navMesh == null)
				return;
			if (chunk.navMeshSourceIndex < 0) {
				NavMeshBuildSource source = new NavMeshBuildSource ();
				source.shape = NavMeshBuildSourceShape.Mesh;
				source.size = chunk.navMesh.bounds.size;
				source.sourceObject = chunk.navMesh;
				source.transform = chunk.transform.localToWorldMatrix;
				int count = navMeshSources.Count;
				chunk.navMeshSourceIndex = count;
				navMeshSources.Add (source);
			} else {
				NavMeshBuildSource source = navMeshSources [chunk.navMeshSourceIndex];
				source.size = chunk.navMesh.bounds.size;
				source.sourceObject = chunk.navMesh;
				source.transform = chunk.transform.localToWorldMatrix;
				navMeshSources [chunk.navMeshSourceIndex] = source;
			}
			worldBounds.Encapsulate (chunk.mr.bounds);
			worldBounds.Expand (0.1f);
			navMeshHasNewData = true;
		}

		void UpdateNavMesh () {
			if (navMeshIsUpdating) {
				if (navMeshUpdateOperation.isDone) {
					if (navMeshInstance.valid) {
						NavMesh.RemoveNavMeshData (navMeshInstance);
					}
					navMeshInstance = NavMesh.AddNavMeshData (navMeshData);
					navMeshIsUpdating = false;
				}
			} else if (navMeshHasNewData) {
				try {
					navMeshUpdateOperation = NavMeshBuilder.UpdateNavMeshDataAsync (navMeshData, navMeshBuildSettings, navMeshSources, worldBounds);
					navMeshIsUpdating = true;
				} catch (Exception ex) {
					Debug.Log (ex.ToString ());
				}
				navMeshHasNewData = false;
			} 
		}
	}



}
