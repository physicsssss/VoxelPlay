using UnityEngine;


namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        int ComputeSunLightMap (VoxelChunk chunk)
        {
            Voxel [] voxels = chunk.voxels;
            if (voxels == null)
                return 0;

            bool isAboveSurface = chunk.isAboveSurface;
            tempLightmapIndex = -1;
            int lightmapSignature = 0;

            // Get top chunk but only if it has been rendered at least once.
            // means that the top chunk is not available which in the case of surface will switch to the heuristic of heightmap (see else below)
            VoxelChunk topChunk = chunk.top;
            bool topChunkIsAccesible = (object)topChunk != null && topChunk.isPopulated;
            if (topChunkIsAccesible) {
                int top = (CHUNK_SIZE - 1) * ONE_Y_ROW;
                for (int bottom = 0; bottom < CHUNK_SIZE * CHUNK_SIZE; bottom++, top++) {
                    byte light = topChunk.voxels [bottom].light;
                    lightmapSignature += light;
                    if (voxels [top].opaque < FULL_OPAQUE) {
                        if (light > voxels [top].light) {
                            voxels [top].light = light;
                            tempLightmapPos [++tempLightmapIndex] = top;
                        }
                    }
                }
            } else if (isAboveSurface) {
                for (int top = (CHUNK_SIZE - 1) * ONE_Y_ROW; top < CHUNK_VOXEL_COUNT; top++) {
                    if (voxels [top].opaque < FULL_OPAQUE) {
                        if (voxels [top].light != FULL_LIGHT) {
                            voxels [top].light = FULL_LIGHT;
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
                    byte light = bottomChunk.voxels [top].light;
                    lightmapSignature += light;
                    if (voxels [bottom].opaque < FULL_OPAQUE) {
                        if (light > voxels [bottom].light) {
                            voxels [bottom].light = light;
                            tempLightmapPos [++tempLightmapIndex] = bottom;
                        }
                    }
                }
            } else if (isAboveSurface && (waterLevel == 0 || chunk.position.y - CHUNK_HALF_SIZE > waterLevel)) {
                for (int z = 0; z <= CHUNK_SIZE - 1; z++) {
                    for (int bottom = 0; bottom < CHUNK_SIZE * CHUNK_SIZE; bottom++) {
                        if (voxels [bottom].opaque < FULL_OPAQUE) {
                            if (voxels [bottom].light != FULL_LIGHT) {
                                voxels [bottom].light = FULL_LIGHT;
                                tempLightmapPos [++tempLightmapIndex] = bottom;
                            }
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
                    byte light = leftChunk.voxels [right].light;
                    lightmapSignature += light;
                    if (voxels [left].opaque < FULL_OPAQUE) {
                        if (light > voxels [left].light) {
                            voxels [left].light = light;
                            tempLightmapPos [++tempLightmapIndex] = left;
                        }
                    }
                }
            } else if (isAboveSurface) {
                int left = (CHUNK_SIZE - 1) * ONE_Y_ROW + (CHUNK_SIZE - 1) * ONE_Z_ROW;
                for (int z = 0; z < CHUNK_SIZE * CHUNK_SIZE; z++, left -= ONE_Z_ROW) {
                    if (voxels [left].opaque < FULL_OPAQUE) {
                        if (voxels [left].light != FULL_LIGHT) {
                            voxels [left].light = FULL_LIGHT;
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
                    byte light = rightChunk.voxels [left].light;
                    lightmapSignature += light;
                    if (voxels [right].opaque < FULL_OPAQUE) {
                        if (light > voxels [right].light) {
                            voxels [right].light = light;
                            tempLightmapPos [++tempLightmapIndex] = right;
                        }
                    }
                }
            } else if (isAboveSurface) {
                int right = (CHUNK_SIZE - 1) * ONE_Y_ROW + (CHUNK_SIZE - 1) * ONE_Z_ROW + (CHUNK_SIZE - 1);
                for (int z = 0; z < CHUNK_SIZE * CHUNK_SIZE; z++, right -= ONE_Z_ROW) {
                    if (voxels [right].opaque < FULL_OPAQUE) {
                        if (voxels [right].light != FULL_LIGHT) {
                            voxels [right].light = FULL_LIGHT;
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
                        byte light = forwardChunk.voxels [back].light;
                        lightmapSignature += light;
                        if (voxels [forward].opaque < FULL_OPAQUE) {
                            if (light > voxels [forward].light) {
                                voxels [forward].light = light;
                                tempLightmapPos [++tempLightmapIndex] = forward;
                            }
                        }
                    }
                }
            } else if (isAboveSurface) {
                for (int y = (CHUNK_SIZE - 1); y >= 0; y--) {
                    int forward = y * ONE_Y_ROW + (CHUNK_SIZE - 1) * ONE_Z_ROW;
                    for (int x = 0; x <= (CHUNK_SIZE - 1); x++, forward++) {
                        if (voxels [forward].opaque < FULL_OPAQUE) {
                            if (voxels [forward].light != FULL_LIGHT) {
                                voxels [forward].light = FULL_LIGHT;
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
                        byte light = backChunk.voxels [forward].light;
                        lightmapSignature += light;
                        if (voxels [back].opaque < FULL_OPAQUE) {
                            if (light > voxels [back].light) {
                                voxels [back].light = light;
                                tempLightmapPos [++tempLightmapIndex] = back;
                            }
                        }
                    }
                }
            } else if (isAboveSurface) {
                for (int y = (CHUNK_SIZE - 1); y >= 0; y--) {
                    int back = y * ONE_Y_ROW;
                    for (int x = 0; x <= (CHUNK_SIZE - 1); x++, back++) {
                        if (voxels [back].opaque < FULL_OPAQUE) {
                            if (voxels [back].light != FULL_LIGHT) {
                                voxels [back].light = FULL_LIGHT;
                                tempLightmapPos [++tempLightmapIndex] = back;
                            }
                        }
                    }
                }
            }

            int index = 0;
            int notIsAboveSurfaceReduction, isAboveSurfaceReduction;
            if (isAboveSurface) {
                isAboveSurfaceReduction = world.lightSunAttenuation;
                notIsAboveSurfaceReduction = 0;
            } else {
                isAboveSurfaceReduction = 0;
                notIsAboveSurfaceReduction = world.lightSunAttenuation;
            }

            while (index <= tempLightmapIndex) {

                // Pop element
                int voxelIndex = tempLightmapPos [index];
                byte light = voxels [voxelIndex].light;
                index++;

                if (light <= voxels [voxelIndex].opaque)
                    continue;
                int reducedLight = light - voxels [voxelIndex].opaque;

                // Spread light

                // down
                reducedLight -= notIsAboveSurfaceReduction;
                if (reducedLight <= 0)
                    continue;

                int py = voxelIndex / ONE_Y_ROW;

                // we check if current voxel is at the edge of any direction. If it's on the edge of the chunk, it checks if current light differs from the light used in the last mesh generation. If it's different we force a refresh of the neighbour chunk since
                // its mesh can have old AO terms. Also, if the neighbour voxel/chunk's light is less than this voxels', we spread the light over the neighbour.
                // if we're not at the edge of the direction, then simply decrement light, add a pointer to the queue and advance in that direction.
                if (py == 0) {
                    if (bottomChunkIsAccesible) {
                        if (chunk.lightmapIsClear || voxels [voxelIndex].light != voxels [voxelIndex].lightMesh) {
                            voxels [voxelIndex].lightMesh = voxels [voxelIndex].light;
                            bottomChunkIsAccesible = false;
                            ChunkRequestRefresh (bottomChunk, false, true);
                        } else {
                            int up = voxelIndex + (CHUNK_SIZE - 1) * ONE_Y_ROW;
                            if (bottomChunk.voxels [up].light < reducedLight && bottomChunk.voxels [up].opaque < FULL_OPAQUE) {
                                bottomChunkIsAccesible = false;
                                ChunkRequestRefresh (bottomChunk, false, false);
                            }
                        }
                    }
                } else {
                    int down = voxelIndex - ONE_Y_ROW;
                    if (voxels [down].light < reducedLight && voxels [down].opaque < FULL_OPAQUE) {
                        voxels [down].light = (byte)reducedLight;
                        lightmapSignature += reducedLight;
                        tempLightmapPos [--index] = down;
                    }
                }

                reducedLight -= isAboveSurfaceReduction;
                if (reducedLight <= 0)
                    continue;

                int pz = (voxelIndex - py * ONE_Y_ROW) / ONE_Z_ROW;
                int px = voxelIndex & (CHUNK_SIZE - 1);

                // backwards
                if (pz == 0) {
                    if (backChunkIsAccesible) {
                        if (chunk.lightmapIsClear || voxels [voxelIndex].light != voxels [voxelIndex].lightMesh) {
                            voxels [voxelIndex].lightMesh = voxels [voxelIndex].light;
                            backChunkIsAccesible = false;
                            ChunkRequestRefresh (backChunk, false, true);
                        } else {
                            int forward = voxelIndex + (CHUNK_SIZE - 1) * ONE_Z_ROW;
                            if (backChunk.voxels [forward].light < reducedLight && backChunk.voxels [forward].opaque < FULL_OPAQUE) {
                                backChunkIsAccesible = false;
                                ChunkRequestRefresh (backChunk, false, false);
                            }
                        }
                    }
                } else {
                    int back = voxelIndex - ONE_Z_ROW;
                    if (voxels [back].light < reducedLight && voxels [back].opaque < FULL_OPAQUE) {
                        voxels [back].light = (byte)reducedLight;
                        lightmapSignature += reducedLight;
                        tempLightmapPos [++tempLightmapIndex] = back;
                    }
                }

                // forward
                if (pz == (CHUNK_SIZE - 1)) {
                    if (forwardChunkIsAccesible) {
                        if (chunk.lightmapIsClear || voxels [voxelIndex].light != voxels [voxelIndex].lightMesh) {
                            voxels [voxelIndex].lightMesh = voxels [voxelIndex].light;
                            forwardChunkIsAccesible = false;
                            ChunkRequestRefresh (forwardChunk, false, true);
                        } else {
                            int back = voxelIndex - (CHUNK_SIZE - 1) * ONE_Z_ROW;
                            if (forwardChunk.voxels [back].light < reducedLight && forwardChunk.voxels [back].opaque < FULL_OPAQUE) {
                                forwardChunkIsAccesible = false;
                                ChunkRequestRefresh (forwardChunk, false, false);
                            }
                        }
                    }
                } else {
                    int forward = voxelIndex + ONE_Z_ROW;
                    if (voxels [forward].light < reducedLight && voxels [forward].opaque < FULL_OPAQUE) {
                        voxels [forward].light = (byte)reducedLight;
                        lightmapSignature += reducedLight;
                        tempLightmapPos [++tempLightmapIndex] = forward;
                    }
                }

                // left
                if (px == 0) {
                    if (leftChunkIsAccesible) {
                        if (chunk.lightmapIsClear || voxels [voxelIndex].light != voxels [voxelIndex].lightMesh) {
                            voxels [voxelIndex].lightMesh = voxels [voxelIndex].light;
                            leftChunkIsAccesible = false;
                            ChunkRequestRefresh (leftChunk, false, true);
                        } else {
                            int right = voxelIndex + (CHUNK_SIZE - 1);
                            if (leftChunk.voxels [right].light < reducedLight && leftChunk.voxels [right].opaque < FULL_OPAQUE) {
                                leftChunkIsAccesible = false;
                                ChunkRequestRefresh (leftChunk, false, false);
                            }
                        }
                    }
                } else {
                    int left = voxelIndex - 1;
                    if (voxels [left].light < reducedLight && voxels [left].opaque < FULL_OPAQUE) {
                        voxels [left].light = (byte)reducedLight;
                        lightmapSignature += reducedLight;
                        tempLightmapPos [++tempLightmapIndex] = left;
                    }
                }

                // right
                if (px == (CHUNK_SIZE - 1)) {
                    if (rightChunkIsAccesible) {
                        if (chunk.lightmapIsClear || voxels [voxelIndex].light != voxels [voxelIndex].lightMesh) {
                            voxels [voxelIndex].lightMesh = voxels [voxelIndex].light;
                            rightChunkIsAccesible = false;
                            ChunkRequestRefresh (rightChunk, false, true);
                        } else {
                            int left = voxelIndex - (CHUNK_SIZE - 1);
                            if (rightChunk.voxels [left].light < reducedLight && rightChunk.voxels [left].opaque < FULL_OPAQUE) {
                                rightChunkIsAccesible = false;
                                ChunkRequestRefresh (rightChunk, false, false);
                            }
                        }
                    }
                } else {
                    int right = voxelIndex + 1;
                    if (voxels [right].light < reducedLight && voxels [right].opaque < FULL_OPAQUE) {
                        voxels [right].light = (byte)reducedLight;
                        lightmapSignature += reducedLight;
                        tempLightmapPos [++tempLightmapIndex] = right;
                    }
                }

                // up
                if (py == (CHUNK_SIZE - 1)) {
                    if (topChunkIsAccesible) {
                        if (chunk.lightmapIsClear || voxels [voxelIndex].light != voxels [voxelIndex].lightMesh) {
                            voxels [voxelIndex].lightMesh = voxels [voxelIndex].light;
                            topChunkIsAccesible = false;
                            ChunkRequestRefresh (topChunk, false, true);
                        } else {
                            int down = voxelIndex - (CHUNK_SIZE - 1) * ONE_Y_ROW;
                            if (topChunk.voxels [down].light < reducedLight && topChunk.voxels [down].opaque < FULL_OPAQUE) {
                                topChunkIsAccesible = false;
                                ChunkRequestRefresh (topChunk, false, false);
                            }
                        }
                    }
                } else {
                    int up = voxelIndex + ONE_Y_ROW;
                    if (voxels [up].light < reducedLight && voxels [up].opaque < FULL_OPAQUE) {
                        voxels [up].light = (byte)reducedLight;
                        lightmapSignature += reducedLight;
                        tempLightmapPos [++tempLightmapIndex] = up;
                    }
                }
            }
            return lightmapSignature;
        }

    }



}
