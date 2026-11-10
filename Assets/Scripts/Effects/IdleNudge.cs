using System;
using DG.Tweening;

namespace OndeVai.Effects
{
    /// <summary>
    /// Fires a callback when nothing happened for a while, so the game can gently call the child's
    /// attention. Uses game time, so it never fires while the match is paused.
    /// </summary>
    public sealed class IdleNudge
    {
        private readonly float _delay;
        private readonly Action _onIdle;
        private Tween _timer;

        /// <param name="delay">Seconds of inactivity before the callback. Zero or less disables the nudge.</param>
        public IdleNudge(float delay, Action onIdle)
        {
            _delay = delay;
            _onIdle = onIdle ?? throw new ArgumentNullException(nameof(onIdle));
        }

        /// <summary>Restarts the countdown. Call on every sign of activity.</summary>
        public void Arm()
        {
            Cancel();
            if (_delay > 0f)
                _timer = DOVirtual.DelayedCall(_delay, Fire, false);
        }

        public void Cancel()
        {
            _timer?.Kill();
            _timer = null;
        }

        private void Fire()
        {
            _timer = null;
            _onIdle();
        }
    }
}
