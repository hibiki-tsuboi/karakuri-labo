using UnityEngine;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class DraggableObject : MonoBehaviour
    {
        [SerializeField] private GameObject selectionVisual;
        [SerializeField] private string displayName = "RAMP";

        public bool IsSelected { get; private set; }
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "PART" : displayName;

        private void Awake()
        {
            SetSelected(false);
        }

        private void OnDisable()
        {
            SetSelected(false);
        }

        public void Configure(GameObject visual)
        {
            if (selectionVisual != null)
            {
                selectionVisual.SetActive(false);
            }

            selectionVisual = visual;
            SetSelected(IsSelected);
        }

        public void Configure(GameObject visual, string label)
        {
            Configure(visual);
            displayName = label;
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (selectionVisual != null)
            {
                selectionVisual.SetActive(selected);
            }
        }
    }
}
