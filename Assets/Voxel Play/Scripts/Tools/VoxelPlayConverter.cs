using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

    /// <summary>
    /// A neutral / engine independent model definition. Basically an array of colors which specify voxels inside a model with specified size and optional center offset
    /// </summary>
    public struct ColorBasedModelDefinition {
		public string name;
		public int sizeX, sizeY, sizeZ;
		public int offsetX, offsetY, offsetZ;

		/// <summary>
		/// Colors are arranged in Y/Z/X structure
		/// </summary>
		public Color32[] colors;

		public static ColorBasedModelDefinition Null = new ColorBasedModelDefinition ();
	}

	public static class VoxelPlayConverter {

		struct Cuboid {
			public Bounds bounds;
			public Color32 color;
			public int textureIndex;
			public bool deleted;
		}

		struct Face {
			public Vector3 center;
			public Vector3 size;
			public Vector3[] vertices;
			public Vector3[] normals;
			public Color32 color;
			public int textureIndex;

			public Face (Vector3 center, Vector3 size, Vector3[] vertices, Vector3[] normals, Color32 color, int textureIndex) {
				this.center = center;
				this.size = size;
				this.vertices = vertices;
				this.normals = normals;
				this.color = color;
				this.textureIndex = textureIndex;
			}


			public static bool operator == (Face f1, Face f2) {
				return f1.size == f2.size && f1.center == f2.center;
			}

			public static bool operator != (Face f1, Face f2) {
				return f1.size != f2.size || f1.center != f2.center;
			}

			public override bool Equals (object obj) {
				if (obj == null || !(obj is Face))
					return false;
				Face other = (Face)obj;
				return size == other.size && center == other.center;
			}

			public override int GetHashCode () {
				int hash = 23;
				hash = hash * 31 + center.GetHashCode ();
				hash = hash * 31 + size.GetHashCode ();
				return hash;
			}
		}

		static Vector3[] faceVerticesForward =  {
			new Vector3 (0.5f, -0.5f, 0.5f),
			new Vector3 (0.5f, 0.5f, 0.5f),
			new Vector3 (-0.5f, -0.5f, 0.5f),
			new Vector3 (-0.5f, 0.5f, 0.5f)
		};
		static Vector3[] faceVerticesBack = {
			new Vector3 (-0.5f, -0.5f, -0.5f),
			new Vector3 (-0.5f, 0.5f, -0.5f),
			new Vector3 (0.5f, -0.5f, -0.5f),
			new Vector3 (0.5f, 0.5f, -0.5f)
		};
		static Vector3[] faceVerticesLeft = {
			new Vector3 (-0.5f, -0.5f, 0.5f),
			new Vector3 (-0.5f, 0.5f, 0.5f),
			new Vector3 (-0.5f, -0.5f, -0.5f),
			new Vector3 (-0.5f, 0.5f, -0.5f)
		};
		static Vector3[] faceVerticesRight ={
			new Vector3 (0.5f, -0.5f, -0.5f),
			new Vector3 (0.5f, 0.5f, -0.5f),
			new Vector3 (0.5f, -0.5f, 0.5f),
			new Vector3 (0.5f, 0.5f, 0.5f)
		};
		static Vector3[] faceVerticesTop =  {
			new Vector3 (-0.5f, 0.5f, 0.5f),
			new Vector3 (0.5f, 0.5f, 0.5f),
			new Vector3 (-0.5f, 0.5f, -0.5f),
			new Vector3 (0.5f, 0.5f, -0.5f)
		};
		static Vector3[] faceVerticesBottom = {
			new Vector3 (-0.5f, -0.5f, -0.5f),
			new Vector3 (0.5f, -0.5f, -0.5f),
			new Vector3 (-0.5f, -0.5f, 0.5f),
			new Vector3 (0.5f, -0.5f, 0.5f)
		};
		static Vector3[] normalsBack =  {
			Misc.vector3back, Misc.vector3back, Misc.vector3back, Misc.vector3back
		};
		static Vector3[] normalsForward = {
			Misc.vector3forward, Misc.vector3forward, Misc.vector3forward, Misc.vector3forward
		};
		static Vector3[] normalsLeft = {
			Misc.vector3left, Misc.vector3left, Misc.vector3left, Misc.vector3left
		};
		static Vector3[] normalsRight = {
			Misc.vector3right, Misc.vector3right, Misc.vector3right, Misc.vector3right
		};
		static Vector3[] normalsUp = {
			Misc.vector3up, Misc.vector3up, Misc.vector3up, Misc.vector3up
		};
		static Vector3[] normalsDown = {
			Misc.vector3down, Misc.vector3down, Misc.vector3down, Misc.vector3down
		};
		static Vector3[] faceUVs =  {
			new Vector3 (0, 0, 0), new Vector3 (0, 1, 0), new Vector3 (1, 0, 0), new Vector3 (1, 1, 0)
		};


		public static ModelDefinition GetModelDefinition (VoxelDefinition voxelTemplate, ColorBasedModelDefinition model, bool ignoreOffset, ColorToVoxelMap colorMap = null) {
			ModelDefinition md = ScriptableObject.CreateInstance<ModelDefinition> ();
			md.sizeX = model.sizeX;
			md.sizeY = model.sizeY;
			md.sizeZ = model.sizeZ;
			if (!ignoreOffset) {
				md.offsetX = model.offsetX;
				md.offsetY = model.offsetY;
				md.offsetZ = model.offsetZ;
			}
			if (colorMap != null && voxelTemplate == null) {
				voxelTemplate = colorMap.defaultVoxelDefinition;

			}
			List<ModelBit> bits = new List<ModelBit> ();
			for (int y = 0; y < model.sizeY; y++) {
				int posy = y * model.sizeX * model.sizeZ;
				for (int z = 0; z < model.sizeZ; z++) {
					int posz = z * model.sizeX;
					for (int x = 0; x < model.sizeX; x++) {
						int index = posy + posz + x;
						if (model.colors [index].a > 0) {
							ModelBit bit = new ModelBit ();
							bit.voxelIndex = index;
							if (colorMap != null) {
								bit.voxelDefinition = colorMap.GetVoxelDefinition (model.colors [index], voxelTemplate);
								bit.color = Misc.color32White;
								//bit.color = colorMap.colorMap[index].color;
							} else {
								bit.voxelDefinition = voxelTemplate;
								bit.color = model.colors [index];
							}
							bits.Add (bit);
						}
					}
				}
			}
			md.bits = bits.ToArray ();
			return md;
		}

		public static ColorToVoxelMap GetColorToVoxelMapDefinition (ColorBasedModelDefinition model, bool ignoreTransparency = true) {
			ColorToVoxelMap mapping = ScriptableObject.CreateInstance<ColorToVoxelMap> ();
			List<Color32> uniqueColors = new List<Color32> ();
			Color32 prevColor = Misc.color32Transparent;
			for (int k = 0; k < model.colors.Length; k++) {
				Color32 color = model.colors [k];
				if (color.a == 0) continue;
                if (color.r == prevColor.r && color.g == prevColor.g && color.b == prevColor.b)
					continue;
				if (ignoreTransparency) {
					color.a = 255;
				}
				if (!uniqueColors.Contains (color)) {
					uniqueColors.Add (color);
				}
			}
			int colorCount = uniqueColors.Count;
			mapping.colorMap = new ColorToVoxelMapEntry[colorCount];
			for (int k = 0; k < colorCount; k++) {
				mapping.colorMap [k].color = uniqueColors [k];
			}
			return mapping;
		}


		static List<Vector3> vertices = new List<Vector3> ();
		static List<int> indices = new List<int> ();
		static List<Vector3> uvs = new List<Vector3> ();
		static List<Vector3> normals = new List<Vector3> ();
		static List<Color32> meshColors = new List<Color32> ();
		static Cuboid[] cuboids = new Cuboid[128];
		static Material litMat;

		public static GameObject GenerateVoxelObject (Color32[] colors, int sizeX, int sizeY, int sizeZ, Vector3 offset, Vector3 scale) {
			return GenerateVoxelObject (colors, null, sizeX, sizeY, sizeZ, offset, scale, true);
		}


		public static GameObject GenerateVoxelObject (Color32[] colors, int[] textureIndices, int sizeX, int sizeY, int sizeZ, Vector3 offset, Vector3 scale, bool skipTransparentEntries = false) {

			int index;
			int ONE_Y_ROW = sizeZ * sizeX;
			int ONE_Z_ROW = sizeX;

			Vector3 center;
			Cuboid cuboid = new Cuboid ();
			int cuboidsCount = 0;
			for (int y = 0; y < sizeY; y++) {
				int posy = y * ONE_Y_ROW;
				for (int z = 0; z < sizeZ; z++) {
					int posz = z * ONE_Z_ROW;
					for (int x = 0; x < sizeX; x++) {
						index = posy + posz + x;
						Color32 color = colors [index];
						if (!skipTransparentEntries || color.a > 0) {
							center.x = x - sizeX / 2f + 0.5f;
							center.y = y + 0.5f;
							center.z = z - sizeZ / 2f + 0.5f;
							cuboid.bounds = new Bounds (center, Misc.vector3one);
							cuboid.color = color;
							cuboid.textureIndex = textureIndices != null ? textureIndices [index] : 0;
							if (cuboidsCount >= cuboids.Length) {
								Cuboid[] newCuboids = new Cuboid[cuboidsCount * 2];
								System.Array.Copy (cuboids, newCuboids, cuboids.Length);
								cuboids = newCuboids;
							}
							cuboids [cuboidsCount++] = cuboid;
						}
					}
				}
			}

			// Optimization 1: Fusion same color cuboids
			bool repeat = true;
			while (repeat) {
				repeat = false;
				for (int k = 0; k < cuboidsCount; k++) {
					if (cuboids [k].deleted)
						continue;
					for (int j = k + 1; j < cuboidsCount; j++) {
						if (cuboids [j].deleted)
							continue;
						if (cuboids [k].color.r == cuboids [j].color.r && cuboids [k].color.g == cuboids [j].color.g && cuboids [k].color.b == cuboids [j].color.b && cuboids [k].textureIndex == cuboids [j].textureIndex) {
							bool touching = false;
							Bounds f1 = cuboids [k].bounds;
							Bounds f2 = cuboids [j].bounds;
							// Touching back or forward faces?
							if (f1.min.x == f2.min.x && f1.max.x == f2.max.x && f1.min.y == f2.min.y && f1.max.y == f2.max.y) {
								touching = f1.min.z == f2.max.z || f1.max.z == f2.min.z;
								// ... left or right faces?
							} else if (f1.min.z == f2.min.z && f1.max.z == f2.max.z && f1.min.y == f2.min.y && f1.max.y == f2.max.y) {
								touching = f1.min.x == f2.max.x || f1.max.x == f2.min.x;
								// ... top or bottom faces?
							} else if (f1.min.x == f2.min.x && f1.max.x == f2.max.x && f1.min.z == f2.min.z && f1.max.z == f2.max.z) {
								touching = f1.min.y == f2.max.y || f1.max.y == f2.min.y;
							}
							if (touching) {
								cuboids [k].bounds.Encapsulate (cuboids [j].bounds);
								cuboids[j].deleted = true;
								repeat = true;
							}
						}
					}
				}
			}

			// Optimization 2: Remove hidden cuboids
			for (int k = 0; k < cuboidsCount; k++) {
				if (cuboids [k].deleted)
					continue;
				for (int j = k + 1; j < cuboidsCount; j++) {
					if (cuboids [j].deleted)
						continue;
					int occlusion = 0;
					Bounds f1 = cuboids [k].bounds;
					Bounds f2 = cuboids [j].bounds;
					// Touching back or forward faces?
					if (f1.min.x >= f2.min.x && f1.max.x <= f2.max.x && f1.min.y >= f2.min.y && f1.max.y <= f2.max.y) {
						if (f1.min.z == f2.max.z)
							occlusion++;
						if (f1.max.z == f2.min.z)
							occlusion++;
						// ... left or right faces?
					} else if (f1.min.z >= f2.min.z && f1.max.z <= f2.max.z && f1.min.y >= f2.min.y && f1.max.y <= f2.max.y) {
						if (f1.min.x == f2.max.x)
							occlusion++;
						if (f1.max.x == f2.min.x)
							occlusion++;
						// ... top or bottom faces?
					} else if (f1.min.x >= f2.min.x && f1.max.x <= f2.max.x && f1.min.z >= f2.min.z && f1.max.z <= f2.max.z) {
						if (f1.min.y == f2.max.y)
							occlusion++;
						if (f1.max.y == f2.min.y)
							occlusion++;
					}
					if (occlusion == 6) {
						cuboids [k].deleted = true;
						break;
					}
				}
			}

			// Optimization 3: Fragment cuboids into faces and remove duplicates
			List<Face> faces = new List<Face> ();
			for (int k = 0; k < cuboidsCount; k++) {
				if (cuboids [k].deleted)
					continue;
				Vector3 min = cuboids [k].bounds.min;
				Vector3 max = cuboids [k].bounds.max;
				Vector3 size = cuboids [k].bounds.size;
				Face top = new Face (new Vector3 ((min.x + max.x) * 0.5f, max.y, (min.z + max.z) * 0.5f), new Vector3 (size.x, 0, size.z), faceVerticesTop, normalsUp, cuboids [k].color, cuboids [k].textureIndex);
				RemoveDuplicateOrAddFace (faces, top);
				Face bottom = new Face (new Vector3 ((min.x + max.x) * 0.5f, min.y, (min.z + max.z) * 0.5f), new Vector3 (size.x, 0, size.z), faceVerticesBottom, normalsDown, cuboids [k].color, cuboids [k].textureIndex);
				RemoveDuplicateOrAddFace (faces, bottom);
				Face left = new Face (new Vector3 (min.x, (min.y + max.y) * 0.5f, (min.z + max.z) * 0.5f), new Vector3 (0, size.y, size.z), faceVerticesLeft, normalsLeft, cuboids [k].color, cuboids [k].textureIndex);
				RemoveDuplicateOrAddFace (faces, left);
				Face right = new Face (new Vector3 (max.x, (min.y + max.y) * 0.5f, (min.z + max.z) * 0.5f), new Vector3 (0, size.y, size.z), faceVerticesRight, normalsRight, cuboids [k].color, cuboids [k].textureIndex);
				RemoveDuplicateOrAddFace (faces, right);
				Face back = new Face (new Vector3 ((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, min.z), new Vector3 (size.x, size.y, 0), faceVerticesBack, normalsBack, cuboids [k].color, cuboids [k].textureIndex);
				RemoveDuplicateOrAddFace (faces, back);
				Face forward = new Face (new Vector3 ((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, max.z), new Vector3 (size.x, size.y, 0), faceVerticesForward, normalsForward, cuboids [k].color, cuboids [k].textureIndex);
				RemoveDuplicateOrAddFace (faces, forward);
			}

			// Create geometry & uv mapping
			int facesCount = faces.Count;
			vertices.Clear ();
			uvs.Clear ();
			indices.Clear ();
			normals.Clear ();
			meshColors.Clear ();
			index = 0;
			for (int k = 0; k < facesCount; k++, index += 4) {
				Face face = faces [k];
				Vector3 faceVertex;
				for (int j = 0; j < 4; j++) {
					faceVertex.x = (face.center.x + face.vertices [j].x * face.size.x) * scale.x + offset.x;
					faceVertex.y = (face.center.y + face.vertices [j].y * face.size.y) * scale.y + offset.y;
					faceVertex.z = (face.center.z + face.vertices [j].z * face.size.z) * scale.z + offset.z;
					vertices.Add (faceVertex);
					meshColors.Add (face.color);
					faceUVs [j].z = face.textureIndex;
					uvs.Add (faceUVs [j]);
				}
				normals.AddRange (face.normals);
				indices.Add (index);
				indices.Add (index + 1);
				indices.Add (index + 2);
				indices.Add (index + 3);
				indices.Add (index + 2);
				indices.Add (index + 1);
			}

			Mesh mesh = new Mesh ();
			mesh.SetVertices (vertices);
			if (textureIndices != null) {
				mesh.SetUVs (0, uvs);
			}
			mesh.SetNormals (normals);
			mesh.SetTriangles (indices, 0);
			mesh.SetColors (meshColors);

			GameObject obj = new GameObject ("Model");
			MeshFilter mf = obj.AddComponent<MeshFilter> ();
			mf.mesh = mesh;
			MeshRenderer mr = obj.AddComponent<MeshRenderer> ();
			if (litMat == null) {
				litMat = Object.Instantiate (Resources.Load<Material> ("VoxelPlay/Materials/VP Model VertexLit"));
				litMat.DisableKeyword ("VOXELPLAY_GPU_INSTANCING"); // keyword is set by Voxel Play at runtime
			} 
			mr.sharedMaterial = litMat;

			return obj;
		}

		static void RemoveDuplicateOrAddFace (List<Face> faces, Face face) {
			int index = faces.IndexOf (face);
			if (index >= 0) {
				faces.RemoveAt (index);
			} else {
				faces.Add (face);
			}
		}


		/// <summary>
		/// Generates a gameobject from a model definition. Currently it does not convert textures.
		/// </summary>
		public static GameObject GenerateVoxelObject (ModelDefinition modelDefinition, Vector3 offset, Vector3 scale) {

			int sizeY = modelDefinition.sizeY;
			int sizeZ = modelDefinition.sizeZ;
			int sizeX = modelDefinition.sizeX;
			Color32[] colors = new Color32[sizeY * sizeZ * sizeX];
			int[] textureIndices = new int[colors.Length];
			for (int k = 0; k < modelDefinition.bits.Length; k++) {
				if (modelDefinition.bits [k].isEmpty) {
					continue;
				}
				int voxelIndex = modelDefinition.bits [k].voxelIndex;
				if (voxelIndex >= 0 && voxelIndex < colors.Length) {
					VoxelDefinition vd = modelDefinition.bits [k].voxelDefinition;
					colors [voxelIndex] = modelDefinition.bits [k].finalColor;
					textureIndices [voxelIndex] = vd.textureIndexSide;
				}
			}
			return GenerateVoxelObject (colors, textureIndices, sizeX, sizeY, sizeZ, offset, scale, true);
		}

	}

}
