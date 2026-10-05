using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KarakuriLabo.Tests
{
    internal static class TestSceneLoader
    {
        public static AsyncOperation Load(string scene)
        {
            string path = scene.StartsWith("Assets/") ? scene : $"Assets/Scenes/{scene}.unity";
            if (Application.CanStreamedLevelBeLoaded(path))
            {
                return SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
            }
#if UNITY_EDITOR
            // Retain generic physics/placement regression fixtures without shipping
            // the retired domino level or the early sandbox in the iPhone player.
            return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore($"Editor-only prototype fixture: {path}");
            return null;
#endif
        }
    }
}
