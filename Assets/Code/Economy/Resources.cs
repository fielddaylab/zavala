using System;
using System.Runtime.CompilerServices;
using BeauPools;
using BeauUtil;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;

namespace Zavala.Economy {
    /// <summary>
    /// Resource identifier.
    /// </summary>
    [LabeledEnum]
    public enum ResourceId : ushort {
        Manure,
        MFertilizer,
        DFertilizer,
        Grain,
        Milk,

        [Hidden]
        COUNT
    }

    /// <summary>
    /// Resource mask.
    /// </summary>
    [Flags]
    public enum ResourceMask : uint {
        Manure = 1 << ResourceId.Manure,
        MFertilizer = 1 << ResourceId.MFertilizer,
        DFertilizer = 1 << ResourceId.DFertilizer,
        Grain = 1 << ResourceId.Grain,
        Milk = 1 << ResourceId.Milk,

        [Hidden] Phosphorus = Manure | MFertilizer | DFertilizer
    }

    /// <summary>
    /// Block of 32-bit resource values.
    /// </summary>
    [Serializable]
    public struct ResourceBlock {
        public int Manure;
        public int MFertilizer;
        public int DFertilizer;
        public int Grain;
        public int Milk;

        /// <summary>
        /// Gets or sets the resource count for the given resource.
        /// </summary>
        public int this[ResourceId id] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                unsafe {
                    fixed (int* start = &Manure) {
                        return *(start + (int) id);
                    }
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                unsafe {
                    fixed (int* start = &Manure) {
                        *(start + (int) id) = value;
                    }
                }
            }
        }

        /// <summary>
        /// Gets a mask of all resources with a non-zero value.
        /// </summary>
        public ResourceMask NonZeroMask {
            get {
                ResourceMask mask = default;
                if (Manure != 0) {
                    mask |= ResourceMask.Manure;
                }
                if (MFertilizer != 0) {
                    mask |= ResourceMask.MFertilizer;
                }
                if (DFertilizer != 0) {
                    mask |= ResourceMask.DFertilizer;
                }
                if (Grain != 0) {
                    mask |= ResourceMask.Grain;
                }
                if (Milk != 0) {
                    mask |= ResourceMask.Milk;
                }
                return mask;
            }
        }

        /// <summary>
        /// Gets a mask of all resources with a positive value.
        /// </summary>
        public ResourceMask PositiveMask {
            get {
                ResourceMask mask = default;
                if (Manure > 0) {
                    mask |= ResourceMask.Manure;
                }
                if (MFertilizer > 0) {
                    mask |= ResourceMask.MFertilizer;
                }
                if (DFertilizer > 0) {
                    mask |= ResourceMask.DFertilizer;
                }
                if (Grain > 0) {
                    mask |= ResourceMask.Grain;
                }
                if (Milk > 0) {
                    mask |= ResourceMask.Milk;
                }
                return mask;
            }
        }

        /// <summary>
        /// Gets a mask of all resources with a negative value.
        /// </summary>
        public ResourceMask NegativeMask {
            get {
                ResourceMask mask = default;
                if (Manure < 0) {
                    mask |= ResourceMask.Manure;
                }
                if (MFertilizer < 0) {
                    mask |= ResourceMask.MFertilizer;
                }
                if (DFertilizer < 0) {
                    mask |= ResourceMask.DFertilizer;
                }
                if (Grain < 0) {
                    mask |= ResourceMask.Grain;
                }
                if (Milk < 0) {
                    mask |= ResourceMask.Milk;
                }
                return mask;
            }
        }

        /// <summary>
        /// Returns if this resource block is empty.
        /// </summary>
        public bool IsZero {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                return Manure == 0 && MFertilizer == 0 && DFertilizer == 0
                    && Grain == 0 && Milk == 0;
            }
        }

        /// <summary>
        /// Returns if this resource block has at least one positive component.
        /// </summary>
        public bool IsPositive {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                return Manure > 0 || MFertilizer > 0 || DFertilizer > 0
                    || Grain > 0 || Milk > 0;
            }
        }

        /// <summary>
        /// Returns the total number of resources in this block.
        /// </summary>
        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Manure + MFertilizer + DFertilizer + Grain + Milk; }
        }

        /// <summary>
        /// Returns the total number of phosphorus resources in this block.
        /// </summary>
        public int PhosphorusCount {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Manure + MFertilizer + DFertilizer; }
        }

        #region Utilities

        /// <summary>
        /// Returns if a resource block can fulfill a given request.
        /// </summary>
        static public bool Fulfills(in ResourceBlock source, in ResourceBlock request) {
            return source.PhosphorusCount >= request.PhosphorusCount
                && source.Grain >= request.Grain && source.Milk >= request.Milk;
        }

        /// <summary>
        /// Returns if a resource block can be partially added to the given source block.
        /// </summary>
        static public bool CanAddPartial(in ResourceBlock source, ref ResourceBlock production, in ResourceBlock capacity) {
            if (source.Manure + production.Manure > capacity.Manure) {
                production.Manure = capacity.Manure - source.Manure;
            }
            if (source.MFertilizer + production.MFertilizer > capacity.MFertilizer) {
                production.MFertilizer = capacity.MFertilizer - source.MFertilizer;
            }
            if (source.DFertilizer + production.DFertilizer > capacity.DFertilizer) {
                production.DFertilizer = capacity.DFertilizer - source.DFertilizer;
            }
            if (source.Grain + production.Grain > capacity.Grain) {
                production.Grain = capacity.Grain - source.Grain;
            }
            if (source.Milk + production.Milk > capacity.Milk) {
                production.Milk = capacity.Milk - source.Milk;
            }
            return production.IsPositive;
        }

        /// <summary>
        /// Returns if a resource block can be fully added to the given source block.
        /// </summary>
        static public bool CanAddFull(in ResourceBlock source, in ResourceBlock production, in ResourceBlock capacity) {
            return source.Manure + production.Manure <= capacity.Manure
                && source.MFertilizer + production.MFertilizer <= capacity.MFertilizer
                && source.DFertilizer + production.DFertilizer <= capacity.DFertilizer
                && source.Grain + production.Grain <= capacity.Grain
                && source.Milk + production.Milk <= capacity.Milk;
        }

        /// <summary>
        /// Returns if a resource block is over the specified capacity.
        /// </summary>
        static public bool IsOverCapacity(in ResourceBlock source, in ResourceBlock capacity) {
            return source.Manure > capacity.Manure && source.MFertilizer > capacity.MFertilizer && source.DFertilizer > capacity.DFertilizer
                && source.Grain > capacity.Grain && source.Milk > capacity.Milk;
        }

        /// <summary>
        /// Clamps the given block to the given capacity.
        /// </summary>
        static public void Clamp(ref ResourceBlock source, in ResourceBlock capacity) {
            if (source.Manure > capacity.Manure) {
                source.Manure = capacity.Manure;
            }
            if (source.MFertilizer > capacity.MFertilizer) {
                source.MFertilizer = capacity.MFertilizer;
            }
            if (source.DFertilizer> capacity.DFertilizer) {
                source.DFertilizer = capacity.DFertilizer;
            }
            if (source.Grain > capacity.Grain) {
                source.Grain = capacity.Grain;
            }
            if (source.Milk > capacity.Milk) {
                source.Milk = capacity.Milk;
            }
        }

        /// <summary>
        /// Clamps the given block to the given capacity, outputting the amount over capacity.
        /// </summary>
        static public void Clamp(ref ResourceBlock source, in ResourceBlock capacity, out ResourceBlock overflow) {
            overflow = default;

            if (source.Manure > capacity.Manure) {
                overflow.Manure = source.Manure - capacity.Manure;
                source.Manure = capacity.Manure;
            }
            if (source.MFertilizer > capacity.MFertilizer) {
                overflow.MFertilizer = source.MFertilizer - capacity.MFertilizer;
                source.MFertilizer = capacity.MFertilizer;
            }
            if (source.DFertilizer > capacity.DFertilizer) {
                overflow.DFertilizer = source.DFertilizer - capacity.DFertilizer;
                source.DFertilizer = capacity.DFertilizer;
            }
            if (source.Grain > capacity.Grain) {
                overflow.Grain = source.Grain - capacity.Grain;
                source.Grain = capacity.Grain;
            }
            if (source.Milk > capacity.Milk) {
                overflow.Milk = source.Milk - capacity.Milk;
                source.Milk = capacity.Milk;
            }
        }

        /// <summary>
        /// Consumess the given resource block from the given source.
        /// </summary>
        static public bool Consume(ref ResourceBlock source, ref ResourceBlock request) {
            if (source.PhosphorusCount >= request.PhosphorusCount
                && source.Grain >= request.Grain && source.Milk >= request.Milk) {
                GatherPhosphorusPrioritized(source, request.PhosphorusCount, out ResourceBlock consumed);
                consumed.Grain = request.Grain;
                consumed.Milk = request.Milk;
                source -= consumed;
                request = consumed;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Consumess the given resource block from the given source.
        /// </summary>
        static public bool Consume(ref ResourceBlock source, ResourceBlock request) {
            if (source.PhosphorusCount >= request.PhosphorusCount
                && source.Grain >= request.Grain && source.Milk >= request.Milk) {
                GatherPhosphorusPrioritized(source, request.PhosphorusCount, out ResourceBlock consumed);
                consumed.Grain = request.Grain;
                consumed.Milk = request.Milk;
                source -= consumed;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gathers phosphorus from the given source, prioritizing digested fertilizer, then mineral fertilizer, and finally manure.
        /// Returns if it was able to gather all requested phosphorus.
        /// </summary>
        static public bool GatherPhosphorusPrioritized(in ResourceBlock source, int phosphorusRequest, out ResourceBlock gathered) {
            gathered = default;
            int digested = Math.Min(phosphorusRequest, source.DFertilizer);
            gathered.DFertilizer = digested;
            phosphorusRequest -= digested;
            
            int mineral = Math.Min(phosphorusRequest, source.MFertilizer);
            gathered.MFertilizer = mineral;
            phosphorusRequest -= mineral;

            int manure = Math.Min(phosphorusRequest, source.Manure);
            gathered.Manure = manure;
            phosphorusRequest -= manure;

            return phosphorusRequest == 0;
        }

        #endregion // Utilities

        #region Operators

        static public ResourceBlock operator+(in ResourceBlock a, in ResourceBlock b) {
            return new ResourceBlock() {
                Manure = a.Manure + b.Manure,
                MFertilizer = a.MFertilizer + b.MFertilizer,
                DFertilizer = a.DFertilizer + b.DFertilizer,
                Grain = a.Grain + b.Grain,
                Milk = a.Milk + b.Milk
            };
        }

        static public ResourceBlock operator -(in ResourceBlock a, in ResourceBlock b) {
            return new ResourceBlock() {
                Manure = a.Manure - b.Manure,
                MFertilizer = a.MFertilizer - b.MFertilizer,
                DFertilizer = a.DFertilizer - b.DFertilizer,
                Grain = a.Grain - b.Grain,
                Milk = a.Milk - b.Milk
            };
        }

        static public ResourceBlock operator *(in ResourceBlock a, float multiplier) {
            return new ResourceBlock() {
                Manure = (int) (a.Manure * multiplier),
                MFertilizer = (int) (a.MFertilizer * multiplier),
                DFertilizer = (int) (a.DFertilizer * multiplier),
                Grain = (int) (a.Grain * multiplier),
                Milk = (int) (a.Milk * multiplier)
            };
        }

        static public ResourceBlock operator *(in ResourceBlock a, in ResourceBlock b) {
            return new ResourceBlock() {
                Manure = (int) (a.Manure * b.Manure),
                MFertilizer = (int) (a.MFertilizer * b.MFertilizer),
                DFertilizer = (int) (a.DFertilizer * b.DFertilizer),
                Grain = (int) (a.Grain * b.Grain),
                Milk = (int) (a.Milk * b.Grain)
            };
        }

        static public ResourceBlock operator &(in ResourceBlock a, ResourceMask mask) {
            unsafe {
                ResourceBlock block = a;
                int* ptr = &block.Manure;
                int idx = 0;
                uint idxMask = 1;
                while(idx < ResourceUtility.Count) {
                    if ((idxMask & (uint) mask) == 0) {
                        *ptr = 0;
                    }
                    ptr++;
                    idx++;
                    idxMask <<= 1;
                }
                return block;
            }
        }

        public override string ToString() {
            using(PooledStringBuilder psb = PooledStringBuilder.Create()) {
                psb.Builder.Append('[');
                if (Manure != 0) {
                    psb.Builder.Append("Manure=").AppendNoAlloc(Manure).Append(", ");
                }
                if (MFertilizer != 0) {
                    psb.Builder.Append("MFertilizer=").AppendNoAlloc(MFertilizer).Append(", ");
                }
                if (DFertilizer != 0) {
                    psb.Builder.Append("DFertilizer=").AppendNoAlloc(DFertilizer).Append(", ");
                }
                if (Grain != 0) {
                    psb.Builder.Append("Grain=").AppendNoAlloc(Grain).Append(", ");
                }
                if (Milk != 0) {
                    psb.Builder.Append("Milk=").AppendNoAlloc(Milk).Append(", ");
                }
                if (psb.Builder.Length > 1) {
                    return psb.Builder.TrimEnd(StringTrimChars).Append(']').Flush();
                } else {
                    return "[empty]";
                }
            }
        }

        static private readonly char[] StringTrimChars = new char[] { ',', ' ' };

        #endregion // Operators
    }

    /// <summary>
    /// Resources utilities.
    /// </summary>
    static public class ResourceUtility {
        /// <summary>
        /// Number of resource types.
        /// </summary>
        public const int Count = (int) ResourceId.COUNT;

        /// <summary>
        /// Returns the first set ResourceId for the given mask.
        /// </summary
        static public ResourceId FirstResource(ResourceMask mask) {
            for(int i = 0; i < Count; i++) {
                if (((uint) mask & (1u << i)) != 0) {
                    return (ResourceId) i;
                }
            }

            return ResourceId.COUNT;
        }
    }
}