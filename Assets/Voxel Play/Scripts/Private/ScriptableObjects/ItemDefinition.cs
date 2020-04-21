using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	public enum ItemCategory {
		Voxel = 0,
		Torch = 1,
		Model = 2,
		General = 10
	}

    public enum PickingMode
    {
        PickOnApproach = 0,
        PickOnClick = 1
    }

	[Serializable]
	public struct ItemProperty {
		public string name;
		public string value;
	}

	[CreateAssetMenu (menuName = "Voxel Play/Item Definition", fileName = "ItemDefinition", order = 104)]
	[HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000051366-item-definitions")]
	public partial class ItemDefinition : ScriptableObject {
		public string title;
		public ItemCategory category;

		[Tooltip("Voxel Definition associated to this item.")]
		public VoxelDefinition voxelType;

		[Tooltip("Model Definition associated to this item.")]
		public ModelDefinition model;

		[Tooltip ("Icon used in the inventory panel.")]
		public Texture2D icon;

		[Tooltip("Optioanl tint color for the icon.")]
		public Color32 color = Misc.color32White;

		[Tooltip ("Prefab used when this item can be placed on the scene as a normal gameobject (ie. a torch)")]
		public GameObject prefab;

		[Tooltip ("Sound played when player attacks using this item")]
		public AudioClip useSound;

        [Tooltip ("If this item can be picked from the scene")]
        public bool canBePicked = true;

        [Tooltip("How this item can be picked")]
        public PickingMode pickMode;

        [Tooltip ("Sound played when item is picked from the scene")]
		public AudioClip pickupSound;

		[Tooltip ("Custom item properties.")]
		public ItemProperty[] properties;

        [Range(0,15), Tooltip("Intensity of emitted light")]
        public byte lightIntensity = 15;


		public T GetPropertyValue<T> (string name, T defaultValue) {
			if (properties == null)
				return defaultValue;
			name = name.ToUpper ();
			for (int k = 0; k < properties.Length; k++) {
				if (properties [k].name.ToUpper().Equals (name)) {
					switch (Type.GetTypeCode (typeof(T))) {
					case TypeCode.Int32:
						return (T)(object)Convert.ToInt32 (properties [k].value, System.Globalization.CultureInfo.InvariantCulture);
					case TypeCode.String:
						return (T)(object)properties [k].value;
					case TypeCode.Single:
						return (T)(object)Convert.ToSingle (properties [k].value, System.Globalization.CultureInfo.InvariantCulture);
					default:
						Debug.LogError ("Only int, float or string types are supported.");
						break;
					}
					break;
				}
			}
			return defaultValue;
		}


		public void SetPropertyValue (string name, string value) {
			if (properties == null) {
				properties = new ItemProperty[0];
			}
			string nameCheck = name.ToUpper ();
			for (int k = 0; k < properties.Length; k++) {
				if (properties [k].name.ToUpper().Equals (nameCheck)) {
					properties [k].value = value;
					return;
				}
			}
			int length = properties.Length;
			Array.Resize (ref properties, length + 1);
			properties [length].name = name;
			properties [length].value = value;

		}
	}

}