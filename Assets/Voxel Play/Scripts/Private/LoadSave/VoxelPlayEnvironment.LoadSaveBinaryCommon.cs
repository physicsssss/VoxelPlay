using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Globalization;

namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        List<string> saveVoxelDefinitionsList;
        Dictionary<VoxelDefinition, int> saveVoxelDefinitionsDict;
        Dictionary<Vector3, Vector3> delayedVoxelCustomRotations;
        List<string> saveItemDefinitionsList;
        Dictionary<ItemDefinition, int> saveItemDefinitionsDict;

        void InitSaveGameStructs ()
        {
            if (saveVoxelDefinitionsList == null) {
                saveVoxelDefinitionsList = new List<string> (100);
            } else {
                saveVoxelDefinitionsList.Clear ();
            }
            if (saveVoxelDefinitionsDict == null) {
                saveVoxelDefinitionsDict = new Dictionary<VoxelDefinition, int> (100);
            } else {
                saveVoxelDefinitionsDict.Clear ();
            }
            if (saveItemDefinitionsList == null) {
                saveItemDefinitionsList = new List<string> (100);
            } else {
                saveItemDefinitionsList.Clear ();
            }
            if (saveItemDefinitionsDict == null) {
                saveItemDefinitionsDict = new Dictionary<ItemDefinition, int> (100);
            } else {
                saveItemDefinitionsDict.Clear ();
            }
        }


        Vector3 DecodeVector3Binary (BinaryReader br)
        {
            Vector3 v = new Vector3 ();
            v.x = br.ReadSingle ();
            v.y = br.ReadSingle ();
            v.z = br.ReadSingle ();
            return v;
        }

        void EncodeVector3Binary (BinaryWriter bw, Vector3 v)
        {
            bw.Write (v.x);
            bw.Write (v.y);
            bw.Write (v.z);
        }


        /// <summary>
        /// Returns a new byte array with enough capacity to hold the contents of a chunk. Use this before calling GetChunkRawData().
        /// </summary>
        /// <returns></returns>
        public byte [] GetChunkRawBuffer ()
        {
            return new byte [4096 * (Voxel.memorySize + 2)];
        }

        /// <summary>
        /// Returns the voxels of a given chunk in compressed binary form (RLE)
        /// </summary>
        /// <param name="contents">The byte array where to write the data. Use GetChunkRawBuffer() to get a new byte array.</param>
        /// <param name="baseIndex">Optional base index of the array to write to</param>
        /// <returns></returns>
        public int GetChunkRawData (VoxelChunk chunk, byte [] contents, int baseIndex = 0)
        {
            if (chunk == null || contents == null) return 0;

            int minimumLength = 4096 * (Voxel.memorySize + 2);
            if (contents.Length < minimumLength) {
                Debug.Log ("Contents length must be at least of " + minimumLength);
            }

            int i, k = 0, count;
            Voxel voxel = Voxel.Empty;
            for (i = 0; i < chunk.voxels.Length; i++) {
                if (chunk.voxels [i] == voxel) continue;
                count = i - k;
                if (count > 0) {
                    contents [baseIndex++] = (byte)(count >> 8);
                    contents [baseIndex++] = (byte)(count % 256);
                    baseIndex = chunk.voxels [k].WriteRawData (contents, baseIndex);
                }
                k = i;
                voxel = chunk.voxels [i];
            }
            count = i - k;
            if (count > 0) {
                contents [baseIndex++] = (byte)(count >> 8);
                contents [baseIndex++] = (byte)(count % 256);
                baseIndex = chunk.voxels [k].WriteRawData (contents, baseIndex);
            }
            return baseIndex;
        }


        /// <summary>
        /// Replaces the content of a chunk with new voxel content provided by the contents array. Use GetChunkRawData() to get ray binary data from a chunk.
        /// </summary>
        /// <param name="contents"></param>
        /// <param name="length">Length of data in the contents buffer</param>
        /// <param name="baseIndex">Optional starting index for the contents buffer</param>
        /// <param name="validate">Ensures the type of voxels corresponds with any voxel definition.</param>
        public void SetChunkRawData (VoxelChunk chunk, byte [] contents, int length, int baseIndex = 0, bool validate = true)
        {
            if (chunk == null || contents == null) return;
            Voxel voxel = new Voxel ();
            for (int i = 0; i < length;) {
                int count = (contents [i] << 8) + contents [i + 1];
                i += 2;
                i = voxel.ReadRawData (contents, i);
                for (int k = 0; k < count; k++) {
                    chunk.voxels [baseIndex++] = voxel;
                }
            }
            if (validate) {
                for (int k=0;k<chunk.voxels.Length;k++) {
                    if (chunk.voxels[k].typeIndex < 0 || chunk.voxels[k].typeIndex >= voxelDefinitionsCount) {
                        chunk.voxels [k].typeIndex = 0;
                    }
                }
            }
        }
    }



}
