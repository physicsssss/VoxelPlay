using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

namespace VoxelPlay {

	public partial class VoxelPlayFirstPersonController :  VoxelPlayCharacterControllerBase {

        GameObject voxelHighlightBuilder;

#if UNITY_EDITOR
        [HideInInspector]
        public ModelDefinition loadModel;

        [HideInInspector]
        public int constructorSize = 15;

        GameObject grid;
        const string GRID_NAME = "Voxel Play Builder Grid";
        Vector3 buildingPosition;
        Vector3 beforeConstructorPosition;
        bool beforeOrbitMode, beforeFreeMode, beforeEnableColliders;
        Vector3 size;
        Vector3 lastFPSPosition;
        Quaternion lastFPSRotation, lastCameraRotation;

        public void ToggleConstructor() {
            env.constructorMode = !env.constructorMode;
            if (env.constructorMode) {
                env.buildMode = true;
                GetModelSize();
            }
            if (env.constructorMode) {
                env.ShowMessage("<color=green>Entered </color><color=yellow>The Constructor</color>.");
            } else {
                env.ShowMessage("<color=green>Back to normal world. Press <color=white>B</color> to cancel <color=yellow>Build Mode</color>.</color>");
            }
            UpdateConstructorMode();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }




        void UpdateConstructorMode() {
            if (env.constructorMode) {
                if (grid == null) {
                    grid = Instantiate<GameObject>(Resources.Load<GameObject>("VoxelPlay/Prefabs/Grid"));
                    grid.name = GRID_NAME;
                } else {
                    grid.SetActive(true);
                }
                buildingPosition = Misc.vector3one * 1608f;
                if (size.x % 2 != 0) { buildingPosition.x += 0.5f; }
                if (size.z % 2 != 0) { buildingPosition.z += 0.5f; }

                grid.transform.localScale = size;
                Vector3 gridPos = buildingPosition + new Vector3(0, size.y / 2, 0);
                grid.transform.position = gridPos;
                grid.GetComponent<Renderer>().sharedMaterial.SetVector("_Size", size);
                Transform gridPivot = grid.transform.Find("GridPivot");
                if (gridPivot!= null) {
                    gridPivot.transform.localScale = new Vector3(0.1f / size.x, 0.1f / size.y, 0.1f / size.z);
                }
                beforeOrbitMode = orbitMode;
                beforeFreeMode = freeMode;
                beforeConstructorPosition = transform.position;
                beforeEnableColliders = env.enableColliders;
                env.enableColliders = false;
                orbitMode = false;
                freeMode = false;
                if (lastFPSPosition != Misc.vector3zero) {
                    MoveTo(lastFPSPosition);
                    transform.rotation = lastFPSRotation;
                    m_Camera.transform.rotation = lastCameraRotation;
                } else {
                    MoveTo(grid.transform.position);
                }
                isFlying = true;
                limitBounds = new Bounds (gridPos, new Vector3 (size.x - 1, size.y - 1, size.z - 1));
                UpdateVoxelHighlight ();
                voxelHighlightBuilder.SetActive(true);
            } else {
                StorePlayerPosition();
                if (grid != null) {
                    grid.SetActive(false);
                }
                if (voxelHighlightBuilder != null) {
                    voxelHighlightBuilder.SetActive(false);
                }
                isFlying = false;
                MoveTo(beforeConstructorPosition);
                orbitMode = beforeOrbitMode;
                freeMode = beforeFreeMode;
                env.enableColliders = beforeEnableColliders;
            }
        }

        void UpdateConstructor() {

            if (!env.buildMode || !env.constructorMode)
                return;

            UpdateVoxelHighlight();
        }

        void UpdateVoxelHighlight() {
            if (voxelHighlightBuilder == null) {
                voxelHighlightBuilder = Instantiate<GameObject>(Resources.Load<GameObject>("VoxelPlay/Prefabs/VoxelHighlight"));
            }

            Vector3 rawPos;
            if (crosshairOnBlock) {
                rawPos = crosshairHitInfo.voxelCenter + crosshairHitInfo.normal;
            } else {
                if (freeMode) {
                    Ray ray = m_Camera.ScreenPointToRay(Input.mousePosition);
                    rawPos = ray.origin + ray.direction * 4f;
                } else {
                    rawPos = m_Camera.transform.position + m_Camera.transform.forward * 4f;
                }
            }

            // Bound check
            for (int i = 0; i < 50; i++) {
                if (limitBounds.Contains(rawPos))
                    break;
                rawPos -= m_Camera.transform.forward * 0.1f;
            }

            rawPos.x = FastMath.FloorToInt(rawPos.x) + 0.5f;
            if (rawPos.x > limitBounds.max.x)
                rawPos.x = limitBounds.max.x - 0.5f;
            if (rawPos.x < limitBounds.min.x)
                rawPos.x = limitBounds.min.x + 0.5f;
            rawPos.y = FastMath.FloorToInt(rawPos.y) + 0.5f;
            if (rawPos.y > limitBounds.max.y)
                rawPos.y = limitBounds.max.y - 0.5f;
            if (rawPos.y < limitBounds.min.y)
                rawPos.y = limitBounds.min.y + 0.5f;
            rawPos.z = FastMath.FloorToInt(rawPos.z) + 0.5f;
            if (rawPos.z > limitBounds.max.z)
                rawPos.z = limitBounds.max.z - 0.5f;
            if (rawPos.z < limitBounds.min.z)
                rawPos.z = limitBounds.min.z + 0.5f;
            voxelHighlightBuilder.transform.position = rawPos;
        }


        public void NewModel() {
            if (!env.constructorMode) return;

            if (!DisplayDialog("New Model", "Discard any change?", "Ok", "Cancel")) {
                return;
            }

            ClearConstructionArea();
            ResetPlayerPosition();
            loadModel = null;
            GetModelSize();
            StorePlayerPosition();
            UpdateConstructorMode();
        }

        public void LoadModel(ModelDefinition model) {
            if (!env.constructorMode || model == null) {
                return;
            }

            if (!DisplayDialog("Load Model", "Discard any change and load the model definition?", "Ok", "Cancel")) {
                return;
            }

            ClearConstructionArea();
            loadModel = model;
            GetModelSize();
            StorePlayerPosition();
            UpdateConstructorMode();

            if (!limitBounds.Contains(transform.position)) {
                ResetPlayerPosition();
            }

            // Loads model content
            Vector3 pos = buildingPosition - new Vector3(loadModel.offsetX, loadModel.offsetY, loadModel.offsetZ); // ignore offset
            env.ModelPlace(pos, loadModel, 0, 1f, false);
        }

        void ClearConstructionArea() {
            for (int y = 0; y < size.y; y++) {
                for (int z = 0; z < size.z; z++) {
                    for (int x = 0; x < size.x; x++) {
                        env.VoxelDestroy(buildingPosition + new Vector3(x - size.x / 2, y, z - size.z / 2));
                    }
                }
            }
        }

        void ResetPlayerPosition() {
            MoveTo(grid.transform.position);
            StorePlayerPosition();
        }

        public bool SaveModel(bool saveAsNew) {

            if (!env.constructorMode) return false;

            string modelFilename;
            bool isNew = (saveAsNew || loadModel == null);
            if (isNew) {
                modelFilename = "Assets/NewModelDefinition.asset";
            } else {
                modelFilename = AssetDatabase.GetAssetPath(loadModel);
            }
            if (!DisplayDialog("Save Model?", "Save current model to file " + modelFilename + "?", "Yes", "No"))
                return false;


            if (isNew) {
                loadModel = ScriptableObject.CreateInstance<ModelDefinition>();
            }
            List<ModelBit> bits = new List<ModelBit>();
            List<TorchBit> torchBits = new List<TorchBit> ();
            int sy = (int)size.y;
            int sz = (int)size.z;
            int sx = (int)size.x;

            for (int y = 0; y < sy; y++) {
                for (int z = 1; z < sz; z++) {
                    for (int x = 0; x < sx; x++) {
                        VoxelChunk chunk;
                        int voxelIndex;
                        Vector3 pos = buildingPosition + new Vector3 (x - size.x / 2, y, z - size.z / 2);
                        if (!env.GetVoxelIndex (pos, out chunk, out voxelIndex, false)) continue;
                        Voxel voxel = chunk.voxels [voxelIndex];
                        if (voxel.hasContent == 1 && !voxel.type.isDynamic) {
                            int k = y * sz * sx + z * sx + x;
                            ModelBit bit = new ModelBit();
                            bit.voxelIndex = k;
                            bit.voxelDefinition = voxel.type;
                            bit.color = voxel.color;
                            bit.rotation = voxel.GetTextureRotationDegrees ();
                            bits.Add(bit);
                        }
                        LightSource ls = chunk.GetLightSource (voxelIndex);
                        if ((object)ls != null) {
                            int k = y * sz * sx + z * sx + x;
                            TorchBit torchBit = new TorchBit ();
                            torchBit.itemDefinition = ls.itemDefinition;
                            torchBit.voxelIndex = k;
                            torchBit.normal = ls.hitInfo.normal;
                            torchBits.Add (torchBit);
                        }
                    }
                }
            }
            loadModel.bits = bits.ToArray();
            loadModel.torches = torchBits.ToArray ();
            loadModel.sizeX = sx;
            loadModel.sizeY = sy;
            loadModel.sizeZ = sz;

            if (isNew) {
                modelFilename = AssetDatabase.GenerateUniqueAssetPath("Assets/NewModelDefinition.asset");
                AssetDatabase.CreateAsset(loadModel, modelFilename);
            }
            EditorUtility.SetDirty(loadModel);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            env.ReloadTextures();

            if (isNew) {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = loadModel;
                EditorUtility.DisplayDialog("Save Model", "New model file created successfully in " + modelFilename + ".", "Ok");
            }

            return true;
        }

        void GetModelSize() {
            if (loadModel == null) {
                size.x = size.y = size.z = constructorSize;
            } else {
                size.x = loadModel.sizeX;
                size.y = loadModel.sizeY;
                size.z = loadModel.sizeZ;
            }
        }

        public void DisplaceModel(int dx, int dy, int dz) {
            int sy = (int)size.y;
            int sz = (int)size.z;
            int sx = (int)size.x;

            Voxel[] newContents = new Voxel[sy * sz * sx];
            int ny, nz, nx;
            for (int y = 0; y < sy; y++) {
                ny = y + dy;
                if (ny >= sy)
                    ny -= sy;
                else if (ny < 0)
                    ny += sy;
                for (int z = 0; z < sz; z++) {
                    nz = z + dz;
                    if (nz >= sz)
                        nz -= sz;
                    else if (nz < 0)
                        nz += sz;
                    for (int x = 0; x < sx; x++) {
                        Voxel voxel = env.GetVoxel(buildingPosition + new Vector3(x - size.x / 2, y, z - size.z / 2));
                        if (voxel.hasContent == 1) {
                            nx = x + dx;
                            if (nx >= sx)
                                nx -= sx;
                            else if (nx < 0)
                                nx += sx;
                            newContents[ny * sz * sx + nz * sx + nx] = voxel;
                        }
                    }
                }
            }

            // Replace voxels
            ClearConstructionArea();
            for (int y = 0; y < sy; y++) {
                for (int z = 0; z < sz; z++) {
                    for (int x = 0; x < sx; x++) {
                        int voxelIndex = y * sz * sx + z * sx + x;
                        if (!newContents[voxelIndex].isEmpty) {
                            env.VoxelPlace(buildingPosition + new Vector3(x - size.x / 2, y, z - size.z / 2), newContents[voxelIndex]);
                        }
                    }
                }
            }
        }


        public void ResizeModel(int dx, int dy, int dz) {
            size.x += dx;
            if (size.x < 1) size.x = 1;
            size.y += dy;
            if (size.y < 1) size.y = 1;
            size.z += dz;
            if (size.z < 1) size.z = 1;
            StorePlayerPosition();
            UpdateConstructorMode();
        }

        void StorePlayerPosition() {
            lastFPSPosition = transform.position;
            lastFPSRotation = transform.rotation;
            lastCameraRotation = m_Camera.transform.rotation;
        }
		
		bool DisplayDialog (string title, string message, string ok, string cancel = null) {
			mouseLook.SetCursorLock (false);
			bool res = EditorUtility.DisplayDialog (title, message, ok, cancel);
			mouseLook.SetCursorLock (true);
			return res;
		}

		#endif

	}
}
