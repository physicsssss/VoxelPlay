using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {
        public const byte FULL_OPAQUE = 15;
        public const byte FULL_LIGHT = 15;

        [NonSerialized]
        public VoxelDefinition [] voxelDefinitions;
        public int voxelDefinitionsCount;

        GameObject defaultVoxelPrefab;
        List<VoxelChunk> voxelPlaceFastAffectedChunks;
        GameObject voxelHighlightGO;


        float [] collapsingOffsets = {
            0, 1, 0,
            -1, 1, -1,
            1, 1, 1,
            -1, 1, 1,
            1, 1, -1,
            1, 1, 0,
            0, 1, 1,
            -1, 1, 0,
            0, 1, -1,
            -1, 1, -1,
            1, 0, 1,
            -1, 0, 1,
            1, 0, -1,
            1, 0, 0,
            0, 0, 1,
            -1, 0, 0,
            0, 0, -1,
            -1, 0, -1
        };


        List<Vector3> tempVertices;
        List<Vector3> tempNormals;
        int [] tempIndices;
        int tempIndicesPos;
        List<Vector4> tempUVs;
        List<Color32> tempColors;

        void VoxelDestroyFast (VoxelChunk chunk, int voxelIndex)
        {

            // Ensure there's content on this position
            if (chunk.voxels [voxelIndex].hasContent != 1)
                return;

            if (OnVoxelBeforeDestroyed != null) {
                OnVoxelBeforeDestroyed (chunk, voxelIndex);
            }

            // 1) Remove placeholder if exists
            VoxelPlaceholderDestroy (chunk, voxelIndex);

            // 2) Clears voxel
            bool triggerCollapse = voxelDefinitions [chunk.voxels [voxelIndex].typeIndex].triggerCollapse && !constructorMode;
            VoxelDestroyFastSingle (chunk, voxelIndex);

            // Voxel replacement
            VoxelDefinition replaceType = chunk.voxels [voxelIndex].type.replacedBy;
            if ((object)replaceType != null) {
                chunk.voxels [voxelIndex].Set (replaceType);
            }

            // Update lightmap and renderers
            if (chunk.lightSources != null && chunk.lightSources.Count > 0) {
                chunk.RemoveLightSource (voxelIndex);
                UpdateChunkRR (chunk);
            } else {
                ChunkRequestRefresh (chunk, true, true);
            }

            // Force rebuild neighbour meshes if destroyed voxel is on a border
            RebuildNeighbours (chunk, voxelIndex);

            if (OnChunkChanged != null) {
                OnChunkChanged (chunk);
            }
            if (OnVoxelDestroyed != null) {
                OnVoxelDestroyed (chunk, voxelIndex);
            }

            // Check if it was surrounded by water. If it was, add water expander
            Vector3 voxelPosition = GetVoxelPosition (chunk, voxelIndex);
            MakeSurroundingWaterExpand (chunk, voxelIndex, voxelPosition);

            // Check if voxels on top can fall
            if (triggerCollapse && world.collapseOnDestroy) {
                VoxelCollapse (voxelPosition, world.collapseAmount, null, world.consolidateDelay);
            }
        }

        void VoxelDestroyFastSingle (VoxelChunk chunk, int voxelIndex)
        {
            chunk.voxels [voxelIndex].Clear (effectiveGlobalIllumination ? (byte)0 : (byte)15);
            chunk.modified = true;
        }

        void VoxelRemoveFast (VoxelChunk chunk, int voxelIndex)
        {
            chunk.ClearVoxel (voxelIndex, noLightValue);
            ChunkRequestRefresh (chunk, true, true);
            RebuildNeighbours (chunk, voxelIndex);
            if (OnChunkChanged != null) {
                OnChunkChanged (chunk);
            }
        }

        /// <summary>
        /// Puts a voxel in the given position. Takes care of informing neighbour chunks.
        /// </summary>
        /// <returns>Returns the affected chunk and voxel index</returns>
        /// <param name="position">Position.</param>
        void VoxelPlaceFast (Vector3 position, VoxelDefinition voxelType, out VoxelChunk chunk, out int voxelIndex, Color32 tintColor, float amount = 1f, int rotation = 0, bool refresh = true)
        {

            VoxelSingleSet (position, voxelType, out chunk, out voxelIndex, tintColor);

            // Apply rotation
            if (voxelType.allowsTextureRotation) {
                chunk.voxels [voxelIndex].SetTextureRotation (rotation);
            }

            // Add light source
            if (voxelType.lightIntensity > 0) {
                chunk.AddLightSource (voxelIndex, voxelType.lightIntensity);
            }

            // If it's water, add flood
            if (voxelType.spreads) {
                chunk.voxels [voxelIndex].SetWaterLevel ((Mathf.CeilToInt (amount * 15f)));
                AddWaterFlood (ref position, voxelType);
                if (refresh) {
                    ChunkRequestRefresh (chunk, true, true);
                }
            } else if (refresh) {
                UpdateChunkRR (chunk);
            }
            chunk.modified = true;

            // Triggers event
            if (OnChunkChanged != null) {
                OnChunkChanged (chunk);
            }
        }


        /// <summary>
        /// Internal method that puts a voxel in a given position. This method does not inform to neighbours. Only used by non-contiguous structures, like trees or vegetation.
        /// For terrain or large scale buildings, use VoxelPlaceFast.
        /// </summary>
        /// <param name="position">Position.</param>
        /// <param name="voxelType">Voxel type.</param>
        /// <param name="chunk">Chunk.</param>
        /// <param name="voxelIndex">Voxel index.</param>
        void VoxelSingleSet (Vector3 position, VoxelDefinition voxelType, out VoxelChunk chunk, out int voxelIndex, Color32 tintColor)
        {
            if (GetVoxelIndex (position, out chunk, out voxelIndex)) {
                if (OnVoxelBeforePlace != null) {
                    OnVoxelBeforePlace (position, chunk, voxelIndex, ref voxelType, ref tintColor);
                    if (voxelType == null)
                        return;
                }
                chunk.voxels [voxelIndex].Set (voxelType, tintColor);
            }
        }

        /// <summary>
        /// Internal method that clears any voxel in a given position. This method does not inform to neighbours.
        /// </summary>
        /// <param name="position">Position.</param>
        void VoxelSingleClear (Vector3 position, out VoxelChunk chunk, out int voxelIndex)
        {
            if (GetVoxelIndex (position, out chunk, out voxelIndex)) {
                VoxelDestroyFastSingle (chunk, voxelIndex);
            }
        }

        /// <summary>
        /// Converts a voxel into dynamic type
        /// </summary>
        /// <param name="chunk">Chunk.</param>
        /// <param name="voxelIndex">Voxel index.</param>
        GameObject VoxelSetDynamic (VoxelChunk chunk, int voxelIndex, bool addRigidbody, float duration)
        {
            if (chunk == null || chunk.voxels [voxelIndex].hasContent == 0)
                return null;

            VoxelDefinition vd = voxelDefinitions [chunk.voxels [voxelIndex].typeIndex];
            if (!vd.renderType.supportsDynamic ()) {
                return null;
            }

            VoxelPlaceholder placeholder = GetVoxelPlaceholder (chunk, voxelIndex, true);
            if (placeholder == null)
                return null;

            // Add rigid body
            if (addRigidbody) {
                Rigidbody rb = placeholder.GetComponent<Rigidbody> ();
                if (rb == null) {
                    placeholder.rb = placeholder.gameObject.AddComponent<Rigidbody> ();
                }
            }

            // If it's a custom model ignore it as it's already a gameobject
            if (placeholder.modelMeshFilter != null)
                return placeholder.gameObject;

            VoxelDefinition vdDyn = vd.dynamicDefinition;

            if (vdDyn == null) {
                // Setup and save voxel definition
                vd.dynamicDefinition = vdDyn = ScriptableObject.CreateInstance<VoxelDefinition> ();
                vdDyn.name = vd.name + " (Dynamic)";
                vdDyn.isDynamic = true;
                vdDyn.doNotSave = true;
                vdDyn.staticDefinition = vd;
                vdDyn.renderType = RenderType.Custom;
                vdDyn.textureIndexBottom = vd.textureIndexBottom;
                vdDyn.textureIndexSide = vd.textureIndexSide;
                vdDyn.textureIndexTop = vd.textureIndexTop;
                vdDyn.textureThumbnailTop = vd.textureThumbnailTop;
                vdDyn.textureThumbnailSide = vd.textureThumbnailSide;
                vdDyn.textureThumbnailBottom = vd.textureThumbnailBottom;
                vdDyn.textureSideIndices = vd.textureSideIndices;
                vdDyn.scale = vd.scale;
                vdDyn.offset = vd.offset;
                vdDyn.offsetRandomRange = vd.offsetRandomRange;
                vdDyn.rotation = vd.rotation;
                vdDyn.rotationRandomY = vd.rotationRandomY;
                vdDyn.sampleColor = vd.sampleColor;
                vdDyn.promotesTo = vd.promotesTo;
                vdDyn.playerDamageDelay = vd.playerDamageDelay;
                vdDyn.playerDamage = vd.playerDamage;
                vdDyn.pickupSound = vd.pickupSound;
                vdDyn.landingSound = vd.landingSound;
                vdDyn.jumpSound = vd.jumpSound;
                vdDyn.impactSound = vd.impactSound;
                vdDyn.footfalls = vd.footfalls;
                vdDyn.destructionSound = vd.destructionSound;
                vdDyn.canBeCollected = vd.canBeCollected;
                vdDyn.dropItem = GetItemDefinition (ItemCategory.Voxel, vd);
                vdDyn.buildSound = vd.buildSound;
                vdDyn.navigatable = true;
                vdDyn.windAnimation = false;
                vdDyn.textureSideIndices = null;
                vdDyn.model = MakeDynamicCubeFromVoxel (chunk, voxelIndex);
                AddVoxelTextures (vdDyn);
            }

            // Clear any vegetation on top if voxel can be moved (has a rigidbody) to avoid floating grass block
            if (placeholder.rb != null) {
                VoxelChunk topChunk;
                int topIndex;
                if (GetVoxelIndex (chunk, voxelIndex, 0, 1, 0, out topChunk, out topIndex)) {
                    if (topChunk.voxels [topIndex].hasContent == 1 && voxelDefinitions [topChunk.voxels [topIndex].typeIndex].renderType == RenderType.CutoutCross) {
                        VoxelDestroyFast (topChunk, topIndex);
                    }
                }
            }
            Color32 color = chunk.voxels [voxelIndex].color;
            int textureRotation = chunk.voxels [voxelIndex].GetTextureRotation ();
            chunk.voxels [voxelIndex].Set (vdDyn, color);
            chunk.voxels [voxelIndex].SetTextureRotation (textureRotation);

            if (duration > 0) {
                placeholder.SetCancelDynamic (duration);
            }

            // Refresh neighbours
            RebuildNeighbours (chunk, voxelIndex);

            return placeholder.gameObject;
        }

        /// <summary>
        /// Finds all voxels with "willCollapse" connected to a given position
        /// </summary>
        /// <returns>The crumbly voxel indices.</returns>
        /// <param name="position">Position.</param>
        /// <param name="voxelIndices">Results.</param>
        int GetCrumblyVoxelIndices (Vector3 position, int amount, List<VoxelIndex> voxelIndices = null)
        {
            if (voxelIndices == null) {
                voxelIndices = tempVoxelIndices;
            }
            voxelIndices.Clear ();
            tempVoxelPositions.Clear ();
            tempVoxelIndicesCount = 0;
            GetCrumblyVoxelRecursive (new Vector3 (position.x, position.y + 1f, position.z), position, amount, voxelIndices);
            return tempVoxelIndicesCount;
        }


        void GetCrumblyVoxelRecursive (Vector3 originalPosition, Vector3 position, int amount, List<VoxelIndex> voxelIndices)
        {
            if (tempVoxelIndicesCount >= amount)
                return;

            VoxelChunk chunk;
            int voxelIndex;
            int c = 0;
            bool dummy;
            VoxelIndex vi = new VoxelIndex ();
            for (int k = 0; k < collapsingOffsets.Length; k += 3) {
                Vector3 pos = position;
                pos.x += collapsingOffsets [k];
                pos.y += collapsingOffsets [k + 1];
                pos.z += collapsingOffsets [k + 2];
                float dx = pos.x > originalPosition.x ? pos.x - originalPosition.x : originalPosition.x - pos.x;
                float dz = pos.z > originalPosition.z ? pos.z - originalPosition.z : originalPosition.z - pos.z;
                if (dx > 8 || dz > 8)
                    continue;
                if (!tempVoxelPositions.TryGetValue (pos, out dummy)) {
                    tempVoxelPositions [pos] = true;
                    if (GetVoxelIndex (pos, out chunk, out voxelIndex, false) && chunk.voxels [voxelIndex].hasContent == 1 && chunk.voxels [voxelIndex].opaque >= 3 && voxelDefinitions [chunk.voxels [voxelIndex].typeIndex].willCollapse) {
                        vi.chunk = chunk;
                        vi.voxelIndex = voxelIndex;
                        vi.position = pos;
                        voxelIndices.Add (vi);
                        tempVoxelIndicesCount++;
                        c++;
                        if (tempVoxelIndicesCount >= amount)
                            break;
                    }
                }
            }
            int lastCount = tempVoxelIndicesCount;
            for (int k = 1; k <= c; k++) {
                GetCrumblyVoxelRecursive (originalPosition, tempVoxelIndices [lastCount - k].position, amount, voxelIndices);
            }
        }

        /// <summary>
        /// Returns the default voxel prefab (usually a cube; the prefab is located in Defaults folder)
        /// </summary>
        /// <returns>The default voxel prefab.</returns>
        GameObject GetDefaultVoxelPrefab ()
        {
            if (defaultVoxelPrefab == null) {
                defaultVoxelPrefab = Resources.Load<GameObject> ("VoxelPlay/Defaults/DefaultModel/Cube");
            }
            return defaultVoxelPrefab;
        }


        void InitTempVertices ()
        {
            tempVertices = new List<Vector3> (36);
            tempNormals = new List<Vector3> (36);
            tempUVs = new List<Vector4> (36);
            tempIndices = new int [36];
            tempColors = new List<Color32> (36);

        }

        /// <summary>
        /// Creates a gameobject with geometry and materials based on the triangle renderer but lmited to one voxel
        /// </summary>
        /// <param name="chunk">Chunk.</param>
        /// <param name="voxelIndex">Voxel index.</param>
        GameObject MakeDynamicCubeFromVoxel (VoxelChunk chunk, int voxelIndex)
        {
            VoxelDefinition type = voxelDefinitions [chunk.voxels [voxelIndex].typeIndex];
            Color32 tintColor = chunk.voxels [voxelIndex].color;

            Mesh mesh = null;
            if (type.dynamicMeshes == null) {
                type.dynamicMeshes = new Dictionary<Color, Mesh> ();
            } else {
                type.dynamicMeshes.TryGetValue (tintColor, out mesh);
            }

            if (mesh == null) {
                // Create cube mesh procedurally
                tempVertices.Clear ();
                tempNormals.Clear ();
                tempUVs.Clear ();
                tempColors.Clear ();
                tempIndicesPos = 0;

                AddFace (MeshingThread.faceVerticesBack, MeshingThread.normalsBack, type.textureIndexSide, tintColor);
                AddFace (MeshingThread.faceVerticesForward, MeshingThread.normalsForward, type.textureIndexSide, tintColor);
                AddFace (MeshingThread.faceVerticesLeft, MeshingThread.normalsLeft, type.textureIndexSide, tintColor);
                AddFace (MeshingThread.faceVerticesRight, MeshingThread.normalsRight, type.textureIndexSide, tintColor);
                AddFace (MeshingThread.faceVerticesTop, MeshingThread.normalsUp, type.textureIndexTop, tintColor);
                AddFace (MeshingThread.faceVerticesBottom, MeshingThread.normalsDown, type.textureIndexBottom, tintColor);

                mesh = new Mesh ();
                mesh.SetVertices (tempVertices);
                mesh.SetUVs (0, tempUVs);
                mesh.SetNormals (tempNormals);
                if (enableTinting) {
                    mesh.SetColors (tempColors);
                }
                mesh.triangles = tempIndices;
                type.dynamicMeshes [tintColor] = mesh;
            }

            GameObject obj = new GameObject ("DynamicVoxelTemplate");
            obj.SetActive (false);

            MeshFilter mf = obj.AddComponent<MeshFilter> ();
            mf.mesh = mesh;

            MeshRenderer mr = obj.AddComponent<MeshRenderer> ();

            Material mat = null;
            if (type.overrideMaterial) {
                mat = type.overrideMaterialNonGeo;
            }
            if (mat == null) {
                mat = type.renderType == RenderType.Cutout ? matDynamicCutout : matDynamicOpaque;
            }
            mr.sharedMaterial = mat;

            BoxCollider boxCollider = obj.AddComponent<BoxCollider> ();
            boxCollider.size = new Vector3 (0.98f, 0.98f, 0.98f);

            obj.transform.SetParent (worldRoot, false);

            return obj;
        }

        void AddFace (Vector3 [] faceVertices, Vector3 [] normals, int textureIndex, Color32 tintColor)
        {
            int index = tempVertices.Count;
            tempVertices.AddRange (faceVertices);
            tempNormals.AddRange (normals);

            tempIndices [tempIndicesPos++] = index;
            tempIndices [tempIndicesPos++] = index + 1;
            tempIndices [tempIndicesPos++] = index + 3;
            tempIndices [tempIndicesPos++] = index + 3;
            tempIndices [tempIndicesPos++] = index + 2;
            tempIndices [tempIndicesPos++] = index + 0;

            Vector4 v4 = new Vector4 (0, 0, textureIndex, 15f);
            tempUVs.Add (v4);
            v4.y = 1f;
            tempUVs.Add (v4);
            v4.x = 1f;
            v4.y = 0;
            tempUVs.Add (v4);
            v4.y = 1f;
            tempUVs.Add (v4);
            if (enableTinting) {
                tempColors.Add (tintColor);
                tempColors.Add (tintColor);
                tempColors.Add (tintColor);
                tempColors.Add (tintColor);
            }
        }


    }



}
