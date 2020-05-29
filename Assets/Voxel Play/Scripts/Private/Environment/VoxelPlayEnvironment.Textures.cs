using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        struct WorldTexture
        {
            public Color32 [] colorsAndEmission;
            public Color32 [] normalsAndElevation;
        }

        static long [] distinctColors = {
            0xFF0000, 0x00FF00, 0x0000FF, 0xFFFF00, 0xFF00FF, 0x00FFFF, 0x000000,
            0x800000, 0x008000, 0x000080, 0x808000, 0x800080, 0x008080, 0x808080,
            0xC00000, 0x00C000, 0x0000C0, 0xC0C000, 0xC000C0, 0x00C0C0, 0xC0C0C0,
            0x400000, 0x004000, 0x000040, 0x404000, 0x400040, 0x004040, 0x404040,
            0x200000, 0x002000, 0x000020, 0x202000, 0x200020, 0x002020, 0x202020,
            0x600000, 0x006000, 0x000060, 0x606000, 0x600060, 0x006060, 0x606060,
            0xA00000, 0x00A000, 0x0000A0, 0xA0A000, 0xA000A0, 0x00A0A0, 0xA0A0A0,
            0xE00000, 0x00E000, 0x0000E0, 0xE0E000, 0xE000E0, 0x00E0E0, 0xE0E0E0
        };

        /// <summary>
        /// List containing all world textures availables
        /// </summary>
        List<WorldTexture> worldTextures;

        /// <summary>
        /// Dictionary for fast texture search
        /// </summary>
        Dictionary<Texture2D, int> worldTexturesDict;

        /// <summary>
        /// Dictionary lookup for the voxel definition by name
        /// </summary>
        Dictionary<string, VoxelDefinition> voxelDefinitionsDict;

        /// <summary>
        /// Set to true if the texture array needs to be recreated (ie. new voxel definitions have been added)
        /// </summary>
        bool requireTextureArrayUpdate;

        /// <summary>
        /// Temporary/session voxels added by users at runtime
        /// </summary>
        List<VoxelDefinition> sessionUserVoxels;
        int sessionUserVoxelsLastIndex;

        Color32 [] defaultMapColors, defaultPinkColors;

        //int texSize=1024;
        void DisposeTextures ()
        {
            if (voxelDefinitions != null) {
                for (int k = 0; k < voxelDefinitionsCount; k++) {
                    VoxelDefinition vd = voxelDefinitions [k];
                    if (vd != null) {
                        if (vd.textureThumbnailBottom != null) DestroyImmediate (vd.textureThumbnailBottom);
                        if (vd.textureThumbnailSide != null) DestroyImmediate (vd.textureThumbnailSide);
                        if (vd.textureThumbnailTop != null) DestroyImmediate (vd.textureThumbnailTop);
                    }
                }
            }
            if (modelHighlightMat != null) {
                DestroyImmediate (modelHighlightMat);
            }
        }


        /// <summary>
        /// Adds a voxel definition to the array. It doesn't do any safety check nor modifies the voxel definition except assigning an index
        /// </summary>
        bool AppendVoxelDefinition (VoxelDefinition vd)
        {

            if (vd == null)
                return false;

            if (vd.index > 0 && vd.index < voxelDefinitionsCount && voxelDefinitions [vd.index] == vd)
                return false; // already added

            // Resize voxel definitions array?
            if (voxelDefinitionsCount >= voxelDefinitions.Length) {
                voxelDefinitions = voxelDefinitions.Extend ();
            }

            voxelDefinitions [voxelDefinitionsCount] = vd;
            vd.index = (ushort)voxelDefinitionsCount;
            voxelDefinitionsCount++;
            voxelDefinitionsDict [vd.name] = vd;

            return true;
        }


        /// <summary>
        /// Inserts an user voxel definition to the array. It doesn't do any safety check nor modifies the voxel definition except assigning an index
        /// </summary>
        bool InsertUserVoxelDefinition (VoxelDefinition vd)
        {

            if (vd == null)
                return false;

            if (vd.index > 0 && vd.index < voxelDefinitionsCount && voxelDefinitions [vd.index] == vd)
                return false; // already added

            // Resize voxel definitions array?
            if (voxelDefinitionsCount >= voxelDefinitions.Length) {
                voxelDefinitions = voxelDefinitions.Extend ();
            }

            // Make space
            for (int k = voxelDefinitionsCount - 1; k > sessionUserVoxelsLastIndex + 1; k--) {
                voxelDefinitions [k] = voxelDefinitions [k - 1];
                voxelDefinitions [k].index++;
            }
            sessionUserVoxelsLastIndex++;
            vd.index = (ushort)sessionUserVoxelsLastIndex;
            voxelDefinitions [sessionUserVoxelsLastIndex] = vd;
            voxelDefinitionsCount++;
            voxelDefinitionsDict [vd.name] = vd;

            sessionUserVoxels.Add (vd);

            return true;
        }

        void AddVoxelTextures (VoxelDefinition vd)
        {

            if (!AppendVoxelDefinition (vd)) {
                return;
            }


            // Autofix certain non supported properties
            if (vd.navigatable) {
                vd.navigatable = vd.renderType.supportsNavigation ();
            }

            // Check if custom model has collider
            vd.prefabUsesCollider = false;
            if (vd.renderType == RenderType.Custom) {
                if (vd.model == null) {
                    // custom voxel is missing model so we assign a default cube
                    vd.model = GetDefaultVoxelPrefab ();
                }
                vd.prefab = vd.model;
                if (vd.model != null) {
                    if (vd.prefabMaterial != CustomVoxelMaterial.PrefabMaterial) {
                        Material instancingMat = null;
                        switch (vd.prefabMaterial) {
                        case CustomVoxelMaterial.VertexLit: instancingMat = Resources.Load<Material> ("VoxelPlay/Materials/VP Model VertexLit"); break;
                        case CustomVoxelMaterial.Texture: instancingMat = Resources.Load<Material> ("VoxelPlay/Materials/VP Model Texture"); break;
                        case CustomVoxelMaterial.TextureAlpha: instancingMat = Resources.Load<Material> ("VoxelPlay/Materials/VP Model Texture Alpha"); break;
                        case CustomVoxelMaterial.TextureAlphaDoubleSided: instancingMat = Resources.Load<Material> ("VoxelPlay/Materials/VP Model Texture Alpha Double Sided"); break;
                        case CustomVoxelMaterial.TextureTriplanar: instancingMat = Resources.Load<Material> ("VoxelPlay/Materials/VP Model Texture Triplanar"); break;
                        case CustomVoxelMaterial.TextureCutout: instancingMat = Resources.Load<Material> ("VoxelPlay/Materials/VP Model Texture Cutout"); break;
                        }
                        if (instancingMat != null) {
                            instancingMat = Instantiate<Material> (instancingMat);
                            vd.prefab = Instantiate<GameObject> (vd.model);
                            vd.prefab.SetActive (false);
                            vd.prefab.transform.SetParent (transform, false);
                            Renderer [] rr = vd.prefab.GetComponentsInChildren<Renderer> ();
                            for (int k = 0; k < rr.Length; k++) {
                                Material refMat = rr [k].sharedMaterial;
                                if (refMat != null) {
                                    if (refMat.HasProperty ("_Color") && instancingMat.HasProperty ("_Color")) {
                                        instancingMat.SetColor ("_Color", refMat.GetColor ("_Color"));
                                    }
                                    if (refMat.HasProperty ("_MainTex") && instancingMat.HasProperty ("_MainTex")) {
                                        instancingMat.SetTexture ("_MainTex", refMat.GetTexture ("_MainTex"));
                                    }
                                    if (refMat.HasProperty ("_BumpMap") && instancingMat.HasProperty ("_BumpMap")) {
                                        instancingMat.SetTexture ("_BumpMap", refMat.GetTexture ("_BumpMap"));
                                    }
                                }
                                rr [k].sharedMaterial = instancingMat;
                            }
                        }
                    }
                    // annotate if model has collider
                    Collider prefabCollider = vd.prefab.GetComponentInChildren<Collider> ();
                    bool hasPrefabCollider = prefabCollider != null;
                    if (vd.gpuInstancing) {
                        if (vd.createGameObject) {
                            vd.prefabUsesCollider = hasPrefabCollider;
                        }
                    } else {
                        vd.prefabUsesCollider = hasPrefabCollider;
                    }
                    if (hasPrefabCollider && applicationIsPlaying && prefabCollider is BoxCollider) {
                        StartCoroutine (ComputePrefabBoxColliderBounds (vd));
                    }
                }
                if (vd.textureSide == null) {
                    // assign default texture sample for inventory icons
                    Material modelMaterial = vd.material;
                    if (modelMaterial != null && modelMaterial.mainTexture != null && modelMaterial.mainTexture is Texture2D) {
                        vd.icon = (Texture2D)modelMaterial.mainTexture;
                    }
                }
            }

            // Assign default material
            Material mat = vd.GetOverrideMaterial ();
            if (mat == null) {
                switch (vd.renderType) {
                case RenderType.Opaque:
                case RenderType.Opaque6tex:
                    vd.materialBufferIndex = INDICES_BUFFER_OPAQUE;
                    break;
                case RenderType.OpaqueAnimated:
                    vd.materialBufferIndex = INDICES_BUFFER_OPANIM;
                    break;
                case RenderType.Cutout:
                    vd.materialBufferIndex = INDICES_BUFFER_CUTOUT;
                    break;
                case RenderType.CutoutCross:
                    vd.materialBufferIndex = INDICES_BUFFER_CUTXSS;
                    break;
                case RenderType.Water:
                    vd.materialBufferIndex = INDICES_BUFFER_WATER;
                    break;
                case RenderType.Transp6tex:
                    vd.materialBufferIndex = INDICES_BUFFER_TRANSP;
                    break;
                case RenderType.Cloud:
                    vd.materialBufferIndex = INDICES_BUFFER_CLOUD;
                    break;
                case RenderType.OpaqueNoAO:
                    vd.materialBufferIndex = INDICES_BUFFER_OPNOAO;
                    break;
                }
            } else {
                // Assign material index
                int materialBufferIndex;
                if (!materialIndices.TryGetValue (mat, out materialBufferIndex)) {
                    if (lastBufferIndex < renderingMaterials.Length - 1) {
                        lastBufferIndex++;
                        materialIndices [mat] = lastBufferIndex;
                        if (vd.texturesByMaterial) {
                            renderingMaterials [lastBufferIndex].material = mat;
                            renderingMaterials [lastBufferIndex].usesTextureArray = false;
                        } else {
                            renderingMaterials [lastBufferIndex].material = Instantiate<Material> (mat);
                            renderingMaterials [lastBufferIndex].usesTextureArray = true;
                        }
                    } else {
                        Debug.LogError ("Too many override materials. Max materials supported = " + MAX_MATERIALS_PER_CHUNK);
                    }
                    materialBufferIndex = lastBufferIndex;
                }
                vd.materialBufferIndex = materialBufferIndex;
            }

            // Compute voxel definition texture indices including rotations
            bool supportsEmission = vd.renderType.supportsEmission ();
            bool animated = vd.renderType.supportsTextureAnimation ();
            vd.textureIndexTop = AddTexture (vd.textureTop, supportsEmission ? vd.textureTopEmission : null, vd.textureTopNRM, vd.textureTopDISP, !animated);
            if (animated) {
                for (int k = 0; k < vd.animationTextures.Length; k++) {
                    AddTexture (vd.animationTextures [k].textureTop != null ? vd.animationTextures [k].textureTop : vd.textureTop, null, null, null, false);
                }
            }
            vd.textureIndexSide = AddTexture (vd.textureSide, supportsEmission ? vd.textureSideEmission : null, vd.textureSideNRM, vd.textureSideDISP, !animated);
            if (animated) {
                for (int k = 0; k < vd.animationTextures.Length; k++) {
                    AddTexture (vd.animationTextures [k].textureSide != null ? vd.animationTextures [k].textureSide : vd.textureSide, null, null, null, false);
                }
            }
            vd.textureIndexBottom = AddTexture (vd.textureBottom, supportsEmission ? vd.textureBottomEmission : null, vd.textureBottomNRM, vd.textureBottomDISP, !animated);
            if (animated) {
                for (int k = 0; k < vd.animationTextures.Length; k++) {
                    AddTexture (vd.animationTextures [k].textureBottom != null ? vd.animationTextures [k].textureBottom : vd.textureBottom, null, null, null, false);
                }
            }
            if (vd.textureSideIndices == null || vd.textureSideIndices.Length != 4) {
                vd.textureSideIndices = new TextureRotationIndices [4];
            }

            if (vd.renderType.numberOfTextures () == 6) {
                int textureIndexRight = vd.textureIndexRight = AddTexture (vd.textureRight, supportsEmission ? vd.textureRightEmission : null, vd.textureRightNRM, vd.textureRightDISP);
                int textureIndexForward = vd.textureIndexForward = AddTexture (vd.textureForward, supportsEmission ? vd.textureForwardEmission : null, vd.textureForwardNRM, vd.textureForwardDISP);
                int textureIndexLeft = vd.textureIndexLeft = AddTexture (vd.textureLeft, supportsEmission ? vd.textureLeftEmission : null, vd.textureLeftNRM, vd.textureLeftDISP);

                vd.textureSideIndices [0] = new TextureRotationIndices {
                    forward = textureIndexForward,
                    right = textureIndexRight,
                    back = vd.textureIndexSide,
                    left = textureIndexLeft
                };
                vd.textureSideIndices [1] = new TextureRotationIndices {
                    forward = textureIndexLeft,
                    right = textureIndexForward,
                    back = textureIndexRight,
                    left = vd.textureIndexSide
                };
                vd.textureSideIndices [2] = new TextureRotationIndices {
                    forward = vd.textureIndexSide,
                    right = textureIndexLeft,
                    back = textureIndexForward,
                    left = textureIndexRight
                };
                vd.textureSideIndices [3] = new TextureRotationIndices {
                    forward = textureIndexRight,
                    right = vd.textureIndexSide,
                    back = textureIndexLeft,
                    left = textureIndexForward
                };
            } else {
                vd.textureSideIndices [0] = vd.textureSideIndices [1] = vd.textureSideIndices [2] = vd.textureSideIndices [3] = new TextureRotationIndices {
                    forward = vd.textureIndexSide,
                    right = vd.textureIndexSide,
                    back = vd.textureIndexSide,
                    left = vd.textureIndexSide
                };
            }

            if (vd.renderType == RenderType.CutoutCross && vd.sampleColor.a == 0) {
                AnalyzeGrassTexture (vd, vd.textureSample != null ? vd.textureSample : vd.textureSide);
            } else {
                if (vd.textureSample != null) {
                    Color32 [] colors = vd.textureSample.GetPixels32 ();
                    vd.sampleColor = colors [Random.Range (0, colors.Length)];
                } else if (vd.textureIndexSide > 0) {
                    Color32 [] colors = worldTextures [vd.textureIndexSide].colorsAndEmission;
                    vd.sampleColor = colors [Random.Range (0, colors.Length)];
                }
            }

            GetVoxelThumbnails (vd);
        }

        /// <summary>
        /// Returns the index in the texture list and the full index (index in the list + some flags specifying existence of normal/displacement maps)
        /// </summary>
        int AddTexture (Texture2D texAlbedo, Texture2D texEmission, Texture2D texNRM, Texture2D texDISP, bool avoidRepetitions = true)
        {
            int index = 0;
            if (texAlbedo == null || (avoidRepetitions && worldTexturesDict.TryGetValue (texAlbedo, out index))) {
                return index;
            }

            // Add entry to dictionary
            index = worldTextures.Count;
            if (avoidRepetitions) {
                worldTexturesDict [texAlbedo] = index;
            }

            // Albedo + Emission mask
            WorldTexture wt = new WorldTexture ();
            wt.colorsAndEmission = CombineAlbedoAndEmission (texAlbedo, texEmission);
            worldTextures.Add (wt);

            // Normal + Elevation Map
            if (enableNormalMap || enableReliefMapping) {
                WorldTexture wextra = new WorldTexture ();
                wextra.normalsAndElevation = CombineNormalsAndElevation (texNRM, texDISP);
                worldTextures.Add (wextra);
            }

            return index;
        }


        /// <summary>
        /// Returns the index of a given texture in the internal array texture. Used for certain Voxel Play extensions, like Connected Texture.
        /// </summary>
        public int GetTextureIndex (Texture2D texture)
        {
            int index = 0;
            worldTexturesDict.TryGetValue (texture, out index);
            return index;
        }

        Color32 [] CombineAlbedoAndEmission (Texture2D albedoMap, Texture2D emissionMap = null)
        {
            Color32 [] mapColors;
            if (albedoMap == null) {
                return GetPinkColors ();
            }
            if (albedoMap.width != textureSize) {
                albedoMap = Instantiate (albedoMap) as Texture2D;
                albedoMap.hideFlags = HideFlags.DontSave;
                TextureTools.Scale (albedoMap, textureSize, textureSize, FilterMode.Point);
                mapColors = albedoMap.GetPixels32 ();
                DestroyImmediate (albedoMap);
            } else {
                mapColors = albedoMap.GetPixels32 ();
            }
            if (emissionMap == null) {
                return mapColors;
            }
            Color32 [] emissionColors;
            if (emissionMap.width != textureSize) {
                emissionMap = Instantiate (emissionMap) as Texture2D;
                emissionMap.hideFlags = HideFlags.DontSave;
                TextureTools.Scale (emissionMap, textureSize, textureSize, FilterMode.Point);
                emissionColors = emissionMap.GetPixels32 ();
                DestroyImmediate (emissionMap);
            } else {
                emissionColors = emissionMap.GetPixels32 ();
            }
            for (int k = 0; k < mapColors.Length; k++) {
                mapColors [k].a = (byte)(255 - emissionColors [k].r);
            }
            return mapColors;
        }


        Color32 [] CombineNormalsAndElevation (Texture2D normalMap, Texture2D elevationMap)
        {
            if (elevationMap == null && normalMap == null) {
                return GetDefaultMapColors ();
            }
            Color32 [] normalMapColors, elevationMapColors;
            if (normalMap == null) {
                normalMapColors = GetDefaultMapColors ();
            } else if (normalMap.width != textureSize) {
                normalMap = Instantiate (normalMap) as Texture2D;
                normalMap.hideFlags = HideFlags.DontSave;
                TextureTools.Scale (normalMap, textureSize, textureSize, FilterMode.Point);
                normalMapColors = normalMap.GetPixels32 ();
                DestroyImmediate (normalMap);
            } else {
                normalMapColors = normalMap.GetPixels32 ();
            }
            if (elevationMap == null) {
                elevationMapColors = GetDefaultMapColors ();
            } else if (elevationMap.width != textureSize) {
                elevationMap = Instantiate (elevationMap) as Texture2D;
                elevationMap.hideFlags = HideFlags.DontSave;
                TextureTools.Scale (elevationMap, textureSize, textureSize, FilterMode.Point);
                elevationMapColors = elevationMap.GetPixels32 ();
                DestroyImmediate (elevationMap);
            } else {
                elevationMapColors = elevationMap.GetPixels32 ();
            }
            for (int k = 0; k < normalMapColors.Length; k++) {
                normalMapColors [k].a = elevationMapColors [k].r;   // copy elevation into alpha channel of normal map to save 1 texture slot in texture array and optimize cache
            }
            return normalMapColors;
        }

        Color32 [] GetPinkColors ()
        {
            int len = textureSize * textureSize;
            if (defaultPinkColors != null && defaultPinkColors.Length == len) {
                return defaultPinkColors;
            }
            defaultPinkColors = new Color32 [len];
            Color32 color = new Color32 (255, 0, 0x80, 255);
            defaultPinkColors.Fill<Color32> (color);
            return defaultPinkColors;
        }

        Color32 [] GetDefaultMapColors ()
        {
            int len = textureSize * textureSize;
            if (defaultMapColors != null && defaultMapColors.Length == len) {
                return defaultMapColors;
            }
            defaultMapColors = new Color32 [len];
            Color32 color = new Color32 (0, 0, 255, 255);
            defaultMapColors.Fill<Color32> (color);
            return defaultMapColors;
        }

        IEnumerator ComputePrefabBoxColliderBounds (VoxelDefinition vd)
        {
            bool oldActiveState = vd.prefab.activeSelf;
            vd.prefab.SetActive (false);
            GameObject dummy = Instantiate<GameObject> (vd.prefab);
            // Disable all components to avoid undesired effects
            Component [] components = dummy.GetComponents<Component> ();
            for (int k = 0; k < components.Length; k++) {
                MonoBehaviour mono = components [k] as MonoBehaviour;
                if (mono != null) {
                    mono.enabled = false;
                }
            }
            vd.prefab.SetActive (oldActiveState);
            dummy.hideFlags = HideFlags.HideAndDontSave;
            dummy.SetActive (true);
            dummy.transform.position = new Vector3 (0, 10000, 10000);
            dummy.transform.rotation = Misc.quaternionZero;
            dummy.transform.localScale = Misc.vector3one;

            yield return new WaitForEndOfFrame ();
            BoxCollider collider = dummy.GetComponentInChildren<BoxCollider> ();
            Bounds bounds = collider.bounds;
            bounds.center -= dummy.transform.position;
            vd.prefabColliderBounds = bounds;
            Destroy (dummy);
        }



        void AnalyzeGrassTexture (VoxelDefinition vd, Texture2D tex)
        {
            if (tex == null) {
                Debug.Log ("AnalyzeGrassTexture: texture not found for " + vd.name);
                return;
            }
            // get sample color (random pixel from texture raw data)
            Color [] colors = tex.GetPixels ();
            int tw = tex.width;
            int th = tex.height;
            int pos = 4 * tw + tw * 3 / 4;
            if (pos >= colors.Length)
                pos = colors.Length - 1;
            for (int k = pos; k > 0; k--) {
                if (colors [k].a > 0.5f) {
                    vd.sampleColor = colors [k];
                    break;
                }
            }
            // get grass dimensions
            int xmin, xmax, ymin, ymax;
            xmin = tw;
            xmax = 0;
            ymin = th;
            ymax = 0;
            for (int y = 0; y < th; y++) {
                int yy = y * tw;
                for (int x = 0; x < tw; x++) {
                    if (colors [yy + x].a > 0.5f) {
                        if (x < xmin)
                            xmin = x;
                        if (x > xmax)
                            xmax = x;
                        if (y < ymin)
                            ymin = y;
                        if (y > ymax)
                            ymax = y;
                    }
                }
            }
            float w = (xmax - xmin + 1f) / tw;
            float h = (ymax - ymin + 1f) / th;
            vd.scale = new Vector3 (w, h, w);
        }

        void GetVoxelThumbnails (VoxelDefinition vd)
        {
            Texture2D top, side, bottom;
            top = side = bottom = null;
            if (vd.overrideMaterial && vd.texturesByMaterial) {
                Material mat = vd.overrideMaterialNonGeo;
                Texture2D tex = (Texture2D)mat.mainTexture;
                if (tex != null) {
#if UNITY_EDITOR
                    string path = UnityEditor.AssetDatabase.GetAssetPath (tex);
                    if (!string.IsNullOrEmpty (path)) {
                        UnityEditor.TextureImporter timp = UnityEditor.AssetImporter.GetAtPath (path) as UnityEditor.TextureImporter;
                        if (timp != null && !timp.isReadable) {
                            timp.isReadable = true;
                            timp.SaveAndReimport ();
                        }
                    }
#endif
                    top = side = bottom = Instantiate<Texture2D> (tex);
                }

            } else {
                if (vd.renderType == RenderType.Custom && vd.textureSample != null) {
                    top = side = bottom = vd.textureSample;
                } else {
                    top = vd.textureTop;
                    side = vd.textureSide;
                    bottom = vd.textureBottom;
                }
            }
            if (top != null) {
                vd.textureThumbnailTop = Instantiate (top) as Texture2D;
                vd.textureThumbnailTop.hideFlags = HideFlags.DontSave;
                TextureTools.Scale (vd.textureThumbnailTop, 64, 64, FilterMode.Point);
            }
            if (side != null) {
                vd.textureThumbnailSide = Instantiate (side) as Texture2D;
                vd.textureThumbnailSide.hideFlags = HideFlags.DontSave;
                TextureTools.Scale (vd.textureThumbnailSide, 64, 64, FilterMode.Point);
            }
            if (bottom != null) {
                vd.textureThumbnailBottom = Instantiate (bottom) as Texture2D;
                vd.textureThumbnailBottom.hideFlags = HideFlags.DontSave;
                TextureTools.Scale (vd.textureThumbnailBottom, 64, 64, FilterMode.Point);
            }
        }


        void LoadWorldTextures ()
        {

            requireTextureArrayUpdate = false;

            // Init texture array
            if (worldTextures == null) {
                worldTextures = new List<WorldTexture> ();
            } else {
                worldTextures.Clear ();
            }
            if (worldTexturesDict == null) {
                worldTexturesDict = new Dictionary<Texture2D, int> ();
            } else {
                worldTexturesDict.Clear ();
            }

            // Clear definitions
            if (voxelDefinitions != null) {
                // Voxel Definitions no longer are added to the dictionary, clear the index field.
                for (int k = 0; k < voxelDefinitionsCount; k++) {
                    if (voxelDefinitions [k] != null) {
                        voxelDefinitions [k].Reset ();
                    }
                }
            } else {
                voxelDefinitions = new VoxelDefinition [128];
            }
            voxelDefinitionsCount = 0;
            if (voxelDefinitionsDict == null) {
                voxelDefinitionsDict = new Dictionary<string, VoxelDefinition> ();
            } else {
                voxelDefinitionsDict.Clear ();
            }
            if (sessionUserVoxels == null) {
                sessionUserVoxels = new List<VoxelDefinition> ();
            }

            // The null voxel definition
            VoxelDefinition nullVoxelDefinition = ScriptableObject.CreateInstance<VoxelDefinition> ();
            nullVoxelDefinition.name = "Null";
            nullVoxelDefinition.hidden = true;
            nullVoxelDefinition.canBeCollected = false;
            nullVoxelDefinition.ignoresRayCast = true;
            nullVoxelDefinition.renderType = RenderType.Empty;
            AddVoxelTextures (nullVoxelDefinition);

            // Check default voxel
            if (defaultVoxel == null) {
                defaultVoxel = Resources.Load<VoxelDefinition> ("VoxelPlay/Defaults/DefaultVoxel");
            }
            AddVoxelTextures (defaultVoxel);

            // Add all biome textures
            if (world.biomes != null) {
                for (int k = 0; k < world.biomes.Length; k++) {
                    BiomeDefinition biome = world.biomes [k];
                    if (biome == null)
                        continue;
                    if (biome.voxelTop != null) {
                        AddVoxelTextures (biome.voxelTop);
                        if (biome.voxelTop.biomeDirtCounterpart == null) {
                            biome.voxelTop.biomeDirtCounterpart = biome.voxelDirt;
                        }
                    }
                    AddVoxelTextures (biome.voxelDirt);
                    if (biome.vegetation != null) {
                        for (int v = 0; v < biome.vegetation.Length; v++) {
                            AddVoxelTextures (biome.vegetation [v].vegetation);
                        }
                    }
                    if (biome.trees != null) {
                        for (int t = 0; t < biome.trees.Length; t++) {
                            ModelDefinition tree = biome.trees [t].tree;
                            if (tree == null)
                                continue;
                            for (int b = 0; b < tree.bits.Length; b++) {
                                AddVoxelTextures (tree.bits [b].voxelDefinition);
                            }
                        }
                    }
                    if (biome.ores != null) {
                        for (int v = 0; v < biome.ores.Length; v++) {
                            // ensure proper size
                            if (biome.ores [v].veinMinSize == biome.ores [v].veinMaxSize && biome.ores [v].veinMaxSize == 0) {
                                biome.ores [v].veinMinSize = 2;
                                biome.ores [v].veinMaxSize = 6;
                                biome.ores [v].veinsCountMin = 1;
                                biome.ores [v].veinsCountMax = 2;
                            }
                            AddVoxelTextures (biome.ores [v].ore);
                        }
                    }
                }
            }

            // Special voxels
            if (enableClouds) {
                if (world.cloudVoxel == null) {
                    world.cloudVoxel = Resources.Load<VoxelDefinition> ("VoxelPlay/Defaults/VoxelCloud");
                }
                AddVoxelTextures (world.cloudVoxel);
            }

            // Add additional world voxels
            if (world.moreVoxels != null) {
                for (int k = 0; k < world.moreVoxels.Length; k++) {
                    AddVoxelTextures (world.moreVoxels [k]);
                }
            }

            // Add all items' textures are available
            if (world.items != null) {
                int itemCount = world.items.Length;
                for (int k = 0; k < itemCount; k++) {
                    ItemDefinition item = world.items [k];
                    if (item != null && item.category == ItemCategory.Voxel) {
                        AddVoxelTextures (item.voxelType);
                    }
                }
            }

            // Add any other voxel found inside Defaults
            VoxelDefinition [] vdd = Resources.LoadAll<VoxelDefinition> ("VoxelPlay/Defaults");
            for (int k = 0; k < vdd.Length; k++) {
                AddVoxelTextures (vdd [k]);
            }

            // Add any other voxel found inside World directory
            if (!string.IsNullOrEmpty (world.name)) {
                vdd = Resources.LoadAll<VoxelDefinition> ("Worlds/" + world.name);
                for (int k = 0; k < vdd.Length; k++) {
                    AddVoxelTextures (vdd [k]);
                }

                // Add any other voxel found inside a resource directory with same name of world (if not placed into Worlds directory)
                vdd = Resources.LoadAll<VoxelDefinition> (world.name);
                for (int k = 0; k < vdd.Length; k++) {
                    AddVoxelTextures (vdd [k]);
                }
            }

            // Add any other voxel found inside a resource directory under the world definition asset
            if (!string.IsNullOrEmpty (world.resourceLocation)) {
                vdd = Resources.LoadAll<VoxelDefinition> (world.resourceLocation);
                for (int k = 0; k < vdd.Length; k++) {
                    AddVoxelTextures (vdd [k]);
                }
            }

            // Add connected textures
            ConnectedTexture [] ctt = Resources.LoadAll<ConnectedTexture> ("");
            for (int k = 0; k < ctt.Length; k++) {
                ConnectedTexture ct = ctt [k];
                VoxelDefinition vd = ctt [k].voxelDefinition;
                if (vd == null || vd.index == 0) continue;
                for (int j = 0; j < ct.config.Length; j++) {
                    ct.config [j].textureIndex = AddTexture (ct.config [j].texture, null, null, null);
                }
                ct.Init ();
            }

            // Add user provided voxels during playtime
            int count = sessionUserVoxels.Count;
            for (int k = 0; k < count; k++) {
                AddVoxelTextures (sessionUserVoxels [k]);
            }
            sessionUserVoxelsLastIndex = voxelDefinitionsCount - 1;

            // Add transparent voxel definitions for the see-through effect
            if (seeThrough) {
                int lastOne = voxelDefinitionsCount; // this loop will add voxels so end at the last regular voxel definition (don't process see-through versions)
                for (int k = 0; k < lastOne; k++) {
                    VoxelDefinition vd = voxelDefinitions [k];
                    if (vd.renderType == RenderType.CutoutCross) {
                        vd.seeThroughMode = SeeThroughMode.FullyInvisible;
                    } else {
                        if (vd.seeThroughMode == SeeThroughMode.Transparency) {
                            if (vd.renderType.supportsAlphaSeeThrough ()) {
                                vd.seeThroughVoxelTempTransp = CreateSeeThroughVoxelDefinition (vd);
                            } else {
                                vd.seeThroughMode = SeeThroughMode.FullyInvisible;
                            }
                        }
                    }
                }
            }

            // Create array texture
            int textureCount = worldTextures.Count;
            if (textureCount > 0) {
                Texture2DArray pointFilterTextureArray = new Texture2DArray (textureSize, textureSize, textureCount, TextureFormat.ARGB32, hqFiltering);
                if (enableReliefMapping || !enableSmoothLighting) {
                    pointFilterTextureArray.wrapMode = TextureWrapMode.Repeat;
                } else {
                    pointFilterTextureArray.wrapMode = TextureWrapMode.Clamp;
                }
                pointFilterTextureArray.filterMode = hqFiltering ? FilterMode.Bilinear : FilterMode.Point;
                pointFilterTextureArray.mipMapBias = -mipMapBias;
                for (int k = 0; k < textureCount; k++) {
                    if (worldTextures [k].colorsAndEmission != null) {
                        pointFilterTextureArray.SetPixels32 (worldTextures [k].colorsAndEmission, k);
                    } else if (worldTextures [k].normalsAndElevation != null) {
                        pointFilterTextureArray.SetPixels32 (worldTextures [k].normalsAndElevation, k);
                    }
                }
                worldTextures.Clear ();

                pointFilterTextureArray.Apply (hqFiltering, true);
                
                // Assign textures to materials
                if (renderingMaterials != null) {
                    for (int k = 0; k < renderingMaterials.Length; k++) {
                        if (renderingMaterials [k].usesTextureArray) {
                            Material mat = renderingMaterials [k].material;
                            if (mat != null && mat.HasProperty ("_MainTex")) {
                                mat.SetTexture ("_MainTex", pointFilterTextureArray);
                            }
                        }
                    }
                }
                matDynamicOpaque.SetTexture ("_MainTex", pointFilterTextureArray);
                matDynamicCutout.SetTexture ("_MainTex", pointFilterTextureArray);

                if (modelHighlightMat == null) {
                    modelHighlightMat = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Highlight Model")) as Material;
                }
                modelHighlightMat.SetTexture ("_MainTex", pointFilterTextureArray);
            }
        }


        /// <summary>
        /// Assigns a color to each biome.
        /// </summary>
        public void SetBiomeDefaultColors (bool force)
        {
            if (world != null) {
                if (world.biomes != null) {
                    for (int b = 0; b < world.biomes.Length; b++) {
                        BiomeDefinition biome = world.biomes [b];
                        if (biome == null || biome.zones == null)
                            continue;
                        if (force || biome.biomeMapColor.a == 0) {
                            long color = distinctColors [b % distinctColors.Length];
                            Color32 biomeColor = new Color32 ((byte)(color >> 16), (byte)((color >> 8) & 255), (byte)(color & 255), 255);
                            biome.biomeMapColor = biomeColor;
                        }
                    }
                }
            }
        }




    }


}
