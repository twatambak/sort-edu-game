using UnityEngine;

namespace OndeVai.Platform
{
    /// <summary>
    /// Device level setup for phones and tablets that does not belong to any scene object:
    /// frame rate, screen sleep and memory pressure.
    /// </summary>
    public static class MobilePlatformRuntime
    {
        /// <summary>Android and iOS default to 30 fps, which makes dragging feel sluggish.</summary>
        public const int MobileTargetFrameRate = 60;

        /// <summary>True on Android and iOS players (not in the editor, not on mobile browsers).</summary>
        public static bool IsNativeMobile =>
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            true;
#else
            false;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (!IsNativeMobile)
                return;

            Application.targetFrameRate = MobileTargetFrameRate;
            Application.lowMemory -= HandleLowMemory;
            Application.lowMemory += HandleLowMemory;
        }

        /// <summary>
        /// Keeps the screen awake while a match is running. Children often think for a while
        /// without touching the screen, and a dimming screen breaks the activity.
        /// </summary>
        public static void SetKeepAwake(bool keepAwake)
        {
            Screen.sleepTimeout = keepAwake ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
        }

        private static void HandleLowMemory()
        {
            Debug.LogWarning("Device reported low memory. Unloading unused assets.");
            Resources.UnloadUnusedAssets();
        }
    }
}
