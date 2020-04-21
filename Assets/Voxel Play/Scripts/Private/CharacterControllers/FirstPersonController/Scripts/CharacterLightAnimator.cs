using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{
				
	public class CharacterLightAnimator : MonoBehaviour
	{

		Light pointLight;
		public float pointLightMaxIntensity = 1.1f;
		public float pointLightMinIntensity = 0.9f;

		void Start ()
		{
			pointLight = GetComponent<Light> ();
		}

		// Update is called once per frame
		void Update ()
		{
			if (pointLight.enabled) {
				float noise = Mathf.PerlinNoise (0, Time.time * 2f);
				pointLight.intensity = Mathf.Lerp (pointLightMinIntensity, pointLightMaxIntensity, noise);
			}
		}
	}
}
