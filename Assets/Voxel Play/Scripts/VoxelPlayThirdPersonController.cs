// Third person controller. Derived and expanded version from Unity standard asset's third person controller

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VoxelPlay {

	[RequireComponent (typeof(Rigidbody))]
	[RequireComponent (typeof(CapsuleCollider))]

	public partial class VoxelPlayThirdPersonController : VoxelPlayCharacterControllerBase {

		public bool useThirdPartyController = false;

		[Header ("Camera")]
		public Camera m_Camera;
		public Vector2 cameraFixedRotationAngles;
		public float cameraDistance = 5f;
		public float cameraMinDistance = 3f;
		public float cameraMaxDistance = 20f;
		public float cameraZoomMultiplier = 10f;
		public float cameraZoomDuration = 0.9f;
		public float cameraXSpeed = 2f;
		public float cameraYSpeed = 2f;
		public float cameraYMinLimit = -20f;
		public float cameraYMaxLimit = 80f;
		public float cameraOrthoMinSize = 1;
		public float cameraOrthoMaxSize = 20;
		public float cameraOrthoDistance = 150;
		public bool avoidObstacles;


		[Header ("Movement")]
		[Tooltip ("Disable camera & movement options to allow other third person controllers")]
		public bool alwaysRun = true;
		[SerializeField] float m_MovingTurnSpeed = 360;
		[SerializeField] float m_StationaryTurnSpeed = 180;
		[SerializeField] float m_JumpPower = 12f;
		[SerializeField] float m_ClimbSpeed = 2f;
		[Range (1f, 4f)][SerializeField] float m_GravityMultiplier = 2f;
		[SerializeField] float m_RunCycleLegOffset = 0.2f;
		//specific to the character in sample assets, will need to be modified to work with others
		[SerializeField] float m_MoveSpeedMultiplier = 1f;
		[SerializeField] float m_AnimSpeedMultiplier = 1f;
		[SerializeField] float m_GroundCheckDistance = 0.1f;

		public string attackAnimationState;
		[SerializeField, HideInInspector]

        public override float GetCharacterHeight() {
            return m_CapsuleHeight;
        }


		bool m_Jump;
		float lastHitButtonPressed;


		Rigidbody m_Rigidbody;
		Animator m_Animator;
		float m_OrigGroundCheckDistance;
		const float k_Half = 0.5f;
		float m_TurnAmount;
		float m_ForwardAmount;
		float m_CapsuleHeight;
		Vector3 m_CapsuleCenter;
		float m_CapsuleRadius;
		CapsuleCollider m_Capsule;
		Vector3 m_CamForward;
		Vector3 m_Move;
		bool m_Climbing;
		bool falling;
		float fallAltitude;
		Vector3 curPos;
		float cameraX, cameraY;
		bool seeking;
		float zoomStartTime;
		float wheel = 0;
		float lastReachDistance, timeSeeking;
		VoxelHitInfo seekTarget;
        bool firePressed;

		enum SeekAction {
			Hit,
			Move
		}

		SeekAction seekAction;


		static VoxelPlayThirdPersonController _thirdPersonController;


		public static VoxelPlayThirdPersonController instance {
			get {
				if (_thirdPersonController == null) {
					_thirdPersonController = FindObjectOfType<VoxelPlayThirdPersonController> ();
				}
				return _thirdPersonController;
			}
		}

		Vector3 characterCenter {
			get {
                return transform.position + new Vector3(0, GetCharacterHeight() * 0.5f * transform.lossyScale.y, 0);
			}
		}

		void OnEnable () {
			env = VoxelPlayEnvironment.instance;
			if (env == null) {
				Debug.LogError ("Voxel Play Environment must be added first.");
			} else {
				env.characterController = this;
			}
		}

		void Start () {
			Init ();

			m_Capsule = GetComponent<CapsuleCollider> ();
			if (m_Capsule != null) {
				m_CapsuleHeight = m_Capsule.height * transform.lossyScale.y;
				m_CapsuleCenter = m_Capsule.center;

				if (Application.isPlaying && !useThirdPartyController && m_Capsule.sharedMaterial == null) {
					// ZeroFriction physic material is used to make easier climbing - however it's only used by this controller so if you're using other controllers do not touch the collider
					PhysicMaterial mat = Resources.Load<PhysicMaterial> ("VoxelPlay/Materials/ZeroFriction");
					m_Capsule.sharedMaterial = mat;
				}
			}

			m_Rigidbody = GetComponent<Rigidbody> ();
			if (m_Rigidbody != null) {
				m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
			}
			m_OrigGroundCheckDistance = m_GroundCheckDistance;
			m_Animator = GetComponentInChildren<Animator> ();

			// Try to assign our designed camera
			if (m_Camera == null) {
				m_Camera = Camera.main;
			}
			if (env != null && m_Camera != null) {
				env.cameraMain = m_Camera;
			}
			// If no camera assigned, get Voxel Play Environment available camera
			if (m_Camera == null) {
				m_Camera = env.cameraMain;
			}

			if (env == null || !env.applicationIsPlaying)
				return;

			ToggleCharacterController (false);

			// Position character on ground
			if (!env.saveFileIsLoaded) {
				if (startOnFlat && env.world != null) {
					float minAltitude = env.world.terrainGenerator.maxHeight;
					Vector3 flatPos = transform.position;
					Vector3 randomPos;
					for (int k = 0; k < startOnFlatIterations; k++) {
						randomPos = Random.insideUnitSphere * 1000;
						float alt = env.GetTerrainHeight (randomPos);
						if (alt < minAltitude && alt >= env.waterLevel + 1) {
							minAltitude = alt;
                            randomPos.y = alt + GetCharacterHeight() * 0.5f + 0.1f;
							flatPos = randomPos;
						}
					}
					transform.position = flatPos;
				}
			}

			input = env.input;

			UpdateLook ();

			InitCrosshair ();

			if (!env.initialized) {
				env.OnInitialized += () => WaitForCurrentChunk ();
			} else {
				WaitForCurrentChunk ();
			}
		}

		public override void UpdateLook () {
			if (m_Camera != null) {
				Vector3 angles = m_Camera.transform.eulerAngles;
				cameraX = angles.y;
				cameraY = angles.x;
				UpdateCamera (false);
			}
		}

		/// <summary>
		/// Disables character controller until chunk is ready
		/// </summary>
		public void WaitForCurrentChunk () {
			ToggleCharacterController (false);
			StartCoroutine (WaitForCurrentChunkCoroutine ());
		}

		/// <summary>
		/// Enables/disables character controller
		/// </summary>
		/// <param name="state">If set to <c>true</c> state.</param>
		public void ToggleCharacterController (bool state) {
			if (m_Rigidbody != null) {
				m_Rigidbody.isKinematic = !state;
			}
			enabled = state;
		}

		/// <summary>
		/// Ensures player chunk is finished before allow player movement / interaction with colliders
		/// </summary>
		IEnumerator WaitForCurrentChunkCoroutine () {
			// Wait until current player chunk is rendered
			WaitForSeconds w = new WaitForSeconds (0.2f);
			for (int k = 0; k < 20; k++) {
				VoxelChunk chunk = env.GetCurrentChunk ();
				if (chunk != null && chunk.isRendered) {
					break;
				}
				yield return w;
			}
			Unstuck ();
			ToggleCharacterController (true);
		}


		void Update ()
		{
			UpdateImpl ();
		}

		protected virtual void UpdateImpl () { 

			curPos = transform.position;
			CheckWaterStatus ();

			if (useThirdPartyController) {
				UpdateSimplified ();
			} else {
				UpdateWithCharacterController ();
			}

			ControllerUpdate ();
		}

		void UpdateSimplified () {
			if (input == null || !input.focused || !input.enabled)
				return;

			CheckCommonKeys ();
		}


		void UpdateWithCharacterController () {

			if (input == null || !input.enabled)
				return;
			
			m_CapsuleRadius = m_Capsule.radius * transform.lossyScale.x;

			if (!m_Jump) {
				m_Jump = input.GetButtonDown (InputButtonNames.Jump);
			}

			CheckFootfalls ();

			// Process click events
			if (input.focused && input.enabled) {
				
				CheckCommonKeys ();

				// Toggles Flight mode
				if (input.GetButtonDown (InputButtonNames.Fly)) {
					isFlying = !isFlying;
					if (isFlying) {
						env.ShowMessage ("<color=green>Flying <color=yellow>ON</color></color>");
					} else {
						env.ShowMessage ("<color=green>Flying <color=yellow>OFF</color></color>");
					}
				}

				if (isGrounded && input.GetButtonDown (InputButtonNames.Crouch)) {
					SetCrouching (!isCrouched);
				} else if (input.GetButtonDown (InputButtonNames.Light)) {
					ToggleCharacterLight ();
				} else if (input.GetButtonDown (InputButtonNames.ThrowItem)) {
					Vector3 direction = transform.forward;
					direction.y = 1f;
					ThrowCurrentItem (characterCenter, direction);
				}
			}

			UpdateCamera (true);
		}

		void CheckCommonKeys () {

			bool leftAltPressed = input.GetButton (InputButtonNames.LeftAlt);
			bool leftShiftPressed = input.GetButton (InputButtonNames.LeftShift);
			bool leftControlPressed = input.GetButton (InputButtonNames.LeftControl);

			bool fire1Clicked = false;
			bool fire2Clicked = false;

			bool overUI = EventSystem.current.IsPointerOverGameObject ();
			if (!overUI) {
                fire1Clicked = input.GetButtonDown (InputButtonNames.Button1);
				fire2Clicked = input.GetButtonClick (InputButtonNames.Button2);
			}

			if (!leftShiftPressed && !leftAltPressed && !leftControlPressed) {
                if (crosshairOnBlock && input.GetButtonClick(InputButtonNames.Button1)) {
                    env.TriggerVoxelClickEvent(crosshairHitInfo.chunk, crosshairHitInfo.voxelIndex, 0);
                }
				if (fire1Clicked) {
                    firePressed = true;
					if (ModelPreviewCancel ()) {
                        firePressed = false;
						lastHitButtonPressed = Time.time + 0.5f;
					}
					if (firePressed && Time.time - lastHitButtonPressed > player.GetHitDelay()) {
						timeSeeking = Time.time;
						lastReachDistance = float.MaxValue;
                        if (crosshairHitInfo.item != null) {
                            crosshairHitInfo.item.PickItem ();
                            crosshairOnBlock = false;
                            firePressed = false;
                        } else {
                            DoHit (player.GetHitDamage());
                        }
					}
				}

				if (fire2Clicked) {
					timeSeeking = Time.time;
					lastReachDistance = float.MaxValue;
					seekTarget = crosshairHitInfo;
					// make character approach target if needed
					seeking = !TargetIsReachable (true);
					if (seeking) {
						seekAction = SeekAction.Move;
					} else {
						DoBuild (curPos, transform.forward, crosshairHitInfo.voxelCenter);
					}
				}
			}


			if (input.GetButtonDown (InputButtonNames.Build)) {
				env.SetBuildMode (!env.buildMode);
				if (env.buildMode) {
					env.ShowMessage ("<color=green>Entered <color=yellow>Build Mode</color>. Press <color=white>B</color> to cancel.</color>");
				} else {
					env.ShowMessage ("<color=green>Back to <color=yellow>Normal Mode</color>.</color>");
				}
			} else if (input.GetButtonDown (InputButtonNames.SeeThroughUp)) {
				env.seeThroughHeightOffset++;
			} else if (input.GetButtonDown (InputButtonNames.SeeThroughDown)) {
				env.seeThroughHeightOffset--;
			}
		}

        // Fixed update is called in sync with physics
        private void FixedUpdate () {
            FixedUpdateImpl ();
        }

        protected virtual void FixedUpdateImpl() { 
			if (useThirdPartyController || input == null || !input.enabled)
				return;
			
			// read inputs
			float h = input.horizontalAxis;
			float v = input.verticalAxis;
			isPressingMoveKeys = h != 0 || v != 0;

			// if seeking target, change move
			if (isPressingMoveKeys) {
				seeking = false;
			}

			if (seeking) {
				// Check orientation
				if (TargetIsReachable ()) {
					seeking = false;
					if (seekAction == SeekAction.Hit) {
						StartCoroutine (CompleteHit (player.GetHitDamage()));
					}
					m_Move = Vector3.zero;
				} else {
					m_Move = seekTarget.voxelCenter - transform.position;
					m_Move.y = 0;
					m_Move.Normalize ();
				}
			} else {
				// calculate move direction to pass to character
				// calculate camera relative direction to move:
				m_CamForward = Vector3.Scale (m_Camera.transform.forward, new Vector3 (1, 0, 1)).normalized;
				m_Move = v * m_CamForward + h * m_Camera.transform.right;
			}

			isMoving = m_Move.sqrMagnitude > 0;
			isRunning = false;

			// run speed multiplier
			float speed = 0;
			if (isMoving) {
				if (isInWater) {
					speed = 0.4f;
				} else {
					if (input.GetButton (InputButtonNames.LeftShift)) {
						if (alwaysRun) {
							speed = 0.5f;
						} else {
							speed = 2f;
							isRunning = true;
						}
					} else {
						if (alwaysRun) {
							speed = 2f;
							isRunning = true;
						} else {
							speed = 0.5f;
						}
					}
				}
				m_Move *= speed;
			
				// allow character to walk over voxels that are one position above current altitude
				if (isGrounded) {
					Vector3 dir = m_Move.normalized;
					dir.y = 0.1f;
					Vector3 frontPos = transform.position + dir * (0.5f + m_CapsuleRadius);
					if (env.IsWallAtPosition (frontPos)) {
						// Make sure there's a walkable voxel in front of player
						frontPos.y++;
						if (!env.IsWallAtPosition (frontPos)) {
							dir = m_Move;
							dir.y = 1f;
							m_Rigidbody.velocity = dir * m_ClimbSpeed;
							if (!m_Climbing) {
								m_Climbing = true;
							}
						}
					}
				}
			} else {
				m_Climbing = false;
			}

			// pass all parameters to the character control script
			Move (m_Move, m_Jump);
			m_Jump = false;

			if (isInWater) {
				ProgressSwimCycle (m_Rigidbody.velocity, speed);
			} else if (!m_Climbing) {
				ProgressStepCycle (m_Rigidbody.velocity, speed);
			}
		}


		public void Move (Vector3 move, bool jump) {

			// convert the world relative moveInput vector into a local-relative
			// turn amount and forward amount required to head in the desired
			// direction.
			if (move.magnitude > 1f)
				move.Normalize ();
			move = transform.InverseTransformDirection (move);
			CheckGroundStatus ();

			Vector3 curPos = m_Rigidbody.position;

			move = Vector3.ProjectOnPlane (move, Misc.vector3up);
			m_TurnAmount = Mathf.Atan2 (move.x, move.z);
			m_ForwardAmount = move.z;

			ApplyExtraTurnRotation ();

			if (!m_Climbing) {
				
				// control and velocity handling is different when grounded and airborne:
				if (isGrounded) {
					HandleGroundedMovement (jump);
				} else {
					HandleAirborneMovement ();
				}

				// Check limits
				if (limitBoundsEnabled && !limitBounds.Contains (m_Rigidbody.position)) {
					m_Rigidbody.position = curPos;
				}
			}

			// send input and other state parameters to the animator
			UpdateAnimator (move);
		}


		void SetCrouching (bool state) {
			isCrouched = state;
			if (isCrouched) {
				env.ShowMessage ("<color=green>Crouching <color=yellow>ON</color></color>");
			} else {
				env.ShowMessage ("<color=green>Crouching <color=yellow>OFF</color></color>");
			}
			ScaleCapsuleForCrouching (isCrouched);
		}


		void ScaleCapsuleForCrouching (bool crouch) {
			if (isGrounded && crouch) {
				if (isCrouched)
					return;
				m_Capsule.height = m_Capsule.height / 2f;
				m_Capsule.center = m_Capsule.center / 2f;
			} else {
				Ray crouchRay = new Ray (m_Rigidbody.position + Vector3.up * m_CapsuleRadius * k_Half, Vector3.up);
				float crouchRayLength = m_CapsuleHeight - m_CapsuleRadius * k_Half;
				if (Physics.SphereCast (crouchRay, m_CapsuleRadius * k_Half, crouchRayLength, Physics.AllLayers, QueryTriggerInteraction.Ignore)) {
					isCrouched = true;
					return;
				}
				m_Capsule.height = m_CapsuleHeight / transform.lossyScale.y;
				m_Capsule.center = m_CapsuleCenter;
			}
		}



		void UpdateAnimator (Vector3 move) {
			// update the animator parameters
			m_Animator.SetFloat ("Forward", m_ForwardAmount, 0.1f, Time.deltaTime);
			m_Animator.SetFloat ("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
			m_Animator.SetBool ("Crouch", isCrouched);
			m_Animator.SetBool ("OnGround", isGrounded);
			if (!isGrounded) {
				m_Animator.SetFloat ("Jump", m_Rigidbody.velocity.y);
			}

			// calculate which leg is behind, so as to leave that leg trailing in the jump animation
			// (This code is reliant on the specific run cycle offset in our animations,
			// and assumes one leg passes the other at the normalized clip times of 0.0 and 0.5)
			float runCycle =
				Mathf.Repeat (
					m_Animator.GetCurrentAnimatorStateInfo (0).normalizedTime + m_RunCycleLegOffset, 1);
			float jumpLeg = (runCycle < k_Half ? 1 : -1) * m_ForwardAmount;
			if (isGrounded) {
				m_Animator.SetFloat ("JumpLeg", jumpLeg);
			}

			// the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector,
			// which affects the movement speed because of the root motion.
			if (isGrounded && move.magnitude > 0) {
				m_Animator.speed = m_AnimSpeedMultiplier;
			} else {
				// don't use that while airborne
				m_Animator.speed = 1;
			}
		}


		void HandleAirborneMovement () {
			// apply extra gravity from multiplier:
			Vector3 extraGravityForce = (Physics.gravity * m_GravityMultiplier) - Physics.gravity;
			extraGravityForce += m_Move * 4.0f;
			m_Rigidbody.AddForce (extraGravityForce);
			m_GroundCheckDistance = m_Rigidbody.velocity.y < 0 ? m_OrigGroundCheckDistance : 0.01f;
		}


		void HandleGroundedMovement (bool jump) {
			// check whether conditions are right to allow a jump:
			if (jump && !isCrouched && m_Animator.GetCurrentAnimatorStateInfo (0).IsName ("Grounded")) {
				// jump!
				m_Rigidbody.velocity = new Vector3 (m_Rigidbody.velocity.x, m_JumpPower, m_Rigidbody.velocity.z);
				isGrounded = false;
				m_Animator.applyRootMotion = false;
				m_GroundCheckDistance = 0.1f;
				PlayJumpSound ();
			}
		}


		void ApplyExtraTurnRotation () {
			// help the character turn faster (this is in addition to root rotation in the animation)
			float turnSpeed = Mathf.Lerp (m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
			transform.Rotate (0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);
		}


		public void OnAnimatorMove () {
			// we implement this function to override the default root motion.
			// this allows us to modify the positional speed before it's applied.
			if (isGrounded && Time.deltaTime > 0) {
				Vector3 v = (m_Animator.deltaPosition * m_MoveSpeedMultiplier) / Time.deltaTime;

				// we preserve the existing y part of the current velocity.
				v.y = m_Rigidbody.velocity.y;
				m_Rigidbody.velocity = v;
			}
		}


		void CheckGroundStatus () {
			if (GroundCheck ()) {
				m_Climbing = false;
				if (!isGrounded && !isInWater) {
					PlayLandingSound ();
				}
				isGrounded = true;
				falling = false;
				m_Animator.applyRootMotion = true;
			} else if (!m_Climbing) {
				// Annotate fall distance
				if (falling) {
					float fallDistance = fallAltitude - transform.position.y;
					if (fallDistance > 1f) {
						isGrounded = false;
						m_Animator.applyRootMotion = false;
					}
				} else {
					falling = true;
					fallAltitude = transform.position.y;
				}
			}
		}


		bool GroundCheck () {
			// 0.1f is a small offset to start the ray from inside the character
			// it is also good to note that the transform position in the sample assets is at the base of the character
			Vector3 pos = transform.position + (Vector3.up * 0.1f * transform.lossyScale.y);
			#if UNITY_EDITOR
			// helper to visualise the ground check ray in the scene view
			Debug.DrawLine (pos, pos + (Vector3.down * m_GroundCheckDistance), Color.yellow);
			#endif
			if (Physics.Raycast (pos, Vector3.down, m_GroundCheckDistance)) {
				return true;
			}
			pos.x += m_CapsuleRadius;
			pos.z += m_CapsuleRadius;
			#if UNITY_EDITOR
			// helper to visualise the ground check ray in the scene view
			Debug.DrawLine (pos, pos + (Vector3.down * m_GroundCheckDistance), Color.yellow);
			#endif
			if (Physics.Raycast (pos, Vector3.down, m_GroundCheckDistance))
				return true;
			pos.z -= m_CapsuleRadius * 2f;
			#if UNITY_EDITOR
			// helper to visualise the ground check ray in the scene view
			Debug.DrawLine (pos, pos + (Vector3.down * m_GroundCheckDistance), Color.yellow);
			#endif
			if (Physics.Raycast (pos, Vector3.down, m_GroundCheckDistance))
				return true;
			pos.x -= m_CapsuleRadius * 2f;
			#if UNITY_EDITOR
			// helper to visualise the ground check ray in the scene view
			Debug.DrawLine (pos, pos + (Vector3.down * m_GroundCheckDistance), Color.yellow);
			#endif
			if (Physics.Raycast (pos, Vector3.down, m_GroundCheckDistance))
				return true;
			pos.z += m_CapsuleRadius * 2f;
			#if UNITY_EDITOR
			// helper to visualise the ground check ray in the scene view
			Debug.DrawLine (pos, pos + (Vector3.down * m_GroundCheckDistance), Color.yellow);
			#endif
			if (Physics.Raycast (pos, Vector3.down, m_GroundCheckDistance))
				return true;

			return false;
		}

		void CheckWaterStatus () {

			bool wasInWater = isInWater;

			isInWater = false;
			isSwimming = false;
			isUnderwater = false;

			if (env.waterLevel == 0)
				return;

			// Check water on character controller position (which is at base of character)
			Voxel voxelCh = env.GetVoxel (curPos + new Vector3 (0, 0.3f, 0));
			CheckDamage (voxelCh.type);

			// Safety check to avoid character go under terrain
			if (voxelCh.isSolid) {
				Unstuck ();
                return;
            }

            AnnotateNonCollidingPosition(curPos);

			if (voxelCh.GetWaterLevel () > 7) { 
				isSwimming = true;
			}

			isInWater = isSwimming || isUnderwater;
			if (!wasInWater && isInWater) {
				PlayWaterSplashSound ();
			}
		}


		void UpdateCamera (bool smooth) {

			if (useThirdPartyController)
				return;

			float oldCameraX = cameraX;
			float oldCameraY = cameraY;

			if (input != null) {
				float w = input.mouseScrollWheel * cameraZoomMultiplier;
				if (w != 0) {
					zoomStartTime = Time.time;
					wheel += w;
				}
			}
			wheel *= 0.9f;
			if (wheel < 0.001f && wheel > -0.001f) {
				wheel = 0;
			}

			Quaternion rotation;
			if (input.GetButton (InputButtonNames.Button2)) {
				cameraX += input.mouseX * cameraXSpeed;
				cameraY -= input.mouseY * cameraYSpeed;
				cameraY = ClampAngle (cameraY, cameraYMinLimit, cameraYMaxLimit);
				smooth = false;
			}
			if (cameraFixedRotationAngles.x != 0) {
				cameraY = cameraFixedRotationAngles.x;
			}
			if (cameraFixedRotationAngles.y != 0) {
				cameraX = cameraFixedRotationAngles.y;
			}
			rotation = Quaternion.Euler (cameraY, cameraX, 0);
	
			Vector3 targetPos = transform.position + Misc.vector3up * (m_CapsuleHeight * 0.5f);
			Vector3 position;
			// orthographic support
			if (m_Camera.orthographic) {
				float newSize = Mathf.Lerp (m_Camera.orthographicSize, m_Camera.orthographicSize + wheel, Time.deltaTime);
				newSize = Mathf.Clamp (newSize, cameraOrthoMinSize, cameraOrthoMaxSize);
				m_Camera.orthographicSize = newSize;
				Vector3 negDistance = new Vector3 (0.0f, 0.0f, -cameraOrthoDistance);
				position = rotation * negDistance + targetPos;
			} else {
				cameraDistance += wheel;
				VoxelHitInfo hitInfo;
				float distance = Vector3.Distance (targetPos, m_Camera.transform.position);
				Vector3 direction = (targetPos - m_Camera.transform.position) / distance;
				if (avoidObstacles && env.RayCast (m_Camera.transform.position, direction, out hitInfo, distance, 3, ColliderTypes.IgnorePlayer)) {
					cameraDistance -= hitInfo.distance + 0.1f;
				}
				cameraDistance = Mathf.Clamp (cameraDistance, cameraMinDistance, cameraMaxDistance);

				Vector3 negDistance = new Vector3 (0.0f, 0.0f, -cameraDistance);
				position = rotation * negDistance + targetPos;

				// check there's no voxel under camera to avoid clipping with ground
				Vector3 pos = position;
				pos.y -= 0.25f;
				if (env.IsWallAtPosition (pos)) {
					cameraX = oldCameraX;
					cameraY = oldCameraY;
					rotation = Quaternion.Euler (cameraY, cameraX, 0);
					position = rotation * negDistance + targetPos;
				}
			}

			// move camera
			m_Camera.transform.rotation = rotation;
			if (smooth) {
				float t = (Time.time - zoomStartTime) / cameraZoomDuration;
				if (t > 1) {
					t = 1;
				}
				m_Camera.transform.position = Vector3.Lerp (m_Camera.transform.position, position, t);
			} else {
				m_Camera.transform.position = position;
			}
		}

		public static float ClampAngle (float angle, float min, float max) {
			if (angle < -360F)
				angle += 360F;
			if (angle > 360F)
				angle -= 360F;
			return Mathf.Clamp (angle, min, max);
		}

        protected virtual void DoHit (int damage) {

			if (crosshairHitInfo.voxelIndex < 0) {
				return;
			}
		
			lastHitButtonPressed = Time.time;
			seekTarget = crosshairHitInfo;
			// make character approach target if needed
			seeking = !TargetIsReachable ();
			if (seeking) {
				seekAction = SeekAction.Hit;
			} else {
				StartCoroutine (CompleteHit (damage));
			}
		}

		bool TargetIsReachable (bool ignoreAngle = false) {
			Vector3 v = seekTarget.voxelCenter - characterCenter;
			float sqrDistance = v.sqrMagnitude;
			if (sqrDistance <= 3) {
                if (ignoreAngle) return true;
				v.y = 0;
				float angle = Vector3.Angle (v, transform.forward);
				if (angle < 30)
					return true;

			}
			// Check if character can still move towards the target
			bool hasElapsed = (Time.time - timeSeeking) > 1f;
			if (sqrDistance < lastReachDistance) {
				lastReachDistance = sqrDistance;
				if (hasElapsed) {
					timeSeeking = Time.time;
				}
			} else if (hasElapsed) {
				seeking = false;
			}

			return false;
		}


		IEnumerator CompleteHit (int damage) {
			seeking = false;

			if (attackAnimationState != null) {
				m_Animator.Play (attackAnimationState);
				yield return new WaitForSeconds (0.3f);
			} 
			env.VoxelDamage (seekTarget, player.GetHitDamage(), true, true);
		}

		Vector3 GetRayOrigin () {
			return characterCenter + transform.forward;
		}


		/// <summary>
		/// Moves character controller to a new position. Use this method instead of changing the transform position
		/// </summary>
		public override void MoveTo (Vector3 newPosition) {
            m_Rigidbody.position = newPosition;
		}

	}

}
