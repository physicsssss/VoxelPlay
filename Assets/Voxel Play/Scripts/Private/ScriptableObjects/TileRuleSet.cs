using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	public enum TileRuleCondition {
		IsEmpty,
		IsOcuppied,
		IsAny,
		Equals,
		Always
	}

	public enum TileRuleAction {
		CancelPlacement,
		Replace,
		Random,
		Cycle
	}


	[Serializable]
	public struct TileRuleData {
		public bool enabled;
		public Vector3 offset;
		public TileRuleCondition condition;
		public VoxelDefinition compareVoxelDefinition;
		public VoxelDefinition[] compareVoxelDefinitionSet;
		public TileRuleAction action;
		public VoxelDefinition replacementSingle;
		public VoxelDefinition[] replacementSet;
	}

// TODO: TILE RULES ARE UNDER DEVELOPMENT
//	[CreateAssetMenu (menuName = "Voxel Play/Tile Rule Set", fileName = "TileRuleSet", order = 132)]
	public class TileRuleSet : ScriptableObject {

		public string description;

		[Tooltip("The voxel the user is placing.")]
		public VoxelDefinition placingVoxel;

		[Tooltip("Rules that apply to this voxel.")]
		public TileRuleData[] rules;

		VoxelPlayEnvironment env;
		int cycleIndex;

		public void Init (VoxelPlayEnvironment env) {
			this.env = env;
			if (env != null) {
				env.OnVoxelBeforePlace += ApplyRule;
			}
		}

		public void OnDestroy () {
			if (env != null) {
				env.OnVoxelBeforePlace -= ApplyRule;
			}
		}


		void ApplyRule (Vector3 position, VoxelChunk chunk, int voxelIndex, ref VoxelDefinition voxelDefinition, ref Color32 tintColor) {
			if (rules == null || env == null || voxelDefinition != this.placingVoxel)
				return;

			VoxelChunk otherChunk;
			int otherIndex;
			VoxelDefinition[] otherVoxelDefinitions;

			for (int k = 0; k < rules.Length; k++) {
				if (!rules [k].enabled)
					continue;
				Vector3 pos = position + rules [k].offset;
				bool res = false;
				switch (rules [k].condition) {
				case TileRuleCondition.IsEmpty: 
					res = env.IsEmptyAtPosition (pos);
					break;
				case TileRuleCondition.IsOcuppied:
					res = !env.IsEmptyAtPosition (pos);
					break;
				case TileRuleCondition.Equals:
					if (env.GetVoxelIndex (pos, out otherChunk, out otherIndex, false)) {
						res = otherChunk.voxels [otherIndex].typeIndex == voxelDefinition.index;
					}
					break;
				case TileRuleCondition.IsAny:
					otherVoxelDefinitions = rules [k].compareVoxelDefinitionSet;
					if (otherVoxelDefinitions != null && env.GetVoxelIndex (pos, out otherChunk, out otherIndex, false)) {
						int otherTypeIndex = otherChunk.voxels [otherIndex].typeIndex;
						for (int j = 0; j < otherVoxelDefinitions.Length; j++) {
							if (otherVoxelDefinitions [j].index == otherTypeIndex) {
								res = true;
								break;
							}
						}
					}
					break;
				case TileRuleCondition.Always:
					res = true;
					break;
				}

				if (res) {
					switch (rules [k].action) {
					case TileRuleAction.CancelPlacement:
						voxelDefinition = null;
						break;
					case TileRuleAction.Replace:
						voxelDefinition = rules [k].replacementSingle;
						break;
					case TileRuleAction.Random:
						otherVoxelDefinitions = rules [k].replacementSet;
						if (otherVoxelDefinitions != null && otherVoxelDefinitions.Length > 0) {
							voxelDefinition = otherVoxelDefinitions [UnityEngine.Random.Range (0, otherVoxelDefinitions.Length)];
						}
						break;
					case TileRuleAction.Cycle:
						otherVoxelDefinitions = rules [k].replacementSet;
						if (otherVoxelDefinitions != null && otherVoxelDefinitions.Length > 0) {
							voxelDefinition = otherVoxelDefinitions [cycleIndex++];
							if (cycleIndex >= otherVoxelDefinitions.Length) {
								cycleIndex = 0;
							}
						}
						break;
					}
					return; // rule executed, exit
				}
			}
		}
	}

}