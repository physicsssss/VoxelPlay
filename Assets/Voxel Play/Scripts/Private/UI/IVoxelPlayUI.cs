using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoxelPlay {

    public delegate void OnConsoleEvent (string text);

	public interface IVoxelPlayUI
	{

		/// <summary>
		/// Triggered when a new message is printed to the console
		/// </summary>
		event OnConsoleEvent OnConsoleNewMessage;

        /// <summary>
        /// Triggered whhen a new command is entered by the user
        /// </summary>
        event OnConsoleEvent OnConsoleNewCommand;

		/// <summary>
		/// Required method to initialize the UI. It's called like OnEnable event.
		/// </summary>
		void InitUI ();

        /// <summary>
        /// Returns true if the console is visible
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Shows/hides the console
        /// </summary>
        void ToggleConsoleVisibility (bool state);

        /// <summary>
        /// Adds a custom text to the console
        /// </summary>
        void AddConsoleText (string text);

        /// <summary>
        /// Adds a custom message to the status bar and to the console.
        /// </summary>
        void AddMessage (string text, float displayTime = 4f, bool flash = true, bool openConsole = false);

        /// <summary>
        /// Hides the status bar
        /// </summary>
        void HideStatusText ();

        /// <summary>
        /// Show/hide inventory
        /// </summary>
        void ToggleInventoryVisibility (bool state);

        /// <summary>
        /// Advances to next inventory page
        /// </summary>
        void InventoryNextPage ();

        /// <summary>
        /// Shows previous inventory page
        /// </summary>
        void InventoryPreviousPage ();

        /// <summary>
        /// Refreshs the inventory contents.
        /// </summary>
        void RefreshInventoryContents ();

        /// <summary>
        /// Updates selected item representation on screen
        /// </summary>
        void ShowSelectedItem (InventoryItem inventoryItem);

        /// <summary>
        /// Hides selected item graphic
        /// </summary>
        void HideSelectedItem ();

        /// <summary>
        /// Shows/hides a panel during loading/starting up the game/engine. Can be called several times to show loading progress
        /// </summary>
        void ToggleInitializationPanel (bool visible, string text = "", float progress = 0);

        /// <summary>
        /// Shows/hides a debug window
        /// </summary>
        void ToggleDebugWindow (bool visible);

	}


}
