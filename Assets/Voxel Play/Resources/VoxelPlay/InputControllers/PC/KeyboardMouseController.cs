using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoxelPlay
{

	public class KeyboardMouseController : VoxelPlayInputController
	{

		protected override void UpdateInputState ()
		{

			screenPos = Input.mousePosition;

			mouseX =  Input.GetAxis ("Mouse X");
			mouseY =  Input.GetAxis ("Mouse Y");
			mouseScrollWheel = Input.GetAxis ("Mouse ScrollWheel");
			horizontalAxis = Input.GetAxis ("Horizontal");
			verticalAxis = Input.GetAxis ("Vertical");

			// Left mouse button
			if (Input.GetMouseButtonDown (0)) {
				buttons [(int)InputButtonNames.Button1].pressStartTime = Time.time;
				buttons [(int)InputButtonNames.Button1].pressState = InputButtonPressState.Down;
			} else if (Input.GetMouseButtonUp (0)) {
				buttons [(int)InputButtonNames.Button1].pressState = InputButtonPressState.Up;
			} else if (Input.GetMouseButton(0)) {
				buttons [(int)InputButtonNames.Button1].pressState = InputButtonPressState.Pressed;
			}
			// Right mouse button
			if (Input.GetMouseButtonDown (1)) {
				buttons [(int)InputButtonNames.Button2].pressStartTime = Time.time;
				buttons [(int)InputButtonNames.Button2].pressState = InputButtonPressState.Down;
			} else if (Input.GetMouseButtonUp (1)) {
				buttons [(int)InputButtonNames.Button2].pressState = InputButtonPressState.Up;
			} else if (Input.GetMouseButton (1)) {
				buttons [(int)InputButtonNames.Button2].pressState = InputButtonPressState.Pressed;
			}
			// Middle mouse button
			if (Input.GetMouseButtonDown (2)) {
				buttons [(int)InputButtonNames.MiddleButton].pressStartTime = Time.time;
				buttons [(int)InputButtonNames.MiddleButton].pressState = InputButtonPressState.Down;
			} else if (Input.GetMouseButtonUp (2)) {
				buttons [(int)InputButtonNames.MiddleButton].pressState = InputButtonPressState.Up;
			} else if (Input.GetMouseButton (2)) {
				buttons [(int)InputButtonNames.MiddleButton].pressState = InputButtonPressState.Pressed;
			}
			// Jump key
			ReadButtonState(InputButtonNames.Jump, "Jump");
			ReadKeyState (InputButtonNames.Up, KeyCode.E);
			ReadKeyState (InputButtonNames.Down, KeyCode.Q);
			ReadKeyState (InputButtonNames.LeftControl, KeyCode.LeftControl);
			ReadKeyState (InputButtonNames.LeftShift, KeyCode.LeftShift);
			ReadKeyState (InputButtonNames.LeftAlt, KeyCode.LeftAlt);
			ReadKeyState (InputButtonNames.Build, KeyCode.B);
			ReadKeyState (InputButtonNames.Fly, KeyCode.F);
			ReadKeyState (InputButtonNames.Crouch, KeyCode.C);
			ReadKeyState (InputButtonNames.Inventory, KeyCode.Tab);
			ReadKeyState (InputButtonNames.Light, KeyCode.L);
			ReadKeyState (InputButtonNames.ThrowItem, KeyCode.G);
			ReadKeyState (InputButtonNames.Action, KeyCode.T);
			ReadKeyState (InputButtonNames.SeeThroughUp, KeyCode.Q);
			ReadKeyState (InputButtonNames.SeeThroughDown, KeyCode.E);
            ReadKeyState (InputButtonNames.Escape, KeyCode.Escape);
            ReadKeyState (InputButtonNames.Console, KeyCode.F1);
            ReadKeyState (InputButtonNames.DebugWindow, KeyCode.F2);
        }

	
	}



}
