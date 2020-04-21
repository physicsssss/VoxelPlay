using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

namespace VoxelPlay {
				
	[CustomEditor (typeof(VoxelPlayBehaviour))]
	public class VoxelPlayBehaviourEditor : Editor {

		SerializedProperty enableVoxelLight, forceUnstuck;
		SerializedProperty checkNearChunks, chunkExtents, renderChunks;

		void OnEnable () {
			enableVoxelLight = serializedObject.FindProperty ("enableVoxelLight");
			forceUnstuck = serializedObject.FindProperty ("forceUnstuck");
			checkNearChunks = serializedObject.FindProperty ("checkNearChunks");
			chunkExtents = serializedObject.FindProperty ("chunkExtents");
			renderChunks = serializedObject.FindProperty ("renderChunks");
		}


		public override void OnInspectorGUI () {
			serializedObject.Update ();
			EditorGUILayout.Separator ();
			EditorGUI.BeginChangeCheck ();
			EditorGUILayout.PropertyField (enableVoxelLight, new GUIContent("Enable Voxel Light", "Enable this property to adjust material lighting based on voxel global illumination"));
			EditorGUILayout.PropertyField (forceUnstuck, new GUIContent("Force Unstuck", "Moves this gameobject to the surface of the terrain if it falls below or crosses a solid voxel"));
			EditorGUILayout.PropertyField (checkNearChunks, new GUIContent("Chunk Area", "Ensures all nearby chunks are generated"));
			if (checkNearChunks.boolValue) {
				EditorGUILayout.PropertyField (chunkExtents, new GUIContent("   Extents", "Distance in chunks around the transform position (1 chunk = 16 world units by default)"));
				EditorGUILayout.PropertyField (renderChunks, new GUIContent("   Render Chunks", "If this option is enabled, chunks within area will also be rendered. If this option is disabled, chunks will only be generated but no mesh/collider/navmesh will be generated."));
			}
			serializedObject.ApplyModifiedProperties ();
			if (EditorGUI.EndChangeCheck ()) {
				VoxelPlayBehaviour b = (VoxelPlayBehaviour)target;
				b.Refresh ();
			}
		}
	}

}
