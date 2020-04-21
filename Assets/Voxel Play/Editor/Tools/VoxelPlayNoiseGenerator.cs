using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace VoxelPlay {
				
	public class VoxelPlayNoiseGenerator : EditorWindow {

		enum NoiseType {
			Perlin = 10,
			Cellular = 20
		}

		[SerializeField]
		int size = 256;

		[SerializeField, Range (0, 1f)]
		float offset = 0;

		[SerializeField]
		NoiseType noiseType = NoiseType.Perlin;

		[SerializeField]
		int cellCount = 36;

		[SerializeField, Range(1, 255)] 
		int cellTypes = 8;


		[SerializeField, Range (1, 12)]
		int octaves = 6;

		[SerializeField]
		float lacunarity = 2f;

		[SerializeField, Range (0, 1f)]
		float persistance = 0.5f;

		[SerializeField]
		bool normalize = true;

		[SerializeField]
		bool normalizeFixed = false;

		[SerializeField]
		float rangeMin = 0, rangeMax = 1;

		SerializedObject serializedObject;
		SerializedProperty sizeProp, noiseTypeProp, offsetProp, normalizeProp, normalizeFixedProp, rangeMinProp, rangeMaxProp;
		SerializedProperty octavesProp, lacunarityProp, persistanceProp;
		SerializedProperty cellCountProp, cellTypesProp;

		Texture2D noiseTexture, textureAsset;
		Material previewTextureMat;
		GUIStyle titleLabelStyle;
		Color titleColor;
		float noiseMinValue, noiseMaxValue;
		float[] values;
		Color[] colors;
		bool requestRebuild;
		Vector3[] randoms;
		int[] offsetsX = new int[] { -1, 0, 1, -1, 0, 1, -1, 0, 1 };
		int[] offsetsY = new int[] { -1, -1, -1, 0, 0, 0, 1, 1, 1 };


		[MenuItem ("Assets/Create/Voxel Play/Noise Generator...", false, 1001)]
		public static void ShowWindow () {
			VoxelPlayNoiseGenerator window = GetWindow<VoxelPlayNoiseGenerator> ("Noise Generator", true);
			window.minSize = new Vector2 (400, 400);
			window.Show ();
		}

		void OnEnable () {
			titleColor = EditorGUIUtility.isProSkin ? new Color (0.52f, 0.66f, 0.9f) : new Color (0.12f, 0.16f, 0.4f);

			ScriptableObject target = this;
			serializedObject = new SerializedObject (target);
			sizeProp = serializedObject.FindProperty ("size");
			noiseTypeProp = serializedObject.FindProperty ("noiseType");
			cellCountProp = serializedObject.FindProperty ("cellCount");
			cellTypesProp = serializedObject.FindProperty ("cellTypes");
			octavesProp = serializedObject.FindProperty ("octaves");
			offsetProp = serializedObject.FindProperty ("offset");
			lacunarityProp = serializedObject.FindProperty ("lacunarity");
			persistanceProp = serializedObject.FindProperty ("persistance");
			normalizeProp = serializedObject.FindProperty ("normalize");
			normalizeFixedProp = serializedObject.FindProperty ("normalizeFixed");
			rangeMinProp = serializedObject.FindProperty ("rangeMin");
			rangeMaxProp = serializedObject.FindProperty ("rangeMax");
			RebuildTexture ();
		}

		void OnGUI () {

			EditorGUIUtility.labelWidth = 120;

			serializedObject.Update ();
			if (titleLabelStyle == null) {
				titleLabelStyle = new GUIStyle (EditorStyles.label);
			}
			titleLabelStyle.normal.textColor = titleColor;
			titleLabelStyle.fontStyle = FontStyle.Bold;

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.HelpBox ("Preview and create noise textures.", MessageType.Info);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.Separator ();

			// Draw noise texture
			if (noiseTexture != null) {
				if (previewTextureMat == null) {
					previewTextureMat = Resources.Load<Material> ("VoxelPlay/PreviewTexture");
				}
				EditorGUILayout.LabelField (new GUIContent ("Noise Preview"), titleLabelStyle);

				Rect space = EditorGUILayout.BeginVertical ();
				GUILayout.FlexibleSpace ();
				EditorGUILayout.EndVertical ();
				space.width -= 5;
				space.x = 5;
				space.width = Mathf.Min (space.height, space.width);
				EditorGUI.DrawPreviewTexture (space, noiseTexture, previewTextureMat, ScaleMode.ScaleToFit);
				EditorGUILayout.BeginHorizontal ();
				string stats = "Texture size = " + noiseTexture.width + "   Min value = " + noiseMinValue + "   Max value = " + noiseMaxValue;
				EditorGUILayout.LabelField ("Noise Stats", stats);
				EditorGUILayout.EndHorizontal ();
			}

			EditorGUI.BeginChangeCheck ();
			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Texture Asset", GUILayout.Width (120));
			textureAsset = (Texture2D)EditorGUILayout.ObjectField (textureAsset, typeof(Texture2D), false);
			if (textureAsset != null) {
				if (GUILayout.Button ("Release", GUILayout.Width (100))) {
					textureAsset = null;
					requestRebuild = true;
				}
			}
			EditorGUILayout.EndHorizontal ();
			if (EditorGUI.EndChangeCheck ()) {
				LoadTexture ();
			}

			EditorGUILayout.Separator ();

			if (textureAsset == null) {
				EditorGUILayout.LabelField (new GUIContent ("Noise Generator"), titleLabelStyle);
				EditorGUI.BeginChangeCheck ();
				EditorGUILayout.PropertyField (sizeProp); 
				if (sizeProp.intValue < 4)
					sizeProp.intValue = 4;
				EditorGUILayout.PropertyField (noiseTypeProp); 
				if (noiseTypeProp.intValue == (int)NoiseType.Perlin) {
					EditorGUILayout.PropertyField (octavesProp); 
					EditorGUILayout.PropertyField (lacunarityProp); 
					EditorGUILayout.PropertyField (persistanceProp); 
				} else {
					EditorGUILayout.PropertyField (cellCountProp);
					if (cellCountProp.intValue < 1) {
						cellCountProp.intValue = 1;
					}
					EditorGUILayout.PropertyField (cellTypesProp);
					if (cellTypesProp.intValue < 1) {
						cellTypesProp.intValue = 1;
					}
				}
				EditorGUILayout.PropertyField (offsetProp); 
				EditorGUILayout.PropertyField (normalizeProp, new GUIContent ("Normalize", "Rescale noise values to fit a defined range.")); 
				if (normalizeProp.boolValue) {
					EditorGUILayout.PropertyField (normalizeFixedProp, new GUIContent ("   Fixed Range", "Enable to specify a custom range for noise values. If disabled, values will be normalized in the 0-1 range.")); 
					if (normalizeFixedProp.boolValue) {
						EditorGUILayout.PropertyField (rangeMinProp, new GUIContent ("      Min Value", "Minimum value for noise.")); 
						EditorGUILayout.PropertyField (rangeMaxProp, new GUIContent ("      Max Value", "Maximum value for noise.")); 
					}
				}
				if (EditorGUI.EndChangeCheck ()) {
					requestRebuild = true;
				}
			

				EditorGUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Refresh", GUILayout.Width (80))) {
					requestRebuild = true;
				}
				GUI.enabled = noiseTexture != null;
				if (GUILayout.Button ("Create Texture Asset", GUILayout.Width (150))) {
					CreateTextureAsset ();
					EditorGUIUtility.ExitGUI ();
					return;
				}
				GUI.enabled = true;
				EditorGUILayout.EndHorizontal ();
			}
			serializedObject.ApplyModifiedProperties ();


			EditorGUILayout.Separator ();
			if (requestRebuild && textureAsset == null) {
				requestRebuild = false;
				RebuildTexture ();
				EditorGUIUtility.ExitGUI ();
			}
		}

		void RebuildTexture () {

			if (values == null || values.Length != size * size) {
				values = new float[size * size];
			}

			float maxValue = float.MinValue;
			float minValue = float.MaxValue;

			switch (noiseType) {
			case NoiseType.Perlin:
				GeneratePerlinNoise (ref minValue, ref maxValue);
				break;
			case NoiseType.Cellular:
				GenerateCellularNoise (ref minValue, ref maxValue);
				break;
			}

			// normalize values
			float range = maxValue - minValue;
			if (range > 0 && normalize) {
				float fixedMin = 0;
				float fixedRange = 1f;
				if (normalizeFixed) {
					fixedMin = rangeMin;
					fixedRange = rangeMax - rangeMin;
				}
				for (int index = 0; index < values.Length; index++) {
					float v = (values [index] - minValue) / range;
					v = fixedMin + v * fixedRange;
					values [index] = v;
				}
			} 

			FillTexture ();
		}

		void GeneratePerlinNoise (ref float minValue, ref float maxValue) {
			Vector2[] pows = new Vector2[octaves];
			for (int k = 0; k < pows.Length; k++) {
				pows [k].x = Mathf.Max (1f, Mathf.Pow (lacunarity, (k + 1)));
				pows [k].y = Mathf.Pow (persistance, k);
			}
			float offsetValue = offset * size;
			for (int index = 0, y = 0; y < size; y++) {
				float yf = (float)y / size;
				for (int x = 0; x < size; x++) {
					float xf = (float)x / size; 
					float v = 1f;
					for (int o = 0; o < octaves; o++) {
						int frequency = (int)pows [o].x;
						float amplitude = pows [o].y;
						float xx = xf * frequency + offsetValue;
						float yy = yf * frequency + offsetValue;
						float ov = NoiseTools.GetTilablePerlinValue (xx, yy, frequency, frequency) + 0.5f;
						v += amplitude * ov; 
					}
					values [index++] = v;
					if (v < minValue) {
						minValue = v;
					}
					if (v > maxValue) {
						maxValue = v;
					}
				}
			}
		}

		void GenerateCellularNoise (ref float minValue, ref float maxValue) {
			int cellMult = (int)Mathf.Sqrt (cellCount);
			int randomLen = cellMult * cellMult;
			if (randoms == null || randoms.Length < randomLen) {
				randoms = new Vector3[randomLen];
			}
			Random.InitState ((int)(offset * 1000f));
			for (int k = 0; k < randoms.Length; k++) {
				randoms [k].x = Random.value;
				randoms [k].y = Random.value;
				randoms [k].z = Random.Range (0, cellTypes);
			}

			float v = 0;
			Vector2 st, ft;
			for (int index = 0, r = 0; r < size; r++) {
				st.y = (float)r / size;
				st.y *= cellMult;
				int fy = (int)st.y;
				ft.y = st.y - fy;
				for (int c = 0; c < size; c++) {
					st.x = (float)c / size;
					st.x *= cellMult;
					int fx = (int)st.x;
					ft.x = st.x - fx;
					float minDist = float.MaxValue; 

					fy += cellMult;
					fx += cellMult;
					for (int w = 0; w < 9; w++) {
						int x = offsetsX [w];
						int y = offsetsY [w];

						int ox = (fx + x) % cellMult;
						int oy = (fy + y) % cellMult;

						int idx = oy * cellMult + ox;
						float dx = x + randoms [idx].x - ft.x;
						float dy = y + randoms [idx].y - ft.y;

						float dist = dx * dx + dy * dy;

						if (dist < minDist) {
							minDist = dist;
							v = randoms [idx].z;
						}
					}
					values [index++] = v;
					if (v < minValue) {
						minValue = v;
					}
					if (v > maxValue) {
						maxValue = v;
					}
				}
			}
		}


		void FillTexture () {

			if (noiseTexture == null || noiseTexture.width != size) {
				noiseTexture = new Texture2D (size, size, TextureFormat.ARGB32, false);
			}

			// update texture
			if (colors == null || colors.Length != values.Length) {
				colors = new Color[size * size];
			}
			Color c = new Color (1, 1, 1);
			noiseMaxValue = float.MinValue;
			noiseMinValue = float.MaxValue;

			for (int index = 0; index < values.Length; index++) {
				float v = values [index];
				if (v < noiseMinValue) {
					noiseMinValue = v;
				}
				if (v > noiseMaxValue) {
					noiseMaxValue = v;
				}
				c.r = c.g = c.b = v;
				colors [index] = c;
			}
			noiseTexture.SetPixels (colors);
			noiseTexture.Apply ();
		}


		void LoadTexture () {
			if (textureAsset == null)
				return;

			size = textureAsset.width;
			Color[] colors = textureAsset.GetPixels ();
			values = new float[colors.Length];
			for (int k = 0; k < colors.Length; k++) {
				values [k] = colors [k].r;
			}
			FillTexture ();
		}

		void CreateTextureAsset () {

			byte[] bytes = noiseTexture.EncodeToPNG ();
			string filename = "NoiseTexture" + System.DateTime.Now.Ticks;
			string path = Application.dataPath;
			File.WriteAllBytes (path + "/" + filename + ".png", bytes);
			AssetDatabase.Refresh ();
			Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D> ("Assets/" + filename + ".png");
			if (tex != null) {
				TextureImporter importer = AssetImporter.GetAtPath ("Assets/" + filename + ".png") as TextureImporter;
				if (importer != null) {
					importer.isReadable = true;
					importer.SaveAndReimport ();
					Selection.activeObject = tex;
					EditorGUIUtility.PingObject (tex);
				}
			}
		}






	}





}