using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;

namespace VoxelPlay
{

    [CustomEditor (typeof (VoxelPlaySaveThis))]
    public class VoxelPlaySaveThisEditor : Editor
    {

        SerializedProperty prefabPath;

        void OnEnable ()
        {
            prefabPath = serializedObject.FindProperty ("prefabResourcesPath");
        }

        public override void OnInspectorGUI ()
        {
            EditorGUILayout.HelpBox ("This GameObject will be included in the savegame.", MessageType.Info);

            serializedObject.Update ();

            if (string.IsNullOrEmpty (prefabPath.stringValue)) {
                string path = AssetDatabase.GetAssetPath (target);
                if (!string.IsNullOrEmpty (path)) {
                    int k = path.IndexOf ("/Resources/", StringComparison.InvariantCulture);
                    if (k < 0) {
                        prefabPath.stringValue = "Object must reside in Resources folder.";
                    } else {
                        int j = path.LastIndexOf (".prefab", StringComparison.InvariantCulture);
                        if (j < 0) {
                            prefabPath.stringValue = "Object does not seem a prefab.";
                        } else {
                            prefabPath.stringValue = path.Substring (k + 11, j - k - 11);
                        }
                    }
                }
            } else {
                GameObject o = Resources.Load<GameObject> (prefabPath.stringValue);
                if (o==null) {
                    prefabPath.stringValue = null;
                }
            }

            EditorGUILayout.PropertyField (prefabPath);
            serializedObject.ApplyModifiedProperties ();
        }



    }

}
