using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace VoxelPlay {
				
	[CustomEditor (typeof(UnityTerrainGenerator))]
	public class VoxelPlayTerrainUnityEditor : Editor {

		Color titleColor;
		static GUIStyle titleLabelStyle, boxStyle, sectionHeaderStyle;
		UnityTerrainGenerator tg;
		int terrainTextureSize = 16;
		int treeTextureSize = 64;
		int vegetationTextureSize = 64;
		float frondDensity = 0.5f;
		bool cleanFolders;
        bool expandTerrainTextures, expandTrees, expandVegetation;

		[Range (16, 256)]
		int thumbnailSize = 104;

		[Range (0.1f, 2f)]
		float treeScale = 1f;

		SerializedProperty terrainData, seaLevel, vegetationDensity, waterVoxel;
		Dictionary<Texture2D, VoxelDefinition> textureVoxels;
		string[] textureIndices;
		int[] textureIndicesValues;

		void OnEnable () {
			titleColor = EditorGUIUtility.isProSkin ? new Color (0.52f, 0.66f, 0.9f) : new Color (0.12f, 0.16f, 0.4f);
			tg = (UnityTerrainGenerator)target;
			if (tg == null)
				return;
			terrainData = serializedObject.FindProperty ("terrainData");
			seaLevel = serializedObject.FindProperty ("seaLevel");
			waterVoxel = serializedObject.FindProperty ("waterVoxel");
			vegetationDensity = serializedObject.FindProperty ("vegetationDensity");
			if (tg.terrainData != null) {
				// Check previews
#if UNITY_2018_3_OR_NEWER
                for (int k = 0; k < tg.terrainData.terrainLayers.Length && k < tg.splatSettings.Length; k++) {
#else
				for (int k = 0; k < tg.terrainData.splatPrototypes.Length && k < tg.splatSettings.Length; k++) {
#endif
					if (tg.splatSettings [k].preview == null) {
						tg.ExamineTerrainData ();
						break;
					}
				}
			}
		}


		public override void OnInspectorGUI () {

			if (tg == null)
				return;

			if (tg.splatSettings == null || tg.splatSettings.Length == 0) {
				tg.Initialize ();
			}

			serializedObject.Update ();

			if (titleLabelStyle == null) {
				titleLabelStyle = new GUIStyle (EditorStyles.label);
			}
			titleLabelStyle.normal.textColor = titleColor;
			titleLabelStyle.fontStyle = FontStyle.Bold;
			if (boxStyle == null) {
				boxStyle = new GUIStyle (GUI.skin.box);
			}
            if (sectionHeaderStyle == null) {
                sectionHeaderStyle = new GUIStyle(EditorStyles.foldout);
            }
            sectionHeaderStyle.SetFoldoutColor();

            EditorGUILayout.BeginHorizontal ();
			TerrainData prevTD = (TerrainData)terrainData.objectReferenceValue;
			EditorGUILayout.PropertyField (terrainData);
			TerrainData td = (TerrainData)terrainData.objectReferenceValue;
			if (td != prevTD) {
				tg.ExamineTerrainData ();
			}
			if (GUILayout.Button ("Refresh", GUILayout.Width (60))) {
				tg.ExamineTerrainData ();
			}
			EditorGUILayout.EndHorizontal ();
			EditorGUILayout.PropertyField (seaLevel);
			EditorGUILayout.PropertyField (waterVoxel);
			bool needCreate = false;
			bool needAssign = false;
			bool allIgnored = true;
			if (tg.terrainData != null) {
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Thumbnail Size", GUILayout.Width (EditorGUIUtility.labelWidth));
				thumbnailSize = EditorGUILayout.IntSlider (thumbnailSize, 16, 256);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.Separator ();
                expandTerrainTextures = EditorGUILayout.Foldout(expandTerrainTextures, new GUIContent("Terrain Textures"), true, sectionHeaderStyle);
                if (expandTerrainTextures) {
                    EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField ("Texture Size", GUILayout.Width (EditorGUIUtility.labelWidth));
				terrainTextureSize = EditorGUILayout.IntField (terrainTextureSize);
				EditorGUILayout.EndHorizontal ();
#if UNITY_2018_3_OR_NEWER
                for (int k = 0; k < tg.terrainData.terrainLayers.Length && k < tg.splatSettings.Length; k++) {
#else
					for (int k = 0; k < tg.terrainData.splatPrototypes.Length && k < tg.splatSettings.Length; k++) {
#endif
					EditorGUILayout.LabelField ("Texture " + (k + 1), GUILayout.Width (80));
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (new GUIContent (tg.splatSettings [k].preview), boxStyle, GUILayout.Width (thumbnailSize), GUILayout.Height (thumbnailSize));
					EditorGUILayout.BeginVertical ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Voxel Top", GUILayout.Width (80));
					tg.splatSettings [k].top = (VoxelDefinition)EditorGUILayout.ObjectField (tg.splatSettings [k].top, typeof(VoxelDefinition), false);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Smooth", GUILayout.Width (80));
					tg.splatSettings [k].smoothPower = EditorGUILayout.Slider (tg.splatSettings [k].smoothPower, 0, 1f);
					EditorGUILayout.EndHorizontal ();

#if UNITY_2018_3_OR_NEWER
                    int textureCount = tg.terrainData.terrainLayers.Length;
#else
					int textureCount = tg.terrainData.splatPrototypes.Length;
#endif
                    if (textureIndices == null || textureIndices.Length != textureCount) {
						textureIndices = new string[textureCount];
						textureIndicesValues = new int[textureCount];
						for (int t = 0; t < textureCount; t++) {
							textureIndices [t] = "Texture " + (t + 1);
							textureIndicesValues [t] = (t + 1);
						}
					}

					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Dirt With", GUILayout.Width (80));
					tg.splatSettings [k].dirtWith = EditorGUILayout.IntPopup (tg.splatSettings [k].dirtWith, textureIndices, textureIndicesValues);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Blend Power", GUILayout.Width (80));
					tg.splatSettings [k].blendPower = EditorGUILayout.Slider (tg.splatSettings [k].blendPower, 0, 1f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Voxel Dirt", GUILayout.Width (80));
					tg.splatSettings [k].dirt = (VoxelDefinition)EditorGUILayout.ObjectField (tg.splatSettings [k].dirt, typeof(VoxelDefinition), false);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Action", GUILayout.Width (80));
					tg.splatSettings [k].action = (UnityTerrainGenerator.TerrainResourceAction)EditorGUILayout.EnumPopup (tg.splatSettings [k].action);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.EndVertical ();
					EditorGUILayout.EndHorizontal ();
					if (tg.splatSettings [k].action != UnityTerrainGenerator.TerrainResourceAction.Ignore)
						allIgnored = false;
					if (tg.splatSettings [k].action == UnityTerrainGenerator.TerrainResourceAction.Create) {
						needCreate = true;
					} else if ((tg.splatSettings [k].top == null || tg.splatSettings [k].dirt == null) && tg.splatSettings [k].action == UnityTerrainGenerator.TerrainResourceAction.Assigned) {
						needAssign = true;
					}
                        EditorGUILayout.Separator();
                    }
                }

				EditorGUILayout.Separator ();
                expandTrees = EditorGUILayout.Foldout(expandTrees, new GUIContent("Trees"), true, sectionHeaderStyle);
                if (expandTrees) {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Texture Size", GUILayout.Width(EditorGUIUtility.labelWidth));
				treeTextureSize = EditorGUILayout.IntField (treeTextureSize);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Frond Density", GUILayout.Width (EditorGUIUtility.labelWidth));
				frondDensity = EditorGUILayout.Slider (frondDensity, 0f, 1f);
				EditorGUILayout.EndHorizontal ();
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Tree Scale", GUILayout.Width (EditorGUIUtility.labelWidth));
				treeScale = EditorGUILayout.Slider (treeScale, 0.1f, 2f);
				EditorGUILayout.EndHorizontal ();
				for (int k = 0; k < tg.terrainData.treePrototypes.Length; k++) {
					EditorGUILayout.LabelField ("Tree " + (k + 1), GUILayout.Width (80));
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (new GUIContent (tg.treeSettings [k].preview), boxStyle, GUILayout.Width (thumbnailSize), GUILayout.Height (thumbnailSize));
					EditorGUILayout.BeginVertical ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Model Def.", GUILayout.Width (80));
					tg.treeSettings [k].md = (ModelDefinition)EditorGUILayout.ObjectField (tg.treeSettings [k].md, typeof(ModelDefinition), false);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Smooth", GUILayout.Width (80));
					tg.treeSettings [k].smoothPower = EditorGUILayout.Slider (tg.treeSettings [k].smoothPower, 0, 1f);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Action", GUILayout.Width (80));
					tg.treeSettings [k].action = (UnityTerrainGenerator.TerrainResourceAction)EditorGUILayout.EnumPopup (tg.treeSettings [k].action);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.EndVertical ();
					EditorGUILayout.EndHorizontal ();
					if (tg.treeSettings [k].action != UnityTerrainGenerator.TerrainResourceAction.Ignore)
						allIgnored = false;
					if (tg.treeSettings [k].action == UnityTerrainGenerator.TerrainResourceAction.Create) {
						needCreate = true;
                        } else if (tg.treeSettings[k].md == null && tg.treeSettings[k].action == UnityTerrainGenerator.TerrainResourceAction.Assigned) {
                            needAssign = true;
                        }
                        EditorGUILayout.Separator();
                    }
                }

                EditorGUILayout.Separator();
                expandVegetation = EditorGUILayout.Foldout(expandVegetation, new GUIContent("Vegetation"), true, sectionHeaderStyle);
                if (expandVegetation) {
                    EditorGUILayout.PropertyField(vegetationDensity);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Texture Size", GUILayout.Width(EditorGUIUtility.labelWidth));
				vegetationTextureSize = EditorGUILayout.IntField (vegetationTextureSize);
				EditorGUILayout.EndHorizontal ();
				for (int k = 0; k < tg.terrainData.detailPrototypes.Length; k++) {
					EditorGUILayout.LabelField ("Detail " + (k + 1), GUILayout.Width (EditorGUIUtility.labelWidth));
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField (new GUIContent (tg.detailSettings [k].preview), boxStyle, GUILayout.Width (thumbnailSize), GUILayout.Height (thumbnailSize));
					EditorGUILayout.BeginVertical ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Voxel Def.", GUILayout.Width (80));
					tg.detailSettings [k].vd = (VoxelDefinition)EditorGUILayout.ObjectField (tg.detailSettings [k].vd, typeof(VoxelDefinition), false);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Action", GUILayout.Width (80));
					tg.detailSettings [k].action = (UnityTerrainGenerator.TerrainResourceAction)EditorGUILayout.EnumPopup (tg.detailSettings [k].action);
					EditorGUILayout.EndHorizontal ();
					EditorGUILayout.EndVertical ();
					EditorGUILayout.EndHorizontal ();
					if (tg.detailSettings [k].action != UnityTerrainGenerator.TerrainResourceAction.Ignore)
						allIgnored = false;
					if (tg.detailSettings [k].action == UnityTerrainGenerator.TerrainResourceAction.Create) {
						needCreate = true;
					} else if (tg.detailSettings [k].vd == null && tg.detailSettings [k].action == UnityTerrainGenerator.TerrainResourceAction.Assigned) {
                            needAssign = true;
                        }
                        EditorGUILayout.Separator();
                    }
                }
			}
			EditorGUILayout.Separator ();
			if (!allIgnored) {
				if (needAssign) {
					EditorGUILayout.HelpBox ("Please check all 'Assigned' resources are correctly set in the list above or press 'Refresh' to reload TerrainData info.", MessageType.Warning);
				} else {
					EditorGUILayout.BeginHorizontal ();
					EditorGUILayout.LabelField ("Clean Folders", GUILayout.Width (EditorGUIUtility.labelWidth));
					cleanFolders = EditorGUILayout.Toggle (cleanFolders);
					EditorGUILayout.EndHorizontal ();
					if (needCreate) {
						EditorGUILayout.HelpBox ("Press 'Generate' to create voxel definitions for the terrain resources.", MessageType.Info);
						if (GUILayout.Button ("Generate")) {
							Generate (cleanFolders);
						}
					} else {
						if (GUILayout.Button ("Generate Again")) {
							Generate (true);
						}
					}
				}
			}

			EditorGUILayout.Separator ();

			serializedObject.ApplyModifiedProperties ();
		}


		void Generate (bool forceGeneration = false) {
			GenerateTerrainVoxels (forceGeneration);
			GenerateTreeModels (forceGeneration);
			GenerateVegetationVoxels (forceGeneration);

			EditorUtility.SetDirty (tg);
			AssetDatabase.SaveAssets ();

		}


		string GetPath () {
			WorldDefinition wd = VoxelPlayEnvironment.instance.world;
			return System.IO.Path.GetDirectoryName (AssetDatabase.GetAssetPath (wd)) + "/Resources/" + wd.name;
		}

		Texture2D CreateTextureFile (Texture2D tex, string path, string filename, float smoothPower) {
			if (smoothPower > 0) {
				TextureTools.Smooth (tex, smoothPower);
			}
			byte[] texBytes = tex.EncodeToPNG ();
			string fullPath = path + "/" + filename + ".png";
			System.IO.File.WriteAllBytes (fullPath, texBytes);
			AssetDatabase.ImportAsset (fullPath);
			TextureImporter importerSettings = AssetImporter.GetAtPath (fullPath) as TextureImporter;
			if (importerSettings != null) {
				importerSettings.isReadable = true;
				importerSettings.filterMode = FilterMode.Point;
				importerSettings.mipmapEnabled = false;
				importerSettings.SaveAndReimport ();
			}
			tex = AssetDatabase.LoadAssetAtPath<Texture2D> (fullPath);
			return tex;
		}

		VoxelDefinition GenerateVoxelFromTexture (Texture2D textureTop, Texture2D textureSide, Texture2D textureDirt, int textureSize, string path, string voxelDefinitionName, RenderType renderType, float smoothPower) {
			// Prepare top texture
			Texture2D texTop = null;
			if (textureTop != null) {
				texTop = Instantiate<Texture2D> (textureTop);
				texTop.name = textureTop.name;
				if (texTop.width != textureSize || texTop.height != textureSize) {
					TextureTools.Scale (texTop, textureSize, textureSize, FilterMode.Bilinear);
				}
				string texName = Sanitize (texTop.name);
				texTop = CreateTextureFile (texTop, path, texName, smoothPower);
			}

			// Side texture
			Texture2D texSide = null;
			if (textureSide != null) {
				if (textureSide == textureTop) {
					texSide = texTop;
				} else {
					texSide = Instantiate<Texture2D> (textureSide);
					texSide.name = textureSide.name;
					if (texSide.width != textureSize || texSide.height != textureSize) {
						TextureTools.Scale (texSide, textureSize, textureSize, FilterMode.Bilinear);
					}
					// Save texture
					string texSideName = Sanitize (texSide.name);
					texSide = CreateTextureFile (texSide, path, texSideName, smoothPower);
				}
			}

			// Dirt texture
			Texture2D texDirt = null;
			if (textureDirt != null) {
				if (textureDirt == textureTop) {
					texDirt = texTop;
				} else {
					texDirt = Instantiate<Texture2D> (textureDirt);
					texDirt.name = textureDirt.name;
					if (texDirt.width != textureSize || texDirt.height != textureSize) {
						TextureTools.Scale (texDirt, textureSize, textureSize, FilterMode.Bilinear);
					}
					// Save texture
					string texDirtName = Sanitize (texDirt.name);
					texDirt = CreateTextureFile (texDirt, path, texDirtName, smoothPower);
				}
			}

			// Setup and save voxel definition
			VoxelDefinition vd = ScriptableObject.CreateInstance<VoxelDefinition> ();
			vd.renderType = renderType;
			vd.textureTop = texTop;
			vd.textureSide = texSide;
			vd.textureBottom = texDirt;
			switch (renderType) {
			case RenderType.CutoutCross:
				vd.navigatable = false;
				vd.windAnimation = true;
				break;
			case RenderType.Cutout:
				vd.navigatable = false;
				vd.windAnimation = true;
				break;
			default:
				vd.navigatable = true;
				vd.windAnimation = false;
				break;
			}
			vd.name = Sanitize (voxelDefinitionName);
			string fullPath = path + "/" + vd.name + ".asset";
			AssetDatabase.CreateAsset (vd, fullPath);
			return vd;
		}


		void GenerateTerrainVoxels (bool forceGeneration) {
			string path = GetPath () + "/TerrainVoxels";
			CheckDirectory (path);
#if UNITY_2018_3_OR_NEWER
            for (int k = 0; k < tg.terrainData.terrainLayers.Length && k < tg.splatSettings.Length; k++) {
#else
			for (int k = 0; k < tg.terrainData.splatPrototypes.Length && k < tg.splatSettings.Length; k++) {
#endif
                if (forceGeneration || tg.splatSettings [k].action == UnityTerrainGenerator.TerrainResourceAction.Create) {
					if (tg.splatSettings [k].preview == null)
						continue;
					int dirtWith = tg.splatSettings [k].dirtWith - 1;
					string texBaseName = tg.splatSettings [k].preview.name;
					Texture2D texTop = Instantiate (tg.splatSettings [k].preview) as Texture2D;
					texTop.name = texBaseName + " Top";
					Texture2D texOther = Instantiate (tg.splatSettings [dirtWith].preview) as Texture2D;
					Texture2D texDirt = CreateDirtTexture (texTop, texOther, tg.splatSettings [dirtWith].blendPower, 0);
					texDirt.name = texBaseName + " Dirt";
					Texture2D texSide = CreateSideTexture (texTop, texDirt);
					texSide.name = texBaseName + " Side";
					tg.splatSettings [k].top = GenerateVoxelFromTexture (texTop, texSide, texDirt, terrainTextureSize, path, "VoxelTerrainTop " + texBaseName, RenderType.Opaque, tg.splatSettings[k].smoothPower);
					tg.splatSettings [k].dirt = GenerateVoxelFromTexture (texDirt, texDirt, texDirt, terrainTextureSize, path, "VoxelTerrainDirt " + texBaseName, RenderType.Opaque, tg.splatSettings[k].smoothPower);
					tg.splatSettings [k].action = UnityTerrainGenerator.TerrainResourceAction.Assigned;
				}
			}
		}

		Texture2D CreateDirtTexture (Texture2D texTop, Texture2D texOther, float smoothPower, float blendAmount) {
			if (texTop.width != terrainTextureSize || texTop.height != terrainTextureSize) {
				TextureTools.Scale (texTop, terrainTextureSize, terrainTextureSize);
			}
			if (texOther.width != terrainTextureSize || texOther.height != terrainTextureSize) {
				TextureTools.Scale (texOther, terrainTextureSize, terrainTextureSize);
			}
			Color32[] colorsTop = texTop.GetPixels32 ();
			Color32[] colorsOther = texOther.GetPixels32 ();
			for (int k = 0; k < colorsTop.Length; k++) {
				colorsTop [k].r = (byte)Mathf.Lerp (colorsTop [k].r, colorsOther [k].r, blendAmount);
				colorsTop [k].g = (byte)Mathf.Lerp (colorsTop [k].g, colorsOther [k].g, blendAmount);
				colorsTop [k].b = (byte)Mathf.Lerp (colorsTop [k].b, colorsOther [k].b, blendAmount);
				colorsTop [k].a = (byte)Mathf.Lerp (colorsTop [k].a, colorsOther [k].a, blendAmount);
			}
			Texture2D tex = new Texture2D (texTop.width, texTop.height, TextureFormat.ARGB32, false);
			tex.SetPixels32 (colorsTop);
			tex.Apply (true);
			return tex;
		}

		Texture2D CreateSideTexture (Texture2D texTop, Texture2D texDirt) {
			// Make side texture
			Color32[] colors = texTop.GetPixels32 ();
			Color32[] colorsDirt = texDirt.GetPixels32 ();
			int h = texTop.height;
			int w = texTop.width;
			int y0 = (int)(h * 0.6f);
			int i = y0 * w;
			for (int y = y0; y < h; y++) {
				float threshold = Mathf.Clamp01 (2f * ((float)(y - y0)) / (h - y0));
				for (int x = 0; x < w; x++, i++) {
					if (UnityEngine.Random.value < threshold) {
						colorsDirt [i].r = (byte)Mathf.Lerp (colorsDirt [i].r, colors [i].r, threshold);
						colorsDirt [i].g = (byte)Mathf.Lerp (colorsDirt [i].g, colors [i].g, threshold);
						colorsDirt [i].b = (byte)Mathf.Lerp (colorsDirt [i].b, colors [i].b, threshold);
						colorsDirt [i].a = (byte)Mathf.Lerp (colorsDirt [i].a, colors [i].a, threshold);
					}
				}
			}
			Texture2D sideTexture = new Texture2D (w, h, TextureFormat.ARGB32, false);
			sideTexture.SetPixels32 (colorsDirt);
			sideTexture.Apply (true);
			return sideTexture;
		}


		void GenerateVegetationVoxels (bool forceGeneration) {
			string path = GetPath () + "/Vegetation";
			CheckDirectory (path);
			for (int k = 0; k < tg.terrainData.detailPrototypes.Length; k++) {
				if (forceGeneration || tg.detailSettings [k].action == UnityTerrainGenerator.TerrainResourceAction.Create) {
					if (tg.detailSettings [k].preview == null)
						continue;
					TextureTools.EnsureTextureReadable (tg.detailSettings [k].preview);
					tg.detailSettings [k].vd = GenerateVoxelFromTexture (null, tg.detailSettings [k].preview, null, vegetationTextureSize, path, "VoxelVegetation " + tg.detailSettings [k].preview.name, RenderType.CutoutCross, 0);
					tg.detailSettings [k].action = UnityTerrainGenerator.TerrainResourceAction.Assigned;
				}
			}
		}

		void GenerateTreeModels (bool forceGeneration) {
			string path = GetPath () + "/Trees";
			CheckDirectory (path);
			List<ModelBit> bits = new List<ModelBit> ();

			for (int k = 0; k < tg.terrainData.treePrototypes.Length; k++) {
				if (forceGeneration || tg.treeSettings [k].action == UnityTerrainGenerator.TerrainResourceAction.Create) {
					// Get tree size
					GameObject o = tg.terrainData.treePrototypes [k].prefab;
					// Look for LOD0
					MeshRenderer[] rr = o.GetComponentsInChildren<MeshRenderer> ();
					if (rr.Length == 0)
						continue;
					MeshRenderer r = rr [0];
					for (int b = 0; b < rr.Length; b++) {
						if (rr [b].name.Contains ("LOD0")) {
							r = rr [b];
							break;
						}
					}
					// Get bounds of renderer
					Bounds bounds = r.bounds;
					int sizeX = (int)(bounds.size.x * treeScale);
					int sizeY = (int)(bounds.size.y * treeScale);
					int sizeZ = (int)(bounds.size.z * treeScale);
					if (sizeX == 0 || sizeY == 0 || sizeZ == 0)
						continue;
					
					// Build model definition
					ModelDefinition md = ScriptableObject.CreateInstance<ModelDefinition> ();
					md.sizeX = sizeX;
					md.sizeY = sizeY;
					md.sizeZ = sizeZ;
					bits.Clear ();
					MeshFilter mf = r.GetComponent<MeshFilter> ();
					Mesh mesh = mf.sharedMesh;
					Vector3[] vertices = mesh.vertices;
					Vector2[] uvs = mesh.uv;
					for (int m = 0; m < mesh.subMeshCount; m++) {
						int[] triangles = mesh.GetTriangles (m);
						for (int i = 0; i < triangles.Length; i += 3) {
							int i1 = triangles [i];
							int i2 = triangles [i + 1];
							int i3 = triangles [i + 2];
							Vector3 v1 = vertices [i1];
							Vector3 v2 = vertices [i2];
							Vector3 v3 = vertices [i3];
							Vector2 uv1 = uvs [i1];
							Vector2 uv2 = uvs [i2];
							Vector2 uv3 = uvs [i3];
							AddModelBit (bits, md, (v1 + v2 + v3) * treeScale / 3f, (uv1 + uv2 + uv3) / 3f, r.sharedMaterials [m], path, tg.treeSettings [k].smoothPower);
						}
					}
					md.bits = bits.ToArray ();

					string treeName = Sanitize (tg.terrainData.treePrototypes [k].prefab.name);
					md.name = "Tree " + treeName;
					string fullPath = path + "/" + md.name + ".asset";
					AssetDatabase.CreateAsset (md, fullPath);
					tg.treeSettings [k].md = md;
					tg.treeSettings [k].action = UnityTerrainGenerator.TerrainResourceAction.Assigned;
				}
			}
		}

		void AddModelBit (List<ModelBit>bits, ModelDefinition md, Vector3 pos, Vector2 uv, Material mat, string path, float smoothPower) {
			Texture2D tex = (Texture2D)mat.mainTexture;
			if (tex == null)
				return;
			int y = Mathf.Clamp (Mathf.FloorToInt (pos.y), 0, md.sizeY - 1);
			int z = Mathf.Clamp (Mathf.FloorToInt (pos.z) + md.sizeZ / 2, 0, md.sizeZ - 1);
			int x = Mathf.Clamp (Mathf.FloorToInt (pos.x) + md.sizeX / 2, 0, md.sizeX - 1);
			int voxelIndex = y * md.sizeZ * md.sizeX + z * md.sizeX + x;
			int bitCount = bits.Count;
			if (bitCount > 0) {
				for (int k = bitCount - 1; k >= 0; k--) {
					if (bits [k].voxelIndex == voxelIndex)
						return;
				}
			}
			TextureTools.EnsureTextureReadable (tex);
			ModelBit bit = new ModelBit ();
			bit.voxelIndex = voxelIndex;
			VoxelDefinition vd;
			if (textureVoxels == null) {
				textureVoxels = new Dictionary<Texture2D, VoxelDefinition> ();
			}
			if (!textureVoxels.TryGetValue (tex, out vd)) {
				RenderType rt = RenderType.Cutout;
				string matName = mat.name.ToUpper ();
				string texName = tex.name.ToUpper ();
				if (matName.Contains ("BRANCH") || matName.Contains ("BARK") || texName.Contains ("BARK")) {
					rt = RenderType.Opaque;
					vd = GenerateVoxelFromTexture (tex, tex, tex, treeTextureSize, path, "VoxelTree " + tex.name, rt, smoothPower);
				} else {
					Texture2D frondTex = GenerateFrondTexture (tex, uv, treeTextureSize, path);
					vd = GenerateVoxelFromTexture (frondTex, frondTex, frondTex, treeTextureSize, path, "VoxelTree " + frondTex.name, rt, smoothPower);
				}
				textureVoxels [tex] = vd;
			}
			bit.voxelDefinition = vd;
			bits.Add (bit);
		}

		Texture2D GenerateFrondTexture (Texture2D tex, Vector2 uv, int textureSize, string path) {
			Color32[] sourceColors = tex.GetPixels32 ();
			// Extract representative colors around uv position
			int w = textureSize;
			int h = textureSize;
			int x = Mathf.Clamp ((int)(w * uv.x), 0, w - 1);
			int y = Mathf.Clamp ((int)(h * uv.y), 0, h - 1);
			List<Color32> repColors = new List<Color32> ();
			int gap = textureSize / 2;
			for (int y0 = y - gap; y0 < y + gap; y0++) {
				int ty = Mathf.Clamp (y0, 0, h - 1);
				for (int x0 = x - gap; x0 < x + gap; x0++) {
					int tx = Mathf.Clamp (x0, 0, w - 1);
					repColors.Add (sourceColors [ty * w + tx]);
				}
			}
			Color32[] colors = repColors.ToArray ();
			Color32[] newColors = new Color32[w * h];
			int i = 0;
			for (int k = 0; k < newColors.Length; k++) {
				if (UnityEngine.Random.value > frondDensity)
					continue;
				for (int c = 0; c < colors.Length; c++) {
					if (colors [i].a > 128)
						break;
					i++;
					if (i >= colors.Length)
						i = 0;
				}
				newColors [k] = colors [i];
				newColors [k].a = 255;
				i++;
				if (i >= colors.Length)
					i = 0;
			}
			Texture2D subTex = new Texture2D (w, h, TextureFormat.ARGB32, false);
			subTex.filterMode = FilterMode.Point;
			subTex.name = tex.name + " Frond";
			subTex.SetPixels32 (newColors);
			subTex.Apply (true);
			return subTex;
		}

		void CheckDirectory (string path) {
			if (cleanFolders) {
				if (Directory.Exists (path)) {
					System.IO.DirectoryInfo di = new DirectoryInfo (path);
					foreach (FileInfo file in di.GetFiles()) {
						file.Delete (); 
					}
					foreach (DirectoryInfo dir in di.GetDirectories()) {
						dir.Delete (true); 
					}
				}
				AssetDatabase.Refresh ();
			}
			System.IO.Directory.CreateDirectory (path);
		}

		string Sanitize (string s) {
			if (string.IsNullOrEmpty (s))
				return "";
			int k = s.IndexOf ("(Clone)");
			if (k >= 0) {
				s = s.Substring (0, k);
			}
			char[] invalidChars = System.IO.Path.GetInvalidFileNameChars ();
			for (int i = 0; i < invalidChars.Length; i++) {
				if (s.IndexOf (invalidChars [i]) >= 0) {
					s = s.Replace (invalidChars [i], '_');
				}
			}
			return s;
		}


	

	}

}