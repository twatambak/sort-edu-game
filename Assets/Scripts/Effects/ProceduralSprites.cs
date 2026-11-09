using UnityEngine;

namespace OndeVai.Effects
{
    /// <summary>
    /// Generates simple shapes (soft circle, ring, star) at runtime so effects work before final art exists.
    /// Results are cached for the lifetime of the app.
    /// </summary>
    public static class ProceduralSprites
    {
        private const int Size = 64;

        private static Sprite _circle;
        private static Sprite _ring;
        private static Sprite _star;
        private static Sprite _tear;

        public static Sprite Circle => _circle ??= Create("fx_circle", CircleAlpha);

        public static Sprite Ring => _ring ??= Create("fx_ring", RingAlpha);

        public static Sprite Star => _star ??= Create("fx_star", StarAlpha);

        public static Sprite Tear => _tear ??= Create("fx_tear", TearAlpha);

        private static Sprite Create(string spriteName, System.Func<float, float, float> alpha)
        {
            Texture2D texture = new(Size, Size, TextureFormat.RGBA32, false)
            {
                name = spriteName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color32[] pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = (x + 0.5f) / Size * 2f - 1f;
                    float v = (y + 0.5f) / Size * 2f - 1f;
                    byte a = (byte)(Mathf.Clamp01(alpha(u, v)) * 255f);
                    pixels[y * Size + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
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

        /// <summary>GLSL style smoothstep: 0 below edge0, 1 above edge1.</summary>
        private static float SmoothEdge(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
    }
}
