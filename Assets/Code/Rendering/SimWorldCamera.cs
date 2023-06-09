using FieldDay.SharedState;
using UnityEngine;

namespace Zavala.World {
    [SharedStateInitOrder(100)]
    public sealed class SimWorldCamera : SharedStateComponent {
        #region Inspector

        [Header("Camera Positioning")]
        public Camera Camera;
        public Transform LookTarget;

        [Header("Camera Movement")]
        public float CameraMoveSpeed;

        #endregion // Inspector
    }
}