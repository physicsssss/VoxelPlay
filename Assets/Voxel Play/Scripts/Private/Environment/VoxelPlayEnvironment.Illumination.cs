using UnityEngine;


namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		// Lightmap renderer
		int[] tempLightmapPos;
		int tempLightmapIndex;

		bool effectiveGlobalIllumination {
			get {
				if (!applicationIsPlaying)
					return false;
				return globalIllumination;
			}
		}

        /// <summary>
        /// Computes light propagation. Only Sun light. Other light sources like torches are handled in the shader itself.
        /// </summary>e
        /// <returns><c>true</c>, if lightmap was built, <c>false</c> if no changes or light detected.</returns>
        /// <param name="chunk">Chunk.</param>
        void ComputeLightmap (VoxelChunk chunk) {
			if (!effectiveGlobalIllumination) {
				return;
			}

            int lightmapSignature; // used to detect lightmap changes that trigger mesh rebuild

            lightmapSignature = ComputeSunLightMap (chunk);
            lightmapSignature += ComputeTorchLightMap (chunk);

            if (lightmapSignature != chunk.lightmapSignature) {
				// There're changes, so annotate this chunk mesh to be rebuilt
				chunk.lightmapSignature = lightmapSignature;
				chunk.needsMeshRebuild = true;
			}

			chunk.lightmapIsClear = false;
		}
	}



}
