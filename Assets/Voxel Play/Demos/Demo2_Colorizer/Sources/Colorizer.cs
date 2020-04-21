using UnityEngine;
using UnityEngine.UI;
using VoxelPlay;

namespace VoxelPlayDemos {
				
	public class Colorizer : MonoBehaviour {

		public ModelDefinition modelToLoad;
		public Font font;
		public Shader textShader;
		public Text eraseModeText, globalIllumText;
		public GameObject colorButtonTemplate;

		VoxelPlayEnvironment env;
		float orbitDistance;
		VoxelPlayFirstPersonController fps;
		bool eraseMode;
		byte[] saveGameData;
		Color currentColor = Color.white;
		int modelSize = 32;
		int modelHeight = 32;

		void Start() {
			env = VoxelPlayEnvironment.instance;

			// Position player on the scene and setup orbit parameters
			fps = VoxelPlayFirstPersonController.instance;

			// Create scene objects
			CreateCube();
			CreateLabels();
			CreateColorSwatch();

			// Color the voxel in black when clicked OR place a new voxel
			env.OnVoxelClick += (chunk, voxelIndex, buttonIndex) => {
				if (buttonIndex == 0) {
					if (!eraseMode) {
						// If left click, change the color of voxel to white
						env.VoxelSetColor(chunk, voxelIndex, currentColor);
					}
				} else {
					// If right click, places a new voxel on top of this in the direction of the plane
					Vector3 position = fps.crosshairHitInfo.voxelCenter + fps.crosshairHitInfo.normal;
					env.VoxelPlace(position, Color.red, true);
				}
			};
																
			// Control erase mode
			env.OnVoxelDamaged += (VoxelChunk chunk, int voxelIndex, ref int damage) => {
				if (!eraseMode) {
					damage = 0;
				}
			};
		}

		void Update() {
			// Manages orbit or free mode according to distance to model
			fps.SetOrbitMode(fps.transform.position.sqrMagnitude > orbitDistance);

			if (Input.GetKeyDown(KeyCode.X)) {
				ToggleEraseMode();
			}
			if (Input.GetKeyDown(KeyCode.G)) {
				ToggleGlobalIllum();
			}
			if (Input.GetKeyDown(KeyCode.F1)) {
				LoadModel();
			}
			if (Input.GetKeyDown(KeyCode.F2)) {
				SaveGame();
			}
			if (Input.GetKeyDown(KeyCode.F3)) {
				LoadGame();
			}
		}

		void ToggleEraseMode() {
			eraseMode = !eraseMode;
			if (eraseMode) {
				eraseModeText.text = "<color=green>Erase Mode</color> <color=yellow>ON</color>";
			} else {
				eraseModeText.text = "<color=green>Erase Mode</color> <color=yellow>OFF</color>";
			}
		}

		void ToggleGlobalIllum() {
			env.globalIllumination = !env.globalIllumination;
			env.Redraw();
			if (env.globalIllumination) {
				globalIllumText.text = "<color=green>Global Illum</color> <color=yellow>ON</color>";
			} else {
				globalIllumText.text = "<color=green>Global Illum</color> <color=yellow>OFF</color>";
			}
		}

		void CreateCube() {
			// Fill a 3D array with random colors and place it on the scene
			Color[,,] myModel = new Color[modelSize, modelSize, modelSize];

			int maxY = myModel.GetUpperBound(0);
			int maxZ = myModel.GetUpperBound(1);
			int maxX = myModel.GetUpperBound(2);
			for (int y = 0; y <= maxY; y++) {
				for (int z = 0; z <= maxZ; z++) {
					for (int x = 0; x <= maxX; x++) {
						Color r = new Color(Random.value, Random.value, Random.value);
						myModel[y, z, x] = r;
					}
				}
			}
			env.ModelPlace(Vector3.zero, myModel);
			SetupNavigation();
		}

		void CreateLabels() {
			// Create a label for each row
			font.material.shader = textShader;

			for (int k = 0; k < modelHeight; k++) {
				string rowName = "Row " + k.ToString();
				GameObject t = new GameObject(rowName);
				t.transform.position = new Vector3(0, k + 0.5f, -modelSize / 2);
				TextMesh tm = t.AddComponent<TextMesh>();
				tm.font = font;
				tm.GetComponent<Renderer>().sharedMaterial = font.material;
				tm.text = rowName;
				tm.color = Color.white;
				tm.alignment = TextAlignment.Center;
				tm.anchor = TextAnchor.MiddleCenter;
				t.transform.localScale = new Vector3(0.03f, 0.03f, 1f);
			}
		}

		void CreateColorSwatch() {
			Vector2 pos = colorButtonTemplate.transform.localPosition;
			Random.InitState(0);
			for (int j = 0; j < 6; j++) {
				for (int k = 0; k < 4; k++) {
					Vector2 newPos = new Vector2(pos.x + k * 32, pos.y - j * 32);
					GameObject newColorSwatch = Instantiate<GameObject>(colorButtonTemplate);
					newColorSwatch.SetActive(true);
					newColorSwatch.transform.SetParent(colorButtonTemplate.transform.parent, false);
					newColorSwatch.transform.localPosition = newPos;
					Button button = newColorSwatch.GetComponent<Button>();
					ColorBlock colorBlock = button.colors;
					Color buttonColor = new Color(Random.value, Random.value, Random.value);
					colorBlock.normalColor = buttonColor;
					colorBlock.pressedColor = new Color(buttonColor.r, buttonColor.g, buttonColor.b, 0.5f);
					colorBlock.highlightedColor = new Color(buttonColor.r * 1.1f, buttonColor.g * 1.1f, buttonColor.b * 1.1f);
					button.colors = colorBlock;
					button.onClick.AddListener(() => {
							currentColor = colorBlock.normalColor;
						});
																			
				}
			}
		}

		void SaveGame() {
			saveGameData = env.SaveGameToByteArray();
			env.ShowMessage("<color=yellow>World saved into memory!</color>");
		}

		void LoadGame() {
			if (env.LoadGameFromByteArray(saveGameData, true)) {
				env.ShowMessage("<color=yellow>World restored!</color>");
			} else {
				env.ShowError("<color=red>World could not be restored!</color>");
			}
		}

		void LoadModel() {
			if (modelToLoad != null) {
				env.DestroyAllVoxels();
				env.ModelPlace(Vector3.zero, modelToLoad);
				modelSize = Mathf.Max(modelToLoad.sizeX, modelToLoad.sizeZ);
				modelHeight = modelToLoad.sizeY;
				SetupNavigation();
			}
		}

		void SetupNavigation() {
			fps.transform.position = new Vector3(0, modelSize / 2f, -modelSize - 20f);
			fps.lookAt = new Vector3(0, modelSize / 2f, 0);
			orbitDistance = Mathf.Pow(modelSize * 1.1f, 2f);
		}
	}

}