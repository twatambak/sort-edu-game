using UnityEngine;

namespace OndeVai.Effects
{
    /// <summary>
    /// Particle recipes used by the gameplay feedback. Kept apart from the component that decides
    /// when to play them, so the look of an effect can be tuned in one place.
    /// </summary>
    public static class FeedbackBursts
    {
        private static readonly Vector2 FullCircle = new(0f, 360f);

        /// <summary>Small sparkles around an item that just appeared.</summary>
        public static BurstSettings SpawnSparkles(Color[] colors) => new()
        {
            Sprite = ProceduralSprites.Circle,
            Colors = colors,
            Count = 6,
            MinSpeed = 1.2f,
            MaxSpeed = 2f,
            Lifetime = 0.35f,
            StartSize = 0.18f,
            AngleRange = FullCircle
        };

        /// <summary>Ring that expands from an item when it is picked up.</summary>
        public static BurstSettings PickupRing(Color[] colors) => new()
        {
            Sprite = ProceduralSprites.Ring,
            Colors = colors,
            Count = 1,
            Lifetime = 0.3f,
            StartSize = 0.6f,
            EndScale = 2.2f,
            AngleRange = Vector2.zero
        };

        /// <summary>Sparkle left behind by an item while it is dragged.</summary>
        public static BurstSettings DragTrail(Color[] colors) => new()
        {
            Sprite = ProceduralSprites.Star,
            Colors = colors,
            Count = 1,
            MinSpeed = 0.1f,
            MaxSpeed = 0.5f,
            Lifetime = 0.45f,
            Gravity = 0.8f,
            StartSize = 0.2f,
            Spin = 240f,
            AngleRange = FullCircle
        };

        /// <summary>Fountain of stars for a right answer.</summary>
        public static BurstSettings CorrectStars(Color[] colors) => new()
        {
            Sprite = ProceduralSprites.Star,
            Colors = colors,
            Count = 14,
            MinSpeed = 2.5f,
            MaxSpeed = 5f,
            Lifetime = 0.7f,
            Gravity = 6f,
            StartSize = 0.45f,
            Spin = 360f,
            AngleRange = new Vector2(20f, 160f)
        };

        /// <summary>Single expanding ring. <paramref name="size"/> is the starting diameter in world units.</summary>
        public static BurstSettings Ring(Color[] colors, float size) => new()
        {
            Sprite = ProceduralSprites.Ring,
            Colors = colors,
            Count = 1,
            Lifetime = 0.35f,
            StartSize = size,
            EndScale = 1.8f,
            AngleRange = Vector2.zero
        };

        /// <summary>Soft glow behind a right answer.</summary>
        public static BurstSettings Glow(Color[] colors) => new()
        {
            Sprite = ProceduralSprites.Glow,
            Colors = colors,
            Count = 1,
            Lifetime = 0.4f,
            StartSize = 2.4f,
            EndScale = 1.5f,
            AngleRange = Vector2.zero
        };

        /// <summary>Round burst of small spinning stars.</summary>
        public static BurstSettings Firework(Color[] colors) => new()
        {
            Sprite = ProceduralSprites.Star,
            Colors = colors,
            Count = 10,
            MinSpeed = 1.8f,
            MaxSpeed = 3.4f,
            Lifetime = 0.9f,
            Gravity = 1.2f,
            StartSize = 0.24f,
            Spin = 540f,
            AngleRange = FullCircle
        };

        /// <summary>Grey puff for a wrong answer.</summary>
        public static BurstSettings WrongPuff(Color[] colors) => new()
        {
            Sprite = ProceduralSprites.Circle,
            Colors = colors,
            Count = 8,
            MinSpeed = 1f,
            MaxSpeed = 2.2f,
            Lifetime = 0.45f,
            Gravity = -1f,
            StartSize = 0.3f,
            AngleRange = FullCircle
        };

        /// <summary>Falling tear drops.</summary>
        public static BurstSettings Tears(Color[] colors, int count) => new()
        {
            Sprite = ProceduralSprites.Tear,
            Colors = colors,
            Count = count,
            MinSpeed = 0.7f,
            MaxSpeed = 1.5f,
            Lifetime = 0.8f,
            Gravity = 2.8f,
            StartSize = 0.28f,
            Spin = 18f,
            AngleRange = new Vector2(230f, 310f)
        };

        /// <summary>Particles dropped by the timer every second. Urgent ticks are brighter stars.</summary>
        public static BurstSettings TimerTick(Color[] colors, bool urgent) => new()
        {
            Sprite = urgent ? ProceduralSprites.Star : ProceduralSprites.Circle,
            Colors = colors,
            Count = urgent ? 5 : 2,
            MinSpeed = 0.3f,
            MaxSpeed = 0.9f,
            Lifetime = 0.45f,
            StartSize = 0.16f,
            AngleRange = new Vector2(235f, 305f)
        };

        /// <summary>Confetti that rains from the top of the screen at the end of a match.</summary>
        public static BurstSettings Confetti(Color[] colors, bool stars) => new()
        {
            Sprite = stars ? ProceduralSprites.Star : ProceduralSprites.Circle,
            Colors = colors,
            Count = 12,
            MinSpeed = 0.5f,
            MaxSpeed = 2.5f,
            Lifetime = 2.2f,
            Gravity = 3.5f,
            StartSize = 0.3f,
            Spin = 540f,
            AngleRange = new Vector2(200f, 340f)
        };
    }
}
