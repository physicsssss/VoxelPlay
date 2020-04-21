using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VoxelPlay {
	
	[CustomPropertyDrawer (typeof(NoteAttribute))]
	public class VoxelPlayNoteAttributeDrawer : PropertyDrawer {

		NoteAttribute noteAttribute { get { return (NoteAttribute)attribute; } }

		public override float GetPropertyHeight (SerializedProperty prop, GUIContent label) {
			float height = 0;
			if (!string.IsNullOrEmpty (noteAttribute.text)) {
				GUIContent content = new GUIContent (noteAttribute.text);
				GUIStyle style = GUI.skin.GetStyle ("helpbox");
				height += style.CalcHeight (content, EditorGUIUtility.currentViewWidth);
			}
			height += noteAttribute.margin;
			return height;
		}


		public override void OnGUI (Rect position, SerializedProperty prop, GUIContent label) {
			if (!string.IsNullOrEmpty (noteAttribute.text)) {
				position.y += noteAttribute.margin;
				position.height -= noteAttribute.margin;
				EditorGUI.HelpBox (position, noteAttribute.text, MessageType.Info);
			}
		}
	}

}
