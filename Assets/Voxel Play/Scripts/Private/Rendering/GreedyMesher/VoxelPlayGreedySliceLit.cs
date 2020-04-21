using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

	public class QuadFull
	{
		public float x, y, w, h;
		public bool used;
		public Color32 color;
		public float light;
		public int textureIndex;
	}

	public class VoxelPlayGreedySliceLit
	{

		QuadFull[] qq;
		QuadFull lastQ;
		int qqCount;

		public VoxelPlayGreedySliceLit ()
		{
			qq = new QuadFull[VoxelPlayEnvironment.CHUNK_SIZE * VoxelPlayEnvironment.CHUNK_SIZE];
			for (int k = 0; k < qq.Length; k++) {
				qq [k] = new QuadFull ();
			}
		}

		public void Clear ()
		{
			qqCount = 0;
		}


		public void AddQuad (float x, float y, Color32 color, float light, int textureIndex)
		{
			if (qqCount > 0 && lastQ.y == y && lastQ.x + lastQ.w == x && lastQ.textureIndex == textureIndex && lastQ.light == light && lastQ.color.r == color.r && lastQ.color.g == color.g && lastQ.color.b == color.b) {
				lastQ.w++;
			} else {
				QuadFull q = lastQ = qq [qqCount++];
				q.x = x;
				q.y = y;
				q.w = 1;
				q.h = 1;
				q.color = color;
				q.light = light;
				q.textureIndex = textureIndex;
				q.used = false;
			}
		}

		public void FlushTriangles (FaceDirection direction, int slice, List<Vector3> vertices, List<int>indices, List<Vector4>uv0, List<Vector3>normals, List<Color32>colors)
		{
			if (qqCount == 0) {
				return;
			}
			Vector3 pos;
			Vector4 uv;
            Vector3 normal;
            switch(direction) {
                case FaceDirection.Back: normal = Misc.vector3back; break;
                case FaceDirection.Forward: normal = Misc.vector3forward; break;
                case FaceDirection.Left: normal = Misc.vector3left; break;
                case FaceDirection.Right: normal = Misc.vector3right; break;
                case FaceDirection.Top: normal = Misc.vector3up; break;
                default: normal = Misc.vector3down; break;
            }
			int index = vertices.Count;
			bool enableColors = colors != null;
			for (int k = 0; k < qqCount; k++) {
				QuadFull q1 = qq [k];
				if (q1.used) {
					continue;
				}
				for (int j = k + 1; j < qqCount; j++) {
					QuadFull q2 = qq [j];
					if (q2.used)
						continue;
					if (q1.y == q2.y && q1.h == q2.h && q1.x + q1.w == q2.x && q1.textureIndex == q2.textureIndex && q1.light == q2.light && q1.color.r == q2.color.r && q1.color.g == q2.color.g && q1.color.b == q2.color.b) {
						q1.w += q2.w;
						q2.used = true;
						continue;
					}
                    if (q1.x == q2.x && q1.w == q2.w && q1.y + q1.h == q2.y && q1.textureIndex == q2.textureIndex && q1.light == q2.light && q1.color.r == q2.color.r && q1.color.g == q2.color.g && q1.color.b == q2.color.b) {
						q1.h += q2.h;
						q2.used = true;
						continue;
					}
				}
				uv.x = 0;
				uv.y = 0;
				uv.z = q1.textureIndex;
				uv.w = q1.light;
				uv0.Add (uv);
				switch (direction) {
				case FaceDirection.Top:
					pos.y = slice - (VoxelPlayEnvironment.CHUNK_HALF_SIZE - 1);
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
					// tex coords
					uv.y = q1.w;
					uv0.Add (uv);
					uv.x = q1.h;
					uv.y = 0f;
					uv0.Add (uv);
					uv.y = q1.w;
					uv0.Add (uv);
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
					// tex coords
					uv.y = q1.w;
					uv0.Add (uv);
					uv.x = q1.h;
					uv.y = 0f;
					uv0.Add (uv);
					uv.y = q1.w;
					uv0.Add (uv);
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
					// tex coords
					uv.y = q1.h;
					uv0.Add (uv);
					uv.x = q1.w;
					uv.y = 0f;
					uv0.Add (uv);
					uv.y = q1.h;
					uv0.Add (uv);
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
					// tex coords
					uv.y = q1.h;
					uv0.Add (uv);
					uv.x = q1.w;
					uv.y = 0f;
					uv0.Add (uv);
					uv.y = q1.h;
					uv0.Add (uv);
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
					// tex coords
					uv.y = q1.h;
					uv0.Add (uv);
					uv.x = q1.w;
					uv.y = 0f;
					uv0.Add (uv);
					uv.y = q1.h;
					uv0.Add (uv);
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
					// tex coords
					uv.y = q1.h;
					uv0.Add (uv);
					uv.x = q1.w;
					uv.y = 0f;
					uv0.Add (uv);
					uv.y = q1.h;
					uv0.Add (uv);
					break;
				}

				if (enableColors) {
					colors.Add (q1.color);
					colors.Add (q1.color);
					colors.Add (q1.color);
					colors.Add (q1.color);
				}

				normals.Add (normal);
				normals.Add (normal);
				normals.Add (normal);
				normals.Add (normal);

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