- ************************************
*            Voxel Play          *
*     by Ramiro Oliva (Kronnect)   * 
*            README FILE           *
************************************


What's Voxel Play?
--------------------

Voxel Play is a voxelized environment for your game. It aims to provide a complete solution for terrain, sky, water, UI, inventory and character interaction.


How to use this asset
---------------------
Firstly, you should run the Demo scenes to get an idea of the overall functionality.
Then, please take a look at the online documentation to learn how to use all the features that Voxel Play can offer.

Documentation/API reference
---------------------------
The user manual is available online:
https://kronnect.freshdesk.com/support/home

You can find internal development notes in the Documentation folder.


Support
-------
Please read the documentation and browse/play with the demo scene and sample source code included before contacting us for support :-)

* E-mail support: contact@kronnect.me
* Support forum: http://kronnect.me
* Twitter: @KronnectGames


Future updates
--------------

All our assets follow an incremental development process by which a few beta releases are published on our support forum (kronnect.com).
We encourage you to signup and engage our forum. The forum is the primary support and feature discussions medium.

Of course, all updates of Voxel Play will be eventually available on the Asset Store.


Version history
---------------

6.5
- Vegetation now uses same displacement when stacked vertically
- [Fix] Model importer fixes
- [Fix] Removed shadow acne
- [Fix] Fixed empty voxels showing up in model preview

6.4
- Added a warning to the inspector when Curvature option is enabled but Smooth Lighiting disabled which can cause artifacts
- Added "Ignore Transparency" option to Import Tools window
- API: added VoxelDamage (origin, damage, radius, attenuateDamageWithDistance, addParticles, playSound)
- API: ModelFillInside now fills interior of building models with holes to avoid being filled by terrain generator
- Improvement to model placement: position can now be moved with mouse and elevation controlled with mouse wheel
- First person controller: added crosshair max distance parameter
- [Fix] Fixed displacement issue when placing models at certain rotations
- [Fix] Fixed custom voxels not being rotated properly when placed as part of a rotated model definition
- [Fix] API: fixed OnVoxelAfterAreaDamage event not returning the list of damaged voxels if the List is not provided in the function call

6.3
- Voxel definition: added DropItemLifeTime, DropItemScale properties to customize the duration and size of recoverable voxels
- [Fix] Fixed building generator not respecting world seed when chosing a random rotation
- [Fix] Fixed Qubicle import tool issue with transparent voxels
- [Fix] Prevent out of range issue with Unity to Voxel Terrain generator after splatmaps are modified/removed
- [Fix] Fixed some generated chunk remains hidden when Unload Far Chunks option is enabled
- [Fix] Fixed override material issue with chunks not using AO

6.2
- World definition: added option "Light Sun Attenuation" / "Light Torch Attenuation" parameters. Control the speed at which light attenuates across distance.
- Voxel definition: added option  "Light Intensity". Voxels can now emit light without a torch (ie. lava, special voxels, etc.)
- Voxel definition: added option  "Generate Colliders" (only applies to Cutout render type like tree leaves)
- API: added VoxelRemove
- Custom voxels performance optimizations
- [Fix] Fixed drop item animated cube scale issue
- [Fix] Fixed precision issue when compiled with IL2CPP

6.1
- Connected textures: neighbour voxel type can now be specified (video: https://youtu.be/x9UilyySLFQ)
- Voxel Definition: added "Highlight Offset" option. Use this property to shift highlight box position if needed.
- Models can now contain torches (and are also saved from the constructor)
- Removed Constructor grid bounds limitation to make easier to build on edges
- API: added GetChunks(bounds)
- [Fix] Fixed rotated voxels placed in constructor not retaining its rotation when placed in world

6.0.2
- [Fix] Fixed some Voxel Play Player component fields not being accesible through scripting

6.0.1
- Cloud chunks no longer are unloaded when moving around since they already translate automatically when required
- [Fix] Fixed some cutout voxels not being rendered correctly when tinting option is enabled
- [Fix] Fixed a race issue during start up that could cause some issues rendering some chunks
- [Fix] Fixed save/load game not accepted when chunk size is set to 32

6.0
- Connected Textures. See https://kronnect.freshdesk.com/en/support/solutions/articles/42000067054-connected-textures.
- Animated textures. New render type "Opaque 3 Textures with AO and animation". Assign frame textures to the Additional Textures property in the Voxel definition and specify a speed for the transition.
- Added "Bright Point Lights" option to Voxel Play Environment inspector. Point light contribution is now baked into voxels separated from Sun light contribution to improve lighting fidelity. Bright Point Lights option allows you to combine baked + realtime point lighting.
- Chunk size is now configurable. 16 or 32.
- Optimized mesh generation on border chunks to reduce triangle count.
- Removed Geometry shaders support.
- Added VR support (Multi, Single Pass Stereo and Single Pass Instanced).
- InventoryItem struct now declared as partial to allow custom fields

5.5
- API: added GetChunkRawBuffer, GetChunkRawData, SetChunkRawData: fast and efficient methods to get/set chunk data
- Improved performance for particle light calculation on creation
- [Fix] Custom voxels without MeshRenderer were being duplicated when chunk is changed
- [Fix] Changing sky tint color in inspector does not reflect in Editor mode

5.4
- Improved cloud rendering (check demo scene 1)
- Improved multithreading meshing architecture (more parallelism, half buffer memory)
- First person controller: added Smooth Climb option (enabled by default)
- Tweaked mobile UI
- BoxCollider from custom voxels prefab are now extracted to ensure accurate test when calling VoxelOverlap
- API: Voxel Play Player has now a public interface which can be used to implement custom player class. You can use the default Voxel Play Player monobehaviour, inherit from it or create your own player class and implement the IVoxelPlayPlayer interface.
- API: IVoxelPlayPlayer: added PickUpItem method. Gets called when player approaches items and they get collected. Override it from your own class to perform custom actions.
- API: added events OnTreeBeforeCreate / OnTreeAfterCreate. OnTreeBeforeCreate handler can return false to cancel tree placement on a certain position. OnTreeAfterCreate receives a list of VoxelIndex elements with the modified positions.
- Reduced memory usage

5.3
- Dual touch controller now remains hidden until initialization is complete
- Custom voxels: can now include LOD group
- Custom voxels: can specify a material override to avoid changing the original prefab
- World definition: added Grass Wind Speed / Tree Wind Speed global multipliers
- API: added GetVoxelIndex overload
- New menu: Assets/Voxel Play/Contructor Tools... which opens the constructor
- [Fix] Fixed global scattering and intensity light settings not affecting vegetation

5.2
- Easier to add your own UI (inventory, console, etc) and Input controllers (see: https://kronnect.freshdesk.com/support/solutions/folders/42000098383)
- Added "Preview Mobile UI in Editor" option to Voxel Play Environment inspector. Allows you to use the mobile dual touch UI in Unity Editor when a mobile build target is set (ie. Android/iOS);
- Improvements to Dual Touch controller: implemented click (single hit) and long press gestures (continuous hit)
- Torch light is now added to the lightmap algorithm (combines with shader lighting)
- API: added "allowDuplicatedMessages" to ShowMessage method
- API: added OnVoxelBeforeAreaDamage / OnVoxelAfterAreaDamage events
- First person controller: when going underground, camera near clip plane is lowered to 0.08 to avoid face clipping. The value is restored when returning to surface.
- Performance improvements during chunk refresh containing custom voxels
- [Fix] Fixes issues when VoxelPlayEnvironment was instantiated from a prefab
- [Fix] Fixed door rotation bug
- Other fixes and minor code improvements

5.1
- GameObjects and custom state data can now be included in saved games (see: https://kronnect.freshdesk.com/solution/articles/42000041550-loading-saving-game-sessions)
- Chunks now can be unloaded when out of visible distance. New option added to Voxel Play Environment: "Unload Far Chunks".
- Improvements to the default terrain generator: water can now flow underground
- Improvements to cave generator, improved performance
- Added console command /redraw
- Torches can be recovered: https://youtu.be/e6iqPwi9ULA
- Item definition: new properties: canBePicked, pickMode.
- API: added TriggerVoxelClickEvent method
- API: added OnPlayerEnterChunk, OnChunkExitVisibleDistance / OnChunkEnterVisibleDistance events
- API: change OnChunkUnload event renamed to OnChunkReuse
- API: added ChunksRedrawAll
- [Fix] Avoid torches being placed on occupied voxels
- [Fix] Fixed "Free Mode" issue in first person controller

5.0
- Realistic water option (https://i.imgur.com/s0dB57e.jpg)
- Improvements to override material option. Example included in demo scene 1.
- Improvements to third person character controller
- Added distanceAnchor to Voxel Play Environment inspector which defined the reference point for chunk distance calculation when rendering the world (useful for third person)
- Changes to see-through effect: added screen mask, improved blending gradient, global alpha multiplier setting removed. WIP.
- Multiple types of torches are now supported
- API: added rotation field to ModelBit structure
- [Fix] Fixed model not being loaded in Colorizer scene due to voxel definitions are not part of the world
- [Fix] Fixed save game not loaded with custom first player controller
- [Fix] Fixed collapsing voxels not rendering when geometry shaders are enabled (introduced in 5.0 b1)

4.3.1
- Improved third person character controller
- Sample hero character now uses Voxel Play model materials to account for environment light and shadows
- [Fix] Fixed bug in when "Render In Editor" and "See-Through" feature are enabled

4.3
- Export chunks: ability to export generated chunks as regular gameobjects. Once exported, Voxel Play Environment can be removed. Materials, textures and meshes are then part of the scene.
- API: added VoxelGetGameObject: returns the gameobject associated to a custom voxel placed in the scene after the chunk has been refreshed
- Biome Explorer: added button "Align With Camera": moves the preview map around SceneView camera position
- Change: fog effect now is disabled in SceneView when "Render In Editor" and "Draft Mode" are enabled

4.2
- Custom voxels can now use Compute Buffers for rendering (enable it in Voxel Play Environment inspector)
- Improved shader performance of point lights
- Console: added /time command to set time of day during gameplay
- API: added SetTimeOfDay (24h format)
- [Fix] Fixed custom voxels not being affected by see-through effect
- [Fix] Fixed custom voxels not using vertex colors when GPU instancing is disabled
- [Fix] Fixed AO seams issue due to race condition between chunk refreshing, rendering and lightmap calculation

4.1.3
- [Fix] Fixed a case where some chunk colliders won't be generated

4.1.1
- [Fix] Fixed out of range error raised by VoxelDamage method
- [Fix] Fixed ignoresRayCast not properly used by RayCastVoxel method
- [Fix] Fixed editor console error when using Regenerate Chunks option from inspector
- [Fix] Fixed issue with DamageAreaFast API arguments order

4.1
- Voxel Definition: added SeeThrough mode (details: https://kronnect.freshdesk.com/solution/articles/42000058885-see-through-occluding-voxels-)
- Voxel Definition: added alpha setting to transparent voxels. Texture alpha is multiplied by this alpha factor so you can reuse opaque textures in transparent voxels with custom alpha values
- Third Person Controller: added "Avoid Obstacles" option to control if camera must move if an obstacle is found between player and camera
- Third Person Controller: prevents character moving when clicking on inventory items
- API: added VoxelSetHidden, VoxelIsHidden
- API: added CanHide property to VoxelDefinition. Specifies if the voxel can be toggled hidden when using VoxelSetHidden method
- API: added LineCast: returns a list of voxels or chunks between two positions in world space.
- Increased performance of GetVoxelIndices methods
- Increased performance of lightmap computation
- [Fix] Fixed some voxel definitions not being loaded due to mismatch between root world folder name and world name
- [Fix] Fixes issue when assigning a Substance texture to a voxel definition

4.0
- Ability to override default materials (new Voxel Definition property: override material). Details: https://kronnect.freshdesk.com/solution/articles/42000057934-custom-material-shaders
- Added new fields to define world limits in World Definition
- API: safety InventoryItems null checks added (thanks to Rafael)
- API: VoxelDropItemEvent now passes the VoxelHitInfo struct instead of just VoxelIndex (thanks to Rafael) 
- Change: Partial class files that extend another file now include the file name of the primary file. Eg: VoxelPlayTrees now becomes VoxelPlayEnvironment.Trees
- Change: Internal folder reorganization for clarity purposes
- Change: seaLevel has been replaced by waterLevel in Terrain Generator. Instead of a 0..1 value, water level is the exact integer altitude of water level.
- Reduced one shader keyword (tinting)
- [Fix] Fixed character drifting when Use Third Party Controller option is enabled

3.8.1
- [Fix] Fixed regression chunk bounds issue which resulted in reduced performance 

3.8
- Change: bedrock voxel field moved from World Definition to the Terrain Generator
- Bedrock is now placed at the minimum height defined in terrain generator instead of the bottom of lower chunk
- Prevents ore spawning in water voxels
- Player position now shows in debug window (F2)
- API: ReloadWorld now preserves changes

3.7
- New savegame format (7.0) which avoids problems due to missing voxel definitions 
- Tweaked lighting function so AO is not affected by ambient light parameter
- API: TerrainDefaultGenerator now implements ITerrainDefaultGenerator interface to allow further extensibility
- Added Copy/Paste terrain steps commands to terrain generator inspector gear icon/context menu
- Removed temporary black fade when destroying voxels

3.6
- New curvature effect (enable it on Voxel Play Environment inspector; details: https://kronnect.freshdesk.com/solution/articles/42000055254-shaders)
- Improvements to model placement (visualization, player facing)
- Reduced vegetation & leaves halo artifacts

3.5
- New feature: model definitions can be part of inventory and placed like other voxels
- Added tinting support to cutout voxels
- Model Definition: added field "buildDuration"
- API: added overload to ModelPlace which accepts a duration
- API: new events: OnModelBuildStart / OnModelBuildEnd
- [Fix] Fixed issue when reloading a world containing GPU instanced voxels
- [Fix] Fixed ground clipping issue with third person controller

3.4.3
- Added "Transparent Bling" option to Voxel Play Environment
- [Fix] Fixed lighting issues in big caves/rooms

3.4.2
- [Fix] Fixed AO not showing correctly
- [Fix] Fixed tint color on transparent voxels

3.4.1
- Added "Low Memory Mode" to Voxel Play Environment

3.4
- Added specular lighting to water shaders
- Voxel Play First Person Controller can now work without Unity Character Controller providing basic features like crosshair, digging and building
- Added inspector help links (like Biome Definition, etc) to online documentation
- Improvements to Third Person Character Controller

3.3
- Added Initial Wait Time option to Voxel Play Environment (waits for some rendering before player can move)
- Improved water foam corners
- New noise viewer & generator tool. Includes tilable Perlin and Cellular generators.
- Added Show All / Default Colors buttons to biome explorer
- Character can now enable flying mode while jumping
- Minor improvements to cave and ore generators
- [Fix] Fixed issue with entering constructor mode in Unity 2018.3

3.2
- Reworked ore generation algorithm (faster, more options)
- New Texture Voxelizer tool: converts a texture into an optimized prefab
- New Biome Map explorer: open from Voxel Play menu or from Voxel Play Environment inspector
- Items can now be thrown and picked up
- Items are now saved as part of the chunk
- New bare-hand attack properties for player
- New property bag for item definitions
- Voxel sword and shovel items added to demo world Earth
- New Tile Rules Set: define prioritized rules that execute when placing specific voxels to change the final placed voxel. Useful to create connected voxels.
- Improved performance of GPU instancing and custom voxels
- New opaque property for custom voxels to occlude adjacent voxels
- If model is not provided in a custom voxel definition, a default cube is used
- Improved outline effect when using geometry shaders
- Added VPModelTextureTriplanar material/shader
- API: improved performance of Voxel Place
- API: added OnVoxelBeforePlace event
- API: changed VoxelChunk.Contains method to avoid dependency on its MeshRenderer component
- API: added OnChunkUnload event
- API: GetItemByType renamed to GetItemDefinition
- API: new ItemThrow / ItemSpawn
- API: added GetBiome(altitude, moisture)
- Only Generate in Frustum has been removed. Use Only Render in Frustum.
- [Fix] To prevent reading issues from textures, the VoxelDefinition inspector now automatically sets Read/Write enabled property to the assigned textures
- [Fix] Voxels won't collapse when loading a saved game during the same session
- [Fix] Color defined at material was being replaced by voxel tint color
- [Fix] Unity terrain voxel converter generated flat terrain in Edit mode

3.1.1
- [Fix] Skybox shows pitch black in Editor
- [Fix] Water and shore voxels shows in white when duplicating the world asset
- [Fix] API: some enums names have been fixed to respect coding style

3.1
- VoxelPlay Environment inspector: character controller setting moved to Optional Game Features section. Character controller is now optional and can be omitted. In this case the active camera is used to render the world.
- New Optional Game Features section - groups default in-game features like status bar, inventory display, etc. and can now be completely disabled to provide custom features
- API: added ChunkCheckArea. Ensures all chunks within bounds are generated.
- API: VoxelPlace(positions|indices) now allow voxelType = null which clears those positions in batch
- Voxel Play Behaviour: added generation bounds option. Useful to ensure chunks within an area around the transform are fully generated
- An error message will now be shown if chunk pool cannot be fullfilled (ie. out of memory error)

3.0.3
- API: added SaveGameToBase64, LoadGameFromBase64
- [Fix] Fixed demo scene Colorizer
- [Fix] Fixed dynamic voxels not colliding with terrain voxels
- [Fix] Fixed issue with contructor model displacement command

3.0.2
- Improvements to SceneView terrain generation
- Water surface can now be seen when diving
- [Fix] Fixed race condition when chunk generation demand exceeds mesh generation+upload speed

3.0.1
- Added "flood on|off" command to console. Video: https://youtu.be/pB71KAM5TPI
- [Fix] Fixed potential exception in UI console module when loading game
- [Fix] Textures are not available after loading a saved game
- [Fix] Removed unneeded step in the multi-step terrain generator used in demo world
- [Fix] Other minor fixes 

3.0
- GPU Instancing options for Custom voxel types
- New lighting system. Up to 32 point lights can illuminate voxels and models. Per-vertex or per-pixel calculation (new option in Voxel Play Environment inspector)
- Point light color alpha determines the light wrapping factor
- World Definition: added global light intensity and scattering parameters
- Added shadows fade out
- Improved water flood. New "drain" property in Voxel Definition. New VoxelWaterSea that does not drain when flooding. Avoids slopes on seas and floods more distance
- Custom voxel type: new random offset and rotation properties
- Voxel Play World can now be parented to any other object in the scene as long as it's located at 0,0,0
- Voxel Play UI Canvas parented to Voxel Play World gameobject as root
- Added Hide Chunks In Hierarchy option to Voxel Play Environment
- Crosshair material is now instantiated during start up
- Unity 2018.3 support
- [Fix] Tweaked point light falloff to reduce sudden-lit chunks
- [Fix] GPU Instanced models now receive vertex lights
- [Fix] Added VoxelSetDynamic voxel type check
- [Fix] Fixed flood light issue on chunks crossing the terrain surface 

2.2
- Overlap test when placing custom models
- Biome voxels now pile up automatically: https://youtu.be/VBdJEIR47cY
- Support for fractional amounts in inventory
- Water can be collected 
- Added Daylight Shadow Atten option to Voxel Play Environment: a value of 0 preserves standard shadow intensity. A value of 1 makes shadows disappear when Sun is high. A middle value makes shadows more prominent when Sun is low and more subtle when Sun is high.
- API: added IsEmptyAtPosition(position)
- API: added VoxelOverlaps: returns true if a voxel can collide with existing geometry

2.1
- Added building and door example to Demo scene World
- Sample doors included (double and simple doors).
- Cube creation tool improved for easy door creation. Tutorial: https://kronnect.freshdesk.com/solution/articles/42000047211-door-creation-tool
- Added SetTimeAndAzimuth, TimeOfDay and Azimuth (Sun Y-axis rotation) to World settings
- Interactive object framework beta. Press T to interact with objects (eg. doors)
- Tweaks to clouds lighting
- Default point light range reduced to 5
- Inventory panel simplified: alt key no longer used, use shift key to switch column on current page
- Chunks and other objects created by Voxel Play are now parented to "Voxel Play World" parent gameobject
- API: added IsWallAtPosition(position) returns true if there's a solid block on the given position
- FPS Controller: pressing middle mouse button autoselects voxel type on crosshair
- [Fix] Character can't cross doors of 2 block height
- [Fix] Fixed torches not appearing after loading a saved game

2.0
- New demo scene: HQForest
- Relief Mapping: added per voxel texture parallax occlusion mapping option with relief mapping technique
- Normal Mapping: added per voxel texture normal mapping option
- Emission/glow: added emission mask to opaque voxel definitions and emission animation options in World Definition (FX section)
- Global Illumination: light spread no longer attenuates horizontally above terrain surface
- Ambient occlusion: renamed to Smooth Lighting. Ambient Occlusion Intensity parameter removed
- Voxel definition: added color variation parameter
- Voxel textures no longer kept in memory after starting up

1.6
- Left control key: crouch while walk, avoids falling off voxel edges
- Voxel Definition: can specify a default tint color
- Voxel Definition: can enable OnVoxelEnter / OnVoxelWalk events (raised from FPS controller)
- API: new GetVoxels / VoxelPlace with support for 3d array of voxels
- API: VoxelCreateGameObject method creates a gameobject from a color array with optimized vertex count
- Qubicle importer: ability to define color to voxel definition mappings
- [Fix] Player no longer moves when input manager is disabled
- [Fix] Some voxels won't collapse when breaking a structure/tree
- [Fix] Regression: fixed cutout/vegetation voxel types not showing the texture field in the custom inspector

1.5
- New voxel type: transparent voxels
- New voxel type: empty voxels/airblock
- New option in Voxel Play Environment inspector: double sided glass
- Voxel Definition: added Title field to customize shown name (by default empty)
- Voxel Definition: new properties: IgnoresRayCast, PlaceFacingPlayer, Allows Texture Rotation
- API: VoxelPlace: added overload to fill a volume with same voxel definition and optional tint color
- API: GetVoxelIndices: added option to get all voxel indices
- API: VoxelPlayer: added HasItem, GetItemQuantity methods
- [Fix] Fixed ore generation issue

1.4
- New: outline effect option in Voxel Play Environment inspector)
- Improved performance of default cave generator
- 6-textures voxels are now placed facing the player
- An informative error is now shown in the console if player's layer cannot collide with voxel's layer
- Shore voxel moved to VoxelPlay Resources's Defaults folder
- Crosshair material is now instantiated to avoid changes to original material

1.3
- Added native support for voxels with 6 different textures (render type = opaque 6 textures)
- Added native voxel texture rotation (Y-axis only)
- API: added VoxelRotate, VoxelSetRotation method
- API: added VoxelRotateTextures, VoxelSetTexturesRotation, VoxelGetTexturesRotation
- Save game format now supports voxel rotation

1.2
- Added VoxelPlace variant which accepts a list of positions or voxel indices
- Added GetVoxelIndices method which accepts a SDF function. The SDF function accepts a world space coordinate and it must return a negative value if that position is inside the desired volume.
- Voxel highlight now adapts to grass boundary
- Quick load / save now mapped to Control + F3 / F4

1.1
- Added OnChunkRender event
- Added Collapse On Destroy feature to World Definition (voxels types marked with Trigger Collapse / Will Collapse will fall when nearby voxels are destroyed).
- Added Collapse Amount to World Definition (maximum number of voxels included in a falling group)
- Added Consolidate Delay parameter to World Definition
- Performance optimizations
- API: GetChunks(chunks, modified) can return only modified chunks by user
- API: LoadGameFromByteArray optional parameter to avoid clearing the entire scene (loads faster)
- API: added VoxelCancelDynamic
- API: added ChunkIsInFrustum
- [Fix] Fixed black voxels when destroying certain positions
- [Fix] Fixed some voxel faces not being rendered in chunks with caves

1.0
- Added damage indicator duration parameter to World definition
- Voxel highlight now adapts to water block size
- API: declared most scriptable objects classes as partial to improve customizability
- API: added GetVoxelIndices(position, radius) and the returned VoxelIndex values now include the distance to the center.
- API: redesigned VoxelDamage method with radius effect

0.20
- New Cube Creator tool: https://kronnect.freshdesk.com/solution/articles/42000038971-cube-creation-tool
- New tutorial on how to create rotated cubes: https://kronnect.freshdesk.com/solution/articles/42000038963-rotated-voxels
- New demo scene 4: third person controller
- First Person Controller: added voxel highlight effect (new options in crosshair section)
- Camera far clipping plane and fog distances get synced based on visible chunk distance
- Added drop item to voxel definition (allows specifying a different item when voxel is destroyed)
- Added ore settings to biomes
- API: Added NoiseTools.GetNoiseValue for 3D textures
- API: Added VoxelHighlight

0.19 b3
- [Fix] Fixed issue on WebGL

0.19
- Optimization (mesh generation): detection of fully occluded chunks. Chunks fully surrounded by other chunks skip mesh generation phase.
- Optimization (memory): chunk memory requirement down by 47% (without voxel tinting support) or 24% (with voxel tinting support)

0.18 b2
- Added constructor size parameter to FPSController inspector
- Water can now be poured from any height

0.18
- API: VoxelGetDynamic, VoxelIsDynamic
- Improved custom-type voxel rendering. Color and lighting now baked in mesh reducing draw calls.
- Added offset, rotation and scale to voxel definitions

0.17 b4
- Improved load/save format (new save file format 3.0, see: https://kronnect.freshdesk.com/solution/articles/42000036928-saved-games-file-format) 
- API: added LoadGameBinary, GetSaveGameBinary, SaveGameBinary, SaveGameToByteArray, LoadGameFromByteArray

0.17 b3
- Added hit damage / hit delay to ItemDefinition class
- Added VoxelThrow method (key G)

0.17
- New water system
- New voxel: lava
- Voxel Definition: new properties (spread/spread delay, contact damage)
- API: new events: OnVoxelBeforeSpread, OnVoxelAfterSpread
- API: new player events: OnPlayerGetDamage, OnPlayerIsKilled
- API: added VoxelDamage method

0.16 b3
- New skybox style: Earth + Day + Night cubemap (assign your HDR cubemaps in Voxel Play Environment)
- API: added GetChunks method

0.16
- New skybox style: Earth + Night cubemap (assign your HDR cubemap in Voxel Play Environment)
- API: added event VoxelPlayPlayer.OnSelectedItemChange
- [Fix] Fixes transparent voxel under leaves when placed over certain solid voxels

0.15
- Optimized/reduced CPU load on chunk thread generation (good for mobile)
- Unity point lights support
- Torches now cast a point light at position (customize point light color and behaviour in torch prefab itself)
- Trees no longer respawn over loaded chunks from saved games
- FPS character carries a light. Press L to toggle it on/off (customize light properties in FirstPersonCharacter gameobject scripts).
- API: added AddVoxelDefinition method
- API: added ModelPlace method overload to place an array of voxel definitions with optional colors
- API: ModelPlace now can return the list of all vixible voxels in the model
- API: added GetVoxelIndices: returns a list of visible voxel indices inside a given 3D box

0.14
- Unity terrain/vegetation/trees to Voxel converter/generator

0.13
- Added custom voxel definition types (eg. half blocks)
- Added VPModelTextureAlpha shader which allows transparency on models

0.12
- Added HQ filtering option
- Integrated mobile touch controls (automatically enabled when running on mobile)
- Improved reliability on mobile
- Added FPS counter (hotkey F8)
- New simplified skybox for mobile (set it in World definition; picked automatically when running on mobile)
- Added windAnimation property to Voxel Definitions
- Added Cactus tree models

0.11
- Customizable water in-block height (water block height property in World definition)
- Added greedy meshing for any geometry without ambient occlusion (clouds or terrain voxels when AO is disabled)

0.10
- Added NavMesh support (preview)
- Greedy meshing for colliders and NavMesh
- Added texture size parameter to Voxel Play Environment component
- Added bedrock voxel option to world definition
- Added minHeight parameter to terrain generator
- Added navigatable property to Voxel Definition
- New demo scene 3: Simple Flat Terrain with Simple Foes
- API: added DamageRadius to player and RayHit methods
- Player now can run/climb over voxels without jumping everytime
- Added crouching to first person controller (key C)

0.9.2 
- Added new demo scene (custom flat terrain generator)
- [Fix] Fixed casting issue on World inspector when using custom terrain generators

0.9
- Random tree rotation
- Detail spawners: villages and caves
- New debug window (F2)
- Octree implementation for faster chunk culling prepass

0.8
- Revamped collider system, now uses standard Unity colliders - now compatible with other assets!
- Add multithread chunk generation at runtime
- Improve pixel shader performance
- Smoother ambient occlusion
- Updated demo scene Colorizer
- API: Added OnVoxelBeforeDestroyed event

0.7
- Import tools: option to create prefab
- New shaders for in-game models supporting fog and smooth lighting
- Improved demo scene Earth - press 1,2,3 to create ball, deer, brick
- Improved performance of water style with no shadows
- Added flash option to status bar messages

0.6
- New demo scene: supercube
- Qubicle import tool (binary format only at this moment)
- Constructor: added instructions panel on top/right corner
- Constructor: added new options: pressing Control + AWSDQE displaces model
- Voxels now support tint color (new Enable Tinting option in inspector)
- FPS Controller: added orbit mode
- Ability to disable build mode (EnableBuildMode property)
- Ability to disable console UI (EnableConsole property)
- Ability to disable inventory UI (EnableInventory property)
- API: Added ModelPlace(position, model)
- API: Added ModelPlace(position, color array)
- API: Added OnVoxelBeforeDropItem, OnVoxelClick events
- API: Added SaveGameToText()
- Models are no longer limited to 16x16x16 in size and can include an optional center offset
- More internal changes and optimizations

0.5 b2
- New Scriptable Object: TerrainGenerator. Enables creation of user-defined terrain heightmap/biome providers (see documentation)
- Added terrain generator section in Voxel Play Environment inspector
- Added /unstuck console command
- FPS Controller: added fly speed parameter
- Editor: added "Scene Camera To Surface" button -> repositions Scene camera on top of terrain
- Editor: added "Find Main Camera" button -> repositions Scene camera on top of main camera
- Lot of internal changes and optimizations

0.5 b1
- Added option to disable ambient occlusion
- Added option to disable shadows
- Added 3 performance preset buttons

0.4
- New inspector option: geometry shaders (on/off; off = support for Shader Model 3.5 so rendering should work on most modern devices)
- New inspector option: water receives shadows (only geometry shader)
- New inspector option: dense trees (default = yes, off = less geometry on contiguous tree voxels)
- Improved vegetation and tree leaves shading (more color variation and ambient occlusion)
- Added 2 new bushes and denser folliage to grassland and forest

0.3
- Added "/visibleDistance" console command
- New voxel definitions can be used at runtime when calling VoxelPlace method (no need to add them all to world definition)

0.3 b1 Current Release
- Rivers. New world definition properties.

0.2 b9 12-FEB-2018
- Reusable chunk pool (control max memory usage using Chunk Pool Size property)

0.2 b7 5-FEB-2018
- Improvements to water flood system
  - Fixed water not visible under certain positions/angles
  - New flood range setting in World settings
- Added "Help Cookies" to inspector
- Fixed conflict between special keys shift, alt, Fire1 and inventory selection

0.2 1-FEB-2018
Second beta:
- Added inventory system

0.1 January/2018
First Beta

