//#define USES_SEE_THROUGH
#define USES_BRIGHT_POINT_LIGHTS
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using VoxelPlay.GPURendering;
using VoxelPlay.GPURendering.Instancing;
using VoxelPlay.GPURendering.InstancingIndirect;
using VoxelPlay.GPULighting;

namespace VoxelPlay
{

    public partial class VoxelPlayEnvironment : MonoBehaviour
    {

        public static bool supportsSeeThrough {
            get {
#if USES_SEE_THROUGH
				return true;
#else
                return false;
#endif
            }
        }

        public static bool supportsBrightPointLights {
            get {
#if USES_BRIGHT_POINT_LIGHTS
				return true;
#else
                return false;
#endif
            }
        }


        struct RenderingMaterial
        {
            public Material material;
            public bool usesTextureArray;
        }


        public const int MESH_JOBS_TOTAL_POOL_SIZE_PC = 2000;
        public const int MESH_JOBS_TOTAL_POOL_SIZE_MOBILE = 128;
        public const int MAX_MATERIALS_PER_CHUNK = 16;

        /* cube coords
		
		7+------+6
		/.   3 /|
		2+------+ |
		|4.....|.+5
		|/     |/
		0+------+1
		
		*/

        public const int INDICES_BUFFER_OPAQUE = 0;
        public const int INDICES_BUFFER_CUTXSS = 1;
        public const int INDICES_BUFFER_CUTOUT = 2;
        public const int INDICES_BUFFER_WATER = 3;
        public const int INDICES_BUFFER_TRANSP = 4;
        public const int INDICES_BUFFER_OPNOAO = 5;
        public const int INDICES_BUFFER_CLOUD = 6;
        public const int INDICES_BUFFER_OPANIM = 7;

        // Unconclusive neighbours
        const byte CHUNK_TOP = 1;
        const byte CHUNK_BOTTOM = 2;
        const byte CHUNK_LEFT = 4;
        const byte CHUNK_RIGHT = 8;
        const byte CHUNK_BACK = 16;
        const byte CHUNK_FORWARD = 32;

        byte [] inconclusiveNeighbourTable = {
            0, 0, 0,
            0, CHUNK_BOTTOM, 0,
            0, 0, 0,
            0, CHUNK_BACK, 0,
            CHUNK_LEFT, 0, CHUNK_RIGHT,
            0, CHUNK_FORWARD, 0,
            0, 0, 0,
            0, CHUNK_TOP, 0,
            0, 0, 0
        };

        // Chunk Rendering
		
        struct BakedMesh {
            public Mesh mesh;
            public Color32 tintColor;
        }
		
        bool effectiveMultithreadGeneration;

        [NonSerialized]
        public VirtualVoxel [] virtualChunk;

        [NonSerialized]
        public Voxel [] emptyChunkUnderground, emptyChunkAboveTerrain;
		
        RenderingMaterial [] renderingMaterials;
        Dictionary<int, Material []> materialsDict;
        List<Color32> modelMeshColors;
        Material matDynamicCutout, matDynamicOpaque;
        Dictionary<BakedMesh, Mesh> bakedMeshes = new Dictionary<BakedMesh, Mesh>();

        /// Each material has an index power of 2 which is combined with other materials to create a multi-material chunk mesh
        Dictionary<Material, int> materialIndices;
        int lastBufferIndex;

        // Multi-thread support
        MeshingThread [] meshingThreads;
        bool generationThreadsRunning;
        private readonly object seeThroughLock = new object ();

        // Instancing
        IGPUInstancingRenderer instancedRenderer;


        #region Renderer initialization

        void InitRenderer ()
        {

            draftModeActive = !applicationIsPlaying && editorRenderDetail == EditorRenderDetail.Draft;

            // Init materials
            matDynamicOpaque = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Dynamic Opaque"));
            matDynamicCutout = Instantiate<Material> (Resources.Load<Material> ("VoxelPlay/Materials/VP Voxel Dynamic Cutout"));

            Material matTerrainOpaque = Instantiate<Material> (RenderType.Opaque.GetDefaultMaterial (this));
            Material matTerrainCutout = Instantiate<Material> (RenderType.Cutout.GetDefaultMaterial (this));
            Material matTerrainOpNoAO = Instantiate<Material> (RenderType.OpaqueNoAO.GetDefaultMaterial (this));
            Material matTerrainCloud = Instantiate<Material> (RenderType.Cloud.GetDefaultMaterial (this));
            Material matTerrainWater = Instantiate<Material> (RenderType.Water.GetDefaultMaterial (this));
            Material matTerrainCutxss = Instantiate<Material> (RenderType.CutoutCross.GetDefaultMaterial (this));
            Material matTerrainTransp = Instantiate<Material> (RenderType.Transp6tex.GetDefaultMaterial (this));
            Material matTerrainOpAnim = Instantiate<Material> (RenderType.OpaqueAnimated.GetDefaultMaterial (this));

            // Init system arrays and structures
            if (materialsDict == null) {
                materialsDict = new Dictionary<int, Material []> ();
            } else {
                materialsDict.Clear ();
            }

            // Assign materials to rendering buffers
            renderingMaterials = new RenderingMaterial [MAX_MATERIALS_PER_CHUNK];
            renderingMaterials [INDICES_BUFFER_OPAQUE] = new RenderingMaterial { material = matTerrainOpaque, usesTextureArray = true };
            renderingMaterials [INDICES_BUFFER_CUTOUT] = new RenderingMaterial { material = matTerrainCutout, usesTextureArray = true };
            renderingMaterials [INDICES_BUFFER_CUTXSS] = new RenderingMaterial { material = matTerrainCutxss, usesTextureArray = true };
            renderingMaterials [INDICES_BUFFER_WATER] = new RenderingMaterial { material = matTerrainWater, usesTextureArray = true };
            renderingMaterials [INDICES_BUFFER_TRANSP] = new RenderingMaterial { material = matTerrainTransp, usesTextureArray = true };
            renderingMaterials [INDICES_BUFFER_OPANIM] = new RenderingMaterial { material = matTerrainOpAnim, usesTextureArray = true };
            renderingMaterials [INDICES_BUFFER_OPNOAO] = new RenderingMaterial { material = matTerrainOpNoAO, usesTextureArray = true };
            renderingMaterials [INDICES_BUFFER_CLOUD] = new RenderingMaterial { material = matTerrainCloud, usesTextureArray = true };

            if (materialIndices == null) {
                materialIndices = new Dictionary<Material, int> ();
            } else {
                materialIndices.Clear ();
            }
            materialIndices [matTerrainOpaque] = INDICES_BUFFER_OPAQUE;
            materialIndices [matTerrainCutout] = INDICES_BUFFER_CUTOUT;
            materialIndices [matTerrainCutxss] = INDICES_BUFFER_CUTXSS;
            materialIndices [matTerrainWater] = INDICES_BUFFER_WATER;
            materialIndices [matTerrainTransp] = INDICES_BUFFER_TRANSP;
            materialIndices [matTerrainOpAnim] = INDICES_BUFFER_OPANIM;
            materialIndices [matTerrainOpNoAO] = INDICES_BUFFER_OPNOAO;
            materialIndices [matTerrainCloud] = INDICES_BUFFER_CLOUD;

            // Triangle opaque and cutout are always loaded because dynamic voxels requires them
            lastBufferIndex = 7;

            modelMeshColors = new List<Color32> (128);
            Voxel.Empty.light = noLightValue;

            InitTempVertices ();
            InitSeeThrough ();
            InitMeshingThreads ();

            if (delayedVoxelCustomRotations == null) {
                delayedVoxelCustomRotations = new Dictionary<Vector3, Vector3> ();
            } else {
                delayedVoxelCustomRotations.Clear ();
            }

            if (useComputeBuffers) {
                instancedRenderer = new GPUInstancingIndirectRenderer (this);
            } else {
                instancedRenderer = new GPUInstancingRenderer (this);
            }

            VoxelPlayLightManager lightManager = currentCamera.GetComponent<VoxelPlayLightManager> ();
            if (lightManager == null) {
                currentCamera.gameObject.AddComponent<VoxelPlayLightManager> ();
            } else {
                lightManager.enabled = true;
            }

            if (realisticWater) {
                currentCamera.depthTextureMode |= DepthTextureMode.Depth;
                currentCamera.forceIntoRenderTexture = true;
            }
            //TODO: Creating Meshes
            StartGenerationThreads ();

        }

        void DisposeRenderer ()
        {
            if (matDynamicOpaque != null) {
                DestroyImmediate (matDynamicOpaque);
            }
            if (matDynamicCutout != null) {
                DestroyImmediate (matDynamicCutout);
            }
            if (instancedRenderer != null) {
                instancedRenderer.Dispose ();
            }
            if (meshingThreads != null) {
                for (int k = 0; k < meshingThreads.Length; k++) {
                    meshingThreads [k].Clear ();
                }
            }
        }


        void InitMeshingThreads ()
        {
            InitVirtualChunk ();
            int maxThreads = effectiveMultithreadGeneration ? SystemInfo.processorCount - 1 : 1;
            if (maxThreads < 1) maxThreads = 1;
            meshingThreads = new MeshingThread [maxThreads];
            int poolSize = (isMobilePlatform ? MESH_JOBS_TOTAL_POOL_SIZE_MOBILE: MESH_JOBS_TOTAL_POOL_SIZE_PC) / maxThreads;
            for (int k = 0; k < meshingThreads.Length; k++) {
                meshingThreads [k] = new MeshingThreadTriangle ();
                meshingThreads [k].Init (k, poolSize, this);
            }
        }

        void StartGenerationThreads ()
        {
            if (effectiveMultithreadGeneration) {
                generationThreadsRunning = true;
                for (int k = 0; k < meshingThreads.Length; k++) {
                    MeshingThread thread = meshingThreads [k];
                    thread.waitEvent = new AutoResetEvent (false);
                    thread.meshGenerationThread = new Thread (() => GenerateChunkMeshDataInBackgroundThread (thread));
                    thread.meshGenerationThread.Start ();
                }
            }
        }

        void StopGenerationThreads ()
        {
            generationThreadsRunning = false;
            if (meshingThreads == null) return;
            for (int k = 0; k < meshingThreads.Length; k++) {
                MeshingThread meshingThread = meshingThreads [k];
                if (meshingThread != null && meshingThread.meshGenerationThread != null) {
                    meshingThread.waitEvent.Set ();
                }
            }
            for (int t = 0; t < meshingThreads.Length; t++) {
                MeshingThread meshingThread = meshingThreads [t];
                if (meshingThread != null && meshingThread.meshGenerationThread != null) {
                    for (int k = 0; k < 100; k++) {
                        bool wait = false;
                        if (meshingThread.meshGenerationThread.IsAlive)
                            wait = true;
                        if (!wait)
                            break;
                        Thread.Sleep (10);
                    }
                }
            }
        }


        void InitVirtualChunk ()
        {
            emptyChunkUnderground = new Voxel [CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE];
            emptyChunkAboveTerrain = new Voxel [CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE];
            for (int k = 0; k < emptyChunkAboveTerrain.Length; k++) {
                emptyChunkAboveTerrain [k].light = (byte)15;
                emptyChunkUnderground [k].hasContent = 1;
                emptyChunkUnderground [k].opaque = FULL_OPAQUE;
            }

            virtualChunk = new VirtualVoxel [CHUNK_SIZE_PLUS_2 * CHUNK_SIZE_PLUS_2 * CHUNK_SIZE_PLUS_2];

            int index = 0;
            for (int y = 0; y < CHUNK_SIZE_PLUS_2; y++) {
                for (int z = 0; z < CHUNK_SIZE_PLUS_2; z++) {
                    for (int x = 0; x < CHUNK_SIZE_PLUS_2; x++, index++) {
                        int vy = 1, vz = 1, vx = 1;
                        if (y == 0) {
                            vy = 0;
                        } else if (y == CHUNK_SIZE + 1) {
                            vy = 2;
                        }
                        if (z == 0) {
                            vz = 0;
                        } else if (z == CHUNK_SIZE + 1) {
                            vz = 2;
                        }
                        if (x == 0) {
                            vx = 0;
                        } else if (x == CHUNK_SIZE + 1) {
                            vx = 2;
                        }
                        virtualChunk [index].chunk9Index = vy * 9 + vz * 3 + vx;
                        int py = (y + CHUNK_SIZE - 1) % CHUNK_SIZE;
                        int pz = (z + CHUNK_SIZE - 1) % CHUNK_SIZE;
                        int px = (x + CHUNK_SIZE - 1) % CHUNK_SIZE;
                        virtualChunk [index].voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
                    }
                }
            }
        }

        #endregion


        #region Rendering


        public void UpdateMaterialProperties ()
        {

            NotifyCameraMove ();
            UpdateAmbientProperties ();

            if (renderingMaterials == null || renderingMaterials.Length == 0)
                return;

            ToggleMaterialKeyword (SKW_VOXELPLAY_USE_AO, !draftModeActive && enableSmoothLighting);
            ToggleMaterialKeyword (SKW_VOXELPLAY_TRANSP_BLING, transparentBling);
            ToggleMaterialKeyword (SKW_VOXELPLAY_AA_TEXELS, hqFiltering && !enableReliefMapping);

            if (enableFogSkyBlending && !draftModeActive) {
                Shader.EnableKeyword (SKW_VOXELPLAY_GLOBAL_USE_FOG);
            } else {
                Shader.DisableKeyword (SKW_VOXELPLAY_GLOBAL_USE_FOG);
            }

            for (int k = 0; k < renderingMaterials.Length; k++) {
                Material mat = renderingMaterials [k].material;
                if (mat != null) {
                    UpdateOutlinePropertiesMat (mat);
                    UpdateNormalMapPropertiesMat (mat);
                    UpdateParallaxPropertiesMat (mat);
                    UpdatePixelLightsPropertiesMat (mat);
                }
            }

            UpdateOutlinePropertiesMat (matDynamicOpaque);
            UpdateNormalMapPropertiesMat (matDynamicOpaque);
            UpdateParallaxPropertiesMat (matDynamicOpaque);
            UpdatePixelLightsPropertiesMat (matDynamicOpaque);
            UpdateOutlinePropertiesMat (matDynamicCutout);
            UpdateNormalMapPropertiesMat (matDynamicCutout);
            UpdateParallaxPropertiesMat (matDynamicCutout);
            UpdatePixelLightsPropertiesMat (matDynamicCutout);

            UpdateRealisticWaterMat ();

            if (OnSettingsChanged != null) {
                OnSettingsChanged ();
            }
        }

        void ToggleMaterialKeyword (string keyword, bool enabled)
        {
            if (renderingMaterials == null)
                return;

            for (int k = 0; k < renderingMaterials.Length; k++) {
                Material mat = renderingMaterials [k].material;
                if (mat != null) {
                    if (enabled && !mat.IsKeywordEnabled (keyword)) {
                        mat.EnableKeyword (keyword);
                    } else if (!enabled && mat.IsKeywordEnabled (keyword)) {
                        mat.DisableKeyword (keyword);
                    }
                }
            }
        }

        void UpdateAmbientProperties ()
        {

            if (world == null)
                return;

            if (cameraMain != null) {
                if (adjustCameraFarClip && distanceAnchor == cameraMain.transform) {
                    cameraMain.farClipPlane = visibleChunksDistance * CHUNK_SIZE;
                }
                float thisFogDistance = fogUseCameraFarClip ? cameraMain.farClipPlane : fogDistance;
                float thisFogStart = thisFogDistance * fogFallOff;
                Vector3 fogData = new Vector3 (thisFogStart * thisFogStart, thisFogDistance * thisFogDistance - thisFogStart * thisFogStart, 0);
                Shader.SetGlobalVector ("_VPFogData", fogData);
            }

            //TODO: Force variables
            // Global sky & global uniforms
            Shader.SetGlobalColor ("_VPSkyTint", world.skyTint);
            Shader.SetGlobalFloat ("_VPFogAmount", fogAmount);
            Shader.SetGlobalFloat ("_VPExposure", world.exposure);
            Shader.SetGlobalFloat ("_VPAmbientLight", ambientLight);
            Shader.SetGlobalFloat ("_VPDaylightShadowAtten", daylightShadowAtten);
            Shader.SetGlobalFloat ("_VPGrassWindSpeed", world.grassWindSpeed * 0.01f);
            Shader.SetGlobalFloat ("_VPTreeWindSpeed", world.treeWindSpeed * 0.005f);

            // Update skybox material
            VoxelPlaySkybox worldSkybox = isMobilePlatform ? world.skyboxMobile : world.skyboxDesktop;

            if (worldSkybox != VoxelPlaySkybox.UserDefined) {
                if (skyboxMaterial != RenderSettings.skybox || RenderSettings.skybox == null) {
                    switch (worldSkybox) {
                    case VoxelPlaySkybox.Earth:
                        if (skyboxEarth == null) {
                            skyboxEarth = Resources.Load<Material> ("VoxelPlay/Materials/VP Skybox Earth");
                        }
                        skyboxMaterial = skyboxEarth;
                        break;
                    case VoxelPlaySkybox.EarthSimplified:
                        if (skyboxEarthSimplified == null) {
                            skyboxEarthSimplified = Resources.Load<Material> ("VoxelPlay/Materials/VP Skybox Earth Simplified");
                        }
                        skyboxMaterial = skyboxEarthSimplified;
                        break;
                    case VoxelPlaySkybox.Space:
                        if (skyboxSpace == null) {
                            skyboxSpace = Resources.Load<Material> ("VoxelPlay/Materials/VP Skybox Space");
                        }
                        skyboxMaterial = skyboxSpace;
                        break;
                    case VoxelPlaySkybox.EarthNightCubemap:
                        if (skyboxEarthNightCube == null) {
                            skyboxEarthNightCube = Resources.Load<Material> ("VoxelPlay/Materials/VP Skybox Earth Night Cubemap");
                        }
                        if (world.skyboxNightCubemap != null) {
                            skyboxEarthNightCube.SetTexture ("_NightTex", world.skyboxNightCubemap);
                        }
                        skyboxMaterial = skyboxEarthNightCube;
                        break;
                    case VoxelPlaySkybox.EarthDayNightCubemap:
                        if (skyboxEarthDayNightCube == null) {
                            skyboxEarthDayNightCube = Resources.Load<Material> ("VoxelPlay/Materials/VP Skybox Earth Day Night Cubemap");
                        }
                        if (world.skyboxDayCubemap != null)
                            skyboxEarthDayNightCube.SetTexture ("_DayTex", world.skyboxDayCubemap);
                        if (world.skyboxNightCubemap != null)
                            skyboxEarthDayNightCube.SetTexture ("_NightTex", world.skyboxNightCubemap);
                        skyboxMaterial = skyboxEarthDayNightCube;
                        break;
                    }
                    RenderSettings.skybox = skyboxMaterial;
                }
            }

        }

        void UpdateRealisticWaterMat ()
        {
            // Update realistic water properties
            if (realisticWater) {
                Material waterMat = renderingMaterials [INDICES_BUFFER_WATER].material;
                if (waterMat != null) {
                    waterMat.SetColor ("_WaterColor", world.waterColor);
                    waterMat.SetColor ("_UnderWaterFogColor", world.underWaterFogColor);

                    waterMat.SetColor ("_FoamColor", world.foamColor);
                    waterMat.SetFloat ("_WaveScale", world.waveScale * world.waveAmplitude);
                    waterMat.SetFloat ("_WaveSpeed", world.waveSpeed * world.waveAmplitude);
                    waterMat.SetFloat ("_WaveAmplitude", world.waveAmplitude);
                    waterMat.SetFloat ("_SpecularIntensity", world.specularIntensity);
                    waterMat.SetFloat ("_SpecularPower", world.specularPower);
                    waterMat.SetFloat ("_RefractionDistortion", world.refractionDistortion * world.waveAmplitude);
                    waterMat.SetFloat ("_Fresnel", 1f - world.fresnel);
                    waterMat.SetFloat ("_NormalStrength", world.normalStrength * world.waveAmplitude);
                    waterMat.SetVector ("_OceanWave", new Vector3 (world.oceanWaveThreshold, world.oceanWaveIntensity, 0));
                }
            }
        }

        void UpdateOutlinePropertiesMat (Material mat)
        {
            if (enableOutline) {
                mat.EnableKeyword (SKW_VOXELPLAY_USE_OUTLINE);
                mat.SetColor ("_OutlineColor", outlineColor);
                mat.SetFloat ("_OutlineThreshold", hqFiltering ? outlineThreshold * 10f : outlineThreshold);
            } else {
                mat.DisableKeyword (SKW_VOXELPLAY_USE_OUTLINE);
            }
        }

        void UpdateParallaxPropertiesMat (Material mat)
        {
            if (enableReliefMapping) {
                mat.EnableKeyword (SKW_VOXELPLAY_USE_PARALLAX);
                mat.SetFloat ("_VPParallaxStrength", reliefStrength);
                mat.SetInt ("_VPParallaxIterations", reliefIterations);
                mat.SetInt ("_VPParallaxIterationsBinarySearch", reliefIterationsBinarySearch);
            } else {
                mat.DisableKeyword (SKW_VOXELPLAY_USE_PARALLAX);
            }
        }

        void UpdateNormalMapPropertiesMat (Material mat)
        {
            if (enableNormalMap) {
                mat.EnableKeyword (SKW_VOXELPLAY_USE_NORMAL);
            } else {
                mat.DisableKeyword (SKW_VOXELPLAY_USE_NORMAL);
            }
        }

        void UpdatePixelLightsPropertiesMat (Material mat)
        {
            if (usePixelLights) {
                mat.EnableKeyword (SKW_VOXELPLAY_USE_PIXEL_LIGHTS);
            } else {
                mat.DisableKeyword (SKW_VOXELPLAY_USE_PIXEL_LIGHTS);
            }
        }


        bool CreateChunkMeshJob (VoxelChunk chunk)
        {
            int threadId = chunk.poolIndex % meshingThreads.Length;
            return meshingThreads [threadId].CreateChunkMeshJob (chunk, generationThreadsRunning);
        }

        void GenerateChunkMeshDataInBackgroundThread (MeshingThread thread)
        {
            try {
                while (generationThreadsRunning) {
                    bool idle;
                    lock (thread.indicesUpdating) {
                        idle = thread.meshJobMeshDataGenerationIndex == thread.meshJobMeshLastIndex;
                    }
                    if (idle) {
                        thread.waitEvent.WaitOne ();
                        continue;
                    }
                    GenerateChunkMeshDataOneJob (thread);
                    lock (thread.indicesUpdating) {
                        thread.meshJobMeshDataGenerationReadyIndex = thread.meshJobMeshDataGenerationIndex;
                    }
                }
            } catch (Exception ex) {
                ShowExceptionMessage (ex);
            }
        }



        void GenerateChunkMeshDataInMainThread (long endTime)
        {

            long elapsed;
            MeshingThread thread = meshingThreads [0];
            do {
                if (thread.meshJobMeshDataGenerationIndex == thread.meshJobMeshLastIndex)
                    return;
                GenerateChunkMeshDataOneJob (thread);
                thread.meshJobMeshDataGenerationReadyIndex = thread.meshJobMeshDataGenerationIndex;
                elapsed = stopWatch.ElapsedMilliseconds;
            } while (elapsed < endTime);
        }


        void GenerateChunkMeshDataOneJob (MeshingThread thread)
        {
            lock (thread.indicesUpdating)
            {
                thread.meshJobMeshDataGenerationIndex++;
                if (thread.meshJobMeshDataGenerationIndex >= thread.meshJobs.Length)
                {
                    thread.meshJobMeshDataGenerationIndex = 0;
                }
            }

            VoxelChunk chunk = thread.meshJobs [thread.meshJobMeshDataGenerationIndex].chunk;
            Voxel [] [] chunk9 = thread.chunk9;
            chunk9 [13] = chunk.voxels;
            Voxel [] emptyChunk = chunk.isAboveSurface ? emptyChunkAboveTerrain : emptyChunkUnderground;
            int chunkX, chunkY, chunkZ;
            FastMath.FloorToInt (chunk.position.x / CHUNK_SIZE, chunk.position.y / CHUNK_SIZE, chunk.position.z / CHUNK_SIZE, out chunkX, out chunkY, out chunkZ);

            // Reset bit field; inconclusive neighbours are those neighbours which are undefined when an adjacent chunk is rendered. We mark it so then it's finally rendered, we re-render the adjacent chunk. This is required if the new chunk can 
            // break holes on the edge of the chunk while no lighting is entering the chunk or global illumination is disabled (yes, it's an edge case but must be addressed to avoid gaps in those cases).
            chunk.inconclusiveNeighbours = 0; //(byte)(chunk.inconclusiveNeighbours & ~CHUNK_IS_INCONCLUSIVE);

            VoxelChunk [] neighbourChunks = thread.neighbourChunks;
            neighbourChunks [13] = chunk;
            thread.allowAO = enableSmoothLighting && !draftModeActive; // AO is disabled on edges of world to reduce vertex count
            for (int c = 0, y = -1; y <= 1; y++) {
                int yy = chunkY + y;
                for (int z = -1; z <= 1; z++) {
                    int zz = chunkZ + z;
                    for (int x = -1; x <= 1; x++, c++) {
                        if (y == 0 && z == 0 && x == 0)
                            continue;
                        int xx = chunkX + x;
                        VoxelChunk neighbour;
                        if (GetChunkFast (xx, yy, zz, out neighbour, false) && (neighbour.isPopulated || neighbour.isRendered)) {
                            chunk9 [c] = neighbour.voxels;
                        } else {
                            chunk.inconclusiveNeighbours |= inconclusiveNeighbourTable [c];
                            chunk9 [c] = emptyChunk;
                            if (y == 0 && !chunk.modified) {
                                // if this chunk has no neighbours horizontally and is not modified, it's probably an edge chunk.
                                // in this case, disable AO so the entire edge wall can be rendered using a single quad thanks to greedy meshing
                                thread.allowAO = false;
                            }
                        }
                        neighbourChunks [c] = neighbour;
                    }
                }
            }
#if USES_SEE_THROUGH
			lock (seeThroughLock) {

				// Hide voxels marked as hidden
				for (int c = 0; c < neighbourChunks.Length; c++) {
					ToggleHiddenVoxels (neighbourChunks [c], false);	
				}
#endif

            thread.GenerateMeshData ();

#if USES_SEE_THROUGH
				// Reactivate hidden voxels
				for (int c = 0; c < neighbourChunks.Length; c++) {
					ToggleHiddenVoxels (neighbourChunks [c], true);	
				}
			}
#endif
        }

        void UploadMeshData (MeshingThread thread, int jobIndex)
        {
            MeshJobData [] meshJobs = thread.meshJobs;
            VoxelChunk chunk = meshJobs [jobIndex].chunk;
            int diviser = 10;
            // Update collider?
            if (enableColliders && meshJobs [jobIndex].needsColliderRebuild) {
                int colliderVerticesCount = meshJobs [jobIndex].colliderVertices.Count;
                Mesh colliderMesh = chunk.mc.sharedMesh;
#if UNITY_EDITOR
                if (!applicationIsPlaying && editorRenderDetail != EditorRenderDetail.StandardPlusColliders) {
                    colliderVerticesCount = 0;
                }
#endif
                if (colliderVerticesCount == 0) {
                    chunk.mc.enabled = false;
                } else {
                    if (colliderMesh == null) {
                        colliderMesh = new Mesh ();
                    } else {
                        colliderMesh.Clear ();
                    }
                    //for(int l = 0; l < meshJobs[jobIndex].colliderVertices.Count; l++)
                    //{
                    //    meshJobs[jobIndex].colliderVertices[l] = new Vector3(meshJobs[jobIndex].colliderVertices[l].x / meshJobs[jobIndex].colliderVertices[l].y / diviser, meshJobs[jobIndex].colliderVertices[l].z / diviser);
                    //}
                    colliderMesh.SetVertices (meshJobs [jobIndex].colliderVertices);
                    colliderMesh.SetTriangles (meshJobs [jobIndex].colliderIndices, 0);
                    chunk.mc.sharedMesh = colliderMesh;
                    chunk.mc.enabled = true;
                }

                // Update navmesh
                if (enableNavMesh) {
                    int navMeshVerticesCount = meshJobs [jobIndex].navMeshVertices.Count;
                    Mesh navMesh = chunk.navMesh;
                    if (navMesh == null) {
                        navMesh = new Mesh ();
                    } else {
                        navMesh.Clear ();
                    }
                    navMesh.SetVertices (meshJobs [jobIndex].navMeshVertices);
                    navMesh.SetTriangles (meshJobs [jobIndex].navMeshIndices, 0);
                    chunk.navMesh = navMesh;
                    AddChunkNavMesh (chunk);
                }
            }

            // Update mesh?
            if (meshJobs [jobIndex].totalVoxels == 0) {
                if (chunk.mf.sharedMesh != null) {
                    chunk.mf.sharedMesh.Clear (false);
                }
                chunk.renderState = ChunkRenderState.RenderingComplete;
                return;
            }

            // Create mesh
            Mesh mesh = chunk.mf.sharedMesh;
#if !UNITY_EDITOR
			if (isMobilePlatform) {
                if (mesh != null) {
                    DestroyImmediate(mesh);
                }
				mesh = new Mesh (); // on mobile will be released mesh data upon uploading to the GPU so the mesh is no longer readable; need to recreate it everytime the chunk is rendered
			} else {
				if (mesh == null) {
					mesh = new Mesh ();
				} else {
					mesh.Clear ();
				}
			}
#else
            if (mesh == null) {
                mesh = new Mesh ();
                chunksDrawn++;
            } else {
                voxelsCreatedCount -= chunk.totalVisibleVoxelsCount;
                mesh.Clear ();
            }
            chunk.totalVisibleVoxelsCount = meshJobs [jobIndex].totalVoxels;
            voxelsCreatedCount += chunk.totalVisibleVoxelsCount;
#endif

            // Assign materials and submeshes
            mesh.subMeshCount = meshJobs [jobIndex].subMeshCount;
            if (mesh.subMeshCount > 0) {
                //todo: Changing vertices.. correct this
                
                //for (int l= 0; l< meshJobs[jobIndex].vertices.Count; l++)
                //{
                //    meshJobs[jobIndex].vertices[l] = new Vector3 (meshJobs[jobIndex].vertices[l].x / diviser, meshJobs[jobIndex].vertices[l].y/ diviser, meshJobs[jobIndex].vertices[l].z/ diviser);
                //}
                // Vertices
                mesh.SetVertices (meshJobs [jobIndex].vertices);

                // UVs, normals, colors
                mesh.SetUVs (0, meshJobs [jobIndex].uv0);
                mesh.SetNormals (meshJobs [jobIndex].normals);
                if (enableTinting) {
                    mesh.SetColors (meshJobs [jobIndex].colors);
                }

                int subMeshIndex = -1;
                int matIndex = 0;

                for (int k = 0; k < MAX_MATERIALS_PER_CHUNK; k++) {
                    if (meshJobs [jobIndex].buffers [k].indicesCount > 0) {
                        subMeshIndex++;
                        mesh.SetTriangles (meshJobs [jobIndex].buffers [k].indices, subMeshIndex, false);
                        matIndex += 1 << k;
                    }
                }

                // Compute material array
                Material [] matArray;
                if (!materialsDict.TryGetValue (matIndex, out matArray)) {
                    matArray = new Material [mesh.subMeshCount];
                    for (int k = 0, j = 0; k < MAX_MATERIALS_PER_CHUNK; k++) {
                        if (meshJobs [jobIndex].buffers [k].indicesCount > 0) {
                            matArray [j++] = renderingMaterials [k].material;
                        }
                    }
                    materialsDict [matIndex] = matArray;
                }
                chunk.mr.sharedMaterials = matArray;

                if (chunk.isCloud) {
                    mesh.bounds = new Bounds (Misc.vector3zero, new Vector3 (CHUNK_SIZE * 4, CHUNK_SIZE * 2, CHUNK_SIZE * 4));
                } else if (enableCurvature) {
                    mesh.bounds = new Bounds (Misc.vector3zero, new Vector3 (CHUNK_SIZE, CHUNK_SIZE + 32, CHUNK_SIZE));
                } else {
                    mesh.bounds = new Bounds (Misc.vector3zero, new Vector3 (CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE));
                }

                chunk.mf.sharedMesh = mesh;

#if !UNITY_EDITOR
				if (isMobilePlatform) {
					mesh.UploadMeshData (true);
				}
#endif
                if (!chunk.mr.enabled) {
                    chunk.mr.enabled = true;
                }
            }

            RenderModelsInVoxels (chunk, meshJobs [jobIndex].mivs);

            if (chunk.renderState != ChunkRenderState.RenderingComplete) {
                chunk.renderState = ChunkRenderState.RenderingComplete;
                if (OnChunkAfterFirstRender != null) {
                    OnChunkAfterFirstRender (chunk);
                }
            }

            if (OnChunkRender != null) {
                OnChunkRender (chunk);
            }

            shouldUpdateParticlesLighting = true;
        }

        Mesh BakeMeshLighting(MeshFilter mf, Color32 tintColor) {

            BakedMesh bm = new BakedMesh();
            bm.mesh = mf.sharedMesh;
            bm.tintColor = tintColor;

            Mesh mesh;
            if (bakedMeshes.TryGetValue(bm, out mesh)) {
                return mesh;
            }

            Mesh meshWithColors = Instantiate<Mesh>(bm.mesh);
            meshWithColors.hideFlags = HideFlags.DontSave;

            Color32[] colors32 = meshWithColors.colors32;
            modelMeshColors.Clear();
            int vertexCount = meshWithColors.vertexCount;
            if (colors32 == null || colors32.Length == 0) {
                for (int c = 0; c < vertexCount; c++) {
                    modelMeshColors.Add(tintColor);
                }
            } else {
                for (int c = 0; c < vertexCount; c++) {
                    Color32 color = tintColor.MultiplyRGB(colors32[c]);
                    modelMeshColors.Add(color);
                }
            }
            meshWithColors.SetColors(modelMeshColors);

            bakedMeshes[bm] = meshWithColors;
            return meshWithColors;
        }

        void RenderModelsInVoxels (VoxelChunk chunk, FastList<ModelInVoxel> mivs)
        {

            instancedRenderer.ClearChunk (chunk);

            // deactivate all models in this chunk
            // we need to iterate the placeholders list entirely to address the case when the voxel is not using GPU instancing. In this case the gameobject renderer needs to be disabled 
            // and we need to do this way because mivs won't contain the custom voxel since it may be termporarily converted to a transparent voxels due to see-through effect
            if (chunk.placeholders != null) {
                int count = chunk.placeholders.Count;
                for (int k = 0; k < count; k++) {
                    if (chunk.placeholders.entries [k].key >= 0) {
                        VoxelPlaceholder placeHolder = chunk.placeholders.entries [k].value;
                        if (placeHolder != null) {
                            placeHolder.ToggleRenderers (false);
                        }
                    }
                }
            }

            Quaternion rotation = Misc.quaternionZero;
            Vector3 position;

            for (int k = 0; k < mivs.count; k++) {
                ModelInVoxel miv = mivs.values [k];
                VoxelDefinition voxelDefinition = miv.vd;

                bool createGO = voxelDefinition.createGameObject || !voxelDefinition.gpuInstancing;

                if (VoxelIsHidden (chunk, miv.voxelIndex)) {
                    continue;
                }

                if (createGO) {
                    VoxelPlaceholder placeholder = GetVoxelPlaceholder (chunk, miv.voxelIndex, true);
                    bool createModel = true;
                    if (placeholder.modelInstance != null) {
                        if (placeholder.modelTemplate != voxelDefinition.prefab) {
                            DestroyImmediate (placeholder.modelInstance);
                            placeholder.originalMeshColors32 = null;
                            placeholder.lastMivTintColor = Misc.color32White;
                        } else {
                            createModel = false;
                        }
                    }

                    if (createModel || placeholder.modelInstance == null) {
                        if (voxelDefinition.prefab == null)
                            continue;
                        placeholder.modelTemplate = voxelDefinition.prefab;
                        placeholder.modelInstance = Instantiate (voxelDefinition.prefab);
                        placeholder.modelInstance.name = "DynamicVoxelInstance";
                        // Note: placeHolder.modelInstance layer must be different from layerVoxels to allow dynamic voxels collide with terrain. So don't set its layer to layer voxels
                        placeholder.modelMeshRenderers = placeholder.modelInstance.GetComponentsInChildren<MeshRenderer> ();
                        if (voxelDefinition.gpuInstancing) {
                            placeholder.ToggleRenderers (false);
                        } else {
                            placeholder.modelMeshFilter = placeholder.modelInstance.GetComponentInChildren<MeshFilter> (true);
                        }

                        // Parent model to the placeholder
                        Transform tModel = placeholder.modelInstance.transform;
                        tModel.SetParent (placeholder.transform, false);
                        tModel.transform.localPosition = Misc.vector3zero;
                        tModel.transform.localScale = voxelDefinition.scale;

                    } else {
                        placeholder.ToggleRenderers (true);
                    }

                    if (voxelDefinition.gpuInstancing) {
                        rotation = placeholder.transform.localRotation;
                    } else {
                        // Adjust lighting
                        if (effectiveGlobalIllumination || chunk.voxels [miv.voxelIndex].isColored) {
                            // Update mesh colors
                            MeshFilter mf = placeholder.modelMeshFilter;
                            if (mf != null) {
                                Mesh mesh = mf.sharedMesh;
                                if (mesh != null) {
                                    Color32 tintColor = chunk.voxels [miv.voxelIndex].color;
                                    tintColor.a = (byte)(chunk.voxels[miv.voxelIndex].lightMesh + (chunk.voxels[miv.voxelIndex].torchLight << 4));
                                    if (placeholder.lastMivTintColor.a != tintColor.a || placeholder.lastMivTintColor.r != tintColor.r || placeholder.lastMivTintColor.g != tintColor.g || placeholder.lastMivTintColor.b != tintColor.b) {
                                        mf.sharedMesh = BakeMeshLighting(mf, tintColor);
                                        placeholder.lastMivTintColor = tintColor;
                                    }
                                }
                            }
                        }
                    }
                    if (!placeholder.modelInstance.gameObject.activeSelf) {
                        placeholder.modelInstance.gameObject.SetActive (true);
                    }
                    position = placeholder.transform.position;
                } else {
                    // pure gpu instancing, no gameobject

                    position = GetVoxelPosition (chunk, miv.voxelIndex);

                    rotation = voxelDefinition.GetRotation (position); // deterministic rotation
                                                                       // User rotation
                    float rot = chunk.voxels [miv.voxelIndex].GetTextureRotationDegrees ();
                    if (rot != 0) {
                        rotation *= Quaternion.Euler (0, rot, 0);
                    }

                    // Custom position
                    position += rotation * voxelDefinition.GetOffset (position);
                }

                if (voxelDefinition.gpuInstancing) {
                    instancedRenderer.AddVoxel (chunk, miv.voxelIndex, position, rotation, voxelDefinition.scale);
                }

            }
        }

        #endregion

    }



}
