using UnityEngine;
using VoxelPlay;

namespace VoxelPlayDemos
{

    public class DemoEarth : MonoBehaviour {

        public GameObject deerPrefab;
		public GameObject bouncingSpherePrefab;
		VoxelPlayEnvironment env;

		void Start () {
			env = VoxelPlayEnvironment.instance;

			// When Voxel Play is ready, do some stuff...
			env.OnInitialized += OnInitialized;
            
			// Get notified if player is damaged
			VoxelPlayPlayer.instance.OnPlayerGetDamage += OnPlayerGetDamage;

			// Get notified if player is killed
			VoxelPlayPlayer.instance.OnPlayerIsKilled += OnPlayerIsKilled;

        }


		void OnInitialized () {

			// Item definitions are stored in Items folder within the world name folder

			// Add 3 torches to initial player inventory
			// VoxelPlayPlayer.instance.AddInventoryItem (env.GetItemDefinition ("Torch"), 3);
			// VoxelPlayPlayer.instance.AddInventoryItem (env.GetItemDefinition ("Torch Red"), 2);

			// // Add a shovel (no need to specify quantity it's 1 unit)
			// VoxelPlayPlayer.instance.AddInventoryItem (env.GetItemDefinition ("Shovel"));

			// // Add a sword 
			// VoxelPlayPlayer.instance.AddInventoryItem (env.GetItemDefinition ("Sword"));

			// VoxelPlayPlayer.instance.AddInventoryItem (env.GetItemDefinition ("Axe", 0), 3);
			// VoxelPlayPlayer.instance.AddInventoryItem (env.GetItemDefinition ("Pickaxe", 3), 3);
			// VoxelPlayPlayer.instance.AddInventoryItem (env.GetItemDefinition ("Spade", 2), 1);
			// VoxelPlayPlayer.instance.AddInventoryItem (env.GetItemDefinition ("Sword", 3), 7);
			// VoxelPlayPlayer.instance.AddInventoryItem (env.GetItemDefinition ("Hoe", 0));
			// VoxelPlayPlayer.instance.AddInventoryItem (env.GetItemDefinition ("Hoe", 1));
			// VoxelPlayPlayer.instance.AddInventoryItem (env.GetItemDefinition ("Hoe", 2));
			// VoxelPlayPlayer.instance.AddInventoryItem (env.GetItemDefinition ("Hoe", 3));
			// VoxelPlayPlayer.instance.AddInventoryItem (env.GetItemDefinition ("Hoe", 4));
			// VoxelPlayPlayer.instance.AddInventoryItem (env.GetItemDefinition ("Hoe", 5));

			// Add special instructions after 4 seconds of game running
			Invoke ("SpecialKeys", 4);
//			Debug.Log("DemoEarth -> OnInitialized");
		}

		void OnPlayerGetDamage (ref int damage, int remainingLifePoints) {
			Debug.Log ("Player gets " + damage + " damage points (" + remainingLifePoints + " life points left)"); 
		}


		void OnPlayerIsKilled () {
			Debug.Log ("Player is dead!");
		}


		void SpecialKeys () {
			env.ShowMessage ("<color=green>Press <color=yellow>R</color> to throw a ball, <color=yellow>Y</color> to summon a deer, <color=yellow>X</color> to place a brick, <color=yellow>M</color> to levitate a voxel :)</color>", 20, true);
		}

		void Update () {
			// If Voxel Play is not yet initialized OR console is visible, do not react to normal player input
			if (!env.initialized || VoxelPlayUI.instance.IsVisible)
				return;
			if (Input.GetKeyDown (KeyCode.R)) {
				ThrowBall ();
			}
			if (Input.GetKeyDown (KeyCode.Y)) {
				SummonDeer ();
			}
			if (Input.GetKeyDown (KeyCode.X)) {
				PlaceBrick ();
			}
			if (Input.GetKeyDown (KeyCode.M)) {
                LevitateVoxel ();
            }
        }

	
		/// <summary>
		/// Summons a ball that interacts with voxel environment. It can be launched entering in the console "Invoke Demo Ball"
		/// </summary>
		void ThrowBall () {
			GameObject ball = Instantiate (bouncingSpherePrefab);
			ball.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
			ball.GetComponent<Renderer> ().material.color = new Color (Random.value * 0.5f + 0.5f, Random.value * 0.5f + 0.5f, Random.value * 0.5f + 0.5f);

			// Throw it! :)
			ball.GetComponent<Rigidbody> ().velocity = Camera.main.transform.forward * 10f;
		}

		/// <summary>
		/// Summons a deer prefab
		/// </summary>
		void SummonDeer () {
			VoxelHitInfo hitInfo;
			if (env.RayCast (Camera.main.transform.position, Camera.main.transform.forward, out hitInfo)) {
				// Instantiate deer
				GameObject deer = Instantiate (deerPrefab);
				// Position it on ground
				deer.transform.position = hitInfo.point;
				// Important: instantiate material so different deers can have different colors and smooth lighting; we do it by assigning a random color to provide variation
				deer.GetComponent<MeshRenderer> ().material.color = new Color (Random.value * 0.1f + 0.9f, Random.value * 0.1f + 0.9f, 1f);
			}
		}


		/// <summary>
		/// Places a brickWall voxel in front of player. Can be executed in game entering in the console "Invoke Demo PlaceBrick"
		/// </summary>
		void PlaceBrick () {
			// Instead of using Raycast like in the SummonDeer function, will reuse the crosshair data (just another way of doing the same)
			VoxelPlayFirstPersonController fpsController = VoxelPlayFirstPersonController.instance;
			if (fpsController.crosshairOnBlock) {
				Vector3 pos = fpsController.crosshairHitInfo.voxelCenter + fpsController.crosshairHitInfo.normal;
				VoxelDefinition brickWall = env.GetVoxelDefinition ("VoxelBrickWall");
				env.VoxelPlace (pos, brickWall);
			}
		}

		/// <summary>
		/// Converts voxel on the crosshair into a dynamic gameobject
		/// </summary>
		void LevitateVoxel () {
			VoxelPlayFirstPersonController fpsController = VoxelPlayFirstPersonController.instance;
			if (fpsController.crosshairOnBlock) {
				VoxelChunk chunk = fpsController.crosshairHitInfo.chunk;
				int voxelIndex = fpsController.crosshairHitInfo.voxelIndex;
				GameObject obj = env.VoxelGetDynamic (chunk, voxelIndex, true);
				if (obj != null) {
					Rigidbody rb = obj.GetComponent<Rigidbody> ();
					rb.AddForce (Vector3.up * 500f);
				}
			}
		}


	}

}