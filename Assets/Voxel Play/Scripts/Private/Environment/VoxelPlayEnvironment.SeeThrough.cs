using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay
{

    public enum HideStyle
    {
        DefinedByVoxelDefinition = 0,
        FullyInvisible = 1
    }


    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        VoxelIndex [] alphaOccludedIndices;
        int alphaOccludedIndicesCount = 0;
        VoxelChunk [] occludedChunks;

        void InitSeeThrough ()
        {
            if (alphaOccludedIndices == null || alphaOccludedIndices.Length == 0) {
                alphaOccludedIndices = new VoxelIndex [256];
            }
            if (occludedChunks == null || occludedChunks.Length == 0) {
                occludedChunks = new VoxelChunk [20];
            }
        }


        int CreateSeeThroughVoxelDefinition (VoxelDefinition original)
        {
            VoxelDefinition clone = Instantiate<VoxelDefinition> (original);
            clone.name = original.name + " SeeThrough";
            clone.renderType = RenderType.Transp6tex;
            clone.index = 0;
            clone.doNotSave = true;
            clone.alpha = original.alpha;
            clone.hidden = true;
            clone.canBeCollected = false;
            clone.textureIndexTop = original.textureIndexTop;
            clone.textureIndexSide = original.textureIndexSide;
            clone.textureIndexBottom = original.textureIndexBottom;
            clone.textureSideIndices = original.textureSideIndices;
            clone.seeThroughMode = SeeThroughMode.Transparency;
            clone.colorVariation = original.colorVariation;
            clone.ignoresRayCast = true;
            clone.navigatable = original.navigatable;
            clone.opaque = original.opaque;
            clone.tintColor = original.tintColor;
            clone.windAnimation = original.windAnimation;
            clone.materialBufferIndex = INDICES_BUFFER_TRANSP;
            AppendVoxelDefinition (clone);
            return clone.index;
        }

        void SetArrays (bool visible)
        {
            lock (seeThroughLock) {
                VoxelSetHidden (alphaOccludedIndices, alphaOccludedIndicesCount, visible);
            }
        }

        void ManageSeeThrough ()
        {

            SetArrays (false);
            Camera cam = currentCamera;
            if (cam == null)
                return;
            Vector3 camPos = cam.transform.position;

            if (seeThroughTarget == null) {
                if (characterController != null) {
                    seeThroughTarget = characterController.gameObject;
                }
                if (seeThroughTarget == null)
                    return;
            }
            Vector3 targetPos = seeThroughTarget.transform.position;

            // Exclude any voxel above roof from rendering
            float radiusSqr = seeThroughRadius * seeThroughRadius;
            Vector3 cylinderAxis;
            float distToTarget = Vector3.Distance (targetPos, camPos);
            cylinderAxis.x = (camPos.x - targetPos.x) / distToTarget;
            cylinderAxis.y = (camPos.y - targetPos.y) / distToTarget;
            cylinderAxis.z = (camPos.z - targetPos.z) / distToTarget;
            Shader.SetGlobalVector ("_VPSeeThroughData", new Vector4 (-cylinderAxis.x, -cylinderAxis.y, -cylinderAxis.z, radiusSqr));

            alphaOccludedIndicesCount = 0;

            // Add surrounding chunks
            int chunkCount = LineCast (targetPos, camPos, occludedChunks);
            int flag = Time.frameCount;
            for (int k = 0; k < chunkCount; k++) {
                VoxelChunk chunk = occludedChunks [k];
                chunk.tempFlag = flag;
            }

            int lineChunks = chunkCount;
            for (int k = 0; k < lineChunks; k++) {
                VoxelChunk chunk = occludedChunks [k];
                VoxelChunk n = chunk.top;
                if (n != null && n.tempFlag != flag) {
                    if (chunkCount >= occludedChunks.Length) {
                        occludedChunks = occludedChunks.Extend ();
                    }
                    occludedChunks [chunkCount++] = n;
                    n.tempFlag = flag;
                }
                n = chunk.bottom;
                if (n != null && n.tempFlag != flag) {
                    if (chunkCount >= occludedChunks.Length) {
                        occludedChunks = occludedChunks.Extend ();
                    }
                    occludedChunks [chunkCount++] = n;
                    n.tempFlag = flag;
                }
                n = chunk.left;
                if (n != null && n.tempFlag != flag) {
                    if (chunkCount >= occludedChunks.Length) {
                        occludedChunks = occludedChunks.Extend ();
                    }
                    occludedChunks [chunkCount++] = n;
                    n.tempFlag = flag;
                }
                n = chunk.right;
                if (n != null && n.tempFlag != flag) {
                    if (chunkCount >= occludedChunks.Length) {
                        occludedChunks = occludedChunks.Extend ();
                    }
                    occludedChunks [chunkCount++] = n;
                    n.tempFlag = flag;
                }
                n = chunk.forward;
                if (n != null && n.tempFlag != flag) {
                    if (chunkCount >= occludedChunks.Length) {
                        occludedChunks = occludedChunks.Extend ();
                    }
                    occludedChunks [chunkCount++] = n;
                    n.tempFlag = flag;
                }
                n = chunk.back;
                if (n != null && n.tempFlag != flag) {
                    if (chunkCount >= occludedChunks.Length) {
                        occludedChunks = occludedChunks.Extend ();
                    }
                    occludedChunks [chunkCount++] = n;
                    n.tempFlag = flag;
                }
            }

            float minY = targetPos.y + seeThroughHeightOffset;
            Vector3 voxelPosition = Misc.vector3zero;
            for (int k = 0; k < chunkCount; k++) {
                VoxelChunk chunk = occludedChunks [k];
                Vector3 chunkPosBase = chunk.position;
                chunkPosBase.x = chunkPosBase.x - CHUNK_HALF_SIZE + 0.5f;
                chunkPosBase.y = chunkPosBase.y - CHUNK_HALF_SIZE + 0.5f;
                chunkPosBase.z = chunkPosBase.z - CHUNK_HALF_SIZE + 0.5f;
                for (int voxelIndex = 0; voxelIndex < chunk.voxels.Length; voxelIndex++) {
                    if (chunk.voxels [voxelIndex].hasContent == 1) {
                        int py = voxelIndex / ONE_Y_ROW;
                        voxelPosition.y = py + chunkPosBase.y;
                        if (voxelPosition.y < minY)
                            continue;
                        int pz = (voxelIndex - py * ONE_Y_ROW) / ONE_Z_ROW;
                        int px = voxelIndex & (CHUNK_SIZE - 1);
                        voxelPosition.x = px + chunkPosBase.x;
                        voxelPosition.z = pz + chunkPosBase.z;

                        // Check if it's inside the cylinder
                        Vector3 v = voxelPosition;
                        v.x -= targetPos.x;
                        v.y -= targetPos.y;
                        v.z -= targetPos.z;
                        // v is the voxel position from targetPos
                        // cylinderDist is the distance from targetPos to the projected vector v on the cylinder axis
                        float cylinderDist = v.x * cylinderAxis.x + v.y * cylinderAxis.y + v.z * cylinderAxis.z;
                        if (cylinderDist < 1 || cylinderDist > distToTarget)
                            continue;

                        v.x -= cylinderDist * cylinderAxis.x;
                        v.y -= cylinderDist * cylinderAxis.y;
                        v.z -= cylinderDist * cylinderAxis.z;
                        // this is the distance between the voxel position and the projected point calculated above = distance from the voxel to the axis of the cylinder
                        float orthDistanceSqr = v.x * v.x + v.y * v.y + v.z * v.z;

                        if (orthDistanceSqr < radiusSqr) {
                            if (alphaOccludedIndicesCount >= alphaOccludedIndices.Length) {
                                alphaOccludedIndices = alphaOccludedIndices.Extend ();
                            }
                            alphaOccludedIndices [alphaOccludedIndicesCount].chunk = chunk;
                            alphaOccludedIndices [alphaOccludedIndicesCount].voxelIndex = voxelIndex;
                            alphaOccludedIndicesCount++;
                        }
                    }
                }
            }
            SetArrays (true);
        }


        /// <summary>
        /// Toggles on/off hidden voxels.
        /// </summary>
        void ToggleHiddenVoxels (VoxelChunk chunk, bool visible)
        {

            if (chunk == null)
                return;
            FastHashSet<VoxelExtraData> data = chunk.voxelsExtraData;
            if (data == null)
                return;

            int count = data.Count;
            Voxel [] voxels = chunk.voxels;

            if (visible) {
                for (int k = 0; k < count; k++) {
                    int voxelIndex = data.entries [k].key;
                    if (voxelIndex >= 0 && data.entries [k].value.hidden) {
                        voxels [voxelIndex].typeIndex = data.entries [k].value.hiddenTypeIndex;
                        voxels [voxelIndex].opaque = data.entries [k].value.hiddenOpaque;
                        voxels [voxelIndex].light = data.entries [k].value.hiddenLight;
                    }
                }
            } else {
                for (int k = 0; k < count; k++) {
                    int voxelIndex = data.entries [k].key;
                    if (voxelIndex >= 0 && data.entries [k].value.hidden) {
                        ushort typeIndex = voxels [voxelIndex].typeIndex;
                        data.entries [k].value.hiddenTypeIndex = typeIndex;
                        data.entries [k].value.hiddenOpaque = voxels [voxelIndex].opaque;
                        data.entries [k].value.hiddenLight = voxels [voxelIndex].light;
                        if (data.entries [k].value.hiddenStyle == HideStyle.DefinedByVoxelDefinition) {
                            VoxelDefinition vd = voxelDefinitions [typeIndex];
                            switch (vd.seeThroughMode) {
                            case SeeThroughMode.FullyInvisible:
                                voxels [voxelIndex].typeIndex = 0; // null voxel whose renderType = Empty (but genereate colliders)
                                voxels [voxelIndex].opaque = 0;
                                break;
                            case SeeThroughMode.ReplaceVoxel:
                                voxels [voxelIndex].typeIndex = vd.seeThroughVoxel.index; // null voxel whose renderType = Empty (but genereate colliders)
                                voxels [voxelIndex].opaque = vd.seeThroughVoxel.opaque;
                                break;
                            case SeeThroughMode.Transparency:
                                voxels [voxelIndex].typeIndex = (ushort)vd.seeThroughVoxelTempTransp; // see-through voxel rendered with transparency
                                voxels [voxelIndex].opaque = 2;
                                break;
                            }
                        } else {
                            voxels [voxelIndex].typeIndex = 0; // null voxel whose renderType = Empty (but genereate colliders)
                            voxels [voxelIndex].opaque = 0;
                        }
                        voxels [voxelIndex].light = 15;
                    }
                }
            }
        }

        void VoxelSetHiddenOne (VoxelChunk chunk, int voxelIndex, bool hidden, HideStyle hiddenStyle)
        {

            if (chunk.voxels [voxelIndex].hasContent != 1)
                return;

            int typeIndex = chunk.voxels [voxelIndex].typeIndex;
            if (typeIndex < 0 || typeIndex >= voxelDefinitions.Length)
                return;
            if (voxelDefinitions [typeIndex].seeThroughMode == SeeThroughMode.NotSupported)
                return;

            if (chunk.voxelsExtraData == null) {
                if (!hidden)
                    return;
                chunk.voxelsExtraData = new FastHashSet<VoxelExtraData> ();
            }
            VoxelExtraData hiddenVoxel = new VoxelExtraData ();
            hiddenVoxel.hidden = hidden;
            hiddenVoxel.hiddenStyle = hiddenStyle;
            chunk.voxelsExtraData.Add (voxelIndex, hiddenVoxel, true);
        }



    }



}
