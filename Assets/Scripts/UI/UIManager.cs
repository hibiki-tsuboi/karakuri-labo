using UnityEngine;
using UnityEngine.UI;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private Text clearText;

        public bool IsConfigured => clearText != null;

        private void Awake()
        {
            if (IsConfigured)
            {
                SetCleared(false);
            }
        }

        public void Configure(Text text)
        {
            clearText = text;
            if (IsConfigured)
            {
                SetCleared(false);
            }
        }

        public void SetCleared(bool cleared)
        {
            if (!IsConfigured)
            {
                Debug.LogError("UIManager requires a CLEAR text reference.", this);
                return;
            }

            clearText.text = GameText.Clear;
            clearText.enabled = cleared;
        }
    }
}
