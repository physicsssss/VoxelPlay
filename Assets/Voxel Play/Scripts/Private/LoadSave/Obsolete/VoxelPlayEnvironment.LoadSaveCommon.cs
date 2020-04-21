using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Globalization;

namespace VoxelPlay {

	public partial class VoxelPlayEnvironment : MonoBehaviour {

		static char[] LOAD_DATA_SEPARATOR = new char[] { ',' };


		Vector3 DecodeVector3(string line) {
			string[] parts = line.Split(LOAD_DATA_SEPARATOR);
			Vector3 v = Misc.vector3zero;
			if (parts.Length >= 1) {
				v.x = float.Parse(parts[0], CultureInfo.InvariantCulture);
			}
			if (parts.Length >= 2) {
				v.y = float.Parse(parts[1], CultureInfo.InvariantCulture);
			}
			if (parts.Length >= 3) {
				v.z = float.Parse(parts[2], CultureInfo.InvariantCulture);
			}
			return v;
		}
	}



}
