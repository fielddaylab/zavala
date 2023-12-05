using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;
using Zavala.Input;
using Zavala.UI;

namespace Zavala.World {
    [SysUpdate(GameLoopPhase.UnscaledUpdate, 100)]
    public sealed class SimWorldCameraInputSystem : SharedStateSystemBehaviour<SimWorldCamera, InputState, SimWorldState, InteractionState> {
        public override void ProcessWork(float deltaTime) {
            if ((m_StateD.AllowedInteractions & InteractionMask.Movement) == 0) {
                // Only move camera when Movement interaction is allowed
                return;
            }

            Vector2 moveDist = m_StateB.NormalizedKeyboardMoveVector * ((m_StateA.CameraMoveSpeed + 0.75f * m_StateC.RegionCount) * deltaTime);
            if (moveDist.sqrMagnitude > 0) {
                Vector3 cameraRotEuler = m_StateA.LookTarget.localEulerAngles;
                Quaternion cameraRot = Quaternion.Euler(0, cameraRotEuler.y, 0);
                Vector3 moveVec = cameraRot * Geom.SwizzleYZ(moveDist);
                m_StateA.LookTarget.Translate(moveVec, Space.World);
                // TODO: better solution?
                // temp: close context menu when moving
                //BuildingPopup.instance.CloseMenu();
            }

            float zoomDelta = m_StateB.ScrollWheel.y * m_StateA.ZoomFactor;
            if (zoomDelta == 0) {
                return;
            }
            
            Vector3 camPos = m_StateA.Camera.transform.localPosition;
            if (zoomDelta > 0) {
                camPos.z = Mathf.Min(m_StateA.CameraMaxZoomDist, camPos.z + zoomDelta);
            } else {
                camPos.z = Mathf.Max(m_StateA.CameraMinZoomDist - 1 * m_StateC.RegionCount, camPos.z + zoomDelta);
            }
            m_StateA.Camera.transform.localPosition = camPos;
        }
    }
}