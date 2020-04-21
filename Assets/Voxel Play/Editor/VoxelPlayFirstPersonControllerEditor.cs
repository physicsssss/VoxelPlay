using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;

namespace VoxelPlay {
				
	[CustomEditor (typeof(VoxelPlayFirstPersonController))]
	public class VoxelPlayFirstPersonControllerEditor : Editor {

		SerializedProperty startOnFlat, startOnFlatIterations, _characterHeight;
		SerializedProperty crosshairScale, targetAnimationScale, targetAnimationSpeed, crosshairNormalColor, crosshairOnTargetColor, changeOnBlock, autoInvertColors;
		SerializedProperty voxelHighlight, voxelHighlightColor, voxelHighlightEdge;
        SerializedProperty loadModel, constructorSize;

		VoxelPlayFirstPersonController fps;
        VoxelPlayEnvironment env;

		void OnEnable () {
			startOnFlat = serializedObject.FindProperty ("startOnFlat");
			startOnFlatIterations = serializedObject.FindProperty ("startOnFlatIterations");
			_characterHeight = serializedObject.FindProperty ("_characterHeight");

			crosshairScale = serializedObject.FindProperty ("crosshairScale");
			targetAnimationScale = serializedObject.FindProperty ("targetAnimationScale");
			targetAnimationSpeed = serializedObject.FindProperty ("targetAnimationSpeed");
			crosshairNormalColor = serializedObject.FindProperty ("crosshairNormalColor");
			crosshairOnTargetColor = serializedObject.FindProperty ("crosshairOnTargetColor");
			changeOnBlock = serializedObject.FindProperty ("changeOnBlock");
			autoInvertColors = serializedObject.FindProperty ("autoInvertColors");

			voxelHighlight = serializedObject.FindProperty ("voxelHighlight");
			voxelHighlightColor = serializedObject.FindProperty ("voxelHighlightColor");
			voxelHighlightEdge = serializedObject.FindProperty ("voxelHighlightEdge");

            loadModel = serializedObject.FindProperty("loadModel");
            constructorSize = serializedObject.FindProperty("constructorSize");

            fps = (VoxelPlayFirstPersonController)target;
            env = VoxelPlayEnvironment.instance;
		}


		public override void OnInspectorGUI () {

            if (env != null && env.buildMode) {
                DrawBuildModeOptions();
                return;
            }

			if (fps.characterController != null) {
				DrawDefaultInspector ();
				return;
			}

			serializedObject.Update ();

			EditorGUILayout.PropertyField (startOnFlat);
			if (startOnFlat.boolValue) {
				EditorGUILayout.PropertyField (startOnFlatIterations);
			}
			EditorGUILayout.PropertyField (_characterHeight);

			EditorGUILayout.PropertyField (crosshairScale);
			EditorGUILayout.PropertyField (targetAnimationScale);
			EditorGUILayout.PropertyField (targetAnimationSpeed);
			EditorGUILayout.PropertyField (crosshairNormalColor);
			EditorGUILayout.PropertyField (crosshairOnTargetColor);
			EditorGUILayout.PropertyField (changeOnBlock);
			EditorGUILayout.PropertyField (autoInvertColors);

			EditorGUILayout.PropertyField (voxelHighlight);
			if (voxelHighlight.boolValue) {
				EditorGUILayout.PropertyField (voxelHighlightColor);
				EditorGUILayout.PropertyField (voxelHighlightEdge);
			}

			if (serializedObject.ApplyModifiedProperties ()) {
				fps.ResetCrosshairPosition ();
			}
		}


        public void DrawBuildModeOptions() {

            serializedObject.Update();

            EditorGUILayout.PropertyField(loadModel, new GUIContent("Model"));
            EditorGUILayout.PropertyField(constructorSize, new GUIContent("Constructor Size", "Default constructor size."));

            serializedObject.ApplyModifiedProperties();
        }

	
				

	}

}
