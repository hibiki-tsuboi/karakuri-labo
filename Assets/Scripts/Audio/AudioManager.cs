using UnityEngine;
using UnityEngine.UI;

namespace KarakuriLabo
{
    [DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource clearSource;
        [SerializeField] private AudioClip clearClip;
        [SerializeField] private Button soundButton;
        [SerializeField] private Text soundLabel;
        [SerializeField] private AudioClip musicClip;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.24f;

        private BackgroundMusicPlayer music;

        public bool IsMuted { get; private set; }

        private void OnEnable()
        {
            ConnectMusic();
            ConfigureSource();
            Subscribe();
            RefreshLabel();
        }

        private void OnDisable()
        {
            Unsubscribe();
            Stop();
            if (music != null)
            {
                music.Detach(this);
            }
        }

        public void Configure(AudioSource source, AudioClip clip, Button button, Text label)
        {
            Unsubscribe();
            Stop();
            clearSource = source;
            clearClip = clip;
            soundButton = button;
            soundLabel = label;
            ConfigureSource();
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
            RefreshLabel();
        }

        public void PlayClear()
        {
            if (!isActiveAndEnabled || IsMuted || clearSource == null || clearClip == null)
            {
                return;
            }

            clearSource.Stop();
            clearSource.clip = clearClip;
            clearSource.Play();
        }

        public void ConfigureMusic(AudioClip clip, float volume)
        {
            musicClip = clip;
            musicVolume = Mathf.Clamp01(volume);
            if (isActiveAndEnabled)
            {
                ConnectMusic();
                ConfigureSource();
                RefreshLabel();
            }
        }

        private void ConnectMusic()
        {
            if (!Application.isPlaying || musicClip == null)
            {
                return;
            }
            music = BackgroundMusicPlayer.Attach(this, musicClip, musicVolume, IsMuted);
            IsMuted = music.IsMuted;
        }

        public void Stop()
        {
            if (clearSource != null)
            {
                clearSource.Stop();
            }
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            if (music != null)
            {
                music.SetMuted(muted);
            }
            if (clearSource != null)
            {
                clearSource.mute = muted;
            }
            if (muted)
            {
                Stop();
            }
            RefreshLabel();
        }

        private void ConfigureSource()
        {
            if (clearSource == null)
            {
                return;
            }

            clearSource.playOnAwake = false;
            clearSource.loop = false;
            clearSource.spatialBlend = 0f;
            clearSource.volume = 0.35f;
            clearSource.mute = IsMuted;
            clearSource.clip = clearClip;
        }

        private void Subscribe()
        {
            if (soundButton != null)
            {
                soundButton.onClick.AddListener(ToggleMuted);
            }
        }

        private void Unsubscribe()
        {
            if (soundButton != null)
            {
                soundButton.onClick.RemoveListener(ToggleMuted);
            }
        }

        private void ToggleMuted() => SetMuted(!IsMuted);

        private void RefreshLabel()
        {
            if (soundLabel != null)
            {
                soundLabel.text = IsMuted ? "SOUND OFF" : "SOUND ON";
            }
        }
    }
}
