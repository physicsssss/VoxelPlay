using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{
				
	public class TorchLightAnimator : MonoBehaviour
	{

		[Range (0, 10)]
		public int sparksCount = 5;
		public GameObject sparkPrefab;
		public float sparkSpread = 0.5f;
		public float sparkVerticalSpeed = 2.0f;
		public float sparkRibbonSpeed = 5.0f;
		public float sparkLiftTime = 2f;
		Material mat;
		GameObject[] sparks;
		float[] sparksTime;
		float[] sparksVerticalSpeed;
		Light pointLight;
		public float pointLightMaxIntensity = 1.1f;
		public float pointLightMinIntensity = 0.9f;
		float pointLightIntensitySeed;

		void Start ()
		{
			mat = GetComponent<Renderer> ().material;

			sparks = new GameObject[sparksCount];
			sparksTime = new float[sparksCount];
			sparksVerticalSpeed = new float[sparksCount];
			for (int k = 0; k < sparks.Length; k++) {
				sparks [k] = Instantiate<GameObject> (sparkPrefab, transform);
				sparks [k].transform.localScale = Misc.vector3one * 0.1f;
				Vector3 initialPos = Random.insideUnitSphere * 0.5f;
				initialPos.y = Random.Range (0, 4f);
				sparks [k].transform.localPosition = initialPos;
				sparksVerticalSpeed [k] = Random.Range (0.3f, 4f) * sparkVerticalSpeed;
				sparksTime [k] = Random.Range (0f, sparkLiftTime);
			}
			pointLight = GetComponent<Light> ();
			pointLightIntensitySeed = Random.Range(0.0f, 65535.0f);
		}

		void OnBecameVisible ()
		{
			enabled = true;
		}

		void OnBecameInvisible ()
		{
			enabled = false;
		}
	
		// Update is called once per frame
		void Update ()
		{
			mat.mainTextureOffset -= Misc.vector2up * (Time.deltaTime * 2f);

			Vector3 localScale = transform.localScale;
			for (int k = 0; k < sparks.Length; k++) {
				sparksTime [k] += 0.1f;
				if (sparksTime [k] > sparkLiftTime) {
					sparks [k].transform.localPosition = Random.insideUnitSphere * 0.5f;
					sparksTime [k] = 0;
					sparksVerticalSpeed [k] = Random.Range (0.3f, 4f) * sparkVerticalSpeed;
				} else {
					float r = (Random.value - 0.5f) * 0.1f;
					float tr = sparkRibbonSpeed * (Time.time + sparksTime [k] + r);
					Vector3 delta;
					delta.x = localScale.x * (Mathf.Sin (tr) * sparksTime [k]) * sparkSpread;
					delta.z = localScale.y * (Mathf.Cos (tr) * sparksTime [k]) * sparkSpread;
					delta.y = localScale.z * sparksTime [k] * sparksVerticalSpeed [k] * Time.deltaTime;
					sparks [k].transform.position += delta;
				}
			}

			if (pointLight.enabled) {
				float noise = Mathf.PerlinNoise (pointLightIntensitySeed, Time.time * 2f);
				pointLight.intensity = Mathf.Lerp (pointLightMinIntensity, pointLightMaxIntensity, noise);
			}

		}
	}
}
