using UnityEngine;

namespace OndeVai.Platform
{
    /// <summary>
    /// Keeps the designed play area visible on every aspect ratio. The scene is authored for a
    /// 9:16 screen; taller phones (19.5:9, 20:9) would otherwise crop the storages on the sides,
    /// so the orthographic size grows until the design width fits. Wider screens keep the design
    /// height and simply show more background.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFitter : MonoBehaviour
    {
        [Tooltip("Orthographic size the scene was authored with.")]
        [SerializeField, Min(0.1f)] private float _designOrthographicSize = 5f;

        [Tooltip("Width / height of the screen the scene was authored for.")]
        [SerializeField, Min(0.1f)] private float _designAspect = 1080f / 1920f;

        private Camera _camera;
        private int _lastWidth;
        private int _lastHeight;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (!_camera.orthographic)
                Debug.LogError("CameraFitter only supports orthographic cameras.", this);

            Fit();
        }

        // Unity has no screen resize event for world cameras; two integer comparisons per frame are negligible.
        private void LateUpdate()
        {
            if (Screen.width != _lastWidth || Screen.height != _lastHeight)
                Fit();
        }

        private void Fit()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            if (_camera == null || !_camera.orthographic || _lastHeight <= 0)
                return;

            _camera.orthographicSize = CalculateSize(_designOrthographicSize, _designAspect, (float)_lastWidth / _lastHeight);
        }

        /// <summary>Orthographic size that shows at least the design width and the design height.</summary>
        public static float CalculateSize(float designSize, float designAspect, float screenAspect)
        {
            if (screenAspect <= 0f)
                return designSize;

            float sizeForWidth = designSize * designAspect / screenAspect;
            return Mathf.Max(designSize, sizeForWidth);
        }
    }
}
