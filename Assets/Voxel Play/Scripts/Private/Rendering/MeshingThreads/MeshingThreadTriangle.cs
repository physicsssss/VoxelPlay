//#define USES_TINTING

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace VoxelPlay
{

    public class MeshingThreadTriangle : MeshingThread
    {

        const int V_ONE_Y_ROW = VoxelPlayEnvironment.CHUNK_SIZE_PLUS_2 * VoxelPlayEnvironment.CHUNK_SIZE_PLUS_2;
        const int V_ONE_Z_ROW = VoxelPlayEnvironment.CHUNK_SIZE_PLUS_2;

        // Chunk Creation helpers for non-geometry shaders
        float aoBase;
        VoxelPlayGreedyMesherLit greedyClouds, greedyOpaqueNoAO, greedyCutoutNoAO;

        public override void Init (int threadId, int poolSize, VoxelPlayEnvironment env)
        {
            base.Init (threadId, poolSize, env);

            greedyOpaqueNoAO = new VoxelPlayGreedyMesherLit ();
            greedyClouds = new VoxelPlayGreedyMesherLit ();
            greedyCutoutNoAO = new VoxelPlayGreedyMesherLit ();
        }


        [MethodImpl (256)] // equals to MethodImplOptions.AggressiveInlining
        float ComputeVertexLight (int voxelLight, int side1, int side2, int corner)
        {
            int light = (side1 | side2) == 0 ? voxelLight : voxelLight + side1 + side2 + corner;
            return ((light >> 2) & 0xFFC00) + (light & 0x1FF) * aoBase;
        }

        /// <summary>
        /// Generates chunk mesh. Also computes lightmap if needed.
        /// </summary>
        public override void GenerateMeshData ()
        {

            int jobIndex = meshJobMeshDataGenerationIndex;

            for (int j = 0; j < meshJobs [jobIndex].buffers.Length; j++) {
                if (meshJobs [jobIndex].buffers [j].indicesCount > 0) {
                    meshJobs [jobIndex].buffers [j].indices.Clear ();
                    meshJobs [jobIndex].buffers [j].indicesCount = 0;
                }
            }

            VoxelChunk chunk = meshJobs [jobIndex].chunk;
            tempChunkVertices = meshJobs [jobIndex].vertices;
            tempChunkUV0 = meshJobs [jobIndex].uv0;
            tempChunkColors32 = meshJobs [jobIndex].colors;
            tempChunkNormals = meshJobs [jobIndex].normals;
            meshColliderVertices = meshJobs [jobIndex].colliderVertices;
            meshColliderIndices = meshJobs [jobIndex].colliderIndices;
            navMeshVertices = meshJobs [jobIndex].navMeshVertices;
            navMeshIndices = meshJobs [jobIndex].navMeshIndices;
            FastList<ModelInVoxel> mivs = meshJobs [jobIndex].mivs;

            tempChunkVertices.Clear ();
            tempChunkUV0.Clear ();
            tempChunkColors32.Clear ();
            tempChunkNormals.Clear ();
            mivs.Clear ();

            if (enableColliders) {
                meshColliderIndices.Clear ();
                meshColliderVertices.Clear ();
                if (enableNavMesh) {
                    navMeshIndices.Clear ();
                    navMeshVertices.Clear ();
                }
            }
            Color32 tintColor = Misc.color32White;

            int chunkVoxelCount = 0;
            Vector3 pos = Misc.vector3zero;

            Voxel [] voxels = chunk.voxels;

            ModelInVoxel miv = new ModelInVoxel ();

            int voxelSignature = 1;
            int voxelIndex = 0;
            for (int y = 0; y < VoxelPlayEnvironment.CHUNK_SIZE; y++) {
                int vy = (y + 1) * VoxelPlayEnvironment.CHUNK_SIZE_PLUS_2 * VoxelPlayEnvironment.CHUNK_SIZE_PLUS_2;
                for (int z = 0; z < VoxelPlayEnvironment.CHUNK_SIZE; z++) {
                    int vyz = vy + (z + 1) * VoxelPlayEnvironment.CHUNK_SIZE_PLUS_2;
                    for (int x = 0; x < VoxelPlayEnvironment.CHUNK_SIZE; x++, voxelIndex++) {
                        voxels [voxelIndex].lightMesh = voxels [voxelIndex].light;
                        if (voxels [voxelIndex].hasContent != 1)
                            continue;

                        // If voxel is surrounded by material, don't render
                        int vxyz = vyz + x + 1;

                        int vindex = vxyz - 1;
                        Voxel [] chunk_middle_middle_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_middle_left = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz + 1;
                        Voxel [] chunk_middle_middle_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_middle_right = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz + V_ONE_Y_ROW;
                        Voxel [] chunk_top_middle_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_middle_middle = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz - V_ONE_Y_ROW;
                        Voxel [] chunk_bottom_middle_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_middle_middle = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz + V_ONE_Z_ROW;
                        Voxel [] chunk_middle_forward_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_forward_middle = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz - V_ONE_Z_ROW;
                        Voxel [] chunk_middle_back_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_back_middle = virtualChunk [vindex].voxelIndex;

                        // If voxel is surrounded by material, don't render
                        int v1b = chunk_middle_back_middle [middle_back_middle].opaque;
                        int v1f = chunk_middle_forward_middle [middle_forward_middle].opaque;
                        int v1u = chunk_top_middle_middle [top_middle_middle].opaque;
                        int v1d = chunk_bottom_middle_middle [bottom_middle_middle].opaque;
                        int v1l = chunk_middle_middle_left [middle_middle_left].opaque;
                        int v1r = chunk_middle_middle_right [middle_middle_right].opaque;
                        if (v1u + v1f + v1b + v1l + v1r + v1d == 90) // 90 = 15 * 6
                            continue;

                        // top
                        vindex = vxyz + V_ONE_Y_ROW + V_ONE_Z_ROW - 1;
                        Voxel [] chunk_top_forward_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_forward_left = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_top_forward_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_forward_middle = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_top_forward_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_forward_right = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz + V_ONE_Y_ROW - 1;
                        Voxel [] chunk_top_middle_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_middle_left = virtualChunk [vindex].voxelIndex;

                        vindex += 2;
                        Voxel [] chunk_top_middle_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_middle_right = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz + V_ONE_Y_ROW - V_ONE_Z_ROW - 1;
                        Voxel [] chunk_top_back_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_back_left = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_top_back_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_back_middle = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_top_back_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int top_back_right = virtualChunk [vindex].voxelIndex;

                        // middle
                        vindex = vxyz + V_ONE_Z_ROW - 1;
                        Voxel [] chunk_middle_forward_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_forward_left = virtualChunk [vindex].voxelIndex;

                        vindex += 2;
                        Voxel [] chunk_middle_forward_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_forward_right = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz - V_ONE_Z_ROW - 1;
                        Voxel [] chunk_middle_back_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_back_left = virtualChunk [vindex].voxelIndex;

                        vindex += 2;
                        Voxel [] chunk_middle_back_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int middle_back_right = virtualChunk [vindex].voxelIndex;

                        // bottom
                        vindex = vxyz - V_ONE_Y_ROW + V_ONE_Z_ROW - 1;
                        Voxel [] chunk_bottom_forward_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_forward_left = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_bottom_forward_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_forward_middle = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_bottom_forward_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_forward_right = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz - V_ONE_Y_ROW - 1;
                        Voxel [] chunk_bottom_middle_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_middle_left = virtualChunk [vindex].voxelIndex;

                        vindex += 2;
                        Voxel [] chunk_bottom_middle_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_middle_right = virtualChunk [vindex].voxelIndex;

                        vindex = vxyz - V_ONE_Y_ROW - V_ONE_Z_ROW - 1;
                        Voxel [] chunk_bottom_back_left = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_back_left = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_bottom_back_middle = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_back_middle = virtualChunk [vindex].voxelIndex;

                        vindex++;
                        Voxel [] chunk_bottom_back_right = chunk9 [virtualChunk [vindex].chunk9Index];
                        int bottom_back_right = virtualChunk [vindex].voxelIndex;

                        //TODO: Changes voxels positions
                        pos.x = x - VoxelPlayEnvironment.CHUNK_HALF_SIZE + 0.5f;
                        pos.y = y - VoxelPlayEnvironment.CHUNK_HALF_SIZE + 0.5f;
                        pos.z = z - VoxelPlayEnvironment.CHUNK_HALF_SIZE + 0.5f;

                        chunkVoxelCount++;
                        voxelSignature += voxelIndex;

                        VoxelDefinition type = env.voxelDefinitions [voxels [voxelIndex].typeIndex];
                        List<int> indices = meshJobs [jobIndex].buffers [type.materialBufferIndex].indices;

#if USES_TINTING
                        tintColor.r = voxels [voxelIndex].red;
                        tintColor.g = voxels [voxelIndex].green;
                        tintColor.b = voxels [voxelIndex].blue;
                        faceColors [0].r = faceColors [1].r = faceColors [2].r = faceColors [3].r = tintColor.r;
                        faceColors [0].g = faceColors [1].g = faceColors [2].g = faceColors [3].g = tintColor.g;
                        faceColors [0].b = faceColors [1].b = faceColors [2].b = faceColors [3].b = tintColor.b;
#endif

                        switch (type.renderType) {
                        case RenderType.Water: {
                                int occ;
                                int foam = 0;
                                const int noflow = (1 << 8); // vertical flow

                                // Get corners heights
                                //int light = voxels [voxelIndex].light << 13;
                                int light = (voxels [voxelIndex].light << 13) + (voxels[voxelIndex].torchLight << 17);
                                int flow = noflow;
                                int hf = chunk_middle_forward_middle [middle_forward_middle].GetWaterLevel ();
                                int hb = chunk_middle_back_middle [middle_back_middle].GetWaterLevel ();
                                int hr = chunk_middle_middle_right [middle_middle_right].GetWaterLevel ();
                                int hl = chunk_middle_middle_left [middle_middle_left].GetWaterLevel ();
                                int th = chunk_top_middle_middle [top_middle_middle].GetWaterLevel ();
                                int wh = voxels [voxelIndex].GetWaterLevel ();

                                int corner_height_fr, corner_height_br, corner_height_fl, corner_height_bl;
                                int hfr = 0, hbr = 0, hbl = 0, hfl = 0;
                                // If there's water on top, full size
                                if (th > 0) {
                                    corner_height_fr = corner_height_br = corner_height_fl = corner_height_bl = 15;
                                } else {
                                    hfr = corner_height_fr = chunk_middle_forward_right [middle_forward_right].GetWaterLevel ();
                                    hbr = corner_height_br = chunk_middle_back_right [middle_back_right].GetWaterLevel ();
                                    hbl = corner_height_bl = chunk_middle_back_left [middle_back_left].GetWaterLevel ();
                                    hfl = corner_height_fl = chunk_middle_forward_left [middle_forward_left].GetWaterLevel ();

                                    int tf = chunk_top_forward_middle [top_forward_middle].GetWaterLevel ();
                                    int tfr = chunk_top_forward_right [top_forward_right].GetWaterLevel ();
                                    int tr = chunk_top_middle_right [top_middle_right].GetWaterLevel ();
                                    int tbr = chunk_top_back_right [top_back_right].GetWaterLevel ();
                                    int tb = chunk_top_back_middle [top_back_middle].GetWaterLevel ();
                                    int tbl = chunk_top_back_left [top_back_left].GetWaterLevel ();
                                    int tl = chunk_top_middle_left [top_middle_left].GetWaterLevel ();
                                    int tfl = chunk_top_forward_left [top_forward_left].GetWaterLevel ();

                                    // forward right corner
                                    if (tf * hf + tfr * corner_height_fr + tr * hr > 0) {
                                        corner_height_fr = 15;
                                    } else {
                                        corner_height_fr = wh > corner_height_fr ? wh : corner_height_fr;
                                        if (hf > corner_height_fr)
                                            corner_height_fr = hf;
                                        if (hr > corner_height_fr)
                                            corner_height_fr = hr;
                                    }
                                    // bottom right corner
                                    if (tr * hr + tbr * corner_height_br + tb * hb > 0) {
                                        corner_height_br = 15;
                                    } else {
                                        corner_height_br = wh > corner_height_br ? wh : corner_height_br;
                                        if (hr > corner_height_br)
                                            corner_height_br = hr;
                                        if (hb > corner_height_br)
                                            corner_height_br = hb;
                                    }
                                    // bottom left corner
                                    if (tb * hb + tbl * corner_height_bl + tl * hl > 0) {
                                        corner_height_bl = 15;
                                    } else {
                                        corner_height_bl = wh > corner_height_bl ? wh : corner_height_bl;
                                        if (hb > corner_height_bl)
                                            corner_height_bl = hb;
                                        if (hl > corner_height_bl)
                                            corner_height_bl = hl;
                                    }
                                    // forward left corner
                                    if (tl * hl + tfl * corner_height_fl + tf * hf > 0) {
                                        corner_height_fl = 15;
                                    } else {
                                        corner_height_fl = wh > corner_height_fl ? wh : corner_height_fl;
                                        if (hl > corner_height_fl)
                                            corner_height_fl = hl;
                                        if (hf > corner_height_fl)
                                            corner_height_fl = hf;
                                    }

                                    // flow
                                    int fx = corner_height_fr + corner_height_br - corner_height_fl - corner_height_bl;
                                    if (fx < 0)
                                        flow = 2 << 10;
                                    else if (fx == 0)
                                        flow = 1 << 10;
                                    else
                                        flow = 0;

                                    int fz = corner_height_fl + corner_height_fr - corner_height_bl - corner_height_br;
                                    if (fz > 0)
                                        flow += 2 << 8;
                                    else if (fz == 0)
                                        flow += 1 << 8;
                                }
                                pos.y -= 0.5f;

                                // back face
                                occ = chunk_middle_back_middle [middle_back_middle].hasContent;
                                if (occ == 1) {
                                    // 0 means that face is visible
                                    if (hb == 0) {
                                        foam = 1;
                                    }
                                } else {
                                    AddFaceWater (faceVerticesBack, normalsBack, pos, indices, type.textureIndexSide, light + noflow, 0, corner_height_bl, 0, corner_height_br);
#if USES_TINTING
                                        tempChunkColors32.AddRange (faceColors);
#endif
                                }

                                // front face
                                occ = chunk_middle_forward_middle [middle_forward_middle].hasContent;
                                if (occ == 1) {
                                    if (hf == 0) {
                                        foam |= 2;
                                    }
                                } else {
                                    AddFaceWater (faceVerticesForward, normalsForward, pos, indices, type.textureIndexSide, light + noflow, 0, corner_height_fr, 0, corner_height_fl);
#if USES_TINTING
                                        tempChunkColors32.AddRange (faceColors);
#endif
                                }

                                // left face
                                occ = chunk_middle_middle_left [middle_middle_left].hasContent;
                                if (occ == 1) {
                                    if (hl == 0) {
                                        foam |= 4;
                                    }
                                } else {
                                    AddFaceWater (faceVerticesLeft, normalsLeft, pos, indices, type.textureIndexSide, light + noflow, 0, corner_height_fl, 0, corner_height_bl);
#if USES_TINTING
                                        tempChunkColors32.AddRange (faceColors);
#endif
                                }

                                // right face
                                occ = chunk_middle_middle_right [middle_middle_right].hasContent;
                                if (occ == 1) {
                                    if (hr == 0) {
                                        foam |= 8;
                                    }
                                } else {
                                    AddFaceWater (faceVerticesRight, normalsRight, pos, indices, type.textureIndexSide, light + noflow, 0, corner_height_br, 0, corner_height_fr);
#if USES_TINTING
                                        tempChunkColors32.AddRange (faceColors);
#endif
                                }

                                // top (hide only if water level is full or voxel on top is water)
                                occ = chunk_top_middle_middle [top_middle_middle].hasContent;
                                if (occ != 1 || (wh < 15 && th == 0)) {
                                    if (type.showFoam) {
                                        // corner foam
                                        if (hbl == 0) {
                                            foam |= chunk_middle_back_left [middle_back_left].hasContent << 4;
                                        }
                                        if (hfl == 0) {
                                            foam |= chunk_middle_forward_left [middle_forward_left].hasContent << 5;
                                        }
                                        if (hfr == 0) {
                                            foam |= chunk_middle_forward_right [middle_forward_right].hasContent << 6;
                                        }
                                        if (hbr == 0) {
                                            foam |= chunk_middle_back_right [middle_back_right].hasContent << 7;
                                        }
                                    } else {
                                        foam = 0;
                                    }
                                    AddFaceWater (faceVerticesTop, normalsUp, pos, indices, type.textureIndexSide, light + foam + flow, corner_height_bl, corner_height_fl, corner_height_br, corner_height_fr);
                                    AddFaceWater (faceVerticesTopFlipped, normalsUp, pos, indices, type.textureIndexSide, light + foam + flow, corner_height_bl, corner_height_fl, corner_height_br, corner_height_fr);
#if USES_TINTING
                                        tempChunkColors32.AddRange (faceColors);
                                        tempChunkColors32.AddRange (faceColors);
#endif
                                }

                                // bottom
                                occ = chunk_bottom_middle_middle [bottom_middle_middle].hasContent;
                                if (occ != 1) {
                                    AddFaceWater (faceVerticesBottom, normalsDown, pos, indices, type.textureIndexSide, light + noflow, 0, 0, 0, 0);
#if USES_TINTING
                                        tempChunkColors32.AddRange (faceColors);
#endif
                                }
                            }
                            break;
                        case RenderType.CutoutCross: {
                                float random = WorldRand.GetValue (pos.x, pos.z);
                                float colorVariation = 1f + (random - 0.45f) * type.colorVariation;
                                float light = voxels [voxelIndex].GetPackedLight (colorVariation);
                                int texData = type.textureIndexSide;
                                if (type.windAnimation) {
                                    texData |= 65536;
                                }
                                AddFaceVegetation (faceVerticesCross1, pos, indices, texData, light);
                                AddFaceVegetation (faceVerticesCross2, pos, indices, texData, light);
#if USES_TINTING
                                    tempChunkColors32.AddRange (faceColors);
                                    tempChunkColors32.AddRange (faceColors);
#endif
                            }
                            break;
                        case RenderType.Cloud: {
                                // back face
                                if (v1b < FULL_OPAQUE) {
                                    greedyClouds.AddQuad (FaceDirection.Back, x, y, z, tintColor, 1f, type.textureIndexSide);
                                }
                                // forward face
                                if (v1f < FULL_OPAQUE) {
                                    greedyClouds.AddQuad (FaceDirection.Forward, x, y, z, tintColor, 1f, type.textureIndexSide);
                                }
                                // left face
                                if (v1l < FULL_OPAQUE) {
                                    greedyClouds.AddQuad (FaceDirection.Left, z, y, x, tintColor, 1f, type.textureIndexSide);
                                }
                                // right face
                                if (v1r < FULL_OPAQUE) {
                                    greedyClouds.AddQuad (FaceDirection.Right, z, y, x, tintColor, 1f, type.textureIndexSide);
                                }
                                // top face
                                if (v1u < FULL_OPAQUE) {
                                    greedyClouds.AddQuad (FaceDirection.Top, x, z, y, tintColor, 1f, type.textureIndexTop);
                                }
                                // bottom face
                                if (v1d < FULL_OPAQUE) {
                                    greedyClouds.AddQuad (FaceDirection.Bottom, x, z, y, tintColor, 1f, type.textureIndexBottom);
                                }
                            }
                            break;
                        case RenderType.OpaqueNoAO: {
                                int lu = chunk_top_middle_middle [top_middle_middle].packedLight;
                                int ll = chunk_middle_middle_left [middle_middle_left].packedLight;
                                int lf = chunk_middle_forward_middle [middle_forward_middle].packedLight;
                                int lr = chunk_middle_middle_right [middle_middle_right].packedLight;
                                int lb = chunk_middle_back_middle [middle_back_middle].packedLight;
                                int ld = chunk_bottom_middle_middle [bottom_middle_middle].packedLight;

                                bool addCollider = enableColliders && voxels [voxelIndex].opaque > 5;
                                int rotationIndex = voxels [voxelIndex].GetTextureRotation ();

                                // back face
                                if (v1b < FULL_OPAQUE) {
                                    int textureIndex = type.textureSideIndices [rotationIndex].back;
                                    if (type.customTextureProvider != null) {
                                        textureIndex = type.customTextureProvider (textureIndex,
                                        chunk_top_middle_left [top_middle_left].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_middle_right [top_middle_right].typeIndex,
                                        chunk_middle_middle_left [middle_middle_left].typeIndex, chunk_middle_middle_right [middle_middle_right].typeIndex,
                                        chunk_bottom_middle_left [bottom_middle_left].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_middle_right [bottom_middle_right].typeIndex);
                                    }
                                    greedyOpaqueNoAO.AddQuad (FaceDirection.Back, x, y, z, tintColor, lb, textureIndex);
                                    if (addCollider) {
                                        greedyCollider.AddQuad (FaceDirection.Back, x, y, z);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Back, x, y, z);
                                        }
                                    }
                                }
                                // forward face
                                if (v1f < FULL_OPAQUE) {
                                    int textureIndex = type.textureSideIndices [rotationIndex].forward;
                                    if (type.customTextureProvider != null) {
                                        textureIndex = type.customTextureProvider (textureIndex,
                                         chunk_top_middle_right [top_middle_right].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_middle_left [top_middle_left].typeIndex,
                                        chunk_middle_middle_right [middle_middle_right].typeIndex, chunk_middle_middle_left [middle_middle_left].typeIndex,
                                        chunk_bottom_middle_right [bottom_middle_right].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_middle_right [bottom_middle_left].typeIndex);
                                    }
                                    greedyOpaqueNoAO.AddQuad (FaceDirection.Forward, x, y, z, tintColor, lf, textureIndex);
                                    if (addCollider) {
                                        greedyCollider.AddQuad (FaceDirection.Forward, x, y, z);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Forward, x, y, z);
                                        }
                                    }
                                }
                                // left face
                                if (v1l < FULL_OPAQUE) {
                                    int textureIndex = type.textureSideIndices [rotationIndex].left;
                                    if (type.customTextureProvider != null) {
                                        textureIndex = type.customTextureProvider (textureIndex,
                                        chunk_top_forward_middle [top_forward_middle].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_back_middle [top_back_middle].typeIndex,
                                        chunk_middle_forward_left [middle_forward_left].typeIndex, chunk_middle_back_left [middle_back_left].typeIndex,
                                        chunk_bottom_forward_middle [bottom_forward_middle].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_back_middle [bottom_back_middle].typeIndex);
                                    }
                                    greedyOpaqueNoAO.AddQuad (FaceDirection.Left, z, y, x, tintColor, ll, textureIndex);
                                    if (addCollider) {
                                        greedyCollider.AddQuad (FaceDirection.Left, z, y, x);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Left, z, y, y);
                                        }
                                    }
                                }
                                // right face
                                if (v1r < FULL_OPAQUE) {
                                    int textureIndex = type.textureSideIndices [rotationIndex].right;
                                    if (type.customTextureProvider != null) {
                                        textureIndex = type.customTextureProvider (textureIndex,
                                        chunk_top_back_middle [top_back_middle].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_forward_middle [top_forward_middle].typeIndex,
                                        chunk_middle_back_left [middle_back_left].typeIndex, chunk_middle_forward_left [middle_forward_left].typeIndex,
                                        chunk_bottom_back_middle [bottom_back_middle].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_forward_middle [bottom_forward_middle].typeIndex);
                                    }
                                    greedyOpaqueNoAO.AddQuad (FaceDirection.Right, z, y, x, tintColor, lr, textureIndex);
                                    if (addCollider) {
                                        greedyCollider.AddQuad (FaceDirection.Right, z, y, x);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Right, z, y, y);
                                        }
                                    }
                                }
                                // top face
                                if (v1u < FULL_OPAQUE) {
                                    int textureIndex = type.textureIndexTop;
                                    if (type.customTextureProvider != null) {
                                        textureIndex = type.customTextureProvider (textureIndex,
                                        chunk_middle_forward_left [middle_forward_left].typeIndex, chunk_middle_forward_middle [middle_forward_middle].typeIndex, chunk_middle_forward_right [middle_forward_right].typeIndex,
                                        chunk_middle_middle_left [middle_middle_left].typeIndex, chunk_middle_middle_right [middle_middle_right].typeIndex,
                                        chunk_middle_back_left [middle_back_left].typeIndex, chunk_middle_back_middle [middle_back_middle].typeIndex, chunk_middle_back_right [middle_back_right].typeIndex);
                                    }
                                    greedyOpaqueNoAO.AddQuad (FaceDirection.Top, x, z, y, tintColor, lu, textureIndex);
                                    if (addCollider) {
                                        greedyCollider.AddQuad (FaceDirection.Top, x, z, y);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Top, x, z, y);
                                        }
                                    }
                                }
                                // bottom face
                                if (v1d < FULL_OPAQUE) {
                                    int textureIndex = type.textureIndexBottom;
                                    if (type.customTextureProvider != null) {
                                        textureIndex = type.customTextureProvider (textureIndex,
                                       chunk_middle_forward_right [middle_forward_right].typeIndex, chunk_middle_forward_middle [middle_forward_middle].typeIndex, chunk_middle_forward_left [middle_forward_left].typeIndex,
                                        chunk_middle_middle_right [middle_middle_right].typeIndex, chunk_middle_middle_left [middle_middle_left].typeIndex,
                                        chunk_middle_back_right [middle_back_right].typeIndex, chunk_middle_back_middle [middle_back_middle].typeIndex, chunk_middle_back_left [middle_back_left].typeIndex);
                                    }
                                    greedyOpaqueNoAO.AddQuad (FaceDirection.Bottom, x, z, y, tintColor, ld, textureIndex);
                                    if (addCollider) {
                                        greedyCollider.AddQuad (FaceDirection.Bottom, x, z, y);
                                    }
                                }
                            }
                            break;
                        case RenderType.Transp6tex: {
                                int rotationIndex = voxels [voxelIndex].GetTextureRotation ();
                                float light = voxels [voxelIndex].packedLight;
                                int typeIndex = voxels [voxelIndex].typeIndex;

                                // back face
                                if (v1b != FULL_OPAQUE && chunk_middle_back_middle [middle_back_middle].typeIndex != typeIndex) {
                                    int textureIndex = type.textureSideIndices [rotationIndex].back;
                                    if (type.customTextureProvider != null) {
                                        textureIndex = type.customTextureProvider (textureIndex,
                                        chunk_top_middle_left [top_middle_left].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_middle_right [top_middle_right].typeIndex,
                                        chunk_middle_middle_left [middle_middle_left].typeIndex, chunk_middle_middle_right [middle_middle_right].typeIndex,
                                        chunk_bottom_middle_left [bottom_middle_left].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_middle_right [bottom_middle_right].typeIndex);
                                    }

                                    AddFaceTransparent (faceVerticesBack, normalsBack, pos, indices, textureIndex, light, type.alpha);
#if USES_TINTING
                                        tempChunkColors32.AddRange (faceColors);
#endif
                                    if (enableColliders) {
                                        greedyCollider.AddQuad (FaceDirection.Back, x, y, z);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Back, x, y, z);
                                        }
                                    }
                                }

                                // forward
                                if (v1f != FULL_OPAQUE && chunk_middle_forward_middle [middle_forward_middle].typeIndex != typeIndex) {
                                    int textureIndex = type.textureSideIndices [rotationIndex].forward;
                                    if (type.customTextureProvider != null) {
                                        textureIndex = type.customTextureProvider (textureIndex,
                                         chunk_top_middle_right [top_middle_right].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_middle_left [top_middle_left].typeIndex,
                                        chunk_middle_middle_right [middle_middle_right].typeIndex, chunk_middle_middle_left [middle_middle_left].typeIndex,
                                        chunk_bottom_middle_right [bottom_middle_right].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_middle_left [bottom_middle_left].typeIndex);
                                    }
                                    AddFaceTransparent (faceVerticesForward, normalsForward, pos, indices, textureIndex, light, type.alpha);
#if USES_TINTING
                                        tempChunkColors32.AddRange (faceColors);
#endif
                                    if (enableColliders) {
                                        greedyCollider.AddQuad (FaceDirection.Forward, x, y, z);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Forward, x, y, z);
                                        }
                                    }
                                }

                                // top
                                if (v1u != FULL_OPAQUE && chunk_top_middle_middle [top_middle_middle].typeIndex != typeIndex) {
                                    int textureIndex = type.textureIndexTop;
                                    if (type.customTextureProvider != null) {
                                        textureIndex = type.customTextureProvider (textureIndex,
                                        chunk_middle_forward_left [middle_forward_left].typeIndex, chunk_middle_forward_middle [middle_forward_middle].typeIndex, chunk_middle_forward_right [middle_forward_right].typeIndex,
                                        chunk_middle_middle_left [middle_middle_left].typeIndex, chunk_middle_middle_right [middle_middle_right].typeIndex,
                                        chunk_middle_back_left [middle_back_left].typeIndex, chunk_middle_back_middle [middle_back_middle].typeIndex, chunk_middle_back_right [middle_back_right].typeIndex);
                                    }
                                    AddFaceTransparent (faceVerticesTop, normalsUp, pos, indices, textureIndex, light, type.alpha);
#if USES_TINTING
                                        tempChunkColors32.AddRange (faceColors);
#endif
                                    if (enableColliders) {
                                        greedyCollider.AddQuad (FaceDirection.Top, x, z, y);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Top, x, z, y);
                                        }
                                    }
                                }

                                // bottom
                                if (v1d != FULL_OPAQUE && chunk_bottom_middle_middle [bottom_middle_middle].typeIndex != typeIndex) {
                                    int textureIndex = type.textureIndexBottom;
                                    if (type.customTextureProvider != null) {
                                        textureIndex = type.customTextureProvider (textureIndex,
                                       chunk_middle_forward_right [middle_forward_right].typeIndex, chunk_middle_forward_middle [middle_forward_middle].typeIndex, chunk_middle_forward_left [middle_forward_left].typeIndex,
                                        chunk_middle_middle_right [middle_middle_right].typeIndex, chunk_middle_middle_left [middle_middle_left].typeIndex,
                                        chunk_middle_back_right [middle_back_right].typeIndex, chunk_middle_back_middle [middle_back_middle].typeIndex, chunk_middle_back_left [middle_back_left].typeIndex);
                                    }
                                    AddFaceTransparent (faceVerticesBottom, normalsDown, pos, indices, textureIndex, light, type.alpha);
#if USES_TINTING
                                        tempChunkColors32.AddRange (faceColors);
#endif
                                    if (enableColliders) {
                                        greedyCollider.AddQuad (FaceDirection.Bottom, x, z, y);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Bottom, x, z, y);
                                        }
                                    }
                                }

                                // left
                                if (v1l != FULL_OPAQUE && chunk_middle_middle_left [middle_middle_left].typeIndex != typeIndex) {
                                    int textureIndex = type.textureSideIndices [rotationIndex].left;
                                    if (type.customTextureProvider != null) {
                                        textureIndex = type.customTextureProvider (textureIndex,
                                        chunk_top_forward_middle [top_forward_middle].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_back_middle [top_back_middle].typeIndex,
                                        chunk_middle_forward_middle [middle_forward_middle].typeIndex, chunk_middle_back_middle [middle_back_middle].typeIndex,
                                        chunk_bottom_forward_middle [bottom_forward_middle].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_back_middle [bottom_back_middle].typeIndex);
                                    }
                                    AddFaceTransparent (faceVerticesLeft, normalsLeft, pos, indices, textureIndex, light, type.alpha);
#if USES_TINTING
                                        tempChunkColors32.AddRange (faceColors);
#endif
                                    if (enableColliders) {
                                        greedyCollider.AddQuad (FaceDirection.Left, z, y, x);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Left, z, y, y);
                                        }
                                    }
                                }
                                // right
                                if (v1r != FULL_OPAQUE && chunk_middle_middle_right [middle_middle_right].typeIndex != typeIndex) {
                                    int textureIndex = type.textureSideIndices [rotationIndex].right;
                                    if (type.customTextureProvider != null) {
                                        textureIndex = type.customTextureProvider (textureIndex,
                                        chunk_top_back_middle [top_back_middle].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_forward_middle [top_forward_middle].typeIndex,
                                        chunk_middle_back_middle [middle_back_middle].typeIndex, chunk_middle_forward_middle [middle_forward_middle].typeIndex,
                                        chunk_bottom_back_middle [bottom_back_middle].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_forward_middle [bottom_forward_middle].typeIndex);
                                    }
                                    AddFaceTransparent (faceVerticesRight, normalsRight, pos, indices, textureIndex, light, type.alpha);
#if USES_TINTING
                                        tempChunkColors32.AddRange (faceColors);
#endif
                                    if (enableColliders) {
                                        greedyCollider.AddQuad (FaceDirection.Right, z, y, x);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Right, z, y, y);
                                        }
                                    }
                                }
                            }
                            break;
                        default: //case RenderType.Custom:
                            miv.vd = type;
                            miv.voxelIndex = voxelIndex;
                            mivs.Add (miv);
                            break;
                        case RenderType.Empty: {
                                // back face
                                if (v1b < FULL_OPAQUE) {
                                    if (enableColliders) {
                                        greedyCollider.AddQuad (FaceDirection.Back, x, y, z);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Back, x, y, z);
                                        }
                                    }
                                }
                                // forward face
                                if (v1f < FULL_OPAQUE) {
                                    if (enableColliders) {
                                        greedyCollider.AddQuad (FaceDirection.Forward, x, y, z);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Forward, x, y, z);
                                        }
                                    }
                                }
                                // left face
                                if (v1l < FULL_OPAQUE) {
                                    if (enableColliders) {
                                        greedyCollider.AddQuad (FaceDirection.Left, z, y, x);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Left, z, y, y);
                                        }
                                    }
                                }
                                // right face
                                if (v1r < FULL_OPAQUE) {
                                    if (enableColliders) {
                                        greedyCollider.AddQuad (FaceDirection.Right, z, y, x);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Right, z, y, y);
                                        }
                                    }
                                }
                                // top face
                                if (v1u < FULL_OPAQUE) {
                                    if (enableColliders) {
                                        greedyCollider.AddQuad (FaceDirection.Top, x, z, y);
                                        if (enableNavMesh && type.navigatable) {
                                            greedyNavMesh.AddQuad (FaceDirection.Top, x, z, y);
                                        }
                                    }
                                }
                                // bottom face
                                if (v1d < FULL_OPAQUE) {
                                    if (enableColliders) {
                                        greedyCollider.AddQuad (FaceDirection.Bottom, x, z, y);
                                    }
                                }
                            }
                            break;
                        case RenderType.Opaque:
                        case RenderType.OpaqueAnimated:
                        case RenderType.Opaque6tex:
                        case RenderType.Cutout: // Opaque & Cutout
                            {

                                int lu = chunk_top_middle_middle [top_middle_middle].packedLight;
                                int ll = chunk_middle_middle_left [middle_middle_left].packedLight;
                                int lf = chunk_middle_forward_middle [middle_forward_middle].packedLight;
                                int lr = chunk_middle_middle_right [middle_middle_right].packedLight;
                                int lb = chunk_middle_back_middle [middle_back_middle].packedLight;
                                int ld = chunk_bottom_middle_middle [bottom_middle_middle].packedLight;

                                if (allowAO || type.overrideMaterial) {
                                    // Opaque / Cutout with AO

                                    int v2r = chunk_top_middle_right [top_middle_right].packedLight;
                                    int v2br = chunk_top_back_right [top_back_right].packedLight;
                                    int v2b = chunk_top_back_middle [top_back_middle].packedLight;
                                    int v2bl = chunk_top_back_left [top_back_left].packedLight;
                                    int v2l = chunk_top_middle_left [top_middle_left].packedLight;
                                    int v2fl = chunk_top_forward_left [top_forward_left].packedLight;
                                    int v2f = chunk_top_forward_middle [top_forward_middle].packedLight;
                                    int v2fr = chunk_top_forward_right [top_forward_right].packedLight;

                                    int v1fr = chunk_middle_forward_right [middle_forward_right].packedLight;
                                    int v1br = chunk_middle_back_right [middle_back_right].packedLight;
                                    int v1bl = chunk_middle_back_left [middle_back_left].packedLight;
                                    int v1fl = chunk_middle_forward_left [middle_forward_left].packedLight;

                                    int v0r = chunk_bottom_middle_right [bottom_middle_right].packedLight;
                                    int v0br = chunk_bottom_back_right [bottom_back_right].packedLight;
                                    int v0b = chunk_bottom_back_middle [bottom_back_middle].packedLight;
                                    int v0bl = chunk_bottom_back_left [bottom_back_left].packedLight;
                                    int v0l = chunk_bottom_middle_left [bottom_middle_left].packedLight;
                                    int v0fl = chunk_bottom_forward_left [bottom_forward_left].packedLight;
                                    int v0f = chunk_bottom_forward_middle [bottom_forward_middle].packedLight;
                                    int v0fr = chunk_bottom_forward_right [bottom_forward_right].packedLight;

                                    float l0, l1, l2, l3;

                                    bool denseTreeCheck = false;
                                    int extraData = 0;
                                    aoBase = 1f / 4f; // 4 light factors per vertex
                                    bool addCollider = enableColliders;
                                    if (type.renderType == RenderType.Cutout) {
                                        addCollider &= type.generateColliders;
                                        denseTreeCheck = denseTrees;
                                        float random = WorldRand.GetValue (pos);
                                        aoBase *= 1f + (random - 0.45f) * type.colorVariation;
                                        if (type.windAnimation) {
                                            extraData = 65536;
                                        }
                                    } else {
                                        if (type.renderType == RenderType.OpaqueAnimated) {
                                            extraData = (type.animationSpeed << 18) | ((type.animationTextures.Length + 1) << 14);
                                        }
                                    }
                                    int rotationIndex = voxels [voxelIndex].GetTextureRotation ();

                                    // back face
                                    if (chunk_middle_back_middle [middle_back_middle].opaque < FULL_OPAQUE || denseTreeCheck) {
                                        // Vertex 0 (from the cube representatino)
                                        l0 = ComputeVertexLight (lb, v0b, v1bl, v0bl);
                                        // Vertex 2
                                        l1 = ComputeVertexLight (lb, v2b, v1bl, v2bl);
                                        // Vertex 1
                                        l2 = ComputeVertexLight (lb, v0b, v1br, v0br);
                                        // Vertex 3
                                        l3 = ComputeVertexLight (lb, v2b, v1br, v2br);
                                        int textureIndex = type.textureSideIndices [rotationIndex].back;
                                        if (type.customTextureProvider != null) {
                                            textureIndex = type.customTextureProvider (textureIndex,
                                            chunk_top_middle_left [top_middle_left].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_middle_right [top_middle_right].typeIndex,
                                            chunk_middle_middle_left [middle_middle_left].typeIndex, chunk_middle_middle_right [middle_middle_right].typeIndex,
                                            chunk_bottom_middle_left [bottom_middle_left].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_middle_right [bottom_middle_right].typeIndex);
                                        }

                                        AddFaceWithAO (faceVerticesBack, normalsBack, pos, indices, textureIndex + extraData, l0, l1, l2, l3);
#if USES_TINTING
                                            tempChunkColors32.AddRange (faceColors);
#endif
                                        if (addCollider) {
                                            greedyCollider.AddQuad (FaceDirection.Back, x, y, z);
                                            if (enableNavMesh && type.navigatable) {
                                                greedyNavMesh.AddQuad (FaceDirection.Back, x, y, z);
                                            }
                                        }
                                    }
                                    // forward face
                                    if (chunk_middle_forward_middle [middle_forward_middle].opaque < FULL_OPAQUE || denseTreeCheck) {
                                        // Vertex 5
                                        l0 = ComputeVertexLight (lf, v0f, v1fr, v0fr);
                                        // Vertex 6
                                        l1 = ComputeVertexLight (lf, v2f, v1fr, v2fr);
                                        // Vertex 4
                                        l2 = ComputeVertexLight (lf, v0f, v1fl, v0fl);
                                        // Vertex 7
                                        l3 = ComputeVertexLight (lf, v2f, v1fl, v2fl);

                                        int textureIndex = type.textureSideIndices [rotationIndex].forward;
                                        if (type.customTextureProvider != null) {
                                            if (type.customTextureProvider != null) {
                                                textureIndex = type.customTextureProvider (textureIndex,
                                                 chunk_top_middle_right [top_middle_right].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_middle_left [top_middle_left].typeIndex,
                                                chunk_middle_middle_right [middle_middle_right].typeIndex, chunk_middle_middle_left [middle_middle_left].typeIndex,
                                                chunk_bottom_middle_right [bottom_middle_right].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_middle_right [bottom_middle_left].typeIndex);
                                            }
                                        }

                                        AddFaceWithAO (faceVerticesForward, normalsForward, pos, indices, textureIndex + extraData, l0, l1, l2, l3);
#if USES_TINTING
                                            tempChunkColors32.AddRange (faceColors);
#endif
                                        if (addCollider) {
                                            greedyCollider.AddQuad (FaceDirection.Forward, x, y, z);
                                            if (enableNavMesh && type.navigatable) {
                                                greedyNavMesh.AddQuad (FaceDirection.Forward, x, y, z);
                                            }
                                        }
                                    }
                                    // left face
                                    if (chunk_middle_middle_left [middle_middle_left].opaque < FULL_OPAQUE || denseTreeCheck) {
                                        // Vertex 4
                                        l0 = ComputeVertexLight (ll, v0l, v1fl, v0fl);
                                        // Vertex 7
                                        l1 = ComputeVertexLight (ll, v2l, v1fl, v2fl);
                                        // Vertex 0
                                        l2 = ComputeVertexLight (ll, v0l, v1bl, v0bl);
                                        // Vertex 2
                                        l3 = ComputeVertexLight (ll, v2l, v1bl, v2bl);

                                        int textureIndex = type.textureSideIndices [rotationIndex].left;
                                        if (type.customTextureProvider != null) {
                                            textureIndex = type.customTextureProvider (textureIndex,
                                            chunk_top_forward_middle [top_forward_middle].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_back_middle [top_back_middle].typeIndex,
                                            chunk_middle_forward_left [middle_forward_left].typeIndex, chunk_middle_back_left [middle_back_left].typeIndex,
                                            chunk_bottom_forward_middle [bottom_forward_middle].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_back_middle [bottom_back_middle].typeIndex);
                                        }

                                        AddFaceWithAO (faceVerticesLeft, normalsLeft, pos, indices, textureIndex + extraData, l0, l1, l2, l3);
#if USES_TINTING
                                            tempChunkColors32.AddRange (faceColors);
#endif
                                        if (addCollider) {
                                            greedyCollider.AddQuad (FaceDirection.Left, z, y, x);
                                            if (enableNavMesh && type.navigatable) {
                                                greedyNavMesh.AddQuad (FaceDirection.Left, z, y, x);
                                            }
                                        }
                                    }
                                    // right face
                                    if (chunk_middle_middle_right [middle_middle_right].opaque < FULL_OPAQUE || denseTreeCheck) {
                                        // Vertex 1
                                        l0 = ComputeVertexLight (lr, v0r, v1br, v0br);
                                        // Vertex 3
                                        l1 = ComputeVertexLight (lr, v2r, v1br, v2br);
                                        // Vertex 5
                                        l2 = ComputeVertexLight (lr, v0r, v1fr, v0fr);
                                        // Vertex 6
                                        l3 = ComputeVertexLight (lr, v2r, v1fr, v2fr);

                                        int textureIndex = type.textureSideIndices [rotationIndex].right;
                                        if (type.customTextureProvider != null) {
                                            textureIndex = type.customTextureProvider (textureIndex,
                                            chunk_top_back_middle [top_back_middle].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_forward_middle [top_forward_middle].typeIndex,
                                            chunk_middle_back_left [middle_back_left].typeIndex, chunk_middle_forward_left [middle_forward_left].typeIndex,
                                            chunk_bottom_back_middle [bottom_back_middle].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_forward_middle [bottom_forward_middle].typeIndex);
                                        }

                                        AddFaceWithAO (faceVerticesRight, normalsRight, pos, indices, textureIndex + extraData, l0, l1, l2, l3);
#if USES_TINTING
                                            tempChunkColors32.AddRange (faceColors);
#endif
                                        if (addCollider) {
                                            greedyCollider.AddQuad (FaceDirection.Right, z, y, x);
                                            if (enableNavMesh && type.navigatable) {
                                                greedyNavMesh.AddQuad (FaceDirection.Right, z, y, x);
                                            }
                                        }
                                    }
                                    // top face
                                    if (chunk_top_middle_middle [top_middle_middle].opaque < FULL_OPAQUE || denseTreeCheck) {
                                        // Top face
                                        // Vertex 2
                                        l0 = ComputeVertexLight (lu, v2b, v2l, v2bl);
                                        // Vertex 7
                                        l1 = ComputeVertexLight (lu, v2l, v2f, v2fl);
                                        // Vvertex 3
                                        l2 = ComputeVertexLight (lu, v2b, v2r, v2br);
                                        // Vertex 6
                                        l3 = ComputeVertexLight (lu, v2r, v2f, v2fr);

                                        int textureIndex = type.textureIndexTop;
                                        if (type.customTextureProvider != null) {
                                            textureIndex = type.customTextureProvider (textureIndex,
                                            chunk_middle_forward_left [middle_forward_left].typeIndex, chunk_middle_forward_middle [middle_forward_middle].typeIndex, chunk_middle_forward_right [middle_forward_right].typeIndex,
                                            chunk_middle_middle_left [middle_middle_left].typeIndex, chunk_middle_middle_right [middle_middle_right].typeIndex,
                                            chunk_middle_back_left [middle_back_left].typeIndex, chunk_middle_back_middle [middle_back_middle].typeIndex, chunk_middle_back_right [middle_back_right].typeIndex);
                                        }

                                        AddFaceWithAO (faceVerticesTop, normalsUp, pos, indices, textureIndex + extraData, l0, l1, l2, l3);
#if USES_TINTING
                                            tempChunkColors32.AddRange (faceColors);
#endif
                                        if (addCollider) {
                                            greedyCollider.AddQuad (FaceDirection.Top, x, z, y);
                                            if (enableNavMesh && type.navigatable) {
                                                greedyNavMesh.AddQuad (FaceDirection.Top, x, z, y);
                                            }
                                        }
                                    }
                                    // bottom face
                                    if (chunk_bottom_middle_middle [bottom_middle_middle].opaque < FULL_OPAQUE || denseTreeCheck) {
                                        // Vertex 1
                                        l0 = ComputeVertexLight (ld, v0b, v0r, v0br);
                                        // Vertex 5
                                        l1 = ComputeVertexLight (ld, v0f, v0r, v0fr);
                                        // Vertex 0
                                        l2 = ComputeVertexLight (ld, v0b, v0l, v0bl);
                                        // Vertex 4
                                        l3 = ComputeVertexLight (ld, v0f, v0l, v0fl);

                                        int textureIndex = type.textureIndexBottom;
                                        if (type.customTextureProvider != null) {
                                            textureIndex = type.customTextureProvider (textureIndex,
                                           chunk_middle_forward_right [middle_forward_right].typeIndex, chunk_middle_forward_middle [middle_forward_middle].typeIndex, chunk_middle_forward_left [middle_forward_left].typeIndex,
                                            chunk_middle_middle_right [middle_middle_right].typeIndex, chunk_middle_middle_left [middle_middle_left].typeIndex,
                                            chunk_middle_back_right [middle_back_right].typeIndex, chunk_middle_back_middle [middle_back_middle].typeIndex, chunk_middle_back_left [middle_back_left].typeIndex);
                                        }

                                        AddFaceWithAO (faceVerticesBottom, normalsDown, pos, indices, textureIndex + extraData, l0, l1, l2, l3);
#if USES_TINTING
                                            tempChunkColors32.AddRange (faceColors);
#endif
                                        if (addCollider) {
                                            greedyCollider.AddQuad (FaceDirection.Bottom, x, z, y);
                                            // no NavMesh for bottom faces
                                        }
                                    }
                                } else {
                                    // Opaque / Cutout without AO
                                    bool denseTreeCheck = false;
                                    float aoBase = 1f;
                                    bool isOpaqueType = type.renderType != RenderType.Cutout;
                                    int extraData = 0;
                                    bool addCollider = enableColliders;
                                    if (!isOpaqueType) {
                                        addCollider &= type.generateColliders;
                                        denseTreeCheck = denseTrees;
                                        if (type.windAnimation) {
                                            extraData = 65536;
                                        }
                                        float random = WorldRand.GetValue (pos);
                                        aoBase *= 1f + (random - 0.45f) * type.colorVariation;
                                    } else {
                                        if (type.renderType == RenderType.OpaqueAnimated) {
                                            extraData = (type.animationSpeed << 18) | ((type.animationTextures.Length + 1) << 14);
                                        }
                                    }
                                    int rotationIndex = voxels [voxelIndex].GetTextureRotation ();

                                    // back face
                                    if (v1b < FULL_OPAQUE || denseTreeCheck) {
                                        float backFaceGI = lb * aoBase;

                                        int textureIndex = type.textureSideIndices [rotationIndex].back;
                                        if (type.customTextureProvider != null) {
                                            textureIndex = type.customTextureProvider (textureIndex,
                                            chunk_top_middle_left [top_middle_left].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_middle_right [top_middle_right].typeIndex,
                                            chunk_middle_middle_left [middle_middle_left].typeIndex, chunk_middle_middle_right [middle_middle_right].typeIndex,
                                            chunk_bottom_middle_left [bottom_middle_left].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_middle_right [bottom_middle_right].typeIndex);
                                        }

                                        if (isOpaqueType) {
                                            greedyOpaqueNoAO.AddQuad (FaceDirection.Back, x, y, z, tintColor, backFaceGI, textureIndex + extraData);
                                        } else {
                                            greedyCutoutNoAO.AddQuad (FaceDirection.Back, x, y, z, tintColor, backFaceGI, textureIndex + extraData);
                                        }
                                        if (addCollider) {
                                            greedyCollider.AddQuad (FaceDirection.Back, x, y, z);
                                            if (enableNavMesh && type.navigatable) {
                                                greedyNavMesh.AddQuad (FaceDirection.Back, x, y, z);
                                            }
                                        }
                                    }
                                    // forward face
                                    if (v1f < FULL_OPAQUE || denseTreeCheck) {
                                        float frontFaceGI = lf * aoBase;

                                        int textureIndex = type.textureSideIndices [rotationIndex].forward;
                                        if (type.customTextureProvider != null) {
                                            textureIndex = type.customTextureProvider (textureIndex,
                                             chunk_top_middle_right [top_middle_right].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_middle_left [top_middle_left].typeIndex,
                                            chunk_middle_middle_right [middle_middle_right].typeIndex, chunk_middle_middle_left [middle_middle_left].typeIndex,
                                            chunk_bottom_middle_right [bottom_middle_right].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_middle_right [bottom_middle_left].typeIndex);
                                        }

                                        if (isOpaqueType) {
                                            greedyOpaqueNoAO.AddQuad (FaceDirection.Forward, x, y, z, tintColor, frontFaceGI, textureIndex + extraData);
                                        } else {
                                            greedyCutoutNoAO.AddQuad (FaceDirection.Forward, x, y, z, tintColor, frontFaceGI, textureIndex + extraData);
                                        }
                                        if (addCollider) {
                                            greedyCollider.AddQuad (FaceDirection.Forward, x, y, z);
                                            if (enableNavMesh && type.navigatable) {
                                                greedyNavMesh.AddQuad (FaceDirection.Forward, x, y, z);
                                            }
                                        }
                                    }
                                    // left face
                                    if (v1l < FULL_OPAQUE || denseTreeCheck) {
                                        float leftFaceGI = ll * aoBase;

                                        int textureIndex = type.textureSideIndices [rotationIndex].left;
                                        if (type.customTextureProvider != null) {
                                            textureIndex = type.customTextureProvider (textureIndex,
                                            chunk_top_forward_middle [top_forward_middle].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_back_middle [top_back_middle].typeIndex,
                                            chunk_middle_forward_left [middle_forward_left].typeIndex, chunk_middle_back_left [middle_back_left].typeIndex,
                                            chunk_bottom_forward_middle [bottom_forward_middle].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_back_middle [bottom_back_middle].typeIndex);
                                        }

                                        if (isOpaqueType) {
                                            greedyOpaqueNoAO.AddQuad (FaceDirection.Left, z, y, x, tintColor, leftFaceGI, textureIndex + extraData);
                                        } else {
                                            greedyCutoutNoAO.AddQuad (FaceDirection.Left, z, y, x, tintColor, leftFaceGI, textureIndex + extraData);
                                        }
                                        if (addCollider) {
                                            greedyCollider.AddQuad (FaceDirection.Left, z, y, x);
                                            if (enableNavMesh && type.navigatable) {
                                                greedyNavMesh.AddQuad (FaceDirection.Left, z, y, y);
                                            }
                                        }
                                    }
                                    // right face
                                    if (v1r < FULL_OPAQUE || denseTreeCheck) {
                                        float rightFaceGI = lr * aoBase;

                                        int textureIndex = type.textureSideIndices [rotationIndex].right;
                                        if (type.customTextureProvider != null) {
                                            textureIndex = type.customTextureProvider (textureIndex,
                                            chunk_top_back_middle [top_back_middle].typeIndex, chunk_top_middle_middle [top_middle_middle].typeIndex, chunk_top_forward_middle [top_forward_middle].typeIndex,
                                            chunk_middle_back_left [middle_back_left].typeIndex, chunk_middle_forward_left [middle_forward_left].typeIndex,
                                            chunk_bottom_back_middle [bottom_back_middle].typeIndex, chunk_bottom_middle_middle [bottom_middle_middle].typeIndex, chunk_bottom_forward_middle [bottom_forward_middle].typeIndex);
                                        }

                                        if (isOpaqueType) {
                                            greedyOpaqueNoAO.AddQuad (FaceDirection.Right, z, y, x, tintColor, rightFaceGI, textureIndex + extraData);
                                        } else {
                                            greedyCutoutNoAO.AddQuad (FaceDirection.Right, z, y, x, tintColor, rightFaceGI, textureIndex + extraData);
                                        }
                                        if (addCollider) {
                                            greedyCollider.AddQuad (FaceDirection.Right, z, y, x);
                                            if (enableNavMesh && type.navigatable) {
                                                greedyNavMesh.AddQuad (FaceDirection.Right, z, y, y);
                                            }
                                        }
                                    }
                                    // top face
                                    if (v1u < FULL_OPAQUE || denseTreeCheck) {
                                        // Top face
                                        float topFaceGI = lu * aoBase;

                                        int textureIndex = type.textureIndexTop;
                                        if (type.customTextureProvider != null) {
                                            textureIndex = type.customTextureProvider (textureIndex,
                                            chunk_middle_forward_left [middle_forward_left].typeIndex, chunk_middle_forward_middle [middle_forward_middle].typeIndex, chunk_middle_forward_right [middle_forward_right].typeIndex,
                                            chunk_middle_middle_left [middle_middle_left].typeIndex, chunk_middle_middle_right [middle_middle_right].typeIndex,
                                            chunk_middle_back_left [middle_back_left].typeIndex, chunk_middle_back_middle [middle_back_middle].typeIndex, chunk_middle_back_right [middle_back_right].typeIndex);
                                        }

                                        if (isOpaqueType) {
                                            greedyOpaqueNoAO.AddQuad (FaceDirection.Top, x, z, y, tintColor, topFaceGI, textureIndex + extraData);
                                        } else {
                                            greedyCutoutNoAO.AddQuad (FaceDirection.Top, x, z, y, tintColor, topFaceGI, textureIndex + extraData);
                                        }
                                        if (addCollider) {
                                            greedyCollider.AddQuad (FaceDirection.Top, x, z, y);
                                            if (enableNavMesh && type.navigatable) {
                                                greedyNavMesh.AddQuad (FaceDirection.Top, x, z, y);
                                            }
                                        }
                                    }
                                    // bottom face
                                    if (v1d < FULL_OPAQUE || denseTreeCheck) {
                                        float bottomFaceGI = ld * aoBase;

                                        int textureIndex = type.textureIndexBottom;
                                        if (type.customTextureProvider != null) {
                                            textureIndex = type.customTextureProvider (textureIndex,
                                           chunk_middle_forward_right [middle_forward_right].typeIndex, chunk_middle_forward_middle [middle_forward_middle].typeIndex, chunk_middle_forward_left [middle_forward_left].typeIndex,
                                            chunk_middle_middle_right [middle_middle_right].typeIndex, chunk_middle_middle_left [middle_middle_left].typeIndex,
                                            chunk_middle_back_right [middle_back_right].typeIndex, chunk_middle_back_middle [middle_back_middle].typeIndex, chunk_middle_back_left [middle_back_left].typeIndex);
                                        }

                                        if (isOpaqueType) {
                                            greedyOpaqueNoAO.AddQuad (FaceDirection.Bottom, x, z, y, tintColor, bottomFaceGI, textureIndex + extraData);
                                        } else {
                                            greedyCutoutNoAO.AddQuad (FaceDirection.Bottom, x, z, y, tintColor, bottomFaceGI, textureIndex + extraData);
                                        }
                                        if (addCollider) {
                                            greedyCollider.AddQuad (FaceDirection.Bottom, x, z, y);
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }

            meshJobs [jobIndex].chunk = chunk;
            meshJobs [jobIndex].totalVoxels = chunkVoxelCount;
            if (chunkVoxelCount == 0) {
                return;
            }

            meshJobs [jobIndex].needsColliderRebuild = (voxelSignature != chunk.voxelSignature);

            if (enableColliders) {
                if (meshJobs [jobIndex].needsColliderRebuild) {
                    greedyCollider.FlushTriangles (meshColliderVertices, meshColliderIndices);
                    if (enableNavMesh) {
                        greedyNavMesh.FlushTriangles (navMeshVertices, navMeshIndices);
                    }
                } else {
                    greedyCollider.Clear ();
                    greedyNavMesh.Clear ();
                }
            }

            chunk.voxelSignature = voxelSignature;

            greedyOpaqueNoAO.FlushTriangles (tempChunkVertices, meshJobs [jobIndex].buffers [VoxelPlayEnvironment.INDICES_BUFFER_OPAQUE].indices, tempChunkUV0, tempChunkNormals, enableTinting ? tempChunkColors32 : null);

            greedyClouds.FlushTriangles (tempChunkVertices, meshJobs [jobIndex].buffers [VoxelPlayEnvironment.INDICES_BUFFER_CLOUD].indices, tempChunkUV0, tempChunkNormals, enableTinting ? tempChunkColors32 : null);

            // Cutout greedy triangles go to cutout buffer (buffer index = 1)
            greedyCutoutNoAO.FlushTriangles (tempChunkVertices, meshJobs [jobIndex].buffers [VoxelPlayEnvironment.INDICES_BUFFER_CUTOUT].indices, tempChunkUV0, tempChunkNormals, enableTinting ? tempChunkColors32 : null);

            int subMeshCount = 0;
            for (int k = 0; k < VoxelPlayEnvironment.MAX_MATERIALS_PER_CHUNK; k++) {
                meshJobs [jobIndex].buffers [k].indicesCount = meshJobs [jobIndex].buffers [k].indices.Count;
                if (meshJobs [jobIndex].buffers [k].indicesCount > 0) {
                    subMeshCount++;
                }
            }
            meshJobs [jobIndex].subMeshCount = subMeshCount;

            meshJobs [jobIndex].mivs = mivs;
        }

        void AddFaceWithAO (Vector3 [] faceVertices, Vector3 [] normals, Vector3 pos, List<int> indices, int textureIndex, float w0, float w1, float w2, float w3)
        {
            int index = tempChunkVertices.Count;
            Vector3 vertPos;
            for (int v = 0; v < 4; v++) {
                vertPos.x = faceVertices [v].x + pos.x;
                vertPos.y = faceVertices [v].y + pos.y;
                vertPos.z = faceVertices [v].z + pos.z;
                tempChunkVertices.Add (vertPos);
            }
            tempChunkNormals.AddRange (normals);

            // Flip triangle so AO looks good at all corners

            if ( w0 + w3  > w1 + w2) {
                indices.Add (index);
                indices.Add (index + 1);
                indices.Add (index + 3);
                indices.Add (index + 3);
                indices.Add (index + 2);
                indices.Add (index + 0);
            } else {
                indices.Add (index);
                indices.Add (index + 1);
                indices.Add (index + 2);
                indices.Add (index + 3);
                indices.Add (index + 2);
                indices.Add (index + 1);
            }
            

            Vector4 v4;
            v4.x = 0;
            v4.y = 0;
            v4.z = textureIndex;
            v4.w = w0;
            tempChunkUV0.Add (v4);
            v4.y = 1f;
            v4.w = w1;
            tempChunkUV0.Add (v4);
            v4.x = 1f;
            v4.y = 0;
            v4.w = w2;
            tempChunkUV0.Add (v4);
            v4.y = 1f;
            v4.w = w3;
            tempChunkUV0.Add (v4);
        }

        void AddFaceWater (Vector3 [] faceVertices, Vector3 [] normals, Vector3 pos, List<int> indices, int textureIndex, int w, int h0, int h1, int h2, int h3)
        {
            int index = tempChunkVertices.Count;
            Vector3 vertPos;
            // vertices
            vertPos.x = faceVertices [0].x + pos.x;
            vertPos.y = h0 / 15f + pos.y;
            vertPos.z = faceVertices [0].z + pos.z;
            tempChunkVertices.Add (vertPos);
            vertPos.x = faceVertices [1].x + pos.x;
            vertPos.y = h1 / 15f + pos.y;
            vertPos.z = faceVertices [1].z + pos.z;
            tempChunkVertices.Add (vertPos);
            vertPos.x = faceVertices [2].x + pos.x;
            vertPos.y = h2 / 15f + pos.y;
            vertPos.z = faceVertices [2].z + pos.z;
            tempChunkVertices.Add (vertPos);
            vertPos.x = faceVertices [3].x + pos.x;
            vertPos.y = h3 / 15f + pos.y;
            vertPos.z = faceVertices [3].z + pos.z;
            tempChunkVertices.Add (vertPos);
            tempChunkNormals.AddRange (normals);

            // indices
            indices.Add (index);
            indices.Add (index + 1);
            indices.Add (index + 2);
            indices.Add (index + 3);
            indices.Add (index + 2);
            indices.Add (index + 1);
            Vector4 v4 = new Vector4 (0f, 0f, textureIndex, w);
            tempChunkUV0.Add (v4);
            v4.y = 1f;
            tempChunkUV0.Add (v4);
            v4.x = 1f;
            v4.y = 0f;
            tempChunkUV0.Add (v4);
            v4.y = 1f;
            tempChunkUV0.Add (v4);
        }

        void AddFaceVegetation (Vector3 [] faceVertices, Vector3 pos, List<int> indices, int textureIndex, float w)
        {
            int index = tempChunkVertices.Count;

            // Add random displacement and elevation
            Vector3 aux = pos;
            float random = WorldRand.GetValue (aux);
            pos.x += random * 0.5f - 0.25f;
            aux.x += 1f;
            random = WorldRand.GetValue (aux);
            pos.z += random * 0.5f - 0.25f;
            pos.y -= random * 0.1f;
            for (int v = 0; v < 4; v++) {
                aux.x = faceVertices [v].x + pos.x;
                aux.y = faceVertices [v].y + pos.y;
                aux.z = faceVertices [v].z + pos.z;
                tempChunkVertices.Add (aux);
                tempChunkNormals.Add (Misc.vector3zero);
            }
            indices.Add (index);
            indices.Add (index + 1);
            indices.Add (index + 2);
            indices.Add (index + 3);
            indices.Add (index + 2);
            indices.Add (index + 1);
            Vector4 v4 = new Vector4 (0, 0, textureIndex, w);
            tempChunkUV0.Add (v4);
            v4.y = 1f;
            tempChunkUV0.Add (v4);
            v4.x = 1f;
            v4.y = 0f;
            tempChunkUV0.Add (v4);
            v4.y = 1f;
            tempChunkUV0.Add (v4);
        }


        void AddFaceTransparent (Vector3 [] faceVertices, Vector3 [] normals, Vector3 pos, List<int> indices, int textureIndex, float light, float alpha)
        {
            int index = tempChunkVertices.Count;
            Vector3 vertPos;
            for (int v = 0; v < 4; v++) {
                vertPos.x = faceVertices [v].x + pos.x;
                vertPos.y = faceVertices [v].y + pos.y;
                vertPos.z = faceVertices [v].z + pos.z;
                tempChunkVertices.Add (vertPos);
            }
            tempChunkNormals.AddRange (normals);


            indices.Add (index);
            indices.Add (index + 1);
            indices.Add (index + 3);
            indices.Add (index + 3);
            indices.Add (index + 2);
            indices.Add (index + 0);

            Vector4 v4 = new Vector4 (0, alpha, textureIndex, light);
            tempChunkUV0.Add (v4);
            v4.x = 1f;
            tempChunkUV0.Add (v4);
            v4.x = 2f;
            tempChunkUV0.Add (v4);
            v4.x = 3f;
            tempChunkUV0.Add (v4);
        }

    }
}
