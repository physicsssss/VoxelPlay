using System;
using System.Collections.Generic;
using UnityEngine;


namespace VoxelPlay
{

    public enum ChunkRenderState : byte
    {
        Pending,
        RenderingRequested,
        RenderingComplete
    }

    public enum ChunkVisibleDistanceStatus : byte
    {
        Unknown = 0,
        WithinVisibleDistance = 1,
        OutOfVisibleDistance = 2
    }

    public partial class VoxelChunk : MonoBehaviour
    {

        /// <summary>
        /// Index of this chunk in the pool
        /// </summary>
        [NonSerialized]
        public int poolIndex;

        /// <summary>
        /// Voxels definition
        /// </summary>
        [NonSerialized]
        public Voxel [] voxels;

        /// <summary>
        /// Number of voxels in this chunk that contribute to mesh or custom types
        /// </summary>
        [NonSerialized]
        public int totalVisibleVoxelsCount;

        /// <summary>
        /// Chunk center position. A 16x16x16 chunk starts at position-8 and ends on position+8
        /// </summary>
        [NonSerialized] public Vector3 position;

        /// <summary>
        /// If the chunk is visible in frustum. This value is stored for internal optimization purposes and could not reflect the current state, call ChunkIsInFrustum() instead if you want to know if a chunk is within camera frustum.
        /// </summary>
        [NonSerialized] public bool visibleInFrustum;

        [NonSerialized] public int frustumCheckIteration;

        [NonSerialized] public int lightmapSignature = -1;

        [NonSerialized] public int voxelSignature;

        [NonSerialized] public MeshFilter mf;

        [NonSerialized] public MeshRenderer mr;

        [NonSerialized] public MeshCollider mc;

        [NonSerialized] public bool allowTrees = true;

        [NonSerialized] public int navMeshSourceIndex = -1;

        [NonSerialized] public Mesh navMesh;

        /// <summary>
        /// If this chunk is currently within visible distance or not
        /// </summary>
        [NonSerialized] public ChunkVisibleDistanceStatus visibleDistanceStatus;

        /// <summary>
        /// A flag that specified if this chunk is being hit by day light from above
        /// </summary>
		[NonSerialized] public bool isAboveSurface;

        /// <summary>
        /// A flag that specifies that the chunk mesh needs to be rebuilt when it gets refreshed
        /// </summary>
        [NonSerialized] public bool needsMeshRebuild;

        /// <summary>
        /// A flag that specifies that the chunk lightmap needs to be rebuilt when it gets refreshed
        /// </summary>
        [NonSerialized] public bool needsLightmapRebuild;

        /// <summary>
        /// A flag that specifies that the chunk to be rendered will ignore frustum (ie. can be a chunk required by a distant AI)
        /// </summary>
        [NonSerialized] public bool ignoreFrustum;

        /// <summary>
        /// If chunk has been filled/populated with voxels. It might not been rendered yet.
        /// </summary>
        [NonSerialized] public bool isPopulated;

        /// <summary>
        /// Chunk is pending rendering (in queue)
        /// </summary>
        [NonSerialized] public bool inqueue;

        /// <summary>
        /// If this chunk can be reused, or it's a special chunk that needs to stay as it's
        /// </summary>
        /// <value><c>true</c> if can be reused; otherwise, <c>false</c>.</value>
        [NonSerialized] public bool cannotBeReused;

        /// <summary>
        /// if this chunk is used for cloud rendering.
        /// </summary>
        [NonSerialized] public bool isCloud;

        /// <summary>
        /// Chunk has been modified in game
        /// </summary>
        [NonSerialized] public bool modified;

        /// <summary>
        /// Returns true if the chunk has been rendered at least once (and it might have no visible contents)
        /// </summary>
        [NonSerialized] public ChunkRenderState renderState = ChunkRenderState.Pending;

        /// <summary>
        /// Chunk has been rendered and uploaded to the GPU?
        /// </summary>
        public bool isRendered { get { return renderState == ChunkRenderState.RenderingComplete; } }

        /// <summary>
        /// Which neighbours were not created yet when this chunk data was generated. Used to refresh NEW chunks when their neighbourhood is available.
        /// </summary>
        [NonSerialized]
        public byte inconclusiveNeighbours;

        /// <summary>
        /// The frame number where this chunk is rendered. Used for optimization.
        /// </summary>
        [NonSerialized]
        public int renderingFrame;

        /// <summary>
        /// Light sources in this chunk (ie. torches)
        /// </summary>
        [NonSerialized]
        public List<LightSource> lightSources;

        /// <summary>
        /// Voxel placeholders in this chunk. A placeholder is used to provide additional visual or interaction to a specific voxel (ie. damage cracks, physics, ...)
        /// </summary>
        [NonSerialized]
        public FastHashSet<VoxelPlaceholder> placeholders;

        /// <summary>
        /// Items spawn in this chunk
        /// </summary>
        [NonSerialized]
        public FastList<Item> items;

        /// <summary>
        /// Additional optional data for some voxels
        /// </summary>
        [NonSerialized]
        public FastHashSet<VoxelExtraData> voxelsExtraData;

        VoxelChunk _top;

        public VoxelChunk top {
            get {
                if ((object)_top == null) {
                    Vector3 topPosition = position;
                    topPosition.y += VoxelPlayEnvironment.CHUNK_SIZE;
                    VoxelPlayEnvironment.instance.GetChunk (topPosition, out _top, false);
                    if ((object)_top != null)
                        _top._bottom = this;
                }
                return _top;
            }
            set {
                _top = value;
            }
        }

        VoxelChunk _bottom;

        public VoxelChunk bottom {
            get {
                if ((object)_bottom == null) {
                    Vector3 bottomPosition = position;
                    bottomPosition.y -= VoxelPlayEnvironment.CHUNK_SIZE;
                    VoxelPlayEnvironment.instance.GetChunk (bottomPosition, out _bottom, false);
                    if ((object)_bottom != null)
                        _bottom._top = this;
                }
                return _bottom;
            }
            set {
                _bottom = value;
            }
        }

        VoxelChunk _left;

        public VoxelChunk left {
            get {
                if ((object)_left == null) {
                    Vector3 leftPosition = position;
                    leftPosition.x -= VoxelPlayEnvironment.CHUNK_SIZE;
                    VoxelPlayEnvironment.instance.GetChunk (leftPosition, out _left, false);
                    if ((object)_left != null)
                        _left._right = this;
                }
                return _left;
            }
            set {
                _left = value;
            }

        }

        VoxelChunk _right;

        public VoxelChunk right {
            get {
                if ((object)_right == null) {
                    Vector3 rightPosition = position;
                    rightPosition.x += VoxelPlayEnvironment.CHUNK_SIZE;
                    VoxelPlayEnvironment.instance.GetChunk (rightPosition, out _right, false);
                    if ((object)_right != null)
                        _right._left = this;
                }
                return _right;
            }
            set {
                _right = value;
            }

        }

        VoxelChunk _forward;

        public VoxelChunk forward {
            get {
                if ((object)_forward == null) {
                    Vector3 forwardPosition = position;
                    forwardPosition.z += VoxelPlayEnvironment.CHUNK_SIZE;
                    VoxelPlayEnvironment.instance.GetChunk (forwardPosition, out _forward, false);
                    if ((object)_forward != null)
                        _forward._back = this;
                }
                return _forward;
            }
            set {
                _forward = value;
            }

        }

        VoxelChunk _back;

        public VoxelChunk back {
            get {
                if ((object)_back == null) {
                    Vector3 backPosition = position;
                    backPosition.z -= VoxelPlayEnvironment.CHUNK_SIZE;
                    VoxelPlayEnvironment.instance.GetChunk (backPosition, out _back, false);
                    if ((object)_back != null)
                        _back._forward = this;
                }
                return _back;
            }
            set {
                _back = value;
            }

        }

        /// <summary>
        /// Used to accelerate certain algorithms
        /// </summary>
        [NonSerialized]
        public int tempFlag;


        [NonSerialized]
        public bool lightmapIsClear;


        /// <summary>
		/// Clears the lightmap of this chunk or initializes it with a value
		/// </summary>
		public void ClearLightmap (byte value = 0)
        {
            if (lightmapIsClear && voxels [0].light == value)
                return;
            for (int k = 0; k < voxels.Length; k++) {
                voxels [k].light = value;
                voxels [k].torchLight = 0;
            }
            lightmapIsClear = true;
        }

        /// <summary>
        /// Removes all existing voxels in this chunk.
        /// </summary>
        public void ClearVoxels (byte light)
        {
            if (lightSources != null) {
                int lightSourcesCount = lightSources.Count;
                for (int k = 0; k < lightSourcesCount; k++) {
                    if (lightSources [k].gameObject != null) {
                        DestroyImmediate (lightSources [k].gameObject);
                    }
                }
                lightSources.Clear ();
            }
            if (placeholders != null) {
                int phCount = placeholders.Count;
                for (int k = 0; k < phCount; k++) {
                    if (placeholders.entries [k].key >= 0) {
                        VoxelPlaceholder ph = placeholders.entries [k].value;
                        if (ph != null) {
                            DestroyImmediate (ph.gameObject);
                        }
                    }
                }
                placeholders.Clear ();
            }
            if (voxelsExtraData != null) {
                voxelsExtraData.Clear ();
            }

            Voxel.Clear (voxels, light);
        }

        /// <summary>
        /// Clears a single voxel
        /// </summary>
        /// <param name="voxelIndex">Index of voxel in the chunk</param>
        /// <param name="light">Light intensity left at the empty position</param>
        public void ClearVoxel (int voxelIndex, byte light)
        {
            if (lightSources != null) {
                int lightSourcesCount = lightSources.Count;
                for (int k = 0; k < lightSourcesCount; k++) {
                    LightSource ls = lightSources [k];
                    if (ls.voxelIndex == voxelIndex && ls.gameObject != null) {
                        DestroyImmediate (ls.gameObject);
                        break;
                    }
                }
            }
            if (placeholders != null) {
                VoxelPlaceholder placeholder;
                if (placeholders.TryGetValue (voxelIndex, out placeholder)) {
                    if (placeholder != null && placeholder.voxelIndex == voxelIndex) {
                        DestroyImmediate (placeholder.gameObject);
                        placeholders.Remove (voxelIndex);
                    }
                }
            }
            voxels [voxelIndex].Clear (light);
        }

        /// <summary>
        /// Returns true if this chunk contains a given position in world space
        /// </summary>
        public bool Contains (Vector3 position)
        {
            float xDiff = position.x - this.position.x;
            float yDiff = position.y - this.position.y;
            float zDiff = position.z - this.position.z;
            return (xDiff <= (VoxelPlayEnvironment.CHUNK_HALF_SIZE - 1) && xDiff >= -VoxelPlayEnvironment.CHUNK_HALF_SIZE && yDiff <= (VoxelPlayEnvironment.CHUNK_HALF_SIZE - 1) && yDiff >= -VoxelPlayEnvironment.CHUNK_HALF_SIZE && zDiff <= (VoxelPlayEnvironment.CHUNK_HALF_SIZE - 1) && zDiff >= -VoxelPlayEnvironment.CHUNK_HALF_SIZE);
        }


        /// <summary>
        /// Clears chunk state before returning it to the pool. This method is called when this chunk is reused.
        /// </summary>
        public void PrepareForReuse (byte light)
        {
            isAboveSurface = false;
            needsMeshRebuild = false;
            isPopulated = false;
            inqueue = false;
            modified = false;
            renderState = ChunkRenderState.Pending;
            allowTrees = true;
            lightmapSignature = -1;
            frustumCheckIteration = 0;
            navMesh = null;
            navMeshSourceIndex = -1;
            inconclusiveNeighbours = 0;
            renderingFrame = -1;
            visibleDistanceStatus = ChunkVisibleDistanceStatus.Unknown;
            lightmapIsClear = false;

            if (items != null) {
                for (int k = 0; k < items.count; k++) {
                    Item item = items.values [k];
                    if (item != null && item.gameObject != null) {
                        DestroyImmediate (item.gameObject);
                    }
                }
                items.Clear ();
            }


            if (_left != null) {
                _left.right = null;
                _left = null;
            }
            if (_right != null) {
                _right.left = null;
                _right = null;
            }
            if (_forward != null) {
                _forward.back = null;
                _forward = null;
            }
            if (_back != null) {
                _back.forward = null;
                _back = null;
            }
            if (_top != null) {
                _top.bottom = null;
                _top = null;
            }
            if (_bottom != null) {
                _bottom.top = null;
                _bottom = null;
            }
            ClearVoxels (light);
            mr.enabled = false;
            if (mc != null) mc.enabled = false;
            gameObject.SetActive (true);
        }

        /// <summary>
        /// Marks this chunk as inconclusive which means some neighbour may need to be rendered again due some special changes in this chunk (eg. drawing holes on this chunk edges)
        /// </summary>
        public void MarkAsInconclusive (int neighbourFlags = 128)
        {
            inconclusiveNeighbours = (byte)(inconclusiveNeighbours | neighbourFlags);
        }

        public void RemoveItem (Item item)
        {
            if (items != null) {
                if (items.Remove (item)) {
                    modified = true;
                }
            }
        }

        public void AddItem (Item item)
        {
            if (items == null) {
                items = new FastList<Item> ();
            }
            items.Add (item);
            modified = true;
        }

        public override string ToString ()
        {
            return string.Format ("[VoxelChunk: x={0}, y={1}, zm={2}]", position.x, position.y, position.z);
        }

        public LightSource GetLightSource (int voxelIndex)
        {
            if (lightSources == null) return null;
            int lsCount = lightSources.Count;
            for (int k = 0; k < lsCount; k++) {
                LightSource ls = lightSources [k];
                if (ls.voxelIndex == voxelIndex) {
                    return ls;
                }
            }
            return null;
        }

        public void AddLightSource (LightSource ls)
        {
            if (lightSources == null) {
                lightSources = new List<LightSource> ();
            }
            lightSources.Add (ls);
        }

        public void RemoveLightSource (int voxelIndex)
        {
            int count = lightSources.Count;
            for (int k = 0; k < count; k++) {
                if (lightSources [k].voxelIndex == voxelIndex) {
                    lightSources.RemoveAt (k);
                    k--;
                    count--;
                }
            }
        }

        public void AddLightSource (int voxelIndex, byte lightIntensity)
        {
            // Check if current light intensity is lower
            if (lightSources != null) {
                int count = lightSources.Count;
                for (int k=0;k<count;k++) {
                    LightSource l = lightSources [k];
                    if (l.voxelIndex == voxelIndex) {
                        if (l.lightIntensity < lightIntensity) {
                            l.lightIntensity = lightIntensity;
                        }
                        return;
                    }
                }
            }
            LightSource ls = new LightSource ();
            ls.voxelIndex = voxelIndex;
            ls.lightIntensity = lightIntensity;
            AddLightSource (ls);
        }
    }


}