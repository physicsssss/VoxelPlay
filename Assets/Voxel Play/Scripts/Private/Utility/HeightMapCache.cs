using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace VoxelPlay {

    public class HeightMapCache {
        const int HEIGHTMAP_POOL_SIZE = 25;
        class HeightMapInfoPoolEntry {
            public int uses;
            public int key;
            public HeightMapInfo[] heights;
        }
        HeightMapInfoPoolEntry[] sectorsPool;

        FastHashSet<int> sectorsDict;
        int lastKey;
        int lastSector;

        public HeightMapCache(int poolSize) {
            sectorsDict = new FastHashSet<int>(16);
            sectorsPool = new HeightMapInfoPoolEntry[poolSize];
            for (int k = 0; k < sectorsPool.Length; k++) {
                sectorsPool[k] = new HeightMapInfoPoolEntry();
            }
            lastSector = -1;
        }

        public void Clear() {
            sectorsDict.Clear();
            for (int k = 0; k < sectorsPool.Length; k++) {
                sectorsPool[k].key = 0;
                sectorsPool[k].uses = 0;
            }
            lastKey = 0;
            lastSector = -1;
        }

        public bool TryGetValue(int x, int z, out HeightMapInfo[] heights, out int heightIndex) {

            int fx = x >> 7;
            int fz = z >> 7;
            heightIndex = ((z - (fz << 7)) << 7) + (x - (fx << 7));
            int key = ((fz + 1024) << 16) + (fx + 1024);
            if (key != lastKey || lastSector < 0) {
                int poolIndex;
                if (!sectorsDict.TryGetValue(key, out poolIndex) || key != sectorsPool[poolIndex].key) {
                    int leastUsed = int.MaxValue;
                    for (int k = 0; k < sectorsPool.Length; k++) {
                        if (sectorsPool[k].uses < leastUsed) {
                            leastUsed = sectorsPool[k].uses;
                            poolIndex = k;
                        }
                    }

                    // free entry from dictionary
                    HeightMapInfoPoolEntry sector = sectorsPool[poolIndex];
                    if (sector.key > 0) {
                        sectorsDict.Remove(sector.key);
                    }

                    // set new key and add to dictionary
                    sector.key = key;
                    sector.uses = 0;
                    sectorsDict[key] = poolIndex;

                    // alloc buffer if it's the first time
                    if (sector.heights == null) {
                        sector.heights = new HeightMapInfo[16384];
                    } else {
                        for (int k=0;k<sector.heights.Length;k++) {
                            sector.heights[k].biome = null;
                            sector.heights[k].moisture = 0;
                            sector.heights[k].groundLevel = 0;
                        }
                    }
                }
                lastKey = key;
                lastSector = poolIndex;
            }

            HeightMapInfoPoolEntry theSector = sectorsPool[lastSector];
            theSector.uses++;
            heights = theSector.heights;
            return heights[heightIndex].groundLevel != 0;
        }

    }

}