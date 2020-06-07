using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VoxelPlay
{


    public partial class VoxelPlayFirstPersonController : VoxelPlayCharacterControllerBase
    {
        Transform crosshair;
        const string CROSSHAIR_NAME = "Crosshair";
        Material crosshairMat;
        bool forceUpdateCrosshair;

        void InitCrosshair()
        {
            if (env.crosshairPrefab == null)
            {
                Debug.LogError("Crosshair prefab not assigned to this world.");
                return;
            }
            GameObject obj = Instantiate<GameObject>(env.crosshairPrefab);
            obj.name = CROSSHAIR_NAME;
            crosshair = obj.transform;
            crosshair.SetParent(m_Camera.transform, false);
            if (autoInvertColors)
            {
                crosshairMat = Resources.Load<Material>("VoxelPlay/Materials/VP Crosshair");
            }
            else
            {
                crosshairMat = Resources.Load<Material>("VoxelPlay/Materials/VP Crosshair No GrabPass");
            }
            crosshairMat = Instantiate<Material>(crosshairMat);
            crosshairMat.hideFlags = HideFlags.DontSave;
            obj.GetComponent<Renderer>().sharedMaterial = crosshairMat;
            if (env.crosshairTexture != null)
            {
                crosshairMat.mainTexture = env.crosshairTexture;
            }
            ResetCrosshairPosition();
            if (!enableCrosshair || UnityEngine.XR.XRSettings.enabled)
            {
                crosshair.gameObject.SetActive(false);
            }
            // ensure crosshair gets updated when a chunk changes on screen (including custom voxels which are created when rendering the chunk)
            env.OnChunkRender += (VoxelChunk chunk) =>
            {
                forceUpdateCrosshair = true;
            };
        }

        public void ResetCrosshairPosition()
        {
            UpdateCrosshairScreenPosition();
            crosshair.localRotation = Misc.quaternionZero;
            crosshair.localScale = Misc.vector3one * crosshairScale;
            crosshairMat.color = crosshairNormalColor;
        }

        void UpdateCrosshairScreenPosition()
        {
            if (freeMode)
            {
                Vector3 scrPos = input.screenPos;
                scrPos.z = 1f;
                Vector3 newPosition = m_Camera.ScreenToWorldPoint(scrPos);
                if (switchingLapsed < 1f)
                {
                    crosshair.position = Vector3.Lerp(crosshair.position, newPosition, switchingLapsed);
                }
                else
                {
                    crosshair.position = newPosition;
                }
            }
            else
            {
                if (switchingLapsed < 1f)
                {
                    crosshair.localPosition = Vector3.Lerp(crosshair.localPosition, Misc.vector3forward, switchingLapsed);
                }
                else
                {
                    crosshair.localPosition = Misc.vector3forward;
                }
            }
        }


        void LateUpdate()
        {

            if (env == null || !env.applicationIsPlaying)
                return;

            if (freeMode || switching)
            {
                UpdateCrosshairScreenPosition();
                forceUpdateCrosshair = true;
            }

            if (env.cameraHasMoved || forceUpdateCrosshair)
            {
                forceUpdateCrosshair = false;

                Ray ray;
                if (freeMode || switching)
                {
                    ray = m_Camera.ScreenPointToRay(input.screenPos);
                }
                else
                {
                    ray = m_Camera.ViewportPointToRay(Misc.vector2half);
                }

                // Check if there's a voxel in range
                crosshairOnBlock = env.RayCast(ray, out crosshairHitInfo, env.buildMode ? crosshairMaxDistance : VoxelPlayPlayer.instance.GetHitRange()) && crosshairHitInfo.voxelIndex >= 0;
                if (changeOnBlock)
                {
                    if (crosshairOnBlock)
                    {
                        // Puts crosshair over the voxel but do it only if crosshair won't disappear because of the angle or it's switching from orbit to free mode (or viceversa)
                        float d = -1;
                        if (crosshairHitInfo.sqrDistance > 6f)
                        {
                            d = Vector3.Dot(ray.direction, crosshairHitInfo.normal);
                        }
                        if (d < -0.2f && switchingLapsed >= 1f)
                        {
                            crosshair.position = crosshairHitInfo.point;
                            crosshair.LookAt(crosshairHitInfo.point + crosshairHitInfo.normal);
                        }
                        else
                        {
                            crosshair.localRotation = Misc.quaternionZero;
                        }
                        WeaponType wt1 = crosshairHitInfo.voxel.type.weaponType; // Weapon that can destroy this voxel
                        WeaponType wt2 = player.GetSelectedItem().item.weaponType; // Weapon that we are using
                        crosshairMat.color = wt2.Equals(wt1) || wt2.Equals(WeaponType.Any) ? crosshairOnTargetColor : Color.red;
                    }
                    else
                    {
                        ResetCrosshairPosition();
                    }
                }
            }
            if (crosshairOnBlock)
            {
                crosshair.localScale = Misc.vector3one * (crosshairScale * (1f - targetAnimationScale * 0.5f + Mathf.PingPong(Time.time * targetAnimationSpeed, targetAnimationScale)));
                if (voxelHighlight)
                {

                    // * Faiq: Color selected vocel yellow if you can hit otherwise red.
                    WeaponType wt1 = crosshairHitInfo.voxel.type.weaponType; // Weapon that can destroy this voxel
                    WeaponType wt2 = player.GetSelectedItem().item.weaponType; // Weapon that we are using

                    Color highlightColor = wt2.Equals(wt1) || wt2.Equals(WeaponType.Any) ? voxelHighlightColor : voxelHighlightColorR;
                    Debug.Log("(" + crosshairHitInfo.voxel.type.name + ", " + wt1.ToString() + ") | (" + wt2.ToString() + ", " + player.GetSelectedItem().item.name + ")");

                    env.VoxelHighlight(crosshairHitInfo, highlightColor, voxelHighlightEdge);
                }
            }
            else
            {
                env.VoxelHighlight(false);
            }

        }
    }




}

