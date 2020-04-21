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

		const string SAVEGAMEDATA_EXTENSION = ".bytes";

		/// <summary>
		/// True if the current game has been loaded from a savefile.
		/// </summary>
		[NonSerialized]
		public bool saveFileIsLoaded;


		const byte SAVE_FILE_CURRENT_FORMAT = 10;
		bool isLoadingGame;

		public bool LoadGameBinary (bool firstLoad, bool preservePlayerPosition = false) {

			saveFileIsLoaded = false;

			if (firstLoad) {
				if (string.IsNullOrEmpty (saveFilename))
					return false;
			} else {
				// If LoadGame is called during a game, initializes everything first
				DestroyAllVoxels ();
				if (!CheckGameFilename ())
					return false;
			}

			bool result = true;
			try {
                byte [] saveGameData = GetSaveGameDataBinary ();
                if (saveGameData == null) {
                    return false;
                }

                // get version
                isLoadingGame = true;
                using (BinaryReader br = new BinaryReader (new MemoryStream (saveGameData, false), Encoding.UTF8)) {
                    int version = br.ReadByte ();
#pragma warning disable 0429
#pragma warning disable 0162
                    if (VoxelPlayEnvironment.CHUNK_SIZE != 16 && version <= 9) {
                        throw new ApplicationException ("Saved game cannot be loaded. Chunk size does not match!");
                    }
                    if (version >= 10) {
                        int chunkSize = br.ReadByte ();
                        if (VoxelPlayEnvironment.CHUNK_SIZE != chunkSize) {
                            throw new ApplicationException ("Saved game cannot be loaded. Saved chunk size (" + chunkSize + ") does not match current scene chunk size!");
                        }
                    }
#pragma warning restore 0162
#pragma warning restore 0429
                    switch (version) {
                    case 5:
                        LoadGameBinaryFileFormat_5 (br, preservePlayerPosition);
                        break;
                    case 6:
                        LoadGameBinaryFileFormat_6 (br, preservePlayerPosition);
                        break;
                    case 7:
                        LoadGameBinaryFileFormat_7 (br, preservePlayerPosition);
                        break;
                    case 8:
                        LoadGameBinaryFileFormat_8 (br, preservePlayerPosition);
                        break;
                    case 9:
                        LoadGameBinaryFileFormat_9 (br, preservePlayerPosition);
                        break;
                    case 10:
                        LoadGameBinaryFileFormat_10 (br, preservePlayerPosition);
                        break;
                    default:
                        throw new ApplicationException ("LoadGame() does not support this file format.");
                    }
                    br.Close ();
                }
                isLoadingGame = false;
                saveFileIsLoaded = true;
                if (!firstLoad) {
                    VoxelPlayUI.instance.ToggleConsoleVisibility (false);
                    ShowMessage ("<color=green>Game loaded successfully!</color>");
                }
                if (OnGameLoaded != null) {
                    OnGameLoaded ();
                }
            } catch (Exception ex) {
				ShowError ("<color=red>Load error:</color> <color=orange>" + ex.Message + "</color><color=white>" + ex.StackTrace + "</color>");
				result = false;
			}

			isLoadingGame = false;
			shouldCheckChunksInFrustum = true;
			return result;
		}

		string GetFullFilename () {
			#if UNITY_EDITOR
			string path = AssetDatabase.GetAssetPath (world);
			path = Path.GetDirectoryName (path) + "/SavedGames";
			Directory.CreateDirectory (path);
			path += "/" + saveFilename + SAVEGAMEDATA_EXTENSION;
			return path;
			#else
												string path = Application.persistentDataPath + "/VoxelPlay";
												Directory.CreateDirectory (path);
			string fullName = path + "/" + saveFilename + SAVEGAMEDATA_EXTENSION;
												return fullName;
			#endif
		}


		byte[] GetSaveGameDataBinary () {

			#if UNITY_EDITOR
			// In Editor, always load saved game from Resources/Worlds/<name of world>/SavedGames folder
			string path = AssetDatabase.GetAssetPath (world);
			path = Path.GetDirectoryName (path) + "/SavedGames/" + saveFilename + SAVEGAMEDATA_EXTENSION;
			if (File.Exists (path)) {
				return File.ReadAllBytes (path);
			}
			return null;
			
			#else
												// In Build, try to load the saved game from application data path. If there's none, try to load a default saved game from Resources.
			string path = Application.persistentDataPath + "/VoxelPlay/" + saveFilename + SAVEGAMEDATA_EXTENSION;
												if (File.Exists(path)) {
			return File.ReadAllBytes (path);

												} else {
																string resource = "Worlds/" + world.name + "/SavedGames/" + saveFilename;
												TextAsset ta = Resources.Load<TextAsset>(resource);
												if (ta!=null) {
												return ta.bytes;
												} else {
												return null;
												}
												}
			#endif
		}


		bool CheckGameFilename () {
			if (string.IsNullOrEmpty (saveFilename)) {
				ShowMessage ("<color=orange>Set a file name for the game to load/save first.</color>");
				return false;
			}
			return true;
		}

		public bool SaveGameBinary () {
			if (!CheckGameFilename ())
				return false;

			bool success = true;
			try {
				string filename = GetFullFilename ();
				FileStream fs = new FileStream (filename, FileMode.Create);
				BinaryWriter bw = new BinaryWriter (fs, Encoding.UTF8);
				SaveGameBinaryFormat_10 (bw);
				bw.Close ();
				fs.Close ();
				ShowMessage ("<color=green>Game saved successfully in </color><color=yellow>" + filename + "</color>");
			} catch (Exception ex) {
				ShowError ("<color=red>Error:</color> <color=orange>" + ex.Message + "</color>");
				success = false;
			}
			return success;
		}

		/// <summary>
		/// Returns the world encoded in a string
		/// </summary>
		/// <returns>The game to text.</returns>
		public byte[] SaveGameToByteArray () {
			MemoryStream ms = new MemoryStream ();
			BinaryWriter bw = new BinaryWriter (ms, Encoding.UTF8);
			SaveGameBinaryFormat_10 (bw);
			bw.Close ();
			return ms.ToArray ();
		}

		/// <summary>
		/// Returns the world encoded in base 64 format
		/// </summary>
		public string SaveGameToBase64() {
			return System.Convert.ToBase64String (SaveGameToByteArray());
		}

		/// <summary>
		/// Loads game world from a string
		/// </summary>
		/// <returns>True if saveGameData was loaded successfully.</returns>
		/// <param name="preservePlayerPosition">If set to <c>true</c> preserve player position.</param>
		/// <param name="clearScene">If set to <c>true</c> existing chunks will be cleared before loading the game. If set to false, only chunks from the saved game will be replaced.</param>
		public bool LoadGameFromBase64(string saveGameDataBase64string, bool preservePlayerPosition, bool clearScene = true) {
			byte[] saveGameData = System.Convert.FromBase64String (saveGameDataBase64string);
			return LoadGameFromByteArray (saveGameData, preservePlayerPosition, clearScene);
		}

		/// <summary>
		/// Loads game world from a string
		/// </summary>
		/// <returns>True if saveGameData was loaded successfully.</returns>
		/// <param name="preservePlayerPosition">If set to <c>true</c> preserve player position.</param>
		/// <param name="clearScene">If set to <c>true</c> existing chunks will be cleared before loading the game. If set to false, only chunks from the saved game will be replaced.</param>
		public bool LoadGameFromByteArray (byte[] saveGameData, bool preservePlayerPosition, bool clearScene = true) {

			bool result = false;
			if (clearScene) {
				DestroyAllVoxels ();
			} else {
				// Remove all modified chunks to ensure only loaded chunks are the modified ones
				GetChunks(tempChunks, true);
				int count = tempChunks.Count;
				for (int k=0;k<count;k++) {
					VoxelChunk chunk = tempChunks [k];
					if (chunk != null && chunk.modified) {
						// Restore original contents
						world.terrainGenerator.PaintChunk (chunk);
						ChunkRequestRefresh (chunk, true, true);
						chunk.modified = false;
					}
				}
			}

			try {
                if (saveGameData == null) {
                    return false;
                }

                // get version
                isLoadingGame = true;
                using (BinaryReader br = new BinaryReader (new MemoryStream (saveGameData), Encoding.UTF8)) {
                    byte version = br.ReadByte ();
#pragma warning disable 0429
#pragma warning disable 0162
                    if (VoxelPlayEnvironment.CHUNK_SIZE != 16 && version <= 9) {
                        throw new ApplicationException ("Saved game cannot be loaded. Chunk size does not match!");
                    }
                    if (version >= 10) {
                        int chunkSize = br.ReadByte ();
                        if (VoxelPlayEnvironment.CHUNK_SIZE != chunkSize) {
                            throw new ApplicationException ("Saved game cannot be loaded. Saved chunk size (" + chunkSize + ") does not match current scene chunk size!");
                        }
                    }
#pragma warning restore 0162
#pragma warning restore 0429
                    switch (version) {
                    case 5:
                        LoadGameBinaryFileFormat_5 (br, preservePlayerPosition);
                        break;
                    case 6:
                        LoadGameBinaryFileFormat_6 (br, preservePlayerPosition);
                        break;
                    case 7:
                        LoadGameBinaryFileFormat_7 (br, preservePlayerPosition);
                        break;
                    case 8:
                        LoadGameBinaryFileFormat_8 (br, preservePlayerPosition);
                        break;
                    case 9:
                        LoadGameBinaryFileFormat_9 (br, preservePlayerPosition);
                        break;
                    case 10:
                        LoadGameBinaryFileFormat_10 (br, preservePlayerPosition);
                        break;
                    default:
                        throw new ApplicationException ("LoadGameFromArray() does not support this file format.");
                    }
                    br.Close ();
                }
                isLoadingGame = false;
                saveFileIsLoaded = true;
                result = true;
            } catch (Exception ex) {
				Debug.LogError ("Voxel Play: " + ex.Message);
				result = false;
			}

			isLoadingGame = false;
			shouldCheckChunksInFrustum = true;
			return result;

		}


	}



}
