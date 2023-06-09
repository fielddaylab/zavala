using System;
using UnityEngine.Scripting;

namespace FieldDay.Systems {
    /// <summary>
    /// Base interface for a game system.
    /// Systems should possess no state.
    /// </summary>
    public interface ISystem {
        /// <summary>
        /// Initializes the system.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Shuts down the system.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Indicates if the system has any work to process.
        /// </summary>
        bool HasWork();

        /// <summary>
        /// Processes available work.
        /// </summary>
        void ProcessWork(float deltaTime);
    }

    /// <summary>
    /// Attribute defining system initialization order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false), Preserve]
    public sealed class SysInitOrderAttribute : PreserveAttribute {
        public readonly int Order;

        public SysInitOrderAttribute(int order) {
            Order = order;
        }
    }

    /// <summary>
    /// Attribute defining system update order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false), Preserve]
    public sealed class SysUpdateAttribute : PreserveAttribute {
        public readonly GameLoopPhase Phase;
        public readonly int Order;

        public SysUpdateAttribute(GameLoopPhase phase, int order = 0) {
            Phase = phase;
            Order = order;
        }
    }
}