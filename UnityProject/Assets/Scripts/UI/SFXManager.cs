using UnityEngine;

namespace IsoRPG.UI
{
    /// <summary>
    /// Centralized sound effect player. Scene-scoped singleton.
    /// All clips are optional — gracefully skips null clips for MVP.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SFXManager : MonoBehaviour
    {
        public static SFXManager Instance { get; private set; }

        [Header("UI Sounds")]
        public AudioClip menuTick;
        public AudioClip menuConfirm;
        public AudioClip menuCancel;
        public AudioClip menuInvalid;

        [Header("Battle Sounds")]
        public AudioClip turnStart;
        public AudioClip attackHit;
        public AudioClip attackMiss;
        public AudioClip heal;
        public AudioClip unitDied;
        public AudioClip victory;
        public AudioClip defeat;

        private AudioSource _source;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PlayTick() => Play(menuTick);
        public void PlayConfirm() => Play(menuConfirm);
        public void PlayCancel() => Play(menuCancel);
        public void PlayInvalid() => Play(menuInvalid);
        public void PlayTurnStart() => Play(turnStart);
        public void PlayAttackHit() => Play(attackHit);
        public void PlayAttackMiss() => Play(attackMiss);
        public void PlayHeal() => Play(heal);
        public void PlayUnitDied() => Play(unitDied);
        public void PlayVictory() => Play(victory);
        public void PlayDefeat() => Play(defeat);

        /// <summary>Play a clip. Null-safe — silently skips if clip is null.</summary>
        public void Play(AudioClip clip)
        {
            if (clip != null && _source != null)
                _source.PlayOneShot(clip);
        }
    }
}
