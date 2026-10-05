using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KarakuriLabo.Tests
{
    [SetUpFixture]
    public class InputFocusTestSetup
    {
        private InputSettings originalSettings;
        private InputSettings testSettings;

        [OneTimeSetUp]
        public void SetUpInputFocus()
        {
            originalSettings = InputSystem.settings;
            testSettings = Object.Instantiate(originalSettings);
            testSettings.hideFlags = HideFlags.HideAndDontSave;

            // Queued test-device events must reach both gameplay and UI when the
            // automated runner's Game View is not the foreground editor window.
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            testSettings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = testSettings;
        }

        [OneTimeTearDown]
        public void RestoreInputFocus()
        {
            if (originalSettings != null)
            {
                InputSystem.settings = originalSettings;
            }
            if (testSettings != null)
            {
                Object.DestroyImmediate(testSettings);
            }
            originalSettings = null;
            testSettings = null;
        }
    }
}
