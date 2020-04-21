using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace VoxelPlay {

	[Serializable]
	public class VoxelPlayCubeTools : EditorWindow {

		public enum CubeShadingStyle {
			Color = 0,
			Textured = 1,
			TexturedAlpha = 2
		}

		public enum CubeVertexOffsetOption {
			Custom = 0,
			DoorOpensToTheRight = 10,
			DoorOpensToTheLeft = 11
		}

		[Serializable]
		public struct CubeSideSettings {
			public Texture2D texture;
			public Color color;
			public Vector2 uvMin;
			public Vector2 uvMax;
		}

		static Vector3[] faceVerticesForward = {
			new Vector3 (0.5f, -0.5f, 0.5f),
			new Vector3 (0.5f, 0.5f, 0.5f),
			new Vector3 (-0.5f, -0.5f, 0.5f),
			new Vector3 (-0.5f, 0.5f, 0.5f)
		};
		static Vector3[] faceVerticesBack = {
			new Vector3 (-0.5f, -0.5f, -0.5f),
			new Vector3 (-0.5f, 0.5f, -0.5f),
			new Vector3 (0.5f, -0.5f, -0.5f),
			new Vector3 (0.5f, 0.5f, -0.5f)
		};
		static Vector3[] faceVerticesLeft = {
			new Vector3 (-0.5f, -0.5f, 0.5f),
			new Vector3 (-0.5f, 0.5f, 0.5f),
			new Vector3 (-0.5f, -0.5f, -0.5f),
			new Vector3 (-0.5f, 0.5f, -0.5f)
		};
		static Vector3[] faceVerticesRight = {
			new Vector3 (0.5f, -0.5f, -0.5f),
			new Vector3 (0.5f, 0.5f, -0.5f),
			new Vector3 (0.5f, -0.5f, 0.5f),
			new Vector3 (0.5f, 0.5f, 0.5f)
		};
		static Vector3[] faceVerticesTop = {
			new Vector3 (-0.5f, 0.5f, 0.5f),
			new Vector3 (0.5f, 0.5f, 0.5f),
			new Vector3 (-0.5f, 0.5f, -0.5f),
			new Vector3 (0.5f, 0.5f, -0.5f)
		};
		static Vector3[] faceVerticesBottom = {
			new Vector3 (-0.5f, -0.5f, -0.5f),
			new Vector3 (0.5f, -0.5f, -0.5f),
			new Vector3 (-0.5f, -0.5f, 0.5f),
			new Vector3 (0.5f, -0.5f, 0.5f)
		};
		static Vector3[] normalsBack = {
			Misc.vector3back, Misc.vector3back, Misc.vector3back, Misc.vector3back
		};
		static Vector3[] normalsForward = {
			Misc.vector3forward, Misc.vector3forward, Misc.vector3forward, Misc.vector3forward
		};
		static Vector3[] normalsLeft = {
			Misc.vector3left, Misc.vector3left, Misc.vector3left, Misc.vector3left
		};
		static Vector3[] normalsRight = {
			Misc.vector3right, Misc.vector3right, Misc.vector3right, Misc.vector3right
		};
		static Vector3[] normalsUp =  {
			Misc.vector3up, Misc.vector3up, Misc.vector3up, Misc.vector3up
		};
		static Vector3[] normalsDown = {
			Misc.vector3down, Misc.vector3down, Misc.vector3down, Misc.vector3down
		};



		[SerializeField]
		public string cubeName = "Cube";

		[SerializeField]
		public int textureAtlasSize = 2048;

		[SerializeField]
		public CubeSideSettings[] sides;

		[SerializeField]
		public CubeShadingStyle cubeShadingStyle = CubeShadingStyle.Color;

		[SerializeField]
		public Vector3 scale = Misc.vector3one;

		[SerializeField]
		public Vector3 offset;

		[SerializeField]
		public CubeVertexOffsetOption offsetOption = CubeVertexOffsetOption.Custom;

		[SerializeField]
		public Texture2D icon;


		string[] sideNames = { "Top", "Bottom", "Forward", "Back", "Left", "Right" };
		List<Vector3> tempVertices;
		List<Vector3> tempNormals;
		List<Vector2> tempUVs;
		int[] tempIndices;
		List<Color32> tempColors;
		Rect[] uvRects;
		int tempIndicesPos;

		[MenuItem ("Assets/Create/Voxel Play/Cube-Door Creator...", false, 1000)]
		public static void ShowWindow () {
			VoxelPlayCubeTools window = GetWindow<VoxelPlayCubeTools> ("Cube/Door Creator", true);
			window.minSize = new Vector2 (300, 450);
			window.Show ();
		}

		void OnEnable () {
			if (sides == null || sides.Length < 6) {
				sides = new CubeSideSettings[6];
				for (int k = 0; k < sides.Length; k++) {
					sides [k].color = Misc.colorWhite;
				}
			}
			tempVertices = new List<Vector3> (36);
			tempNormals = new List<Vector3> (36);
			tempUVs = new List<Vector2> (36);
			tempIndices = new int[36];
			tempColors = new List<Color32> (36);
		}


		void OnGUI () {
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.HelpBox ("Create custom cube models.", MessageType.Info);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.Separator ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Name", GUILayout.Width (120));
			cubeName = EditorGUILayout.TextField (cubeName);
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Shading Style", GUILayout.Width (120));
			cubeShadingStyle = (CubeShadingStyle)EditorGUILayout.EnumPopup (cubeShadingStyle);
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Vertex Scale", GUILayout.Width (120));
			scale = EditorGUILayout.Vector3Field ("", scale);
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent ("Offset Option", "Vertex offset option"), GUILayout.Width (120));
			offsetOption = (CubeVertexOffsetOption)EditorGUILayout.EnumPopup (offsetOption);
			EditorGUILayout.EndHorizontal ();

			switch (offsetOption) {
			case CubeVertexOffsetOption.DoorOpensToTheLeft:
				offset = new Vector3 (0.5f - scale.z * 0.5f, scale.y * 0.5f, 0);
				break;
			case CubeVertexOffsetOption.DoorOpensToTheRight:
				offset = new Vector3 (-0.5f + scale.z * 0.5f, scale.y * 0.5f, 0);
				break;
			}

			GUI.enabled = (offsetOption == CubeVertexOffsetOption.Custom);
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField (new GUIContent ("   Custom Offset", "Applied after scale"), GUILayout.Width (120));
			offset = EditorGUILayout.Vector3Field ("", offset);
			EditorGUILayout.EndHorizontal ();
			GUI.enabled = true;

			EditorGUILayout.Separator ();

			if (offsetOption != CubeVertexOffsetOption.Custom) {
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Icon", GUILayout.Width (120));
				icon = (Texture2D)EditorGUILayout.ObjectField (icon, typeof(Texture2D), false);
				EditorGUILayout.EndHorizontal ();
			}

			if (cubeShadingStyle != CubeShadingStyle.Color) {
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Texture Atlas Size", GUILayout.Width (120));
				textureAtlasSize = EditorGUILayout.IntField (textureAtlasSize);
				EditorGUILayout.EndHorizontal ();
			}

			EditorGUILayout.Separator ();

			for (int k = 0; k < sides.Length; k++) {
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField (sideNames [k] + " side", GUILayout.Width (120));
				EditorGUILayout.EndHorizontal ();

				if (cubeShadingStyle != CubeShadingStyle.Color) {
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("   Texture", GUILayout.Width (120));
					sides [k].texture = (Texture2D)EditorGUILayout.ObjectField (sides [k].texture, typeof(Texture2D), false);
					EditorGUILayout.EndHorizontal ();
				}

				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("   Color", GUILayout.Width (120));
				sides [k].color = EditorGUILayout.ColorField (sides [k].color);
				EditorGUILayout.EndHorizontal ();

			}

			EditorGUILayout.Separator ();
			EditorGUILayout.BeginHorizontal ();
			if (offsetOption != CubeVertexOffsetOption.Custom) {
				if (GUILayout.Button ("Generate Door Prefab & Voxel Definition", GUILayout.Width (300))) {
					VoxelDefinition vd = GenerateDoorVoxel ();
					if (vd != null) {
						EditorUtility.FocusProjectWindow ();
						Selection.activeObject = vd;
					}
					GUIUtility.ExitGUI ();
				}
			} else {
				if (GUILayout.Button ("Generate Cube Prefab", GUILayout.Width (180))) {
					GameObject prefab = GenerateCubePrefab ();
					if (prefab != null) {
						EditorUtility.FocusProjectWindow ();
						Selection.activeObject = prefab;
					}
					GUIUtility.ExitGUI ();
				}
			}
			EditorGUILayout.EndHorizontal ();
		}

		GameObject GenerateCubePrefab () {

			string path = GetPathForNewCube ();
			path += "/" + cubeName + ".prefab";
			if (File.Exists (path)) {
				UnityEngine.Object o = AssetDatabase.LoadAssetAtPath (path, typeof(UnityEngine.Object));
				Selection.activeObject = o;
				EditorGUIUtility.PingObject (o);
				EditorUtility.DisplayDialog ("Error saving prefab", "A prefab with same name already exists on destination folder - choose another name.", "Ok");
				return null;
			}

			Material mat;
			if (cubeShadingStyle == CubeShadingStyle.Textured) {
				mat = Resources.Load<Material> ("VoxelPlay/Materials/VP Model Texture");
			} else if (cubeShadingStyle == CubeShadingStyle.TexturedAlpha) {
				mat = Resources.Load<Material> ("VoxelPlay/Materials/VP Model Texture Alpha");
			} else {
				mat = Resources.Load<Material> ("VoxelPlay/Materials/VP Model VertexLit");
			}

			if (cubeShadingStyle != CubeShadingStyle.Color) {
				Texture2D tex = PackTextures ();
				mat = Instantiate<Material> (mat);
				mat.mainTexture = tex;
			}

			GameObject obj = new GameObject ("Cube", typeof(MeshRenderer));
			MeshFilter mf = obj.AddComponent<MeshFilter> ();
			Mesh mesh = GetMesh ();
			mf.mesh = mesh;
#if UNITY_2018_3_OR_NEWER
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset (obj, path);
#else
            GameObject prefab = PrefabUtility.CreatePrefab (path, obj);
#endif
            // Store packed texture and mesh inside the prefab
            if (cubeShadingStyle != CubeShadingStyle.Color && mat.mainTexture != null) {
				AssetDatabase.AddObjectToAsset (mat.mainTexture, prefab);
				AssetDatabase.AddObjectToAsset (mat, prefab);
			}
			AssetDatabase.AddObjectToAsset (mesh, prefab);
			prefab.GetComponent<MeshFilter> ().sharedMesh = mesh;
			prefab.GetComponent<MeshRenderer> ().sharedMaterial = mat;
			MeshCollider mc = prefab.AddComponent<MeshCollider> ();
			mc.sharedMesh = mesh;
			mc.convex = true;
			AssetDatabase.SaveAssets ();
			DestroyImmediate (obj);

			return prefab;
		}

		VoxelDefinition GenerateDoorVoxel () {

			GameObject doorPrefab = GenerateCubePrefab ();
			if (doorPrefab == null) {
				return null;
			}
			Door door = doorPrefab.AddComponent<Door> ();
			if (offsetOption == CubeVertexOffsetOption.DoorOpensToTheLeft) {
				door.customTag = "left";
			}
			VoxelDefinition doorVoxel = ScriptableObject.CreateInstance<VoxelDefinition> ();
			doorVoxel.renderType = RenderType.Custom;
            doorVoxel.model = doorPrefab;
			doorVoxel.name = "Voxel" + doorPrefab.name;
			doorVoxel.icon = icon;

			if (offsetOption == CubeVertexOffsetOption.DoorOpensToTheRight) {
				doorVoxel.offset = new Vector3 (scale.x * 0.5f - scale.z * 0.5f, -0.5f, 0);
			} else {
				doorVoxel.offset = new Vector3 (scale.x * -0.5f + scale.z * 0.5f, -0.5f, 0);
			}

			string path = AssetDatabase.GetAssetPath (doorPrefab);
			if (path != null) {
				path = System.IO.Path.GetDirectoryName (path);
			}
			AssetDatabase.CreateAsset (doorVoxel, path + "/" + doorPrefab.name + ".asset");
			AssetDatabase.SaveAssets ();
			AssetDatabase.Refresh ();
			return doorVoxel;
		}


		string GetPathForNewCube () {
			string path = null;

			// Check any texture
			for (int k = 0; k < sides.Length; k++) {
				if (sides [k].texture != null) {
					path = AssetDatabase.GetAssetPath (sides [k].texture);
					if (path != null) {
						path = System.IO.Path.GetDirectoryName (path);
						break;
					}
				}
			}
			if (path == null) {
				if (VoxelPlayEnvironment.instance != null) {
					path = AssetDatabase.GetAssetPath (VoxelPlayEnvironment.instance.world);
					path = System.IO.Path.GetDirectoryName (path) + "/Models";
				} else {
					path = "Assets/ImportedModels";
					System.IO.Directory.CreateDirectory (path);
				}
			}

			return path;
		}

		Texture2D PackTextures () {
			List<Texture2D> textures = new List<Texture2D> ();
			for (int k = 0; k < sides.Length; k++) {
				if (sides [k].texture == null) {
					textures.Add (Texture2D.whiteTexture);
				} else {
					textures.Add (sides [k].texture);
				}
			}
			Texture2D tex = new Texture2D (textureAtlasSize, textureAtlasSize);
			uvRects = tex.PackTextures (textures.ToArray (), 4, textureAtlasSize);
			if (uvRects == null)
				return null;
			for (int k = 0; k < uvRects.Length; k++) {
				sides [k].uvMin = uvRects [k].min;
				sides [k].uvMax = uvRects [k].max;
			}
			return tex;
		}


		Mesh GetMesh () {
			tempVertices.Clear ();
			tempNormals.Clear ();
			tempUVs.Clear ();
			tempColors.Clear ();
			tempIndicesPos = 0;

			Mesh mesh = new Mesh ();
			AddFace (faceVerticesTop, normalsUp, sides [0].color, sides [0].uvMin, sides [0].uvMax);
			AddFace (faceVerticesBottom, normalsDown, sides [1].color, sides [1].uvMin, sides [1].uvMax);
			AddFace (faceVerticesForward, normalsForward, sides [2].color, sides [2].uvMin, sides [2].uvMax);
			AddFace (faceVerticesBack, normalsBack, sides [3].color, sides [3].uvMin, sides [3].uvMax);
			AddFace (faceVerticesLeft, normalsLeft, sides [4].color, sides [4].uvMin, sides [4].uvMax);
			AddFace (faceVerticesRight, normalsRight, sides [5].color, sides [5].uvMin, sides [5].uvMax);

			mesh.SetVertices (tempVertices);
			if (cubeShadingStyle != CubeShadingStyle.Color) {
				mesh.SetUVs (0, tempUVs);
			}
			mesh.SetNormals (tempNormals);
			mesh.SetColors (tempColors);
			mesh.triangles = tempIndices;

			return mesh;
		}



		void AddFace (Vector3[] faceVertices, Vector3[] normals, Color32 color, Vector2 uvMin, Vector2 uvMax) {
			int index = tempVertices.Count;
			for (int i = 0; i < 4; i++) {
				Vector3 v = faceVertices [i];
				v.x = v.x * scale.x + offset.x;
				v.y = v.y * scale.y + offset.y;
				v.z = v.z * scale.z + offset.z;
				tempVertices.Add (v);
				tempNormals.Add (normals [i]);
			}

			tempIndices [tempIndicesPos++] = index;
			tempIndices [tempIndicesPos++] = index + 1;
			tempIndices [tempIndicesPos++] = index + 3;
			tempIndices [tempIndicesPos++] = index + 3;
			tempIndices [tempIndicesPos++] = index + 2;
			tempIndices [tempIndicesPos++] = index + 0;

			Vector2 uv = uvMin;
			tempUVs.Add (uv);
			uv.y = uvMax.y;
			tempUVs.Add (uv);
			uv.x = uvMax.x;
			uv.y = uvMin.y;
			tempUVs.Add (uv);
			uv.y = uvMax.y;
			tempUVs.Add (uv);

			tempColors.Add (color);
			tempColors.Add (color);
			tempColors.Add (color);
			tempColors.Add (color);
		}

	}

}