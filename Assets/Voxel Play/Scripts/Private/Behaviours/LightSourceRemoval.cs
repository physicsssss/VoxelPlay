using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

	public class LightSourceRemoval : MonoBehaviour
	{

		[NonSerialized]
		public VoxelPlayEnvironment env;

		[NonSerialized]
		public VoxelChunk chunk;

		void OnDestroy ()
		{
			if (env != null) {
				env.TorchDetach (chunk, gameObject);
			}
		}

	}

}