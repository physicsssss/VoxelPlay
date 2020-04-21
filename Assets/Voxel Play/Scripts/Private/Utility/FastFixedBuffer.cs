using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VoxelPlay {

	// Fast preallocated buffer
	public class FastFixedBuffer<T> {
		public T[] values;
		public int count;

		public FastFixedBuffer (int capacity) {
			values = new T[capacity];
			count = 0;
		}

		public void Clear () {
			count = 0;
		}

		[MethodImpl(256)] // equals to MethodImplOptions.AggressiveInlining
		public void Add (T value) {
			values [count++] = value;
		}

		public bool Contains (T value) {
			for (int k = 0; k < count; k++) {
				if (values [k] != null && values [k].Equals (value))
					return true;
			}
			return false;
		}

		public int IndexOf (T value) {
			for (int k = 0; k < count; k++) {
				if (values [k] != null && values [k].Equals (value))
					return k;
			}
			return -1;
		}

		public bool RemoveAt (int index) {
			if (index < 0 || index >= count)
				return false;
			for (int k = index; k < count - 1; k++) {
				values [k] = values [k + 1];
			}
			count--;
			return true;
		}

		public bool Remove (T value) {
			int k = IndexOf (value);
			if (k < 0) {
				return false;
			}
			return RemoveAt (k);
		}


		/// <summary>
		/// Removes the last added element
		/// </summary>
		public bool RemoveLast () {
			if (count <= 0)
				return false;
			--count;
			return true;
		}

		/// <summary>
		/// Returns a copy of the values
		/// </summary>
		public T[] ToArray () {
			T[] a = new T[count];
			Array.Copy (values, a, count);
			return a;
		}
	}

}
					