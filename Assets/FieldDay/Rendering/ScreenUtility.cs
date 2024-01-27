#if UNITY_WEBGL && !UNITY_EDITOR
#define USE_JSLIB
#endif // UNITY_WEBGL && !UNITY_EDITOR

using System.Runtime.InteropServices;
using UnityEngine;

namespace FieldDay.Rendering {
    static public class ScreenUtility {
#if UNITY_WEBGL && !UNITY_EDITOR

        [DllImport("__Internal")]
        static private extern void NativeFullscreen_SetFullscreen(bool fullscreen);

#endif // UNITY_WEBGL && !UNITY_EDITOR

        /// <summary>
        /// Sets the fullscreen mode.
        /// </summary>
        static public void SetFullscreen(bool fullscreen) {
#if USE_JSLIB
            NativeFullscreen_SetFullscreen(fullscreen);
#elif UNITY_EDITOR
            // TODO: fullscreen within editor?
            Screen.fullScreen = fullscreen;
#else
            Screen.fullScreen = fullscreen;
#endif // UNITY_WEBGL && !UNITY_EDITOR
        }

        /// <summary>
        /// Returns the fullscreen mode.
        /// </summary>
        static public bool GetFullscreen() {
            return Screen.fullScreen;
        }
    }
}