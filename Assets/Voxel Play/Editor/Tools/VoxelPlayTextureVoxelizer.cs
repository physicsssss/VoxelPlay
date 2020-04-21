using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace VoxelPlay {
				
	public class VoxelPlayTextureVoxelizer : EditorWindow {

		Texture2D texture;
		Vector3 size = new Vector3 (1, 1, 1f / 16);

		[MenuItem ("Assets/Create/Voxel Play/Texture Voxelizer...", false, 1000)]
		public static void ShowWindow () {
			VoxelPlayTextureVoxelizer window = GetWindow<VoxelPlayTextureVoxelizer> ("Texture Voxelizer", true);
			window.minSize = new Vector2 (400, 140);
			window.Show ();
		}

		void OnGUI () {
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.HelpBox ("Create a voxel prefab from a 2D texture.", MessageType.Info);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.Separator ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Texture", GUILayout.Width (120));
			EditorGUI.BeginChangeCheck ();
			texture = (Texture2D)EditorGUILayout.ObjectField (texture, typeof(Texture2D), false);
			if (EditorGUI.EndChangeCheck ()) {
				if (texture != null) {
					VoxelPlayEditorCommons.CheckImportSettings (texture, true);
				}
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent ("Size", "Size of the resulting prefab."), GUILayout.Width (120));
			size = EditorGUILayout.Vector3Field ("", size);
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.Separator ();
			GUI.enabled = texture != null;
			EditorGUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Generate Prefab")) {
				GeneratePrefab ();
				GUIUtility.ExitGUI ();
			}
			if (GUILayout.Button ("Generate Item Definition")) {
				GenerateItemDefinitionAsset ();
				GUIUtility.ExitGUI ();
			}
			GUI.enabled = false;
			EditorGUILayout.EndHorizontal ();
		}

		GameObject GeneratePrefab () {
			ColorBasedModelDefinition baseModel = TextureToColorBasedModelDefinition (texture);
			if (baseModel.colors == null)
				return null;

			// Generate a cuboid per visible voxel
			int sizeX = baseModel.sizeX;
			int sizeY = baseModel.sizeY;
			int sizeZ = baseModel.sizeZ;
			Color32[] colors = baseModel.colors;

			Vector3 scale = new Vector3 (size.x / texture.width, size.y / texture.height, size.z);
			Vector3 offset = new Vector3 (0, -0.5f, 0);
			GameObject obj = VoxelPlayConverter.GenerateVoxelObject (colors, sizeX, sizeY, sizeZ, offset, scale);

			string path = GetPathForNewAsset ();
			path += "/" + GetFilenameForNewModel (baseModel.name) + ".prefab";
#if UNITY_2018_3_OR_NEWER
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
#else
			GameObject prefab = PrefabUtility.CreatePrefab (path, obj);
#endif
			// Store the mesh inside the prefab
			Mesh mesh = obj.GetComponent<MeshFilter> ().sharedMesh;
			AssetDatabase.AddObjectToAsset (mesh, prefab);
			prefab.GetComponent<MeshFilter> ().sharedMesh = mesh;
			Material mat = obj.GetComponent<MeshRenderer> ().sharedMaterial;
			AssetDatabase.AddObjectToAsset (mat, prefab);
			prefab.GetComponent<MeshRenderer> ().sharedMaterial = mat;
			MeshCollider mc = prefab.AddComponent<MeshCollider> ();
			mc.sharedMesh = mesh;
			mc.convex = true;
			Rigidbody rb = prefab.AddComponent<Rigidbody> ();
			rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
			AssetDatabase.SaveAssets ();
			DestroyImmediate (obj);

			EditorUtility.FocusProjectWindow ();
			Selection.activeObject = prefab;
			EditorGUIUtility.PingObject (prefab);

			return prefab;
		}


		void GenerateItemDefinitionAsset () {
			GameObject prefab = GeneratePrefab ();
			if (prefab == null)
				return;

			ItemDefinition itemDefinition = ScriptableObject.CreateInstance<ItemDefinition> ();
			itemDefinition.category = ItemCategory.General;
			itemDefinition.title = prefab.name;
			itemDefinition.icon = texture;
			itemDefinition.prefab = prefab;

			// Create a suitable file path
			string path = GetPathForNewAsset ();
			AssetDatabase.CreateAsset (itemDefinition, path + "/" + GetFilenameForNewModel (itemDefinition.title.Substring (0, 1).ToUpper () + itemDefinition.title.Substring (1)) + ".asset");
			AssetDatabase.SaveAssets ();
			EditorUtility.FocusProjectWindow ();
			Selection.activeObject = itemDefinition;
			EditorGUIUtility.PingObject (itemDefinition);
		}


		ColorBasedModelDefinition TextureToColorBasedModelDefinition (Texture2D texture) {
			Color32[] colors = texture.GetPixels32 ();
			ColorBasedModelDefinition model = new ColorBasedModelDefinition ();
			model.name = texture.name;
			model.sizeX = texture.width;
			model.sizeY = texture.height;
			model.sizeZ = 1;
			model.colors = colors;
			return model;
		}

		string GetPathForNewAsset () {
			return Path.GetDirectoryName (AssetDatabase.GetAssetPath (texture));
		}

		string GetFilenameForNewModel (string proposed) {
			if (string.IsNullOrEmpty (proposed)) {
				return "NewModel";
			}
			return String.Concat (proposed.Split (Path.GetInvalidFileNameChars ()));
		}



	}

}