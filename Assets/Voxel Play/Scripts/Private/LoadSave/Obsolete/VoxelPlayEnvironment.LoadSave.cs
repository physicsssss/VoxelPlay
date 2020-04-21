using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		//StringBuilder sb;

//		[Obsolete("Use LoadGameBinary instead.")]
//		public bool LoadGame(bool firstLoad, bool preservePlayerPosition = false) {
//
//			saveFileIsLoaded = false;
//
//			if (firstLoad) {
//				if (string.IsNullOrEmpty(saveFilename))
//					return false;
//			} else {
//				// If LoadGame is called during a game, initializes everything first
//				DestroyAllVoxels();
//				if (!CheckGameFilename())
//					return false;
//			}
//
//			bool result = true;
//			try {
//				string saveGameData = GetSaveGameData();
//				if (string.IsNullOrEmpty(saveGameData)) {
//					return false;
//				}
//
//				// get version
//				isLoadingGame = true;
//				StringReader sr = new StringReader(saveGameData);
//				string version = sr.ReadLine();
//				if (version != null) {
//					if (version.Equals("1.0")) {
//						LoadSaveFileFormat_1_0(sr);
//					} else if (version.Equals("1.1")) {
//						LoadSaveFileFormat_1_1(sr);
//					} else if (version.Equals("1.2")) {
//						LoadSaveFileFormat_1_2(sr);
//					} else {
//						throw new ApplicationException("LoadGame() does not support this file format.");
//					}
//				}
//				sr.Close();
//				isLoadingGame = false;
//				saveFileIsLoaded = true;
//				if (!firstLoad) {
//					VoxelPlayUI.instance.ToggleConsoleVisibility(false);
//					ShowMessage("<color=green>Game loaded successfully!</color>");
//				}
//				if (OnGameLoaded != null) {
//					OnGameLoaded();
//				}
//			} catch (Exception ex) {
//				ShowMessage("<color=red>Load error:</color> <color=orange>" + ex.Message + "</color><color=white>" + ex.StackTrace + "</color>");
//				result = false;
//			}
//
//			isLoadingGame = false;
//			return result;
//		}
//
//
//		[Obsolete("Use GetSaveGameDataBinary() instead.")]
//		string GetSaveGameData() {
//			#if UNITY_EDITOR
//			// In Editor, always load saved game from Resources/Worlds/<name of world>/SavedGames folder
//			string path = AssetDatabase.GetAssetPath(world);
//			path = Path.GetDirectoryName(path) + "/SavedGames/" + saveFilename + SAVEGAMEDATA_EXTENSION;
//			if (File.Exists(path)) {
//				return File.ReadAllText(path, Encoding.UTF8);
//			} else {
//				return null;
//			}
//			#else
//												// In Build, try to load the saved game from application data path. If there's none, try to load a default saved game from Resources.
//			string path = Application.persistentDataPath + "/VoxelPlay/" + saveFilename + SAVEGAMEDATA_EXTENSION;
//												if (File.Exists(path)) {
//												return File.ReadAllText(path, Encoding.UTF8);
//												} else {
//																string resource = "VoxelPlay/Worlds/" + world.name + "/SavedGames/" + saveFilename;
//												TextAsset ta = Resources.Load<TextAsset>(resource);
//												if (ta!=null) {
//												return ta.text;
//												} else {
//												return null;
//												}
//												}
//			#endif
//		}
//
//
//		[Obsolete("Use SaveGameBinary() instead.")]
//		public void SaveGame() {
//			if (!CheckGameFilename())
//				return;
//
//			try {
//				string filename = GetFullFilename();
//				StreamWriter sw = new StreamWriter(filename, false, Encoding.UTF8);
//				SaveGameFormat_1_2(sw);
//				sw.Close();
//				ShowMessage("<color=green>Game saved successfully in </color><color=yellow>" + filename + "</color>");
//			} catch (Exception ex) {
//				ShowMessage("<color=red>Error:</color> <color=orange>" + ex.Message + "</color>");
//			}
//		}
//
//		/// <summary>
//		/// Returns the world encoded in a string
//		/// </summary>
//		/// <returns>The game to text.</returns>
//		[Obsolete("Use SaveGameToArray() instead.")]
//		public string SaveGameToText() {
//
//			StringBuilder sb = new StringBuilder();
//			StringWriter sw = new StringWriter(sb);
//			SaveGameFormat_1_2(sw);
//			sw.Close();
//			return sb.ToString();
//		}
//
//		/// <summary>
//		/// Loads game world from a string
//		/// </summary>
//		/// <returns>True if saveGameData was loaded successfully.</returns>
//		/// <param name="preservePlayerPosition">If set to <c>true</c> preserve player position.</param>
//		[Obsolete("Use LoadGameFromArray() instead.")]
//		public bool LoadGameFromText(string saveGameData, bool preservePlayerPosition) {
//
//			bool result = false;
//			DestroyAllVoxels();
//
//			try {
//				if (string.IsNullOrEmpty(saveGameData)) {
//					return false;
//				}
//
//				// get version
//				isLoadingGame = true;
//				StringReader sr = new StringReader(saveGameData);
//				string version = sr.ReadLine();
//				if (version != null) {
//					if (version.Equals("1.0")) {
//						LoadSaveFileFormat_1_0(sr, preservePlayerPosition);
//					} else if (version.Equals("1.1")) {
//						LoadSaveFileFormat_1_1(sr, preservePlayerPosition);
//					}
//
//				}
//				sr.Close();
//				isLoadingGame = false;
//				saveFileIsLoaded = true;
//				result = true;
//			} catch (Exception ex) {
//				Debug.LogError("Voxel Play: " + ex.Message);
//				result = false;
//			}
//
//			isLoadingGame = false;
//			return result;
//
//		}
//

	}



}
