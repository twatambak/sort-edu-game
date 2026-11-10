using UnityEngine;

namespace OndeVai.Effects
{
    /// <summary>
    /// Generates simple shapes at runtime so effects and highlights work before final art exists.
    /// Results are cached for the lifetime of the app.
    /// </summary>
    public static class ProceduralSprites
    {
        private const int Size = 64;
        private const int LargeSize = 256;
        private const int SliceBorder = 24;

        private static Sprite _circle;
        private static Sprite _ring;
        private static Sprite _star;
        private static Sprite _starLarge;
        private static Sprite _tear;
        private static Sprite _glow;
        private static Sprite _chevron;
        private static Sprite _roundedFrame;
        private static Sprite _roundedPanel;

        public static Sprite Circle => _circle ??= Create("fx_circle", CircleAlpha);

        public static Sprite Ring => _ring ??= Create("fx_ring", RingAlpha);

        public static Sprite Star => _star ??= Create("fx_star", StarAlpha);

        /// <summary>High resolution star for UI, where the particle sized one would look blurry.</summary>
        public static Sprite StarLarge => _starLarge ??= Create("fx_star_large", StarAlpha, LargeSize);

        public static Sprite Tear => _tear ??= Create("fx_tear", TearAlpha);

        /// <summary>Soft radial falloff, used for glows and drop shadows.</summary>
        public static Sprite Glow => _glow ??= Create("fx_glow", GlowAlpha);

        /// <summary>Arrow head pointing right. Flip X to point left.</summary>
        public static Sprite Chevron => _chevron ??= Create("fx_chevron", ChevronAlpha);

        /// <summary>Rounded outline with a 9-slice border, for sliced SpriteRenderers.</summary>
        public static Sprite RoundedFrame => _roundedFrame ??= Create("fx_rounded_frame", FrameAlpha, Size, SliceBorder);

        /// <summary>Filled rounded rectangle with a 9-slice border, for sliced UI images.</summary>
        public static Sprite RoundedPanel => _roundedPanel ??= Create("fx_rounded_panel", PanelAlpha, Size, SliceBorder);

        private static Sprite Create(string spriteName, System.Func<float, float, float> alpha, int size = Size, int border = 0)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
            {
                name = spriteName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f;
                    float v = (y + 0.5f) / size * 2f - 1f;
                    byte a = (byte)(Mathf.Clamp01(alpha(u, v)) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            sprite.name = spriteName;
            return sprite;
        }

        private static float CircleAlpha(float u, float v)
        {
            float distance = Mathf.Sqrt(u * u + v * v);
            return 1f - SmoothEdge(0.85f, 1f, distance);
        }

        private static float RingAlpha(float u, float v)
        {
            float distance = Mathf.Sqrt(u * u + v * v);
            float outer = 1f - SmoothEdge(0.88f, 1f, distance);
            float inner = SmoothEdge(0.62f, 0.74f, distance);
            return outer * inner;
        }

        private static float StarAlpha(float u, float v)
        {
            const int points = 5;
            float angle = Mathf.Atan2(v, u) - Mathf.PI / 2f;
            float distance = Mathf.Sqrt(u * u + v * v);
            float sector = Mathf.Repeat(angle, 2f * Mathf.PI / points) / (2f * Mathf.PI / points);
            float radius = Mathf.Lerp(1f, 0.45f, Mathf.Abs(sector - 0.5f) * -2f + 1f);
            return 1f - SmoothEdge(radius - 0.08f, radius, distance);
        }

        private static float TearAlpha(float u, float v)
        {
            float distance = Mathf.Sqrt(u * u + v * v);
            float direction = distance > 0.001f ? v / distance : 0f;
            float edge = 0.74f - direction * 0.28f;
            return 1f - SmoothEdge(edge - 0.1f, edge, distance);
        }

        private static float GlowAlpha(float u, float v)
        {
            float falloff = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v));
            return falloff * falloff;
        }

        private static float ChevronAlpha(float u, float v)
        {
            float height = Mathf.Abs(v);
            float centerLine = 0.42f - height * 0.9f;
            float band = 1f - SmoothEdge(0.2f, 0.27f, Mathf.Abs(u - centerLine));
            float cap = 1f - SmoothEdge(0.78f, 0.86f, height);
            return band * cap;
        }

        private static float FrameAlpha(float u, float v)
        {
            float distance = RoundedBoxDistance(u, v, 0.6f);
            float outer = 1f - SmoothEdge(-0.06f, 0f, distance);
            float inner = SmoothEdge(-0.32f, -0.26f, distance);
            return outer * inner;
        }

        private static float PanelAlpha(float u, float v)
        {
            return 1f - SmoothEdge(-0.06f, 0f, RoundedBoxDistance(u, v, 0.6f));
        }

        /// <summary>Signed distance to a rounded box that fills the -1..1 square. Negative inside.</summary>
        private static float RoundedBoxDistance(float u, float v, float radius)
        {
            float qx = Mathf.Abs(u) - (1f - radius);
            float qy = Mathf.Abs(v) - (1f - radius);
            float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
            float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return outside + inside - radius;
        }

        /// <summary>GLSL style smoothstep: 0 below edge0, 1 above edge1.</summary>
        private static float SmoothEdge(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
    }
}
