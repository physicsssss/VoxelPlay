using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	public class VoxelCloudsAnimator : MonoBehaviour {

		[NonSerialized]
		public List<VoxelChunk> cloudChunks;

		int cloudCount;
		int cloudIndex;

		void Start() {
			cloudCount = cloudChunks.Count;
		}

        void LateUpdate() {
            if (cloudChunks == null) return;
            VoxelChunk cloudChunk = cloudChunks[cloudIndex];
            transform.position -= new Vector3(Time.deltaTime, 0, 0);
            if (cloudChunk != null) {
                Vector3 refPos = VoxelPlayEnvironment.instance.cameraMain != null ? VoxelPlayEnvironment.instance.cameraMain.transform.position : Misc.vector3zero;
                Transform cloudTransform = cloudChunk.transform;
                Vector3 pos = cloudTransform.position;
                if (pos.x < refPos.x - 512) {
                    pos.x += 1024;
                } else if (pos.x > refPos.x + 512) {
                    pos.x -= 1024;
                }
                if (pos.z < refPos.z - 512) {
                    pos.z += 1024;
                } else if (pos.z > refPos.z + 512) {
                    pos.z -= 1024;
                }
                if (cloudTransform.position != pos) {
                    cloudTransform.position = pos;
                }
                cloudChunk.position = pos;
            }
            cloudIndex++;
            if (cloudIndex >= cloudCount)
				cloudIndex = 0;
		}


	}

}