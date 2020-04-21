using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

namespace VoxelPlay {
	
	public class Door : VoxelPlayInteractiveObject {

		public float speed = 50f;

		public AudioClip sound;


		[NonSerialized]
		public bool isOpen;

		bool shown;
		WaitForEndOfFrame nextFrame;
		bool rotating;
		float targetRotation;
		float baseRotation;
        float direction;
        float currentAngle;

		public override void OnStart () {
			nextFrame = new WaitForEndOfFrame ();
			baseRotation = currentAngle = transform.eulerAngles.y;
		}

		public override void OnPlayerApproach () {
			if (!shown) {
				env.ShowMessage (txt: "<color=green>Press </color><color=yellow>T</color> to open/close this door.", allowDuplicatedMessage: true);
				shown = true;
			}
		}

		public override void OnPlayerGoesAway () {
		}

		public override void OnPlayerAction () {
			if (speed <= 0)
				return;

			float openRotation = customTag.Equals ("left") ? -90 : 90;
			isOpen = !isOpen;
			if (isOpen && sound != null) {
				AudioSource.PlayClipAtPoint (sound, transform.position);
			}
			targetRotation = isOpen ? baseRotation + openRotation : baseRotation;
            direction = targetRotation > currentAngle ? 1 : -1;
            if (!rotating) {
				rotating = true;
				StartCoroutine (RotateDoor ());
			}
		}

		IEnumerator RotateDoor () {

			for (;;) {
                currentAngle += speed * Time.deltaTime * direction;
                float sign = targetRotation > currentAngle ? 1 : -1;
                bool ends = false;
                if (sign != direction) {
                    currentAngle = targetRotation;
                    ends = true;
                }
                transform.eulerAngles = new Vector3 (0, currentAngle, 0);
				if (ends) {
					if (!isOpen && sound != null) {
						AudioSource.PlayClipAtPoint (sound, transform.position);
					}
					rotating = false;
					yield break;
				}
				yield return nextFrame;
			}
		
		}
	
	}

}