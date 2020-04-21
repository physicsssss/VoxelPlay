using UnityEngine;


namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        int ComputeTorchLightMap (VoxelChunk chunk)
        {
            Voxel [] voxels = chunk.voxels;
            if (voxels == null)
                return 0;

            tempLightmapIndex = -1;
            int lightmapSignature = 0;

            // Get top chunk but only if it has been rendered at least once.
            // means that the top chunk is not available which in the case of surface will switch to the heuristic of heightmap (see else below)
            VoxelChunk topChunk = chunk.top;
            bool topChunkIsAccesible = (object)topChunk != null && topChunk.isPopulated;
            if (topChunkIsAccesible) {
                int top = (CHUNK_SIZE - 1) * ONE_Y_ROW;
                for (int bottom = 0; bottom < CHUNK_SIZE * CHUNK_SIZE; bottom++, top++) {
                    byte torchLight = topChunk.voxels [bottom].torchLight;
                    tempLightmapIndex += torchLight;
                    if (voxels [top].opaque < FULL_OPAQUE) {
                        if (torchLight > voxels [top].torchLight) {
                            voxels [top].torchLight = torchLight;
                            tempLightmapPos [++tempLightmapIndex] = top;
                        }
                    }
                }
            }

            // Check bottom chunk
            VoxelChunk bottomChunk = chunk.bottom;
            bool bottomChunkIsAccesible = (object)bottomChunk != null && bottomChunk.isPopulated;
            if (bottomChunkIsAccesible) {
                int top = (CHUNK_SIZE - 1) * ONE_Y_ROW;
                for (int bottom = 0; bottom < CHUNK_SIZE * CHUNK_SIZE; bottom++, top++) {
                    byte torchLight = bottomChunk.voxels [top].torchLight;
                    tempLightmapIndex += torchLight;
                    if (voxels [bottom].opaque < FULL_OPAQUE) {
                        if (torchLight > voxels [bottom].torchLight) {
                            voxels [bottom].torchLight = torchLight;
                            tempLightmapPos [++tempLightmapIndex] = bottom;
                        }
                    }
                }
            }


            // Check left face
            VoxelChunk leftChunk = chunk.left;
            bool leftChunkIsAccesible = (object)leftChunk != null && leftChunk.isPopulated;
            if (leftChunkIsAccesible) {
                int left = (CHUNK_SIZE - 1) * ONE_Y_ROW + (CHUNK_SIZE - 1) * ONE_Z_ROW;
                int right = left + CHUNK_SIZE - 1;
                for (int z = 0; z < CHUNK_SIZE * CHUNK_SIZE; z++, right -= ONE_Z_ROW, left -= ONE_Z_ROW) {
                    byte torchLight = leftChunk.voxels [right].torchLight;
                    lightmapSignature += torchLight;
                    if (voxels [left].opaque < FULL_OPAQUE) {
                        if (torchLight > voxels [left].torchLight) {
                            voxels [left].torchLight = torchLight;
                            tempLightmapPos [++tempLightmapIndex] = left;
                        }
                    }
                }
            }


            // Check right face
            VoxelChunk rightChunk = chunk.right;
            bool rightChunkIsAccesible = (object)rightChunk != null && rightChunk.isPopulated;
            if (rightChunkIsAccesible) {
                int left = (CHUNK_SIZE - 1) * ONE_Y_ROW + (CHUNK_SIZE - 1) * ONE_Z_ROW;
                int right = left + (CHUNK_SIZE - 1);
                for (int z = 0; z < CHUNK_SIZE * CHUNK_SIZE; z++, right -= ONE_Z_ROW, left -= ONE_Z_ROW) {
                    byte torchLight = rightChunk.voxels [left].torchLight;
                    lightmapSignature += torchLight;
                    if (voxels [right].opaque < FULL_OPAQUE) {
                        if (torchLight > voxels [right].torchLight) {
                            voxels [right].torchLight = torchLight;
                            tempLightmapPos [++tempLightmapIndex] = right;
                        }
                    }
                }
            }

            // Check forward face
            VoxelChunk forwardChunk = chunk.forward;
            bool forwardChunkIsAccesible = (object)forwardChunk != null && forwardChunk.isPopulated;
            if (forwardChunkIsAccesible) {
                for (int y = (CHUNK_SIZE - 1); y >= 0; y--) {
                    int back = y * ONE_Y_ROW;
                    int forward = back + (CHUNK_SIZE - 1) * ONE_Z_ROW;
                    for (int x = 0; x <= (CHUNK_SIZE - 1); x++, back++, forward++) {
                        byte torchLight = forwardChunk.voxels [back].torchLight;
                        lightmapSignature += torchLight;
                        if (voxels [forward].opaque < FULL_OPAQUE) {
                            if (torchLight > voxels [forward].torchLight) {
                                voxels [forward].torchLight = torchLight;
                                tempLightmapPos [++tempLightmapIndex] = forward;
                            }
                        }
                    }
                }
            }

            // Check back face
            VoxelChunk backChunk = chunk.back;
            bool backChunkIsAccesible = (object)backChunk != null && backChunk.isPopulated;
            if (backChunkIsAccesible) {
                for (int y = (CHUNK_SIZE - 1); y >= 0; y--) {
                    int back = y * ONE_Y_ROW;
                    int forward = back + (CHUNK_SIZE - 1) * ONE_Z_ROW;
                    for (int x = 0; x <= (CHUNK_SIZE - 1); x++, back++, forward++) {
                        byte torchLight = backChunk.voxels [forward].torchLight;
                        lightmapSignature += torchLight;
                        if (voxels [back].opaque < FULL_OPAQUE) {
                            if (torchLight > voxels [back].torchLight) {
                                voxels [back].torchLight = torchLight;
                                tempLightmapPos [++tempLightmapIndex] = back;
                            }
                        }
                    }
                }
            }

            // Add torchLightsources
            if (chunk.lightSources != null) {
                int count = chunk.lightSources.Count;
                for (int k = 0; k < count; k++) {
                    LightSource ls = chunk.lightSources [k];
                    // add opaque so emitting opaque voxels do not stop light from spreading themselves
                    voxels [ls.voxelIndex].torchLight = (byte)(ls.lightIntensity + voxels [ls.voxelIndex].opaque);
                    tempLightmapPos [++tempLightmapIndex] = ls.voxelIndex;
                }
            }

            int index = 0;

            while (index <= tempLightmapIndex) {

                // Pop element
                int voxelIndex = tempLightmapPos [index];
                byte torchLight = voxels [voxelIndex].torchLight;
                index++;

                if (torchLight <= voxels [voxelIndex].opaque)
                    continue;
                int reducedTorchLight = torchLight - voxels [voxelIndex].opaque;

                // Spread torchLight

                // down
                reducedTorchLight -= world.lightTorchAttenuation;
                if (reducedTorchLight <= 0)
                    continue;

                int py = voxelIndex / ONE_Y_ROW;

                // we check if current voxel is at the edge of any direction. If it's on the edge of the chunk, it checks if current torchLight differs from the torchLight used in the last mesh generation. If it's different we force a refresh of the neighbour chunk since
                // its mesh can have old AO terms. Also, if the neighbour voxel/chunk's torchLight is less than this voxels', we spread the torchLight over the neighbour.
                // if we're not at the edge of the direction, then simply decrement torchLight, add a pointer to the queue and advance in that direction.
                if (py == 0) {
                    if (bottomChunkIsAccesible) {
                        int up = voxelIndex + (CHUNK_SIZE - 1) * ONE_Y_ROW;
                        if (bottomChunk.voxels [up].torchLight < reducedTorchLight && bottomChunk.voxels [up].opaque < FULL_OPAQUE) {
                            bottomChunkIsAccesible = false;
                            ChunkRequestRefresh (bottomChunk, false, false);
                        }
                    }
                } else {
                    int down = voxelIndex - ONE_Y_ROW;
                    if (voxels [down].torchLight < reducedTorchLight && voxels [down].opaque < FULL_OPAQUE) {
                        voxels [down].torchLight = (byte)reducedTorchLight;
                        lightmapSignature += reducedTorchLight;
                        tempLightmapPos [--index] = down;
                    }
                }

                int pz = (voxelIndex - py * ONE_Y_ROW) / ONE_Z_ROW;
                int px = voxelIndex & (CHUNK_SIZE - 1);

                // backwards
                if (pz == 0) {
                    if (backChunkIsAccesible) {
                        int forward = voxelIndex + (CHUNK_SIZE - 1) * ONE_Z_ROW;
                        if (backChunk.voxels [forward].torchLight < reducedTorchLight && backChunk.voxels [forward].opaque < FULL_OPAQUE) {
                            backChunkIsAccesible = false;
                            ChunkRequestRefresh (backChunk, false, false);
                        }
                    }
                } else {
                    int back = voxelIndex - ONE_Z_ROW;
                    if (voxels [back].torchLight < reducedTorchLight && voxels [back].opaque < FULL_OPAQUE) {
                        voxels [back].torchLight = (byte)reducedTorchLight;
                        lightmapSignature += reducedTorchLight;
                        tempLightmapPos [++tempLightmapIndex] = back;
                    }
                }

                // forward
                if (pz == (CHUNK_SIZE - 1)) {
                    if (forwardChunkIsAccesible) {
                        int back = voxelIndex - (CHUNK_SIZE - 1) * ONE_Z_ROW;
                        if (forwardChunk.voxels [back].torchLight < reducedTorchLight && forwardChunk.voxels [back].opaque < FULL_OPAQUE) {
                            forwardChunkIsAccesible = false;
                            ChunkRequestRefresh (forwardChunk, false, false);
                        }
                    }
                } else {
                    int forward = voxelIndex + ONE_Z_ROW;
                    if (voxels [forward].torchLight < reducedTorchLight && voxels [forward].opaque < FULL_OPAQUE) {
                        voxels [forward].torchLight = (byte)reducedTorchLight;
                        lightmapSignature += reducedTorchLight;
                        tempLightmapPos [++tempLightmapIndex] = forward;
                    }
                }

                // left
                if (px == 0) {
                    if (leftChunkIsAccesible) {
                        int right = voxelIndex + (CHUNK_SIZE - 1);
                        if (leftChunk.voxels [right].torchLight < reducedTorchLight && leftChunk.voxels [right].opaque < FULL_OPAQUE) {
                            leftChunkIsAccesible = false;
                            ChunkRequestRefresh (leftChunk, false, false);
                        }
                    }
                } else {
                    int left = voxelIndex - 1;
                    if (voxels [left].torchLight < reducedTorchLight && voxels [left].opaque < FULL_OPAQUE) {
                        voxels [left].torchLight = (byte)reducedTorchLight;
                        lightmapSignature += reducedTorchLight;
                        tempLightmapPos [++tempLightmapIndex] = left;
                    }
                }

                // right
                if (px == (CHUNK_SIZE - 1)) {
                    if (rightChunkIsAccesible) {
                        int left = voxelIndex - (CHUNK_SIZE - 1);
                        if (rightChunk.voxels [left].torchLight < reducedTorchLight && rightChunk.voxels [left].opaque < FULL_OPAQUE) {
                            rightChunkIsAccesible = false;
                            ChunkRequestRefresh (rightChunk, false, false);
                        }
                    }
                } else {
                    int right = voxelIndex + 1;
                    if (voxels [right].torchLight < reducedTorchLight && voxels [right].opaque < FULL_OPAQUE) {
                        voxels [right].torchLight = (byte)reducedTorchLight;
                        lightmapSignature += reducedTorchLight;
                        tempLightmapPos [++tempLightmapIndex] = right;
                    }
                }

                // up
                if (py == (CHUNK_SIZE - 1)) {
                    if (topChunkIsAccesible) {
                        int down = voxelIndex - (CHUNK_SIZE - 1) * ONE_Y_ROW;
                        if (topChunk.voxels [down].torchLight < reducedTorchLight && topChunk.voxels [down].opaque < FULL_OPAQUE) {
                            topChunkIsAccesible = false;
                            ChunkRequestRefresh (topChunk, false, false);
                        }
                    }
                } else {
                    int up = voxelIndex + ONE_Y_ROW;
                    if (voxels [up].torchLight < reducedTorchLight && voxels [up].opaque < FULL_OPAQUE) {
                        voxels [up].torchLight = (byte)reducedTorchLight;
                        lightmapSignature += reducedTorchLight;
                        tempLightmapPos [++tempLightmapIndex] = up;
                    }
                }
            }

            // Reduce opaque on emitting opaque voxels
            if (chunk.lightSources != null) {
                int count = chunk.lightSources.Count;
                for (int k = 0; k < count; k++) {
                    LightSource ls = chunk.lightSources [k];
                    voxels [ls.voxelIndex].torchLight -= voxels [ls.voxelIndex].opaque;
                }
            }

            return lightmapSignature;
        }

    }



}
