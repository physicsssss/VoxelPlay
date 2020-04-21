//#define USES_TINTING

using System.Runtime.CompilerServices;
using UnityEngine;

namespace VoxelPlay
{


    public partial struct Voxel
    {

        /// <summary>
        /// Voxel definition index in voxelDefinitions list
        /// </summary>
        public ushort typeIndex;

        public bool isEmpty {
            get { return typeIndex == 0; }
        }

        public VoxelDefinition type {
            get {
                return VoxelPlayEnvironment.instance.voxelDefinitions [typeIndex];
            }
        }

        /// <summary>
        /// If this voxel has content. 1 = has content, 0 or other = empty, 2 = hole (temporarily used by cave generator)
        /// </summary>
        public byte hasContent;

        /// <summary>
        /// If this voxel lets light pass through, opaque = 0, otherwise this is the light intensity reduction factor
        /// </summary>
        public byte opaque;

        /// <summary>
        /// Current Sun light value of this voxel. Light is the intensity of light that crosses the voxel.
        /// This value will fluctuate during a lightmap update. Use lightMesh to get the last light value sent to the mesh vertices.
        /// Lower 4 bits are for Sun light contribution. Top 4 bits are for torch contribution.
        /// </summary>
        public byte light;

        /// <summary>
        /// Torch light intensity for this voxel.
        /// </summary>
        public byte torchLight;

        /// <summary>
        /// Packed light intensities (torch + Sun light)
        /// </summary>
        public int packedLight {
            get { return (torchLight << 12) | light; }
        }

        [MethodImpl (256)] // equals to MethodImplOptions.AggressiveInlining
        public int GetPackedLight (float variation)
        {
            return (torchLight << 12) + (int)(light * variation);
        }

#if USES_TINTING
		public byte red, green, blue;
#else
        public byte red { get { return 255; } }
        public byte green { get { return 255; } }
        public byte blue { get { return 255; } }
#endif

        /// <summary>
        /// Returns the 
        /// </summary>
        /// <value>The color of the tint.</value>
        public Color32 color {
            get {
#if USES_TINTING
				return new Color32 (red, green, blue, 255);
#else
                return Misc.color32White;
#endif
            }
            set {
#if USES_TINTING
				red = value.r;
				green = value.g;
				blue = value.b;
#endif
            }
        }

        /// <summary>
        /// Returns true if this voxel has a custom color set
        /// </summary>
        public bool isColored {
            get {
                return red != 255 || green != 255 || blue != 255;
            }
        }

        /// <summary>
        /// Extra packed info for this voxel
        /// Lower 4 bits = water level
        /// Upper 4 bits = texture rotation
        /// </summary>
        byte _flags;

        [MethodImpl (256)] // equals to MethodImplOptions.AggressiveInlining
        public byte GetFlags ()
        {
            return _flags;
        }

        [MethodImpl (256)] // equals to MethodImplOptions.AggressiveInlining
        public void SetFlags (byte value)
        {
            _flags = value;
        }

        [MethodImpl (256)] // equals to MethodImplOptions.AggressiveInlining
        public int GetWaterLevel ()
        {
            return _flags & 0xF;
        }

        [MethodImpl (256)] // equals to MethodImplOptions.AggressiveInlining
        public void SetWaterLevel (int value)
        {
            _flags = (byte)((_flags & 0xF0) | value);
        }

        [MethodImpl (256)] // equals to MethodImplOptions.AggressiveInlining
        public int GetTextureRotation ()
        {
            return _flags >> 4;
        }


        [MethodImpl (256)] // equals to MethodImplOptions.AggressiveInlining
        public float GetTextureRotationDegrees ()
        {
            int rot = _flags >> 4;
            switch (rot) {
            case 1:
                return 90;
            case 2:
                return 180;
            case 3:
                return 270;
            default:
                return 0;
            }
        }


        [MethodImpl (256)] // equals to MethodImplOptions.AggressiveInlining
        public static float GetTextureRotationDegrees (int rotation)
        {
            switch (rotation) {
            case 1:
                return 90;
            case 2:
                return 180;
            case 3:
                return 270;
            default:
                return 0;
            }
        }


        [MethodImpl (256)] // equals to MethodImplOptions.AggressiveInlining
        public static int GetTextureRotationFromDegrees (float rotation)
        {
            if (rotation == 90f) {
                return 1;
            } else if (rotation == 180) {
                return 2;
            } else if (rotation == 270) {
                return 3;
            } else {
                return 0;
            }
        }

        [MethodImpl (256)] // equals to MethodImplOptions.AggressiveInlining
        public void SetTextureRotation (int value)
        {
            _flags = (byte)((_flags & 0xF) | (value << 4));
        }

        /// <summary>
        /// Last light value sent to the mesh vertex. This is the real visible light intensity.
        /// </summary>
        public byte lightMesh;


        public byte lightMeshOrTorch {
            get { return lightMesh > torchLight ? lightMesh : torchLight; }
        }

        /// <summary>
        /// Whether this voxel has water
        /// </summary>
        /// <value><c>true</c> if has water; otherwise, <c>false</c>.</value>
        public bool hasWater {
            get {
                return (_flags & 0xF) > 0;
            }
        }

        /// <summary>
        /// Whether this voxel is a solid block
        /// </summary>
        /// <value><c>true</c> if is solid; otherwise, <c>false</c>.</value>
        public bool isSolid {
            get {
                return opaque >= 15;
            }
        }


        public Voxel (VoxelDefinition type)
        {
            if ((object)type != null) {
                this.hasContent = type.hasContent;
                switch (type.renderType) {
                case RenderType.Opaque:
                case RenderType.Opaque6tex:
                case RenderType.OpaqueAnimated:
                    this.opaque = 15;
                    this._flags = 0;
                    break;
                case RenderType.Transp6tex:
                    this.opaque = 2;
                    this._flags = 0;
                    break;
                case RenderType.Cutout:
                    this.opaque = 3;
                    this._flags = 0;
                    break;
                case RenderType.Water:
                    this.opaque = 2;
                    this._flags = 0xF;
                    break;
                case RenderType.OpaqueNoAO:
                case RenderType.Cloud:
                    this.opaque = 15;
                    this._flags = 0;
                    break;
                default:
                    this.opaque = type.opaque;
                    this._flags = 0;
                    break;
                }
                this.typeIndex = type.index;
            } else {
                this.hasContent = (byte)0;
                this.opaque = (byte)0;
                this.typeIndex = 0;
                this._flags = 0;
            }
            this.light = 0;
            this.torchLight = 0;
            this.lightMesh = 0;

#if USES_TINTING
			this.red = this.green = this.blue = 255;
#endif
        }


        public void Clear (byte light)
        {
            typeIndex = 0;
            hasContent = 0;
            opaque = 0;
            this.light = light;
			this.torchLight = 0;
            this._flags = 0;
        }

        public static void Clear (Voxel [] voxels, byte light)
        {
            // Faster method
            Voxel emptyVoxel = new Voxel ();
            emptyVoxel.light = light;
            voxels.Fill<Voxel> (emptyVoxel);
        }

        public void Set (VoxelDefinition type)
        {
#if USES_TINTING
			this.red = type.tintColor.r;
			this.green = type.tintColor.g;
			this.blue = type.tintColor.b;
#endif

            this.typeIndex = type.index;
            this.hasContent = type.hasContent;
            switch (type.renderType) {
            case RenderType.Opaque:
            case RenderType.Opaque6tex:
                case RenderType.OpaqueAnimated:
                this.opaque = 15;
                this._flags = 0;
                break;
            case RenderType.Transp6tex:
                this.opaque = 2;
                this._flags = 0;
                break;
            case RenderType.Cutout:
                this.opaque = 3;
                this._flags = 0;
                break;
            case RenderType.Water:
                this.opaque = 2;
                this._flags = type.height;
                break;
            case RenderType.OpaqueNoAO:
            case RenderType.Cloud:
                this.opaque = 15;
                this._flags = 0;
                break;
            default:
                this.opaque = type.opaque;
                this._flags = 0;
                break;
            }
        }

        public void Set (VoxelDefinition type, Color32 tintColor)
        {
#if USES_TINTING
			this.red = tintColor.r;
			this.green = tintColor.g;
			this.blue = tintColor.b;
#endif

            this.typeIndex = type.index;
            this.hasContent = type.hasContent;
            switch (type.renderType) {
            case RenderType.Opaque:
            case RenderType.Opaque6tex:
                case RenderType.OpaqueAnimated:
                this.opaque = 15;
                this._flags = 0;
                break;
            case RenderType.Transp6tex:
                this.opaque = 2;
                this._flags = 0;
                break;
            case RenderType.Cutout:
                this.opaque = 3;
                this._flags = 0;
                break;
            case RenderType.Water:
                this.opaque = 2;
                this._flags = type.height;
                break;
            case RenderType.OpaqueNoAO:
            case RenderType.Cloud:
                this.opaque = 15;
                this._flags = 0;
                break;
            default:
                this.opaque = type.opaque;
                this._flags = 0;
                break;
            }
        }

        [MethodImpl (256)]
        public void SetFast (VoxelDefinition type, byte opaque)
        {
#if USES_TINTING
			this.red = type.tintColor.r;
			this.green = type.tintColor.g;
			this.blue = type.tintColor.b;
#endif
            this.typeIndex = type.index;
            this.hasContent = type.hasContent;
            this.opaque = opaque;
            this._flags = 0;
        }

        [MethodImpl (256)]
        public void SetFastOpaque (VoxelDefinition type)
        {
#if USES_TINTING
			this.red = type.tintColor.r;
			this.green = type.tintColor.g;
			this.blue = type.tintColor.b;
#endif
            this.typeIndex = type.index;
            this.hasContent = type.hasContent;
            this.opaque = 15;
            this._flags = 0;
        }


        [MethodImpl (256)]
        public void SetFast (VoxelDefinition type, byte opaque, Color32 tintColor)
        {
#if USES_TINTING
			this.red = tintColor.r;
			this.green = tintColor.g;
			this.blue = tintColor.b;
#endif
            this.typeIndex = type.index;
            this.hasContent = type.hasContent;
            this.opaque = type.opaque;
            this._flags = 0;
        }

        [MethodImpl (256)]
        public void SetFastWater (VoxelDefinition type)
        {
            this.typeIndex = type.index;
            this.hasContent = type.hasContent;
            this.opaque = 2;
            this._flags = type.height;
        }

        /// <summary>
        /// Represents nothing
        /// </summary>
        public static Voxel Empty = new Voxel (null);

        /// <summary>
        /// Represents a hole (an empty voxel that won't be filled by terrain generator if this voxel is placed before the terrain generator fills the chunk)
        /// </summary>
        public static Voxel Hole = new Voxel () { hasContent = 2 };


        public static bool supportsTinting {
            get {
#if USES_TINTING
				return true;
#else
                return false;
#endif
            }
        }

        public int WriteRawData (byte [] buffer, int index)
        {
            buffer [index++] = (byte)(typeIndex >> 8);
            buffer [index++] = (byte)(typeIndex % 256);
            buffer [index++] = hasContent;
            buffer [index++] = opaque;
            buffer [index++] = light;
            buffer [index++] = torchLight;
            buffer [index++] = lightMesh;
            buffer [index++] = _flags;
#if USES_TINTING
            buffer [index++] = red;
            buffer [index++] = green;
            buffer [index++] = blue;
#endif
            return index;
        }

        public int ReadRawData (byte [] buffer, int index)
        {
            typeIndex = (ushort)((buffer [index] << 8) + buffer [index + 1]);
            index += 2;
            hasContent = buffer [index++];
            opaque = buffer [index++];
            light = buffer [index++];
            torchLight = buffer [index++];
            lightMesh = buffer [index++];
            _flags = buffer [index++];
#if USES_TINTING
            red = buffer [index++];
            green = buffer [index++];
            blue = buffer [index++];
#endif
            return index;

        }


        public static bool operator == (Voxel c1, Voxel c2)
        {
            return (c1.typeIndex == c2.typeIndex && c1.opaque == c2.opaque && c1.light == c2.light && c1.torchLight == c2.torchLight && c1.hasContent == c2.hasContent && c1.lightMesh == c2.lightMesh && c1.red == c2.red && c1.green == c2.green && c1.blue == c2.blue);
        }

        public static bool operator != (Voxel c1, Voxel c2)
        {
            return !(c1.typeIndex == c2.typeIndex && c1.opaque == c2.opaque && c1.light == c2.light && c1.torchLight == c2.torchLight && c1.hasContent == c2.hasContent && c1.lightMesh == c2.lightMesh && c1.red == c2.red && c1.green == c2.green && c1.blue == c2.blue);
        }

        public override bool Equals (object obj)
        {
            if ((obj == null) || !GetType ().Equals (obj.GetType ())) {
                return false;
            }
            Voxel c2 = (Voxel)obj;
            return (typeIndex == c2.typeIndex && opaque == c2.opaque && light == c2.light && torchLight == c2.torchLight && hasContent == c2.hasContent && lightMesh == c2.lightMesh && red == c2.red && green == c2.green && blue == c2.blue);
        }

        public override int GetHashCode ()
        {
            return base.GetHashCode ();
        }

        public override string ToString ()
        {
            return "Voxel type: " + type;
        }

        public static int memorySize {
            get {
#if USES_TINTING
                return 10;
#else
                return 7;
#endif
            }
        }



    }

}
