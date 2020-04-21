using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

    public static class Misc
    {

        public static Vector4 vector4zero = Vector4.zero;
        public static Vector3 vector4max = new Vector4 (float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue);
        public static Vector3 vector3zero = Vector3.zero;
        public static Vector3 vector3one = Vector3.one;
        public static Vector3 vector3up = Vector3.up;
        public static Vector3 vector3down = Vector3.down;
        public static Vector3 vector3far = new Vector3 (0, -10000, 0);
        public static Vector3 vector3left = Vector3.left;
        public static Vector3 vector3right = Vector3.right;
        public static Vector3 vector3back = Vector3.back;
        public static Vector3 vector3forward = Vector3.forward;
        public static Vector3 vector3diagonalXZ1 = new Vector3 (1, 0, 1).normalized;
        public static Vector3 vector3diagonalXZ2 = new Vector3 (1, 0, -1).normalized;
        public static Vector3 vector3min = new Vector3 (float.MinValue, float.MinValue, float.MinValue);
        public static Vector3 vector3max = new Vector3 (float.MaxValue, float.MaxValue, float.MaxValue);
        public static Vector3 vector3half = new Vector3 (0.5f, 0.5f, 0.5f);

        public static Vector3Int vector3ifar = new Vector3Int (0, -10000, 0);
        public static Vector3Int vector3izero = new Vector3Int();

        public static Vector2 vector2zero = Vector2.zero;
        public static Vector2 vector2one = Vector2.one;
        public static Vector2 vector2up = Vector2.up;
        public static Vector2 vector2half = new Vector2 (0.5f, 0.5f);

        public static Bounds bounds1 = new Bounds (Misc.vector3zero, Misc.vector3one);
        public static Bounds boundsInfinite = new Bounds (Misc.vector3zero, new Vector3 (10000, 10000, 10000));

        public static Bounds boundsEmpty = new Bounds ();

        public static Quaternion quaternionZero = Quaternion.Euler (0, 0, 0);
        public static Quaternion quaternionIdentity = Quaternion.identity;

        public static Rect rectFullViewport = new Rect (0, 0, 1, 1);

        public static Color colorWhite = Color.white;
        public static Color colorTransparent = new Color (0, 0, 0, 0);
        public static Color32 color32White = new Color32 (255, 255, 255, 255);
        public static Color32 color32Transparent = new Color32 (0, 0, 0, 0);

        // Remove empty elements
        public static T [] PackArray<T> (T [] original)
        {
            int count = original.Length;
            List<T> n = new List<T> (count);
            for (int k = 0; k < count; k++) {
                if (original [k] != null)
                    n.Add (original [k]);
            }
            return n.ToArray ();
        }

        public static Color32 MultiplyRGB (this Color32 c1, Color32 c2)
        {
            c1.r = (byte)(c1.r * c2.r / 255);
            c1.g = (byte)(c1.g * c2.g / 255);
            c1.b = (byte)(c1.b * c2.b / 255);
            return c1;
        }


        public static List<T> GetList<T> (bool lowMemoryMode, int proposedSize)
        {
            return new List<T> (lowMemoryMode ? 4 : proposedSize);
        }
    }

}