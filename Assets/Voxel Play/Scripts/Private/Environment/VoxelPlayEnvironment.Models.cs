using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        IEnumerator ModelPlaceWithDuration (Vector3 position, ModelDefinition model, float buildDuration, int rotationDegrees = 0, float colorBrightness = 1f, bool fitTerrain = false, VoxelPlayModelBuildEvent callback = null)
        {

            if (OnModelBuildStart != null) {
                OnModelBuildStart (model, position);
            }
            int currentIndex = 0;
            int len = model.bits.Length - 1;
            float startTime = Time.time;
            float t = 0;
            WaitForEndOfFrame w = new WaitForEndOfFrame ();
            Bounds bounds = new Bounds (position, Misc.vector3one);
            while (t < 1f) {
                t = (Time.time - startTime) / buildDuration;
                if (t >= 1f) {
                    t = 1f;
                }
                int lastIndex = (int)(len * t);
                if (lastIndex >= currentIndex) {
                    ModelPlace (position, model, ref bounds, rotationDegrees, colorBrightness, fitTerrain, null, currentIndex, lastIndex);
                    currentIndex = lastIndex + 1;
                }
                yield return w;
            }

            ModelPlaceTorches (position, model, rotationDegrees);

            if (callback != null) {
                callback (model, position);
            }
            if (OnModelBuildEnd != null) {
                OnModelBuildEnd (model, position);
            }
        }


        void ModelPlace (Vector3 position, ModelDefinition model, ref Bounds bounds, int rotationDegrees = 0, float colorBrightness = 1f, bool fitTerrain = false, List<VoxelIndex> indices = null, int indexStart = -1, int indexEnd = -1, bool useUnpopulatedChunks = false, bool refreshChunks = true) {

            if (model == null)
                return;
            if (indexStart < 0) {
                indexStart = 0;
            }
            if (indexEnd < 0) {
                indexEnd = model.bits.Length - 1;
            }

            Vector3 pos;
            int modelOneYRow = model.sizeZ * model.sizeX;
            int modelOneZRow = model.sizeX;

            if (rotationDegrees == 360) {
                switch (WorldRand.Range (0, 4)) {
                case 1:
                    rotationDegrees = 90;
                    break;
                case 2:
                    rotationDegrees = 180;
                    break;
                case 3:
                    rotationDegrees = 270;
                    break;
                }
            }

            int halfSizeX = model.sizeX / 2;
            int halfSizeZ = model.sizeZ / 2;
            Vector3 zeroPos = Quaternion.Euler (0, rotationDegrees, 0) * new Vector3 (-halfSizeX, 0, -halfSizeZ);

            // ensure all voxel definitions are present
            //bool reloadTextures = false;
            for (int b = indexStart; b <= indexEnd; b++) {
                VoxelDefinition vd = model.bits [b].voxelDefinition;
                if (vd != null && vd.index == 0) {
                    AddVoxelDefinition (vd);
                    //reloadTextures = true;
                }
            }
            //if (reloadTextures) { // TODO: RML (AddVoxelDefinition already marks reload world textures flag)
            //    LoadWorldTextures();
            //}

            bool indicesProvided = indices != null;
            if (indicesProvided && indexStart < 0 && indexEnd < 0) {
                indices.Clear ();
            }
            VoxelIndex index = new VoxelIndex ();
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            for (int b = indexStart; b <= indexEnd; b++) {
                int bitIndex = model.bits [b].voxelIndex;
                int py = bitIndex / modelOneYRow;
                int remy = bitIndex - py * modelOneYRow;
                int pz = remy / modelOneZRow;
                int px = remy - pz * modelOneZRow;

                float wx = zeroPos.x, wz = zeroPos.z;
                switch (rotationDegrees) {
                case 90:
                    wx += pz;
                    wz -= px;
                    break;
                case 180:
                    wx -= px;
                    wz -= pz;
                    break;
                case 270:
                    wx -= pz;
                    wz += px;
                    break;
                default:
                    wx += px;
                    wz += pz;
                    break;
                }

                pos.x = position.x + model.offsetX + wx;
                pos.y = position.y + model.offsetY + py;
                pos.z = position.z + model.offsetZ + wz;

                VoxelChunk chunk;
                int voxelIndex;
                if (useUnpopulatedChunks) {
                    chunk = GetChunkUnpopulated (pos);
                }
                if (GetVoxelIndex (pos, out chunk, out voxelIndex)) {
                    bool emptyVoxel = model.bits [b].isEmpty;
                    if (emptyVoxel) {
                        chunk.voxels [voxelIndex] = Voxel.Hole;
                    } else {
                        Color32 color = model.bits [b].finalColor;
                        if (colorBrightness != 1f) {
                            color.r = (byte)(color.r * colorBrightness);
                            color.g = (byte)(color.g * colorBrightness);
                            color.b = (byte)(color.b * colorBrightness);
                        }
                        VoxelDefinition vd = model.bits [b].voxelDefinition ?? defaultVoxel;
                        chunk.voxels [voxelIndex].Set (vd, color);
                        float rotation = (model.bits [b].rotation + 360 + rotationDegrees) % 360;
                        if (rotation != 0) {
                            chunk.voxels [voxelIndex].SetTextureRotation (Voxel.GetTextureRotationFromDegrees (rotation));
                        }

                        // Add index
                        if (indicesProvided) {
                            index.chunk = chunk;
                            index.voxelIndex = voxelIndex;
                            index.position = pos;
                            indices.Add (index);
                        }
                        if (pos.x < min.x) {
                            min.x = pos.x;
                        }
                        if (pos.y < min.y) {
                            min.y = pos.y;
                        }
                        if (pos.z < min.z) {
                            min.z = pos.z;
                        }
                        if (pos.x > max.x) {
                            max.x = pos.x;
                        }
                        if (pos.y > max.y) {
                            max.y = pos.y;
                        }
                        if (pos.z > max.z) {
                            max.z = pos.z;
                        }

                        if (fitTerrain) {
                            // Fill beneath row 1
                            if (py == 0) {
                                Vector3 under = pos;
                                under.y -= 1;
                                for (int k = 0; k < 100; k++, under.y--) {
                                    VoxelChunk lowChunk;
                                    int vindex;
                                    GetVoxelIndex (under, out lowChunk, out vindex, false);
                                    if (lowChunk != null && lowChunk.voxels [vindex].opaque < FULL_OPAQUE) {
                                        lowChunk.voxels [vindex].Set (vd, color);
                                        lowChunk.modified = true;
                                        if (!lowChunk.inqueue && !useUnpopulatedChunks) {
                                            ChunkRequestRefresh (lowChunk, true, true);
                                        }
                                    } else {
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // Prevent tree population
                    chunk.allowTrees = false;
                    chunk.modified = true;

                    if (!chunk.inqueue && !useUnpopulatedChunks) {
                        chunk.MarkAsInconclusive ();
						if (refreshChunks) {
	                        ChunkRequestRefresh (chunk, true, true);
						}
                    }
                }
            }

            FastVector.Floor (ref min);
            FastVector.Ceiling (ref max);
            bounds.center = (max + min) * 0.5f;
            bounds.size = max - min;
        }


        void ModelPlaceTorches (Vector3 position, ModelDefinition model, int rotationDegrees = 0)
        {

            if (model == null || model.torches == null)
                return;
            FastVector.Floor (ref position);
            Vector3 pos;
            int modelOneYRow = model.sizeZ * model.sizeX;
            int modelOneZRow = model.sizeX;
            int halfSizeX = model.sizeX / 2;
            int halfSizeZ = model.sizeZ / 2;

            if (rotationDegrees == 360) {
                switch (UnityEngine.Random.Range (0, 4)) {
                case 0:
                    rotationDegrees = 90;
                    break;
                case 1:
                    rotationDegrees = 180;
                    break;
                case 2:
                    rotationDegrees = 270;
                    break;
                }
            }

            // ensure all voxel definitions are present
            int tmp;
            for (int b = 0; b < model.torches.Length; b++) {
                int bitIndex = model.torches [b].voxelIndex;
                int py = bitIndex / modelOneYRow;
                int remy = bitIndex - py * modelOneYRow;
                int pz = remy / modelOneZRow;
                int px = remy - pz * modelOneZRow;
                Vector3 normal = model.torches [b].normal;
                switch (rotationDegrees) {
                case 90:
                    tmp = px;
                    px = halfSizeZ - pz;
                    pz = halfSizeX - tmp;
                    tmp = (int)normal.x;
                    normal.x = normal.z;
                    normal.z = -tmp;
                    break;
                case 180:
                    px = halfSizeX - px;
                    pz = halfSizeZ - pz;
                    normal.x = -normal.x;
                    normal.z = -normal.z;
                    break;
                case 270:
                    tmp = px;
                    px = pz - halfSizeZ;
                    pz = tmp - halfSizeX;
                    tmp = (int)normal.x;
                    normal.x = normal.z;
                    normal.z = tmp;
                    break;
                default:
                    px -= halfSizeX;
                    pz -= halfSizeZ;
                    break;
                }

                pos.x = position.x + model.offsetX + px;
                pos.y = position.y + model.offsetY + py;
                pos.z = position.z + model.offsetZ + pz;
                pos -= normal;

                VoxelChunk chunk;
                int voxelIndex;
                VoxelHitInfo hitInfo = new VoxelHitInfo ();
                if (GetVoxelIndex (pos, out chunk, out voxelIndex)) {
                    hitInfo.chunk = chunk;
                    hitInfo.voxelIndex = voxelIndex;
                    hitInfo.normal = normal;
                    hitInfo.voxelCenter = pos + Misc.vector3half;
                    TorchAttach (hitInfo, model.torches [b].itemDefinition, false);
                }
            }
        }

    }



}
