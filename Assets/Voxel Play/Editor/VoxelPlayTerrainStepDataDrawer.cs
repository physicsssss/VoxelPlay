using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VoxelPlay {
	[CustomPropertyDrawer(typeof(StepData))]
	public class TerrainStepDataDrawer : PropertyDrawer {

		// Draw the property inside the given rect
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			position.height -= 5f;
			Rect box = new Rect(position.x - 2f, position.y - 2f, position.width + 4f, position.height + 4f);
			EditorGUI.DrawRect(box, new Color(0, 0, 0.175f, 0.15f));

			float lineHeight = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
			position.height = EditorGUIUtility.singleLineHeight;

			ITerrainDefaultGenerator tg = (ITerrainDefaultGenerator)property.serializedObject.targetObject;
			if (tg.Steps == null)
				return;
			
			int[] stepIndices = new int[tg.Steps.Length];
			for (int k = 0; k < tg.Steps.Length; k++) {
				stepIndices[k] = k;
			}
			GUIContent[] stepLabels = new GUIContent[tg.Steps.Length];
			for (int k = 0; k < tg.Steps.Length; k++) {
				stepLabels[k] = new GUIContent("Step " + k.ToString());
			}

			EditorGUIUtility.labelWidth = 120;

			SerializedProperty enabled = property.FindPropertyRelative("enabled");
			Rect prevPosition = position;
			position.x -= 14;
			position.width = 35;
			EditorGUI.PropertyField(position, enabled, GUIContent.none);
			position = prevPosition;
			SerializedProperty stepType = property.FindPropertyRelative("operation");
			int index = property.GetArrayIndex ();
			EditorGUI.PropertyField(position, stepType, new GUIContent("Step " + index));
			if (enabled.boolValue) {
				switch (stepType.intValue) {
					case (int)TerrainStepType.SampleHeightMapTexture:
					case (int)TerrainStepType.SampleRidgeNoiseFromTexture:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("noiseTexture"));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("frecuency"), new GUIContent("Frecuency", "The scale applied to the noise texture"));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("noiseRangeMin"), new GUIContent("Min", "The value of noise is mapped to min-max range"));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("noiseRangeMax"), new GUIContent("Max", "The value of noise is mapped to min-max range"));
						break;
					case (int)TerrainStepType.Constant:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param"), new GUIContent("Constant", "Outputs a constant value."));
						break;		
					case (int)TerrainStepType.Copy:
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Copy Output From", "Copies a result from a previous step."));
						break;		
					case (int)TerrainStepType.Random:
						break;
					case (int)TerrainStepType.Invert:
						break;
					case (int)TerrainStepType.Shift:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param"), new GUIContent("Add", "The value to add to the previous result."));
						break;
					case (int)TerrainStepType.BeachMask:
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Mask Source", "If mask value is zero and altitude is at beach level then altitude will be reduced."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("threshold"), new GUIContent("Threshold", "Values greater than this threshold will cancel beach."));
						break;
					case (int)TerrainStepType.AddAndMultiply:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param"), new GUIContent("Add", "The value to add to the previous result."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param2"), new GUIContent("Then Multiply", "Multiply the result by this value."));
						break;
					case (int)TerrainStepType.MultiplyAndAdd:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param"), new GUIContent("Multiply", "Multiply the value by this value."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param2"), new GUIContent("Then Add", "Add this value to the result."));
						break;
					case (int)TerrainStepType.Exponential:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("param"), new GUIContent("Exponent", "Result = exp(previous_value, exponent)"));
						break;
					case (int)TerrainStepType.Threshold:
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Input", "The source for the threshold operation"));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("threshold"), new GUIContent("Threshold", "Only values greater than threshold are preserved. Otherwise 0 is output."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("thresholdShift"), new GUIContent("If Greater, Add", "A value that is added to the previous value if it passes the threshold."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("thresholdParam"), new GUIContent("If Not, Output...", "A value that is set if the previous value does not pass the threshold."));
						break;
					case (int)TerrainStepType.FlattenOrRaise:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("threshold"), new GUIContent("Min Elevation", "Values greater than this threshold will be flattened."));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("thresholdParam"), new GUIContent("Multiplier", "Flatten multiplier."));
						break;
					case (int)TerrainStepType.BlendAdditive:
						position.y += lineHeight;
						prevPosition = position;
						position.width = 190;
						float labelWidth = EditorGUIUtility.labelWidth;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Input A", "One of the inputs to combine"));
						position.x += 190;
						position.width = 120;
						EditorGUIUtility.labelWidth = 60;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("weight0"), new GUIContent("Weight", "Input A is multiplied by Weight"));
						position = prevPosition;
						position.y += lineHeight;
						prevPosition = position;
						position.width = 190;
						EditorGUIUtility.labelWidth = labelWidth;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex1"), stepLabels, stepIndices, new GUIContent("Input B", "The other part of combination"));
						position.x += 190;
						position.width = 120;
						EditorGUIUtility.labelWidth = 60;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("weight1"), new GUIContent("Weight", "Input A is multiplied by Weight"));
						position = prevPosition;
						break;
					case (int)TerrainStepType.BlendMultiply:
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Input A", "Result = input A * input B"));
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex1"), stepLabels, stepIndices, new GUIContent("Input B", "Result = input A * input B"));
						break;
					case (int)TerrainStepType.Clamp:
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("min"), new GUIContent("Min", "Outputs value or Min if value < Min"));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("max"), new GUIContent("Max", "Outputs value or Max if value > Max"));
						break;
					case (int)TerrainStepType.Select:
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Input", "Choose a step as a source"));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("min"), new GUIContent("Range Min", "Outputs 0 if value is less than Min"));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("max"), new GUIContent("Range Max", "Outputs 0 if value is greater than Max"));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("thresholdParam"), new GUIContent("Outside Value", "Outputs a different value if it's out of range"));
						break;
					case (int)TerrainStepType.Fill:
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Input", "Choose a step as a source"));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("min"), new GUIContent("Range Min", "Outputs fill value if value is between min and max"));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("max"), new GUIContent("Range Max", "Outputs fill value if value is between min and max"));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("thresholdParam"), new GUIContent("Fill Value", "Replaces input value if it's inside the min-max range"));
						break;
					case (int)TerrainStepType.Test:
						position.y += lineHeight;
						EditorGUI.IntPopup(position, property.FindPropertyRelative("inputIndex0"), stepLabels, stepIndices, new GUIContent("Input", "Choose a step as a source"));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("min"), new GUIContent("Range Min", "Outputs 0 if value is less than Min"));
						position.y += lineHeight;
						EditorGUI.PropertyField(position, property.FindPropertyRelative("max"), new GUIContent("Range Max", "Outputs 0 if value is greater than Max"));
						break;
				}

				// Buttons
				prevPosition = position;
				position.x += 20;
				position.y += lineHeight;
				const float buttonWidth = 60;
				const float buttonSpacing = 70;
				position.width = buttonWidth;
				bool markSceneChanges = false;

				if (GUI.Button(position, "Add")) {
					List<StepData> od = new List<StepData>(tg.Steps);
					StepData stepData = new StepData();
					stepData.inputIndex0 = index;
					od.Insert(index + 1, stepData);
					tg.Steps = od.ToArray();
					// Shift any input reference
					for (int k = 0; k < tg.Steps.Length; k++) {
						if (tg.Steps[k].inputIndex0 > index)
							tg.Steps[k].inputIndex0++;
						if (tg.Steps[k].inputIndex1 > index)
							tg.Steps[k].inputIndex1++;

					}
					markSceneChanges = true;
				}
				position.x += buttonSpacing;
				if (GUI.Button(position, "Remove")) {
					List<StepData> od = new List<StepData>(tg.Steps);
					od.RemoveAt(index);
					tg.Steps = od.ToArray();
					// Shift any input reference
					for (int k = 0; k < tg.Steps.Length; k++) {
						if (tg.Steps[k].inputIndex0 >= index)
							tg.Steps[k].inputIndex0--;
						if (tg.Steps[k].inputIndex1 >= index)
							tg.Steps[k].inputIndex1--;
					}
					markSceneChanges = true;
				}
				if (index > 0) {
					position.x += buttonSpacing;
					if (GUI.Button(position, "Up")) {
						StepData o = tg.Steps[index - 1];
						tg.Steps[index - 1] = tg.Steps[index];
						tg.Steps[index] = o;
						// Shift any input reference
						for (int k = 0; k < tg.Steps.Length; k++) {
							if (tg.Steps[k].inputIndex0 == index)
								tg.Steps[k].inputIndex0--;
							if (tg.Steps[k].inputIndex1 == index)
								tg.Steps[k].inputIndex1--;
						}
						markSceneChanges = true;
					}
				}
				if (index < tg.Steps.Length - 1) {
					position.x += buttonSpacing;
					if (GUI.Button(position, "Down")) {
						StepData o = tg.Steps[index + 1];
						tg.Steps[index + 1] = tg.Steps[index];
						tg.Steps[index] = o;
						// Shift any input reference
						for (int k = 0; k < tg.Steps.Length; k++) {
							if (tg.Steps[k].inputIndex0 == index)
								tg.Steps[k].inputIndex0++;
							if (tg.Steps[k].inputIndex1 == index)
								tg.Steps[k].inputIndex1++;
						}
						markSceneChanges = true;
					}
				}

				if (markSceneChanges && !Application.isPlaying) {
					UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
				}

			}

		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			float lineHeight = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
			int numLines = 2;
			switch (property.FindPropertyRelative("operation").intValue) {
				case (int)TerrainStepType.SampleHeightMapTexture:
				case (int)TerrainStepType.SampleRidgeNoiseFromTexture:
					numLines += 4;
					break;
				case (int)TerrainStepType.BlendAdditive:
				case (int)TerrainStepType.BlendMultiply:
				case (int)TerrainStepType.Clamp:
				case (int)TerrainStepType.MultiplyAndAdd:
				case (int)TerrainStepType.AddAndMultiply:
				case (int)TerrainStepType.FlattenOrRaise:
				case (int)TerrainStepType.BeachMask:
					numLines += 2;
					break;
				case (int)TerrainStepType.Test:
					numLines += 3;
					break;
				case (int)TerrainStepType.Threshold:
				case (int)TerrainStepType.Select:
				case (int)TerrainStepType.Fill:
					numLines += 4;
					break;
				case (int)TerrainStepType.Random:
				case (int)TerrainStepType.Invert:
					break;
				default:
					numLines++;
					break;
			}
			float height = property.FindPropertyRelative("enabled").boolValue ? lineHeight * numLines : lineHeight;
			return height + 5f;
		}
	}
}