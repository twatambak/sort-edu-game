using OndeVai.Settings;
using UnityEngine;

namespace OndeVai.Audio
{
    /// <summary>
    /// Plays music and sound effects in response to gameplay events. Authored clips assigned in the
    /// inspector replace the procedural placeholders.
    /// </summary>
    public sealed class GameAudio : SingletonBase<GameAudio>
    {
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Optional authored clips (procedural placeholders are used when empty)")]
        [SerializeField] private AudioClip _music;
        [SerializeField] private AudioClip _pop;
        [SerializeField] private AudioClip _pickup;
        [SerializeField] private AudioClip _click;
        [SerializeField] private AudioClip _correct;
        [SerializeField] private AudioClip _wrong;
        [SerializeField] private AudioClip _win;
        [SerializeField] private AudioClip _lose;

        [SerializeField, Range(0f, 0.3f)] private float _pitchVariation = 0.08f;

        private GameController _gameController;
        private GameSettingsService _settings;
        private float _sfxVolume = 1f;
        private int _correctStreak;

        protected override void Awake()
        {
            base.Awake();
            if (GetInstance() != this)
                return;

            EnsureSources();
            _music = _music != null ? _music : ProceduralAudio.CreateMusicLoop();
            _pop = _pop != null ? _pop : ProceduralAudio.CreatePop();
            _pickup = _pickup != null ? _pickup : ProceduralAudio.CreatePickup();
            _click = _click != null ? _click : ProceduralAudio.CreateClick();
            _correct = _correct != null ? _correct : ProceduralAudio.CreateCorrect();
            _wrong = _wrong != null ? _wrong : ProceduralAudio.CreateWrong();
            _win = _win != null ? _win : ProceduralAudio.CreateWin();
            _lose = _lose != null ? _lose : ProceduralAudio.CreateLose();
        }

        private void Start()
        {
            _gameController = GameController.Instance;
            if (_gameController == null)
            {
                Debug.LogError("GameAudio could not find the GameController.", this);
                return;
            }

            _settings = _gameController.Settings;
            _settings.Changed += ApplySettings;
            ApplySettings(_settings.Current);

            _gameController.OnItemSpawned += HandleItemSpawned;
            _gameController.OnItemPicked += HandleItemPicked;
            _gameController.OnItemSorted += HandleItemSorted;
            _gameController.OnGameWon += HandleGameWon;
            _gameController.OnGameLost += HandleGameLost;
            _gameController.OnGameStarted += HandleGameStarted;

            _musicSource.clip = _music;
            _musicSource.loop = true;
            _musicSource.Play();
        }

        /// <summary>Plays the UI click sound.</summary>
        public void PlayClick()
        {
            PlaySfx(_click, 0f);
        }

        /// <summary>Plays any clip through the effects channel using the effects volume.</summary>
        public void PlaySfx(AudioClip clip, float pitchVariation)
        {
            if (clip == null || _sfxSource == null || _sfxVolume <= 0f)
                return;

            _sfxSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            _sfxSource.PlayOneShot(clip, _sfxVolume);
        }

        private void HandleGameStarted(GameConfiguration config)
        {
            _correctStreak = 0;
        }

        private void HandleItemSpawned(Item item)
        {
            PlaySfx(_pop, _pitchVariation);
        }

        private void HandleItemPicked(Item item)
        {
            PlaySfx(_pickup, _pitchVariation);
        }

        private void HandleItemSorted(ItemSortResult result)
        {
            if (!result.Correct)
            {
                _correctStreak = 0;
                PlaySfx(_wrong, 0f);
                return;
            }

            if (_sfxSource == null || _sfxVolume <= 0f)
                return;

            // Raise the pitch slightly on streaks to reward consecutive correct answers.
            _sfxSource.pitch = 1f + Mathf.Min(_correctStreak, 6) * 0.04f;
            _sfxSource.PlayOneShot(_correct, _sfxVolume);
            _correctStreak++;
        }

        private void HandleGameWon()
        {
            PlaySfx(_win, 0f);
        }

        private void HandleGameLost()
        {
            PlaySfx(_lose, 0f);
        }

        private void ApplySettings(GameSettings settings)
        {
            _sfxVolume = settings.SfxVolume;
            if (_musicSource != null)
                _musicSource.volume = settings.MusicVolume;
        }

        private void EnsureSources()
        {
            if (_musicSource == null)
                _musicSource = gameObject.AddComponent<AudioSource>();

            if (_sfxSource == null)
                _sfxSource = gameObject.AddComponent<AudioSource>();

            _musicSource.playOnAwake = false;
            _sfxSource.playOnAwake = false;
            _musicSource.ignoreListenerPause = true;
        }

        protected override void OnDestroy()
        {
            if (_settings != null)
                _settings.Changed -= ApplySettings;

            if (_gameController != null)
            {
                _gameController.OnItemSpawned -= HandleItemSpawned;
                _gameController.OnItemPicked -= HandleItemPicked;
                _gameController.OnItemSorted -= HandleItemSorted;
                _gameController.OnGameWon -= HandleGameWon;
                _gameController.OnGameLost -= HandleGameLost;
                _gameController.OnGameStarted -= HandleGameStarted;
            }

            base.OnDestroy();
        }
    }
}
