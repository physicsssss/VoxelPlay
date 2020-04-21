using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;


namespace VoxelPlay {

	public partial class VoxelDefinition {
		
		// look-up index for batched mesh array in GPU instancing
		[NonSerialized]
		public int batchedIndex = -1;

	}

}
