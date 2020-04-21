using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VoxelPlay {

	// Fast index-based list with no repetitions and O(1) access on add, fetch and remove
	public class FastIndexedList<K, T> {
		public T[] values;
		int[] freeIndices;
		int topfreeIndex;
		int _lastAddedIndex;

		public T lastAdded { get { return values [_lastAddedIndex]; } }

		public int lastIndex;

		Dictionary<K, int> dict;

		public FastIndexedList (int initialCapacity = 4) {
			values = new T[initialCapacity];
			dict = new Dictionary<K, int> (initialCapacity);
			freeIndices = new int[initialCapacity];
			_lastAddedIndex = -1;
			lastIndex = -1;
			FillFreeIndices (0);
		}

		public void Clear () {
			_lastAddedIndex = -1;
			lastIndex = -1;
			FillFreeIndices (0);
			dict.Clear ();
		}

		public void Add (K key, T value) {
			if (dict.ContainsKey (key)) {
				return;
			}
			
			if (topfreeIndex < 0) {
				int count = values.Length;
				int newCapacity = count * 2;
				Array.Resize<T> (ref values, newCapacity);
				Array.Resize<int> (ref freeIndices, newCapacity);
				FillFreeIndices (count);
			}
			int nextIndex = freeIndices [topfreeIndex--];
			values [nextIndex] = value;
			dict [key] = nextIndex;
			_lastAddedIndex = nextIndex;
			if (nextIndex > lastIndex) {
				lastIndex = nextIndex;
			}
		}

		public bool Remove (K key) {
			int index;
			if (!dict.TryGetValue (key, out index)) {
				return false;
			}
			freeIndices [++topfreeIndex] = index;
			dict.Remove (key);
			return true;
		}

		public bool Contains (K key) {
			return dict.ContainsKey (key);
		}

		public int IndexOf (K key) {
			int index;
			if (dict.TryGetValue (key, out index)) {
				return index;
            }
				return -1;
		}

		public bool TryGetValue (K key, out T value) {
			int index;
			if (dict.TryGetValue (key, out index)) {
				value = values [index];
				return true;
			}
			value = default(T);
			return false;
		}


		void FillFreeIndices (int startIndex) {
			int count = freeIndices.Length;
			for (int k = startIndex; k < count; k++) {
				freeIndices [k - startIndex] = k;
			}
			topfreeIndex = count - 1 - startIndex;
		}

		// All of this is slower than accessing above methods directly. Commented out to avoid lazzy/slow top-level calls.
//		public T this[K key] {
//			get {
//				T v;
//				TryGetValue (key, out v);
//				return v;
//			}
//			set {
//				Add (key, value);
//			}
//		}


	

	}

}