using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{
    public static class QubicleImporter
    {

        public static ColorBasedModelDefinition ImportBinary (Stream stream, Encoding encoding)
        {
            uint version;
            uint colorFormat;
            uint compressed;
            uint index, count;
            Color32 [] colors = null;

            const int CODEFLAG = 2;
            const int NEXTSLICEFLAG = 6;

            using (BinaryReader br = new BinaryReader (stream, encoding)) {
                version = br.ReadUInt32 ();
                if (version != 257) {
                    Debug.LogError ("Voxel Play: Unrecognized Qubicly binary version in model!");
                    return ColorBasedModelDefinition.Null;
                }
                colorFormat = br.ReadUInt32 ();
                br.ReadUInt32 ();  // zAxis Orientation
                compressed = br.ReadUInt32 ();
                br.ReadUInt32 ();  // Visibility encoded, not used
                uint numMatrices = br.ReadUInt32 ();
                if (numMatrices == 0) {
                    Debug.LogError ("Voxel Play: Model is empty.");
                    return ColorBasedModelDefinition.Null;
                }
                //												List<Color[]> matrixList = new List<Color[]> ();
                //												for (int i = 0; i < numMatrices; i++) {								// for each matrix stored in  
                // read matrix name 
                int nameLength = br.ReadByte ();
                string name = new string (br.ReadChars (nameLength));
                // read matrix size  
                uint sizeX = br.ReadUInt32 ();
                uint sizeY = br.ReadUInt32 ();
                uint sizeZ = br.ReadUInt32 ();
                if (sizeX < 0 || sizeX > 128 || sizeY < 0 || sizeY > 128 || sizeZ < 0 || sizeZ > 128) {
                    Debug.LogError ("Voxel Play: Invalid model size: " + sizeX + "/" + sizeY + "/" + sizeZ);
                    return ColorBasedModelDefinition.Null;
                }
                // read matrix position (in this example the position is irrelevant) 
                int offsetX = br.ReadInt32 ();
                int offsetY = br.ReadInt32 ();
                int offsetZ = br.ReadInt32 ();
                // create matrix and add to matrix list 
                colors = new Color32 [sizeX * sizeY * sizeZ];
                //																matrixList.Add (matrix);  
                if (compressed == 0) { // if uncompressd 
                    for (int z = 0; z < sizeZ; z++)
                        for (int y = 0; y < sizeY; y++)
                            for (int x = 0; x < sizeX; x++) {
                                long colorIndex = x + z * sizeX + y * sizeX * sizeZ;
                                if (colorIndex >= colors.Length) {
                                    Debug.LogError ("Voxel Play: Model size or contents do not match.");
                                    return ColorBasedModelDefinition.Null;
                                }
                                colors [colorIndex] = GetColor (br.ReadUInt32 (), colorFormat);
                            }
                } else { // if compressed
                    int z = -1;
                    while (++z < sizeZ) {
                        index = 0;
                        while (true) {
                            uint data = br.ReadUInt32 ();
                            if (data == NEXTSLICEFLAG)
                                break;

                            if (data == CODEFLAG) {
                                count = br.ReadUInt32 ();
                                data = br.ReadUInt32 ();
                                for (int j = 0; j < count; j++) {
                                    if (data != 0) {
                                        uint x = index % sizeX; // mod = modulo e.g. 12 mod 8 = 4
                                        uint y = index / sizeX; // div = integer division e.g. 12 div 8 = 1 
                                        long colorIndex = x + z * sizeX + y * sizeX * sizeZ;
                                        if (colorIndex >= colors.Length) {
                                            Debug.LogError ("Voxel Play: Model size or contents do not match.");
                                            return ColorBasedModelDefinition.Null;
                                        }
                                        colors [colorIndex] = GetColor (data, colorFormat);
                                    }
                                    index++;
                                }
                            } else {
                                if (data != 0) {
                                    uint x = index % sizeX;
                                    uint y = index / sizeX;
                                    long colorIndex = x + z * sizeX + y * sizeX * sizeZ;
                                    if (colorIndex >= colors.Length) {
                                        Debug.LogError ("Voxel Play: Model size or contents do not match.");
                                        return ColorBasedModelDefinition.Null;
                                    }
                                    colors [colorIndex] = GetColor (data, colorFormat);
                                }
                                index++;
                            }
                        }
                    }
                }
                //												} 


                ColorBasedModelDefinition model = new ColorBasedModelDefinition ();
                model.name = name;
                model.offsetX = offsetX;
                model.offsetY = offsetY;
                model.offsetZ = offsetZ;
                model.sizeX = (int)sizeX;
                model.sizeY = (int)sizeY;
                model.sizeZ = (int)sizeZ;
                model.colors = colors;
                return model;
            }
        }

        /// <summary>
        /// Gets the color.
        /// </summary>
        /// <returns>The color.</returns>
        /// <param name="data">Data.</param>
        /// <param name="colorFormat">Can be either 0 or 1. If 0 voxel data is encoded as RGBA, if 1 as BGRA.</param>
        static Color32 GetColor (uint data, uint colorFormat)
        {
            byte a = (byte)(data >> 24);
            byte b = (byte)((data >> 16) & 255);
            byte g = (byte)((data >> 8) & 255);
            byte r = (byte)(data & 255);
            if (colorFormat == 0) {
                return new Color32 (r, g, b, a);
            }
            return new Color32 (b, g, r, a);

        }
    }

}
