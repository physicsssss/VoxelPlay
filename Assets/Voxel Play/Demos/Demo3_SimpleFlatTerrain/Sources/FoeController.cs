using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using VoxelPlay;

namespace VoxelPlayDemos {
	public class FoeController : MonoBehaviour {

		NavMeshAgent agent;

		void Start () {
			agent = gameObject.AddComponent<NavMeshAgent> ();
		}


		// Update is called once per frame
		void Update () {
			if (Random.value > 0.99f) {
				agent.SetDestination (VoxelPlayEnvironment.instance.cameraMain.transform.position);
			}
		}
	}

}
