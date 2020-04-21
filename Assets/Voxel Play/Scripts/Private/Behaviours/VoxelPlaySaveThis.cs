using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

    public class VoxelPlaySaveThis : MonoBehaviour
    {

        /// <summary>
        /// Path to the prefab (ie. "Worlds/Earth/Models/Deer").
        /// </summary>
        [Tooltip("The path to the prefab in a Resources folder")]
        public string prefabResourcesPath;

        VoxelPlayEnvironment env;
        Rigidbody rb;

        void Start ()
        {
            rb = GetComponent<Rigidbody> ();
            if (rb == null || rb.isKinematic) return;

            // If chunk is not rendered and have a rigidbody, wait until ready
            env = VoxelPlayEnvironment.instance;
            if (env == null) return;

            VoxelChunk chunk;
            if (!env.GetChunk(transform.position, out chunk, false) || !chunk.isRendered) {
                rb.isKinematic = true;
                StartCoroutine (WaitForChunk (chunk));
            }
        }

        IEnumerator WaitForChunk(VoxelChunk chunk)
        {
            WaitForEndOfFrame w = new WaitForEndOfFrame ();
            while(chunk != null && !chunk.isRendered) {
                if (gameObject == null) yield break;
                yield return w;
            }
            if (rb != null) {
                rb.isKinematic = false;
            }
        }
    }
}
