using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;

namespace VoxelPlay
{

    [CustomEditor (typeof (ConnectedTexture))]
    public class VoxelPlayConnectedTextureConfigEditor : Editor
    {

        SerializedProperty voxelDefinition, neighbourDefinition;
        SerializedProperty config;

        void OnEnable ()
        {
            voxelDefinition = serializedObject.FindProperty ("voxelDefinition");
            neighbourDefinition = serializedObject.FindProperty ("neighbourDefinition");
            config = serializedObject.FindProperty ("config");
        }

        public override void OnInspectorGUI ()
        {
            serializedObject.Update ();
            EditorGUILayout.PropertyField (voxelDefinition);
            EditorGUILayout.PropertyField (neighbourDefinition);
            EditorGUILayout.HelpBox ("Specify which adjacent tiles are connected and which texture must be used in each case. Drag the texture to the center of the mosaic.", MessageType.Info);
            EditorGUILayout.PropertyField (config, new GUIContent ("Configuration"), true);
            serializedObject.ApplyModifiedProperties ();
        }



    }

}
