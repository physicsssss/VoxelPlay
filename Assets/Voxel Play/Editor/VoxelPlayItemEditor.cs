using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

namespace VoxelPlay {

    [CustomEditor(typeof(Item))]
    public class VoxelPlayItemEditor : Editor {

        public override void OnInspectorGUI() {

            Item item = (Item)target;
            if (item == null) return;

            EditorGUILayout.LabelField("World Position", item.transform.position.ToString());
            EditorGUILayout.LabelField("Creation Time", item.creationTime.ToString());
            EditorGUILayout.LabelField("AutoRotate", item.autoRotate.ToString());
            EditorGUILayout.LabelField("Can Be Destroyed", item.canBeDestroyed.ToString());
            EditorGUILayout.LabelField("Can Pick On Approach", item.canPickOnApproach.ToString());
            EditorGUILayout.LabelField("Is Persistent", item.persistentItem.ToString());
            EditorGUILayout.LabelField("Quantity", item.quantity.ToString());
            if (item.itemVoxelIndex >= 0 && item.itemChunk != null) {
                EditorGUILayout.LabelField("Chunk Position", item.itemChunk.position.ToString());
                EditorGUILayout.LabelField("Voxel Index", item.itemVoxelIndex.ToString());
            }
        }

    }

}
