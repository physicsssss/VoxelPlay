using UnityEngine;

namespace VoxelPlay {

	[HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000027332-detail-generators")]
	public abstract class VoxelPlayDetailGenerator : ScriptableObject {

		public bool enabled = true;

		protected const int ONE_Y_ROW = VoxelPlayEnvironment.CHUNK_SIZE * VoxelPlayEnvironment.CHUNK_SIZE;
		protected const int ONE_Z_ROW = VoxelPlayEnvironment.CHUNK_SIZE;

		/// <summary>
		/// Initialization method. Called by Voxel Play at startup.
		/// </summary>
		public virtual void Init() { }


		/// <summary>
		/// Called by Voxel Play to inform that player has moved onto another chunk so new detail can start generating
		/// </summary>
		/// <param name="currentPosition">Current player position.</param>
		/// <param name="checkOnlyBorders">True means the player has moved to next chunk. False means player position is completely new and all chunks in range should be checked for detail in this call.</param>
        /// <param name="endTime">Provides a maximum time frame for execution this frame. Compare this with env.stopwatch milliseconds.</param>
		public virtual void ExploreArea(Vector3 currentPosition, bool checkOnlyBorders, long endTime) { }

		/// <summary>
		/// Called by Voxel Play so detail can be computed incrementally so detail info is ready when needed (retrieved by GetDetail method)
		/// At runtime this method will be called in a specific thread so Unity API cannot be used.
		/// This method should not produce spikes nor heavy computation in a single frame.		
		/// </summary>
		/// <param name="endTime">Provides a maximum time frame for execution this frame. Compare this with env.stopwatch milliseconds.</param>
		/// <returns><c>true</c>, if there's more work to be executed, <c>false</c> otherwise.</returns>
		public virtual bool DoWork(long endTime) { return false; }

		/// <summary>
		/// Fills the given chunk with detail. Filled voxels won't be replaced by the terrain generator.
		/// Use Voxel.Empty to fill with void.
		/// </summary>
		public virtual void AddDetail(VoxelChunk chunk) { return; }


		/// <summary>
		/// Call this method from your DoWork() / AddDetail() code if you modify the chunk contents to ensure world is updated accordingly
		/// </summary>
		public void SetChunkIsDirty(VoxelChunk chunk) {
			chunk.MarkAsInconclusive ();
		}


	}

}