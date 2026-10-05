using UnityEngine;
using UnityEngine.SceneManagement;

namespace KarakuriLabo
{
    /// <summary>One music voice survives NEXT; stage UI and clear effects stay in their scenes.</summary>
    [DisallowMultipleComponent]
    public sealed class BackgroundMusicPlayer : MonoBehaviour
    {
        private static BackgroundMusicPlayer instance;
        private AudioSource source;
        private AudioManager owner;
        private float targetVolume;
        private float fade;
        private bool appPaused;
        private bool suspended;

        public bool IsMuted { get; private set; }

        public static BackgroundMusicPlayer Attach(AudioManager manager, AudioClip clip, float volume, bool muted)
        {
            if (instance == null)
            {
                instance = new GameObject("BackgroundMusic").AddComponent<BackgroundMusicPlayer>();
                instance.SetMuted(muted);
            }
            instance.owner = manager;
            instance.targetVolume = volume;
            if (instance.source.clip != clip)
            {
                instance.source.clip = clip;
                instance.fade = 0f;
                instance.source.volume = 0f;
                instance.source.Play();
                instance.suspended = false;
            }
            instance.UpdatePlayback();
            return instance;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.priority = 128;
            source.volume = 0f;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        public void Detach(AudioManager manager)
        {
            if (owner == manager)
            {
                // Defer pausing to Update so a scene handoff can attach its new owner in the same frame.
                owner = null;
            }
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            source.mute = muted;
            if (muted)
            {
                fade = 0f;
                source.volume = 0f;
            }
        }

        private void Update()
        {
            UpdatePlayback();
            if (!suspended && !IsMuted)
            {
                fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / 0.6f);
                source.volume = targetVolume * fade;
            }
        }

        private void UpdatePlayback()
        {
            bool shouldSuspend = appPaused || owner == null || !owner.isActiveAndEnabled;
            if (shouldSuspend == suspended)
            {
                return;
            }
            suspended = shouldSuspend;
            if (suspended)
            {
                source.Pause();
            }
            else
            {
                fade = 0f;
                source.volume = 0f;
                source.UnPause();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            appPaused = paused;
            UpdatePlayback();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single && owner == null)
            {
                // Editor-only prototypes and non-game scenes must not inherit campaign music.
                source.Stop();
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
