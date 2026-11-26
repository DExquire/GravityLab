using GravityLab.Core;
using UnityEngine;

namespace GravityLab.Audio
{
    /// <summary>
    /// Voice pool for synthesised one-shots (with per-sound rate limiting and simple distance attenuation),
    /// plus a music loop and a held-object hum loop. Volumes follow the saved settings.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        const int VoiceCount = 18;
        const float MinInterval = 0.04f;
        const float HearingRange = 28f;

        static AudioManager _instance;

        AudioClip[] _clips;
        float[] _lastPlayed;
        AudioSource[] _voices;
        AudioSource _music;
        AudioSource _hum;
        int _nextVoice;
        float _humTarget;

        /// <summary>Where distance attenuation is measured from (the player's head while playing).</summary>
        public static Transform Listener { get; set; }

        void Awake()
        {
            _instance = this;
            int count = System.Enum.GetValues(typeof(SoundId)).Length;
            _clips = new AudioClip[count];
            _lastPlayed = new float[count];
            for (int i = 0; i < count; i++)
            {
                _clips[i] = SynthClips.Create((SoundId)i);
                _lastPlayed[i] = -1f;
            }

            _voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++) _voices[i] = CreateSource(false);

            _music = CreateSource(true);
            _music.clip = SynthClips.CreateMusic();
            _music.Play();

            _hum = CreateSource(true);
            _hum.clip = SynthClips.CreateHum();
            _hum.volume = 0f;
            _hum.Play();

            RefreshVolumes();
        }

        AudioSource CreateSource(bool loop)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.loop = loop;
            return src;
        }

        void Update()
        {
            float target = _humTarget * SaveSystem.Data.SfxVolume * 0.35f;
            _hum.volume = Mathf.MoveTowards(_hum.volume, target, Time.unscaledDeltaTime * 1.5f);
        }

        public static void RefreshVolumes()
        {
            if (_instance == null) return;
            _instance._music.volume = SaveSystem.Data.MusicVolume * 0.5f;
        }

        public static void Play(SoundId id, float volume = 1f, float pitch = 1f, float jitter = 0.05f)
        {
            if (_instance != null) _instance.PlayInternal(id, volume, pitch, jitter);
        }

        /// <summary>World-positioned one-shot, attenuated linearly with distance from <see cref="Listener"/>.</summary>
        public static void PlayAt(SoundId id, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (_instance == null) return;
            if (Listener != null)
            {
                float d = Vector3.Distance(Listener.position, position);
                volume *= Mathf.Clamp01(1f - d / HearingRange);
            }
            _instance.PlayInternal(id, volume, pitch, 0.05f);
        }

        /// <summary>Fades the held-object hum in or out; pitch follows the hold strain.</summary>
        public static void SetHum(bool on, float pitch = 1f)
        {
            if (_instance == null) return;
            _instance._humTarget = on ? 1f : 0f;
            _instance._hum.pitch = Mathf.Lerp(_instance._hum.pitch, pitch, 0.2f);
        }

        void PlayInternal(SoundId id, float volume, float pitch, float jitter)
        {
            volume *= SaveSystem.Data.SfxVolume;
            if (volume <= 0.01f) return;
            int index = (int)id;
            float now = Time.unscaledTime;
            if (now - _lastPlayed[index] < MinInterval) return;
            _lastPlayed[index] = now;

            AudioSource src = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Length;
            src.clip = _clips[index];
            src.volume = Mathf.Clamp01(volume);
            src.pitch = pitch * (1f + Random.Range(-jitter, jitter));
            src.Play();
        }
    }
}
