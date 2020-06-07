using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VoxelPlay
{

    [ExecuteInEditMode]
    [HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000001854-voxel-play-fps-controller")]
    public partial class VoxelPlayFirstPersonController : VoxelPlayCharacterControllerBase
    {

        [Header("Movement")]
        public float walkSpeed = 5f;
        public float runSpeed = 10f;
        public float flySpeed = 20f;
        public float swimSpeed = 3.7f;
        public float jumpSpeed = 10f;
        public float stickToGroundForce = 10f;
        public float gravityMultiplier = 2f;
        public MouseLook mouseLook;
        public bool useFovKick = true;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        public bool useHeadBob = true;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();

        [Header("Smooth Climb")]
        public bool smoothClimb = true;
        public float climbYThreshold = 0.5f;
        public float climbSpeed = 4f;


        public override float GetCharacterHeight()
        {
            return hasCharacterController ? m_CharacterController.height : _characterHeight;
        }

        [Header("Orbit")]
        public bool orbitMode;
        public Vector3 lookAt;
        public float minDistance = 1f;
        public float maxDistance = 100f;

        // internal fields
        Camera m_Camera;
        bool m_Jump;
        Vector3 m_Input;
        Vector3 m_MoveDir = Misc.vector3zero;
        CharacterController m_CharacterController;
        CollisionFlags m_CollisionFlags;
        bool m_PreviouslyGrounded;
        Vector3 m_OriginalCameraPosition;
        float prevCrouchYPos;
        float prevCrouchTime;
        bool movingSmooth;

        bool m_Jumping;
        float lastHitButtonPressed;
        GameObject underwaterPanel;
        Material underWaterMat;
        Transform crouch;

        int lastNearClipPosX, lastNearClipPosY, lastNearClipPosZ;
        Vector3 curPos;
        float waterLevelTop;

        const float switchDuration = 2f;
        bool firePressed;
        bool switching;
        float switchingStartTime;
        float switchingLapsed;

        float lastUserCameraNearClipPlane;

        static VoxelPlayFirstPersonController _firstPersonController;
        public bool hasCharacterController;

        public CharacterController characterController
        {
            get { return m_CharacterController; }
        }

        public static VoxelPlayFirstPersonController instance
        {
            get
            {
                if (_firstPersonController == null)
                {
                    _firstPersonController = FindObjectOfType<VoxelPlayFirstPersonController>();
                }
                return _firstPersonController;
            }
        }

        void OnEnable()
        {
            m_CharacterController = GetComponent<CharacterController>();
            hasCharacterController = m_CharacterController != null;
            if (hasCharacterController)
            {
                m_CharacterController.stepOffset = 0.4f;
            }
            env = VoxelPlayEnvironment.instance;
            if (env != null)
            {
                env.characterController = this;
            }
            crouch = transform.Find("Crouch");
        }

        void Start()
        {
            base.Init();
            m_Camera = GetComponentInChildren<Camera>();
            if (m_Camera != null)
            {
                if (env != null)
                {
                    env.cameraMain = m_Camera;
                }
                m_OriginalCameraPosition = m_Camera.transform.localPosition;
                prevCrouchYPos = crouch.position.y;
                if (hasCharacterController)
                {
                    m_FovKick.Setup(m_Camera);
                    m_HeadBob.Setup(m_Camera, footStepInterval);
                }
            }
            m_Jumping = false;

            if (env == null || !env.applicationIsPlaying)
                return;

            InitUnderwaterEffect();

            ToggleCharacterController(false);
            input = env.input;

            if (hasCharacterController)
            {

                // Position character on ground
                if (!env.saveFileIsLoaded)
                {
                    if (startOnFlat && env.world != null)
                    {
                        float minAltitude = env.world.terrainGenerator.maxHeight;
                        Vector3 flatPos = transform.position;
                        Vector3 randomPos;
                        for (int k = 0; k < startOnFlatIterations; k++)
                        {
                            randomPos = Random.insideUnitSphere * 1000;
                            float alt = env.GetTerrainHeight(randomPos);
                            if (alt < minAltitude && alt >= env.waterLevel + 1)
                            {
                                minAltitude = alt;
                                randomPos.y = alt + GetCharacterHeight() + 1;
                                flatPos = randomPos;
                            }
                        }
                        transform.position = flatPos;
                    }
                }

                SetOrbitMode(orbitMode);
                mouseLook.Init(transform, m_Camera.transform, input);
            }

            InitCrosshair();

            if (!env.initialized)
            {
                env.OnInitialized += () => WaitForCurrentChunk();
            }
            else
            {
                WaitForCurrentChunk();
            }
        }

        void InitUnderwaterEffect()
        {
            underwaterPanel = Instantiate<GameObject>(Resources.Load<GameObject>("VoxelPlay/Prefabs/UnderwaterPanel"), m_Camera.transform);
            underwaterPanel.name = "UnderwaterPanel";
            Renderer underwaterRenderer = underwaterPanel.GetComponent<Renderer>();
            underWaterMat = underwaterRenderer.sharedMaterial;
            underWaterMat = Instantiate<Material>(underWaterMat);
            underwaterRenderer.sharedMaterial = underWaterMat;

            underwaterPanel.transform.localPosition = new Vector3(0, 0, m_Camera.nearClipPlane + 0.001f);
            underwaterPanel.SetActive(false);
        }


        public override void UpdateLook()
        {
            // Pass initial rotation to mouseLook script
            if (m_Camera != null)
            {
                mouseLook.Init(characterController.transform, m_Camera.transform, null);
            }
        }


        /// <summary>
        /// Disables character controller until chunk is ready
        /// </summary>
        public void WaitForCurrentChunk()
        {
            ToggleCharacterController(false);
            StartCoroutine(WaitForCurrentChunkCoroutine());
        }

        /// <summary>
        /// Enables/disables character controller
        /// </summary>
        /// <param name="state">If set to <c>true</c> state.</param>
        public void ToggleCharacterController(bool state)
        {
            if (hasCharacterController)
            {
                m_CharacterController.enabled = state;
            }
            enabled = state;
        }

        /// <summary>
        /// Ensures player chunk is finished before allow player movement / interaction with colliders
        /// </summary>
        IEnumerator WaitForCurrentChunkCoroutine()
        {
            // Wait until current player chunk is rendered
            WaitForSeconds w = new WaitForSeconds(0.2f);
            for (int k = 0; k < 20; k++)
            {
                VoxelChunk chunk = env.GetCurrentChunk();
                if (chunk != null && chunk.isRendered)
                {
                    break;
                }
                yield return w;
            }
            Unstuck(true);
            ToggleCharacterController(true);
            if (!hasCharacterController)
            {
                switchingLapsed = 1f;
            }
        }

        void Update()
        {
            UpdateImpl();
        }

        protected virtual void UpdateImpl()
        {
            if (env == null || !env.applicationIsPlaying || !env.initialized || input == null)
                return;

            curPos = transform.position;

            if (hasCharacterController)
            {
                UpdateWithCharacterController();
                if (smoothClimb)
                {
                    SmoothClimb();
                }
            }
            else
            {
                UpdateSimple();
            }

            ControllerUpdate();
        }

        protected virtual void UpdateWithCharacterController()
        {

            CheckFootfalls();

            RotateView();

            if (orbitMode)
                isFlying = true;

            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump && !isFlying)
            {
                m_Jump = input.GetButtonDown(InputButtonNames.Jump);
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;

            // Process click events
            if (input.focused && input.enabled)
            {
                bool leftAltPressed = input.GetButton(InputButtonNames.LeftAlt);
                bool leftShiftPressed = input.GetButton(InputButtonNames.LeftShift);
                bool leftControlPressed = input.GetButton(InputButtonNames.LeftControl);
                bool fire1Clicked = input.GetButtonDown(InputButtonNames.Button1);

                if (fire1Clicked)
                {
                    firePressed = true;
                    if (crosshairOnBlock && input.GetButtonClick(InputButtonNames.Button1))
                    {
                        env.TriggerVoxelClickEvent(crosshairHitInfo.chunk, crosshairHitInfo.voxelIndex, 0);
                    }
                    if (ModelPreviewCancel())
                    {
                        firePressed = false;
                        lastHitButtonPressed = Time.time + 0.5f;
                    }
                }
                else if (!input.GetButton(InputButtonNames.Button1))
                {
                    firePressed = false;
                }

                bool fire2Clicked = input.GetButtonDown(InputButtonNames.Button2);
                if (!leftShiftPressed && !leftAltPressed && !leftControlPressed)
                {
                    if (Time.time - lastHitButtonPressed > player.GetHitDelay())
                    {
                        if (firePressed)
                        {
                            if (crosshairHitInfo.item != null)
                            {
                                crosshairHitInfo.item.PickItem();
                                crosshairOnBlock = false;
                                firePressed = false;
                            }
                            else
                            {
                                DoHit(player.GetHitDamage());
                            }
                        }
                    }
                    if (fire2Clicked)
                    {
                        DoHit(0);
                    }
                }

                if (crosshairOnBlock && input.GetButtonDown(InputButtonNames.MiddleButton))
                {
                    player.SetSelectedItem(crosshairHitInfo.voxel.type);
                }

                if (input.GetButtonDown(InputButtonNames.Build))
                {
                    env.SetBuildMode(!env.buildMode);
                    if (env.buildMode)
                    {
                        env.ShowMessage("<color=green>Entered <color=yellow>Build Mode</color>. Press <color=white>B</color> to cancel.</color>");
                    }
                    else
                    {
                        env.ShowMessage("<color=green>Back to <color=yellow>Normal Mode</color>.</color>");
                    }
                }

                if (fire2Clicked && !leftAltPressed && !leftShiftPressed)
                {
#if UNITY_EDITOR
                    DoBuild(m_Camera.transform.position, m_Camera.transform.forward, voxelHighlightBuilder != null ? voxelHighlightBuilder.transform.position : Misc.vector3zero);
#else
                    DoBuild (m_Camera.transform.position, m_Camera.transform.forward, Misc.vector3zero);
#endif
                }

                // Toggles Flight mode
                if (input.GetButtonDown(InputButtonNames.Fly))
                {
                    isFlying = !isFlying;
                    if (isFlying)
                    {
                        m_Jumping = false;
                        env.ShowMessage("<color=green>Flying <color=yellow>ON</color></color>");
                    }
                    else
                    {
                        env.ShowMessage("<color=green>Flying <color=yellow>OFF</color></color>");
                    }
                }

                if (isGrounded && !isCrouched && input.GetButtonDown(InputButtonNames.LeftControl))
                {
                    isCrouched = true;
                }
                else if (isGrounded && isCrouched && input.GetButtonUp(InputButtonNames.LeftControl))
                {
                    isCrouched = false;
                }
                else if (isGrounded && input.GetButtonDown(InputButtonNames.Crouch))
                {
                    isCrouched = !isCrouched;
                    if (isCrouched)
                    {
                        env.ShowMessage("<color=green>Crouching <color=yellow>ON</color></color>");
                    }
                    else
                    {
                        env.ShowMessage("<color=green>Crouching <color=yellow>OFF</color></color>");
                    }
                }
                else if (input.GetButtonDown(InputButtonNames.Light))
                {
                    ToggleCharacterLight();
                }
                else if (input.GetButtonDown(InputButtonNames.ThrowItem))
                {
                    ThrowCurrentItem(m_Camera.transform.position, m_Camera.transform.forward);
                }
            }

            // Check water
            if (!movingSmooth)
            {
                CheckWaterStatus();

                // Check crouch status
                if (!isInWater)
                {
                    UpdateCrouch();
                }
            }

#if UNITY_EDITOR
            UpdateConstructor();
#endif

        }

        protected virtual void UpdateSimple()
        {

            // Process click events
            if (input.focused && input.enabled)
            {
                bool leftAltPressed = input.GetButton(InputButtonNames.LeftAlt);
                bool leftShiftPressed = input.GetButton(InputButtonNames.LeftShift);
                bool leftControlPressed = input.GetButton(InputButtonNames.LeftControl);
                bool fire1Pressed = input.GetButton(InputButtonNames.Button1);
                bool fire2Clicked = input.GetButtonDown(InputButtonNames.Button2);
                if (!leftShiftPressed && !leftAltPressed && !leftControlPressed)
                {
                    if (Time.time - lastHitButtonPressed > player.GetHitDelay())
                    {
                        if (fire1Pressed)
                        {
                            DoHit(player.GetHitDamage());
                        }
                    }
                    if (fire2Clicked)
                    {
                        DoHit(0);
                    }
                }

                if (crosshairOnBlock && input.GetButtonDown(InputButtonNames.MiddleButton))
                {
                    player.SetSelectedItem(crosshairHitInfo.voxel.type);
                }

                if (input.GetButtonDown(InputButtonNames.Build))
                {
                    env.SetBuildMode(!env.buildMode);
                    if (env.buildMode)
                    {
                        env.ShowMessage("<color=green>Entered <color=yellow>Build Mode</color>. Press <color=white>B</color> to cancel.</color>");
                    }
                    else
                    {
                        env.ShowMessage("<color=green>Back to <color=yellow>Normal Mode</color>.</color>");
                    }
                }

                if (fire2Clicked && !leftAltPressed && !leftShiftPressed)
                {
#if UNITY_EDITOR
                    DoBuild(m_Camera.transform.position, m_Camera.transform.forward, voxelHighlightBuilder.transform.position);
#else
                    DoBuild(m_Camera.transform.position, m_Camera.transform.forward, Misc.vector3zero);
#endif
                }
            }

            // Check water
            CheckWaterStatus();

        }

        public void SetOrbitMode(bool enableOrbitMode)
        {
            if (orbitMode != enableOrbitMode)
            {
                orbitMode = enableOrbitMode;
                switching = true;
                switchingStartTime = Time.time;
                freeMode = orbitMode;
            }
        }


        void UpdateCrouch()
        {
            if (isCrouched && crouch.localPosition.y == 0)
            {
                crouch.transform.localPosition = Misc.vector3down;
                m_CharacterController.stepOffset = 0.4f;
            }
            else if (!isCrouched && crouch.localPosition.y != 0)
            {
                crouch.transform.localPosition = Misc.vector3zero;
                m_CharacterController.stepOffset = 1f;
            }
        }

        void CheckWaterStatus()
        {

            Vector3 nearClipPos = m_Camera.transform.position + m_Camera.transform.forward * (m_Camera.nearClipPlane + 0.001f);
            if (nearClipPos.x == lastNearClipPosX && nearClipPos.y == lastNearClipPosY && nearClipPos.z == lastNearClipPosZ)
                return;

            lastNearClipPosX = (int)nearClipPos.x;
            lastNearClipPosY = (int)nearClipPos.y;
            lastNearClipPosZ = (int)nearClipPos.z;

            bool wasInWater = isInWater;

            isInWater = false;
            isSwimming = false;
            isUnderwater = false;

            // Check water on character controller position
            VoxelChunk chunk;
            int voxelIndex;
            Voxel voxelCh;
            if (env.GetVoxelIndex(curPos, out chunk, out voxelIndex, false))
            {
                voxelCh = chunk.voxels[voxelIndex];
            }
            else
            {
                voxelCh = Voxel.Empty;
            }
            VoxelDefinition voxelChType = env.voxelDefinitions[voxelCh.typeIndex];
            if (voxelCh.hasContent == 1)
            {
                CheckEnterTrigger(chunk, voxelIndex);
                CheckDamage(voxelChType);
            }

            // Safety check; if voxel at character position is solid, move character on top of terrain
            if (voxelCh.isSolid)
            {
                Unstuck(false);
            }
            else
            {
                AnnotateNonCollidingPosition(curPos);
                // Check if water surrounds camera
                Voxel voxelCamera = env.GetVoxel(nearClipPos, false);
                VoxelDefinition voxelCameraType = env.voxelDefinitions[voxelCamera.typeIndex];
                if (voxelCamera.hasContent == 1)
                {
                    CheckEnterTrigger(chunk, voxelIndex);
                    CheckDamage(voxelCameraType);
                }

                if (voxelCamera.GetWaterLevel() > 7)
                {
                    // More water on top?
                    Vector3 pos1Up = nearClipPos;
                    pos1Up.y += 1f;
                    Voxel voxel1Up = env.GetVoxel(pos1Up);
                    if (voxel1Up.GetWaterLevel() > 0)
                    {
                        isUnderwater = true;
                        waterLevelTop = nearClipPos.y + 1f;
                    }
                    else
                    {
                        waterLevelTop = FastMath.FloorToInt(nearClipPos.y) + 0.9f;
                        isUnderwater = nearClipPos.y < waterLevelTop;
                        isSwimming = !isUnderwater;
                    }
                    underWaterMat.color = voxelCameraType.diveColor;
                }
                else if (voxelCh.GetWaterLevel() > 7)
                { // type == env.world.waterVoxel) {
                    isSwimming = true;
                    waterLevelTop = FastMath.FloorToInt(curPos.y) + 0.9f;
                    underWaterMat.color = voxelChType.diveColor;

                }
                underWaterMat.SetFloat("_WaterLevel", waterLevelTop);
            }

            isInWater = isSwimming || isUnderwater;
            if (crouch != null)
            {
                // move camera a bit down to simulate swimming position
                if (!wasInWater && isInWater)
                {
                    PlayWaterSplashSound();
                    crouch.localPosition = Misc.vector3down * 0.6f; // crouch
                }
                else if (wasInWater && !isInWater)
                {
                    crouch.localPosition = Misc.vector3zero;
                }
            }

            // Show/hide underwater panel
            if (isInWater && !underwaterPanel.activeSelf)
            {
                underwaterPanel.SetActive(true);
            }
            else if (!isInWater && underwaterPanel.activeSelf)
            {
                underwaterPanel.SetActive(false);
            }

            // Check if underground and adjust camera near clip plane
            float alt = env.GetTerrainHeight(nearClipPos);
            if (nearClipPos.y < alt)
            {
                if (env.cameraMain.nearClipPlane > 0.081f)
                {
                    lastUserCameraNearClipPlane = env.cameraMain.nearClipPlane;
                    env.cameraMain.nearClipPlane = 0.08f;
                }
            }
            else if (env.cameraMain.nearClipPlane < lastUserCameraNearClipPlane)
            {
                env.cameraMain.nearClipPlane = lastUserCameraNearClipPlane;
            }

        }

        protected virtual void DoHit(int damage)
        {
            lastHitButtonPressed = Time.time;
            Ray ray;
            if (freeMode)
            {
                ray = m_Camera.ScreenPointToRay(input.screenPos);
            }
            else
            {
                ray = m_Camera.ViewportPointToRay(Misc.vector2half);
            }

            WeaponType wt1 = crosshairHitInfo.voxel.type.weaponType; // Weapon that can destroy this voxel
            WeaponType wt2 = player.GetSelectedItem().item.weaponType; // Weapon that we are using
            

            if(!wt2.Equals(WeaponType.Any)) if(!wt2.Equals(wt1) )
                return; // Don't do any damage

            // Check item sound
            InventoryItem inventoryItem = player.GetSelectedItem();
            if (inventoryItem != InventoryItem.Null)
            {
                ItemDefinition currentItem = inventoryItem.item;
                player.ChangeDurability(-1);
                PlayCustomSound(currentItem.useSound);
            }

            env.RayHit(ray, damage, env.buildMode ? crosshairMaxDistance : player.GetHitRange(), player.GetHitDamageRadius());
        }


        private void FixedUpdate()
        {
            FixedUpdateImpl();
        }

        protected virtual void FixedUpdateImpl()
        {

            if (!hasCharacterController)
                return;

            float speed;
            GetInput(out speed);

            Vector3 pos = transform.position;
            if (isFlying || isInWater)
            {
                m_MoveDir = m_Camera.transform.forward * m_Input.y + m_Camera.transform.right * m_Input.x + m_Camera.transform.up * m_Input.z;
                m_MoveDir *= speed;
                if (!isFlying)
                {
                    if (m_MoveDir.y < 0)
                    {
                        m_MoveDir.y += 0.1f * Time.fixedDeltaTime;
                    }
                    if (m_Jump)
                    {
                        // Check if player is next to terrain
                        if (env.CheckCollision(new Vector3(pos.x + m_Camera.transform.forward.x, pos.y, pos.z + m_Camera.transform.forward.z)))
                        {
                            m_MoveDir.y = jumpSpeed * 0.5f;
                            m_Jumping = true;
                        }
                        m_Jump = false;
                    }
                    else
                    {
                        m_MoveDir += Physics.gravity * gravityMultiplier * Time.fixedDeltaTime * 0.5f;
                    }
                    if (pos.y > waterLevelTop && m_MoveDir.y > 0)
                    {
                        m_MoveDir.y = 0; // do not exit water
                    }
                    ProgressSwimCycle(m_CharacterController.velocity, swimSpeed);
                }
            }
            else
            {
                // always move along the camera forward as it is the direction that it being aimed at
                Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

                // get a normal for the surface that is being touched to move along it
                RaycastHit hitInfo;
                Physics.SphereCast(pos, m_CharacterController.radius, Misc.vector3down, out hitInfo,
                    GetCharacterHeight() / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
                desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

                m_MoveDir.x = desiredMove.x * speed;
                m_MoveDir.z = desiredMove.z * speed;
                if (m_CharacterController.isGrounded)
                {
                    m_MoveDir.y = -stickToGroundForce;

                    if (m_Jump)
                    {
                        m_MoveDir.y = jumpSpeed;
                        PlayJumpSound();
                        m_Jump = false;
                        m_Jumping = true;
                    }
                }
                else
                {
                    m_MoveDir += Physics.gravity * gravityMultiplier * Time.fixedDeltaTime;
                }

                UpdateCameraPosition(speed);
                ProgressStepCycle(m_CharacterController.velocity, speed);
            }


            Vector3 finalMove = m_MoveDir * Time.fixedDeltaTime;
            Vector3 newPos = pos + finalMove;
            bool canMove = true;
            if (m_PreviouslyGrounded && !isFlying && isCrouched)
            {
                // check if player is beyond the edge
                Ray ray = new Ray(newPos, Misc.vector3down);
                canMove = Physics.SphereCast(ray, 0.3f, 1f);
                // if player can't move, clamp movement along the edge and check again
                if (!canMove)
                {
                    if (Mathf.Abs(m_MoveDir.z) > Mathf.Abs(m_MoveDir.x))
                    {
                        m_MoveDir.x = 0;
                    }
                    else
                    {
                        m_MoveDir.z = 0;
                    }
                    finalMove = m_MoveDir * Time.fixedDeltaTime;
                    newPos = pos + finalMove;
                    ray.origin = newPos;
                    canMove = Physics.SphereCast(ray, 0.3f, 1f);
                }
            }

            // if constructor is enabled, disable any movement if control key is pressed (reserved for special constructor actions)
            if (env.constructorMode && input.GetButton(InputButtonNames.LeftControl))
            {
                canMove = false;
            }
            else if (!m_CharacterController.enabled)
            {
                canMove = false;
            }
            if (canMove && isActiveAndEnabled)
            {
                // autoclimb
                Vector3 dir = new Vector3(m_MoveDir.x, 0, m_MoveDir.z);
                Vector3 basePos = new Vector3(pos.x, pos.y - GetCharacterHeight() * 0.25f, pos.z);
                Ray ray = new Ray(basePos, dir);
                if (Physics.SphereCast(ray, 0.3f, 1f))
                {
                    m_CharacterController.stepOffset = 1.1f;
                }
                else
                {
                    m_CharacterController.stepOffset = 8f;
                }
                m_CollisionFlags = m_CharacterController.Move(finalMove);
                // check limits
                if (limitBoundsEnabled)
                {
                    pos = m_CharacterController.transform.position;
                    bool clamp = false;
                    if (pos.x > limitBounds.max.x) { pos.x = limitBounds.max.x; clamp = true; } else if (pos.x < limitBounds.min.x) { pos.x = limitBounds.min.x; clamp = true; }
                    if (pos.y > limitBounds.max.y) { pos.y = limitBounds.max.y; clamp = true; } else if (pos.y < limitBounds.min.y) { pos.y = limitBounds.min.y; clamp = true; }
                    if (pos.z > limitBounds.max.z) { pos.z = limitBounds.max.z; clamp = true; } else if (pos.z < limitBounds.min.z) { pos.z = limitBounds.min.z; clamp = true; }
                    if (clamp)
                    {
                        MoveTo(pos);
                    }

                }

            }
            isGrounded = m_CharacterController.isGrounded;

            // Check limits
            if (orbitMode)
            {
                if (FastVector.ClampDistance(ref lookAt, ref pos, minDistance, maxDistance))
                {
                    m_CharacterController.transform.position = pos;
                }
            }

            mouseLook.UpdateCursorLock();

            if (!isGrounded && !isFlying)
            {
                // Check current chunk
                VoxelChunk chunk = env.GetCurrentChunk();
                if (chunk != null && !chunk.isRendered)
                {
                    WaitForCurrentChunk();
                    return;
                }
            }
        }



        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!useHeadBob)
            {
                return;
            }
            float velocity = m_CharacterController.velocity.magnitude;
            if (velocity > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition = m_HeadBob.DoHeadBob(velocity + (speed * (isMoving ? 1f : runstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        void SmoothClimb()
        {
            if (!movingSmooth)
            {
                if (crouch.position.y - prevCrouchYPos >= climbYThreshold && !isFlying)
                {
                    prevCrouchTime = Time.time;
                    movingSmooth = true;
                }
                else
                {
                    prevCrouchYPos = crouch.position.y;
                }
            }

            if (movingSmooth)
            {
                float t = (Time.time - prevCrouchTime) * climbSpeed;
                if (t > 1f)
                {
                    t = 1f;
                    movingSmooth = false;
                    prevCrouchYPos = crouch.position.y;
                }
                UpdateCrouch();
                Vector3 pos = crouch.position;
                pos.y = prevCrouchYPos * (1f - t) + crouch.position.y * t;
                crouch.position = pos;

            }
        }

        protected virtual void GetInput(out float speed)
        {
            float up = 0;
            bool waswalking = isMoving;
            if (input == null || !input.enabled)
            {
                speed = 0;
                return;
            }

            if (input.GetButton(InputButtonNames.Up))
            {
                up = 1f;
            }
            else if (input.GetButton(InputButtonNames.Down))
            {
                up = -1f;
            }

            bool leftShiftPressed = input.GetButton(InputButtonNames.LeftShift);
            isMoving = isGrounded && !isInWater && !isFlying && !leftShiftPressed;
            isRunning = false;

            // set the desired speed to be walking or running
            if (isFlying)
            {
                speed = leftShiftPressed ? flySpeed * 2 : flySpeed;
            }
            else if (isInWater)
            {
                speed = swimSpeed;
            }
            else if (isCrouched)
            {
                speed = walkSpeed * 0.25f;
            }
            else if (isMoving)
            {
                speed = walkSpeed;
            }
            else
            {
                speed = runSpeed;
                isRunning = true;
            }
            m_Input = new Vector3(input.horizontalAxis, input.verticalAxis, up);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            isPressingMoveKeys = m_Input.x != 0 || m_Input.y != 0;

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (isMoving != waswalking && useFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!isMoving ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }

        }


        private void RotateView()
        {
            if (switching)
            {
                switchingLapsed = (Time.time - switchingStartTime) / switchDuration;
                if (switchingLapsed > 1f)
                {
                    switchingLapsed = 1f;
                    switching = false;
                }
            }
            else
            {
                switchingLapsed = 1;
            }

#if UNITY_EDITOR
            if (Input.GetMouseButtonUp(0))
            {
                mouseLook.SetCursorLock(true);
                input.focused = true;
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {

                input.focused = false;
            }
#endif
            if (input.focused)
            {
                mouseLook.LookRotation(transform, m_Camera.transform, orbitMode, lookAt, switchingLapsed);
            }
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {

            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }
            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
        }

        /// <summary>
        /// Moves character controller to a new position. Use this method instead of changing the transform position
        /// </summary>
        public override void MoveTo(Vector3 newPosition)
        {
            m_CharacterController.enabled = false;
            transform.position = newPosition;
            m_CharacterController.enabled = true;
        }

    }
}
