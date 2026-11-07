using UnityEngine;

namespace OndeVai.UI
{
    /// <summary>
    /// Keeps a full screen RectTransform inside <see cref="Screen.safeArea"/> so buttons are not hidden
    /// by notches or rounded corners. Reacts to resolution and orientation changes without polling.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _appliedArea;
        private Vector2Int _appliedResolution;
        private bool _isApplying;

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        private void Apply()
        {
            if (_isApplying || _rect == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            Rect area = Screen.safeArea;
            Vector2Int resolution = new(Screen.width, Screen.height);
            if (area == _appliedArea && resolution == _appliedResolution)
                return;

            _isApplying = true;
            _appliedArea = area;
            _appliedResolution = resolution;

            Vector2 min = area.position;
            Vector2 max = area.position + area.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
            _isApplying = false;
        }
    }
}
