using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

	public class LightSource
	{
		public GameObject gameObject;
		public int voxelIndex;
		public VoxelHitInfo hitInfo;
		public ItemDefinition itemDefinition;
        public byte lightIntensity = 15;
	}

}