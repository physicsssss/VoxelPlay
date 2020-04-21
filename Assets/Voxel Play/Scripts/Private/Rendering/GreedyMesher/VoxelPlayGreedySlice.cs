using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay {

	public class Quad {
		public int x, y, w, h;
		public bool used;
	}

	public class VoxelPlayGreedySlice {

		Quad[] qq;
		Quad lastQ;
		int qqCount;

		public VoxelPlayGreedySlice () {
			qq = new Quad[VoxelPlayEnvironment.CHUNK_SIZE * VoxelPlayEnvironment.CHUNK_SIZE];
			for (int k = 0; k < qq.Length; k++) {
				qq [k] = new Quad ();
			}
		}

		public void Clear () {
			qqCount = 0;
		}


		public void AddQuad (int x, int y) {
			if (qqCount > 0 && lastQ.y == y && lastQ.x + lastQ.w == x) {
				lastQ.w++;
			} else {
				Quad q = lastQ = qq [qqCount++];
				q.x = x;
				q.y = y;
				q.w = 1;
				q.h = 1;
				q.used = false;
			}
		}

		public void FlushTriangles (FaceDirection direction, int slice, List<Vector3> vertices, List<int>indices) {
			if (qqCount == 0) {
				return;
			}
			Vector3 pos;
			int index = vertices.Count;
			for (int k = 0; k < qqCount; k++) {
				Quad q1 = qq [k];
				if (q1.used) {
					continue;
				}
				for (int j = k + 1; j < qqCount; j++) {
					Quad q2 = qq [j];
					if (q2.used)
						continue;
                    if (q1.y == q2.y && q1.h == q2.h && q1.x + q1.w == q2.x) {
                        q1.w += q2.w;
                        q2.used = true;
                        continue;
                    }
					if (q1.x == q2.x && q1.w == q2.w && q1.y + q1.h == q2.y) {
						q1.h += q2.h;
						q2.used = true;
						continue;
					}
				}
				switch (direction) {
				case FaceDirection.Top:
					pos.y = slice - (VoxelPlayEnvironment.CHUNK_HALF_SIZE-1);
					pos.x = q1.x - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
					pos.z = q1.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE + q1.h;
					vertices.Add (pos);
					pos.x += q1.w;
					vertices.Add (pos);
					pos.x -= q1.w;
					pos.z -= q1.h;
					vertices.Add (pos);
					pos.x += q1.w;
					vertices.Add (pos);
					break;
				case FaceDirection.Bottom:
					pos.y = slice - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
					pos.x = q1.x - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
					pos.z = q1.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
					vertices.Add (pos);
					pos.x += q1.w;
					vertices.Add (pos);
					pos.x -= q1.w;
					pos.z += q1.h;
					vertices.Add (pos);
					pos.x += q1.w;
					vertices.Add (pos);
					break;
				case FaceDirection.Left:
					pos.x = slice - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
					pos.z = q1.x - VoxelPlayEnvironment.CHUNK_HALF_SIZE + q1.w;
					pos.y = q1.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
					vertices.Add (pos);
					pos.y += q1.h;
					vertices.Add (pos);
					pos.y -= q1.h;
					pos.z -= q1.w;
					vertices.Add (pos);
					pos.y += q1.h;
					vertices.Add (pos);
					break;
				case FaceDirection.Right:
					pos.x = slice - (VoxelPlayEnvironment.CHUNK_HALF_SIZE - 1);
					pos.z = q1.x - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
					pos.y = q1.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
					vertices.Add (pos);
					pos.y += q1.h;
					vertices.Add (pos);
					pos.z += q1.w;
					pos.y -= q1.h;
					vertices.Add (pos);
					pos.y += q1.h;
					vertices.Add (pos);
					break;
				case FaceDirection.Back:
					pos.z = slice - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
					pos.x = q1.x - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
					pos.y = q1.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
					vertices.Add (pos);
					pos.y += q1.h;
					vertices.Add (pos);
					pos.x += q1.w;
					pos.y -= q1.h;
					vertices.Add (pos);
					pos.y += q1.h;
					vertices.Add (pos);
					break;
				case FaceDirection.Forward:
					pos.z = slice - (VoxelPlayEnvironment.CHUNK_HALF_SIZE - 1);
					pos.x = q1.x - VoxelPlayEnvironment.CHUNK_HALF_SIZE + q1.w;
					pos.y = q1.y - VoxelPlayEnvironment.CHUNK_HALF_SIZE;
					vertices.Add (pos);
					pos.y += q1.h;
					vertices.Add (pos);
					pos.x -= q1.w;
					pos.y -= q1.h;
					vertices.Add (pos);
					pos.y += q1.h;
					vertices.Add (pos);
					break;
				}
				indices.Add (index);
				indices.Add (index + 1);
				indices.Add (index + 2);
				indices.Add (index + 3);
				indices.Add (index + 2);
				indices.Add (index + 1);	
				index += 4;
			}

			// Clear for next usage
			qqCount = 0;
		}

	}
}