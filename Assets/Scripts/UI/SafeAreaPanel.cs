using UnityEngine;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        private RectTransform panel;
        private Rect lastSafeArea;
        private Vector2Int lastSize;

        private void OnEnable()
        {
            panel = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea ||
                lastSize.x != Screen.width || lastSize.y != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }
            lastSafeArea = Screen.safeArea;
            lastSize = new Vector2Int(Screen.width, Screen.height);
            panel.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
            panel.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
        }
    }
}
