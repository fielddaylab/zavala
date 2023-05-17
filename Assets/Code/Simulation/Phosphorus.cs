using System;
using BeauUtil;

namespace Zavala.Sim {

    /// <summary>
    /// Data for each tile indicating how phosphorus flows.
    /// </summary>
    public struct PhosphorusTileInfo {
        public TileAdjacencyMask FlowMask; // valid flow directions
        public TileAdjacencyMask SteepMask; // which directions are marked as steep (distribution weighted towards them)
        public ushort RegionId; // region identifier. used as a mask for sim updates (e.g. update region 1, update region 2, etc)
        public ushort Flags; // copy of tile masks
        public ushort Height; // copy of tile height
    }

    /// <summary>
    /// State for an individual tile.
    /// </summary>
    public struct PhosphorusTileState {
        public ushort Count;
    }

    public struct PhosphorusTileTransfer {
        public int StartIdx;
        public int EndIdx;
        public ushort Transfer;
    }

    static public class PhosphorusSim {
        #region Tunable Parameters

        // height calculations
        static public int SimilarHeightThreshold = 20;
        static public int SteepHeightThreshold = 100;

        // flow
        static public float RemainAtSourceProportion = 0.3f;
        static public float MinFlowProportion = 0.5f;
        static public float MaxFlowProportionSteep = 1.3f;

        #endregion // Tunable Parameters

        // delegate for extracting height different from one tile to another
        static private readonly Tile.TileDataMapDelegate<PhosphorusTileInfo, short> ExtractHeightDifference = (in PhosphorusTileInfo c, in PhosphorusTileInfo a, out short o) => {
            if (a.Height < ushort.MaxValue && a.Height < c.Height + SimilarHeightThreshold) {
                o = (short) (a.Height - c.Height);
                return true;
            }

            o = 0;
            return false;
        };

        static public unsafe void EvaluateFlowField(PhosphorusTileInfo* infoBuffer, int tileCount, in HexGridSize gridSize) {
            for(int i = 0; i < tileCount; i++) {
                TileAdjacencyDataSet<short> heightDifferences = Tile.GatherAdjacencySet<PhosphorusTileInfo, short>(i, infoBuffer, tileCount, gridSize, ExtractHeightDifference);

                ref PhosphorusTileInfo info = ref infoBuffer[i];
                TileAdjacencyMask dropMask = default;
                TileAdjacencyMask steepMask = default;
                foreach(var kv in heightDifferences) {
                    if (kv.Value < -SteepHeightThreshold) {
                        dropMask |= kv.Key;
                        steepMask |= kv.Key;
                    } else if (kv.Value < -SimilarHeightThreshold) {
                        dropMask |= kv.Key;
                    }
                }

                if (dropMask.IsEmpty) {
                    info.SteepMask.Clear();
                    info.FlowMask = ~dropMask;
                    info.FlowMask &= ~TileDirection.Self;
                } else {
                    info.SteepMask = steepMask;
                    info.FlowMask = dropMask;
                }
            }
        }

        static public unsafe void Tick(PhosphorusTileInfo* infoBuffer, PhosphorusTileState* stateBuffer, PhosphorusTileState* targetStateBuffer, int tileCount, in HexGridSize gridSize, Random random) {
            // copy current state over
            for(int i = 0; i < tileCount; i++) {
                targetStateBuffer[i] = stateBuffer[i];
            }

            TileDirection* directionOrder = stackalloc TileDirection[6];
            int directionCount = 0;

            // now we'll do the actual processing
            for(int i = 0; i < tileCount; i++) {
                ref PhosphorusTileState currentState = ref stateBuffer[i];
                PhosphorusTileInfo tileInfo = infoBuffer[i];
                if (currentState.Count == 0 || tileInfo.FlowMask.IsEmpty) {
                    continue;
                }

                directionCount = 0;
                foreach(var dir in tileInfo.FlowMask) {
                    directionOrder[directionCount++] = dir;
                }

                // TODO: Randomize the order we review this

                TileAdjacencyMask steepMask = tileInfo.SteepMask;
                int transferRemaining = (int) (currentState.Count * (1f - RemainAtSourceProportion));
                int perDirection = transferRemaining / directionCount;
                for(int dirIdx = 0; dirIdx < directionCount && transferRemaining > 0; dirIdx++) {
                    TileDirection dir = directionOrder[dirIdx];
                    int queuedTransfer;
                    if (dirIdx == directionCount - 1) {
                        queuedTransfer = transferRemaining;
                    } else {
                        queuedTransfer = Math.Min((int) (perDirection * random.NextFloat(MinFlowProportion, steepMask[dir] ? MaxFlowProportionSteep : 1)), transferRemaining);
                    }
                    targetStateBuffer[i].Count -= (ushort) queuedTransfer;
                    targetStateBuffer[gridSize.OffsetIndexFrom(i, dir)].Count += (ushort) queuedTransfer;
                    transferRemaining -= queuedTransfer;
                }
            }
        }
    }
}