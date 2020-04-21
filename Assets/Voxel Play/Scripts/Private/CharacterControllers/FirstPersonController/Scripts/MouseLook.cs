using System;
using UnityEngine;

namespace VoxelPlay {
	[Serializable]
	public class MouseLook {
		public float XSensitivity = 2f;
		public float YSensitivity = 2f;
		public bool clampVerticalRotation = true;
		public float MinimumX = -90F;
		public float MaximumX = 90F;
		public bool smooth;
		public float smoothTime = 5f;
		public bool lockCursor = true;

		Quaternion m_CharacterTargetRot;
		Quaternion m_CameraTargetRot;
		bool m_cursorIsLocked = true;
		VoxelPlayInputController m_Input;

		public void Init (Transform character, Transform camera, VoxelPlayInputController input) {
			m_CharacterTargetRot = character.localRotation;
			m_CameraTargetRot = camera.localRotation;
			if (input != null) {
				m_Input = input;
			}
		}

		public void LookRotation (Transform character, Transform camera, bool orbitMode, Vector3 lookAt, float transition) {

			if (!m_cursorIsLocked || m_Input == null || !m_Input.enabled || UnityEngine.XR.XRSettings.enabled)
				return;

			if (orbitMode) {
				if (transition < 1f) {
					Quaternion lr = Quaternion.LookRotation ((lookAt - camera.transform.position));
					camera.localRotation = Quaternion.Slerp (camera.localRotation, lr, transition);
					character.localRotation = Quaternion.Slerp (character.localRotation, Misc.quaternionZero, transition);
				} else {
					camera.LookAt (lookAt);
					character.localRotation = Misc.quaternionZero;
				}
				// sync values with other mode
				Vector3 angles = camera.eulerAngles;
				m_CharacterTargetRot = Quaternion.Euler (0, angles.y, 0);
				m_CameraTargetRot = Quaternion.Euler (angles.x, 0, 0);
			} else {
				float yRot = m_Input.mouseX * XSensitivity;
				float xRot = m_Input.mouseY * YSensitivity;
				m_CharacterTargetRot *= Quaternion.Euler (0f, yRot, 0f);
				m_CameraTargetRot *= Quaternion.Euler (-xRot, 0f, 0f);

				if (clampVerticalRotation)
					m_CameraTargetRot = ClampRotationAroundXAxis (m_CameraTargetRot);
												
				if (smooth) {
					character.localRotation = Quaternion.Slerp (character.localRotation, m_CharacterTargetRot,
						smoothTime * Time.deltaTime);
					camera.localRotation = Quaternion.Slerp (camera.localRotation, m_CameraTargetRot,
						smoothTime * Time.deltaTime);
				} else {
					character.localRotation = m_CharacterTargetRot;
					camera.localRotation = m_CameraTargetRot;
				}
			}
																
			UpdateCursorLock ();
		}

		public void SetCursorLock (bool value) {
			lockCursor = value;
			if (lockCursor) {
				m_cursorIsLocked = true;
			} else {//we force unlock the cursor if the user disable the cursor locking helper
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}

		public void UpdateCursorLock () {

			// if the user set "lockCursor" we check & properly lock the cursor
			if (lockCursor) {
				InternalLockUpdate ();
			}
		}

		private void InternalLockUpdate () {
			if (m_cursorIsLocked) {
				Cursor.lockState = VoxelPlayFirstPersonController.instance.freeMode ? CursorLockMode.None : CursorLockMode.Locked;
				Cursor.visible = false;
			} else if (!m_cursorIsLocked) {
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}

		Quaternion ClampRotationAroundXAxis (Quaternion q) {
			q.x /= q.w;
			q.y /= q.w;
			q.z /= q.w;
			q.w = 1.0f;

			float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan (q.x);

			angleX = Mathf.Clamp (angleX, MinimumX, MaximumX);

			q.x = Mathf.Tan (0.5f * Mathf.Deg2Rad * angleX);

			return q;
		}

	}
}
