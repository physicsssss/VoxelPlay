using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Text;
using System.Globalization;

namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        void LoadGameBinaryFileFormat_10 (BinaryReader br, bool preservePlayerPosition = false)
        {
            // Character controller transform position & rotation
            Vector3 pos = DecodeVector3Binary (br);
            Vector3 characterRotationAngles = DecodeVector3Binary (br);
            Vector3 cameraLocalRotationAngles = DecodeVector3Binary (br);
            if (!preservePlayerPosition) {
                if (characterController != null) {
                    characterController.transform.position = pos;
                    characterController.transform.rotation = Quaternion.Euler (characterRotationAngles);
                    cameraMain.transform.localRotation = Quaternion.Euler (cameraLocalRotationAngles);
                    characterController.UpdateLook ();
                }
            }

            InitSaveGameStructs ();
            // Read voxel definition table
            int vdCount = br.ReadInt16 ();
            for (int k = 0; k < vdCount; k++) {
                saveVoxelDefinitionsList.Add (br.ReadString ());
            }
            // Read item definition table
            int idCount = br.ReadInt16 ();
            for (int k = 0; k < idCount; k++) {
                saveItemDefinitionsList.Add (br.ReadString ());
            }

            int numChunks = br.ReadInt32 ();
            VoxelDefinition voxelDefinition = defaultVoxel;
            int prevVdIndex = -1;
            Color32 voxelColor = Misc.color32White;
            for (int c = 0; c < numChunks; c++) {
                // Read chunks
                // Get chunk position
                Vector3 chunkPosition = DecodeVector3Binary (br);
                VoxelChunk chunk = GetChunkUnpopulated (chunkPosition);
                byte isAboveSurface = br.ReadByte ();
                chunk.isAboveSurface = isAboveSurface == 1;
                chunk.back = chunk.bottom = chunk.left = chunk.right = chunk.forward = chunk.top = null;
                chunk.allowTrees = false;
                chunk.modified = true;
                chunk.isPopulated = true;
                chunk.voxelSignature = chunk.lightmapSignature = -1;
                chunk.renderState = ChunkRenderState.Pending;
                SetChunkOctreeIsDirty (chunkPosition, false);
                ChunkClearFast (chunk);
                // Read voxels
                int numWords = br.ReadInt16 ();
                for (int k = 0; k < numWords; k++) {
                    // Voxel definition
                    int vdIndex = br.ReadInt16 ();
                    if (prevVdIndex != vdIndex) {
                        if (vdIndex >= 0 && vdIndex < vdCount) {
                            voxelDefinition = GetVoxelDefinition (saveVoxelDefinitionsList [vdIndex]);
                            prevVdIndex = vdIndex;
                        }
                    }
                    // RGB
                    voxelColor.r = br.ReadByte ();
                    voxelColor.g = br.ReadByte ();
                    voxelColor.b = br.ReadByte ();
                    // Voxel index
                    int voxelIndex = br.ReadInt16 ();
                    // Repetitions
                    int repetitions = br.ReadInt16 ();

                    byte flags = br.ReadByte ();
                    byte hasCustomRotation = br.ReadByte ();

                    if (voxelDefinition == null) {
                        continue;
                    }

                    // Custom voxel flags
                    if (voxelDefinition.renderType == RenderType.Custom) {
                        flags &= 0xF; // custom voxels do not store texture rotation; their transform has the final rotation
                        if (hasCustomRotation == 1) {
                            Vector3 voxelAngles = DecodeVector3Binary (br);
                            delayedVoxelCustomRotations.Add (GetVoxelPosition (chunkPosition, voxelIndex), voxelAngles);
                        }
                    }
                    for (int i = 0; i < repetitions; i++) {
                        chunk.voxels [voxelIndex + i].Set (voxelDefinition, voxelColor);
                        if (voxelDefinition.renderType == RenderType.Water || voxelDefinition.renderType.supportsTextureRotation ()) {
                            chunk.voxels [voxelIndex + i].SetFlags (flags);
                        }
                        if (voxelDefinition.lightIntensity > 0) {
                            chunk.AddLightSource (voxelIndex + i, voxelDefinition.lightIntensity);
                        }
                    }
                }
                // Read light sources
                int lightCount = br.ReadInt16 ();
                VoxelHitInfo hitInfo = new VoxelHitInfo ();
                for (int k = 0; k < lightCount; k++) {
                    // Voxel index
                    hitInfo.voxelIndex = br.ReadInt16 ();
                    // Voxel center
                    hitInfo.voxelCenter = GetVoxelPosition (chunkPosition, hitInfo.voxelIndex);
                    // Normal
                    hitInfo.normal = DecodeVector3Binary (br);
                    hitInfo.chunk = chunk;
                    // Item definition
                    int itemIndex = br.ReadInt16 ();
                    if (itemIndex < 0 || itemIndex >= idCount)
                        continue;
                    string itemDefinitionName = saveItemDefinitionsList [itemIndex];
                    ItemDefinition itemDefinition = GetItemDefinition (itemDefinitionName);
                    TorchAttach (hitInfo, itemDefinition);
                }
                // Read items
                int itemCount = br.ReadInt16 ();
                for (int k = 0; k < itemCount; k++) {
                    // Voxel index
                    int itemIndex = br.ReadInt16 ();
                    if (itemIndex < 0 || itemIndex >= idCount)
                        continue;
                    string itemDefinitionName = saveItemDefinitionsList [itemIndex];
                    Vector3 itemPosition = DecodeVector3Binary (br);
                    int quantity = br.ReadInt16 ();
                    ItemSpawn (itemDefinitionName, itemPosition, quantity);
                }
            }
            // Destroy any object with VoxelPlaySaveThis component to avoid repetitions
            VoxelPlaySaveThis [] gos = FindObjectsOfType<VoxelPlaySaveThis> ();
            for (int k=0;k<gos.Length;k++) {
                DestroyImmediate(gos [k].gameObject);
            }
            // Load gameobjects
            int goCount = br.ReadInt16 ();
            Dictionary<string, string> data = new Dictionary<string, string> ();
            for (int k = 0; k < goCount; k++) {
                string prefabPath = br.ReadString ();
                GameObject o = Resources.Load<GameObject> (prefabPath);
                if (o != null) {
                    o = Instantiate<GameObject> (o) as GameObject;
                    o.name = br.ReadString ();
                    VoxelPlaySaveThis go = o.GetComponent<VoxelPlaySaveThis> ();
                    if (go == null) {
                        DestroyImmediate (o);
                        continue;
                    }
                    o.transform.position = DecodeVector3Binary (br);
                    o.transform.eulerAngles = DecodeVector3Binary (br);
                    o.transform.localScale = DecodeVector3Binary (br);
                    data.Clear ();
                    Int16 dataCount = br.ReadInt16 ();
                    for (int j = 0; j < dataCount; j++) {
                        string key = br.ReadString ();
                        string value = br.ReadString ();
                        data [key] = value;
                    }
                    go.SendMessage ("OnLoadGame", data);
                }
            }
        }


        void SaveGameBinaryFormat_10 (BinaryWriter bw)
        {

            if (cachedChunks == null)
                return;
            // Build a table with all voxel definitions used in modified chunks
            InitSaveGameStructs ();
            int voxelDefinitionsCount = 0;
            int itemDefinitionsCount = 0;
            int numChunks = 0;

            // Pack used voxel and item definitions
            foreach (KeyValuePair<int, CachedChunk> kv in cachedChunks) {
                if (kv.Value == null)
                    continue;
                VoxelChunk chunk = kv.Value.chunk;
                if (chunk != null && chunk.modified) {
                    numChunks++;
                    if (chunk.voxels != null) {
                        VoxelDefinition last = null;
                        for (int k = 0; k < chunk.voxels.Length; k++) {
                            VoxelDefinition vd = chunk.voxels [k].type;
                            if (vd == null || vd == last || vd.isDynamic || vd.doNotSave)
                                continue;
                            last = vd;
                            if (!saveVoxelDefinitionsDict.ContainsKey (vd)) {
                                saveVoxelDefinitionsDict [vd] = voxelDefinitionsCount++;
                                saveVoxelDefinitionsList.Add (vd.name);
                            }
                        }
                    }
                    if (chunk.items != null) {
                        ItemDefinition last = null;
                        for (int k = 0; k < chunk.items.count; k++) {
                            Item item = chunk.items.values [k];
                            if (item == null)
                                continue;
                            ItemDefinition id = item.itemDefinition;
                            if (id == null || id == last)
                                continue;
                            last = id;
                            if (!saveItemDefinitionsDict.ContainsKey (id)) {
                                saveItemDefinitionsDict [id] = itemDefinitionsCount++;
                                saveItemDefinitionsList.Add (id.name);
                            }
                        }
                    }
                    if (chunk.lightSources != null) {
                        ItemDefinition last = null;
                        for (int k = 0; k < chunk.lightSources.Count; k++) {
                            ItemDefinition id = chunk.lightSources [k].itemDefinition;
                            if (id == null || id == last)
                                continue;
                            last = id;
                            if (!saveItemDefinitionsDict.ContainsKey (id)) {
                                saveItemDefinitionsDict [id] = itemDefinitionsCount++;
                                saveItemDefinitionsList.Add (id.name);
                            }
                        }
                    }
                }
            }

            // Header
            bw.Write (SAVE_FILE_CURRENT_FORMAT);
            bw.Write ((byte)CHUNK_SIZE);
            // Character controller transform position
            if (characterController != null) {
                EncodeVector3Binary (bw, characterController.transform.position);
                // Character controller transform rotation
                EncodeVector3Binary (bw, characterController.transform.rotation.eulerAngles);
            } else {
                EncodeVector3Binary (bw, Misc.vector3zero);
                EncodeVector3Binary (bw, Misc.vector3zero);
            }
            // Character controller's camera local rotation
            if (cameraMain != null) {
                EncodeVector3Binary (bw, cameraMain.transform.localRotation.eulerAngles);
            } else {
                EncodeVector3Binary (bw, Misc.vector3zero);
            }
            // Add voxel definitions table
            int vdCount = saveVoxelDefinitionsList.Count;
            bw.Write ((Int16)vdCount);
            for (int k = 0; k < vdCount; k++) {
                bw.Write (saveVoxelDefinitionsList [k]);
            }
            // Add item definitions table
            int idCount = saveItemDefinitionsList.Count;
            bw.Write ((Int16)idCount);
            for (int k = 0; k < idCount; k++) {
                bw.Write (saveItemDefinitionsList [k]);
            }
            // Add modified chunks
            bw.Write (numChunks);
            foreach (KeyValuePair<int, CachedChunk> kv in cachedChunks) {
                if (kv.Value == null)
                    continue;
                VoxelChunk chunk = kv.Value.chunk;
                if (chunk != null && chunk.modified) {
                    ToggleHiddenVoxels (chunk, true);
                    WriteChunkData_10 (bw, chunk);
                    ToggleHiddenVoxels (chunk, false);
                }
            }
            // Add gameobjects
            VoxelPlaySaveThis [] gos = FindObjectsOfType<VoxelPlaySaveThis> ();
            bw.Write ((Int16)gos.Length);
            Dictionary<string, string> data = new Dictionary<string, string> ();
            for (int k = 0; k < gos.Length; k++) {
                VoxelPlaySaveThis go = gos [k];
                if (string.IsNullOrEmpty (go.prefabResourcesPath)) continue;
                bw.Write (go.prefabResourcesPath);
                bw.Write (go.name);
                EncodeVector3Binary (bw, go.transform.position);
                EncodeVector3Binary (bw, go.transform.eulerAngles);
                EncodeVector3Binary (bw, go.transform.localScale);
                data.Clear ();
                go.SendMessage ("OnSaveGame", data);
                //go.GetData (data);
                Int16 dataCount = (Int16)data.Count;
                bw.Write (dataCount);
                foreach (KeyValuePair<string, string> entry in data) {
                    bw.Write (entry.Key);
                    bw.Write (entry.Value);
                }
            }
        }

        void WriteChunkData_10 (BinaryWriter bw, VoxelChunk chunk)
        {
            // Chunk position
            EncodeVector3Binary (bw, chunk.position);
            bw.Write (chunk.isAboveSurface ? (byte)1 : (byte)0);

            int voxelDefinitionIndex = 0;
            VoxelDefinition prevVD = null;


            // Count voxels words
            int k = 0;
            int numWords = 0;
            while (k < chunk.voxels.Length) {
                if (chunk.voxels [k].hasContent == 1) {
                    VoxelDefinition voxelDefinition = chunk.voxels [k].type;
                    if (voxelDefinition.isDynamic) {
                        k++;
                        continue;
                    }
                    if (voxelDefinition != prevVD) {
                        if (!saveVoxelDefinitionsDict.TryGetValue (voxelDefinition, out voxelDefinitionIndex)) {
                            k++;
                            continue;
                        }
                        prevVD = voxelDefinition;
                    }
                    Color32 tintColor = chunk.voxels [k].color;
                    int flags = 0;
                    if (voxelDefinition.renderType == RenderType.Water || voxelDefinition.renderType.supportsTextureRotation ()) {
                        flags = chunk.voxels [k].GetFlags ();
                    }
                    k++;
                    while (k < chunk.voxels.Length && chunk.voxels [k].type == voxelDefinition && chunk.voxels [k].color.r == tintColor.r && chunk.voxels [k].color.g == tintColor.g && chunk.voxels [k].color.b == tintColor.b && voxelDefinition.renderType != RenderType.Custom && chunk.voxels [k].GetFlags () == flags) {
                        k++;
                    }
                    numWords++;
                } else {
                    k++;
                }
            }
            bw.Write ((Int16)numWords);

            // Write voxels
            k = 0;
            while (k < chunk.voxels.Length) {
                if (chunk.voxels [k].hasContent == 1) {
                    int voxelIndex = k;
                    VoxelDefinition voxelDefinition = chunk.voxels [k].type;
                    if (voxelDefinition.isDynamic) {
                        k++;
                        continue;
                    }
                    if (voxelDefinition != prevVD) {
                        if (!saveVoxelDefinitionsDict.TryGetValue (voxelDefinition, out voxelDefinitionIndex)) {
                            k++;
                            continue;
                        }
                        prevVD = voxelDefinition;
                    }
                    Color32 tintColor = chunk.voxels [k].color;
                    byte flags = 0;
                    if (voxelDefinition.renderType == RenderType.Water || voxelDefinition.renderType.supportsTextureRotation ()) {
                        flags = chunk.voxels [k].GetFlags ();
                    }
                    int repetitions = 1;
                    k++;
                    while (k < chunk.voxels.Length && chunk.voxels [k].type == voxelDefinition && chunk.voxels [k].color.r == tintColor.r && chunk.voxels [k].color.g == tintColor.g && chunk.voxels [k].color.b == tintColor.b && voxelDefinition.renderType != RenderType.Custom && chunk.voxels [k].GetFlags () == flags) {
                        repetitions++;
                        k++;
                    }
                    bw.Write ((Int16)voxelDefinitionIndex);
                    bw.Write (tintColor.r);
                    bw.Write (tintColor.g);
                    bw.Write (tintColor.b);
                    bw.Write ((Int16)voxelIndex);
                    bw.Write ((Int16)repetitions);
                    bw.Write (flags);
                    if (voxelDefinition.renderType == RenderType.Custom) {
                        // Check rotation
                        VoxelPlaceholder placeholder = GetVoxelPlaceholder (chunk, voxelIndex, false);
                        if (placeholder != null) {
                            Vector3 angles = placeholder.transform.eulerAngles;
                            if (angles.x != 0 || angles.y != 0 || angles.z != 0) {
                                bw.Write ((byte)1); // has rotation
                                EncodeVector3Binary (bw, angles);
                            } else {
                                bw.Write ((byte)0);
                            }
                        } else {
                            bw.Write ((byte)0);
                        }
                    } else {
                        bw.Write ((byte)0);
                    }
                } else {
                    k++;
                }
            }

            // Write number of light sources
            int lightCount = chunk.lightSources != null ? chunk.lightSources.Count : 0;
            bw.Write ((Int16)lightCount);
            if (lightCount > 0) {
                for (k = 0; k < lightCount; k++) {
                    LightSource lightSource = chunk.lightSources [k];
                    int voxelIndex = lightSource.hitInfo.voxelIndex;
                    Vector3 normal = lightSource.hitInfo.normal;
                    int itemIndex = 0;
                    ItemDefinition id = lightSource.itemDefinition;
                    if (id != null) {
                        saveItemDefinitionsDict.TryGetValue (id, out itemIndex);
                    }
                    bw.Write ((Int16)voxelIndex);
                    EncodeVector3Binary (bw, normal);
                    bw.Write ((Int16)itemIndex);
                }
            }

            // Write number of items
            int itemCount = chunk.items != null ? chunk.items.count : 0	;
            bw.Write ((Int16)itemCount);
            if (itemCount > 0) {
                for (k = 0; k < itemCount; k++) {
                    Int16 itemIndex = 0;
                    Int16 itemQuantity = 0;
                    Vector3 itemPosition = Misc.vector3zero;
                    Item item = chunk.items.values [k];
                    if (item != null && item.itemDefinition != null) {
                        ItemDefinition id = item.itemDefinition;
                        int idIndex;
                        if (saveItemDefinitionsDict.TryGetValue (id, out idIndex)) {
                            itemIndex = ((Int16)idIndex);
                            itemPosition = item.transform.position;
                            bw.Write ((Int16)item.quantity);
                        }
                    }
                    bw.Write (itemIndex);
                    EncodeVector3Binary (bw, itemPosition);
                    bw.Write (itemQuantity);
                }

            }
        }

    }



}
