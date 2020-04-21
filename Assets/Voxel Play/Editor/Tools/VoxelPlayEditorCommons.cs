using UnityEngine;
using UnityEditor;
using System;
using System.Collections;

namespace VoxelPlay {
				
	public static class VoxelPlayEditorCommons {

		public static void CheckImportSettings(Texture texture, bool forcePixelFilter = false) {
			if (texture == null)
				return;

			string fullPath = AssetDatabase.GetAssetPath (texture);
			if (string.IsNullOrEmpty (fullPath))
				return;
			
			TextureImporter importerSettings = AssetImporter.GetAtPath (fullPath) as TextureImporter;
			if (importerSettings != null) {
				if (!importerSettings.isReadable) { 
					importerSettings.isReadable = true;
					if (forcePixelFilter) {
						importerSettings.filterMode = FilterMode.Point;
						importerSettings.mipmapEnabled = false;
					}
					importerSettings.SaveAndReimport ();
				}
			}
		}
				

	}

}
