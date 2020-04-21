using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay
{

	public partial class VoxelPlayEnvironment : MonoBehaviour
	{
		void InitTileRules() {

			if (world.tileRuleSets == null)
				return;
			int ruleCount = world.tileRuleSets.Length;
			for (int k = 0; k < ruleCount; k++) {
				if (world.tileRuleSets [k] != null) {
					world.tileRuleSets [k].Init (this);
				}
			}
		}
					
	}



}
