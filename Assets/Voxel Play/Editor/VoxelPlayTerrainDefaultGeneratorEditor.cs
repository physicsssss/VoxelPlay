using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace VoxelPlay {
				
	[CustomEditor (typeof(TerrainDefaultGenerator))]
	public class VoxelPlayTerrainDefaultGeneratorEditor : Editor {

		[Serializable]
		class StepsWrapper {
			[SerializeField]
			public StepData[] steps;
		}

		[MenuItem ("CONTEXT/TerrainDefaultGenerator/Clear Steps")]
		static void ClearSteps (MenuCommand command) {
			try {
				if (EditorUtility.DisplayDialog("Clear Generation Steps", "Are you sure you want to remove all generator steps?", "Yes", "No")) {
					ITerrainDefaultGenerator thisTG = (ITerrainDefaultGenerator)command.context;
					thisTG.Steps = new StepData[0];
					EditorUtility.SetDirty (command.context);
				}
			} catch {
			}
		}

		[MenuItem ("CONTEXT/TerrainDefaultGenerator/Copy Steps")]
		static void CopySteps (MenuCommand command) {
			try {
				ITerrainDefaultGenerator tg = (ITerrainDefaultGenerator)command.context;
				StepsWrapper sw = new StepsWrapper ();
				sw.steps = tg.Steps;
				string text = JsonUtility.ToJson (sw);
				EditorGUIUtility.systemCopyBuffer = text;
			} catch {
			}
		}

		[MenuItem ("CONTEXT/TerrainDefaultGenerator/Paste Steps")]
		static void PasteSteps (MenuCommand command) {
			try {
				string text = EditorGUIUtility.systemCopyBuffer;
				StepsWrapper sw = JsonUtility.FromJson<StepsWrapper> (text);
				StepData[] refSteps = sw.steps;
				StepData[] newSteps = new StepData[refSteps.Length];
				for (int k = 0; k < refSteps.Length; k++) {
					newSteps [k] = refSteps [k];
				}
				ITerrainDefaultGenerator thisTG = (ITerrainDefaultGenerator)command.context;
				thisTG.Steps = newSteps;
				EditorUtility.SetDirty (command.context);
			} catch {

			}
		}

	}



}