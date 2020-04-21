using System;
using System.IO;

using UnityEngine;
using UnityEditor;

namespace VoxelPlay {
				
	public class VoxelPlayImportTools : EditorWindow {

		enum ImportFormat {
			QubicleBinary
		}


		// Model import tools
		ImportFormat importFormat;
		bool importIgnoreOffset = true;
		bool importIgnoreTransparency = true;
		string importFilename;
		Vector3 scale = Misc.vector3one;
		ColorToVoxelMap mapping;

		[MenuItem ("Assets/Create/Voxel Play/Import Tools...", false, 151)]
		public static void ShowWindow () {
			VoxelPlayImportTools window = GetWindow<VoxelPlayImportTools> ("Import Tools", true);
			window.minSize = new Vector2 (400, 140);
			window.Show ();
		}

		void OnGUI () {
			EditorGUIUtility.wideMode = true;

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.HelpBox ("Import voxel models from other applications.", MessageType.Info);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.Separator ();

			importFormat = (ImportFormat)EditorGUILayout.EnumPopup (new GUIContent("Format"), importFormat);

			EditorGUILayout.BeginHorizontal ();
			importFilename = EditorGUILayout.TextField (new GUIContent("File name"), importFilename);
			if (GUILayout.Button ("Open...", GUILayout.Width (80))) {
				importFilename = EditorUtility.OpenFilePanel ("Select model File (*.qb)", "", "qb");
			}
			EditorGUILayout.EndHorizontal ();

			mapping = (ColorToVoxelMap) EditorGUILayout.ObjectField (new GUIContent ("Color-Voxel Map", "Optional color to voxel mapping."), mapping, typeof(ColorToVoxelMap), false);
			importIgnoreOffset = EditorGUILayout.Toggle (new GUIContent ("Ignore Offset", "Model can specify an offset for the center."), importIgnoreOffset);
			scale = EditorGUILayout.Vector3Field (new GUIContent ("Scale", "Scale applied to the model."), scale);
			importIgnoreTransparency = EditorGUILayout.Toggle (new GUIContent ("Ignore Transparency", "Ignore alpha values when determining unique colors."), importIgnoreTransparency);

			EditorGUILayout.Separator ();
			GUI.enabled = !string.IsNullOrEmpty (importFilename);
			EditorGUILayout.BeginHorizontal ();
			if (GUILayout.Button ("Generate ColorMap Asset")) {
				GenerateColorMapAsset ();
				GUIUtility.ExitGUI ();
			}
			if (GUILayout.Button ("Generate Model Asset")) {
				GenerateModelAsset ();
				GUIUtility.ExitGUI ();
			}
			if (GUILayout.Button ("Generate Prefab")) {
				GeneratePrefab ();
				GUIUtility.ExitGUI ();
			}
			GUI.enabled = false;
			EditorGUILayout.EndHorizontal ();
		}


		void GenerateColorMapAsset () {
			ColorBasedModelDefinition baseModel = QubicleBinaryToColorBasedModelDefinition ();
			if (baseModel.colors == null)
				return;
			ColorToVoxelMap colorMap = VoxelPlayConverter.GetColorToVoxelMapDefinition (baseModel, importIgnoreTransparency);
			if (!string.IsNullOrEmpty (baseModel.name)) {
				colorMap.name = baseModel.name;
			}

			colorMap.name += " ColorMap";

			// Create a suitable file path
			string path = GetPathForNewAsset ();
			AssetDatabase.CreateAsset (colorMap, path + "/" + GetFilenameForNewModel (colorMap.name) + ".asset");
			AssetDatabase.SaveAssets ();
			EditorUtility.FocusProjectWindow ();
			Selection.activeObject = colorMap;
			EditorGUIUtility.PingObject (colorMap);
		}


		void GenerateModelAsset () {
			ColorBasedModelDefinition baseModel = QubicleBinaryToColorBasedModelDefinition ();
			if (baseModel.colors == null)
				return;
			ModelDefinition newModel = VoxelPlayConverter.GetModelDefinition (null, baseModel, importIgnoreOffset, mapping);
			if (!string.IsNullOrEmpty (baseModel.name)) {
				newModel.name = baseModel.name;
			}

			// Create a suitable file path
			string path = GetPathForNewAsset ();
			AssetDatabase.CreateAsset (newModel, path + "/" + GetFilenameForNewModel (newModel.name) + ".asset");
			AssetDatabase.SaveAssets ();
			EditorUtility.FocusProjectWindow ();
			Selection.activeObject = newModel;
			EditorGUIUtility.PingObject (newModel);
		}


		void GeneratePrefab () {
			ColorBasedModelDefinition baseModel = QubicleBinaryToColorBasedModelDefinition ();
			if (baseModel.colors == null)
				return;

			// Generate a cuboid per visible voxel
			int sizeX = baseModel.sizeX;
			int sizeY = baseModel.sizeY;
			int sizeZ = baseModel.sizeZ;
			float offsetX = 0, offsetY = 0, offsetZ = 0;
			if (!importIgnoreOffset) {
				offsetX += baseModel.offsetX;
				offsetY += baseModel.offsetY;
				offsetZ += baseModel.offsetZ;
			}
			Color32[] colors = baseModel.colors;

			GameObject obj = VoxelPlayConverter.GenerateVoxelObject (colors, sizeX, sizeY, sizeZ, new Vector3(offsetX, offsetY, offsetZ), scale);

			string path = GetPathForNewAsset ();
			path += "/" + GetFilenameForNewModel (baseModel.name) + ".prefab";
#if UNITY_2018_3_OR_NEWER
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
#else
			GameObject prefab = PrefabUtility.CreatePrefab (path, obj);
#endif
			// Store the mesh inside the prefab
			Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
			AssetDatabase.AddObjectToAsset (mesh, prefab);
			prefab.GetComponent<MeshFilter> ().sharedMesh = mesh;
			Material mat = obj.GetComponent<MeshRenderer> ().sharedMaterial;
			AssetDatabase.AddObjectToAsset(mat, prefab);
			prefab.GetComponent<MeshRenderer> ().sharedMaterial = mat;
			MeshCollider mc = prefab.AddComponent<MeshCollider> ();
			mc.sharedMesh = mesh;
			mc.convex = true;
			Rigidbody rb = prefab.AddComponent<Rigidbody> ();
			rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
			AssetDatabase.SaveAssets ();
			DestroyImmediate (obj);

			EditorUtility.FocusProjectWindow ();
			Selection.activeObject = prefab;
			EditorGUIUtility.PingObject (prefab);
		}


		ColorBasedModelDefinition QubicleBinaryToColorBasedModelDefinition () {
			ColorBasedModelDefinition baseModel = ColorBasedModelDefinition.Null;
			Stream file = System.IO.File.Open (importFilename, FileMode.Open);
			try {
				baseModel = QubicleImporter.ImportBinary (file, System.Text.Encoding.UTF8);
			} catch {
			} finally {
				file.Close ();
			}
			return baseModel;
		}

		string GetPathForNewAsset () {
			string path;
			if (VoxelPlayEnvironment.instance != null) {
				path = AssetDatabase.GetAssetPath (VoxelPlayEnvironment.instance.world);
				path = System.IO.Path.GetDirectoryName (path) + "/Models";
			} else {
				path = "Assets/ImportedModels";
			}
			System.IO.Directory.CreateDirectory (path);
			return path;
		}

		string GetFilenameForNewModel (string proposed) {
			if (string.IsNullOrEmpty (proposed)) {
				return "NewModel";
			}
            return String.Concat (proposed.Split (Path.GetInvalidFileNameChars ()));
		}



	}

}