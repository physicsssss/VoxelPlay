using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VoxelPlay;

public class VoxelPlayReplaceModelDefinitionTool : EditorWindow {

	public ModelDefinition md;
	public VoxelDefinition vd1, vd2;
	public bool replaceColor;
	public Color color = Color.white;


	[MenuItem ("Assets/Create/Voxel Play/Replace Model Voxels...", false, 1000)]
	public static void ShowWindow () {
		VoxelPlayReplaceModelDefinitionTool window = GetWindow<VoxelPlayReplaceModelDefinitionTool> ("Replace Voxels", true);
		window.minSize = new Vector2 (400, 140);
		window.Show ();
	}

	void OnGUI () {
		EditorGUILayout.BeginHorizontal ();
		EditorGUILayout.HelpBox ("Replace voxels in a model definition.", MessageType.Info);
		EditorGUILayout.EndHorizontal ();
		EditorGUILayout.Separator ();

		EditorGUILayout.BeginHorizontal ();
		EditorGUILayout.LabelField ("Model Definition", GUILayout.Width (120));
		md = (ModelDefinition)EditorGUILayout.ObjectField (md, typeof(ModelDefinition), false);
		EditorGUILayout.EndHorizontal ();

		EditorGUILayout.BeginHorizontal ();
		EditorGUILayout.LabelField ("Replace Voxel", GUILayout.Width (120));
		vd1 = (VoxelDefinition)EditorGUILayout.ObjectField (vd1, typeof(VoxelDefinition), false);
		EditorGUILayout.EndHorizontal ();

		EditorGUILayout.BeginHorizontal ();
		EditorGUILayout.LabelField ("With Voxel", GUILayout.Width (120));
		vd2 = (VoxelDefinition)EditorGUILayout.ObjectField (vd2, typeof(VoxelDefinition), false);
		EditorGUILayout.EndHorizontal ();


		EditorGUILayout.BeginHorizontal ();
		EditorGUILayout.LabelField ("Replace Color", GUILayout.Width (120));
		replaceColor = EditorGUILayout.Toggle (replaceColor);
		EditorGUILayout.EndHorizontal ();

		if (replaceColor) {
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("New Color", GUILayout.Width (120));
			color = EditorGUILayout.ColorField (color);
			EditorGUILayout.EndHorizontal ();
		}

		if (GUILayout.Button ("Replace!")) {
			Replace ();
		}

	}


	void Replace() {
		if (md == null || vd1 == null || vd2 == null)
			return;

		int changes = 0;
		for (int k = 0; k < md.bits.Length; k++) {
			if (md.bits [k].voxelDefinition == vd1) {
				changes++;
				md.bits [k].voxelDefinition = vd2;
				if (replaceColor)
					md.bits [k].color = color;
			}
		}

		Debug.Log ("Modified " + changes + " voxels in model...");
		EditorUtility.SetDirty (md);
		AssetDatabase.SaveAssets ();
		AssetDatabase.Refresh ();

	}
}
