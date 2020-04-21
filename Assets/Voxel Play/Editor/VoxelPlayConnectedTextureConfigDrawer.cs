using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VoxelPlay
{
    [CustomPropertyDrawer (typeof (ConnectedTextureConfig))]
    public class VoxelPlayConnectedTextureConfigDrawer : PropertyDrawer
    {
        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {

            float lineHeight = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;

            const float w = 60f;
            const float sw = 70f;

            position.y += 6;

            float previewSize = lineHeight * 4f;
            Rect previewRect = new Rect (position.position, new Vector2 (previewSize, previewSize));
            previewRect.x += sw * 3 - 5f;
            previewRect.y += 2f;

            Rect box = new Rect (position.x - 2f, position.y - 4f, sw * 4f + 12f + previewSize, lineHeight * 3f + 14f);
            GUI.Box (box, GUIContent.none);

            position.height = lineHeight;
            position.width = w;

            EditorGUI.BeginChangeCheck ();

            Rect prevPosition = position;

            SerializedProperty tl = property.FindPropertyRelative ("tl");
            tl.boolValue = Toggle (position, tl.boolValue);
            position.x += sw;
            SerializedProperty t = property.FindPropertyRelative ("t");
            t.boolValue = Toggle (position, t.boolValue);
            position.x += sw;
            SerializedProperty tr = property.FindPropertyRelative ("tr");
            tr.boolValue = Toggle (position, tr.boolValue);

            position = prevPosition;
            position.y += lineHeight + 2f;
            prevPosition = position;

            SerializedProperty l = property.FindPropertyRelative ("l");
            l.boolValue = Toggle (position, l.boolValue);
            position.x += sw;
            SerializedProperty texture = property.FindPropertyRelative ("texture");
            EditorGUI.ObjectField (position, texture, GUIContent.none);

            EditorGUI.LabelField(previewRect, new GUIContent( (Texture2D)texture.objectReferenceValue));

            position.x += sw;
            SerializedProperty r = property.FindPropertyRelative ("r");
            r.boolValue = Toggle (position, r.boolValue);

            position = prevPosition;
            position.y += lineHeight + 2f;

            SerializedProperty bl = property.FindPropertyRelative ("bl");
            bl.boolValue = Toggle (position, bl.boolValue);
            position.x += sw;
            SerializedProperty b = property.FindPropertyRelative ("b");
            b.boolValue = Toggle (position, b.boolValue);
            position.x += sw;
            SerializedProperty br = property.FindPropertyRelative ("br");
            br.boolValue = Toggle (position, br.boolValue);

            position.x += sw * 2 + 10f;
            if (GUI.Button (position, "Delete")) {
                if (EditorUtility.DisplayDialog ("", "Delete this entry?", "Yes", "No")) {
                    ConnectedTexture ct = (ConnectedTexture)property.serializedObject.targetObject;
                    List<ConnectedTextureConfig> od = new List<ConnectedTextureConfig> (ct.config);
                    int index = property.GetArrayIndex ();
                    od.RemoveAt (index);
                    ct.config = od.ToArray ();
                    GUI.changed = true;
                }
            }

            if ((EditorGUI.EndChangeCheck () || GUI.enabled) && !Application.isPlaying) {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty (UnityEngine.SceneManagement.SceneManager.GetActiveScene ());
            }
        }

        bool Toggle (Rect position, bool value)
        {
            GUIStyle style = new GUIStyle ("Button");
            style.onActive = style.onNormal;
            style.onFocused = style.onNormal;
            style.onHover = style.onNormal;
            style.active = style.onNormal;
            return EditorGUI.Toggle (position, GUIContent.none, value, style);
        }

        public override float GetPropertyHeight (SerializedProperty prop, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 4f + 5f;
        }

    }
}