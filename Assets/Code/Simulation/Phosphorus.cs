using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Zavala.Sim {

    #region State Tracking

    /// <summary>
    /// Data for each tile indicating how phosphorus flows.
    /// </summary>
    public struct PhosphorusTileInfo {
        public TileAdjacencyMask FlowMask; // valid flow directions
        public TileAdjacencyMask SteepMask; // which directions are marked as steep (distribution weighted towards them)
        public ushort RegionIndex; // region identifier. used as a mask for sim updates (e.g. update region 1, update region 2, etc)
        public ushort Flags; // copy of tile flags
        public ushort Height; // copy of tile height
    }

    /// <summary>
    /// State for an individual tile.
    /// </summary>
    public struct PhosphorusTileState {
        public ushort Count;
    }


    #endregion // State Tracking

    #region Recording Changes

    /// <summary>
    /// Data recording a transfer of phosphorus from one tile to another.
    /// </summary>
    public struct PhosphorusTileTransfer {
        public int StartIdx;
        public int EndIdx;
        public ushort StartRegionIndex;
        public ushort EndRegionIndex;
        public ushort Transfer;

        public override string ToString() {
            return string.Format("[{0}]: {1} to [{2}]", StartIdx, Transfer, EndIdx);
        }

        public sealed class Sorter : IComparer<PhosphorusTileTransfer> {
            public int Compare(PhosphorusTileTransfer x, PhosphorusTileTransfer y) {
                int regionComp = x.StartRegionIndex - y.StartRegionIndex;
                return regionComp == 0 ? x.StartIdx - y.StartIdx : regionComp;
            }

            static public readonly Sorter Instance = new Sorter();
        }
    }

    /// <summary>
    /// Data recording an addition or removal of phosphorus to/from one tile.
    /// </summary>
    public struct PhosphorusTileAddRemove {
        public int TileIdx;
        public ushort RegionIndex;
        public ushort Amount;

        public override string ToString() {
            return string.Format("[{0}]: {1}", TileIdx, Amount);
        }

        public sealed class Sorter : IComparer<PhosphorusTileAddRemove> {
            public int Compare(PhosphorusTileAddRemove x, PhosphorusTileAddRemove y) {
                int regionComp = x.RegionIndex - y.RegionIndex;
                return regionComp == 0 ? x.TileIdx - y.TileIdx : regionComp;
            }

            static public readonly Sorter Instance = new Sorter();
        }
    }

    /// <summary>
    /// Buffer containing all phosphorus changes.
    /// </summary>
    public struct PhosphorusChangeBuffer {
        public RingBuffer<PhosphorusTileTransfer> Transfers;
        public RingBuffer<PhosphorusTileAddRemove> Add;
        public RingBuffer<PhosphorusTileAddRemove> Remove;
        public HashSet<ushort> AffectedRegions;
        public HashSet<int> AffectedTiles;

        public void Create() {
            Transfers = new RingBuffer<PhosphorusTileTransfer>(64, RingBufferMode.Expand);
            Add = new RingBuffer<PhosphorusTileAddRemove>(8, RingBufferMode.Expand);
            Remove = new RingBuffer<PhosphorusTileAddRemove>(8, RingBufferMode.Expand);
            AffectedRegions = new HashSet<ushort>(RegionInfo.MaxRegions);
            AffectedTiles = new HashSet<int>(200);
        }

        public void PushTransfer(PhosphorusTileTransfer transfer) {
            if (transfer.Transfer > 0) {
                Transfers.PushBack(transfer);
                AffectedRegions.Add(transfer.StartRegionIndex);
                AffectedRegions.Add(transfer.EndRegionIndex);
                AffectedTiles.Add(transfer.StartIdx);
                AffectedTiles.Add(transfer.EndIdx);
            }
        }

        public void PushAdd(PhosphorusTileAddRemove add) {
            if (add.Amount > 0) {
                Add.PushBack(add);
                AffectedRegions.Add(add.RegionIndex);
                AffectedTiles.Add(add.TileIdx);
            }
        }

        public void PushRemove(PhosphorusTileAddRemove remove) {
            if (remove.Amount > 0) {
                Remove.PushBack(remove);
                AffectedRegions.Add(remove.RegionIndex);
                AffectedTiles.Add(remove.TileIdx);
            }
        }

        public void Clear() {
            Transfers.Clear();
            Add.Clear();
            Remove.Clear();
            AffectedRegions.Clear();
            AffectedTiles.Clear();
        }
    }

    #endregion // Recording Changes

    /// <summary>
    /// Phosphorus simulation logic.
    /// </summary>
    static public class PhosphorusSim {
        #region Tunable Parameters

        // height calculations
        [ConfigIntVar("Similar Height Threshold", 0, 100, 25)] static public int SimilarHeightThreshold = 50;
        [ConfigIntVar("Steep Height Threshold", 0, 500, 25)] static public int SteepHeightThreshold = 300;

        // flow
        [ConfigIntVar("Minimum Phosphorus for Flow", 0, 10, 1)] static public int MinFlowThreshold = 5;
        [ConfigFloatVar("Phosphorus Remain at Source", 0, 1, 0.1f)] static public float RemainAtSourceProportion = 0.6f;
        [ConfigFloatVar("Minimum Flow Proportion", 0, 1, 0.1f)] static public float MinFlowProportion = 0.5f;
        [ConfigFloatVar("Steep Flow Proportion", 0, 1, 0.1f)] static public float MaxFlowProportionSteep = 1.3f;
        [ConfigIntVar("Tile Saturation Threshold", 8, 32, 4)] static public int TileSaturationThreshold = 32;

        #endregion // Tunable Parameters

        public const int MaxPhosphorusPerTile = 64;

        private struct TileFlowData {
            public short HeightDiff;
            public bool IsWater;
        }

        // delegate for extracting height different from one tile to another
        static private readonly Tile.TileDataMapDelegate<PhosphorusTileInfo, TileFlowData> ExtractHeightDifference = (in PhosphorusTileInfo c, in PhosphorusTileInfo a, out TileFlowData o) => {
            if (a.Height < ushort.MaxValue && a.Height < c.Height + SimilarHeightThreshold) {
                o.HeightDiff = (short) (a.Height - c.Height);
                o.IsWater = (a.Flags & (ushort) TerrainFlags.IsWater) != 0;
                return true;
            }

            o.HeightDiff = 0;
            o.IsWater = false;
            return false;
        };

        // delegate for mapping terrain info to phosphorus info
        static private readonly SimBufferMapDelegate<TerrainTileInfo, PhosphorusTileInfo> ExtractPhosphorusTileInfo = (in TerrainTileInfo info) => {
            return new PhosphorusTileInfo() {
                Flags = (ushort) info.Flags,
                Height = info.Height,
                RegionIndex = info.RegionIndex,
            };
        };

        /// <summary>
        /// Extracts phosphorus tile information from the terrain tile information.
        /// </summary>
        static public unsafe void ExtractPhosphorusTileInfoFromTerrain(SimBuffer<TerrainTileInfo> terrainBuffer, SimBuffer<PhosphorusTileInfo> phosphorusTileBuffer) {
            SimBuffer.Map(terrainBuffer, phosphorusTileBuffer, ExtractPhosphorusTileInfo);
        }

        /// <summary>
        /// Evaluates the flow masks for the given buffer.
        /// </summary>
        static public unsafe void EvaluateFlowField(SimBuffer<PhosphorusTileInfo> infoBuffer, in HexGridSize gridSize) {
            foreach(var index in gridSize) {
                EvaluateFlowField_Step(index, infoBuffer, gridSize);
            }
        }

        /// <summary>
        /// Evaluates the flow masks for the given buffer.
        /// </summary>
        static public unsafe void EvaluateFlowField(SimBuffer<PhosphorusTileInfo> infoBuffer, in HexGridSize gridSize, in HexGridSubregion subRegion) {
            foreach(var index in subRegion) {
                EvaluateFlowField_Step(index, infoBuffer, gridSize);
            }
        }

        // internal evaluation
        static private unsafe void EvaluateFlowField_Step(int index, SimBuffer<PhosphorusTileInfo> infoBuffer, in HexGridSize gridSize) {
            TileAdjacencyDataSet<TileFlowData> heightDifferences = Tile.GatherAdjacencySet<PhosphorusTileInfo, TileFlowData>(index, infoBuffer, gridSize, ExtractHeightDifference);

            ref PhosphorusTileInfo info = ref infoBuffer[index];
            TileAdjacencyMask dropMask = default;
            TileAdjacencyMask steepMask = default;
            TileAdjacencyMask waterMask = default;

            bool isWater = (info.Flags & (ushort) TerrainFlags.IsWater) != 0;
            foreach(var kv in heightDifferences) {
                // water always tries to flow into water
                if (isWater && kv.Value.IsWater) {
                    waterMask |= kv.Key;
                }
                
                // otherwise we just go by height
                if (kv.Value.HeightDiff < -SteepHeightThreshold) {
                    dropMask |= kv.Key;
                    steepMask |= kv.Key;
                } else if (kv.Value.HeightDiff < -SimilarHeightThreshold) {
                    dropMask |= kv.Key;
                }
            }

            if (!waterMask.IsEmpty) {
                info.SteepMask.Clear();
                info.FlowMask = waterMask;
            } else if (dropMask.IsEmpty) {
                info.SteepMask.Clear();
                info.FlowMask = heightDifferences.Mask;
            } else {
                info.SteepMask = steepMask;
                info.FlowMask = dropMask;
            }
        }

        /// <summary>
        /// Runs phosphorus flow simulation.
        /// </summary>
        static public unsafe void Tick(SimBuffer<PhosphorusTileInfo> infoBuffer, SimBuffer<PhosphorusTileState> stateBuffer, SimBuffer<PhosphorusTileState> targetStateBuffer, in HexGridSize gridSize, System.Random random, PhosphorusChangeBuffer stateChangeBuffer) {
            // copy current state over
            SimBuffer.Copy(stateBuffer, targetStateBuffer);

            TileDirection* directionOrder = stackalloc TileDirection[6];
            foreach(var index in gridSize) {
                Tick_Step(index, infoBuffer, stateBuffer, targetStateBuffer, gridSize, Tile.InvalidIndex16, 0, random, directionOrder, stateChangeBuffer);
            }
        }

        /// <summary>
        /// Runs phosphorus flow simulation.
        /// </summary>
        static public unsafe void Tick(SimBuffer<PhosphorusTileInfo> infoBuffer, SimBuffer<PhosphorusTileState> stateBuffer, SimBuffer<PhosphorusTileState> targetStateBuffer, in HexGridSize gridSize, in HexGridSubregion subRegion, ushort regionIdx, ushort flagMask, System.Random random, PhosphorusChangeBuffer stateChangeBuffer) {
            TileDirection* directionOrder = stackalloc TileDirection[6];
            foreach(var index in subRegion) {
                Tick_Step(index, infoBuffer, stateBuffer, targetStateBuffer, gridSize, regionIdx, flagMask, random, directionOrder, stateChangeBuffer);
            }
        }
    
        static private unsafe void Tick_Step(int index, SimBuffer<PhosphorusTileInfo> infoBuffer, SimBuffer<PhosphorusTileState> stateBuffer, SimBuffer<PhosphorusTileState> targetStateBuffer, in HexGridSize gridSize, ushort regionIdx, ushort flagMask, System.Random random, TileDirection* directionBuffer, PhosphorusChangeBuffer stateChangeBuffer) {
            ref PhosphorusTileState currentState = ref stateBuffer[index];
            PhosphorusTileInfo tileInfo = infoBuffer[index];

            if ((regionIdx != Tile.InvalidIndex16 && tileInfo.RegionIndex != regionIdx) || (tileInfo.Flags & flagMask) != flagMask) {
                return;
            }
            if (tileInfo.FlowMask.IsEmpty || currentState.Count <= MinFlowThreshold) {
                return;
            }

            float remainProportion = RemainAtSourceProportion;
            bool isWater = false;

            // more flows through water - preserve fewer phosphorus at current tile
            if ((tileInfo.Flags & (ushort) TerrainFlags.IsWater) != 0) {
                remainProportion *= remainProportion;
                isWater = true;
            }

            int transferRemaining = (int) ((currentState.Count - MinFlowThreshold) * (1f - remainProportion));

            if (transferRemaining <= 0) {
                return;
            }

            int directionCount = 0;
            foreach(var dir in tileInfo.FlowMask) {
                directionBuffer[directionCount++] = dir;
            }
            UnsafeExt.Shuffle(directionBuffer, directionCount, random);

            TileAdjacencyMask steepMask = tileInfo.SteepMask;
            int perDirection = (int) Math.Ceiling((float) transferRemaining / directionCount);
            for(int dirIdx = 0; dirIdx < directionCount && transferRemaining > 0; dirIdx++) {
                TileDirection dir = directionBuffer[dirIdx];
                int targetIdx = gridSize.OffsetIndexFrom(index, dir);
                int queuedTransfer;
                if (isWater) {
                    // transfer is based on relative phosphorus count, leads to diffusion
                    queuedTransfer = Math.Min((int) Math.Ceiling((float) (currentState.Count - targetStateBuffer[targetIdx].Count) / currentState.Count) * perDirection, transferRemaining);
                } else {
                    if (dirIdx == directionCount - 1) {
                        queuedTransfer = transferRemaining;
                    } else {
                        // transfer is based on steepness + randomization
                        queuedTransfer = Math.Min((int) Math.Ceiling(perDirection * random.NextFloat(MinFlowProportion, steepMask[dir] ? MaxFlowProportionSteep : 1)), transferRemaining);
                    }
                }
                queuedTransfer = Math.Min(queuedTransfer, TileSaturationThreshold - targetStateBuffer[targetIdx].Count);
                if (queuedTransfer > 0) {
                    targetStateBuffer[index].Count -= (ushort) queuedTransfer;
                    targetStateBuffer[targetIdx].Count += (ushort) queuedTransfer;
                    transferRemaining -= queuedTransfer;
                    PhosphorusTileTransfer tileTransfer = new PhosphorusTileTransfer() {
                        StartIdx = index,
                        EndIdx = targetIdx,
                        StartRegionIndex = tileInfo.RegionIndex,
                        EndRegionIndex = infoBuffer[targetIdx].RegionIndex,
                        Transfer = (ushort) queuedTransfer
                    };
                    stateChangeBuffer.PushTransfer(tileTransfer);
                }
            }
        }
    }
}