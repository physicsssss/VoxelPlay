using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//#define DEBUG_RAYCAST

namespace VoxelPlay
{

    public enum ColliderTypes
    {
        AnyCollider = 0,
        OnlyVoxels = 1,
        IgnorePlayer = 2
    }


    public struct VoxelHitInfo
    {

        /// <summary>
        /// The world space position of the ray hit
        /// </summary>
        public Vector3 point;


        float lastSqrDistance;
        float computedDistance;

        /// <summary>
        /// Distance to the hit position
        /// </summary>
        public float distance {
            get {
                if (lastSqrDistance != sqrDistance) {
                    computedDistance = Mathf.Sqrt (sqrDistance);
                }
                return computedDistance;
            }
            set {
                computedDistance = value;
                sqrDistance = computedDistance * computedDistance;
                lastSqrDistance = sqrDistance;
            }
        }


        /// <summary>
        /// Squared distance (distance * distance) to the hit position
        /// </summary>
        public float sqrDistance;

        /// <summary>
        /// The index of the voxel being hit in the chunk.voxels array
        /// </summary>
        public int voxelIndex;

        /// <summary>
        /// The chunk to which the voxel belongs to
        /// </summary>
        public VoxelChunk chunk;

        /// <summary>
        /// The center of the voxel in world space coordinates
        /// </summary>
        public Vector3 voxelCenter;

        /// <summary>
        /// The normal of the side of the voxel being hit
        /// </summary>
        public Vector3 normal;

        /// <summary>
        /// Copy of the voxel hit. This copy does not change even if the position has been cleared after the RayCast finishes. To get the voxel at this position at this moment, call GetVoxelNow()
        /// </summary>
        /// <value>The voxel.</value>
        public Voxel voxel;

        /// <summary>
        /// The collider of the gameobject which is hit by the ray
        /// </summary>
        public Collider collider;

        /// <summary>
        /// If the voxel hit has a placeholder attached, a reference to it is returned here
        /// </summary>
        public VoxelPlaceholder placeholder;

        /// <summary>
        /// If the hit is on an item
        /// </summary>
        public Item item;


        public void Clear ()
        {
            placeholder = null;
            chunk = null;
            voxelIndex = -1;
            collider = null;
            item = null;
        }

        /// <summary>
        /// Returns a copy of the voxel at this position
        /// </summary>
        public Voxel GetVoxelNow ()
        {
            if (chunk != null && voxelIndex >= 0) {
                return chunk.voxels [voxelIndex];
            }
            return Voxel.Empty;
        }
    }

    public partial class VoxelPlayEnvironment : MonoBehaviour {
        const int DEFAULT_DESTROYED_VOXEL_PARTICLE_AMOUNT = 20;

        List<VoxelIndex> tempVoxelIndices, tempVoxelIndices2;
        Dictionary<Vector3, bool> tempVoxelPositions;
        int tempVoxelIndicesCount;
        int destroyedVoxelParticlesAmount = DEFAULT_DESTROYED_VOXEL_PARTICLE_AMOUNT;

        void InitPhysics ()
        {

            if (tempVoxelIndices == null) {
                tempVoxelIndices = new List<VoxelIndex> (100);
            } else {
                tempVoxelIndices.Clear ();
            }
            if (tempVoxelIndices2 == null) {
                tempVoxelIndices2 = new List<VoxelIndex>(100);
            } else {
                tempVoxelIndices2.Clear();
            }
            if (tempVoxelPositions == null) {
                tempVoxelPositions = new Dictionary<Vector3, bool> (100);
            } else {
                tempVoxelPositions.Clear ();
            }

            if (layerVoxels == layerParticles) {
                layerParticles = layerVoxels + 1;
            }
            Physics.IgnoreLayerCollision (layerVoxels, layerVoxels);
        }


        bool RayCastFast (Vector3 origin, Vector3 direction, out VoxelHitInfo hitInfo, float maxDistance = 0, bool createChunksIfNeeded = false, byte minOpaque = 0, ColliderTypes colliderTypes = ColliderTypes.AnyCollider)
        {

            bool voxelHit = RayCastFastVoxel (origin, direction, out hitInfo, maxDistance, createChunksIfNeeded, minOpaque);
            if ((colliderTypes & ColliderTypes.OnlyVoxels) != 0) {
                return voxelHit;
            }

            if (voxelHit) {
                maxDistance = hitInfo.distance - 0.1f;
            }
            // Cast a normal raycast to detect normal gameobjects within ray
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, maxDistance))
                {
                // ensures this is not a normal voxel in which case keep current hitInfo data
                if (hit.collider.transform.parent != chunksRoot) {
                    if ((colliderTypes & ColliderTypes.IgnorePlayer) != 0 && characterController != null && characterController.transform == hit.collider.transform) {
                        return voxelHit;
                    }
                    hitInfo.distance = hit.distance;
                    hitInfo.point = hit.point;
                    hitInfo.normal = hit.normal;
                    hitInfo.collider = hit.collider;

                    // Check if gameobject is an item
                    Item item = hit.collider.GetComponent<Item> ();
                    if (item != null && item.itemChunk != null) {
                        hitInfo.chunk = item.itemChunk;
                        hitInfo.voxelIndex = item.itemVoxelIndex;
                        hitInfo.voxel = item.itemChunk.voxels [hitInfo.voxelIndex];
                        hitInfo.voxelCenter = GetVoxelPosition (item.itemChunk, item.itemVoxelIndex);
                        hitInfo.item = item;
                        hitInfo.placeholder = null;
                        return true;
                    }

                    // Check if gameobject is a dynamic voxel
                    hitInfo.voxelIndex = -1;
                    VoxelPlaceholder placeholder = hit.collider.GetComponentInParent<VoxelPlaceholder> ();
                    if (placeholder != null) {
                        hitInfo.chunk = placeholder.chunk;
                        hitInfo.voxelIndex = placeholder.voxelIndex;
                        hitInfo.voxel = placeholder.chunk.voxels [placeholder.voxelIndex];
                        hitInfo.voxelCenter = placeholder.transform.position;
                        hitInfo.placeholder = placeholder;
                    }
                    return true;
                }
            }

            return voxelHit;
        }

        VoxelChunk RayCastFastVoxel (Vector3 origin, Vector3 direction, out VoxelHitInfo hitInfo, float maxDistance = 0, bool createChunksIfNeeded = false, byte minOpaque = 0)
        {

#if DEBUG_RAYCAST
												GameObject o;
#endif

            float maxDistanceSqr = maxDistance == 0 ? 1000 * 1000 : maxDistance * maxDistance;

            // Ray march throuch chunks until hit one loaded chunk
            Vector3 position = origin;
            VoxelChunk chunk = null;
            hitInfo = new VoxelHitInfo ();
            hitInfo.voxelIndex = -1;

            Vector3 viewDirSign = FastVector.Sign (ref direction);
            Vector3 viewSign = new Vector3 ((viewDirSign.x + 1f) * 0.5f, (viewDirSign.y + 1f) * 0.5f, (viewDirSign.z + 1f) * 0.5f); // 0 = left, 1 = right

            float vxz, vzy, vxy;
            if (direction.y != 0) {
                float a = direction.x / direction.y;
                float b = direction.z / direction.y;
                vxz = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vxz = 1000000f;
            }
            if (direction.x != 0) {
                float a = direction.z / direction.x;
                float b = direction.y / direction.x;
                vzy = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vzy = 1000000f;
            }
            if (direction.z != 0) {
                float a = direction.x / direction.z;
                float b = direction.y / direction.z;
                vxy = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vxy = 1000000f;
            }

            Vector3 v3 = new Vector3 (vzy, vxz, vxy);
            Vector3 viewSignChunk = viewSign * CHUNK_SIZE;
            Vector3 viewDirSignOffset = viewDirSign * 0.002f;

            int chunkX, chunkY, chunkZ;
            int chunkCount = 0;
            float t;
            Vector3 normal = Misc.vector3zero, db;

            while (chunkCount++ < 500) { // safety counter to avoid any potential infinite loop

                // Check max distance
                float distSqr = (position.x - origin.x) * (position.x - origin.x) + (position.y - origin.y) * (position.y - origin.y) + (position.z - origin.z) * (position.z - origin.z);
                if (distSqr > maxDistanceSqr)
                    return null;


#if DEBUG_RAYCAST
																o = GameObject.CreatePrimitive(PrimitiveType.Cube);
																o.transform.localScale = Misc.Vector3one * 0.15f;
																o.transform.position = position;
																DestroyImmediate(o.GetComponent<Collider>());
																o.GetComponent<Renderer>().material.color = Color.blue;
#endif

                FastMath.FloorToInt (position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out chunkX, out chunkY, out chunkZ);

                chunk = null;
                if (createChunksIfNeeded) {
                    chunk = GetChunkOrCreate (chunkX, chunkY, chunkZ);
                } else {
                    int x00 = WORLD_SIZE_DEPTH * WORLD_SIZE_HEIGHT * (chunkX + WORLD_SIZE_WIDTH);
                    int y00 = WORLD_SIZE_DEPTH * (chunkY + WORLD_SIZE_HEIGHT);
                    int hash = x00 + y00 + chunkZ;
                    chunk = GetChunkIfExists (hash);
                }

                chunkX *= CHUNK_SIZE;
                chunkY *= CHUNK_SIZE;
                chunkZ *= CHUNK_SIZE;

                if (chunk) {
                    // Ray-march through chunk
                    Voxel [] voxels = chunk.voxels;
                    Vector3 inPosition = position;

                    for (int k = 0; k < CHUNK_SIZE * 4; k++) {

#if DEBUG_RAYCAST
																								o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
																								o.transform.localScale = Misc.Vector3one * 0.1f;
																								o.transform.position = inPosition;
																								DestroyImmediate(o.GetComponent<Collider>());
																								o.GetComponent<Renderer>().material.color = Color.yellow;
#endif

                        // Check voxel content
                        int fx, fy, fz;
                        FastMath.FloorToInt (inPosition.x, inPosition.y, inPosition.z, out fx, out fy, out fz);
                        int py = fy - chunkY;
                        int pz = fz - chunkZ;
                        int px = fx - chunkX;
                        if (px < 0 || px >= CHUNK_SIZE || py < 0 || py >= CHUNK_SIZE || pz < 0 || pz >= CHUNK_SIZE) {
                            break;
                        }

                        int voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
                        if (voxels [voxelIndex].hasContent == 1 && (minOpaque == 255 || voxels [voxelIndex].opaque >= minOpaque) && !VoxelIsHidden (chunk, voxelIndex)) {

                            VoxelDefinition vd = voxelDefinitions [voxels [voxelIndex].typeIndex];
                            if (!vd.ignoresRayCast && (vd.renderType != RenderType.Custom || !vd.prefabUsesCollider)) {
                                // Check max distance
                                distSqr = (inPosition.x - origin.x) * (inPosition.x - origin.x) + (inPosition.y - origin.y) * (inPosition.y - origin.y) + (inPosition.z - origin.z) * (inPosition.z - origin.z);
                                if (distSqr > maxDistanceSqr)
                                    return null;

                                // Check water level or grass height
                                float voxelHeight = 0;
                                if (vd.renderType == RenderType.Water) {
                                    voxelHeight = voxels [voxelIndex].GetWaterLevel () / 15f;
                                } else if (vd.renderType == RenderType.CutoutCross) {
                                    voxelHeight = vd.scale.y;
                                }
                                bool hit = true;
                                Vector3 voxelCenter = new Vector3 (chunkX + px + 0.5f, chunkY + py + 0.5f, chunkZ + pz + 0.5f);
                                Vector3 localHitPos = inPosition - voxelCenter;
                                if (voxelHeight > 0 && voxelHeight < 1f && direction.y != 0) {
                                    t = localHitPos.y + 0.5f - voxelHeight;
                                    if (t > 0) {
                                        t = t * Mathf.Sqrt (1 + (direction.x * direction.x + direction.z * direction.z) / (direction.y * direction.y));
                                        localHitPos += t * direction;
                                        hit = localHitPos.x >= -0.5f && localHitPos.x <= 0.5f && localHitPos.z >= -0.5f && localHitPos.z <= 0.5f;
                                    }
                                }
                                if (hit) {
                                    hitInfo = new VoxelHitInfo ();
                                    hitInfo.chunk = chunk;
                                    hitInfo.voxel = voxels [voxelIndex];
                                    hitInfo.point = inPosition - normal;
                                    hitInfo.sqrDistance = distSqr;
                                    hitInfo.voxelIndex = voxelIndex;
                                    hitInfo.voxelCenter = voxelCenter;
                                    if (localHitPos.y >= 0.495) {
                                        hitInfo.normal = Misc.vector3up;
                                    } else if (localHitPos.y <= -0.495) {
                                        hitInfo.normal = Misc.vector3down;
                                    } else if (localHitPos.x < -0.495) {
                                        hitInfo.normal = Misc.vector3left;
                                    } else if (localHitPos.x > 0.495) {
                                        hitInfo.normal = Misc.vector3right;
                                    } else if (localHitPos.z < -0.495) {
                                        hitInfo.normal = Misc.vector3back;
                                    } else if (localHitPos.z > 0.495) {
                                        hitInfo.normal = Misc.vector3forward;
                                    }

#if DEBUG_RAYCAST
																												o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
																												o.transform.localScale = Misc.Vector3one * 0.15f;
																												o.transform.position = inPosition;
																												DestroyImmediate(o.GetComponent<Collider>());
																												o.GetComponent<Renderer>().material.color = Color.red;
#endif

                                    return chunk;
                                }
                            }
                        }

                        db.x = (fx + viewSign.x - inPosition.x) * v3.x;
                        db.y = (fy + viewSign.y - inPosition.y) * v3.y;
                        db.z = (fz + viewSign.z - inPosition.z) * v3.z;

                        db.x = db.x < 0 ? -db.x : db.x;
                        db.y = db.y < 0 ? -db.y : db.y;
                        db.z = db.z < 0 ? -db.z : db.z;

                        t = db.x;
                        normal.x = viewDirSignOffset.x;
                        normal.y = 0;
                        normal.z = 0;
                        if (db.y < t) {
                            t = db.y;
                            normal.x = 0;
                            normal.y = viewDirSignOffset.y;
                        }
                        if (db.z < t) {
                            t = db.z;
                            normal.x = 0;
                            normal.y = 0;
                            normal.z = viewDirSignOffset.z;
                        }

                        inPosition.x += direction.x * t + normal.x;
                        inPosition.y += direction.y * t + normal.y;
                        inPosition.z += direction.z * t + normal.z;
                    }
                }

                db.x = (chunkX + viewSignChunk.x - position.x) * v3.x;
                db.y = (chunkY + viewSignChunk.y - position.y) * v3.y;
                db.z = (chunkZ + viewSignChunk.z - position.z) * v3.z;

                db.x = db.x < 0 ? -db.x : db.x;
                db.y = db.y < 0 ? -db.y : db.y;
                db.z = db.z < 0 ? -db.z : db.z;

                t = db.x;
                normal.x = viewDirSignOffset.x;
                normal.y = 0;
                normal.z = 0;
                if (db.y < t) {
                    t = db.y;
                    normal.x = 0;
                    normal.y = viewDirSignOffset.y;
                }
                if (db.z < t) {
                    t = db.z;
                    normal.x = 0;
                    normal.y = 0;
                    normal.z = viewDirSignOffset.z;
                }

                position.x += direction.x * t + normal.x;
                position.y += direction.y * t + normal.y;
                position.z += direction.z * t + normal.z;

            }
            return null;
        }


        int LineCastFastVoxel (Vector3 startPosition, Vector3 endPosition, VoxelIndex [] indices, int startIndex = 0, byte minOpaque = 0)
        {

#if DEBUG_RAYCAST
                                                GameObject o;
#endif

            // Ray march throuch chunks and fill indices array
            Vector3 position = startPosition;
            VoxelChunk chunk = null;
            float maxDistanceSqr = FastVector.SqrDistanceByValue (startPosition, endPosition);

            Vector3 direction = FastVector.NormalizedDirectionByValue (ref startPosition, ref endPosition);
            Vector3 viewDirSign = FastVector.Sign (ref direction);
            Vector3 viewSign = new Vector3 ((viewDirSign.x + 1f) * 0.5f, (viewDirSign.y + 1f) * 0.5f, (viewDirSign.z + 1f) * 0.5f); // 0 = left, 1 = right

            float vxz, vzy, vxy;
            if (direction.y != 0) {
                float a = direction.x / direction.y;
                float b = direction.z / direction.y;
                vxz = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vxz = 1000000f;
            }
            if (direction.x != 0) {
                float a = direction.z / direction.x;
                float b = direction.y / direction.x;
                vzy = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vzy = 1000000f;
            }
            if (direction.z != 0) {
                float a = direction.x / direction.z;
                float b = direction.y / direction.z;
                vxy = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vxy = 1000000f;
            }

            Vector3 v3 = new Vector3 (vzy, vxz, vxy);
            Vector3 viewSignChunk = viewSign * CHUNK_SIZE;
            Vector3 viewDirSignOffset = viewDirSign * 0.002f;

            int chunkX, chunkY, chunkZ;
            int chunkCount = 0;
            float t;
            Vector3 normal = Misc.vector3zero, db;

            while (chunkCount++ < 500) { // safety counter to avoid any potential infinite loop

                // Check max distance
                float distSqr = (position.x - startPosition.x) * (position.x - startPosition.x) + (position.y - startPosition.y) * (position.y - startPosition.y) + (position.z - startPosition.z) * (position.z - startPosition.z);
                if (distSqr > maxDistanceSqr)
                    break;


#if DEBUG_RAYCAST
                                                                o = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                                                o.transform.localScale = Misc.Vector3one * 0.15f;
                                                                o.transform.position = position;
                                                                DestroyImmediate(o.GetComponent<Collider>());
                                                                o.GetComponent<Renderer>().material.color = Color.blue;
#endif

                FastMath.FloorToInt (position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out chunkX, out chunkY, out chunkZ);

                int x00 = WORLD_SIZE_DEPTH * WORLD_SIZE_HEIGHT * (chunkX + WORLD_SIZE_WIDTH);
                int y00 = WORLD_SIZE_DEPTH * (chunkY + WORLD_SIZE_HEIGHT);
                int hash = x00 + y00 + chunkZ;
                chunk = GetChunkIfExists (hash);

                chunkX *= CHUNK_SIZE;
                chunkY *= CHUNK_SIZE;
                chunkZ *= CHUNK_SIZE;

                if (chunk) {
                    // Ray-march through chunk
                    Voxel [] voxels = chunk.voxels;
                    Vector3 inPosition = position;

                    for (int k = 0; k < 64; k++) {

#if DEBUG_RAYCAST
                                                                                                o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                                                                                                o.transform.localScale = Misc.Vector3one * 0.1f;
                                                                                                o.transform.position = inPosition;
                                                                                                DestroyImmediate(o.GetComponent<Collider>());
                                                                                                o.GetComponent<Renderer>().material.color = Color.yellow;
#endif

                        // Check voxel content
                        int fx, fy, fz;
                        FastMath.FloorToInt (inPosition.x, inPosition.y, inPosition.z, out fx, out fy, out fz);
                        int py = fy - chunkY;
                        int pz = fz - chunkZ;
                        int px = fx - chunkX;
                        if (px < 0 || px >= CHUNK_SIZE || py < 0 || py >= CHUNK_SIZE || pz < 0 || pz >= CHUNK_SIZE) {
                            break;
                        }

                        int voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
                        if (voxels [voxelIndex].hasContent == 1 && (minOpaque == 255 || voxels [voxelIndex].opaque >= minOpaque)) {
                            if (startIndex >= indices.Length) {
                                chunkCount = int.MaxValue;
                                break;
                            }

                            // Check max distance
                            distSqr = (inPosition.x - startPosition.x) * (inPosition.x - startPosition.x) + (inPosition.y - startPosition.y) * (inPosition.y - startPosition.y) + (inPosition.z - startPosition.z) * (inPosition.z - startPosition.z);
                            if (distSqr > maxDistanceSqr) {
                                chunkCount = int.MaxValue;
                                break;
                            }

                            indices [startIndex].chunk = chunk;
                            indices [startIndex].voxelIndex = voxelIndex;
                            indices [startIndex].position = inPosition;
                            startIndex++;
                        }

                        db.x = (fx + viewSign.x - inPosition.x) * v3.x;
                        db.y = (fy + viewSign.y - inPosition.y) * v3.y;
                        db.z = (fz + viewSign.z - inPosition.z) * v3.z;

                        db.x = db.x < 0 ? -db.x : db.x;
                        db.y = db.y < 0 ? -db.y : db.y;
                        db.z = db.z < 0 ? -db.z : db.z;

                        t = db.x;
                        normal.x = viewDirSignOffset.x;
                        normal.y = 0;
                        normal.z = 0;
                        if (db.y < t) {
                            t = db.y;
                            normal.x = 0;
                            normal.y = viewDirSignOffset.y;
                        }
                        if (db.z < t) {
                            t = db.z;
                            normal.x = 0;
                            normal.y = 0;
                            normal.z = viewDirSignOffset.z;
                        }

                        inPosition.x += direction.x * t + normal.x;
                        inPosition.y += direction.y * t + normal.y;
                        inPosition.z += direction.z * t + normal.z;
                    }
                }

                db.x = (chunkX + viewSignChunk.x - position.x) * v3.x;
                db.y = (chunkY + viewSignChunk.y - position.y) * v3.y;
                db.z = (chunkZ + viewSignChunk.z - position.z) * v3.z;

                db.x = db.x < 0 ? -db.x : db.x;
                db.y = db.y < 0 ? -db.y : db.y;
                db.z = db.z < 0 ? -db.z : db.z;

                t = db.x;
                normal.x = viewDirSignOffset.x;
                normal.y = 0;
                normal.z = 0;
                if (db.y < t) {
                    t = db.y;
                    normal.x = 0;
                    normal.y = viewDirSignOffset.y;
                }
                if (db.z < t) {
                    t = db.z;
                    normal.x = 0;
                    normal.y = 0;
                    normal.z = viewDirSignOffset.z;
                }

                position.x += direction.x * t + normal.x;
                position.y += direction.y * t + normal.y;
                position.z += direction.z * t + normal.z;

            }
            return startIndex;
        }



        int LineCastFastChunk (Vector3 startPosition, Vector3 endPosition, VoxelChunk [] chunks, int startIndex = 0)
        {

#if DEBUG_RAYCAST
			GameObject o;
#endif

            // Ray march throuch chunks and fill indices array
            Vector3 position = startPosition;
            VoxelChunk chunk = null;
            float maxDistanceSqr = FastVector.SqrDistanceByValue (startPosition, endPosition);

            Vector3 direction = FastVector.NormalizedDirectionByValue (ref startPosition, ref endPosition);
            Vector3 viewDirSign = FastVector.Sign (ref direction);
            Vector3 viewSign = new Vector3 ((viewDirSign.x + 1f) * 0.5f, (viewDirSign.y + 1f) * 0.5f, (viewDirSign.z + 1f) * 0.5f); // 0 = left, 1 = right

            float vxz, vzy, vxy;
            if (direction.y != 0) {
                float a = direction.x / direction.y;
                float b = direction.z / direction.y;
                vxz = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vxz = 1000000f;
            }
            if (direction.x != 0) {
                float a = direction.z / direction.x;
                float b = direction.y / direction.x;
                vzy = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vzy = 1000000f;
            }
            if (direction.z != 0) {
                float a = direction.x / direction.z;
                float b = direction.y / direction.z;
                vxy = Mathf.Sqrt (1f + a * a + b * b);
            } else {
                vxy = 1000000f;
            }

            Vector3 v3 = new Vector3 (vzy, vxz, vxy);
            Vector3 viewSignChunk = viewSign * CHUNK_SIZE;
            Vector3 viewDirSignOffset = viewDirSign * 0.002f;

            int chunkX, chunkY, chunkZ;
            int chunkCount = 0;
            float t;
            Vector3 normal = Misc.vector3zero, db;

            while (chunkCount++ < 500) { // safety counter to avoid any potential infinite loop

                // Check max distance
                float distSqr = (position.x - startPosition.x) * (position.x - startPosition.x) + (position.y - startPosition.y) * (position.y - startPosition.y) + (position.z - startPosition.z) * (position.z - startPosition.z);
                if (distSqr > maxDistanceSqr)
                    break;

                FastMath.FloorToInt (position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out chunkX, out chunkY, out chunkZ);

                int x00 = WORLD_SIZE_DEPTH * WORLD_SIZE_HEIGHT * (chunkX + WORLD_SIZE_WIDTH);
                int y00 = WORLD_SIZE_DEPTH * (chunkY + WORLD_SIZE_HEIGHT);
                int hash = x00 + y00 + chunkZ;
                chunk = GetChunkIfExists (hash);

                chunkX *= CHUNK_SIZE;
                chunkY *= CHUNK_SIZE;
                chunkZ *= CHUNK_SIZE;

                if (chunk) {
                    if (startIndex >= chunks.Length) {
                        break;
                    }
                    chunks [startIndex++] = chunk;
                }

                db.x = (chunkX + viewSignChunk.x - position.x) * v3.x;
                db.y = (chunkY + viewSignChunk.y - position.y) * v3.y;
                db.z = (chunkZ + viewSignChunk.z - position.z) * v3.z;

                db.x = db.x < 0 ? -db.x : db.x;
                db.y = db.y < 0 ? -db.y : db.y;
                db.z = db.z < 0 ? -db.z : db.z;

                t = db.x;
                normal.x = viewDirSignOffset.x;
                normal.y = 0;
                normal.z = 0;
                if (db.y < t) {
                    t = db.y;
                    normal.x = 0;
                    normal.y = viewDirSignOffset.y;
                }
                if (db.z < t) {
                    t = db.z;
                    normal.x = 0;
                    normal.y = 0;
                    normal.z = viewDirSignOffset.z;
                }

                position.x += direction.x * t + normal.x;
                position.y += direction.y * t + normal.y;
                position.z += direction.z * t + normal.z;

            }
            return startIndex;
        }


        bool HitVoxelFast (Vector3 origin, Vector3 direction, int damage, out VoxelHitInfo hitInfo, float maxDistance = 0, int damageRadius = 1, bool addParticles = true, bool playSound = true, bool allowDamageEvent = true)
        {
            RayCastFast (origin, direction, out hitInfo, maxDistance, false, 0, ColliderTypes.IgnorePlayer);
            VoxelChunk chunk = hitInfo.chunk;
            if (chunk == null || hitInfo.voxelIndex < 0) {
                lastHitInfo.chunk = null;
                lastHitInfo.voxelIndex = -1;
                return false;
            }

            lastHitInfo = hitInfo;
            //DamageVoxelFast (ref hitInfo, damage, addParticles, playSound, allowDamageEvent);
            //if (damageRadius > 1) {
                DamageAreaFast(new Vector3(hitInfo.voxelCenter.x, hitInfo.voxelCenter.y, hitInfo.voxelCenter.z), damage, damageRadius, false, true,null,false,true);
                //Vector3 otherPos;
                //Vector3 explosionPosition = hitInfo.voxelCenter + hitInfo.normal * damageRadius;
                //damageRadius--;

                //for (int y = -damageRadius; y <= damageRadius; y++)
                //{
                //    otherPos.y = lastHitInfo.voxelCenter.y + y;
                //    for (int z = -damageRadius; z <= damageRadius; z++)
                //    {
                //        otherPos.z = lastHitInfo.voxelCenter.z + z;
                //        for (int x = -damageRadius; x <= damageRadius; x++)
                //        {
                //            if (x == 0 && z == 0 && y == 0)
                //                continue;
                //            VoxelChunk otherChunk;
                //            int otherIndex;
                //            otherPos.x = lastHitInfo.voxelCenter.x + x;
                //            if (GetVoxelIndex(otherPos, out otherChunk, out otherIndex, false))
                //            {
                //                if (GetVoxelVisibility(otherChunk, otherIndex))
                //                {
                //                    FastVector.NormalizedDirection(ref explosionPosition, ref otherPos, out direction);
                //                    if (RayCast(explosionPosition, direction, out hitInfo))
                //                    {
                //                        DamageVoxelFast(ref hitInfo, damage, addParticles, playSound, allowDamageEvent);
                //                        Debug.DrawLine(explosionPosition, hitInfo.voxelCenter, Color.red);
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}
            //}

            return true;
        }


        int DamageAreaFast(Vector3 origin, int damage, int damageRadius = 1, bool distanceAttenuation = true, bool addParticles = true, List<VoxelIndex> results = null, bool playSound = false, bool showDamageCracks = false) {
            if (OnVoxelAfterAreaDamage != null) {
                if (results == null) {
                    results = tempVoxelIndices2;
                }
            }

            bool hasResults = results != null;
            if (hasResults) {
                results.Clear ();
            }
            if (damageRadius < 0 || damage < 1) {
                return 0;
            }

            int damagedCount = 0;
            VoxelHitInfo hitInfo = new VoxelHitInfo();
            GetVoxelIndices (origin, damageRadius, tempVoxelIndices);
            if (OnVoxelBeforeAreaDamage != null) {
                OnVoxelBeforeAreaDamage (tempVoxelIndices);
            }
            int count = tempVoxelIndices.Count;

            float damageRadiusSqr = damageRadius * damageRadius;
            destroyedVoxelParticlesAmount = 5;
            for (int k = 0; k < count; k++) {
              
                VoxelIndex vi = tempVoxelIndices[k];
                VoxelChunk otherChunk = vi.chunk;
                int otherIndex = vi.voxelIndex;
                int dam = damage;
                if (distanceAttenuation && vi.sqrDistance > 1) {
                    float atten = (damageRadiusSqr - vi.sqrDistance) / damageRadiusSqr;
                    dam = (int)(damage * atten);
                    //destroyedVoxelParticlesAmount = (int)(DEFAULT_DESTROYED_VOXEL_PARTICLE_AMOUNT * atten);
                }
                if (dam > 0) {
                    // Approximates a theorical hit point
                    FastVector.NormalizedDirection(ref vi.position, ref origin, out hitInfo.normal);
                    hitInfo.chunk = otherChunk;
                    hitInfo.voxelIndex = otherIndex;
                    hitInfo.voxel = otherChunk.voxels[otherIndex];
                    hitInfo.voxelCenter = GetVoxelPosition(otherChunk, otherIndex);
                    hitInfo.sqrDistance = vi.sqrDistance;
                    hitInfo.point = hitInfo.voxelCenter + hitInfo.normal * 0.5f;
                    
                    int damageTaken = DamageVoxelFast(ref hitInfo, dam, addParticles, playSound, true, showDamageCracks);
                    if (hasResults) {
                        VoxelIndex di = vi;
                        di.damageTaken = damageTaken;
                        results.Add(di);
                    }
                    damagedCount++;
                }
            }
            if (OnVoxelAfterAreaDamage != null) {
                OnVoxelAfterAreaDamage (results);
            }
            destroyedVoxelParticlesAmount = DEFAULT_DESTROYED_VOXEL_PARTICLE_AMOUNT;
            return damagedCount;
        }
        
        /// <summary>
        /// Performs the voxel damage.
        /// </summary>
        /// <returns>The actual damage taken by the voxe.</returns>
        /// <param name="hitInfo">Hit info.</param>
        /// <param name="damage">Damage.</param>
        /// <param name="addParticles">If set to <c>true</c> add particles.</param>
        int DamageVoxelFast(ref VoxelHitInfo hitInfo, int damage, bool addParticles, bool playSound, bool allowDamageEvent = true, bool showDamageCracks = true) {

            VoxelChunk chunk = hitInfo.chunk;
            if (hitInfo.voxel.typeIndex == 0 || hitInfo.voxelIndex < 0)
                return 0;
            lastHitInfo = hitInfo;
            VoxelDefinition voxelType = hitInfo.voxel.type;
            byte voxelTypeResistancePoints = voxelType.resistancePoints;

            if (buildMode) {
                if (damage > 0) {
                    damage = 255;
                }
            } else {
                if (voxelTypeResistancePoints == (byte)0) {
                    damage = 0;
                } else if (voxelTypeResistancePoints == (byte)255) {
                    if (playSound) {
                        PlayImpactSound (hitInfo.voxel.type.impactSound, hitInfo.voxelCenter);
                    }
                    damage = 0;
                }
            }

            if (allowDamageEvent && OnVoxelDamaged != null) {
                OnVoxelDamaged (chunk, hitInfo.voxelIndex, ref damage);
            }

            if (damage == 0)
                return 0;

            // Gets ambient light near surface
            float voxelLight = GetVoxelLight (hitInfo.point + hitInfo.normal * 0.5f);

            // Get voxel damage indicator GO's name
            bool destroyed = voxelType.renderType == RenderType.CutoutCross || voxelType.resistancePoints <= damage;
            int resistancePointsLeft = 0;
            VoxelPlaceholder placeholder = null;
            if (!destroyed) {
                placeholder = GetVoxelPlaceholder (chunk, hitInfo.voxelIndex, true);
                resistancePointsLeft = placeholder.resistancePointsLeft - damage;
                if (resistancePointsLeft < 0) {
                    resistancePointsLeft = 0;
                    destroyed = true;
                }
                placeholder.resistancePointsLeft = resistancePointsLeft;
            }

            if (voxelType.renderType == RenderType.Empty)
                addParticles = false;

            int particlesAmount;
            if (destroyed) {

                // Add recoverable voxel on the scene (not for vegetation)
                if (voxelType.renderType != RenderType.Empty && voxelType.renderType != RenderType.CutoutCross && voxelType.canBeCollected && !buildMode) {
                    bool create = true;

                    if (OnVoxelBeforeDropItem != null) {
                        OnVoxelBeforeDropItem (chunk, hitInfo, out create);
                    }
                    if (create) {
                        CreateRecoverableVoxel (hitInfo.voxelCenter, voxelDefinitions [hitInfo.voxel.typeIndex], hitInfo.voxel.color);
                    }
                }

                // Destroy the voxel
                VoxelDestroyFast (chunk, hitInfo.voxelIndex);

                // Check if grass is on top and remove it as well
                VoxelChunk topChunk;
                int topIndex;
                if (GetVoxelIndex (hitInfo.voxelCenter + Misc.vector3up, out topChunk, out topIndex, false)) {
                    if (topChunk.voxels [topIndex].typeIndex != 0 && voxelDefinitions [topChunk.voxels [topIndex].typeIndex].renderType == RenderType.CutoutCross) {
                        byte light = topChunk.voxels [topIndex].lightMeshOrTorch;
                        topChunk.voxels [topIndex].Clear (light);
                        topChunk.modified = true;
                    }
                }

                // Max particles
                particlesAmount = destroyedVoxelParticlesAmount;

                if (playSound) {
                    PlayDestructionSound (voxelDefinitions [hitInfo.voxel.typeIndex].destructionSound, hitInfo.voxelCenter);
                }
            } else {
                // Add damage indicator
                float lifePerc = (float)resistancePointsLeft / voxelTypeResistancePoints;
                int textureIndex = FastMath.FloorToInt(world.voxelDamageTextures.Length * lifePerc);

                if (showDamageCracks) {
                if (placeholder.damageIndicator == null) {
                    if (damagedVoxelPrefab == null) {
                        damagedVoxelPrefab = Resources.Load<GameObject> ("VoxelPlay/Prefabs/DamagedVoxel");
                    }
                    GameObject g = Instantiate<GameObject> (damagedVoxelPrefab);
                    g.name = DAMAGE_INDICATOR;
                    Transform tDamageIndicator = g.transform;
                    placeholder.damageIndicator = tDamageIndicator.GetComponent<Renderer> ();
                    tDamageIndicator.SetParent (placeholder.transform, false);
                    tDamageIndicator.localPosition = placeholder.bounds.center;
                    tDamageIndicator.localScale = placeholder.bounds.size * 1.001f;
                }

                if (world.voxelDamageTextures.Length > 0) {
                    if (textureIndex >= world.voxelDamageTextures.Length) {
                        textureIndex = world.voxelDamageTextures.Length - 1;
                    }
                    Material mi = placeholder.damageIndicatorMaterial; // gets a copy of material the first time it's used
                    mi.mainTexture = world.voxelDamageTextures [textureIndex];
                    mi.SetFloat ("_VoxelLight", voxelLight);
                    placeholder.damageIndicator.enabled = true;
                    }
                }

                // Particle amount depending of damage
                particlesAmount = (6 - (int)(5f * lifePerc)) + 3;

                // Sets health recovery for the voxel
                placeholder.StartHealthRecovery (chunk, hitInfo.voxelIndex, world.damageDuration);

                if (playSound) {
                    PlayImpactSound (voxelDefinitions [hitInfo.voxel.typeIndex].impactSound, hitInfo.voxelCenter);
                }
            }

            // Add random particles
            if (addParticles) {
                Vector3 camForward = cameraMain.transform.forward;
                float now = Time.time;
                for (int k = 0; k < particlesAmount; k++) {
                    int ppeIndex = GetParticleFromPool ();
                    if (ppeIndex < 0)
                        continue;

                    // Scale of particle
                    Renderer particleRenderer = particlePool [ppeIndex].renderer;
                    float startScale, endScale;
                    if (destroyed) {
                        if (voxelType.renderType == RenderType.CutoutCross) {   // smaller particles for vegetation
                            float rnd = Random.Range (0.03f, 0.04f);
                            startScale = rnd;;
                            endScale = rnd * 0.6f;
                        } else {
                            float rnd = Random.Range (0.04f, 0.1f);
                            startScale = rnd * 4f;
                            endScale = rnd;
                        }
                    } else {
                        float rnd = Random.Range (0.03f, 0.06f);
                        startScale = endScale = rnd;
                    }
                    particleRenderer.transform.localScale = new Vector3(startScale, startScale, startScale);
                    particlePool[ppeIndex].startScale = startScale;
                    particlePool[ppeIndex].endScale = endScale;

                    // Set particle texture
                    Material instanceMat = particleRenderer.sharedMaterial;
                    SetParticleMaterialTextures (instanceMat, voxelDefinitions [hitInfo.voxel.typeIndex], hitInfo.voxel.color);
                    instanceMat.mainTextureOffset = new Vector2 (Random.value, Random.value);
                    instanceMat.mainTextureScale = new Vector2 (0.05f, 0.05f);
                    instanceMat.SetFloat ("_VoxelLight", voxelLight);
                    instanceMat.SetFloat ("_FlashDelay", 0);

                    // Set position
                    Rigidbody rb = particlePool [ppeIndex].rigidBody;
                    Vector3 particlePos;
                    if (destroyed) {
                        Vector3 expelDir = Random.insideUnitSphere;
                        particlePos = hitInfo.voxelCenter;
                        FastVector.Add (ref particlePos, ref expelDir, 0.6f);
                        float rnd = Random.value * 125f;
                        rb.AddForce (expelDir * rnd);
                    } else {
                        particlePos = hitInfo.point;
                        Vector3 v1 = new Vector3 (-hitInfo.normal.y, hitInfo.normal.z, hitInfo.normal.x);
                        Vector3 v2 = new Vector3 (-hitInfo.normal.z, hitInfo.normal.x, hitInfo.normal.y);
                        Vector3 dx = (Random.value - 0.5f) * 0.7f * v1;
                        Vector3 dy = (Random.value - 0.5f) * 0.7f * v2;
                        particlePos += hitInfo.normal * 0.001f + dx + dy;
                        rb.AddForce (camForward * (Random.value * -125f));
                        rb.AddForce (new Vector3 (0, 25f, 0));
                    }
                    rb.AddTorque (Random.onUnitSphere * 100f);
                    rb.useGravity = true;

                    // Self-destruct
                    particlePool [ppeIndex].creationTime = now;
                    particlePool [ppeIndex].destructionTime = now + 2.5f + Random.value;

                    // Anotate particle voxel so light is not recalculated this frame
                    particleRenderer.transform.position = particlePos;
                    particlePool [ppeIndex].lastX = (int)particlePos.x;
                    particlePool [ppeIndex].lastY = (int)particlePos.y;
                    particlePool [ppeIndex].lastZ = (int)particlePos.z;
                }
            }

            return damage;
        }

        /// <summary>
        /// Plays impact sound at position.
        /// </summary>
        /// <param name="sound">Custom audioclip or pass null to use default impact sound defined in Voxel Play Environment component.</param>
        void PlayImpactSound (AudioClip sound, Vector3 position)
        {
            if (sound == null)
                sound = defaultImpactSound;
            if (sound != null) {
                AudioSource.PlayClipAtPoint (sound, position);
            }
        }

        /// <summary>
        /// Plays voxel build sound at position
        /// </summary>
        /// <param name="sound">Custom audioclip or pass null to use default build sound defined in Voxel Play Environment component.</param>
        void PlayBuildSound (AudioClip sound, Vector3 position)
        {
            if (sound == null)
                sound = defaultBuildSound;
            if (sound != null) {
                AudioSource.PlayClipAtPoint (sound, position);
            }
        }

        /// <summary>
        /// Plays voxel destruction sound at position
        /// </summary>
        /// <param name="sound">Custom audioclip or pass null to use default destruction sound defined in Voxel Play Environment component.</param>
        void PlayDestructionSound (AudioClip sound, Vector3 position)
        {
            if (sound == null)
                sound = defaultDestructionSound;
            if (sound != null) {
                AudioSource.PlayClipAtPoint (sound, position);
            }
        }


        /// <summary>
        /// Checks if there's a voxel at given position.
        /// </summary>
        /// <returns><c>true</c>, if collision was checked, <c>false</c> otherwise.</returns>
        /// <param name="position">Position.</param>
        public bool CheckCollision (Vector3 position)
        {
            int x, y, z;
            FastMath.FloorToInt (position.x / CHUNK_SIZE, position.y / CHUNK_SIZE, position.z / CHUNK_SIZE, out x, out y, out z);
            VoxelChunk chunk = GetChunkOrCreate (x, y, z);
            if (chunk != null) {
                Voxel [] voxels = chunk.voxels;
                int py = (int)(position.y - y * CHUNK_SIZE);
                int pz = (int)(position.z - z * CHUNK_SIZE);
                int px = (int)(position.x - x * CHUNK_SIZE);
                int voxelIndex = py * ONE_Y_ROW + pz * ONE_Z_ROW + px;
                bool collision = voxels [voxelIndex].hasContent == 1 && voxelDefinitions [voxels [voxelIndex].typeIndex].navigatable;
                return collision;
            }
            return false;
        }


    }



}
