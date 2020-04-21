using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	[System.Serializable]
	public struct BiomeZone {
		[Range (0, 1f)]
		public float elevationMin;
		[Range (0, 1f)]
		public float elevationMax;
		[Range (0, 1f)]
		public float moistureMin;
		[Range (0, 1f)]
		public float moistureMax;
	}

	[System.Serializable]
	public struct BiomeTree {
		public ModelDefinition tree;
		public float probability;
	}

	[System.Serializable]
	public struct BiomeVegetation {
		public VoxelDefinition vegetation;
		public float probability;
	}

	[System.Serializable]
	public struct BiomeOre {
		public VoxelDefinition ore;
		[Range (0, 1)]
		[Tooltip("Per chunk minimum probability. This min probability should start at the max value of any previous ore so all probabilities stack up.")]
		public float probabilityMin;
		[Range (0, 1)]
		[Tooltip("Per chunk maximum probability")]
		public float probabilityMax;
		public int depthMin;
		public int depthMax;
		[Tooltip("Min size of vein")]
		public int veinMinSize;
		[Tooltip("Max size of vein")]
		public int veinMaxSize;
		[Tooltip("Per chunk minimum number of veins")]
		public int veinsCountMin;
		[Tooltip("Per chunk maximum number of veins")]
		public int veinsCountMax;
	}


	[CreateAssetMenu (menuName = "Voxel Play/Biome Definition", fileName = "BiomeDefinition", order = 100)]
	[HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000001913-biomes")]
	public partial class BiomeDefinition : ScriptableObject {

        [Header("Biome Settings")]
        public BiomeZone[] zones;

#if UNITY_EDITOR
        // Used by biome map explorer
        [NonSerialized]
        public int biomeMapOccurrences;

        /// <summary>
        /// If this biome is visible in the biome explorer
        /// </summary>
        public bool showInBiomeMap = true;
#endif

        public Color biomeMapColor;

        [Header("Terrain Voxels")]
		public VoxelDefinition voxelTop;
		public VoxelDefinition voxelDirt;
        public BiomeOre[] ores;
		[Header("Trees")]
		[Range (0, 0.05f)]
		public float treeDensity = 0.02f;
		public BiomeTree[] trees;
		[Header("Vegetation")]
		public float vegetationDensity = 0.05f;
		public BiomeVegetation[] vegetation;

	}

}