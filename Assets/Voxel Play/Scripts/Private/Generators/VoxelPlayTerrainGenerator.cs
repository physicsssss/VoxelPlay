using System;
using UnityEngine;

namespace VoxelPlay {

	public abstract class VoxelPlayTerrainGenerator : ScriptableObject {

        protected const int ONE_Y_ROW = VoxelPlayEnvironment.CHUNK_SIZE * VoxelPlayEnvironment.CHUNK_SIZE;
        protected const int ONE_Z_ROW = VoxelPlayEnvironment.CHUNK_SIZE;

        /// <summary>
        /// The maximum height allowed by the terrain generator (usually equals to 255)
        /// </summary>
        [Header ("Terrain Parameters")]
		public float maxHeight = 255;

		public float minHeight = -32;

		/// <summary>
		/// Deprecated. Kept to maintain previous value during conversion to waterLevel
		/// </summary>
		[HideInInspector]
		public float seaLevel = -1;
		public int waterLevel = 25;

		[NonSerialized]
		protected VoxelPlayEnvironment env;

		[NonSerialized]
		protected WorldDefinition world;

		/// <summary>
		/// Resets any cached data and reload info
		/// </summary>
		protected abstract void Init ();

		/// <summary>
		/// Gets the altitude and moisture.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="z">The z coordinate.</param>
		/// <param name="altitude">Altitude (0..1) range.</param>
		/// <param name="moisture">Moisture (0..1) range.</param>
		public abstract void GetHeightAndMoisture (float x, float z, out float altitude, out float moisture);

		/// <summary>
		/// Paints the terrain inside the chunk defined by its central "position"
		/// </summary>
		/// <returns><c>true</c>, if terrain was painted, <c>false</c> otherwise.</returns>
		public abstract bool PaintChunk (VoxelChunk chunk);

		/// <summary>
		/// Returns true if the terrain generator is ready to be used. Call Initialize() otherwise.
		/// </summary>
		[NonSerialized]
		public bool isInitialized;



		/// <summary>
		/// Use this method to initialize the terrain generator
		/// </summary>
		public void Initialize () {
			env = VoxelPlayEnvironment.instance;
			if (env == null)
				return;
			world = env.world;
			if (world == null)
				return;

			// Migration introduced in v4.0: TODO: remove in the future
			#if UNITY_EDITOR
			if (seaLevel >= 0 && !Application.isPlaying) {
				waterLevel = (int)(seaLevel * maxHeight);
				seaLevel = -1;
				UnityEditor.EditorUtility.SetDirty(this);
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
			}
			#endif

			env.waterLevel = waterLevel;
			Init ();
			isInitialized = true;
		}

	}

}