using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace VoxelPlay {
				
	public class VoxelPlayBiomeExplorer : EditorWindow {

		public static bool requestRefresh;

		WorldDefinition world;
		Texture2D terrainTex, moistureTex, biomeTex;
		VoxelPlayEnvironment env;
		VoxelPlayTerrainGenerator tg;
		Material previewTextureMat;
		float minX = -1000;
		float maxX = 1000;
		float minZ = -1000;
		float maxZ = 1000;
		float sliceZ;
		int mapResolution = 512;
		int gridStep = 256;
		GUIStyle titleLabelStyle;
		Color titleColor;
		Color waterColor;

		float calcMinAltitude, calcMaxAltitude, calcMinMoisture, calcMaxMoisture;
		int minY, maxY;
		float proposedMinX = -1000, proposedMaxX = 1000, proposedMinZ = -1000, proposedMaxZ = 1000, proposedSliceZ;
		float inputAltitude, inputMoisture;
		string biomeTestResult;
		Vector2 scrollPosition;

		[MenuItem ("Assets/Create/Voxel Play/Biome Explorer...", false, 1000)]
		public static void ShowWindow () {
			VoxelPlayBiomeExplorer window = GetWindow<VoxelPlayBiomeExplorer> ("Biome Explorer", true);
			window.minSize = new Vector2 (400, 400);
			window.Show ();
		}

		void OnEnable () {
			requestRefresh = true;
			titleColor = EditorGUIUtility.isProSkin ? new Color (0.52f, 0.66f, 0.9f) : new Color (0.12f, 0.16f, 0.4f);
			waterColor = new Color (0, 0.1f, 1f, 0.8f);
		}

		void OnGUI () {

			scrollPosition = EditorGUILayout.BeginScrollView (scrollPosition);

			if (env == null) {
				env = VoxelPlayEnvironment.instance;
				if (env == null) {
					world = null;
				}
				EditorGUILayout.HelpBox ("Biome Explorer cannot find a Voxel Play Environment instance in the current scene.", MessageType.Error);
				GUIUtility.ExitGUI ();
			} else {
				world = env.world;
			}

			if (world == null) {
				EditorGUILayout.HelpBox ("Assign a World Definition to the Voxel Play Environment instance.", MessageType.Warning);
				GUIUtility.ExitGUI ();
			}

			if (terrainTex == null || moistureTex == null) {
				RefreshTextures ();
				GUIUtility.ExitGUI ();
			}

			GUIStyle labelStyle = new GUIStyle (GUI.skin.label);
			if (titleLabelStyle == null) {
				titleLabelStyle = new GUIStyle (EditorStyles.label);
			}
			titleLabelStyle.normal.textColor = titleColor;
			titleLabelStyle.fontStyle = FontStyle.Bold;

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.HelpBox ("Preview terrain generation and biome distribution based on current settings.", MessageType.Info);
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.Separator ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Min X", GUILayout.Width (100));
			proposedMinX = EditorGUILayout.FloatField (proposedMinX, GUILayout.MaxWidth (120));
			EditorGUILayout.LabelField ("Max X", GUILayout.Width (100));
			proposedMaxX = EditorGUILayout.FloatField (proposedMaxX, GUILayout.MaxWidth (120));
			if (GUILayout.Button ("<<", GUILayout.Width (40))) {
				float shift = (maxX - minX) * 0.5f;
				proposedMinX -= shift;
				proposedMaxX -= shift;
				requestRefresh = true;
			}
			if (GUILayout.Button ("<", GUILayout.Width (40))) {
				float shift = (maxX - minX) * 0.1f;
				proposedMinX -= shift;
				proposedMaxX -= shift;
				requestRefresh = true;
			}
			if (GUILayout.Button (">", GUILayout.Width (40))) {
				float shift = (maxX - minX) * 0.1f;
				proposedMinX += shift;
				proposedMaxX += shift;
				requestRefresh = true;
			}
			if (GUILayout.Button (">>", GUILayout.Width (40))) {
				float shift = (maxX - minX) * 0.5f;
				proposedMinX += shift;
				proposedMaxX += shift;
				requestRefresh = true;
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Min Z", GUILayout.Width (100));
			proposedMinZ = EditorGUILayout.FloatField (proposedMinZ, GUILayout.MaxWidth (120));
			EditorGUILayout.LabelField ("Max Z", GUILayout.Width (100));
			proposedMaxZ = EditorGUILayout.FloatField (proposedMaxZ, GUILayout.MaxWidth (120));
			if (GUILayout.Button ("<<", GUILayout.Width (40))) {
				float shift = (maxZ - minZ) * 0.5f;
				proposedMinZ -= shift;
				proposedMaxZ -= shift;
				proposedSliceZ = (proposedMinZ + proposedMaxZ) * 0.5f;
				requestRefresh = true;
			}
			if (GUILayout.Button ("<", GUILayout.Width (40))) {
				float shift = (maxZ - minZ) * 0.1f;
				proposedMinZ -= shift;
				proposedMaxZ -= shift;
				proposedSliceZ = (proposedMinZ + proposedMaxZ) * 0.5f;
				requestRefresh = true;
			}
			if (GUILayout.Button (">", GUILayout.Width (40))) {
				float shift = (maxZ - minZ) * 0.1f;
				proposedMinZ += shift;
				proposedMaxZ += shift;
				proposedSliceZ = (proposedMinZ + proposedMaxZ) * 0.5f;
				requestRefresh = true;
			}
			if (GUILayout.Button (">>", GUILayout.Width (40))) {
				float shift = (maxZ - minZ) * 0.5f;
				proposedMinZ += shift;
				proposedMaxZ += shift;
				proposedSliceZ = (proposedMinZ + proposedMaxZ) * 0.5f;
				requestRefresh = true;
			}

			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			EditorGUILayout.LabelField ("Slice Z", GUILayout.Width (100));
			proposedSliceZ = EditorGUILayout.FloatField (proposedSliceZ, GUILayout.MaxWidth (120));
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.BeginHorizontal ();
			if (GUILayout.Button (new GUIContent ("Refresh Window", "Refresh textures to reflect new filters."), GUILayout.Width (140))) {
				requestRefresh = true;
			}
			if (GUILayout.Button (new GUIContent ("Align With Camera", "Moves the preview area so camera stays at the center."), GUILayout.Width (140))) {
				float dx = maxX - minX;
				float dz = maxZ - minZ;
				Camera cam = env.currentCamera;
				if (cam != null) {
					Vector3 camPos = cam.transform.position;
					proposedMinX = camPos.x - dx * 0.5f;
					proposedMaxX = camPos.x + dx * 0.5f;
					proposedMinZ = camPos.z - dz * 0.5f;
					proposedMaxZ = camPos.z + dz * 0.5f;
					requestRefresh = true;
				}
			}

			if (GUILayout.Button (new GUIContent ("-> World Definition", "Show World Definition in the inspector."), GUILayout.Width (140))) {
				Selection.activeObject = world;
			}
			if (GUILayout.Button (new GUIContent ("-> Terrain Generator", "Show Terrain Generator in the inspector."), GUILayout.Width (140))) {
				Selection.activeObject = tg;
			}
			if (GUILayout.Button (new GUIContent ("-> Environment", "Show Voxel Play Environment in the inspector."), GUILayout.Width (140))) {
				Selection.activeGameObject = env.gameObject;
			}
			if (GUILayout.Button (new GUIContent ("Reload Config", "Resets heightmaps and biome cache and initializes terrain generator."), GUILayout.Width (140))) {
				env.NotifyTerrainGeneratorConfigurationChanged ();
				requestRefresh = true;
				GUIUtility.ExitGUI ();
			}
			EditorGUILayout.EndHorizontal ();

			EditorGUILayout.Separator ();
			Rect space;

			if (previewTextureMat == null) {
				previewTextureMat = Resources.Load<Material> ("VoxelPlay/PreviewTexture");
			}

			// Draw heightmap distribution
			if (terrainTex != null) {
				EditorGUILayout.LabelField (new GUIContent ("Height Map Preview"), titleLabelStyle);
				space = EditorGUILayout.BeginVertical ();
				space.width -= 20;
				GUILayout.Space (terrainTex.height);
				EditorGUILayout.EndVertical ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Space (15);
				space.position += new Vector2 (15, 0);
				EditorGUI.DrawPreviewTexture (space, terrainTex, previewTextureMat);
				GUILayout.Space (5);
				EditorGUILayout.EndHorizontal ();

				// Draw 0-1 range
				space.position -= new Vector2 (15, 0);
				EditorGUI.LabelField (space, "1");
				space.position += new Vector2 (0, space.height - 10f);
				EditorGUI.LabelField (space, "0");

				// Draw x-axis labels
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Space (15);
				EditorGUILayout.LabelField ("Min X = " + minX);
				labelStyle.alignment = TextAnchor.MiddleCenter;
				EditorGUILayout.LabelField ("Slize Z = " + sliceZ + " / Min Y = " + minY + " (" + calcMinAltitude.ToString ("F3") + ") / Max Y = " + maxY + " (" + calcMaxAltitude.ToString ("F3") + ")", labelStyle);
				labelStyle.alignment = TextAnchor.MiddleRight;
				EditorGUILayout.LabelField ("Max X = " + maxX, labelStyle);
				EditorGUILayout.EndHorizontal ();
			}

			EditorGUILayout.Separator ();
			EditorGUILayout.Separator ();


			// Draw moisture distribution
			if (terrainTex != null) {
				EditorGUILayout.LabelField (new GUIContent ("Moisture Preview"), titleLabelStyle);
				space = EditorGUILayout.BeginVertical ();
				space.width -= 20; 
				GUILayout.Space (moistureTex.height);
				EditorGUILayout.EndVertical ();

				EditorGUILayout.BeginHorizontal ();
				GUILayout.Space (15);
				space.position += new Vector2 (15, 0);
				EditorGUI.DrawPreviewTexture (space, moistureTex, previewTextureMat);
				GUILayout.Space (5);
				EditorGUILayout.EndHorizontal ();

				// Draw 0-1 range
				space.position -= new Vector2 (15, 0);
				EditorGUI.LabelField (space, "1");
				space.position += new Vector2 (0, space.height - 10f);
				EditorGUI.LabelField (space, "0");

				// Draw x-axis labels
				EditorGUILayout.BeginHorizontal ();
				GUILayout.Space (15);
				EditorGUILayout.LabelField ("Min X = " + minX);
				labelStyle.alignment = TextAnchor.MiddleCenter;
				EditorGUILayout.LabelField ("Slize Z = " + sliceZ + " / Min Y = " + calcMinMoisture.ToString ("F3") + " / Max Y = " + calcMaxMoisture.ToString ("F3"), labelStyle);
				labelStyle.alignment = TextAnchor.MiddleRight;
				EditorGUILayout.LabelField ("Max X = " + maxX, labelStyle);
				EditorGUILayout.EndHorizontal ();
			}

			EditorGUILayout.Separator ();
			EditorGUILayout.Separator ();

			if (world.biomes != null && biomeTex != null) {

				// Draw heightmap texture
				EditorGUILayout.LabelField (new GUIContent ("Biome Map Preview"), titleLabelStyle);

				EditorGUILayout.BeginHorizontal ();

				// Biome legend
				EditorGUILayout.BeginVertical (GUILayout.MaxWidth (180));
				EditorGUILayout.Separator ();
				EditorGUILayout.BeginHorizontal ();
				if (GUILayout.Button ("Hide All", GUILayout.Width (80))) {
					ToggleBiomes (false);
					requestRefresh = true;
				}
				if (GUILayout.Button ("Show All", GUILayout.Width (80))) {
					ToggleBiomes (true);
					requestRefresh = true;
				}
				if (GUILayout.Button ("Default Colors", GUILayout.Width (120))) {
					env.SetBiomeDefaultColors (true);
					requestRefresh = true;
				}
				EditorGUILayout.EndHorizontal ();
				EditorGUI.BeginChangeCheck ();
				for (int k = 0; k < world.biomes.Length; k++) {
					BiomeDefinition biome = world.biomes [k];
					if (biome == null)
						continue;
					float perc = (100f * biome.biomeMapOccurrences) / (biomeTex.width * biomeTex.height);
					DrawLegend (biome.biomeMapColor, biome.name + " (" + perc.ToString ("F2") + "%)", biome);
				}
				DrawLegend (waterColor, "Water", null);

				if (EditorGUI.EndChangeCheck ()) {
					requestRefresh = true;
				}
				EditorGUILayout.Separator ();
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Grid Size", GUILayout.Width (100));
				gridStep = EditorGUILayout.IntField (gridStep, GUILayout.Width (60));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Texture Size", GUILayout.Width (100));
				mapResolution = EditorGUILayout.IntField (mapResolution, GUILayout.Width (60));
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.Separator ();
				EditorGUILayout.Separator ();
				// Tester
				EditorGUILayout.LabelField (new GUIContent ("Biome Tester"), titleLabelStyle);
				EditorGUI.BeginChangeCheck ();
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Altitude?", GUILayout.Width (100));
				inputAltitude = EditorGUILayout.Slider (inputAltitude, 0, 1, GUILayout.Width (130));
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Moisture?", GUILayout.Width (100));
				inputMoisture = EditorGUILayout.Slider (inputMoisture, 0, 1, GUILayout.Width (130));
				EditorGUILayout.EndHorizontal ();
				if (EditorGUI.EndChangeCheck ()) {
					CalcBiome ();
				}
				EditorGUILayout.LabelField (biomeTestResult);
				EditorGUILayout.EndVertical ();

				// Biome map
				space = EditorGUILayout.BeginVertical ();
				GUILayout.FlexibleSpace ();
				EditorGUILayout.EndVertical ();
				EditorGUI.DrawPreviewTexture (space, biomeTex, previewTextureMat, ScaleMode.ScaleToFit);

				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Separator ();
			}

			EditorGUILayout.EndScrollView ();

			if (requestRefresh) {
				RefreshTextures ();
			}
		}

		void DrawLegend (Color color, string text, BiomeDefinition biome) {

			Rect space = EditorGUILayout.BeginHorizontal ();
			space.position += new Vector2 (5, 0);
			space.width = 16;
			space.height = 16;
			Color prevColor = GUI.color;
			GUI.color = color;
			EditorGUI.DrawPreviewTexture (space, Texture2D.whiteTexture);
			space.position += new Vector2 (20, 0);
			space.width = 20;
			if (biome != null) {
				GUI.color = biome.showInBiomeMap ? prevColor : new Color (prevColor.r, prevColor.g, prevColor.b, 0.4f);
			} else {
				GUI.color = prevColor;
			}
			space.width = 20;
			if (biome != null) {
				biome.showInBiomeMap = EditorGUI.Toggle (space, biome.showInBiomeMap);
				space.width = 35;
				space.position += new Vector2 (20, 0);
				if (GUI.Button (space, "->")) {
					Selection.activeObject = biome;
				}
				space.position += new Vector2 (35, 0);
			}
			space.width = 120;
			EditorGUI.LabelField (space, text);
			GUI.color = prevColor;
			EditorGUILayout.BeginVertical ();
			GUILayout.Space (20);
			EditorGUILayout.EndVertical ();
			EditorGUILayout.EndHorizontal ();
		}

		void RefreshTextures () {

			requestRefresh = false;
			proposedMaxX = Mathf.Max (proposedMinX + 1, proposedMaxX);
			proposedMaxZ = Mathf.Max (proposedMinZ + 1, proposedMaxZ);
			proposedSliceZ = Mathf.Clamp (proposedSliceZ, proposedMinZ, proposedMaxZ);

			minX = proposedMinX;
			maxX = proposedMaxX;
			minZ = proposedMinZ;
			maxZ = proposedMaxZ;
			sliceZ = proposedSliceZ;

			if (tg == null || tg != world.terrainGenerator) {
				tg = world.terrainGenerator;
				if (tg == null)
					return;
			}
			if (!tg.isInitialized) {
				tg.Initialize ();
			}

			RefreshTerrainTexture ();
			RefreshMoistureTexture ();
			RefreshBiomeTexture ();

		}

		void RefreshTerrainTexture () {

			if (terrainTex == null) {
				terrainTex = new Texture2D (256, 64, TextureFormat.ARGB32, false);
				terrainTex.alphaIsTransparency = true;
			}

			int width = terrainTex.width;
			int height = terrainTex.height;
			Color[] colors = new Color[width * height];

			calcMinAltitude = calcMaxAltitude = 0;
			if (env == null || tg == null) {
				colors.Fill<Color> (new Color (0.5f, 0, 0, 0.5f));
			} else {
				colors.Fill<Color> (Misc.colorTransparent);
				// horizontal grid lines
				Color gridColor = new Color (64, 64, 64, 0.2f);
				for (int j = 0; j <= 4; j++) {
					int y = (int)((height - 1) * j / 4f);
					for (int k = 0; k < width; k++) {
						colors [y * width + k] = gridColor;
					}
				}
				calcMinAltitude = float.MaxValue;
				calcMaxAltitude = float.MinValue;
				// draw height vertical bars
				int waterLevelTex = (int)Mathf.Clamp (height * tg.waterLevel / tg.maxHeight, 0, height - 1);
				for (int k = 0; k < width; k++) {
					float altitude, moisture;
					float x = (maxX - minX) * (float)k / width + minX;
					tg.GetHeightAndMoisture (x, sliceZ, out altitude, out moisture);
					if (altitude > calcMaxAltitude) {
						calcMaxAltitude = altitude;
					}
					if (altitude < calcMinAltitude) {
						calcMinAltitude = altitude;
					}
					int y = (int)Mathf.Clamp (height * altitude, 0, height - 1);
					Color color = new Color (altitude, moisture, 0, 0.8f);
					// draw water level
					for (int j = 0; j < waterLevelTex; j++) {
						colors [j * width + k] = waterColor;
					}
					// draw terrain
					for (int j = waterLevelTex; j < y; j++) {
						colors [j * width + k] = color;
					}
				}
			}

			terrainTex.SetPixels (colors);
			terrainTex.Apply ();

			minY = Mathf.FloorToInt (calcMinAltitude * tg.maxHeight);
			maxY = Mathf.FloorToInt (calcMaxAltitude * tg.maxHeight);
		}


		void RefreshMoistureTexture () {

			if (moistureTex == null) {
				moistureTex = new Texture2D (256, 64, TextureFormat.ARGB32, false);
				moistureTex.alphaIsTransparency = true;
			}

			int width = moistureTex.width;
			int height = moistureTex.height;
			Color[] colors = new Color[width * height];

			calcMinMoisture = calcMaxMoisture = 0;
			if (env == null || tg == null) {
				colors.Fill<Color> (new Color (0.5f, 0, 0, 0.5f));
			} else {
				colors.Fill<Color> (Misc.colorTransparent);
				// horizontal grid lines
				Color gridColor = new Color (64, 64, 64, 0.2f);
				for (int j = 0; j <= 4; j++) {
					int y = (int)((height - 1) * j / 4f);
					for (int k = 0; k < width; k++) {
						colors [y * width + k] = gridColor;
					}
				}
				calcMinMoisture = float.MaxValue;
				calcMaxMoisture = float.MinValue;

				// draw height vertical bars
				for (int k = 0; k < width; k++) {
					float altitude, moisture;
					float x = (maxX - minX) * (float)k / width + minX;
					tg.GetHeightAndMoisture (x, sliceZ, out altitude, out moisture);
					if (altitude > calcMaxMoisture) {
						calcMaxMoisture = moisture;
					}
					if (altitude < calcMinMoisture) {
						calcMinMoisture = moisture;
					}
					int y = (int)Mathf.Clamp (height * moisture, 0, height - 1);
					Color color = new Color (moisture * 0.2f, moisture, moisture * 0.2f, 0.8f);
					for (int j = 0; j < y; j++) {
						colors [j * width + k] = color;
					}
				}
			}

			moistureTex.SetPixels (colors);
			moistureTex.Apply ();
		}

		void RefreshBiomeTexture () {

			if (biomeTex == null || biomeTex.width != mapResolution) {
				biomeTex = new Texture2D (mapResolution, mapResolution, TextureFormat.ARGB32, false);
			}

			int width = biomeTex.width;
			int height = biomeTex.height;
			Color[] colors = new Color[width * height];

			if (env == null || tg == null) {
				colors.Fill<Color> (new Color (0, 0.5f, 0, 0.5f));
			} else {
				env.SetBiomeDefaultColors (false);
				colors.Fill<Color> (Misc.colorTransparent);

				// reset biome stats
				for (int k = 0; k < world.biomes.Length; k++) {
					if (world.biomes [k] != null) {
						world.biomes [k].biomeMapOccurrences = 0;
					}
				}
				// draw biome colors
				for (int j = 0; j < height; j++) {
					float z = (maxZ - minZ) * (float)j / height + minZ;
					int jj = j * width;
					for (int k = 0; k < width; k++) {
						float x = (maxX - minX) * (float)k / width + minX;
						HeightMapInfo info = env.GetTerrainInfo (x, z);
						if (info.groundLevel <= tg.waterLevel) {
							colors [jj + k] = waterColor;
						} else {
							BiomeDefinition biome = info.biome;
							if (biome == null)
								continue;
							biome.biomeMapOccurrences++;
							if (biome.showInBiomeMap) {
								colors [jj + k] = biome.biomeMapColor;
							}
						}
					}
				}

				Color gridColor = new Color (64, 64, 64, 0.2f);
				// draw horizontal grid lines
				int gridCount = (int)((maxZ - minZ) / gridStep);
				if (gridCount > 0) {
					for (int j = 0; j <= gridCount; j++) {
						int y = (int)((height - 1f) * j / gridCount);
						for (int k = 0; k < width; k++) {
							colors [y * width + k] = gridColor;
						}
					}
				}
				// draw vertical grid lines
				gridCount = (int)((maxX - minX) / gridStep);
				if (gridCount > 0) {
					for (int j = 0; j <= gridCount; j++) {
						int x = (int)((width - 1f) * j / gridCount);
						for (int k = 0; k < height; k++) {
							colors [k * width + x] = gridColor;
						}
					}
				}
			}

			biomeTex.SetPixels (colors);
			biomeTex.Apply ();
		}

		void ToggleBiomes (bool visible) {
			for (int k = 0; k < world.biomes.Length; k++) {
				BiomeDefinition biome = world.biomes [k];
				if (biome == null)
					continue;
				biome.showInBiomeMap = visible;
			}
		}

		void CalcBiome () {
			BiomeDefinition biome = env.GetBiome (inputAltitude, inputMoisture);
			if (biome == null) {
				biomeTestResult = "No matching biome.";
			} else {
				biomeTestResult = "Matching biome: " + biome.name + ".";
			}
		}

	}

}