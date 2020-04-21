using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;


namespace VoxelPlay {


    public partial class VoxelPlayEnvironment : MonoBehaviour {

        #region Chunk unload management

        bool needCheckUnloadChunks;
        int checkChunksVisibleDistanceIndex = -1;

        [MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
        void TriggerFarChunksUnloadCheck() {
            needCheckUnloadChunks = true;
        }

        void CheckChunksVisibleDistance(long maxFrameTime) {
            if (needCheckUnloadChunks && checkChunksVisibleDistanceIndex < 0) {
                needCheckUnloadChunks = false;
                checkChunksVisibleDistanceIndex = chunksPoolFirstReusableIndex;
            }
            if (checkChunksVisibleDistanceIndex >= 0) {
                CheckChunksVisibleDistanceLoop(maxFrameTime);
            }
        }

        void CheckChunksVisibleDistanceLoop(long maxFrameTime) {

            try {
                bool eventOut = OnChunkExitVisibleDistance != null;
                bool eventIn = OnChunkEnterVisibleDistance != null;
                int max = checkChunksVisibleDistanceIndex + 200;
                if (max >= chunksPoolLoadIndex) max = chunksPoolLoadIndex;
                float visibleDistanceSqr = (_visibleChunksDistance + 1) * CHUNK_SIZE;
                visibleDistanceSqr *= visibleDistanceSqr;
                while (checkChunksVisibleDistanceIndex < max) {
                    VoxelChunk chunk = chunksPool[checkChunksVisibleDistanceIndex];
                    if (chunk.isRendered && !chunk.isCloud) {
                        float dist = FastVector.SqrMaxDistanceXorZ(ref chunk.position, ref currentAnchorPos);
                        if (dist > visibleDistanceSqr) {
                            if (chunk.visibleDistanceStatus != ChunkVisibleDistanceStatus.OutOfVisibleDistance) {
                                chunk.visibleDistanceStatus = ChunkVisibleDistanceStatus.OutOfVisibleDistance;
                                if (unloadFarChunks || eventOut) {
                                    if (unloadFarChunks) { chunk.gameObject.SetActive(false); }
                                    if (eventOut) { OnChunkExitVisibleDistance(chunk); }
                                    if (stopWatch.ElapsedMilliseconds >= maxFrameTime) break;
                                }
                            }
                        } else if (chunk.visibleDistanceStatus != ChunkVisibleDistanceStatus.WithinVisibleDistance) {
                            chunk.visibleDistanceStatus = ChunkVisibleDistanceStatus.WithinVisibleDistance;
                            if (unloadFarChunks || eventOut) {
                                if (unloadFarChunks) { chunk.gameObject.SetActive(true); }
                                if (eventIn) { OnChunkEnterVisibleDistance(chunk); }
                                if (stopWatch.ElapsedMilliseconds >= maxFrameTime) break;
                            }
                        }
                    }
                    checkChunksVisibleDistanceIndex++;
                }
                if (checkChunksVisibleDistanceIndex >= chunksPoolLoadIndex) {
                    checkChunksVisibleDistanceIndex = -1;
                }
            } catch (Exception ex) {
                ShowExceptionMessage(ex);
            }
        }

        #endregion

    }



}
