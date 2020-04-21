using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

namespace VoxelPlayDemos
{
	
	public class Demo : MonoBehaviour
	{

		VoxelPlayEnvironment env;
		// Use this for initialization
		void Start ()
		{
			env = VoxelPlayEnvironment.instance;
			env.OnInitialized += InitializeGame;
		}

		void InitializeGame ()
		{
			// Paint some random walls
			for (int w = 0; w < 30; w++) {
				Vector3 pos = new Vector3 (Random.value * 50 - 25, 50.5f, Random.value * 50 - 25);
				int length = Random.Range (5, 10);
				Vector3 direction = Random.value < 0.5f ? Vector3.right : Vector3.back;
				bool tall = Random.value < 0.5f;
				for (int b = 0; b < length; b++) {
					env.VoxelPlace (pos, Color.white);
					if (tall) {
						env.VoxelPlace (pos + Vector3.up, Color.white);
					}
					pos += direction;
				}
			}

			// Create foes
			StartCoroutine (CreateFoes ());
		}


		IEnumerator CreateFoes ()
		{
			WaitForSeconds waitOneSecond = new WaitForSeconds (1f);

			// Create foes that follows player
			for (int k = 0; k < 3; k++) {
				Vector3 foeInitialPosition = new Vector3 (Random.value * 50 - 25, 51f, 70);
				VoxelChunk chunk = null;
				while (chunk == null || !env.ChunkHasNavMeshReady (chunk)) {
					env.GetChunk (foeInitialPosition, out chunk, true);
					yield return waitOneSecond;
				}
				GameObject foe = GameObject.CreatePrimitive (PrimitiveType.Cylinder);
				foe.transform.position = foeInitialPosition;
				foe.GetComponent<Renderer> ().material.color = Color.black;
				foe.AddComponent<FoeController> ();
			}


		}
	}

}
