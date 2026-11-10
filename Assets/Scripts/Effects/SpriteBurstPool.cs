using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace OndeVai.Effects
{
    /// <summary>Parameters of a burst of sprite particles.</summary>
    public struct BurstSettings
    {
        public Sprite Sprite;
        public Color[] Colors;
        public int Count;
        public float MinSpeed;
        public float MaxSpeed;
        public float Lifetime;
        public float Gravity;
        public float StartSize;
        public float Spin;

        /// <summary>Size at the end of the lifetime as a multiple of <see cref="StartSize"/>. Zero uses the default shrink.</summary>
        public float EndScale;

        /// <summary>Direction range in degrees. 0..360 is a full circle.</summary>
        public Vector2 AngleRange;
    }

    /// <summary>
    /// Lightweight pooled particle system built from SpriteRenderers animated by DOTween.
    /// Avoids ParticleSystem material setup and allocates nothing after warm-up.
    /// </summary>
    public sealed class SpriteBurstPool : MonoBehaviour
    {
        private const float DefaultEndScale = 0.4f;

        [SerializeField, Min(1)] private int _prewarmCount = 48;
        [SerializeField] private string _sortingLayerName = "Default";
        [SerializeField] private int _sortingOrder = 500;

        private readonly Stack<SpriteRenderer> _available = new();

        private void Awake()
        {
            for (int i = 0; i < _prewarmCount; i++)
                _available.Push(CreateParticle());
        }

        /// <summary>Emits a burst at a world position.</summary>
        public void Emit(Vector3 position, in BurstSettings settings)
        {
            for (int i = 0; i < settings.Count; i++)
            {
                SpriteRenderer particle = Rent();
                Color color = settings.Colors != null && settings.Colors.Length > 0
                    ? settings.Colors[Random.Range(0, settings.Colors.Length)]
                    : Color.white;

                float angle = Random.Range(settings.AngleRange.x, settings.AngleRange.y) * Mathf.Deg2Rad;
                Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(settings.MinSpeed, settings.MaxSpeed);
                Animate(particle, position, velocity, color, settings);
            }
        }

        private void Animate(SpriteRenderer particle, Vector3 origin, Vector2 velocity, Color color, in BurstSettings settings)
        {
            Transform particleTransform = particle.transform;
            particle.sprite = settings.Sprite;
            particle.color = color;
            particleTransform.position = origin;
            particleTransform.localScale = Vector3.one * settings.StartSize;
            particleTransform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            particle.gameObject.SetActive(true);

            float gravity = settings.Gravity;
            float lifetime = Mathf.Max(0.05f, settings.Lifetime * Random.Range(0.8f, 1.2f));
            float spin = Random.Range(-settings.Spin, settings.Spin);
            float startSize = settings.StartSize;
            float endScale = settings.EndScale > 0f ? settings.EndScale : DefaultEndScale;

            DOTween.To(() => 0f, t =>
                {
                    float time = t * lifetime;
                    Vector3 offset = new(velocity.x * time, velocity.y * time - 0.5f * gravity * time * time, 0f);
                    particleTransform.position = origin + offset;
                    particleTransform.Rotate(0f, 0f, spin * Time.deltaTime);
                    particleTransform.localScale = Vector3.one * (startSize * Mathf.LerpUnclamped(1f, endScale, t));
                    Color faded = color;
                    faded.a = color.a * (1f - t * t);
                    particle.color = faded;
                }, 1f, lifetime)
                .SetEase(Ease.Linear)
                .SetLink(gameObject)
                .OnComplete(() => Return(particle));
        }

        private SpriteRenderer Rent()
        {
            return _available.Count > 0 ? _available.Pop() : CreateParticle();
        }

        private void Return(SpriteRenderer particle)
        {
            if (particle == null)
                return;

            particle.gameObject.SetActive(false);
            _available.Push(particle);
        }

        private SpriteRenderer CreateParticle()
        {
            GameObject particleObject = new("Particle");
            particleObject.transform.SetParent(transform, false);
            particleObject.SetActive(false);
            SpriteRenderer spriteRenderer = particleObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingLayerName = _sortingLayerName;
            spriteRenderer.sortingOrder = _sortingOrder;
            return spriteRenderer;
        }
    }
}
