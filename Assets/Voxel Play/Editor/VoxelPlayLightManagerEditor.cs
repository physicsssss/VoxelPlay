using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;

namespace VoxelPlay.GPULighting {
				
	[CustomEditor (typeof(VoxelPlayLightManager))]
	public class VoxelPlayLightManagerEditor : Editor {

		public override void OnInspectorGUI () {
			EditorGUILayout.HelpBox ("This camera script manages point light rendering in the Voxel Play Environment.", MessageType.Info);
		}

	}

}
