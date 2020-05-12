using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;

namespace VoxelPlay
{

    [CustomEditor (typeof (VoxelPlayEnvironment))]
    public class VoxelPlayEnvironmentEditor : Editor
    {
        SerializedProperty world, enableBuildMode, buildMode, welcomeMessage, welcomeMessageDuration, renderInEditor, renderInEditorLowPriority, editorRenderDetail, characterController;
        SerializedProperty enableConsole, showConsole, enableInventory, enableStatusBar, enableLoadingPanel, loadingText, initialWaitTime, initialWaitText, loadSavedGame, saveFilename, enableDebugWindow, showFPS;
        SerializedProperty globalIllumination, ambientLight, daylightShadowAtten, enableSmoothLighting, enableFogSkyBlending, textureSize, enableShadows;
        SerializedProperty shadowsOnWater, realisticWater;
        SerializedProperty enableTinting, enableOutline, outlineColor, outlineThreshold, enableCurvature;
        SerializedProperty seeThrough, seeThroughTarget, seeThroughRadius, seeThroughHeightOffset;
        SerializedProperty enableReliefMapping, reliefStrength, reliefIterations, reliefIterationsBinarySearch;
        SerializedProperty enableBrightPointLights;
        SerializedProperty enableNormalMap, usePixelLights;
        SerializedProperty hqFiltering, mipMapBias, doubleSidedGlass, transparentBling, useComputeBuffers;
        SerializedProperty maxChunks, prewarmChunksInEditor, visibleChunksDistance, distanceAnchor, unloadFarChunks, adjustCameraFarClip, forceChunkDistance, maxCPUTimePerFrame, maxChunksPerFrame, maxTreesPerFrame, maxBushesPerFrame, lowMemoryMode, onlyRenderInFrustum;
#if !UNITY_WEBGL
        SerializedProperty multiThreadGeneration;
#endif
        SerializedProperty enableColliders, enableTrees, denseTrees, enableVegetation, enableNavMesh, hideChunksInHierarchy;
        SerializedProperty sun, fogAmount, fogDistance, fogUseCameraFarClip, fogFallOff, enableClouds;
        SerializedProperty uiCanvasPrefab, inputControllerPC, inputControllerMobile, crosshairPrefab, crosshairTexture, consoleBackgroundColor, statusBarBackgroundColor;
        SerializedProperty defaultBuildSound, defaultPickupSound, defaultImpactSound, defaultDestructionSound, defaultVoxel;
        SerializedProperty layerParticles, layerVoxels;
        SerializedProperty previewTouchUIinEditor;

        VoxelPlayEnvironment env;
        WorldDefinition cachedWorld;
        VoxelPlayTerrainGenerator cachedTerrainGenerator;
        Editor cachedWorldEditor, cachedTerrainGeneratorEditor;
        static GUIStyle titleLabelStyle, boxStyle;
        static bool worldExpand, terrainGeneratorExpand;
        static int cookieIndex = -1;
        Color titleColor;
        static GUIStyle sectionHeaderStyle;
        static bool expandQualitySection, expandVoxelGenerationSection, expandSkySection, expandInGameSection, expandDefaultsSection;
        bool enableCurvatureFromShader;
        string[] chunkSizeOptions;
        int[] chunkSizeValues;
        int chunkNewSize;
        string curvatureAmount;


        void OnEnable ()
        {
            titleColor = EditorGUIUtility.isProSkin ? new Color (0.52f, 0.66f, 0.9f) : new Color (0.12f, 0.16f, 0.4f);
            world = serializedObject.FindProperty ("world");
            enableBuildMode = serializedObject.FindProperty ("enableBuildMode");
            buildMode = serializedObject.FindProperty ("buildMode");
            welcomeMessage = serializedObject.FindProperty ("welcomeMessage");
            welcomeMessageDuration = serializedObject.FindProperty ("welcomeMessageDuration");
            renderInEditor = serializedObject.FindProperty ("renderInEditor");
            renderInEditorLowPriority = serializedObject.FindProperty ("renderInEditorLowPriority");
            editorRenderDetail = serializedObject.FindProperty ("editorRenderDetail");
            characterController = serializedObject.FindProperty ("characterController");
            enableConsole = serializedObject.FindProperty ("enableConsole");
            consoleBackgroundColor = serializedObject.FindProperty ("consoleBackgroundColor");
            showConsole = serializedObject.FindProperty ("showConsole");
            enableInventory = serializedObject.FindProperty ("enableInventory");
            prewarmChunksInEditor = serializedObject.FindProperty ("prewarmChunksInEditor");
            enableLoadingPanel = serializedObject.FindProperty ("enableLoadingPanel");
            loadingText = serializedObject.FindProperty ("loadingText");
            initialWaitTime = serializedObject.FindProperty ("initialWaitTime");
            initialWaitText = serializedObject.FindProperty ("initialWaitText");
            loadSavedGame = serializedObject.FindProperty ("loadSavedGame");
            saveFilename = serializedObject.FindProperty ("saveFilename");
            enableDebugWindow = serializedObject.FindProperty ("enableDebugWindow");
            showFPS = serializedObject.FindProperty ("showFPS");

            globalIllumination = serializedObject.FindProperty ("globalIllumination");
            ambientLight = serializedObject.FindProperty ("ambientLight");
            daylightShadowAtten = serializedObject.FindProperty ("daylightShadowAtten");
            enableSmoothLighting = serializedObject.FindProperty ("enableSmoothLighting");

            enableReliefMapping = serializedObject.FindProperty ("enableReliefMapping");
            reliefStrength = serializedObject.FindProperty ("reliefStrength");
            reliefIterations = serializedObject.FindProperty ("reliefIterations");
            reliefIterationsBinarySearch = serializedObject.FindProperty ("reliefIterationsBinarySearch");

            enableNormalMap = serializedObject.FindProperty ("enableNormalMap");
            usePixelLights = serializedObject.FindProperty ("usePixelLights");
            enableBrightPointLights = serializedObject.FindProperty ("enableBrightPointLights");

            enableFogSkyBlending = serializedObject.FindProperty ("enableFogSkyBlending");
            textureSize = serializedObject.FindProperty ("textureSize");
            realisticWater = serializedObject.FindProperty ("realisticWater");
            shadowsOnWater = serializedObject.FindProperty ("shadowsOnWater");
            enableShadows = serializedObject.FindProperty ("enableShadows");
            enableTinting = serializedObject.FindProperty ("enableTinting");
            enableCurvature = serializedObject.FindProperty ("enableCurvature");
            enableOutline = serializedObject.FindProperty ("enableOutline");
            outlineColor = serializedObject.FindProperty ("outlineColor");
            outlineThreshold = serializedObject.FindProperty ("outlineThreshold");
            doubleSidedGlass = serializedObject.FindProperty ("doubleSidedGlass");
            transparentBling = serializedObject.FindProperty ("transparentBling");
            hqFiltering = serializedObject.FindProperty ("hqFiltering");
            mipMapBias = serializedObject.FindProperty ("mipMapBias");
            useComputeBuffers = serializedObject.FindProperty ("useComputeBuffers");

            seeThrough = serializedObject.FindProperty ("seeThrough");
            seeThroughTarget = serializedObject.FindProperty ("seeThroughTarget");
            seeThroughRadius = serializedObject.FindProperty ("seeThroughRadius");
            seeThroughHeightOffset = serializedObject.FindProperty ("_seeThroughHeightOffset");

            maxChunks = serializedObject.FindProperty ("maxChunks");
            visibleChunksDistance = serializedObject.FindProperty ("_visibleChunksDistance");
            distanceAnchor = serializedObject.FindProperty ("distanceAnchor");
            unloadFarChunks = serializedObject.FindProperty ("unloadFarChunks");
            adjustCameraFarClip = serializedObject.FindProperty ("adjustCameraFarClip");

            forceChunkDistance = serializedObject.FindProperty ("forceChunkDistance");
            maxCPUTimePerFrame = serializedObject.FindProperty ("maxCPUTimePerFrame");
            maxChunksPerFrame = serializedObject.FindProperty ("maxChunksPerFrame");
            maxTreesPerFrame = serializedObject.FindProperty ("maxTreesPerFrame");
            maxBushesPerFrame = serializedObject.FindProperty ("maxBushesPerFrame");
#if !UNITY_WEBGL
            multiThreadGeneration = serializedObject.FindProperty ("multiThreadGeneration");
#endif
            lowMemoryMode = serializedObject.FindProperty ("lowMemoryMode");
            onlyRenderInFrustum = serializedObject.FindProperty ("onlyRenderInFrustum");
            enableColliders = serializedObject.FindProperty ("enableColliders");
            enableNavMesh = serializedObject.FindProperty ("enableNavMesh");
            hideChunksInHierarchy = serializedObject.FindProperty ("hideChunksInHierarchy");
            enableTrees = serializedObject.FindProperty ("enableTrees");
            denseTrees = serializedObject.FindProperty ("denseTrees");
            enableVegetation = serializedObject.FindProperty ("enableVegetation");

            sun = serializedObject.FindProperty ("sun");
            fogAmount = serializedObject.FindProperty ("fogAmount");
            fogDistance = serializedObject.FindProperty ("fogDistance");
            fogUseCameraFarClip = serializedObject.FindProperty ("fogUseCameraFarClip");
            fogFallOff = serializedObject.FindProperty ("fogFallOff");
            enableClouds = serializedObject.FindProperty ("enableClouds");

            uiCanvasPrefab = serializedObject.FindProperty ("UICanvasPrefab");
            inputControllerPC = serializedObject.FindProperty ("inputControllerPCPrefab");
            inputControllerMobile = serializedObject.FindProperty ("inputControllerMobilePrefab");
            crosshairPrefab = serializedObject.FindProperty ("crosshairPrefab");
            crosshairTexture = serializedObject.FindProperty ("crosshairTexture");

            enableStatusBar = serializedObject.FindProperty ("enableStatusBar");
            statusBarBackgroundColor = serializedObject.FindProperty ("statusBarBackgroundColor");

            layerParticles = serializedObject.FindProperty ("layerParticles");
            layerVoxels = serializedObject.FindProperty ("layerVoxels");

            defaultBuildSound = serializedObject.FindProperty ("defaultBuildSound");
            defaultPickupSound = serializedObject.FindProperty ("defaultPickupSound");
            defaultImpactSound = serializedObject.FindProperty ("defaultImpactSound");
            defaultDestructionSound = serializedObject.FindProperty ("defaultDestructionSound");
            defaultVoxel = serializedObject.FindProperty ("defaultVoxel");

            env = (VoxelPlayEnvironment)target;
            if (!Application.isPlaying) {
                if (!env.initialized && env.gameObject.activeInHierarchy) {
                    env.Init ();
                }
                env.WantRepaintInspector += this.Repaint;
            }

            worldExpand = EditorPrefs.GetBool ("VoxelPlayWorldSection", worldExpand);
            terrainGeneratorExpand = EditorPrefs.GetBool ("VoxelPlayTerrainGeneratorSection", terrainGeneratorExpand);

            expandQualitySection = EditorPrefs.GetBool ("VoxelPlayExpandQualitySection", false);
            expandVoxelGenerationSection = EditorPrefs.GetBool ("VoxelPlayVoxelGenerationSection", false);
            expandSkySection = EditorPrefs.GetBool ("VoxelPlaySkySection", false);
            expandInGameSection = EditorPrefs.GetBool ("VoxelPlayInGameSection", false);
            expandDefaultsSection = EditorPrefs.GetBool ("VoxelPlayDefaultsSection", false);

            enableCurvatureFromShader = "1".Equals (GetShaderOptionValue ("VOXELPLAY_CURVATURE", "VPCommonVertexModifier.cginc"));
            curvatureAmount = GetShaderOptionValue ("VOXELPLAY_CURVATURE_AMOUNT", "VPCommonVertexModifier.cginc");

            previewTouchUIinEditor = serializedObject.FindProperty ("previewTouchUIinEditor");

            chunkSizeOptions = new string [] { "16", "32" };
            chunkSizeValues = new int [] { 16, 32 };
            chunkNewSize = VoxelPlayEnvironment.CHUNK_SIZE;
        }

        void OnDisable ()
        {
            if (env != null) {
                env.WantRepaintInspector -= this.Repaint;
            }
            EditorPrefs.SetBool ("VoxelPlayExpandQualitySection", expandQualitySection);
            EditorPrefs.SetBool ("VoxelPlayVoxelGenerationSection", expandVoxelGenerationSection);
            EditorPrefs.SetBool ("VoxelPlaySkySection", expandSkySection);
            EditorPrefs.SetBool ("VoxelPlayInGameSection", expandInGameSection);
            EditorPrefs.SetBool ("VoxelPlayDefaultsSection", expandDefaultsSection);
        }


        public override void OnInspectorGUI ()
        {

#if UNITY_5_6_OR_NEWER
            serializedObject.UpdateIfRequiredOrScript ();
#else
			serializedObject.UpdateIfDirtyOrScript();
#endif
            if (boxStyle == null) {
                boxStyle = new GUIStyle (GUI.skin.box);
                boxStyle.padding = new RectOffset (15, 10, 5, 5);
            }
            if (titleLabelStyle == null) {
                titleLabelStyle = new GUIStyle (EditorStyles.label);
            }
            titleLabelStyle.normal.textColor = titleColor;
            titleLabelStyle.fontStyle = FontStyle.Bold;
            EditorGUIUtility.labelWidth = 150;
            if (sectionHeaderStyle == null) {
                sectionHeaderStyle = new GUIStyle (EditorStyles.foldout);
            }
            sectionHeaderStyle.SetFoldoutColor ();

            if (cookieIndex >= 0) {
                EditorGUILayout.Separator ();
                EditorGUILayout.LabelField ("Help Cookie", titleLabelStyle);
                EditorGUILayout.HelpBox (VoxelPlayCookie.GetCookie (cookieIndex), MessageType.Info);
                EditorGUILayout.BeginHorizontal ();
                GUILayout.Label ("  ");
                ShowHelpButtons (true);
                EditorGUILayout.EndHorizontal ();
            }

            EditorGUILayout.Separator ();

            EditorGUILayout.BeginHorizontal ();
            GUILayout.Label ("General Settings", titleLabelStyle);
            if (cookieIndex < 0)
                ShowHelpButtons (false);
            EditorGUILayout.EndHorizontal ();

            bool rebuildWorld = false;
            bool refreshChunks = false;
            bool reloadWorldTextures = false;
            bool updateSpecialFeaturesMacro = false;
            //			bool updateOutlineProperties = false;
            bool updateCurvatureMacro = false;
            bool prevBool;

            // General settings
            EditorGUILayout.BeginHorizontal ();
            WorldDefinition wd = (WorldDefinition)world.objectReferenceValue;
            EditorGUILayout.PropertyField (world, new GUIContent ("World", "The world definition asset. This asset contains the definition of biomes, voxels, items and other world-specific options."));
            if (wd != world.objectReferenceValue)
                rebuildWorld = true;
            if (GUILayout.Button ("Create", GUILayout.Width (50))) {
                CreateWorldDefinition ();
            }
            if (GUILayout.Button ("Locate", GUILayout.Width (50))) {
                Selection.activeObject = world.objectReferenceValue;
            }
            EditorGUILayout.EndHorizontal ();
            if (world.objectReferenceValue == null) {
                EditorGUILayout.HelpBox ("Create or assign a World Definition asset.", MessageType.Warning);
            }

            if (world.objectReferenceValue != null) {
                if (GUILayout.Button ("Expand/Collapse World Settings")) {
                    worldExpand = !worldExpand;
                    EditorPrefs.SetBool ("VoxelPlayWorldSection", worldExpand);
                }
                if (worldExpand) {
                    if (cachedWorld != world.objectReferenceValue) {
                        cachedWorldEditor = null;
                    }
                    if (cachedWorldEditor == null) {
                        cachedWorld = (WorldDefinition)world.objectReferenceValue;
                        cachedWorldEditor = Editor.CreateEditor (world.objectReferenceValue);
                    }

                    // Drawing the world editor
                    EditorGUILayout.BeginVertical (boxStyle);
                    EditorGUI.BeginChangeCheck ();
                    cachedWorldEditor.OnInspectorGUI ();
                    if (EditorGUI.EndChangeCheck()) {
                        env.UpdateMaterialProperties ();
                    }
                    EditorGUILayout.EndVertical ();
                    EditorGUILayout.Separator ();
                }

                VoxelPlayTerrainGenerator terrainGenerator = (VoxelPlayTerrainGenerator)((WorldDefinition)world.objectReferenceValue).terrainGenerator;
                if (terrainGenerator != null) {
                    if (GUILayout.Button ("Expand/Collapse Generator Settings")) {
                        terrainGeneratorExpand = !terrainGeneratorExpand;
                        EditorPrefs.SetBool ("VoxelPlayTerrainGeneratorSection", terrainGeneratorExpand);
                    }
                    if (terrainGeneratorExpand) {
                        if (terrainGenerator != cachedTerrainGenerator) {
                            cachedTerrainGeneratorEditor = null;
                        }
                        if (cachedTerrainGeneratorEditor == null) {
                            cachedTerrainGenerator = terrainGenerator;
                            cachedTerrainGeneratorEditor = Editor.CreateEditor (terrainGenerator);
                        }

                        // Drawing the world editor
                        EditorGUI.BeginChangeCheck ();
                        EditorGUILayout.BeginVertical (boxStyle);
                        cachedTerrainGeneratorEditor.OnInspectorGUI ();
                        EditorGUILayout.EndVertical ();
                        if (EditorGUI.EndChangeCheck ()) {
                            env.NotifyTerrainGeneratorConfigurationChanged ();
                            VoxelPlayBiomeExplorer.requestRefresh = true;
                            UnityEditorInternal.InternalEditorUtility.RepaintAllViews ();
                        }
                        EditorGUILayout.Separator ();
                    }
                }

                if (GUILayout.Button ("Open Biome Map Explorer")) {
                    VoxelPlayBiomeExplorer.ShowWindow ();
                }

                EditorGUILayout.BeginVertical (boxStyle);
                float half = EditorGUIUtility.currentViewWidth * 0.4f;
                EditorGUILayout.BeginHorizontal ();
                EditorGUILayout.Space ();
                if (GUILayout.Button ("Toggle Chunks", GUILayout.Width (half))) {
                    env.ChunksToggle ();
                    SceneView.RepaintAll ();
                }
                if (GUILayout.Button ("Delete Chunks", GUILayout.Width (half))) {
                    renderInEditor.boolValue = false;
                    env.DisposeAll ();
                }
                EditorGUILayout.Space ();
                EditorGUILayout.EndHorizontal ();
                EditorGUILayout.BeginHorizontal ();
                EditorGUILayout.Space ();
                if (GUILayout.Button ("Regenerate Terrain", GUILayout.Width (half))) {
                    renderInEditor.boolValue = true;
                    rebuildWorld = true;
                }
                GUI.enabled = env.chunksCreated > 0;
                if (GUILayout.Button ("Export Chunks", GUILayout.Width (half))) {
                    env.ChunksExport ();
                }
                GUI.enabled = true;
                EditorGUILayout.Space ();
                EditorGUILayout.EndHorizontal ();
                EditorGUILayout.Separator ();

                EditorGUILayout.PropertyField (renderInEditor, new GUIContent ("Render In Editor", "Enable world rendering in Editor. If disabled, world will only be visible during play mode."));
                if (!renderInEditor.boolValue)
                    GUI.enabled = false;
                EditorGUILayout.PropertyField (renderInEditorLowPriority, new GUIContent ("   Low Priority", "When enabled, rendering in editor will only execute when scene camera is static."));
                if (wd != world.objectReferenceValue)
                    rebuildWorld = true;

                prevBool = editorRenderDetail.boolValue;
                EditorGUILayout.PropertyField (editorRenderDetail, new GUIContent ("   Render Detail", "Select the amount of detail to be rendered in Editor time."));
                if (prevBool != editorRenderDetail.boolValue) {
                    rebuildWorld = true;
                }
                GUI.enabled = true;

                if (renderInEditor.boolValue) {

                    if (env.cameraMain != null) {
                        env.cameraMain.transform.position = EditorGUILayout.Vector3Field ("Main Cam Pos", env.cameraMain.transform.position);
                    }
                    if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null) {
                        EditorGUILayout.Vector3Field ("Scene Cam Pos", SceneView.lastActiveSceneView.camera.transform.position);
                    }
                    EditorGUILayout.BeginHorizontal ();
                    if (env.cameraMain != null) {
                        if (SceneView.lastActiveSceneView != null) {
                            if (GUILayout.Button ("Scene Cam To Surface")) {
                                Vector3 pos = Misc.vector3zero;
                                pos = env.cameraMain.transform.position;
                                pos.y = env.GetTerrainHeight (Vector3.zero, true);
                                SceneView.lastActiveSceneView.LookAt (pos + new Vector3 (50, 50, 50));
                            }
                            if (GUILayout.Button ("Find Main Cam")) {
                                Vector3 pos = env.cameraMain.transform.position + new Vector3 (50, 50, 50);
                                Vector3 fwd = (env.cameraMain.transform.position - pos).normalized;
                                SceneView.lastActiveSceneView.LookAt (pos, Quaternion.LookRotation (fwd));
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal ();
                }
                EditorGUILayout.EndVertical ();

            } else {
                renderInEditor.boolValue = false;
            }

            // Quality and effects
            EditorGUILayout.Separator ();
            expandQualitySection = EditorGUILayout.Foldout (expandQualitySection, "Quality And Effects", sectionHeaderStyle);
            if (expandQualitySection) {

                EditorGUILayout.BeginHorizontal ();
                EditorGUILayout.LabelField ("Preset", GUILayout.Width (120));
                if (GUILayout.Button (new GUIContent ("All Features", "Enables all engine visual features available for the active platform."))) {
                    globalIllumination.boolValue = true;
                    enableShadows.boolValue = true;
                    shadowsOnWater.boolValue = true;
                    enableSmoothLighting.boolValue = true;
                    enableFogSkyBlending.boolValue = true;
                    denseTrees.boolValue = true;
                    hqFiltering.boolValue = true;
                    usePixelLights.boolValue = true;
                    enableBrightPointLights.boolValue = true;
                    doubleSidedGlass.boolValue = true;
                    transparentBling.boolValue = true;
                    if (VoxelPlayFirstPersonController.instance != null) {
                        VoxelPlayFirstPersonController.instance.autoInvertColors = true;
                    }
                    rebuildWorld = true;
                }
                if (GUILayout.Button (new GUIContent ("Medium", "Disables shadows to improve performance but keeps global illumination."))) {
                    globalIllumination.boolValue = true;
                    enableShadows.boolValue = false;
                    shadowsOnWater.boolValue = false;
                    enableSmoothLighting.boolValue = true;
                    enableFogSkyBlending.boolValue = true;
                    enableReliefMapping.boolValue = false;
                    usePixelLights.boolValue = true;
                    enableBrightPointLights.boolValue = false;
                    denseTrees.boolValue = true;
                    hqFiltering.boolValue = true;
                    doubleSidedGlass.boolValue = true;
                    transparentBling.boolValue = true;
                    if (VoxelPlayFirstPersonController.instance != null) {
                        VoxelPlayFirstPersonController.instance.autoInvertColors = true;
                    }
                    rebuildWorld = true;
                }
                if (GUILayout.Button (new GUIContent ("Fastest", "Disables all effects to improve performance."))) {
                    globalIllumination.boolValue = false;
                    enableShadows.boolValue = false;
                    shadowsOnWater.boolValue = false;
                    enableSmoothLighting.boolValue = false;
                    enableFogSkyBlending.boolValue = false;
                    enableReliefMapping.boolValue = false;
                    enableNormalMap.boolValue = false;
                    usePixelLights.boolValue = false;
                    enableBrightPointLights.boolValue = false;
                    denseTrees.boolValue = false;
                    hqFiltering.boolValue = false;
                    doubleSidedGlass.boolValue = false;
                    onlyRenderInFrustum.boolValue = true;
                    transparentBling.boolValue = false;
                    if (VoxelPlayFirstPersonController.instance != null) {
                        VoxelPlayFirstPersonController.instance.autoInvertColors = false;
                    }
                    if (visibleChunksDistance.intValue > 6) {
                        visibleChunksDistance.intValue = 6;
                    }
                    if (forceChunkDistance.intValue > 2) {
                        forceChunkDistance.intValue = 2;
                    }
                    if (maxChunks.intValue > 5000) {
                        maxChunks.intValue = 5000;
                    }
                    rebuildWorld = true;
                }
                EditorGUILayout.EndHorizontal ();

                prevBool = globalIllumination.boolValue;
                EditorGUILayout.PropertyField (globalIllumination, new GUIContent ("Global Illumination", "Enables Voxel Play's own lightmap computation. This option adds smooth shading and lighting in combination with Unity shadow system."));
                if (globalIllumination.boolValue != prevBool)
                    refreshChunks = true;

                EditorGUI.BeginChangeCheck ();
                GUI.enabled = SystemInfo.supportsComputeShaders;
                if (!GUI.enabled) {
                    EditorGUILayout.BeginHorizontal ();
                    EditorGUILayout.LabelField (new GUIContent ("Compute Buffers", "Enables compute buffers for custom voxels. This option requires GPU capable of Shader Model 4.5."), GUILayout.Width (EditorGUIUtility.labelWidth));
                    EditorGUILayout.LabelField ("(Unsupported platform or graphics API)");
                    EditorGUILayout.EndHorizontal ();
                } else {
                    EditorGUILayout.PropertyField (useComputeBuffers, new GUIContent ("Compute Buffers", "Enables compute buffers for custom voxels. This option requires GPU capable of Shader Model 4.5."));
                }
                if (EditorGUI.EndChangeCheck ()) {
                    rebuildWorld = true;
                }
                GUI.enabled = true;
                EditorGUI.BeginChangeCheck ();
                EditorGUILayout.PropertyField (enableShadows, new GUIContent ("Enable Shadows", "Turns on/off shadow casting and receiving on voxels."));
                if (EditorGUI.EndChangeCheck ()) {
                    rebuildWorld = true;
                }
                if (!enableShadows.boolValue) {
                    CheckMainLightShadows ();
                }
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(shadowsOnWater, new GUIContent("Shadows On Water", "Enables shadow receiving on water surface."));
                EditorGUILayout.PropertyField(realisticWater, new GUIContent("Realistic Water", "Uses a realistic water shader."));
                if (EditorGUI.EndChangeCheck()) {
                    rebuildWorld = true;
                }
                prevBool = enableSmoothLighting.boolValue;
                EditorGUILayout.PropertyField (enableSmoothLighting, new GUIContent ("Smooth Lighting", "Interpolates lighting between voxel vertices. Also includes ambient occlusion."));
                if (enableSmoothLighting.boolValue != prevBool)
                    refreshChunks = true;
                EditorGUILayout.PropertyField (ambientLight, new GUIContent ("Ambient Light", "Minimum amount of light in the scene affecting the voxels."));
                EditorGUILayout.PropertyField (daylightShadowAtten, new GUIContent ("Daylight Shadow Atten", "Shadow attenuation factor when Sun is high. Set this value to 0 to preserve standard shadow intensity. A value of 1 will make shadows disappear when Sun is on top. A middle value will make shadows more intense when Sun is low in the sky and more subtle when Sun is high."));
                EditorGUILayout.PropertyField (textureSize, new GUIContent ("Texture Size", "Texture size should be a multiple of 2 (eg. 16, 32, 64, 128)"));
                prevBool = enableTinting.boolValue;
                EditorGUILayout.PropertyField (enableTinting, new GUIContent ("Enable Tinting", "Enables individual voxel tint color."));
                if (prevBool != enableTinting.boolValue) {
                    refreshChunks = true;
                    updateSpecialFeaturesMacro = true;
                }

                EditorGUI.BeginChangeCheck ();
                EditorGUILayout.PropertyField (doubleSidedGlass, new GUIContent ("Double Sided Glass", "Renders both sides of transparent voxels."));
                EditorGUILayout.PropertyField (transparentBling, new GUIContent ("Transparent Bling", "Enables shining effect on transparent voxels."));
                if (EditorGUI.EndChangeCheck ()) {
                    rebuildWorld = true;
                }

                EditorGUILayout.PropertyField (enableOutline, new GUIContent ("Outline", "Enables outline effect on solid voxels."));
                if (enableOutline.boolValue) {
                    EditorGUI.BeginChangeCheck ();
                    EditorGUILayout.PropertyField (outlineColor, new GUIContent ("   Color", "Outline color and alpha."));
                    EditorGUILayout.PropertyField (outlineThreshold, new GUIContent ("   Threshold", "Controls outline width."));
                }
                prevBool = enableNormalMap.boolValue;
                EditorGUILayout.PropertyField (enableNormalMap, new GUIContent ("Normal Mapping", "Enables use of normal maps."));
                if (prevBool != enableNormalMap.boolValue) {
                    refreshChunks = true;
                    reloadWorldTextures = true;
                }
                prevBool = enableReliefMapping.boolValue;
                EditorGUILayout.PropertyField (enableReliefMapping, new GUIContent ("Relief Mapping", "Enables parallax occlusion/relief mapping."));
                if (prevBool != enableReliefMapping.boolValue) {
                    refreshChunks = true;
                    reloadWorldTextures = true;
                }
                if (enableReliefMapping.boolValue) {
                    EditorGUILayout.PropertyField (reliefStrength, new GUIContent ("   Strength", "Strength of the parallax effect."));
                    EditorGUILayout.PropertyField (reliefIterations, new GUIContent ("   Iterations", "Max number of ray-marching steps."));
                    EditorGUILayout.PropertyField (reliefIterationsBinarySearch, new GUIContent ("   Binary Search Iterations", "Max number of binary search iterations to precisely find the intersection point."));
                }
                GUI.enabled = !enableReliefMapping.boolValue;
                prevBool = hqFiltering.boolValue;
                EditorGUILayout.PropertyField (hqFiltering, new GUIContent ("HQ Filtering", "Enables mipmapping and intergrated texel antialiasing."));
                if (prevBool != hqFiltering.boolValue) {
                    refreshChunks = true;
                    reloadWorldTextures = true;
                }
                if (hqFiltering.boolValue) {
                    float prevFloat = mipMapBias.floatValue;
                    EditorGUILayout.PropertyField (mipMapBias, new GUIContent ("   MipMap Bias", "Increase to reduce texture blurring."));
                    if (mipMapBias.floatValue != prevFloat) {
                        refreshChunks = true;
                        reloadWorldTextures = true;
                    }
                }
                GUI.enabled = true;
                prevBool = usePixelLights.boolValue;
                EditorGUILayout.PropertyField (usePixelLights, new GUIContent ("Per-Pixel Lighting", "If disabled, lighting will be calculated per-vertex."));
                if (prevBool != usePixelLights.boolValue) {
                    refreshChunks = true;
                    reloadWorldTextures = true;
                }

                EditorGUI.BeginChangeCheck ();
                EditorGUILayout.PropertyField (enableBrightPointLights, new GUIContent ("Bright Point Lights", "Improves appearance of point lights."));
                if (EditorGUI.EndChangeCheck()) {
                    refreshChunks = true;
                    updateSpecialFeaturesMacro = true;
                }

                GUI.enabled = !Application.isPlaying;
                EditorGUI.BeginChangeCheck ();
                enableCurvatureFromShader = EditorGUILayout.Toggle (new GUIContent ("Curvature", "Enables curvature vertex modifier in VoxelPlay shaders."), enableCurvatureFromShader);
                enableCurvature.boolValue = enableCurvatureFromShader;
                if (EditorGUI.EndChangeCheck ()) {
                    updateCurvatureMacro = true;
                    rebuildWorld = true;
                }
                if (enableCurvatureFromShader) {
                    if (!enableSmoothLighting.boolValue) {
                        EditorGUILayout.HelpBox ("It's recommended to enable Smooth Lighting option to avoid vertex artifacts due to curvature.", MessageType.Warning);
                    }
                    EditorGUILayout.BeginHorizontal ();
                    curvatureAmount = EditorGUILayout.TextField (new GUIContent ("   Amount", "Vertex shift amount multiplier."), curvatureAmount);
                    if (GUILayout.Button ("Update", GUILayout.Width (65))) {
                        updateCurvatureMacro = true;
                        rebuildWorld = true;
                    }
                    EditorGUILayout.EndHorizontal ();
                }
                GUI.enabled = true;

                prevBool = seeThrough.boolValue;
                EditorGUILayout.PropertyField (seeThrough, new GUIContent ("See Through", "Hides voxels between camera and desired target. This option is designed for third person perspective."));
                if (prevBool != seeThrough.boolValue) {
                    updateSpecialFeaturesMacro = true;
                }
                if (seeThrough.boolValue) {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField (seeThroughTarget, new GUIContent ("Target", "The target gameobject. Usually this is the character controller or player gameobject."));
                    EditorGUILayout.PropertyField (seeThroughRadius, new GUIContent ("Radius", "Radius of effect. No voxels will be visible within this distance to the target."));
                    EditorGUILayout.PropertyField (seeThroughHeightOffset, new GUIContent ("Height Offset", "Voxels below target plus this height offset won't be hidden. This option avoids hiding the ground."));
                    EditorGUI.indentLevel--;
                }
            }
            // Voxel Generation
            EditorGUILayout.Separator ();
            expandVoxelGenerationSection = EditorGUILayout.Foldout (expandVoxelGenerationSection, "Voxel Generation", sectionHeaderStyle);
            if (expandVoxelGenerationSection) {
                ShowProgressBar ("Chunk Rendering: Pending (" + env.chunksInRenderQueueCount + ") / Drawn (" + env.chunksDrawn + ")", (env.chunksDrawn + 1f) / (env.chunksDrawn + env.chunksInRenderQueueCount + 1f));
                if (env.enableTrees) {
                    ShowProgressBar ("Tree Creation: Pending (" + env.treesInCreationQueueCount + ") / Created (" + env.treesCreated + ")", (env.treesCreated + 1f) / (env.treesCreated + env.treesInCreationQueueCount + 1f));
                } else {
                    ShowProgressBar ("Tree Creation: ---", 1f);
                }
                if (env.enableVegetation) {
                    ShowProgressBar ("Bush Creation: Pending (" + env.vegetationInCreationQueueCount + ") / Created (" + env.vegetationCreated + ")", (env.vegetationCreated + 1f) / (env.vegetationCreated + env.vegetationInCreationQueueCount + 1f));
                } else {
                    ShowProgressBar ("Bush Creation: ---", 1f);
                }
                EditorGUILayout.LabelField ("Total Chunks Created", env.chunksCreated.ToString ());
                EditorGUILayout.LabelField (new GUIContent ("Total Voxels Created", "Number of voxels that contribute to mesh generation. Fully surrounded voxels are hidden and are not included."), new GUIContent (env.voxelsCreatedCount.ToString ()));

                EditorGUILayout.PropertyField (maxChunks, new GUIContent ("Chunks Pool Size", "Number of total chunks allowed in memory."));
                EditorGUILayout.LabelField ("   Recommended >=", env.maxChunksRecommended.ToString ());
                EditorGUILayout.LabelField ("   Used", env.chunksUsed.ToString () + " (" + (env.chunksUsed * 100f / env.maxChunks).ToString ("F1") + "% Pool)");
                EditorGUILayout.IntSlider (prewarmChunksInEditor, 1000, maxChunks.intValue, new GUIContent ("   Prewarm In Editor", "Number of chunks that will be reserved during start in Unity Editor before game starts. In the final build, all chunks are reserved before game starts to provide a smooth gameplay experience."));
                EditorGUILayout.PropertyField (enableLoadingPanel, new GUIContent ("   Loading Screen", "Shows a loading panel during start up while chunks are being reserved."));
                if (enableLoadingPanel.boolValue) {
                    EditorGUILayout.PropertyField (loadingText, new GUIContent ("      Text", "Text to show while initializing the engine."));
                }
                EditorGUILayout.PropertyField (initialWaitTime, new GUIContent ("   Initial Wait Time", "Additional seconds to wait before loading screen is removed."));
                if (initialWaitTime.floatValue > 0) {
                    EditorGUILayout.PropertyField (initialWaitText, new GUIContent ("      Text", "Text to show diring the additional wait time."));
                }
                EditorGUILayout.Separator ();
                EditorGUILayout.BeginHorizontal ();
                chunkNewSize = EditorGUILayout.IntPopup ("Chunk Size", chunkNewSize, chunkSizeOptions, chunkSizeValues);
                GUI.enabled = chunkNewSize != VoxelPlayEnvironment.CHUNK_SIZE;
                if (GUILayout.Button("Change", GUILayout.Width(80))) {
                    ChangeChunkSize ();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal ();
                EditorGUILayout.PropertyField (lowMemoryMode, new GUIContent ("Low Memory Mode", "When enabled, internal buffers size is reduced to the minimum. GC spikes can occur if a buffer needs resizing. Enable this option to reduce memory pressure warnings on mobile devices."));
                EditorGUILayout.PropertyField (onlyRenderInFrustum, new GUIContent ("Only Render In Frustum", "When enabled, only chunks inside the camera frustum will be rendered."));
#if UNITY_WEBGL
				GUI.enabled = false;
				EditorGUILayout.BeginHorizontal ();
				EditorGUILayout.LabelField ("Multi Thread Generation", GUILayout.Width (EditorGUIUtility.labelWidth));
				EditorGUILayout.LabelField ("(Unsupported platform)");
				EditorGUILayout.EndHorizontal ();
				GUI.enabled = true;
#else
                EditorGUILayout.PropertyField (multiThreadGeneration, new GUIContent ("Multi Thread Generation", "When enabled, uses a dedicated background thread for chunk generation (only in build, deactivated while running inside Unity Editor)."));
#endif
                EditorGUILayout.PropertyField (visibleChunksDistance, new GUIContent ("Visible Chunk Distance", "Measured in number of chunks."));
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField (distanceAnchor, new GUIContent ("Distance Anchor", "Where the distance is computed from. Usually this is the camera (in first person view) or the character (in third person view)."));
                EditorGUILayout.PropertyField (unloadFarChunks, new GUIContent ("Unload Far Chunks", "Disable chunk gameobject when it's out of visible distance. Enable it again when it enters the visible distance."));
                EditorGUILayout.PropertyField (adjustCameraFarClip, new GUIContent ("Adjust Cam Far Clip", "Adjusts camera's far clipping plane to visible chunk distance automatically."));
                EditorGUI.indentLevel--;
                EditorGUILayout.PropertyField (forceChunkDistance, new GUIContent ("Force Chunk Distance", "Distance measured in chunks that will be rendered completely before starting the game."));
                EditorGUILayout.PropertyField (maxCPUTimePerFrame, new GUIContent ("Max CPU Time Per Frame", "Maximum milliseconds that can be used by the CPU per frame to generate the world."));
                EditorGUILayout.PropertyField (maxChunksPerFrame, new GUIContent ("Max Chunks Per Frame", "Maximum number of chunks that can be generated in a single frame (0 = unlimited (limited only by the maxCPUTimePerFrame value)"));
                EditorGUILayout.PropertyField (maxTreesPerFrame, new GUIContent ("Max Trees Per Frame", "Maximum number of trees that can be generated in a single frame  (0 = unlimited (limited only by the maxCPUTimePerFrame value)"));
                EditorGUILayout.PropertyField (maxBushesPerFrame, new GUIContent ("Max Bushes Per Frame", "Maximum number of bushes that can be generated in a single frame  (0 = unlimited (limited only by the maxCPUTimePerFrame value)"));
                prevBool = enableColliders.boolValue;
                EditorGUILayout.PropertyField (enableColliders, new GUIContent ("Colliders", "Enables/disables collider generation for opaque voxels."));
                if (enableColliders.boolValue != prevBool) {
                    rebuildWorld = true;
                }
                prevBool = enableNavMesh.boolValue;
                EditorGUILayout.PropertyField (enableNavMesh, new GUIContent ("NavMesh", "Enables/disables NavMesh generation."));
                if (enableNavMesh.boolValue) {
                    EditorGUILayout.HelpBox ("Experimental feature: NavMesh generation is not yet optimized for big/complex worlds.", MessageType.Warning);
                }
                if (enableNavMesh.boolValue != prevBool)
                    rebuildWorld = true;
                prevBool = hideChunksInHierarchy.boolValue;
                EditorGUILayout.PropertyField (hideChunksInHierarchy, new GUIContent ("Hide Chunks Hierarchy", "Do not show chunks in hierarchy (this option has no effect in a build)"));
                if (hideChunksInHierarchy.boolValue != prevBool) {
                    rebuildWorld = true;
                }
                prevBool = enableTrees.boolValue;
                EditorGUILayout.PropertyField (enableTrees, new GUIContent ("Trees", "Enables/disables tree generation."));
                if (enableTrees.boolValue != prevBool)
                    rebuildWorld = true;
                if (enableTrees.boolValue) {
                    prevBool = denseTrees.boolValue;
                    EditorGUILayout.PropertyField (denseTrees, new GUIContent ("   Dense Trees", "If enabled, disables adjacent voxel occlusion making tree leaves cutout denser."));
                    if (denseTrees.boolValue != prevBool)
                        refreshChunks = true;
                }
                prevBool = enableVegetation.boolValue;
                EditorGUILayout.PropertyField (enableVegetation, new GUIContent ("Vegetation", "Enables/disables bush generation."));
                if (enableVegetation.boolValue != prevBool)
                    rebuildWorld = true;
                layerParticles.intValue = EditorGUILayout.LayerField (new GUIContent ("Particles Layer", "The layer used for particles. Used to optimize physics and avoid particle collision between them."), layerParticles.intValue);
                layerVoxels.intValue = EditorGUILayout.LayerField (new GUIContent ("Voxels Layer", "The layer used for voxels. Used to optimize physics and avoid voxels collision between them."), layerVoxels.intValue);
            }

            // Sky Properties
            EditorGUILayout.Separator ();
            expandSkySection = EditorGUILayout.Foldout (expandSkySection, "Sky Properties", sectionHeaderStyle);
            if (expandSkySection) {
                EditorGUILayout.PropertyField (sun, new GUIContent ("Sun", "Assigns the directional light used as the Sun."));
                EditorGUILayout.PropertyField (enableFogSkyBlending, new GUIContent ("Enable Fog", "Enabled fog/sky blending."));
                GUI.enabled = enableFogSkyBlending.boolValue;
                EditorGUILayout.PropertyField (fogAmount, new GUIContent ("   Fog Height", "Amount of fog."));
                if (fogUseCameraFarClip.boolValue)
                    GUI.enabled = false;
                EditorGUILayout.PropertyField (fogDistance, new GUIContent ("   Fog Distance", "Fog's distance factor"));
                GUI.enabled = true;
                EditorGUILayout.BeginHorizontal ();
                EditorGUILayout.PropertyField (fogUseCameraFarClip, new GUIContent ("   Use Camera Far Clip", "Adjust fog distance to match camera's far clipping plane distance."));
                if (env.cameraMain != null) {
                    EditorGUILayout.LabelField ("(Currently: " + env.cameraMain.farClipPlane + ")");
                }
                EditorGUILayout.EndHorizontal ();
                EditorGUILayout.PropertyField (fogFallOff, new GUIContent ("   Fog Fall Off", "Fog's fall off factor"));
                GUI.enabled = true;
                EditorGUILayout.PropertyField (enableClouds, new GUIContent ("Enable Clouds", "Clouds generation on/off"));
            }

            EditorGUILayout.Separator ();
            expandInGameSection = EditorGUILayout.Foldout (expandInGameSection, "Optional Game Features", sectionHeaderStyle);
            if (expandInGameSection) {
                EditorGUILayout.PropertyField (characterController, new GUIContent ("Character Controller", "A reference to the character controller script in the Voxel Play FPS Controller game object. It will be set automatically when you add the Voxel Play FPS Controller to the scene."));
                GUI.enabled = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android || EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS;
                EditorGUILayout.PropertyField (previewTouchUIinEditor, new GUIContent ("Preview Mobile UI in Editor", "Shows mobile UI in Editor when targeting a mobile platform."));
                GUI.enabled = true;
                EditorGUILayout.PropertyField (enableBuildMode, new GUIContent ("Enable Build Mode", "Enables entering Build Mode by pressing key B. In build mode, all world items are available in the inventory in unlimited amount and anything can be destroyed with a single hit. Player is also indestructible."));
                GUI.enabled = enableBuildMode.boolValue;
                EditorGUILayout.PropertyField (buildMode, new GUIContent ("   Build Mode ON", "Activates build mode."));
                GUI.enabled = true;
                EditorGUILayout.PropertyField (enableConsole, new GUIContent ("Enable Console", "Enables console system. Shows when pressing F1."));
                GUI.enabled = enableConsole.boolValue;
                EditorGUILayout.PropertyField (showConsole, new GUIContent ("   Visible", "Toggles console visibility on/off. The console shows useful data for debugging purposes."));
                EditorGUILayout.PropertyField (consoleBackgroundColor, new GUIContent ("   Background Color"));
                GUI.enabled = true;
                EditorGUILayout.PropertyField (enableStatusBar, new GUIContent ("Enable Status Bar"));
                GUI.enabled = enableStatusBar.boolValue;
                EditorGUILayout.PropertyField (statusBarBackgroundColor, new GUIContent ("   Status Bar Color"));
                GUI.enabled = true;
                EditorGUILayout.PropertyField (enableInventory, new GUIContent ("Enable Inventory", "Enables inventory UI when pressing Tab. Disable if you wish to provide your own interface."));
                EditorGUILayout.PropertyField (enableDebugWindow, new GUIContent ("Enable Debug Window", "Enables debug window toggling using F2."));
                EditorGUILayout.PropertyField (showFPS, new GUIContent ("Show FPS", "Shows FPS on top/right screen corner."));
                EditorGUILayout.PropertyField (loadSavedGame, new GUIContent ("Load Saved Game", "If Voxel Play should load a previously saved game at start up. Specify name of saved game in 'Save Filename' field."));
                EditorGUILayout.PropertyField (saveFilename, new GUIContent ("   Filename", "The current name for the saved game file. Used at runtime when pressing F3 to load or F4 to save. You can set a different save filename at runtime to support multiple save slots."));
            }

            EditorGUILayout.Separator ();
            expandDefaultsSection = EditorGUILayout.Foldout (expandDefaultsSection, "Default Assets", sectionHeaderStyle);
            if (expandDefaultsSection) {
                EditorGUILayout.PropertyField (defaultBuildSound, new GUIContent ("Build Sound", "Default sound played when an item or voxel is placed in the scene."));
                EditorGUILayout.PropertyField (defaultPickupSound, new GUIContent ("Pick Up Sound", "Default sound played when an item is collected."));
                EditorGUILayout.PropertyField (defaultImpactSound, new GUIContent ("Impact Sound", "Default sound played when a voxel is hit."));
                EditorGUILayout.PropertyField (defaultDestructionSound, new GUIContent ("Destruction Sound", "Default sound played when a voxel is destroyed."));
                EditorGUILayout.PropertyField (defaultVoxel, new GUIContent ("Default Voxel", "Assumed voxel when the voxel definition is missing or placing colors directly on the positions."));
                EditorGUILayout.PropertyField (uiCanvasPrefab, new GUIContent ("UI Prefab", "The canvas prefab used for the game main interface. This interface has elements for inventory, selected item, crosshair and other information."));
                EditorGUILayout.PropertyField (inputControllerPC, new GUIContent ("Input Prefab (PC)", "The prefab that contains the input controller script for PC."));
                EditorGUILayout.PropertyField (inputControllerMobile, new GUIContent ("Input Prefab (Mobile)", "The prefab that contains the input controller script for mobile."));
                EditorGUILayout.PropertyField (welcomeMessage, new GUIContent ("Welcome Text", "Optional message shown when game starts"));
                EditorGUILayout.PropertyField (welcomeMessageDuration, new GUIContent ("Welcome Duration", "Duration for the welcome text"));
                EditorGUILayout.PropertyField (crosshairPrefab, new GUIContent ("Crosshair Prefab", "The prefab used for the crosshair."));
                EditorGUILayout.PropertyField (crosshairTexture, new GUIContent ("Crosshair Texture", "The texture used for the crosshair."));
            }

            EditorGUILayout.Separator ();

            if (GUILayout.Button ("Import Models...")) {
                VoxelPlayImportTools.ShowWindow ();
            }

            EditorGUILayout.Separator ();

            if (serializedObject.ApplyModifiedProperties () || rebuildWorld || (Event.current.type == EventType.ExecuteCommand &&
                Event.current.commandName == "UndoRedoPerformed")) {
                if (updateSpecialFeaturesMacro) {
                    Debug.Log ("Optimization: modifying scripts/shaders macros to reflect special feature change...");
                    env.UpdateSpecialFeaturesCodeMacro ();
                    EditorGUIUtility.ExitGUI ();
                    return;
                }
                if (updateCurvatureMacro) {
                    UpdateCurvatureMacro ();
                    EditorGUIUtility.ExitGUI ();
                    return;
                }
                if (env.gameObject.activeInHierarchy) {
                    if (Application.isPlaying || env.renderInEditor) {
                        if (rebuildWorld) {
                            rebuildWorld = false;
                            env.ReloadWorld ();

                            // Check if scene camera is under terrain
                            if (!Application.isPlaying && env.renderInEditor && SceneView.lastActiveSceneView != null) {
                                Camera cam = SceneView.lastActiveSceneView.camera;
                                if (cam != null) {
                                    Vector3 camPos = SceneView.lastActiveSceneView.pivot;
                                    float h = env.GetTerrainHeight (camPos, true);
                                    if (camPos.y < h + 2) {
                                        camPos.y = h + 2;
                                    } else if (camPos.y > h + 100) {
                                        camPos.y = h + 50f;
                                    }
                                    SceneView.lastActiveSceneView.LookAt (camPos);
                                }
                            }
                        } else if (refreshChunks) {
                            refreshChunks = false;
                            env.Redraw (reloadWorldTextures);
                        }
                        env.UpdateMaterialProperties ();
                    }
                }

                EditorApplication.update -= env.UpdateInEditor;

                if (renderInEditor.boolValue) {
                    EditorApplication.update += env.UpdateInEditor;
                }
            }
        }

        void ShowHelpButtons (bool showHideButton)
        {
            if (showHideButton && GUILayout.Button ("Hide Cookie", GUILayout.Width (80))) {
                cookieIndex = -1;
                EditorGUIUtility.ExitGUI ();
            }
            if (GUILayout.Button ("New Cookie", GUILayout.Width (80))) {
                cookieIndex++;
            }
            if (GUILayout.Button ("Help", GUILayout.Width (40))) {
                if (!EditorUtility.DisplayDialog ("Voxel Play", "To learn more about a property in this inspector move the mouse over the label for a quick description (tooltip).\n\nPlease check the online Developer Guide on kronnect.com for more details and contact support by email or visiting our support forum on kronnect.com if you need help.\n\nIf you like Voxel Play, please rate it on the Asset Store.", "Close", "Visit Support Forum")) {
                    Application.OpenURL ("http://kronnect.me/taptapgo/index.php/board,56.0.html");
                }
            }
        }


        void ShowProgressBar (string text, float progress)
        {
            Rect r = EditorGUILayout.BeginVertical ();
            EditorGUI.ProgressBar (r, progress, text);
            GUILayout.Space (18);
            EditorGUILayout.EndVertical ();
        }


        void CreateWorldDefinition ()
        {
            WorldDefinition wd = ScriptableObject.CreateInstance<WorldDefinition> ();
            wd.name = "New World Definition";
            AssetDatabase.CreateAsset (wd, "Assets/" + wd.name + ".asset");
            AssetDatabase.SaveAssets ();
            world.objectReferenceValue = wd;
            EditorGUIUtility.PingObject (wd);
        }


        string GetShaderOptionValue (string option, string file)
        {
            string [] res = Directory.GetFiles (Application.dataPath, file, SearchOption.AllDirectories);
            string path = null;
            for (int k = 0; k < res.Length; k++) {
                if (res [k].Contains ("Voxel Play")) {
                    path = res [k];
                    break;
                }
            }
            if (path == null) {
                Debug.LogError (file + " could not be found!");
                return "";
            }

            string [] code = File.ReadAllLines (path, System.Text.Encoding.UTF8);
            string searchToken = "#define " + option;
            for (int k = 0; k < code.Length; k++) {
                if (code [k].Contains (searchToken)) {
                    string [] values = code [k].Trim ().Split ((char [])null, StringSplitOptions.RemoveEmptyEntries);
                    if (values.Length == 3) {
                        return values [2];
                    }
                    break;
                }
            }
            return "";
        }

        void SetShaderOptionValue (string option, string file, string value)
        {
            string [] res = Directory.GetFiles (Application.dataPath, file, SearchOption.AllDirectories);
            string path = null;
            for (int k = 0; k < res.Length; k++) {
                if (res [k].Contains ("Voxel Play")) {
                    path = res [k];
                    break;
                }
            }
            if (path == null) {
                Debug.LogError (file + " could not be found!");
                return;
            }

            string [] code = File.ReadAllLines (path, System.Text.Encoding.UTF8);
            string searchToken = "#define " + option;
            for (int k = 0; k < code.Length; k++) {
                if (code [k].Contains (searchToken)) {
                    code [k] = "#define " + option + " " + value;
                    File.WriteAllLines (path, code, System.Text.Encoding.UTF8);
                    break;
                }
            }
        }

        public void UpdateCurvatureMacro ()
        {
            env.SetShaderOptionValue ("VOXELPLAY_CURVATURE", "VPCommonVertexModifier.cginc", enableCurvature.boolValue ? "1" : "0");
            env.SetShaderOptionValue ("VOXELPLAY_CURVATURE_AMOUNT", "VPCommonVertexModifier.cginc", curvatureAmount);
            Debug.Log ("Voxel Play shaders updated.");
            AssetDatabase.Refresh ();
        }

        void CheckMainLightShadows ()
        {
            Light [] lights = FindObjectsOfType<Light> ();
            for (int k = 0; k < lights.Length; k++) {
                if (lights [k].isActiveAndEnabled && lights [k].shadows != LightShadows.None) {
                    EditorGUILayout.HelpBox ("Light '" + lights [k].name + "' currently is configured to cast shadows. Consider disabling shadows on your lights as well to improve performance.", MessageType.Info);
                }
            }
        }

        void ChangeChunkSize()
        {
            if (!EditorUtility.DisplayDialog("Change Chunk Size", "Please note that saved games with different chunk sizes cannot be loaded.\nThe view distance and chunk pool size will be adjusted to reflect the new chunk size.\n\nDo you want to change the chunk size? (it won't modify any saved game).", "Yes", "No")) {
                return;
            }
            int newVisibleDistance = visibleChunksDistance.intValue * VoxelPlayEnvironment.CHUNK_SIZE / chunkNewSize;
            newVisibleDistance = Mathf.Clamp (newVisibleDistance, 1, 25);
            visibleChunksDistance.intValue = newVisibleDistance;
            maxChunks.intValue = env.maxChunksRecommended;
            serializedObject.ApplyModifiedProperties ();
            env.UpdateChunkSizeInCode (chunkNewSize);
            Debug.Log ("New chunk size updated.");
            AssetDatabase.Refresh ();
            EditorGUIUtility.ExitGUI ();
        }


    }

}
