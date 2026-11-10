using UnityEngine;

namespace OndeVai.Gameplay
{
    /// <summary>A cardinal direction used to sort a swipe card.</summary>
    public enum SwipeDirection
    {
        Left,
        Right
    }

    /// <summary>Maps scenario categories to their fixed swipe directions.</summary>
    public static class SwipeDirectionUtility
    {
        public static bool TryGetGroup(GameConfiguration config, SwipeDirection direction, out ItemGroup group)
        {
            if (config == null)
            {
                group = default;
                return false;
            }

            switch (direction)
            {
                case SwipeDirection.Right:
                    group = config.ItemGroupA;
                    return true;
                case SwipeDirection.Left:
                    group = config.ItemGroupB;
                    return true;
                default:
                    group = default;
                    return false;
            }
        }

        public static bool TryGetDirection(GameConfiguration config, ItemGroup group, out SwipeDirection direction)
        {
            if (config == null)
            {
                direction = default;
                return false;
            }

            if (group == config.ItemGroupA)
                direction = SwipeDirection.Right;
            else if (group == config.ItemGroupB)
                direction = SwipeDirection.Left;
            else
            {
                direction = default;
                return false;
            }

            return true;
        }

        public static Vector2 Vector(SwipeDirection direction)
        {
            return direction switch
            {
                SwipeDirection.Left => Vector2.left,
                SwipeDirection.Right => Vector2.right,
                _ => Vector2.zero
            };
        }
    }
}
