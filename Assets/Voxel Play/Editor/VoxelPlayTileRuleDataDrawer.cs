using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VoxelPlay {
	[CustomPropertyDrawer (typeof(TileRuleData))]
	public class TileRuleDataDrawer : PropertyDrawer {

		// Draw the property inside the given rect
		public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {

			position.height -= 5f;

			float lineHeight = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
			position.height = EditorGUIUtility.singleLineHeight;

			TileRuleSet trs = (TileRuleSet)property.serializedObject.targetObject;
			if (trs.rules == null)
				return;
			
			int[] ruleIndices = new int[trs.rules.Length];
			for (int k = 0; k < trs.rules.Length; k++) {
				ruleIndices [k] = k;
			}
			GUIContent[] ruleLabels = new GUIContent[trs.rules.Length];
			for (int k = 0; k < trs.rules.Length; k++) {
				ruleLabels [k] = new GUIContent ("Rule " + k.ToString ());
			}

			EditorGUIUtility.labelWidth = 120;

			SerializedProperty enabled = property.FindPropertyRelative ("enabled");
			Rect prevPosition = position;
			position.x -= 14;
			position.width = 35;
			EditorGUI.PropertyField (position, enabled, GUIContent.none);
			position = prevPosition;
			SerializedProperty ruleCondition = property.FindPropertyRelative ("condition");
			int index = GetIndex (property);
			EditorGUI.PropertyField (position, ruleCondition, new GUIContent ("Rule " + index));
			if (enabled.boolValue) {
				position.y += lineHeight;
				EditorGUI.PropertyField (position, property.FindPropertyRelative ("offset"), new GUIContent ("Offset"));
				switch (ruleCondition.intValue) {
				case (int)TileRuleCondition.Always:
				case (int)TileRuleCondition.IsEmpty:
				case  (int)TileRuleCondition.IsOcuppied:
					break;
				case (int)TileRuleCondition.Equals:
					position.y += lineHeight;
					EditorGUI.PropertyField (position, property.FindPropertyRelative ("compareVoxelDefinition"), new GUIContent ("Voxel Definition"));
					break;
				case (int)TileRuleCondition.IsAny:
					position.y += lineHeight;
					SerializedProperty compareSet = property.FindPropertyRelative ("compareVoxelDefinitionSet");
					if (compareSet != null) {
						EditorGUI.PropertyField (position, compareSet, new GUIContent ("Voxel Definitions"), true);
						if (compareSet.isExpanded) {
							position.y += lineHeight * (compareSet.arraySize + 1);
						}
					}
					break;
				}

				position.y += lineHeight;
				SerializedProperty action = property.FindPropertyRelative ("action");
				EditorGUI.PropertyField (position, action, new GUIContent ("Action"));
				switch (action.intValue) {
				case (int)TileRuleAction.CancelPlacement:
					break;
				case (int)TileRuleAction.Replace:
					position.y += lineHeight;
					EditorGUI.PropertyField (position, property.FindPropertyRelative ("replacementSingle"), new GUIContent ("Voxel Definition"));
					break;
				case (int)TileRuleAction.Cycle:
				case (int)TileRuleAction.Random:
					position.y += lineHeight;
					SerializedProperty replacementSet = property.FindPropertyRelative ("replacementSet");
					if (replacementSet != null) {
						EditorGUI.PropertyField (position, replacementSet, new GUIContent ("Voxel Definitions"), true);
						if (replacementSet.isExpanded) {
							position.y += lineHeight * (replacementSet.arraySize + 1);
						}
					}
					break;				
				}

				// Buttons
				prevPosition = position;
				position.x += 15;
				position.y += lineHeight;
				const float buttonWidth = 60;
				const float buttonSpacing = 70;
				position.width = buttonWidth;
				bool markSceneChanges = false;

				if (GUI.Button (position, "Add")) {
					List<TileRuleData> od = new List<TileRuleData> (trs.rules);
					TileRuleData tileRuleData = new TileRuleData ();
					od.Insert (index + 1, tileRuleData);
					trs.rules = od.ToArray ();
				}
				markSceneChanges = true;

				position.x += buttonSpacing;
				if (GUI.Button (position, "Remove")) {
					List<TileRuleData> od = new List<TileRuleData> (trs.rules);
					od.RemoveAt (index);
					trs.rules = od.ToArray ();
					markSceneChanges = true;
				}
				if (index > 0) {
					position.x += buttonSpacing;
					if (GUI.Button (position, "Up")) {
						TileRuleData o = trs.rules [index - 1];
						trs.rules [index - 1] = trs.rules [index];
						trs.rules [index] = o;
						markSceneChanges = true;
					}
				}
				if (index < trs.rules.Length - 1) {
					position.x += buttonSpacing;
					if (GUI.Button (position, "Down")) {
						TileRuleData o = trs.rules [index + 1];
						trs.rules [index + 1] = trs.rules [index];
						trs.rules [index] = o;
					}
				}

				if (markSceneChanges && !Application.isPlaying) {
					UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene ());
				}
			}

		}

		public override float GetPropertyHeight (SerializedProperty property, GUIContent label) {
			float lineHeight = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
			int numLines = 4;
			switch (property.FindPropertyRelative ("condition").intValue) {
			case (int)TileRuleCondition.Equals:
				numLines++;
				break;
			case (int)TileRuleCondition.IsAny:
				numLines++;
				SerializedProperty compareSet = property.FindPropertyRelative ("compareVoxelDefinitionSet");
				if (compareSet != null && compareSet.isExpanded) {
					numLines += compareSet.arraySize + 1;
				}
				break;
			}
			switch (property.FindPropertyRelative ("action").intValue) {
			case (int)TileRuleAction.Replace:
				numLines++;
				break;
			case (int)TileRuleAction.Cycle:
			case (int)TileRuleAction.Random:
				numLines++;
				SerializedProperty replacementSet = property.FindPropertyRelative ("replacementSet");
				if (replacementSet != null) {
					if (replacementSet.isExpanded) {
						numLines += replacementSet.arraySize + 1;
					}
				}
				break;				
			}
			float height = property.FindPropertyRelative ("enabled").boolValue ? lineHeight * numLines : lineHeight;
			return height + 5f;
		}

		int GetIndex (SerializedProperty property) {
			string s = property.propertyPath;
			int bracket = s.LastIndexOf ("[");
			if (bracket >= 0) {
				string indexStr = s.Substring (bracket + 1, s.Length - bracket - 2);
				int index;
				if (int.TryParse (indexStr, out index)) {
					return index;
				}
			}
			return 0;
		}

	}
}