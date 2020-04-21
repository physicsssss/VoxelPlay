using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay
{


    public delegate bool OnVoxelSpreadBeforeEvent (VoxelDefinition voxeltype, Vector3 newPosition, VoxelDefinition voxelOnNewPosition);
    public delegate void OnVoxelSpreadAfterEvent (VoxelDefinition voxeltype, Vector3 newPosition);

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        public event OnVoxelSpreadBeforeEvent OnVoxelBeforeSpread;
        public event OnVoxelSpreadAfterEvent OnVoxelAfterSpread;


        WaterFloodList waterFloodSources;
        float [] floodOffsets = {
            0, 1, 0,
            -1, 0, 0,
            1, 0, 0,
            0, 0, 1,
            0, 0, -1
        };
        float [] floodHorizontalOffsets = {
            0, -1,
            -1, 0,
            1, 0,
            0, 1
        };

        void InitWater ()
        {
            if (waterFloodSources == null) {
                waterFloodSources = new WaterFloodList ();
            } else {
                waterFloodSources.Clear ();
            }
        }


        /// <summary>
        /// Checks if there's water surrounding a given voxel. If it's, make that water expand.
        /// </summary>
        /// <param name="chunk">Chunk.</param>
        /// <param name="voxelIndex">Voxel index.</param>
        void MakeSurroundingWaterExpand (VoxelChunk chunk, int voxelIndex, Vector3 position, int lifeTime = 24)
        {
            VoxelChunk otherChunk;
            int otherVoxelIndex;

            Vector3 pos;
            for (int k = 0; k < floodOffsets.Length; k += 3) {
                pos.x = position.x + floodOffsets [k];
                pos.y = position.y + floodOffsets [k + 1];
                pos.z = position.z + floodOffsets [k + 2];
                if (GetVoxelIndex (pos, out otherChunk, out otherVoxelIndex, false)) {
                    int otherWaterLevel = otherChunk.voxels [otherVoxelIndex].GetWaterLevel ();
                    if (otherWaterLevel > 0) {
                        AddWaterFlood (ref pos, voxelDefinitions [otherChunk.voxels [otherVoxelIndex].typeIndex], lifeTime);
                    }
                }
            }

        }

        /// <summary>
        /// Manages flooding
        /// </summary>
        void UpdateWaterFlood ()
        {
            int last = waterFloodSources.last;
            int index = waterFloodSources.root;
            tempChunks.Clear ();

            while (index != -1) {
                if (Time.time >= waterFloodSources.nodes [index].spreadNext) {
                    waterFloodSources.SetNextSpreadTime (index);
                    waterFloodSources.nodes [index].lifeTime--;
                    if (waterFloodSources.nodes [index].lifeTime <= 0) {
                        index = waterFloodSources.RemoveAt (index);
                        continue;
                    }
                    TryWaterSpread (waterFloodSources.nodes [index].position, index);
                    if (index == last)
                        break;
                }
                index = waterFloodSources.nodes [index].next;
            }

            int chunksToRefreshCount = tempChunks.Count;
            for (int k = 0; k < chunksToRefreshCount; k++) {
                VoxelChunk chunk = tempChunks [k];
                RefreshNineChunksMeshes (chunk);

                // Triggers event
                if (OnChunkChanged != null) {
                    OnChunkChanged (chunk);
                }
            }
        }


        void RefreshNineChunksMeshes (VoxelChunk chunk)
        {
            Vector3 position = chunk.position;
            int chunkX, chunkY, chunkZ;
            FastMath.FloorToInt (position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out chunkX, out chunkY, out chunkZ);

            VoxelChunk neighbour;
            for (int y = -1; y <= 1; y++) {
                for (int z = -1; z <= 1; z++) {
                    for (int x = -1; x <= 1; x++) {
                        if (y == 0 && x == 0 && z == 0)
                            continue;
                        GetChunkFast (chunkX + x, chunkY + y, chunkZ + z, out neighbour);
                        if (neighbour != null && neighbour != chunk) {
                            ChunkRequestRefresh (neighbour, false, true);
                        }
                    }
                }
            }
            ChunkRequestRefresh (chunk, false, true);
        }

        /// <summary>
        /// Updates water voxel at a given position. 
        /// </summary>
        /// <returns>Returns the affected chunk and voxel index</returns>
        void WaterUpdateLevelFast (VoxelChunk chunk, int voxelIndex, int waterLevel, VoxelDefinition vd)
        {
            if (waterLevel > 15)
                waterLevel = 15;
            if (chunk.voxels [voxelIndex].GetWaterLevel () == waterLevel)
                return;
            if (waterLevel == 0) {
                chunk.voxels [voxelIndex].Clear (chunk.voxels [voxelIndex].light);
            } else {
                if (chunk.voxels [voxelIndex].GetWaterLevel () == 0) {
                    chunk.voxels [voxelIndex].Set (vd);
                    if (vd.lightIntensity > 0) {
                        chunk.AddLightSource (voxelIndex, vd.lightIntensity);
                    }
                }
                chunk.voxels [voxelIndex].SetWaterLevel (waterLevel);
            }

            chunk.modified = true;

        }

        /// <summary>
        /// Check surrounding voxels - if one if empty add one water voxel and extend flood
        /// </summary>
        /// <returns>The water spread.</returns>
        /// <param name="waterPos">Water position.</param>
        void TryWaterSpread (Vector3 waterPos, int index)
        {

            VoxelChunk chunk;
            int voxelIndex;
            GetVoxelIndex (waterPos, out chunk, out voxelIndex, false);
            if (chunk == null || chunk.voxels [voxelIndex].hasContent != 1) {
                waterFloodSources.RemoveAt (index);
            }

            VoxelChunk otherChunk;
            int vIndex;
            int waterLevel = chunk.voxels [voxelIndex].GetWaterLevel ();
            if (waterLevel == 0) {
                waterFloodSources.RemoveAt (index);
                MakeSurroundingWaterExpand (chunk, voxelIndex, waterPos);
                return;
            }

            VoxelDefinition waterVoxel = waterFloodSources.nodes [index].waterVoxel;

            // Check first beneath, if empty water falls down
            Vector3 down = new Vector3 (waterPos.x, waterPos.y - 1f, waterPos.z);
            if (GetVoxelIndex (down, out otherChunk, out vIndex)) {
                VoxelDefinition vdOther = voxelDefinitions [otherChunk.voxels [vIndex].typeIndex];
                bool canFillWithWater = otherChunk.voxels [vIndex].hasContent != 1 || vdOther.renderType == RenderType.CutoutCross || vdOther.renderType == RenderType.Water;
                if (canFillWithWater) {
                    int otherWaterLevel = otherChunk.voxels [vIndex].GetWaterLevel ();
                    if (otherWaterLevel < 15) {
                        if (OnVoxelBeforeSpread != null && !OnVoxelBeforeSpread (chunk.voxels [voxelIndex].type, down, otherChunk.voxels [vIndex].type)) {
                            return;
                        }
                        otherWaterLevel++;
                        WaterUpdateLevelFast (otherChunk, vIndex, otherWaterLevel, waterVoxel);
                        waterLevel--;
                        WaterUpdateLevelFast (chunk, voxelIndex, waterLevel, waterVoxel);
                        if (waterLevel == 0) {
                            waterFloodSources.RemoveAt (index);
                            MakeSurroundingWaterExpand (chunk, voxelIndex, waterPos);
                        }
                        AddWaterFlood (ref down, waterVoxel);
                        if (!tempChunks.Contains (chunk))
                            tempChunks.Add (chunk);
                        if (OnVoxelAfterSpread != null) {
                            OnVoxelAfterSpread (chunk.voxels [voxelIndex].type, down);
                        }
                        return;
                    }
                }
            }

            // If not, try to flood horizontally
            int leveling = 0;
            Vector3 otherPos = waterPos;
            int prevWaterLevel = waterLevel;
            for (int f = 0; f < floodHorizontalOffsets.Length; f += 2) {
                otherPos.z = waterPos.z + floodHorizontalOffsets [f + 1];
                otherPos.x = waterPos.x + floodHorizontalOffsets [f];
                if (GetVoxelIndex (otherPos, out otherChunk, out vIndex, false)) {
                    VoxelDefinition vdOther = voxelDefinitions [otherChunk.voxels [vIndex].typeIndex];
                    if (otherChunk.voxels [vIndex].hasContent != 1 || vdOther.renderType == RenderType.CutoutCross || vdOther.renderType == RenderType.Water) {
                        int otherWaterLevel = otherChunk.voxels [vIndex].GetWaterLevel ();
                        if (otherWaterLevel < waterLevel) {
                            leveling = 1;
                        }
                        if (otherWaterLevel < waterLevel - 1) {
                            if (waterVoxel.drains && otherWaterLevel < waterLevel - 1) {
                                waterLevel--;
                            }
                            if (waterLevel > 1) {
                                if (OnVoxelBeforeSpread != null && !OnVoxelBeforeSpread (chunk.voxels [voxelIndex].type, otherPos, otherChunk.voxels [vIndex].type)) {
                                    continue;
                                }
                                otherWaterLevel++;
                                AddWaterFlood (ref otherPos, waterVoxel, waterFloodSources.nodes [index].lifeTime - 1);
                                WaterUpdateLevelFast (otherChunk, vIndex, otherWaterLevel, waterVoxel);
                                if (!tempChunks.Contains (otherChunk)) {
                                    tempChunks.Add (otherChunk);
                                }
                                if (OnVoxelAfterSpread != null) {
                                    OnVoxelAfterSpread (chunk.voxels [voxelIndex].type, otherPos);
                                }
                            }
                        }
                    }
                }
            }

            for (int f = 0; f < floodHorizontalOffsets.Length; f += 2) {
                otherPos.z = waterPos.z + floodHorizontalOffsets [f + 1];
                otherPos.x = waterPos.x + floodHorizontalOffsets [f];
                if (GetVoxelIndex (otherPos, out otherChunk, out vIndex, false)) {
                    VoxelDefinition vdOther = voxelDefinitions [otherChunk.voxels [vIndex].typeIndex];
                    int otherWaterLevel = otherChunk.voxels [vIndex].GetWaterLevel ();
                    if (otherWaterLevel > waterLevel) {
                        vdOther = voxelDefinitions [otherChunk.voxels [voxelIndex].typeIndex];
                        if (waterVoxel.drains) {
                            otherWaterLevel--;
                            WaterUpdateLevelFast (otherChunk, vIndex, otherWaterLevel, vdOther);
                            if (!tempChunks.Contains (otherChunk)) {
                                tempChunks.Add (otherChunk);
                            }
                        }
                        AddWaterFlood (ref otherPos, vdOther, waterFloodSources.nodes [index].lifeTime - 1);
                        if (otherWaterLevel > waterLevel + leveling) {
                            if (OnVoxelBeforeSpread != null && !OnVoxelBeforeSpread (otherChunk.voxels [voxelIndex].type, otherPos, chunk.voxels [voxelIndex].type)) {
                                continue;
                            }
                            waterLevel++;
                            if (OnVoxelAfterSpread != null) {
                                OnVoxelAfterSpread (otherChunk.voxels [voxelIndex].type, otherPos);
                            }
                        }
                    }
                }
            }


            if (prevWaterLevel != waterLevel) {
                WaterUpdateLevelFast (chunk, voxelIndex, waterLevel, waterVoxel);
                if (!tempChunks.Contains (chunk)) {
                    tempChunks.Add (chunk);
                }
            }
            if (waterLevel == 0 || prevWaterLevel == waterLevel) {
                waterFloodSources.RemoveAt (index);
                MakeSurroundingWaterExpand (chunk, voxelIndex, waterPos, waterFloodSources.nodes [index].lifeTime - 1);
            }
        }


    }



}
