using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoxelPlay
{

    public class DualTouchController : VoxelPlayInputController
    {

        public float dragThreshold = 10f;
        public float rotationSpeed = 0.1f;
        public float alpha = 0.7f;
        public float fadeInSpeed = 2f;

        RectTransform buttonInventoryRT, buttonCrouchRT, buttonJumpRT, buttonBuildRT;
        CanvasGroup canvasGroup;
        float startTime;
        bool leftTouched;
        float pressTime;
        Vector3 leftTouchPos;
        bool dragged;

        protected override bool Initialize ()
        {
            Transform t = transform.Find ("ButtonBuild");
            if (t != null) {
                buttonBuildRT = t.GetComponent<RectTransform> ();
            }

            t = transform.Find ("ButtonJump");
            if (t != null) {
                buttonJumpRT = t.GetComponent<RectTransform> ();
            }

            t = transform.Find ("ButtonCrouch");
            if (t != null) {
                buttonCrouchRT = t.GetComponent<RectTransform> ();
            }

            t = transform.Find ("ButtonInventory");
            if (t != null) {
                buttonInventoryRT = t.GetComponent<RectTransform> ();
            }

            canvasGroup = GetComponent<CanvasGroup> ();
            canvasGroup.alpha = 0;
            startTime = Time.time;
            return true;
        }

        protected override void UpdateInputState ()
        {
            if (canvasGroup.alpha < alpha) {
                float t = (Time.time - startTime) / fadeInSpeed;
                if (t > alpha)
                    t = alpha;
                canvasGroup.alpha = t;
            }

            screenPos = Input.mousePosition;
            focused = true;

            leftTouched = false;
            int touchCount = Input.touchCount;
            for (int k = 0; k < touchCount; k++) {
                ManageTouch (k);
            }
            if (!leftTouched) {
                horizontalAxis = verticalAxis = 0;
            }
        }

        void ManageTouch (int touchIndex)
        {
            Touch t = Input.touches [touchIndex];

            switch (t.phase) {
            case TouchPhase.Began:

                if (RectTransformUtility.RectangleContainsScreenPoint (buttonBuildRT, t.position, null)) {
                    buttons [(int)InputButtonNames.Button2].pressState = InputButtonPressState.Down;
                    return;
                }
                if (RectTransformUtility.RectangleContainsScreenPoint (buttonJumpRT, t.position, null)) {
                    buttons [(int)InputButtonNames.Jump].pressState = InputButtonPressState.Down;
                    return;
                }
                if (RectTransformUtility.RectangleContainsScreenPoint (buttonCrouchRT, t.position, null)) {
                    buttons [(int)InputButtonNames.Crouch].pressState = InputButtonPressState.Down;
                    return;
                }
                if (RectTransformUtility.RectangleContainsScreenPoint (buttonInventoryRT, t.position, null)) {
                    buttons [(int)InputButtonNames.Inventory].pressState = InputButtonPressState.Down;
                    return;
                }

                // Left half of screen touched
                if (t.position.x < Screen.width / 2) {
                    leftTouched = true;
                    leftTouchPos = t.position;
                    return;
                }

                pressTime = Time.time;
                dragged = false;
                break;
            case TouchPhase.Moved:

                // Left half of screen touched
                if (t.position.x < Screen.width / 2) {
                    leftTouched = true;
                    horizontalAxis = t.position.x - leftTouchPos.x;
                    verticalAxis = t.position.y - leftTouchPos.y;
                    return;
                }

                pressTime = Time.time;
                float deltaX = t.deltaPosition.x;
                if (deltaX > 0) {
                    deltaX -= dragThreshold;
                    if (deltaX < 0)
                        deltaX = 0;
                } else if (deltaX < 0) {
                    deltaX += dragThreshold;
                    if (deltaX > 0)
                        deltaX = 0;
                }
                deltaX *= rotationSpeed;
                mouseX = mouseX * 0.9f + deltaX * 0.1f;

                float deltaY = t.deltaPosition.y;
                if (deltaY > 0) {
                    deltaY -= dragThreshold;
                    if (deltaY < 0)
                        deltaY = 0;
                } else if (deltaY < 0) {
                    deltaY += dragThreshold;
                    if (deltaY > 0)
                        deltaY = 0;
                }
                deltaY *= rotationSpeed;
                mouseY = mouseY * 0.9f + deltaY * 0.1f;
                buttons [(int)InputButtonNames.Button1].pressState = InputButtonPressState.Pressed;
                dragged = true;
                break;
            case TouchPhase.Ended:

                mouseX = mouseY = 0;
                if (RectTransformUtility.RectangleContainsScreenPoint (buttonBuildRT, t.position, null)) {
                    buttons [(int)InputButtonNames.Button2].pressState = InputButtonPressState.Up;
                    return;
                }
                if (RectTransformUtility.RectangleContainsScreenPoint (buttonJumpRT, t.position, null)) {
                    buttons [(int)InputButtonNames.Jump].pressState = InputButtonPressState.Up;
                    return;
                }
                if (RectTransformUtility.RectangleContainsScreenPoint (buttonCrouchRT, t.position, null)) {
                    buttons [(int)InputButtonNames.Crouch].pressState = InputButtonPressState.Up;
                    return;
                }
                if (RectTransformUtility.RectangleContainsScreenPoint (buttonInventoryRT, t.position, null)) {
                    buttons [(int)InputButtonNames.Inventory].pressState = InputButtonPressState.Up;
                    return;
                }
                if (!dragged && Time.time - pressTime < 0.3f) {
                    buttons [(int)InputButtonNames.Button1].pressState = InputButtonPressState.Down; // click
                } else {
                    buttons [(int)InputButtonNames.Button1].pressState = InputButtonPressState.Up;
                }
                break;
            case TouchPhase.Stationary:

                // Left half of screen touched
                if (t.position.x < Screen.width / 2) {
                    leftTouched = true;
                    horizontalAxis = t.position.x - leftTouchPos.x;
                    verticalAxis = t.position.y - leftTouchPos.y;
                    return;
                }

                mouseX = mouseY = 0;
                if (!dragged && Time.time - pressTime > 0.5f) {
                    buttons [(int)InputButtonNames.Button1].pressState = InputButtonPressState.Down; // long press
                }
                break;
            }
        }

    }



}
