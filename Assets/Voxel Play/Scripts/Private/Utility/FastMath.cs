using System;
using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;

namespace VoxelPlay {

    public static class FastMath {

		[MethodImpl(256)]
        public static int FloorToInt(float n) {
#if UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL
			int i = (int)n;
			if (i>n) i--;
			return i;
#else						
            return (int)(n + 1000000d) - 1000000;
#endif
        }


		[MethodImpl(256)]
		public static Vector3Int FloorToInt(float x, float y, float z) {
			#if UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL
			int ix = (int)x;
			if (ix>x) ix--;
			int iy = (int)y;
			if (iy>y) iy--;
			int iz = (int)z;
			if (iz>z) iz--;
			return new Vector3Int(ix, iy, iz);
			#else						
			return new Vector3Int( (int)(x + 1000000d) - 1000000, (int)(y + 1000000d) - 1000000, (int)(z + 1000000d) - 1000000 );
			#endif
		}


		[MethodImpl(256)]
		public static void FloorToInt(float x, float y, float z, out int ix, out int iy, out int iz) {
			#if UNITY_IOS || UNITY_ANDROID || UNITY_WEBGL
			ix = (int)x;
			if (ix>x) ix--;
			iy = (int)y;
			if (iy>y) iy--;
			iz = (int)z;
			if (iz>z) iz--;
			#else					
			ix = (int)(x + 1000000d) - 1000000;
			iy = (int)(y + 1000000d) - 1000000;
			iz = (int)(z + 1000000d) - 1000000;
			#endif
		}


	}
}