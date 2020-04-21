using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

namespace VoxelPlay {
				
	[CustomEditor (typeof(VoxelChunk))]
	public class VoxelPlayChunkEditor : Editor {

        public override void OnInspectorGUI () {

			VoxelChunk chunk = (VoxelChunk)target;
			if (chunk == null) return;
            EditorGUILayout.LabelField("Pool Index", chunk.poolIndex.ToString());
            EditorGUILayout.LabelField("Position", chunk.position.ToString("F0"));
            EditorGUILayout.LabelField("Distance Status", chunk.visibleDistanceStatus.ToString());
            EditorGUILayout.LabelField ("Render Status", chunk.renderState.ToString ());
            EditorGUILayout.LabelField ("   In Queue", chunk.inqueue.ToString ());
            EditorGUILayout.LabelField ("Above Surface", chunk.isAboveSurface.ToString ());
            EditorGUILayout.LabelField ("Populated", chunk.isPopulated.ToString ());
            EditorGUILayout.LabelField ("Modified", chunk.modified.ToString ());
            EditorGUILayout.LabelField("Allow Trees", chunk.allowTrees.ToString());
        }

	}

}
