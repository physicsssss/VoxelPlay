using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

    public enum VoxelPlaySkybox
    {
        UserDefined = 0,
        Earth = 1,
        Space = 2,
        EarthSimplified = 3,
        EarthNightCubemap = 4,
        EarthDayNightCubemap = 5
    }


    [CreateAssetMenu (menuName = "Voxel Play/World Definition", fileName = "WorldDefinition", order = 103)]
    [HelpURL ("https://kronnect.freshdesk.com/support/solutions/articles/42000001884-world-definition-fields")]
    public partial class WorldDefinition : ScriptableObject
    {

        public int seed;

        public BiomeDefinition [] biomes;

        [Tooltip ("Default biome used if no biome matches altitude/moisture at a given position. Optional.")]
        public BiomeDefinition defaultBiome;

        [Tooltip ("Generate infinite world.")]
        public bool infinite = true;

        [Tooltip ("The world extents (half of size) assuming the center is at 0,0,0. Extents must be a multiple of chunk size (16). For example, if extents X = 1024 the world will be generated between -1024 and 1024.")]
        public Vector3 extents = new Vector3 (1024, 1024, 1024);

        [Tooltip ("Global wind speed (only for grass)")]
        [Range (0, 16)]
        public float grassWindSpeed = 1;

        [Tooltip ("Global wind speed (only for trees)")]
        [Range (0, 16)]
        public float treeWindSpeed = 1;

        [Header ("Content Generators")]
        public VoxelPlayTerrainGenerator terrainGenerator;
        public VoxelPlayDetailGenerator [] detailGenerators;

        [Header ("Sky & Lighting")]
        public VoxelPlaySkybox skyboxDesktop = VoxelPlaySkybox.Earth;
        public VoxelPlaySkybox skyboxMobile = VoxelPlaySkybox.EarthSimplified;
        public Texture skyboxDayCubemap, skyboxNightCubemap;

        [Range (-10, 10)]
        public float dayCycleSpeed = 1f;

        public bool setTimeAndAzimuth;

        [Range (0, 24)]
        public float timeOfDay;

        [Range (0, 360)]
        public float azimuth = 15f;

        [Range (0, 2f)]
        public float exposure = 1f;

        [Tooltip ("Used to create clouds")]
        public VoxelDefinition cloudVoxel;

        [Range (0, 255)]
        public int cloudCoverage = 110;

        [Range (0, 1024)]
        public int cloudAltitude = 150;

        public Color skyTint = new Color (0.52f, 0.5f, 1f);

        public float lightScattering = 0.01f;
        public float lightIntensityMultiplier = 2f;
        [Tooltip("The speed at which the sun light attenuates underground")]
        [Range(1, 5)] public int lightSunAttenuation = 1;
        [Tooltip ("The speed at which the torch light attenuates across distance")]
        [Range (1, 5)] public int lightTorchAttenuation = 1;

        [Header ("Realistic Water")]
        public Color waterColor = new Color (0.231f, 0.455f, 0.82f, 0.31f); // (0.26f, 0.46f, 0.76f);
        public Color underWaterFogColor = new Color (0.118f, 0.247f, 0.455f, 0.235f);
        public Color foamColor = Color.white;
        [Range (0f, 1f)]
        public float waveScale = 0.1f;
        public float waveSpeed = 0.4f;
        [Range (0, 3)]
        public float waveAmplitude = 1f;
        public float specularIntensity = 2f;
        public float specularPower = 64;
        public float refractionDistortion = 0.08f;
        [Range (0f, 1f)]
        public float fresnel = 0.9f;
        public float normalStrength = 2f;
        [Range (0.49f, 0.555f)]
        public float oceanWaveThreshold = 0.512f;
        [Range (0, 100)]
        public float oceanWaveIntensity = 12f;

        [Header ("FX")]
        [Tooltip ("Duration for the emission animation on certain materials")]
        public float emissionAnimationSpeed = 0.5f;
        public float emissionMinIntensity = 0.5f;
        public float emissionMaxIntensity = 1.2f;

        [Tooltip ("Duration for the voxel damage cracks")]
        public float damageDuration = 3f;
        public Texture2D [] voxelDamageTextures;
        public float gravity = -9.8f;

        [Tooltip ("When set to true, voxel types with 'Trigger Collapse' will fall along nearby voxels marked with 'Will Collapse' flag")]
        public bool collapseOnDestroy = true;

        [Tooltip ("The maximum number of voxels that can fall at the same time")]
        public int collapseAmount = 50;

        [Tooltip ("Delay for consolidating collapsed voxels into normal voxels. A value of zero keeps dynamic voxels in the scene. Note that consolidation takes place when chunk is not in frustum to avoid visual glitches.")]
        public int consolidateDelay = 5;

        [Header ("Additional Objects")]
        public VoxelDefinition [] moreVoxels;
        public ItemDefinition [] items;

        // TODO: TILE RULES ARE UNDER DEVELOPMENT
        [HideInInspector, Header ("Tile Rules")]
        public TileRuleSet [] tileRuleSets;


        [HideInInspector]
        public string resourceLocation;


#if UNITY_EDITOR
        void OnEnable ()
        {
            try {
                resourceLocation = System.IO.Path.GetDirectoryName (UnityEditor.AssetDatabase.GetAssetPath (this));
                int i = resourceLocation.IndexOf ("Resources/");
                if (i > 0) {
                    resourceLocation = resourceLocation.Substring (i + 10);
                } else {
                    resourceLocation = null;
                }
            } catch {
            }
        }


        void OnValidate ()
        {
            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env != null) {
                env.UpdateMaterialProperties ();
            }
        }


#endif
       

    }

}