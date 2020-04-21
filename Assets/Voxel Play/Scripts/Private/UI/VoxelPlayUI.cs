using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoxelPlay {

	public abstract class VoxelPlayUI : MonoBehaviour, IVoxelPlayUI
	{

        static VoxelPlayUI _instance;

        public static void Init()
        {
            if (_instance != null) return;

            _instance = FindObjectOfType<VoxelPlayUI> ();
            if (_instance != null) return;

            VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
            if (env == null) return;

            if (env.UICanvasPrefab == null) {
                env.UICanvasPrefab = Resources.Load<GameObject> ("VoxelPlay/UI/Voxel Play UI Canvas");
                if (env.UICanvasPrefab == null) return;
            }

            GameObject canvas = Instantiate<GameObject> (env.UICanvasPrefab);
            canvas.name = env.UICanvasPrefab.name;
            if (canvas == null) return;

            _instance = canvas.GetComponent<VoxelPlayUI> ();
        }

        public static VoxelPlayUI instance {
            get {
                if (_instance == null) {
                    Init ();
                }
                return _instance;
           }
        }

		/// <summary>
		/// Triggered when a new message is printed to the console
		/// </summary>
		public event OnConsoleEvent OnConsoleNewMessage;

        /// <summary>
        /// Triggered whhen a new command is entered by the user
        /// </summary>
        public event OnConsoleEvent OnConsoleNewCommand;

        /// <summary>
        /// Called during initialization
        /// </summary>
        public virtual void InitUI () { }

        /// <summary>
        /// Returns true if the console is visible
        /// </summary>
        public virtual bool IsVisible { get { return false; } }

        /// <summary>
        /// Shows/hides the console
        /// </summary>
        /// <param name="state">If set to <c>true</c> state.</param>
        public virtual void ToggleConsoleVisibility (bool state) { }

        /// <summary>
        /// Adds a custom text to the console
        /// </summary>
        public virtual void AddConsoleText (string text) { }

        /// <summary>
        /// Adds a custom message to the status bar and to the console.
        /// </summary>
        public virtual void AddMessage (string text, float displayTime = 4f, bool flash = true, bool openConsole = false) { }

        /// <summary>
        /// Hides the status bar
        /// </summary>
        public virtual void HideStatusText () { }

        /// <summary>
        /// Show/hide inventory
        /// </summary>
        public virtual void ToggleInventoryVisibility (bool state) { }

        /// <summary>
        /// Advances to next inventory page
        /// </summary>
        public virtual void InventoryNextPage () { }

        /// <summary>
        /// Shows previous inventory page
        /// </summary>
        public virtual void InventoryPreviousPage () { }

        /// <summary>
        /// Refreshs the inventory contents.
        /// </summary>
        public virtual void RefreshInventoryContents () { }

        /// <summary>
        /// Updates selected item representation on screen
        /// </summary>
        public virtual void ShowSelectedItem (InventoryItem inventoryItem) { }

        /// <summary>
        /// Hides selected item graphic
        /// </summary>
        public virtual void HideSelectedItem () { }

        /// <summary>
        /// Shows/hides a panel during loading/starting up the game/engine. Can be called several times to show loading progress
        /// </summary>
        public virtual void ToggleInitializationPanel (bool visible, string text = "", float progress = 0) { }

        /// <summary>
        /// Shows/hides a debug window
        /// </summary>
        public virtual void ToggleDebugWindow (bool visible) { }


        public void Awake ()
        {
            InitUI ();
        }

        //The event-invoking method that derived classes can override.
        protected virtual void ConsoleNewMessage (string s)
        {
            if (OnConsoleNewMessage != null) {
                OnConsoleNewMessage.Invoke (s);
            }
        }

        //The event-invoking method that derived classes can override.
        protected virtual void ConsoleNewCommand (string s)
        {
            if (OnConsoleNewCommand != null) {
                OnConsoleNewCommand.Invoke (s);
            }
        }

    }


}
