using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;

namespace VoxelPlay {
				
	[CustomEditor (typeof(VoxelPlayThirdPersonController))]
	public class VoxelPlayThirdPersonControllerEditor : Editor {

		SerializedProperty useThirdPartyController, startOnFlat, startOnFlatIterations, _characterHeight;
		SerializedProperty crosshairEnabled, crosshairMaxDistance, crosshairScale, targetAnimationScale, targetAnimationSpeed, crosshairNormalColor, crosshairOnTargetColor, changeOnBlock, autoInvertColors;
		SerializedProperty voxelHighlight, voxelHighlightColor, voxelHighlightEdge;

		void OnEnable () {
			useThirdPartyController = serializedObject.FindProperty ("useThirdPartyController");
			startOnFlat = serializedObject.FindProperty ("startOnFlat");
			startOnFlatIterations = serializedObject.FindProperty ("startOnFlatIterations");
			_characterHeight = serializedObject.FindProperty ("_characterHeight");

			crosshairEnabled = serializedObject.FindProperty ("crosshairEnabled");
			crosshairMaxDistance = serializedObject.FindProperty ("crosshairMaxDistance");
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
		}


		public override void OnInspectorGUI () {

			EditorGUILayout.Separator ();

			serializedObject.Update ();
			EditorGUILayout.PropertyField (useThirdPartyController);
			EditorGUILayout.HelpBox ("Enable this checkbox to allow other controllers to take control over the camera and character movement.", MessageType.Info);
			serializedObject.ApplyModifiedProperties ();

			if (!useThirdPartyController.boolValue) {
				DrawDefaultInspector ();
				return;
			}

			EditorGUILayout.PropertyField (startOnFlat);
			if (startOnFlat.boolValue) {
				EditorGUILayout.PropertyField (startOnFlatIterations);
			}
			EditorGUILayout.PropertyField (_characterHeight);

			EditorGUILayout.PropertyField (crosshairEnabled);
			if (crosshairEnabled.boolValue) {
				EditorGUILayout.PropertyField (crosshairMaxDistance);
				EditorGUILayout.PropertyField (crosshairScale);
				EditorGUILayout.PropertyField (targetAnimationScale);
				EditorGUILayout.PropertyField (targetAnimationSpeed);
				EditorGUILayout.PropertyField (crosshairNormalColor);
				EditorGUILayout.PropertyField (crosshairOnTargetColor);
				EditorGUILayout.PropertyField (changeOnBlock);
				EditorGUILayout.PropertyField (autoInvertColors);
			}

			EditorGUILayout.PropertyField (voxelHighlight);
			if (voxelHighlight.boolValue) {
				EditorGUILayout.PropertyField (voxelHighlightColor);
				EditorGUILayout.PropertyField (voxelHighlightEdge);
			}

			serializedObject.ApplyModifiedProperties ();

			EditorGUILayout.Separator ();

		}

	
				

	}

}
