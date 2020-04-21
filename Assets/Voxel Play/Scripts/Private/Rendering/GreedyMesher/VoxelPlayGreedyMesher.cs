using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{
    public enum FaceDirection
    {
        Top,
        Bottom,
        Left,
        Right,
        Forward,
        Back
    }

    public class VoxelPlayGreedyMesher
    {
        VoxelPlayGreedySlice [] slices;

        public VoxelPlayGreedyMesher ()
        {
            slices = new VoxelPlayGreedySlice [VoxelPlayEnvironment.CHUNK_SIZE * 6];
            for (int k = 0; k < slices.Length; k++) {
                slices [k] = new VoxelPlayGreedySlice ();
            }
        }


        public void AddQuad (FaceDirection direction, int x, int y, int slice)
        {
            int index = (int)direction * VoxelPlayEnvironment.CHUNK_SIZE + slice;
            slices [index].AddQuad (x, y);
        }

        public void FlushTriangles (List<Vector3> vertices, List<int> indices)
        {
            for (int d = 0; d < 6; d++) {
                for (int s = 0; s < VoxelPlayEnvironment.CHUNK_SIZE; s++) {
                    slices [d * VoxelPlayEnvironment.CHUNK_SIZE + s].FlushTriangles ((FaceDirection)d, s, vertices, indices);
                }
            }
        }

        public void Clear ()
        {
            for (int k = 0; k < slices.Length; k++) {
                slices [k].Clear ();
            }
        }

    }
}